#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        // 元の11設備の有効な年度を使い、保存の読み込みで表示が変わらないことを確かめる。
        // 比較対象は同じ入力のメモリ上の表示。改修前コードの撮影を偽装しない。
        private static string LegacyDisplaySignature(OpsGame game)
        {
            return string.Join("\n",game.Surface.GetComponentsInChildren<TextMeshProUGUI>()
                .Where(t=>t.gameObject.activeInHierarchy&&t.name!="DiaryVoiceCaption"&&t.name!="NavigatorSpeech")
                .Select(t=>{
                    string path=t.name;for(var p=t.transform.parent;p!=null&&p!=game.Surface;p=p.parent)path=p.name+"/"+path;
                    return path+"|"+t.text+"|"+t.rectTransform.sizeDelta+"|"+t.rectTransform.anchoredPosition+"|"+t.color+"|"+t.fontSize+"|"+t.fontStyle;
                }).OrderBy(s=>s,StringComparer.Ordinal));
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next11LegacyUI_旧十一設備の実ファイルで四画面と移行前バックアップを保つ()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);
            var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            typeof(OpsGame).GetProperty("ReducedMotion").SetValue(game,true);
            typeof(OpsGame).GetProperty("CaptionsEnabled").SetValue(game,false);
            string dir=Path.Combine(Application.dataPath,"../Artifacts/Next11/LegacySave");Directory.CreateDirectory(dir);
            foreach(string view in new[]{"incident","report","diary","annual"})
            {
                var state=new OpsState(14,true);
                for(int i=0;i<state.levels.Length;i++)state.levels[i]=i%2+1;
                state.budget=400;state.culture=72;state.fatigue=24;
                if(view=="annual")FinishStoryTestYear(state);
                else {Assert.IsTrue(state.BeginIncident());if(view!="incident")Assert.IsTrue(state.Resolve("scope"));}
                Assert.IsTrue(state.Valid());Assert.AreEqual(11,state.levels.Length);
                string path=Path.Combine(dir,view+".json");
                string original=JsonUtility.ToJson(state).Replace("\"yearGrowthRules\":0,","").Replace(",\"yearGrowthRules\":0","");
                File.WriteAllText(path,original);byte[] bytes=File.ReadAllBytes(path);
                Assert.IsTrue(game.RestoreProgress(new OpsProgress{single=JsonUtility.FromJson<OpsState>(original)}));
                if(view=="diary")game.OpenMonthlyDiary();
                yield return new WaitForSecondsRealtime(3);Canvas.ForceUpdateCanvases();Capture("next11-legacy-"+view+"-reference");
                string expected=LegacyDisplaySignature(game);
                var loaded=OpsSaveStore.ReadProgress(path,out string warning);Assert.IsNotNull(loaded,warning);
                CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path),"読み込みでは元ファイルを変更しない");
                Assert.AreEqual(11,loaded.single.levels.Length);Assert.IsTrue(game.RestoreProgress(loaded));
                if(view=="diary")game.OpenMonthlyDiary();
                yield return new WaitForSecondsRealtime(3);Canvas.ForceUpdateCanvases();Capture("next11-legacy-"+view+"-loaded");
                Assert.AreEqual(expected,LegacyDisplaySignature(game),view+"の文字・大きさ・位置・色が旧11設備の入力と同じ");
                Assert.IsFalse(game.Surface.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>OpsCatalog.AdvancedProjects.Any(p=>t.text.Contains(p.name))),"旧保存に追加設備を表示しない");
                Assert.IsTrue(OpsSaveStore.WriteProgress(path,loaded,out warning),warning);
                CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path+".bak"),"形式を変えて書く前に元ファイルをそのまま退避する");
                Assert.IsNotNull(OpsSaveStore.ReadProgress(path+".bak",out warning),warning);
                Assert.IsNotNull(OpsSaveStore.ReadProgress(path,out warning),warning);
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
