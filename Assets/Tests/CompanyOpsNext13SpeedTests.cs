#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator Next13Speed_設定と演出時間だけを変え制限時間と状態を保つ()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            Click("HomeSettings");yield return new WaitForSecondsRealtime(.5f);Capture("next13-speed-settings-standard");CheckPointer("SpeedFast");Click("SpeedFast");yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(game.FastPresentation);Assert.AreEqual(2,game.PresentationRate);Capture("next13-speed-settings-fast");CheckPointer("ShortenInterruptions");Click("CloseDialog");
            float timeScale=Time.timeScale;game.SetPresentationSpeed(false);float start=Time.realtimeSinceStartup;game.StartYear(14);while(game.PhasePresentationRunning)yield return null;float standard=Time.realtimeSinceStartup-start;
            string baseline=JsonUtility.ToJson(game.State);game.SetPresentationSpeed(true);start=Time.realtimeSinceStartup;game.StartYear(14);while(game.PhasePresentationRunning)yield return null;float fast=Time.realtimeSinceStartup-start;
            Assert.Less(fast,standard*.8f+.1f);Assert.AreEqual(baseline,JsonUtility.ToJson(game.State));Assert.AreEqual(timeScale,Time.timeScale);
            // 設定の保存・読込だけ実動作を通す。年度の保存ファイルは書かない。
            try
            {
                OpsGame.TestMode=false;game.SetPresentationSpeed(true);Assert.AreEqual(1,PlayerPrefs.GetInt("pws_ops_speed"));
                typeof(OpsGame).GetProperty("FastPresentation").SetValue(game,false);
                typeof(OpsGame).GetMethod("LoadDisplaySettings",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);Assert.IsTrue(game.FastPresentation);
            }
            finally{OpsGame.TestMode=true;}
            var fixture=DecisionFixture("bec",false);game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);game.BeginIncident();while(game.PhasePresentationRunning)yield return null;
            string initial=JsonUtility.ToJson(game.State);game.ChooseResponse("scope");Assert.IsInstanceOf<OpsMailMinigame>(game.Minigame);
            var instructions=Find<TextMeshProUGUI>("MinigameInstructions").text.Split('\n');
            // 短縮した遊び方は従来の2行のまま。Next-17の操作説明1行を別に確認する。
            Assert.AreEqual(3,instructions.Length);
            Assert.AreEqual("怪しいメールは報告（→）。本物は仕事を進める（←）。",instructions[0]);
            Assert.AreEqual("リンクの行き先はカーソル／長押しで確認。40秒で10通。",instructions[1]);
            Assert.AreEqual("パッド：十字キーで選ぶ／Aで決定／リンクを選ぶと行き先を確認",instructions[2]);
            Assert.AreEqual(OpsCatalog.MailSeconds,game.Minigame.Duration);Capture("next13-speed-mail-brief");
            Click("MinigameDelegate");Click("MinigameContinue");yield return WaitForResolution(game);var expected=JsonUtility.FromJson<OpsState>(initial);expected.Resolve("scope");Assert.AreEqual(JsonUtility.ToJson(expected),JsonUtility.ToJson(game.State));
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next13Speed_再挑戦のガイドを省略し開幕のスキップを大きくする()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.StartStory(14);Assert.IsFalse(game.RepeatedChallenge);game.StartStory(15);Assert.IsTrue(game.RepeatedChallenge);Assert.IsFalse(game.TutorialActive);
            try
            {
                PlayerPrefs.SetInt("pws_ops_tutorial_seen",0);OpsGame.TestMode=false;
                typeof(OpsGame).GetMethod("TutorialNewYear",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);Assert.IsFalse(game.TutorialActive);
            }
            finally{OpsGame.TestMode=true;}
            var story=AlliesStory(2);Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story,career=new OpsCareer{startedStoryAttempts=2}}));game.PreviewYearOpening(0);yield return new WaitForSecondsRealtime(.7f);
            var skip=Find<Button>("OpeningSkip");Assert.AreEqual(200,((RectTransform)skip.transform).rect.width);CheckPointer("OpeningSkip");Capture("next13-speed-repeat-opening");Click("OpeningSkip");yield return null;Assert.AreEqual(4,game.YearOpeningStage);
            Assert.IsTrue(game.Career.Valid());Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next13Speed_月報を即座に最終値へ止めて日記に進む()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.StartYear(14);while(game.PhasePresentationRunning)yield return null;DiaryTestMonth(game.State);game.OpenTab(0);
            Assert.IsTrue(game.ReportCounting);var loss=Find<TextMeshProUGUI>("MonthlyLossValue");var stop=Find<TextMeshProUGUI>("MonthlyStopValue");var result=game.State.Latest;string before=JsonUtility.ToJson(game.State);
            game.OpenMonthlyDiary();Assert.IsFalse(game.ReportCounting);Assert.AreEqual(result.loss.ToString("N0")+"<size=17>万円</size>",loss.text);Assert.AreEqual(result.downtime.ToString("N0")+"<size=17>時間</size>",stop.text);
            Assert.IsTrue(game.DiaryActive);Assert.AreEqual(before,JsonUtility.ToJson(game.State));yield return new WaitForSecondsRealtime(.7f);Capture("next13-speed-monthly-skipped");Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
