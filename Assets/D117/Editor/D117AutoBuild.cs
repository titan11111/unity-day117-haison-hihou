// 仕掛け：Unityが取り込み直したとき Shots/auto_build.trigger があれば、シナリオ検査→WebGLビルドを自動で行う
// （画面ロック中にメニューを押せないときの代わり。タイタンがUnityを1回クリックすると動く）
using System.IO; using UnityEditor; using UnityEngine;
namespace Day117.EditorTools
{
    [InitializeOnLoad]
    static class D117AutoBuild
    {
        static D117AutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                var trig = Path.Combine(D117Menu.Shots, "auto_build.trigger");
                if (!File.Exists(trig) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete(trig);
                Debug.Log("D117: 自動ビルドを開始します");
                D117Menu.Rebuild(); D117Menu.Check(); D117Menu.WebBuild(); D117Menu.RunTest();
            };
        }
    }
}
