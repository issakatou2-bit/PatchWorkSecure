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
        private static void CheckEndlessTexts()
        {
            foreach(var t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>().Where(t=>t.name.StartsWith("Endless")||t.name=="EndingTitle"))
            {t.ForceMeshUpdate();Assert.IsFalse(t.isTextOverflowing,t.name+" / "+t.text);if(t.name=="EndingTitle")StringAssert.DoesNotContain("、",t.text);}
        }
        [UnityTest] public IEnumerator Next12Screens_続けると引退確認と年数と再開を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.RestoreProgress(new OpsProgress{single=new OpsState(14,true),career=new OpsCareer{endlessUnlocked=true}});game.StartEndless(14);FinishEndlessTestYear(game.State);game.OpenTab(0);yield return new WaitForSecondsRealtime(3);Capture("next12-endless-renew");
            Assert.IsNotNull(Find<Button>("NextEndlessYear"));Assert.IsNotNull(Find<Button>("RetireEndless"));StringAssert.Contains("打ち切りなし",Find<TextMeshProUGUI>("StoryGoalResult").text);CheckPointer("NextEndlessYear");CheckEndlessTexts();
            Find<Button>("NextEndlessYear").onClick.Invoke();yield return null;Assert.AreEqual(2,game.RunYear);Assert.IsTrue(game.YearOpeningActive);game.SkipYearOpening();game.AdvanceYearOpening();
            Assert.AreEqual("2年目",Find<TextMeshProUGUI>("YearLabel").text);FinishEndlessTestYear(game.State);game.OpenTab(0);yield return new WaitForSecondsRealtime(1);Find<Button>("RetireEndless").onClick.Invoke();yield return null;Assert.IsFalse(game.Endless.finished);Assert.IsNotNull(Find<Button>("ConfirmRetireEndless"));Capture("next12-endless-retire-confirm");
            Find<Button>("ConfirmRetireEndless").onClick.Invoke();yield return new WaitForSecondsRealtime(2);Assert.IsTrue(game.Endless.retired);Assert.IsTrue(game.Endless.recordedCareer);Assert.IsNotNull(Find<TextMeshProUGUI>("EndlessBestUpdatedLabel"));Capture("next12-endless-retired");CheckEndlessTexts();
            var saved=game.ExportProgress();Assert.IsTrue(saved.Valid());Assert.IsTrue(game.RestoreProgress(saved));yield return new WaitForSecondsRealtime(1);Assert.IsNotNull(Find<Button>("EndlessBackHome"));Assert.IsNull(UnityEngine.Object.FindObjectsByType<Button>().FirstOrDefault(b=>b.name=="ConfirmFactor"));
            Find<Button>("EndlessAnnualReview").onClick.Invoke();yield return new WaitForSecondsRealtime(2);Assert.IsNotNull(Find<Button>("EndlessAnnualReturn"));Assert.IsNull(UnityEngine.Object.FindObjectsByType<Button>().FirstOrDefault(b=>b.name=="ReplayYear"));Find<Button>("EndlessAnnualReturn").onClick.Invoke();yield return null;Assert.IsNotNull(Find<Button>("EndlessBackHome"));Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next12Screens_運営終了の半分と四年目の月報が日記を上書きしない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            var e=OpsEndless.Begin(14,null);for(int y=1;y<4;y++){FinishEndlessTestYear(e.state);e.AdvanceYear();}
            var career=new OpsCareer{endlessUnlocked=true};Assert.IsTrue(game.RestoreProgress(new OpsProgress{endless=e,career=career}));DiaryTestMonth(game.State);game.OpenTab(0);yield return new WaitForSecondsRealtime(2);Assert.AreEqual("4年目",Find<TextMeshProUGUI>("YearNumber").text);
            string before=JsonUtility.ToJson(career);game.OpenMonthlyDiary();yield return null;Assert.AreEqual(1,game.State.month);Assert.AreEqual(before,JsonUtility.ToJson(career));Assert.IsFalse(game.DiaryActive);
            game.State.stability=0;game.State.phase=OpsPhase.Ended;game.OpenTab(0);yield return new WaitForSecondsRealtime(2);Assert.IsTrue(e.finished);Assert.IsFalse(e.retired);StringAssert.Contains("半分",Find<TextMeshProUGUI>("EndlessScoreHint").text);Assert.AreEqual(e.TotalScore+"点",Find<TextMeshProUGUI>("EndlessScoreValue").text.Replace(",",""));Capture("next12-endless-failed");CheckEndlessTexts();
            Assert.IsTrue(game.ExportProgress().Valid());Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
