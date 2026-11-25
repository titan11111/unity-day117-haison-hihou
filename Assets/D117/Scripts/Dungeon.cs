using System; using System.Collections; using System.Collections.Generic; using UnityEngine;
namespace Day117
{
    /// 第4章の3Dダンジョン（ウィザードリィ風：1マスずつ前後に進み、90度ずつ向きを変える）
    /// 地図は文字列（# 壁／. 床／S 開始／G 出口／その他の文字 = イベント）。映像はRenderTextureに描いて場面窓に出す
    public class Dungeon : MonoBehaviour
    {
        public string[] rows; public int W, H, x, y, dir; // dir 0=北(-y) 1=東 2=南 3=西
        public bool moving; public Vector2Int start, goal;
        public RenderTexture rt; public Texture2D mapTex;
        public Action<char, int, int> onEnter;
        bool[,] seen; Camera cam; Light torch, lumiLight; Transform world;
        static readonly int[] DX = { 0, 1, 0, -1 }, DY = { -1, 0, 1, 0 };
        const float S = 2f, WH = 2.6f, EYE = 1.25f;

        public void Build(string map)
        {
            if (world) Destroy(world.gameObject);
            rows = map.Split('/'); H = rows.Length; W = rows[0].Length; seen = new bool[W, H];
            world = new GameObject("DungeonWorld").transform; world.position = new Vector3(1000, 0, 1000);
            Material wall = Mat("wall"), floor = Mat("floor"), ceil = Mat("ceil");
            for (int yy = 0; yy < H; yy++)
                for (int xx = 0; xx < W; xx++)
                {
                    char c = rows[yy][xx]; var p = Pos(xx, yy);
                    if (c == '#')
                    {
                        if (!NearFloor(xx, yy)) continue;
                        var b = Prim(PrimitiveType.Cube, wall); b.position = p + Vector3.up * WH / 2; b.localScale = new Vector3(S, WH, S);
                        continue;
                    }
                    var f = Prim(PrimitiveType.Quad, floor); f.position = p; f.rotation = Quaternion.Euler(90, 0, 0); f.localScale = Vector3.one * S;
                    var cl = Prim(PrimitiveType.Quad, ceil); cl.position = p + Vector3.up * WH; cl.rotation = Quaternion.Euler(-90, 0, 0); cl.localScale = Vector3.one * S;
                    if (c == 'S') start = new Vector2Int(xx, yy);
                    if (c == 'G') goal = new Vector2Int(xx, yy);
                }
            // 出口の蒼い光
            var gl = new GameObject("GoalLight").AddComponent<Light>(); gl.transform.SetParent(world); gl.transform.position = Pos(goal.x, goal.y) + Vector3.up * 1.2f;
            gl.type = LightType.Point; gl.color = new Color(.35f, .65f, 1f); gl.range = 6; gl.intensity = 2.5f; gl.renderMode = LightRenderMode.ForcePixel;

            if (!rt) { rt = new RenderTexture(1280, 960, 16); rt.Create(); }
            var cgo = new GameObject("DungeonCam"); cgo.transform.SetParent(world); cam = cgo.AddComponent<Camera>();
            cam.fieldOfView = 72; cam.nearClipPlane = .05f; cam.farClipPlane = 40; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.01f, .015f, .03f); cam.targetTexture = rt;
            torch = new GameObject("Torch").AddComponent<Light>(); torch.transform.SetParent(cgo.transform, false); torch.transform.localPosition = new Vector3(.3f, -.2f, .2f);
            torch.type = LightType.Point; torch.range = 9; torch.intensity = 2.2f; torch.color = new Color(1f, .82f, .6f); torch.renderMode = LightRenderMode.ForcePixel;
            lumiLight = new GameObject("Lumi").AddComponent<Light>(); lumiLight.transform.SetParent(cgo.transform, false); lumiLight.transform.localPosition = new Vector3(-.4f, .2f, .3f);
            lumiLight.type = LightType.Point; lumiLight.range = 7; lumiLight.color = new Color(.4f, .7f, 1f); lumiLight.renderMode = LightRenderMode.ForcePixel;
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 4);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 2.5f; RenderSettings.fogEndDistance = 14f;
            RenderSettings.fogColor = new Color(.01f, .015f, .03f); RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.13f, .14f, .17f);
            mapTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Place(start.x, start.y, 1);
        }
        Transform Prim(PrimitiveType t, Material m)
        {
            var g = GameObject.CreatePrimitive(t); var col = g.GetComponent<Collider>(); if (col) Destroy(col);
            g.GetComponent<Renderer>().sharedMaterial = m; g.transform.SetParent(world); return g.transform;
        }
        static Material Mat(string n)
        {
            var m = Resources.Load<Material>("Dungeon/" + n); if (m) return m;
            var mm = new Material(Shader.Find("Standard")); mm.mainTexture = Resources.Load<Texture2D>("Dungeon/" + n); return mm;
        }
        Vector3 Pos(int px, int py) => world.position + new Vector3(px * S, 0, -py * S);
        bool NearFloor(int px, int py)
        {
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                { int nx = px + dx, ny = py + dy; if (nx >= 0 && ny >= 0 && nx < W && ny < H && rows[ny][nx] != '#') return true; }
            return false;
        }
        public bool Walkable(int px, int py) => px >= 0 && py >= 0 && px < W && py < H && rows[py][px] != '#';
        public char Here => rows[y][x];

        public void Place(int px, int py, int d)
        {
            x = px; y = py; dir = d; StopAllCoroutines(); moving = false;
            cam.transform.position = Pos(x, y) + Vector3.up * EYE; cam.transform.rotation = Quaternion.Euler(0, dir * 90, 0); Mark();
        }
        public void SetActive(bool on) { if (world) world.gameObject.SetActive(on); }
        public void Clear() { if (world) Destroy(world.gameObject); world = null; }

        public void Step(int sign)
        {
            if (moving) return; int nx = x + DX[dir] * sign, ny = y + DY[dir] * sign;
            if (!Walkable(nx, ny)) { StartCoroutine(Bump(sign)); return; }
            StartCoroutine(MoveTo(nx, ny));
        }
        public void Turn(int d) { if (!moving) StartCoroutine(TurnTo((dir + d + 4) % 4)); }
        float Dur => Novel.FastTest ? .02f : .22f;
        IEnumerator MoveTo(int nx, int ny)
        {
            moving = true; var a = cam.transform.position; var b = Pos(nx, ny) + Vector3.up * EYE;
            for (float t = 0; t < Dur; t += Time.deltaTime) { float k = t / Dur; cam.transform.position = Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * .06f; yield return null; }
            cam.transform.position = b; x = nx; y = ny; Mark(); moving = false;
            onEnter?.Invoke(rows[y][x], x, y);
        }
        IEnumerator TurnTo(int nd)
        {
            moving = true; var a = cam.transform.rotation; var b = Quaternion.Euler(0, nd * 90, 0);
            for (float t = 0; t < Dur; t += Time.deltaTime) { cam.transform.rotation = Quaternion.Slerp(a, b, t / Dur); yield return null; }
            cam.transform.rotation = b; dir = nd; moving = false;
        }
        IEnumerator Bump(int sign)
        {
            moving = true; var a = cam.transform.position; var f = Quaternion.Euler(0, dir * 90, 0) * Vector3.forward * .18f * sign;
            for (float t = 0; t < .14f; t += Time.deltaTime) { cam.transform.position = a + f * Mathf.Sin(t / .14f * Mathf.PI); yield return null; }
            cam.transform.position = a; moving = false;
        }

        /// 出口までの近さ（0〜1）。ルミの宝石レーダー用
        public float Closeness => 1f - Mathf.Clamp01((Mathf.Abs(x - goal.x) + Mathf.Abs(y - goal.y)) / 22f);

        void Mark()
        {
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                { int nx = x + dx, ny = y + dy; if (nx >= 0 && ny >= 0 && nx < W && ny < H) seen[nx, ny] = true; }
            for (int yy = 0; yy < H; yy++)
                for (int xx = 0; xx < W; xx++)
                {
                    Color c = new Color(0, 0, 0, 0);
                    if (seen[xx, yy]) c = rows[yy][xx] == '#' ? new Color(.25f, .3f, .36f, 1) : new Color(.75f, .8f, .7f, 1);
                    if (seen[xx, yy] && rows[yy][xx] == 'G') c = new Color(.35f, .7f, 1f, 1);
                    mapTex.SetPixel(xx, H - 1 - yy, c);
                }
            mapTex.SetPixel(x, H - 1 - y, new Color(1f, .45f, .3f, 1)); mapTex.Apply();
        }

        void Update()
        {
            if (!torch) return;
            torch.intensity = 2.1f + Mathf.PerlinNoise(Time.time * 3f, 0) * .5f;
            lumiLight.intensity = Mathf.Lerp(.3f, 2.6f, Closeness) * (.85f + .15f * Mathf.Sin(Time.time * 3));
        }

        /// 自動テスト用：出口までの最短経路を1手進める
        public void AutoStep()
        {
            if (moving) return;
            var prev = new Dictionary<Vector2Int, Vector2Int>(); var q = new Queue<Vector2Int>(); var s = new Vector2Int(x, y); q.Enqueue(s); prev[s] = s;
            while (q.Count > 0)
            {
                var c = q.Dequeue(); if (c == goal) break;
                for (int d = 0; d < 4; d++) { var n = new Vector2Int(c.x + DX[d], c.y + DY[d]); if (Walkable(n.x, n.y) && !prev.ContainsKey(n)) { prev[n] = c; q.Enqueue(n); } }
            }
            if (!prev.ContainsKey(goal)) return;
            var cur = goal; while (prev[cur] != s) cur = prev[cur];
            int need = 0; for (int d = 0; d < 4; d++) if (x + DX[d] == cur.x && y + DY[d] == cur.y) need = d;
            if (need == dir) Step(1); else Turn(((need - dir + 4) % 4) == 3 ? -1 : 1);
        }
    }
}
