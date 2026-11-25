using System; using System.Collections.Generic;
namespace Day117
{
    [Serializable] public class Show { public string id; public string pos; public string expr; }
    [Serializable] public class Line
    {
        public string who, text, expr, voice, se, clue, flag, trust, cg;
        public List<Show> show; public bool flash, shake, lumiGlow; public int life;
        public bool hasShow; // JsonUtilityは「無い」と「空」を区別できないので、シナリオ生成側で付ける
    }
    [Serializable] public class Choice { public string text, to, clue, flag, trust, need, once; public bool miss; }
    [Serializable] public class Node
    {
        public string id, type, bg, place, next, prompt, exitText, exitTo, hint, title, text;
        public int chapter, hits; public float window; public string kana, answer, map, events, sprite, note, noteLabel;
        public List<Line> lines; public List<Choice> choices; public List<string> require;
    }
    [Serializable] public class Chapter { public string title, start; public List<Node> nodes; }

    [Serializable] public class SaveData
    {
        public string node; public int line; public int life = 3;
        public List<string> flags = new List<string>(), clues = new List<string>(), trustKeys = new List<string>();
        public List<int> trustVals = new List<int>();
        public List<string> missKeys = new List<string>(); public List<int> missVals = new List<int>();
        public float lumi; public int dx = -1, dy, ddir = 1;
    }

    public static class Cast
    {
        public static readonly string[] Ids = { "yuma", "gantz", "leon", "eris", "rosa", "mio", "sakura", "dante" };
        public static readonly string[] Exprs = { "normal", "smile", "surprise", "fear", "angry", "think" };
        public static string Name(string id) => id switch
        {
            "yuma" => "ユウマ", "gantz" => "ガンツ", "leon" => "レオン", "eris" => "エリス", "rosa" => "ローザ",
            "mio" => "ミオ", "sakura" => "サクラ", "dante" => "ダンテ", "lumi" => "ルミ", _ => ""
        };
        public static bool IsChar(string id) => Array.IndexOf(Ids, id) >= 0;
    }

    public static class ScenarioLoader
    {
        public static readonly string[] Files = { "ch1", "ch2", "ch3", "ch4" };
        /// 全章を1つにまとめて読む（開始はch1）
        public static Chapter LoadAll()
        {
            Chapter all = null;
            foreach (var f in Files)
            {
                var ta = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Scenario/" + f); if (!ta) continue;
                var c = Load(ta.text); if (all == null) all = c; else all.nodes.AddRange(c.nodes);
            }
            return all;
        }
        public static Chapter Load(string json)
        {
            var ch = UnityEngine.JsonUtility.FromJson<Chapter>(json);
            foreach (var n in ch.nodes)
            {
                if (n.lines == null) n.lines = new List<Line>();
                if (n.choices == null) n.choices = new List<Choice>();
                if (n.require == null) n.require = new List<string>();
            }
            return ch;
        }
    }
}
