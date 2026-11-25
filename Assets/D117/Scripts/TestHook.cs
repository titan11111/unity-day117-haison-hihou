#if UNITY_EDITOR
using System.Collections; using System.Collections.Generic; using System.IO; using System.Text; using UnityEngine;
namespace Day117
{
    /// エディタ専用：自動テスト。第1章を最後まで自動で進め、要所でスクショを撮る
    public class TestHook : MonoBehaviour
    {
        string tag;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var t = UnityEditor.EditorPrefs.GetString("d117_test_tag", ""); if (t == "") return;
            Application.runInBackground = true;
            new GameObject("TestHook").AddComponent<TestHook>().tag = t;
        }
        string Dir => Path.GetFullPath("../Shots");
        void Shot(string n) { Directory.CreateDirectory(Dir); ScreenCapture.CaptureScreenshot(Path.Combine(Dir, $"test_{tag}_{n}.png")); }
        IEnumerator Start()
        {
            var log = new StringBuilder(); var tried = new HashSet<string>(); var shot = new HashSet<string>();
            yield return new WaitForSeconds(1.2f); Shot("0title"); yield return new WaitForSeconds(.4f);
            var nv = Novel.I; Novel.FastTest = true; nv.NewGame();
            int steps = 0; string lastNode = "";
            while (!nv.AtEnd && steps < 30000)
            {
                steps++;
                if (nv.NodeId != lastNode) { lastNode = nv.NodeId; log.AppendLine("→ " + lastNode); }
                // 撮影ポイント（文字が出そろった状態で撮る）
                foreach (var p in new[] { "c1_gate", "c3_tower", "c4_open", "c4_helmet", "c4_exit" })
                    if (nv.NodeId == p && !shot.Contains(p) && (!p.EndsWith("square") && !p.EndsWith("tavern") && !p.EndsWith("tower") || nv.WaitingChoice))
                    { shot.Add(p); Novel.FastTest = false; yield return new WaitForSeconds(2.6f); Shot(steps.ToString("D4") + "_" + p); yield return new WaitForSeconds(.4f); Novel.FastTest = true; }
                if (nv.AtChapterEnd) { yield return new WaitForSeconds(.6f); Shot(steps.ToString("D4") + "_chapterend"); yield return new WaitForSeconds(.3f); nv.ContinueNext(); }
                else if (nv.InDungeon) { if (!shot.Contains("dun")) { shot.Add("dun"); yield return new WaitForSeconds(1f); Shot(steps.ToString("D4") + "_dungeon"); } nv.DungeonAuto(); }
                else if (nv.InInput) { if (!shot.Contains("input")) { shot.Add("input"); yield return new WaitForSeconds(.5f); Shot(steps.ToString("D4") + "_input"); } nv.TestAnswer(); }
                else if (nv.InAction) { if (!shot.Contains("act")) { shot.Add("act"); Novel.FastTest = false; yield return new WaitForSeconds(1.2f); Shot(steps.ToString("D4") + "_action"); Novel.FastTest = true; } }
                else if (nv.WaitingChoice)
                {
                    var labels = nv.CommandLabels().ConvertAll(l => l.Contains(". ") ? l.Substring(l.IndexOf(". ") + 2) : l); int pick = labels.FindIndex(l => l.Contains("★"));
                    if (pick < 0) pick = labels.FindIndex(l => !tried.Contains(nv.NodeId + "|" + l));
                    if (pick < 0) pick = 0;
                    tried.Add(nv.NodeId + "|" + labels[pick]); log.AppendLine("   選択：" + labels[pick]);
                    nv.ChooseIndex(pick);
                }
                else nv.Advance();
                yield return null;
            }
            yield return new WaitForSeconds(.8f); Shot("9end");
            var s = nv.State;
            log.Insert(0, $"結果：{(nv.AtEnd ? "END到達" : "未到達")} steps={steps} 手掛かり={s.clues.Count} LIFE={s.life} フラグ={string.Join(",", s.flags)}\n");
            File.WriteAllText(Path.Combine(Dir, $"test_{tag}.txt"), log.ToString());
            Novel.FastTest = false;
            yield return new WaitForSeconds(.8f);
            UnityEditor.EditorPrefs.DeleteKey("d117_test_tag");
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
