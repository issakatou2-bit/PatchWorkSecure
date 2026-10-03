#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [Test] public void Next12Records_年数と引退と自己ベストを記録し再読込で重複しない()
        {
            var run=OpsEndless.Begin(14,null);var career=new OpsCareer{endlessUnlocked=true};
            for(int y=1;y<=5;y++){FinishEndlessTestYear(run.state);run.RecordYear();career.RecordEndless(run);if(y<5)run.AdvanceYear();}
            Assert.IsTrue(career.HasTitle("guard-3"));Assert.IsTrue(career.HasTitle("guard-5"));Assert.IsFalse(career.HasTitle("guard-10"));Assert.AreEqual(0,career.bestDurationMonths,"自己ベストは挑戦の終了時に確定");
            Assert.IsTrue(run.Retire());Assert.IsTrue(career.RecordEndless(run));Assert.IsTrue(career.HasTitle("retire-5"));Assert.AreEqual(60,career.bestDurationMonths);Assert.AreEqual(run.TotalScore,career.bestTotalScore);Assert.AreEqual(OpsCatalog.EndlessRank(run.TotalScore),career.bestOverallRank);Assert.IsTrue(run.bestUpdated);Assert.IsTrue(run.recordedCareer);
            int n=career.titles.Count;Assert.IsFalse(career.RecordEndless(run));Assert.AreEqual(n,career.titles.Count);
            var c=JsonUtility.FromJson<OpsCareer>(JsonUtility.ToJson(career));Assert.IsTrue(c.Valid());CollectionAssert.AreEqual(career.titles,c.titles);Assert.AreEqual(career.bestTotalScore,c.bestTotalScore);
            var shortRun=OpsEndless.Begin(14,null);FinishStoryTestYear(shortRun.state);shortRun.Retire();c.RecordEndless(shortRun);Assert.IsFalse(shortRun.bestUpdated);Assert.AreEqual(60,c.bestDurationMonths);
        }
        [Test] public void Next12Records_九称号とランクの境界と三十九既読を検査する()
        {
            Assert.AreEqual(10,OpsCatalog.TitleIds.Length);Assert.AreEqual(OpsCatalog.TitleIds.Length,OpsCatalog.TitleNames.Length);Assert.AreEqual(OpsCatalog.TitleIds.Length,OpsCatalog.TitleHints.Length);
            Assert.AreEqual("S",OpsCatalog.EndlessRank(OpsCatalog.EndlessRankSS-1));Assert.AreEqual("SS",OpsCatalog.EndlessRank(OpsCatalog.EndlessRankSS));Assert.AreEqual("C",OpsCatalog.EndlessRank(0));
            var c=new OpsCareer{diary=null,titles=null,bestOverallRank=null};Assert.IsTrue(c.Valid());Assert.IsFalse(c.RecordDiaryTitle());c.diary=new System.Collections.Generic.List<OpsDiaryRecord>();
            foreach(int key in Enumerable.Range(0,38))c.diary.Add(new OpsDiaryRecord{key=key,content=key,rank="B",recap="実記録",thought="一言",minigame=""});Assert.IsFalse(c.RecordDiaryTitle());
            c.diary.Add(new OpsDiaryRecord{key=38,content=38,rank="SS",recap="実記録",thought="一言",minigame=""});Assert.IsTrue(c.RecordDiaryTitle());Assert.IsFalse(c.RecordDiaryTitle());Assert.IsTrue(c.HasTitle("diary-all"));Assert.IsTrue(c.Valid());
            c.titles.Add("不明");Assert.IsFalse(c.Valid());
        }
        [Test] public void Next12Balance_採用した式と総合ランク境界と十年称号を維持する()
        {
            Assert.AreEqual(30,OpsCatalog.EndlessPressure(4));Assert.AreEqual(40,OpsCatalog.EndlessPressure(5));Assert.AreEqual(150,OpsCatalog.EndlessPressure(10));
            Assert.AreEqual(2,OpsCatalog.EndlessMonthlyIncomePerYear);
            long[] thresholds={7116,8032,11707,12329};string[] previous={"C","B","A","S"},ranks={"B","A","S","SS"};
            for(int i=0;i<thresholds.Length;i++){Assert.AreEqual(previous[i],OpsCatalog.EndlessRank(thresholds[i]-1));Assert.AreEqual(ranks[i],OpsCatalog.EndlessRank(thresholds[i]));}
            CollectionAssert.AreEqual(new[]{3,5,10},OpsCatalog.EndlessGuardYears);
            var e=OpsEndless.Begin(14,null);var career=new OpsCareer{endlessUnlocked=true};
            for(int y=1;y<=10;y++){FinishEndlessTestYear(e.state);Assert.IsTrue(e.RecordYear());career.RecordEndless(e);Assert.AreEqual(y>=10,career.HasTitle("guard-10"));if(y<10)Assert.IsTrue(e.AdvanceYear());}
            // 強い入力で条件だけを検査。人間の上手なプレイで10年へ届くという証拠にはしない。
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next12RecordsUI_未獲得と獲得の記録画面を既存部品で撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.OpenRecords();yield return new WaitForSecondsRealtime(1);Capture("next12-records-empty");
            Assert.AreEqual("未記録",Find<TextMeshProUGUI>("RecordBestValue0").text);Assert.AreEqual("？",Find<TextMeshProUGUI>("RecordTitleName0").text);
            var e=OpsEndless.Begin(14,null);for(int y=1;y<=3;y++){FinishEndlessTestYear(e.state);e.RecordYear();if(y<3)e.AdvanceYear();}e.Retire();var career=new OpsCareer{endlessUnlocked=true};career.RecordEndless(e);Assert.IsTrue(game.RestoreProgress(new OpsProgress{endless=e,career=career}));game.OpenRecords();yield return new WaitForSecondsRealtime(1);Capture("next12-records-earned");
            Assert.AreEqual("3年",Find<TextMeshProUGUI>("RecordBestValue0").text);Assert.AreEqual("3年の守り",Find<TextMeshProUGUI>("RecordTitleName0").text);
            foreach(var t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>()){t.ForceMeshUpdate();Assert.IsFalse(t.isTextOverflowing,t.name+" / "+t.text);}Find<Button>("RecordsClose").onClick.Invoke();yield return new WaitForSecondsRealtime(1);Assert.IsNotNull(Find<Button>("HomeRecords"));CheckPointer("HomeRecords");Capture("next12-title-records");
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
