// Day117 メニュー：組み直す／シナリオ検査／自動テスト＋画面チェック／横・縦で遊ぶ
using System.Collections.Generic; using System.IO; using System.Linq; using System.Text;
using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine; using UnityEngine.SceneManagement;
namespace Day117.EditorTools
{
    [InitializeOnLoad]
    static class D117Menu
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        public static string Shots => Path.GetFullPath("../Shots");
        static readonly (uint w, uint h, string tag)[] Sizes = { (1920, 1080, "landscape"), (2340, 1080, "phonewide"), (1080, 2340, "portrait") };
        static D117Menu() { EditorApplication.playModeStateChanged += OnState; }

        [MenuItem("Day117/組み直す")]
        public static void Rebuild()
        {
            var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag = "MainCamera";
            cam.gameObject.AddComponent<AudioListener>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.orthographic = true;
            new GameObject("Novel").AddComponent<Day117.Novel>();
            // 3Dダンジョンの材質（アセットにしておくとWebGLでもシェーダーが消されない）
            foreach (var n in new[] { "wall", "floor", "ceil" })
            {
                var path = "Assets/D117/Resources/Dungeon/" + n + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
                m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/D117/Resources/Dungeon/" + n + ".png");
                m.SetFloat("_Glossiness", n == "wall" ? .3f : .15f); EditorUtility.SetDirty(m);
            }
            // フォグの描画が削られないよう、シーンにもフォグを入れておく
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 2.5f; RenderSettings.fogEndDistance = 14f;
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(sc, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "廃村の蒼き秘宝"; PlayerSettings.companyName = "TitanUnity100";
                        PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("D117: シーンを組み直しました " + ScenePath);
        }

        [MenuItem("Day117/シナリオ検査")]
        public static string Check()
        {
            var ch = Day117.ScenarioLoader.LoadAll();
            var map = ch.nodes.ToDictionary(n => n.id); var sb = new StringBuilder(); int err = 0;
            void Bad(string s) { err++; sb.AppendLine("NG " + s); }
            // ダンジョン（type=="dungeon"）は events="P:id,V:id,..." でマス上のイベント先に飛ぶので、通常のnext/choicesに加えてここも辿る
            IEnumerable<string> Targets(Day117.Node n)
            {
                var ts = new[] { n.next, n.exitTo }.Concat(n.choices.Select(c => c.to));
                if (!string.IsNullOrEmpty(n.events)) ts = ts.Concat(n.events.Split(',').Select(e => e.Contains(":") ? e.Split(':')[1] : null));
                return ts;
            }
            foreach (var n in ch.nodes)
            {
                foreach (var t in Targets(n))
                    if (!string.IsNullOrEmpty(t) && !map.ContainsKey(t)) Bad(n.id + " → " + t + " がない");
                foreach (var l in n.lines)
                {
                    if (Day117.Cast.IsChar(l.who) && !Resources.Load<Sprite>("Chars/" + l.who + "_" + (l.expr ?? "normal"))) Bad("立ち絵なし " + l.who + "_" + l.expr);
                    if (l.show != null) foreach (var s in l.show) if (!Resources.Load<Sprite>("Chars/" + s.id + "_" + (s.expr ?? "normal"))) Bad("立ち絵なし " + s.id + "_" + s.expr);
                }
                if (!string.IsNullOrEmpty(n.bg) && n.bg != "black" && !Resources.Load<Sprite>("BG/" + n.bg)) Bad("背景なし " + n.bg);
            }
            // 開始から END まで行けるか
            var seen = new HashSet<string>(); var q = new Queue<string>(); q.Enqueue(ch.start); bool end = false;
            while (q.Count > 0)
            {
                var id = q.Dequeue(); if (!seen.Add(id) || !map.ContainsKey(id)) continue; var n = map[id]; if (n.type == "end") end = true;
                foreach (var t in Targets(n)) if (!string.IsNullOrEmpty(t)) q.Enqueue(t);
            }
            if (!end) Bad("ENDに届かない");
            foreach (var n in ch.nodes) if (!seen.Contains(n.id)) Bad("たどり着けない " + n.id);
            int lines = ch.nodes.Sum(n => n.lines.Count), voices = ch.nodes.Sum(n => n.lines.Count(l => !string.IsNullOrEmpty(l.voice) && Resources.Load<AudioClip>("Voice/" + l.voice)));
            sb.Insert(0, $"シナリオ検査：ノード{ch.nodes.Count} 行{lines} 声{voices}/{lines} エラー{err}\n");
            Directory.CreateDirectory(Shots); File.WriteAllText(Path.Combine(Shots, "scenario_check.txt"), sb.ToString());
            if (err > 0) Debug.LogWarning(sb.ToString()); else Debug.Log(sb.ToString());
            return sb.ToString();
        }

        [MenuItem("Day117/自動テスト＋画面チェック（横・縦）")]
        public static void RunTest() { Check(); EditorPrefs.SetInt("d117_step", 0); StartStep(0); }
        static void StartStep(int i)
        {
            var sz = Sizes[i];
            PlayModeWindow.SetCustomRenderingResolution(sz.w, sz.h, "D117 " + sz.tag);
            EditorSceneManager.SaveOpenScenes(); if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            EditorPrefs.SetString("d117_test_tag", sz.tag);
            EditorApplication.isPlaying = true;
        }
        static void OnState(PlayModeStateChange s)
        {
            if (s != PlayModeStateChange.EnteredEditMode || !EditorPrefs.HasKey("d117_step")) return;
            if (EditorPrefs.HasKey("d117_test_tag")) { EditorPrefs.DeleteKey("d117_test_tag"); EditorPrefs.DeleteKey("d117_step"); return; } // 途中で止まった
            int next = EditorPrefs.GetInt("d117_step") + 1;
            if (next < Sizes.Length) { EditorPrefs.SetInt("d117_step", next); StartStep(next); }
            else { EditorPrefs.DeleteKey("d117_step"); PlayerPrefs.DeleteAll(); Debug.Log("D117: 自動テスト完了 → " + Shots); }
        }

        [MenuItem("Day117/横（PC）で遊ぶ")] static void PlayL() { PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "D117 landscape"); Open(); EditorApplication.isPlaying = true; }
        [MenuItem("Day117/スマホ横で遊ぶ")] static void PlayW() { PlayModeWindow.SetCustomRenderingResolution(2340, 1080, "D117 phone wide"); Open(); EditorApplication.isPlaying = true; }
        [MenuItem("Day117/スマホ縦で遊ぶ")] static void PlayP() { PlayModeWindow.SetCustomRenderingResolution(1080, 2340, "D117 portrait"); Open(); EditorApplication.isPlaying = true; }
        static void Open() { if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath); }

        /// index.html の手直し：①読み込み完了でロゴとバーを必ず消す ②iPhoneのマナーモードでも音が鳴るようにする
        static void PatchIndex(string path)
        {
            var html = File.ReadAllText(path);
            if (html.Contains("d117-patch")) return;
            html = html.Replace("(progress) => {", "(progress) => { if (progress >= 1) setTimeout(d117HideBar, 600);");
            // 0.5秒の無音WAV（8kHz・8bit）を作る
            int n = 4000; var wav = new MemoryStream(); var w = new BinaryWriter(wav);
            w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n); w.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(8000); w.Write(8000); w.Write((short)1); w.Write((short)8); w.Write(Encoding.ASCII.GetBytes("data")); w.Write(n);
            for (int i = 0; i < n; i++) w.Write((byte)128); w.Flush();
            var b64 = System.Convert.ToBase64String(wav.ToArray());
            var js = "<script id=\"d117-patch\">\n" +
                "function d117HideBar(){var b=document.querySelector('#unity-loading-bar');if(b)b.style.display='none';}\n" +
                "(function(){var done=false;function unlock(){if(done)return;done=true;\n" +
                " try{if(navigator.audioSession)navigator.audioSession.type='playback';}catch(e){}\n" +
                " try{var a=new Audio('data:audio/wav;base64," + b64 + "');a.loop=true;a.volume=0.01;a.setAttribute('playsinline','');a.play().catch(function(){done=false;});}catch(e){done=false;}\n" +
                " d117HideBar();}\n" +
                " ['touchend','click','keydown'].forEach(function(t){document.addEventListener(t,unlock,true);});})();\n</script>\n";
            html = html.Replace("</body>", js + "</body>");
            File.WriteAllText(path, html);
        }

        [MenuItem("Day117/WebGLビルド")]
        public static void WebBuild()
        {
            var outDir = Path.GetFullPath("../Web");
            // 取り込み設定を変えたら一度だけ全部取り込み直す（声の圧縮・画像の最大サイズ）
            if (EditorPrefs.GetInt("d117_import_v", 0) < 4)
            {
                AssetDatabase.ImportAsset("Assets/D117/Resources", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
                EditorPrefs.SetInt("d117_import_v", 4);
            }
            // GitHub Pages公開：圧縮なし（iPhoneでブラウザ側の解凍が重くて止まるため）。1ファイル100MBまでOK
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            // iPhoneが直接読めるASTCで画像を圧縮（DXTだと読み込み時に全部展開して重い）
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.WebGL.nameFilesAsHashes = true; // 公開のたびにファイル名が変わる → iPhoneに古いページが残っても混ざらない
            // iPhoneのメモリ対策（第4章で重くなり読み込み中に落ちた）：最初に十分なメモリを取り、上限は控えめに、コードはサイズ優先
            PlayerSettings.WebGL.initialMemorySize = 256;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Linear;
            PlayerSettings.WebGL.linearMemoryGrowthStep = 32;
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.WebGL, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(UnityEditor.Build.NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Master);
            PlayerSettings.WebGL.dataCaching = false; // iPhoneの保存領域を使わない（古いデータが残って止まるのを防ぐ）
            var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = outDir, target = BuildTarget.WebGL, options = BuildOptions.None });
            if (r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) PatchIndex(Path.Combine(outDir, "index.html"));
            File.WriteAllText(Path.Combine(Shots, "webbuild.txt"), r.summary.result + " " + r.summary.totalSize + " errors=" + r.summary.totalErrors);
            Debug.Log("D117: WebGL " + r.summary.result + " → " + outDir);
        }
    }
}
