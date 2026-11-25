using System; using System.Collections; using System.Collections.Generic; using System.Linq;
using UnityEngine; using UnityEngine.UI; using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; using UnityEngine.InputSystem.UI;
#endif
namespace Day117
{
    /// 廃村の蒼き秘宝：サウンドノベル本体。画面はすべてコードで組む（シーンにはこの部品1つだけ置く）
    public class Novel : MonoBehaviour
    {
        public static Novel I;
        public static bool FastTest; // 自動テスト用：文字送りを一瞬にする

        // ---- データ ----
        Chapter ch; readonly Dictionary<string, Node> map = new Dictionary<string, Node>();
        SaveData st = new SaveData();
        const string SaveKey = "d117_save";

        // ---- 画面 ----
        RectTransform party; readonly Dictionary<string, (Image frame, RawImage face)> partyCells = new Dictionary<string, (Image, RawImage)>(); string speaking;
        RawImage faceImg; RectTransform faceBox; float faceSize = 250; int cmdFont = 30;
        Font font; RectTransform root, sceneWin, dialog, side, cmdList, namePlate;
        Image bg, flashImg, lumiIcon, dialogBg; Text locLabel, chapLabel, nameText, bodyText, arrow, statusText, promptText;
        readonly Image[] slots = new Image[3]; readonly string[] slotId = new string[3];
        Button nextBtn; GameObject titlePanel, notePanel, logPanel, endPanel; Text noteText, logText, endTitle, endBody; Button contBtn;
        Button autoBtn, voiceBtn; AudioSource voiceSrc, seSrc; bool voiceOn = true, autoOn;
        readonly List<string> backlog = new List<string>();
        CanvasScaler scaler; bool landscape = true; Vector2 lastScreen;
        readonly Dictionary<string, AudioClip> se = new Dictionary<string, AudioClip>();

        // ---- 進行 ----
        Node cur; List<Line> playing; int idx; Action onLinesDone;
        bool typing, waitClick; Coroutine typeCo, autoCo; string fullText = "";
        public bool WaitingChoice { get; private set; }
        public bool AtEnd { get; private set; }
        public bool AtChapterEnd { get; private set; }
        public void ContinueNext() { if (AtChapterEnd) { AtChapterEnd = false; endPanel.SetActive(false); Go(cur.next); } }
        public bool AtTitle => titlePanel && titlePanel.activeSelf;
        public string NodeId => cur != null ? cur.id : "";
        public int CommandCount => cmdList ? cmdList.childCount : 0;
        public SaveData State => st;
        readonly List<Action> choiceActs = new List<Action>();

        static readonly Color Gold = new Color(.97f, .8f, .49f), Panel = new Color(.04f, .06f, .11f, .92f), Ink = new Color(.91f, .92f, .94f);

        void Awake()
        {
            I = this;
            font = Resources.Load<Font>("Fonts/MPLUS1p-Regular");
            ch = ScenarioLoader.LoadAll();
            foreach (var n in ch.nodes) map[n.id] = n;
            if (!FindFirstObjectByType<AudioListener>()) gameObject.AddComponent<AudioListener>(); // 音を聞く「耳」：これが無いと一切鳴らない
            BuildSounds(); BuildUI(); ShowTitle();
        }

        // =================== 画面づくり ===================
        RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        static void TopLeft(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        }
        static void Fill(RectTransform r, float pad = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(pad, pad); r.offsetMax = new Vector2(-pad, -pad);
        }
        Image Img(string name, Transform parent, Color c)
        {
            var r = Rect(name, parent); var i = r.gameObject.AddComponent<Image>(); i.color = c; return i;
        }
        Text Txt(string name, Transform parent, int size, Color c, TextAnchor a = TextAnchor.UpperLeft)
        {
            var r = Rect(name, parent); var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.color = c; t.alignment = a; t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.lineSpacing = 1.15f;
            return t;
        }
        Button Btn(Transform parent, string label, Action act, int size = 30, float h = 72)
        {
            var img = Img("Btn_" + label, parent, new Color(.09f, .14f, .22f, .95f));
            var ol = img.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.47f, .6f, .67f, .9f); ol.effectDistance = new Vector2(2, -2);
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors; cb.highlightedColor = new Color(1.25f, 1.2f, 1f); cb.pressedColor = new Color(.7f, .7f, .7f); b.colors = cb;
            b.onClick.AddListener(() => { Play("click"); act(); });
            var le = img.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = h; le.minHeight = h;
            var t = Txt("Label", img.transform, size, Ink, TextAnchor.MiddleLeft); Fill(t.rectTransform); t.rectTransform.offsetMin = new Vector2(22, 0);
            t.text = label; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.resizeTextForBestFit = true; t.resizeTextMinSize = 18; t.resizeTextMaxSize = size;
            return b;
        }

        void BuildUI()
        {
            if (!FindFirstObjectByType<EventSystem>())
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                es.AddComponent<InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = cgo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var back = Img("Back", cgo.transform, Color.black); Fill(back.rectTransform);
            root = Rect("Root", cgo.transform); root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);

            // 場面窓（4:3）
            sceneWin = Rect("SceneWindow", root); sceneWin.gameObject.AddComponent<RectMask2D>();
            var frame = Img("Frame", sceneWin, new Color(.03f, .05f, .08f)); Fill(frame.rectTransform);
            bg = Img("BG", sceneWin, Color.white); Fill(bg.rectTransform); bg.preserveAspect = false;
            dunView = Rect("DungeonView", sceneWin).gameObject.AddComponent<RawImage>(); Fill(dunView.rectTransform); dunView.raycastTarget = false; dunView.enabled = false;
            var stage = Rect("Stage", sceneWin); Fill(stage);
            for (int i = 0; i < 3; i++)
            {
                var s = Img("Slot" + i, stage, Color.white); s.preserveAspect = true; s.raycastTarget = false;
                var r = s.rectTransform; r.anchorMin = r.anchorMax = new Vector2(new[] { .22f, .5f, .78f }[i], 0); r.pivot = new Vector2(.5f, 0);
                s.enabled = false; slots[i] = s;
            }
            cgImg = Img("CG", sceneWin, Color.white); cgImg.preserveAspect = true; cgImg.raycastTarget = false; var cgr = cgImg.rectTransform;
            cgr.anchorMin = new Vector2(.15f, .3f); cgr.anchorMax = new Vector2(.85f, .97f); cgr.offsetMin = cgr.offsetMax = Vector2.zero; cgImg.enabled = false;
            BuildAction();
            var scan = Rect("Scanlines", sceneWin); Fill(scan);
            var ri = scan.gameObject.AddComponent<RawImage>(); ri.texture = ScanTex(); ri.raycastTarget = false; ri.uvRect = new Rect(0, 0, 1, 240);
            var vig = Img("Vignette", sceneWin, new Color(0, 0, 0, 0)); Fill(vig.rectTransform); vig.raycastTarget = false;
            flashImg = Img("Flash", sceneWin, new Color(1, 1, 1, 0)); Fill(flashImg.rectTransform); flashImg.raycastTarget = false;
            var hud = Img("LocBox", sceneWin, new Color(.03f, .07f, .11f, .75f)); var hr = hud.rectTransform;
            hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0, 1); hr.anchoredPosition = new Vector2(16, -14); hr.sizeDelta = new Vector2(460, 54);
            locLabel = Txt("Loc", hud.transform, 28, new Color(.73f, .9f, .89f), TextAnchor.MiddleLeft); Fill(locLabel.rectTransform, 6); locLabel.rectTransform.offsetMin = new Vector2(14, 0);
            chapLabel = Txt("Chap", sceneWin, 26, Gold, TextAnchor.MiddleRight); var cr = chapLabel.rectTransform;
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(1, 1); cr.anchoredPosition = new Vector2(-18, -14); cr.sizeDelta = new Vector2(420, 54);
            var adv = sceneWin.gameObject.AddComponent<Button>(); adv.transition = Selectable.Transition.None; adv.onClick.AddListener(Advance);
            frame.raycastTarget = true; adv.targetGraphic = frame;

            // 会話窓
            dialog = Rect("Dialog", root);
            dialogBg = Img("Bg", dialog, Panel); Fill(dialogBg.rectTransform);
            var dol = dialogBg.gameObject.AddComponent<Outline>(); dol.effectColor = new Color(.8f, .9f, .93f, .9f); dol.effectDistance = new Vector2(3, -3);
            var dadv = dialogBg.gameObject.AddComponent<Button>(); dadv.transition = Selectable.Transition.None; dadv.onClick.AddListener(Advance);
            var np = Img("NamePlate", dialog, Gold); namePlate = np.rectTransform;
            namePlate.anchorMin = namePlate.anchorMax = namePlate.pivot = new Vector2(0, 1); namePlate.anchoredPosition = new Vector2(24, 22); namePlate.sizeDelta = new Vector2(250, 52);
            nameText = Txt("Name", np.transform, 30, new Color(.09f, .09f, .13f), TextAnchor.MiddleCenter); Fill(nameText.rectTransform); nameText.fontStyle = FontStyle.Bold;
            lumiIcon = Img("Lumi", dialog, Color.white); var lr = lumiIcon.rectTransform; lumiIcon.preserveAspect = true; lumiIcon.raycastTarget = false;
            lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0, 1); lr.anchoredPosition = new Vector2(-34, 70); lr.sizeDelta = new Vector2(120, 120);
            lumiIcon.sprite = Resources.Load<Sprite>("Chars/lumi"); lumiIcon.enabled = false;
            // 話している仲間の顔（会話窓の左）
            var fb = Img("FaceBox", dialog, Gold); faceBox = fb.rectTransform; fb.raycastTarget = false;
            faceBox.anchorMin = faceBox.anchorMax = faceBox.pivot = new Vector2(0, .5f); faceBox.anchoredPosition = new Vector2(22, -6);
            var fin = Img("FaceBg", fb.transform, new Color(.12f, .16f, .22f)); Fill(fin.rectTransform, 4); fin.raycastTarget = false;
            faceImg = Rect("Face", fb.transform).gameObject.AddComponent<RawImage>(); Fill(faceImg.rectTransform, 4); faceImg.raycastTarget = false;
            faceBox.gameObject.SetActive(false);
            bodyText = Txt("Body", dialog, 36, Ink); Fill(bodyText.rectTransform); bodyText.rectTransform.offsetMin = new Vector2(34, 20); bodyText.rectTransform.offsetMax = new Vector2(-40, -40);
            bodyText.raycastTarget = false;
            arrow = Txt("Arrow", dialog, 30, Gold, TextAnchor.LowerRight); var ar = arrow.rectTransform; ar.anchorMin = ar.anchorMax = ar.pivot = new Vector2(1, 0);
            ar.anchoredPosition = new Vector2(-18, 10); ar.sizeDelta = new Vector2(60, 50); arrow.text = "▼"; arrow.raycastTarget = false;

            // 右（縦画面では下）のコマンド欄
            side = Rect("Side", root);
            var sbg = Img("Bg", side, new Color(.06f, .1f, .16f, .9f)); Fill(sbg.rectTransform);
            var sol = sbg.gameObject.AddComponent<Outline>(); sol.effectColor = new Color(.35f, .48f, .56f); sol.effectDistance = new Vector2(2, -2);
            var head = Txt("Head", side, 24, new Color(.48f, .76f, .83f), TextAnchor.UpperLeft); TopLeftIn(head.rectTransform, 22, 14, 60);
            head.text = "COMMAND ／ コマンド";
            statusText = Txt("Status", side, 26, Ink, TextAnchor.UpperLeft); TopLeftIn(statusText.rectTransform, 22, 50, 84); statusText.verticalOverflow = VerticalWrapMode.Overflow; statusText.lineSpacing = 1f;
            mapView = Rect("Map", side).gameObject.AddComponent<RawImage>(); mapView.raycastTarget = false; mapView.enabled = false;
            promptText = Txt("Prompt", side, 28, Gold, TextAnchor.UpperLeft); TopLeftIn(promptText.rectTransform, 22, 138, 50);
            cmdList = Rect("Commands", side);
            var cg = cmdList.gameObject.AddComponent<GridLayoutGroup>(); cg.spacing = new Vector2(10, 10); cg.constraint = GridLayoutGroup.Constraint.FixedColumnCount; cg.constraintCount = 1; cg.cellSize = new Vector2(486, 64);
            // 仲間の顔ぶれ（会話中、コマンド欄が空いているときに表示。話している人が明るくなる）
            party = Rect("Party", side); var pg = party.gameObject.AddComponent<GridLayoutGroup>(); pg.constraint = GridLayoutGroup.Constraint.FixedColumnCount; pg.constraintCount = 4; pg.spacing = new Vector2(10, 10);
            foreach (var id in Cast.Ids)
            {
                var cell = Img("P_" + id, party, new Color(.47f, .6f, .67f)); cell.raycastTarget = false;
                var inner = Rect("Face", cell.transform).gameObject.AddComponent<RawImage>(); Fill(inner.rectTransform, 3); inner.raycastTarget = false;
                var sp = CharSprite(id, "normal"); if (sp) { inner.texture = sp.texture; inner.uvRect = FaceUV(id); }
                var nm = Txt("N", cell.transform, 22, Color.white, TextAnchor.LowerCenter); Fill(nm.rectTransform, 4); nm.text = Cast.Name(id); nm.raycastTarget = false;
                var o = nm.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, .9f); o.effectDistance = new Vector2(2, -2);
                partyCells[id] = (cell, inner);
            }
            var tools = Rect("Tools", side);
            var gl = tools.gameObject.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(150, 62); gl.spacing = new Vector2(10, 10); gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 3;
            Btn(tools, "手帳", OpenNote, 24, 62); Btn(tools, "履歴", OpenLog, 24, 62);
            autoBtn = Btn(tools, "オート", ToggleAuto, 24, 62); voiceBtn = Btn(tools, "声 ON", ToggleVoice, 24, 62);
            Btn(tools, "タイトル", ShowTitle, 24, 62); Btn(tools, "記録", () => { Save(); Toast("記録しました"); }, 24, 62);
            toolsRT = tools;

            voiceSrc = gameObject.AddComponent<AudioSource>(); seSrc = gameObject.AddComponent<AudioSource>(); seSrc.volume = .5f;

            // 重ねる画面
            titlePanel = BuildTitle(cgo.transform);
            notePanel = Overlay(cgo.transform, "探検手帳", out noteText);
            logPanel = Overlay(cgo.transform, "これまでの会話", out logText);
            endPanel = BuildEnd(cgo.transform);
            toast = Txt("Toast", cgo.transform, 30, Gold, TextAnchor.MiddleCenter); var tr = toast.rectTransform;
            tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(.5f, 1); tr.anchoredPosition = new Vector2(0, -2); tr.sizeDelta = new Vector2(900, 44); toast.text = "";
            var tsh = toast.gameObject.AddComponent<Outline>(); tsh.effectColor = new Color(0, 0, 0, .9f); tsh.effectDistance = new Vector2(2, -2);
            ApplyLayout(true);
        }
        RectTransform toolsRT; Text toast; Image cgImg; RawImage dunView, mapView;
        // ---- 3Dダンジョン ----
        Dungeon dun; string dunBuilt; public bool InDungeon { get; private set; }
        readonly Dictionary<char, string> dunEvents = new Dictionary<char, string>();
        void EnterDungeon(Node n)
        {
            if (!dun) dun = new GameObject("Dungeon").AddComponent<Dungeon>();
            if (dunBuilt != n.id)
            {
                dun.Build(n.map); dunBuilt = n.id; dunEvents.Clear();
                foreach (var e in (n.events ?? "").Split(',')) { var kv = e.Split(':'); if (kv.Length == 2 && kv[0].Length == 1) dunEvents[kv[0][0]] = kv[1]; }
            }
            dun.SetActive(true);
            if (st.dx >= 0 && dun.Walkable(st.dx, st.dy)) dun.Place(st.dx, st.dy, st.ddir); else dun.Place(dun.start.x, dun.start.y, 1);
            dun.onEnter = OnDungeonCell;
            SetStage(null); cgImg.enabled = false; bg.sprite = null; bg.color = Color.black;
            dunView.texture = dun.rt; dunView.enabled = true; mapView.texture = dun.mapTex; mapView.enabled = true;
            ClearCommands(); InDungeon = true; namePlate.gameObject.SetActive(false); arrow.enabled = false;
            promptText.text = "地下迷宮を進め";
            bodyText.text = "十字ボタン／矢印キー（WASD）で進もう。\n<color=#9fb3c4>ルミの光が強いほど、龍神の珠に近い。</color>";
            AddCmd("↑ 前へ進む", () => dun.Step(1), keep: true);
            AddCmd("← 左を向く", () => dun.Turn(-1), keep: true);
            AddCmd("→ 右を向く", () => dun.Turn(1), keep: true);
            AddCmd("↓ 後ろへ下がる", () => dun.Step(-1), keep: true);
            WaitingChoice = true; DungeonStatus();
        }
        void OnDungeonCell(char c, int cx, int cy)
        {
            st.dx = cx; st.dy = cy; st.ddir = dun.dir; Play("step"); DungeonStatus(); Save();
            string key = "ev:" + cx + "," + cy;
            if (dunEvents.TryGetValue(c, out var to) && !Has(key)) { AddFlag(key); LeaveDungeonView(); Go(to); }
        }
        void DungeonStatus() { st.lumi = dun.Closeness; UpdateStatus(); }
        void LeaveDungeonView()
        {
            InDungeon = false; if (dunView) dunView.enabled = false; if (mapView) mapView.enabled = false;
            if (dun) { dun.SetActive(false); st.ddir = dun.dir; }
        }
        public void DungeonAuto() { if (InDungeon && dun) dun.AutoStep(); }
        // ---- 罠突破パート（3×3） ----
        GameObject actRoot; readonly Image[] actCells = new Image[9]; readonly Image[] actSkel = new Image[9]; Text actTitle, actCount;
        Text rotHint; RectTransform actGrid; int actHit = -1; public bool InAction { get; private set; }
        void BuildAction()
        {
            var r = Rect("Action", sceneWin); Fill(r); actRoot = r.gameObject;
            var dim = Img("Dim", r, new Color(0, 0, 0, .35f)); Fill(dim.rectTransform); dim.raycastTarget = false;
            actTitle = Txt("Title", r, 34, Gold, TextAnchor.MiddleCenter); TopLeftIn(actTitle.rectTransform, 0, 70, 50); actTitle.raycastTarget = false;
            actCount = Txt("Count", r, 30, Ink, TextAnchor.MiddleCenter); TopLeftIn(actCount.rectTransform, 0, 120, 44); actCount.raycastTarget = false;
            var grid = Rect("Grid", r); actGrid = grid; grid.anchorMin = new Vector2(.14f, .04f); grid.anchorMax = new Vector2(.86f, .78f); grid.offsetMin = grid.offsetMax = Vector2.zero;
            var skel = Resources.Load<Sprite>("Chars/skeleton_doll");
            for (int i = 0; i < 9; i++)
            {
                int k = i; var c = Img("Cell" + i, grid, new Color(.1f, .14f, .2f, .45f)); var cr = c.rectTransform;
                cr.anchorMin = new Vector2((i % 3) / 3f, 1 - (i / 3 + 1) / 3f); cr.anchorMax = new Vector2((i % 3 + 1) / 3f, 1 - (i / 3) / 3f);
                cr.offsetMin = new Vector2(6, 6); cr.offsetMax = new Vector2(-6, -6);
                var ol = c.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.5f, .7f, .8f, .6f);
                var b = c.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(() => actHit = k);
                var s = Img("Skel", c.transform, Color.white); Fill(s.rectTransform, 4); s.sprite = skel; s.preserveAspect = true; s.raycastTarget = false; s.enabled = false;
                actCells[i] = c; actSkel[i] = s;
            }
            actRoot.SetActive(false);
        }
        IEnumerator ActionPart(Node n)
        {
            InAction = true; actRoot.SetActive(true); namePlate.gameObject.SetActive(false); SetStage(null); cgImg.enabled = false;
            bodyText.text = "光ったマスをタップ（キーボードはテンキー配置の 1〜9）"; arrow.enabled = false;
            actTitle.text = n.title; int need = Mathf.Max(1, n.hits), hits = 0; float win = n.window > 0 ? n.window : 1.6f;
            while (hits < need)
            {
                actCount.text = "あと " + (need - hits) + " 体";
                yield return new WaitForSeconds(.35f);
                int cell = UnityEngine.Random.Range(0, 9); actHit = -1;
                actSkel[cell].sprite = Resources.Load<Sprite>("Chars/" + (string.IsNullOrEmpty(n.sprite) ? "skeleton_doll" : n.sprite)); actSkel[cell].enabled = true; actCells[cell].color = new Color(.35f, .55f, .7f, .6f); Play("drop");
                float t = 0; bool ok = false;
                while (t < win) { if (FastTest && t > .1f) actHit = cell; if (actHit == cell) { ok = true; break; } t += Time.deltaTime; yield return null; }
                actSkel[cell].enabled = false; actCells[cell].color = new Color(.1f, .14f, .2f, .45f);
                if (ok) { hits++; Play("click"); }
                else
                {
                    st.life = Mathf.Max(0, st.life - 1); Play("scare"); StartCoroutine(Shake()); StartCoroutine(Flash()); UpdateStatus();
                    if (st.life <= 0)
                    {
                        actCount.text = "LIFEが尽きた……！　ルミの光で回復して、もう一度！";
                        yield return new WaitForSeconds(1.6f); st.life = 3; hits = 0; UpdateStatus();
                    }
                }
                win = Mathf.Max(.9f, win - .04f);
            }
            actCount.text = "突破！"; yield return new WaitForSeconds(.8f);
            actRoot.SetActive(false); InAction = false; Go(n.next);
        }
        void TopLeftIn(RectTransform r, float x, float y, float h)
        {
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(-x * 2, h);
        }
        GameObject Overlay(Transform parent, string title, out Text body)
        {
            var p = Img(title, parent, new Color(0, 0, 0, .88f)); Fill(p.rectTransform);
            var box = Img("Box", p.transform, Panel); Fill(box.rectTransform, 60);
            var t = Txt("Title", box.transform, 40, Gold, TextAnchor.UpperLeft); TopLeftIn(t.rectTransform, 40, 30, 60); t.text = title;
            body = Txt("Body", box.transform, 30, Ink, TextAnchor.UpperLeft); Fill(body.rectTransform); body.rectTransform.offsetMin = new Vector2(40, 120); body.rectTransform.offsetMax = new Vector2(-40, -110);
            var go = p.gameObject;
            var bt = Btn(box.transform, "とじる", () => go.SetActive(false), 30, 70); var br = (RectTransform)bt.transform;
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(.5f, 0); br.anchoredPosition = new Vector2(0, 30); br.sizeDelta = new Vector2(320, 70);
            go.SetActive(false); return go;
        }
        GameObject creditPanel;
        // VOICEVOX の利用規約に従い、使ったキャラクターを「VOICEVOX:キャラ名」で表記する
        static readonly string[] VoiceChars = { "青山龍星", "白上虎太郎", "ちび式じい", "雀松朱司", "春日部つむぎ", "九州そら", "冥鳴ひまり", "四国めたん", "麒ヶ島宗麟", "ナースロボ＿タイプＴ" };
        static string CreditShort => "音声：" + string.Join(" ／ ", Array.ConvertAll(VoiceChars, c => "VOICEVOX:" + c));
        static string CreditFull =>
            "【音声】\n" + string.Join("\n", Array.ConvertAll(VoiceChars, c => "VOICEVOX:" + c)) +
            "\n\n【フォント】M PLUS 1p（SIL Open Font License）" +
            "\n【画像】ChatGPT 画像生成で作成" +
            "\n【物語・プログラム】オリジナル（TitanUnity100 Day117）";
        GameObject BuildTitle(Transform parent)
        {
            var p = Img("Title", parent, Color.black); Fill(p.rectTransform);
            var tbg = Img("TitleBG", p.transform, Color.white); Fill(tbg.rectTransform); tbg.sprite = Resources.Load<Sprite>("BG/title");
            if (!tbg.sprite) tbg.color = new Color(.05f, .12f, .18f); else tbg.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (tbg.sprite) tbg.GetComponent<AspectRatioFitter>().aspectRatio = tbg.sprite.rect.width / tbg.sprite.rect.height;
            var shade = Img("Shade", p.transform, new Color(0, 0, 0, .45f)); Fill(shade.rectTransform);
            var col = Rect("Col", p.transform); col.anchorMin = col.anchorMax = col.pivot = new Vector2(.5f, .5f); col.sizeDelta = new Vector2(900, 900);
            var t1 = Txt("Eyebrow", col, 26, new Color(.48f, .8f, .86f), TextAnchor.MiddleCenter); TopLeftIn(t1.rectTransform, 0, 40, 40); t1.text = "SOUND NOVEL ・ 8人のお宝ハンター";
            var t2 = Txt("Name", col, 92, new Color(.97f, .91f, .77f), TextAnchor.MiddleCenter); TopLeftIn(t2.rectTransform, 0, 90, 130); t2.text = "廃村の蒼き秘宝"; t2.fontStyle = FontStyle.Bold;
            var sh = t2.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, .1f, .2f, .9f); sh.effectDistance = new Vector2(4, -4);
            var t3 = Txt("Sub", col, 30, Ink, TextAnchor.MiddleCenter); TopLeftIn(t3.rectTransform, 0, 225, 50); t3.text = "― 隠れ湯村に眠る、世界最大の宝石『龍神の珠』―";
            var menu = Rect("Menu", col); menu.anchorMin = menu.anchorMax = menu.pivot = new Vector2(.5f, 1); menu.anchoredPosition = new Vector2(0, -450); menu.sizeDelta = new Vector2(520, 260);
            var vl = menu.gameObject.AddComponent<VerticalLayoutGroup>(); vl.spacing = 18; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false;
            Btn(menu, "はじめから", NewGame, 36, 84);
            contBtn = Btn(menu, "つづきから", Continue, 36, 84);
            Btn(menu, "クレジット", () => { creditPanel.SetActive(true); creditPanel.transform.SetAsLastSibling(); }, 30, 72);
            menu.sizeDelta = new Vector2(520, 340);
            // 暫定版の表示
            var ver = Txt("Version", col, 26, Gold, TextAnchor.MiddleCenter); TopLeftIn(ver.rectTransform, 0, 285, 44);
            ver.text = "― 暫定版　第1章〜第4章 ―";
            rotHint = Txt("RotHint", p.transform, 30, Gold, TextAnchor.MiddleCenter); var rh = rotHint.rectTransform;
            rh.anchorMin = new Vector2(0, 0); rh.anchorMax = new Vector2(1, 0); rh.pivot = new Vector2(.5f, 0); rh.anchoredPosition = new Vector2(0, 90); rh.sizeDelta = new Vector2(0, 50);
            rotHint.text = "スマホは横向きで遊んでね"; rotHint.raycastTarget = false;
            // VOICEVOXの規約：使ったキャラを「VOICEVOX:キャラ名」でタイトルのすぐ下に表記
            var vc = Txt("VoiceCredit", col, 19, new Color(.72f, .8f, .86f), TextAnchor.UpperCenter); TopLeftIn(vc.rectTransform, -60, 334, 100);
            vc.horizontalOverflow = HorizontalWrapMode.Wrap; vc.verticalOverflow = VerticalWrapMode.Overflow; vc.text = CreditShort;
            var cr = Txt("Credit", p.transform, 20, new Color(.6f, .68f, .75f), TextAnchor.LowerCenter); var crr = cr.rectTransform;
            crr.anchorMin = new Vector2(0, 0); crr.anchorMax = new Vector2(1, 0); crr.pivot = new Vector2(.5f, 0); crr.anchoredPosition = new Vector2(0, 20); crr.sizeDelta = new Vector2(0, 40);
            cr.text = "物語・UI オリジナル ／ TitanUnity100 Day117 ／ 暫定版";
            creditPanel = Overlay(p.transform, "クレジット", out var cb); cb.fontSize = 28; cb.text = CreditFull;
            return p.gameObject;
        }
        GameObject BuildEnd(Transform parent)
        {
            var p = Img("End", parent, new Color(0, 0, 0, .92f)); Fill(p.rectTransform);
            var col = Rect("Col", p.transform); col.anchorMin = col.anchorMax = col.pivot = new Vector2(.5f, .5f); col.sizeDelta = new Vector2(980, 760);
            endTitle = Txt("T", col, 56, Gold, TextAnchor.MiddleCenter); TopLeftIn(endTitle.rectTransform, 0, 40, 90);
            endBody = Txt("B", col, 32, Ink, TextAnchor.UpperCenter); TopLeftIn(endBody.rectTransform, 0, 160, 360);
            var menu = Rect("Menu", col); menu.anchorMin = menu.anchorMax = menu.pivot = new Vector2(.5f, 1); menu.anchoredPosition = new Vector2(0, -540); menu.sizeDelta = new Vector2(520, 180);
            var vl = menu.gameObject.AddComponent<VerticalLayoutGroup>(); vl.spacing = 16; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false;
            nextBtn = Btn(menu, "次の章へ進む", () => { endPanel.SetActive(false); Go(cur.next); }, 32, 78);
            Btn(menu, "はじめから", NewGame, 32, 78); Btn(menu, "タイトルへ", ShowTitle, 32, 78);
            p.gameObject.SetActive(false); return p.gameObject;
        }

        void ApplyLayout(bool force = false)
        {
            var scr = new Vector2(Screen.width, Screen.height);
            if (!force && scr == lastScreen) return; lastScreen = scr;
            float aspect = scr.x / Mathf.Max(1, scr.y);
            landscape = aspect >= 1.1f;
            if (landscape)
            {
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = aspect >= 16f / 9f ? 1 : 0;
                // スマホ横（横長）は横幅いっぱい使い、右の欄を広げて押しやすくする
                float W = Mathf.Clamp(1080f * aspect, 1920, 2400); if (aspect < 16f / 9f) W = 1920;
                root.sizeDelta = new Vector2(W, 1080);
                TopLeft(sceneWin, 40, 40, 1280, 960);
                TopLeft(dialog, 40, 40 + 960 - 300, 1280, 300); SizeFace(300);
                float sw = W - 1390; bool wide = sw >= 760;
                TopLeft(side, 1350, 40, sw, 1000);
                float toolH = wide ? 84 : 62, toolsTop = 1000 - 20 - 2 * toolH - 10;
                TopLeftIn(cmdList, 22, wide ? 200 : 190, toolsTop - (wide ? 210 : 200));
                var g1 = cmdList.GetComponent<GridLayoutGroup>();
                if (wide) { g1.constraintCount = 2; g1.cellSize = new Vector2((sw - 44 - 12) / 2, 84); g1.spacing = new Vector2(12, 12); cmdFont = 34; }
                else { g1.constraintCount = 1; g1.cellSize = new Vector2(486, 64); g1.spacing = new Vector2(10, 10); cmdFont = 30; }
                PlaceMap(new Vector2(sw / 2 - 130, wide ? -440 : -560), 260);
                PlaceTools(3, (sw - 44 - 20) / 3, toolH, wide ? 30 : 24);
                statusText.fontSize = wide ? 32 : 26; promptText.fontSize = wide ? 32 : 28;
                TopLeftIn(statusText.rectTransform, 22, 50, wide ? 96 : 84); TopLeftIn(promptText.rectTransform, 22, wide ? 150 : 138, 50);
                bodyText.fontSize = 36;
            }
            else
            {
                scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = aspect <= 9f / 16f ? 0 : 1;
                float h = 1920; if (aspect < 9f / 16f) h = 1080f / aspect; // 縦長の端末は高さを伸ばす
                root.sizeDelta = new Vector2(1080, h);
                float top = Mathf.Max(30, (h - 1920) * .35f + 30);
                TopLeft(sceneWin, 0, top, 1080, 810);
                TopLeft(dialog, 20, top + 830, 1040, 380); SizeFace(330); cmdFont = 32;
                statusText.fontSize = 26; promptText.fontSize = 28; TopLeftIn(statusText.rectTransform, 22, 50, 84); TopLeftIn(promptText.rectTransform, 22, 138, 50);
                TopLeft(side, 20, top + 1230, 1040, h - top - 1250);
                TopLeftIn(cmdList, 22, 190, Mathf.Max(200, h - top - 1250 - 190 - 170));
                var g2 = cmdList.GetComponent<GridLayoutGroup>(); g2.constraintCount = 2; g2.cellSize = new Vector2(493, 66);
                PlaceMap(new Vector2(1040 - 22 - 200, -14), 200);
                PlaceTools(6, 155);
                bodyText.fontSize = 40;
            }
            InputGrid();
            LayoutParty(); if (rotHint) rotHint.gameObject.SetActive(!landscape);
            // 罠パートの3×3：横画面は会話窓（場面の下3割）に隠れないよう、上側に収める
            if (landscape) { actGrid.anchorMin = new Vector2(.2f, .335f); actGrid.anchorMax = new Vector2(.8f, .84f); }
            else { actGrid.anchorMin = new Vector2(.14f, .04f); actGrid.anchorMax = new Vector2(.86f, .78f); }
            actGrid.offsetMin = actGrid.offsetMax = Vector2.zero;
            float sh = sceneWin.sizeDelta.y;
            for (int i = 0; i < 3; i++) slots[i].rectTransform.sizeDelta = new Vector2(sh * .92f, sh * .92f);
        }
        void PlaceMap(Vector2 pos, float size)
        {
            var r = mapView.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = pos; r.sizeDelta = new Vector2(size, size);
        }
        void PlaceTools(int cols, float cellW, float cellH = 62, int fontSize = 24)
        {
            var gl = toolsRT.GetComponent<GridLayoutGroup>(); gl.constraintCount = cols; gl.cellSize = new Vector2(cellW, cellH);
            foreach (var t in toolsRT.GetComponentsInChildren<Text>(true)) t.fontSize = fontSize;
            int rows = Mathf.CeilToInt(6f / cols);
            toolsRT.anchorMin = new Vector2(0, 0); toolsRT.anchorMax = new Vector2(1, 0); toolsRT.pivot = new Vector2(0, 0);
            toolsRT.anchoredPosition = new Vector2(22, 20); toolsRT.sizeDelta = new Vector2(-44, rows * (cellH + 10));
        }

        // =================== 進行 ===================
        public void NewGame()
        {
            StopAllCoroutines(); InAction = false; actRoot.SetActive(false);
            st = new SaveData(); backlog.Clear(); endPanel.SetActive(false); titlePanel.SetActive(false); Go(ch.start);
        }
        void Continue()
        {
            var j = PlayerPrefs.GetString(SaveKey, ""); if (j == "") { NewGame(); return; }
            try { st = JsonUtility.FromJson<SaveData>(j); } catch { NewGame(); return; }
            if (st == null || !map.ContainsKey(st.node ?? "")) { NewGame(); return; }
            titlePanel.SetActive(false); endPanel.SetActive(false); Go(st.node);
        }
        public void ShowTitle()
        {
            StopAuto(); voiceSrc.Stop(); titlePanel.SetActive(true); endPanel.SetActive(false);
            contBtn.gameObject.SetActive(PlayerPrefs.HasKey(SaveKey));
        }
        void Save() { try { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(st)); PlayerPrefs.Save(); } catch { } }

        public void Go(string id)
        {
            if (!map.TryGetValue(id, out var n)) { Debug.LogError("D117: ノードがありません " + id); return; }
            if (cur != null && n.chapter > cur.chapter) st.life = 3; // 新しい章はLIFE満タンから
            if (InDungeon && n.type != "dungeon") LeaveDungeonView();
            if (n.type == "end" && dun) { dun.Clear(); dunBuilt = null; st.dx = -1; }
            cur = n; st.node = id; ClearCommands(); AtEnd = false; AtChapterEnd = false; InInput = false;
            SetBG(n.bg);
            locLabel.transform.parent.gameObject.SetActive(n.bg != "black");
            if (!string.IsNullOrEmpty(n.place)) locLabel.text = n.place;
            chapLabel.text = "CHAPTER " + Mathf.Max(1, n.chapter).ToString("00");
            Save();
            if (n.type == "end") { ShowEnd(); return; }
            if (n.type == "action") { StartCoroutine(ActionPart(n)); return; }
            if (n.type == "input") { ShowInput(n); return; }
            if (n.type == "dungeon") { EnterDungeon(n); return; }
            bool visited = n.type == "explore" && Has("v:" + id);
            if (visited)
            {
                int miss = GetMiss(id);
                if (miss >= 3 && !string.IsNullOrEmpty(n.hint)) { SetMiss(id, 0); PlayLines(new List<Line> { new Line { who = "lumi", text = n.hint, lumiGlow = false } }, AfterLines); }
                else AfterLines();
            }
            else PlayLines(n.lines, AfterLines);
        }
        void AfterLines()
        {
            var n = cur;
            switch (n.type)
            {
                case "explore": AddFlag("v:" + n.id); ShowCommands(); break;
                case "choice": ShowChoices(); break;
                default: if (!string.IsNullOrEmpty(n.next)) Go(n.next); break;
            }
        }
        void PlayLines(List<Line> lines, Action done)
        {
            playing = lines; idx = 0; onLinesDone = done;
            if (lines.Count == 0) { done(); return; }
            ShowLine(lines[0]);
        }
        void ShowLine(Line l)
        {
            waitClick = false;
            if (l.hasShow) SetStage(l.show);
            cgImg.sprite = string.IsNullOrEmpty(l.cg) ? null : Resources.Load<Sprite>("Chars/" + l.cg); cgImg.enabled = cgImg.sprite;
            if (Cast.IsChar(l.who)) PutSpeaker(l.who, l.expr);
            Highlight(l.who); ShowFace(l);
            // 一枚絵を出している間は立ち絵を隠す（絵が顔にかぶらないように）
            for (int i = 0; i < 3; i++) slots[i].gameObject.SetActive(!cgImg.enabled);
            bool narr = string.IsNullOrEmpty(l.who) || l.who == "narr";
            namePlate.gameObject.SetActive(!narr); nameText.text = Cast.Name(l.who);
            lumiIcon.enabled = l.who == "lumi" && lumiIcon.sprite;
            // 効果
            if (!string.IsNullOrEmpty(l.clue) && !st.clues.Contains(l.clue)) { st.clues.Add(l.clue); Toast("手掛かり：" + l.clue); }
            if (!string.IsNullOrEmpty(l.flag)) AddFlag(l.flag);
            if (!string.IsNullOrEmpty(l.trust)) AddTrust(l.trust, 1);
            if (l.lumiGlow) st.lumi = Mathf.Min(1, st.lumi + .34f);
            if (l.life != 0) { st.life = Mathf.Clamp(st.life + l.life, 0, 3); }
            if (l.flash) StartCoroutine(Flash());
            if (l.shake) StartCoroutine(Shake());
            if (!string.IsNullOrEmpty(l.se)) Play(l.se);
            voiceSrc.Stop();
            if (voiceOn && !string.IsNullOrEmpty(l.voice)) { var c = Resources.Load<AudioClip>("Voice/" + l.voice); if (c) { voiceSrc.clip = c; voiceSrc.Play(); } }
            UpdateStatus();
            backlog.Add((narr ? "" : "<color=#f8cb7c>" + Cast.Name(l.who) + "</color>　") + l.text); if (backlog.Count > 80) backlog.RemoveAt(0);
            fullText = l.text; if (typeCo != null) StopCoroutine(typeCo); typeCo = StartCoroutine(TypeText());
        }
        IEnumerator TypeText()
        {
            typing = true; arrow.enabled = false; bodyText.text = "";
            if (!FastTest)
                for (int i = 1; i <= fullText.Length; i++) { bodyText.text = fullText.Substring(0, i); yield return new WaitForSeconds(.028f); }
            bodyText.text = fullText; typing = false; waitClick = true; arrow.enabled = true;
            if (autoOn) { if (autoCo != null) StopCoroutine(autoCo); autoCo = StartCoroutine(AutoNext()); }
        }
        IEnumerator AutoNext()
        {
            float wait = Mathf.Max(1.2f, fullText.Length * .07f);
            if (voiceSrc.isPlaying && voiceSrc.clip) wait = Mathf.Max(wait, voiceSrc.clip.length - voiceSrc.time + .6f);
            yield return new WaitForSeconds(wait); if (waitClick) Advance();
        }
        public void Advance()
        {
            if (InAction) return;
            if (AtTitle || endPanel.activeSelf || notePanel.activeSelf || logPanel.activeSelf) return;
            if (typing) { if (typeCo != null) StopCoroutine(typeCo); bodyText.text = fullText; typing = false; waitClick = true; arrow.enabled = true; return; }
            if (!waitClick) return;
            waitClick = false; idx++;
            if (playing != null && idx < playing.Count) ShowLine(playing[idx]);
            else { arrow.enabled = false; var d = onLinesDone; onLinesDone = null; d?.Invoke(); }
        }

        // ---- コマンド・選択肢 ----
        void ClearCommands()
        {
            WaitingChoice = false; choiceActs.Clear(); promptText.text = "";
            for (int i = cmdList.childCount - 1; i >= 0; i--) Destroy(cmdList.GetChild(i).gameObject);
            cmdList.DetachChildren();
        }
        void AddCmd(string label, Action act, bool keep = false)
        {
            int n = choiceActs.Count + 1;
            Action wrapped = keep ? act : () => { if (!WaitingChoice) return; ClearCommands(); act(); };
            choiceActs.Add(wrapped);
            Btn(cmdList, keep ? label : n + ". " + label, wrapped, cmdFont, 64);
        }
        void ShowCommands()
        {
            ClearCommands(); promptText.text = "どうする？";
            foreach (var c in cur.choices)
            {
                if (!string.IsNullOrEmpty(c.once) && Has(c.once)) continue;
                if (!string.IsNullOrEmpty(c.need) && !Has(c.need)) continue;
                var cc = c; AddCmd(c.text, () => Choose(cc));
            }
            if (cur.require.Count > 0 && cur.require.All(Has)) AddCmd("★ " + cur.exitText, () => Go(cur.exitTo));
            WaitingChoice = true;
            bodyText.text = "<color=#9fb3c4>（コマンドを選んでください）</color>"; arrow.enabled = false;
        }
        void ShowChoices()
        {
            ClearCommands(); promptText.text = cur.prompt ?? "どうする？";
            foreach (var c in cur.choices)
            {
                if (!string.IsNullOrEmpty(c.need) && !Has(c.need)) continue;
                var cc = c; AddCmd(c.text, () => Choose(cc));
            }
            WaitingChoice = true; arrow.enabled = false;
            if (!string.IsNullOrEmpty(cur.prompt)) { namePlate.gameObject.SetActive(false); bodyText.text = cur.prompt; }
        }
        void Choose(Choice c)
        {
            if (!string.IsNullOrEmpty(c.clue) && !st.clues.Contains(c.clue)) { st.clues.Add(c.clue); Toast("手掛かり：" + c.clue); }
            if (!string.IsNullOrEmpty(c.flag)) AddFlag(c.flag);
            if (!string.IsNullOrEmpty(c.trust)) AddTrust(c.trust, 1);
            if (c.miss) SetMiss(cur.id, GetMiss(cur.id) + 1);
            backlog.Add("<color=#5de2ed>→ " + c.text + "</color>");
            Go(c.to);
        }
        public bool ChooseIndex(int i) { if (!WaitingChoice || i < 0 || i >= choiceActs.Count) return false; choiceActs[i](); return true; }
        // ---- 合言葉の入力（かなパネル） ----
        string typed = ""; public bool InInput { get; private set; }
        void ShowInput(Node n)
        {
            ClearCommands(); InInput = true; typed = ""; namePlate.gameObject.SetActive(false); arrow.enabled = false;
            promptText.text = n.prompt;
            foreach (var ch in (n.kana ?? ""))
            {
                var c = ch.ToString();
                AddCmd(c, () => { if (typed.Length < 6) typed += c; RefreshInput(); }, keep: true);
            }
            AddCmd("一字けす", () => { if (typed.Length > 0) typed = typed.Substring(0, typed.Length - 1); RefreshInput(); }, keep: true);
            // 詰まったら手掛かり（石碑の文字の割り当てなど）をいつでも見直せる
            if (!string.IsNullOrEmpty(n.note)) AddCmd(string.IsNullOrEmpty(n.noteLabel) ? "▼ 手掛かりを見る" : n.noteLabel, () => { showNote = !showNote; Play("click"); RefreshInput(); }, keep: true);
            AddCmd("★ 唱える", () => SubmitInput(typed), keep: true);
            WaitingChoice = true; InputGrid(); RefreshInput();
        }
        bool showNote;
        void RefreshInput()
        {
            string note = showNote && !string.IsNullOrEmpty(cur.note) ? "\n<size=30><color=#8fe3ef>" + cur.note + "</color></size>" : "";
            bodyText.text = cur.prompt + note + "\n<size=56><color=#f8cb7c>" + (typed.Length == 0 ? "＿" : typed) + "</color></size>";
        }
        // 入力中はボタンを小さく多列に（縦画面4列・横画面2列）→ 下の道具ボタンに重ならない
        void InputGrid()
        {
            if (!InInput) return;
            var g = cmdList.GetComponent<GridLayoutGroup>();
            if (landscape) { float w = side.sizeDelta.x - 44; bool wd = w >= 700; g.constraintCount = wd ? 4 : 2; g.cellSize = new Vector2((w - (wd ? 36 : 12)) / (wd ? 4 : 2), wd ? 80 : 64); }
            else { g.constraintCount = 4; g.cellSize = new Vector2(241, 66); }
        }
        public void SubmitInput(string s)
        {
            if (!InInput) return;
            if (s == cur.answer)
            {
                InInput = false; showNote = false; ApplyLayout(true); ClearCommands(); Play("click"); Toast("合言葉：" + s); Go(cur.next);
            }
            else
            {
                Play("scare"); typed = "";
                var keep = cur; InInput = false; ApplyLayout(true); ClearCommands();
                PlayLines(new List<Line> { new Line { who = "lumi", text = string.IsNullOrEmpty(keep.hint) ? "違うようです。もう一度。" : keep.hint } }, () => ShowInput(keep));
            }
        }
        public void TestAnswer() { if (InInput) SubmitInput(cur.answer); }

        public List<string> CommandLabels()
        {
            var l = new List<string>(); foreach (Transform t in cmdList) l.Add(t.GetComponentInChildren<Text>().text); return l;
        }

        void ShowEnd()
        {
            StopAuto(); ClearCommands(); endPanel.SetActive(true);
            bool more = !string.IsNullOrEmpty(cur.next); AtEnd = !more; AtChapterEnd = more; nextBtn.gameObject.SetActive(more);
            endTitle.text = cur.title;
            int trust = st.trustVals.Sum();
            endBody.text = cur.text + "\n\n手掛かり " + st.clues.Count + "件 ／ 仲間の信頼 " + trust + " ／ LIFE " + st.life + "\n<size=26><color=#9fb3c4>" + string.Join("・", st.clues) + "</color></size>";
            Save();
        }

        // ---- 立ち絵 ----
        static int PosIndex(string p) => p == "L" ? 0 : p == "R" ? 2 : 1;
        // 顔アップ：立ち絵（640x640）から顔のあたりだけ切り出して表示
        void ShowFace(Line l)
        {
            bool on = Cast.IsChar(l.who); speaking = on ? l.who : null;
            faceBox.gameObject.SetActive(on);
            var br = bodyText.rectTransform; br.offsetMin = new Vector2(on ? 22 + faceSize + 28 : 34, br.offsetMin.y);
            if (!on) return;
            int at = Array.IndexOf(slotId, l.who);
            var sp = string.IsNullOrEmpty(l.expr) && at >= 0 && slots[at].sprite ? slots[at].sprite : CharSprite(l.who, l.expr);
            if (!sp) { faceBox.gameObject.SetActive(false); br.offsetMin = new Vector2(34, br.offsetMin.y); return; }
            faceImg.texture = sp.texture;
            faceImg.uvRect = FaceUV(l.who);
        }
        static Rect FaceUV(string id) => new Rect(.29f, id == "rosa" ? .43f : .52f, .42f, .42f); // ローザだけ顔の位置が低い
        void LayoutParty()
        {
            party.anchorMin = cmdList.anchorMin; party.anchorMax = cmdList.anchorMax; party.pivot = cmdList.pivot;
            party.anchoredPosition = cmdList.anchoredPosition; party.sizeDelta = cmdList.sizeDelta;
            float w = side.sizeDelta.x - 44, h = cmdList.sizeDelta.y;
            float c = Mathf.Min((w - 30) / 4, (h - 10) / 2, 230);
            party.GetComponent<GridLayoutGroup>().cellSize = new Vector2(c, c);
        }
        void RefreshParty()
        {
            bool show = cmdList.childCount == 0 && !InDungeon && !InAction && !InInput;
            if (party.gameObject.activeSelf != show) party.gameObject.SetActive(show);
            if (!show) return;
            foreach (var kv in partyCells)
            {
                bool on = kv.Key == speaking;
                kv.Value.frame.color = on ? Gold : new Color(.3f, .4f, .46f);
                kv.Value.face.color = on || speaking == null ? Color.white : new Color(.55f, .58f, .64f);
            }
        }
        void SizeFace(float dialogH)
        {
            faceSize = dialogH - 44; faceBox.sizeDelta = new Vector2(faceSize, faceSize);
            if (faceBox.gameObject.activeSelf) { var br = bodyText.rectTransform; br.offsetMin = new Vector2(22 + faceSize + 28, br.offsetMin.y); }
        }
        Sprite CharSprite(string id, string expr)
        {
            var s = Resources.Load<Sprite>("Chars/" + id + "_" + (string.IsNullOrEmpty(expr) ? "normal" : expr));
            return s ? s : Resources.Load<Sprite>("Chars/" + id + "_normal");
        }
        void SetStage(List<Show> show)
        {
            for (int i = 0; i < 3; i++) { slotId[i] = null; slots[i].enabled = false; }
            if (show == null) return;
            foreach (var s in show) { int i = PosIndex(s.pos); slotId[i] = s.id; slots[i].sprite = CharSprite(s.id, s.expr); slots[i].enabled = slots[i].sprite; }
        }
        void PutSpeaker(string id, string expr)
        {
            int at = Array.IndexOf(slotId, id);
            if (at < 0) { at = slotId.All(x => x == null) ? 1 : 2; slotId[at] = id; }
            if (!string.IsNullOrEmpty(expr) || !slots[at].enabled) { slots[at].sprite = CharSprite(id, expr); slots[at].enabled = slots[at].sprite; }
        }
        void Highlight(string who)
        {
            bool ch = Cast.IsChar(who);
            for (int i = 0; i < 3; i++)
            {
                bool on = !ch || slotId[i] == who;
                slots[i].color = on ? Color.white : new Color(.42f, .45f, .52f);
                slots[i].transform.localScale = on && ch ? Vector3.one * 1.02f : Vector3.one;
            }
            if (ch) { int at = Array.IndexOf(slotId, who); if (at >= 0) slots[at].transform.SetAsLastSibling(); }
        }
        void SetBG(string name)
        {
            var s = string.IsNullOrEmpty(name) || name == "black" ? null : Resources.Load<Sprite>("BG/" + name);
            bg.sprite = s; bg.color = s ? Color.white : Color.black;
        }

        // ---- 状態 ----
        bool Has(string f) => st.flags.Contains(f);
        void AddFlag(string f) { if (!st.flags.Contains(f)) st.flags.Add(f); }
        void AddTrust(string id, int v)
        {
            int i = st.trustKeys.IndexOf(id); if (i < 0) { st.trustKeys.Add(id); st.trustVals.Add(0); i = st.trustKeys.Count - 1; }
            st.trustVals[i] += v; Toast(Cast.Name(id) + "の信頼が上がった");
        }
        int GetMiss(string id) { int i = st.missKeys.IndexOf(id); return i < 0 ? 0 : st.missVals[i]; }
        void SetMiss(string id, int v) { int i = st.missKeys.IndexOf(id); if (i < 0) { st.missKeys.Add(id); st.missVals.Add(v); } else st.missVals[i] = v; }
        void UpdateStatus()
        {
            string hearts = new string('■', st.life) + "<color=#40505c>" + new string('■', 3 - st.life) + "</color>";
            int dots = Mathf.RoundToInt(st.lumi * 3);
            string lumi = "<color=#5de2ed>" + new string('●', dots) + "</color><color=#40505c>" + new string('●', 3 - dots) + "</color>";
            statusText.text = "手掛かり <color=#f8cb7c>" + st.clues.Count + "</color>　LIFE <color=#ff7d7d>" + hearts + "</color>\nルミの光 " + lumi;
        }

        // ---- 道具 ----
        void OpenNote()
        {
            var sb = new System.Text.StringBuilder("<color=#f8cb7c>■ 手掛かり</color>\n");
            if (st.clues.Count == 0) sb.Append("　まだありません\n"); foreach (var c in st.clues) sb.Append("・" + c + "\n");
            sb.Append("\n<color=#f8cb7c>■ 仲間の信頼</color>\n");
            for (int i = 0; i < st.trustKeys.Count; i++) sb.Append("・" + Cast.Name(st.trustKeys[i]) + "　" + new string('★', st.trustVals[i]) + "\n");
            sb.Append("\n<color=#f8cb7c>■ 古地図</color>\n・" + (Has("map") ? "左右が反転している。鏡に映すと正しい村の形になる" : "村の配置と合わない……？"));
            noteText.text = sb.ToString(); notePanel.SetActive(true);
        }
        void OpenLog() { logText.text = string.Join("\n", backlog.Skip(Math.Max(0, backlog.Count - 14))); logPanel.SetActive(true); }
        void ToggleAuto()
        {
            autoOn = !autoOn; autoBtn.GetComponentInChildren<Text>().text = autoOn ? "オート中" : "オート";
            if (autoOn && waitClick) autoCo = StartCoroutine(AutoNext()); if (!autoOn) StopAuto();
        }
        void StopAuto() { if (autoCo != null) StopCoroutine(autoCo); autoCo = null; }
        void ToggleVoice()
        {
            voiceOn = !voiceOn; voiceBtn.GetComponentInChildren<Text>().text = voiceOn ? "声 ON" : "声 OFF";
            if (!voiceOn) { voiceSrc.Stop(); seSrc.Stop(); }
        }
        Coroutine toastCo;
        void Toast(string s) { if (toastCo != null) StopCoroutine(toastCo); toastCo = StartCoroutine(ToastCo(s)); }
        IEnumerator ToastCo(string s) { toast.text = s; yield return new WaitForSeconds(2f); toast.text = ""; }

        // ---- 演出 ----
        IEnumerator Flash() { for (float t = 0; t < .45f; t += Time.deltaTime) { flashImg.color = new Color(1, 1, 1, .85f * (1 - t / .45f)); yield return null; } flashImg.color = new Color(1, 1, 1, 0); }
        IEnumerator Shake()
        {
            var basePos = sceneWin.anchoredPosition;
            for (float t = 0; t < .35f; t += Time.deltaTime) { sceneWin.anchoredPosition = basePos + UnityEngine.Random.insideUnitCircle * 14; yield return null; }
            sceneWin.anchoredPosition = basePos;
        }
        static Texture2D ScanTex()
        {
            var t = new Texture2D(1, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            t.SetPixels(new[] { new Color(0, 0, 0, .16f), new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), new Color(0, 0, 0, 0) }); t.Apply(); return t;
        }
        void Play(string name) { if (!voiceOn && name != "click") return; if (se.TryGetValue(name, out var c)) seSrc.PlayOneShot(c); }
        void BuildSounds()
        {
            se["click"] = Tone(.06f, f => 880, .18f, false);
            se["scare"] = Tone(.45f, f => Mathf.Lerp(190, 60, f), .5f, true);
            se["drop"] = Tone(.35f, f => Mathf.Lerp(900, 300, Mathf.Sqrt(f)), .35f, false);
            se["bell"] = Bell(3.2f);
            se["step"] = Tone(.09f, f => Mathf.Lerp(140, 70, f), .35f, false);
        }
        static AudioClip Tone(float len, Func<float, float> freq, float vol, bool saw)
        {
            int rate = 44100, n = (int)(rate * len); var d = new float[n]; double ph = 0;
            for (int i = 0; i < n; i++)
            {
                float f = (float)i / n; ph += freq(f) / rate; float env = Mathf.Min(1, i / (rate * .01f)) * (1 - f);
                float w = saw ? (float)(2 * (ph - Math.Floor(ph + .5))) : Mathf.Sin((float)(ph * 2 * Math.PI));
                d[i] = w * env * vol;
            }
            var c = AudioClip.Create("se", n, 1, rate, false); c.SetData(d, 0); return c;
        }
        static AudioClip Bell(float len)
        {
            int rate = 44100, n = (int)(rate * len); var d = new float[n];
            float[] fr = { 110, 220.5f, 331, 523, 690 }; float[] am = { .5f, .3f, .18f, .1f, .06f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate, v = 0; for (int k = 0; k < fr.Length; k++) v += Mathf.Sin(2 * Mathf.PI * fr[k] * t) * am[k] * Mathf.Exp(-t * (1.2f + k * .6f));
                d[i] = v * .6f * Mathf.Min(1, i / (rate * .005f));
            }
            var c = AudioClip.Create("bell", n, 1, rate, false); c.SetData(d, 0); return c;
        }

        void Update()
        {
            ApplyLayout(); RefreshParty();
            float pulse = .75f + .25f * Mathf.Sin(Time.time * 3);
            if (lumiIcon.enabled) lumiIcon.color = Color.Lerp(new Color(.75f, .85f, 1f), Color.white, st.lumi * pulse);
            if (arrow.enabled) arrow.color = new Color(Gold.r, Gold.g, Gold.b, .5f + .5f * Mathf.Sin(Time.time * 6));
            bool adv = false, close = false; int pick = -1;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                adv = k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame;
                close = k.escapeKey.wasPressedThisFrame;
                Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
                for (int i = 0; i < keys.Length; i++) if (k[keys[i]].wasPressedThisFrame) pick = i;
            }
#else
            adv = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
            close = Input.GetKeyDown(KeyCode.Escape);
            for (int i = 0; i < 9; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) pick = i;
#endif
            if (close) { notePanel.SetActive(false); logPanel.SetActive(false); }
            if (InDungeon && dun && !dun.moving && !notePanel.activeSelf && !logPanel.activeSelf)
            {
                bool up, dn, lf, rg;
#if ENABLE_INPUT_SYSTEM
                up = k != null && (k.upArrowKey.isPressed || k.wKey.isPressed); dn = k != null && (k.downArrowKey.isPressed || k.sKey.isPressed);
                lf = k != null && (k.leftArrowKey.isPressed || k.aKey.isPressed); rg = k != null && (k.rightArrowKey.isPressed || k.dKey.isPressed);
#else
                up = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W); dn = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
                lf = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A); rg = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
#endif
                if (up) dun.Step(1); else if (dn) dun.Step(-1); else if (lf) dun.Turn(-1); else if (rg) dun.Turn(1);
                pick = -1; adv = false;
            }
            if (adv) Advance();
            if (pick >= 0) { if (InAction) { int[] m = { 6, 7, 8, 3, 4, 5, 0, 1, 2 }; actHit = m[pick]; } else ChooseIndex(pick); }
        }
        void OnApplicationPause(bool p) { if (p) Save(); }
    }
}
