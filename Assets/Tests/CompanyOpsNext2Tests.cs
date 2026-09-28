using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator チュートリアルは実操作だけで六段階を完了する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);Assert.IsTrue(game.StartTutorial());yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(0,game.TutorialStep);CheckPointer("Stat_0");Click("Stat_0");yield return new WaitForSecondsRealtime(.5f);Click("CloseDialog");yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(1,game.TutorialStep);Assert.IsFalse(game.Surface.GetComponentsInChildren<Transform>(true).First(t=>t.name=="PlanningCharacter").gameObject.activeSelf);
            yield return new WaitForSecondsRealtime(3);CheckPointer("Action_audit");Capture("73-tutorial-audit");Click("Action_audit");yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(2,game.TutorialStep);CheckPointer("ConsultationDetails");Click("ConsultationDetails");yield return new WaitForSecondsRealtime(.5f);CheckPointer("CloseDialog");Click("CloseDialog");yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(3,game.TutorialStep);CheckPointer("OpenProjects");Click("OpenProjects");yield return new WaitForSecondsRealtime(.5f);CheckPointer("Details_inventory");Click("Details_inventory");yield return new WaitForSecondsRealtime(.5f);CheckPointer("Buy_inventory");Click("Buy_inventory");yield return new WaitForSecondsRealtime(.6f);
            Assert.AreEqual(4,game.TutorialStep);CheckPointer("AdvanceMonth");Click("AdvanceMonth");yield return new WaitForSecondsRealtime(.5f);CheckPointer("ConfirmAdvance");Click("ConfirmAdvance");yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual(5,game.TutorialStep);yield return new WaitForSecondsRealtime(3);CheckPointer("Respond_scope");Capture("74-tutorial-incident");Click("Respond_scope");yield return WaitForResolution(game);
            yield return null;Assert.IsFalse(game.TutorialActive);Assert.AreEqual(1,game.State.history.Count);Assert.AreEqual(OpsPhase.Review,game.State.phase);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator チュートリアルは飛ばしてもゲーム数値を変えない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(18);
            string before=JsonUtility.ToJson(game.State);Assert.IsTrue(game.StartTutorial());yield return null;Click("SkipTutorial");yield return null;
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));Assert.IsFalse(game.TutorialActive);Assert.IsTrue(game.Surface.GetComponentsInChildren<Transform>(true).First(t=>t.name=="PlanningCharacter").gameObject.activeSelf);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 月報は三つの数値と実際の支援をモック配置で表示する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);
            game.Buy(OpsCatalog.Index("backup"));game.ChooseAction("audit");game.ChooseAction("map");game.BeginIncident();game.Resolve("recover");yield return WaitForResolution(game);yield return new WaitForSecondsRealtime(1);
            var r=game.State.Latest;Assert.AreEqual(r.loss+"<size=17>万円</size>",Find<TextMeshProUGUI>("MonthlyLossValue").text);
            Assert.AreEqual(r.downtime+"<size=17>時間</size>",Find<TextMeshProUGUI>("MonthlyStopValue").text);
            Assert.AreEqual(new Vector2(40,-140),Find<RectTransform>("MonthlyIncident").anchoredPosition);
            Assert.IsNotNull(r.metricsBefore);Assert.IsNotNull(r.forecast);Assert.AreEqual("今月は支援なし",Find<TextMeshProUGUI>("SupportNone").text);
            CheckPointer("EffectDetails");CheckPointer("ReviewDetails");CheckPointer("NextMonth");Capture("75-monthly-report");CheckText();
            Click("ReviewDetails");yield return new WaitForSecondsRealtime(.5f);CheckPointer("MonthlyLesson");CheckPointer("ViewHistory");CheckText();Click("CloseDialog");yield return new WaitForSecondsRealtime(.25f);
            game.Next();game.State.staffExperience[1]=OpsGrowthCatalog.StaffThresholds[1];game.State.supportOrder="investigate";game.OpenTab(0);game.BeginIncident();game.Resolve("scope");yield return WaitForResolution(game);yield return new WaitForSecondsRealtime(1);
            if(game.State.Latest.power.staff>0)Assert.IsNotNull(Find<TextMeshProUGUI>("SupportName"));Capture("76-monthly-team");LogAssert.NoUnexpectedReceived();
        }
        [Test] public void 年間集計は連携効果を二重計上せず実際の支援と遭遇を数える()
        {
            var state=new OpsState(2);
            for(int i=0;i<2;i++)state.history.Add(new OpsOutcome{month=i,eventId=OpsEventCatalog.Events[0].id,investmentEffects=new System.Collections.Generic.List<OpsInvestmentEffect>{new OpsInvestmentEffect{projectId="backup",avoidedLoss=5},new OpsInvestmentEffect{projectId="drill",avoidedLoss=5}},power=new OpsResponsePower{staff=4,support="佐伯：記録を整理"}});
            state.history.Add(new OpsOutcome{month=2,benign=true,power=new OpsResponsePower{staff=4,support="森：通常業務"}});
            var summary=OpsAnnualSummary.From(state);Assert.AreEqual(2,summary.projects.Count);Assert.AreEqual(10,summary.Mvp.loss);Assert.AreEqual(2,summary.Mvp.activations);Assert.AreEqual(1,summary.encountered.Count);Assert.AreEqual(2,summary.staffSupport[1]);Assert.AreEqual(0,summary.staffSupport[2]);
            var legacy=new OpsState(1);legacy.history.Add(new OpsOutcome{month=0});Assert.IsNull(OpsAnnualSummary.From(legacy).Mvp);Assert.AreEqual(-1,OpsAnnualSummary.From(legacy).StaffMvp);
        }
        [UnityTest] public IEnumerator 年間評価は十二か月とMVPを表示し再挑戦できる()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);
            game.Buy(OpsCatalog.Index("backup"));
            for(int i=0;i<12;i++)
            {
                if(game.State.phase==OpsPhase.Ended)break;
                Plan(game.State);game.OpenTab(0);game.BeginIncident();game.Resolve(game.State.Current.kind=="outage"?"recover":"scope");game.Next();yield return null;
            }
            yield return new WaitForSecondsRealtime(1.6f);Assert.AreEqual(OpsPhase.Ended,game.State.phase);Assert.AreEqual(12,game.State.history.Count);Assert.IsTrue(game.State.IsClear);
            Assert.AreEqual(12,game.Surface.GetComponentsInChildren<RectTransform>().Count(t=>t.name.StartsWith("AnnualMonth")&&!t.name.StartsWith("AnnualMonthName")&&!t.name.StartsWith("AnnualMonthResult")));
            var summary=OpsAnnualSummary.From(game.State);Assert.AreEqual(summary.encountered.Count+" / 40",Find<TextMeshProUGUI>("CollectionValue").text);
            var portraitPosition=Find<RectTransform>("NavigatorPortrait").anchoredPosition;
            Assert.AreEqual(1210,portraitPosition.x);Assert.That(portraitPosition.y,Is.InRange(-630f,-602f),"70px下の基準位置から、喜ぶ動きの28pxだけ上がる");
            CheckPointer("AnnualDetails");CheckPointer("EndingHistory");CheckPointer("BackHome");CheckPointer("ReplayYear");CheckText();Capture("77-annual-report");
            Click("ReplayYear");yield return null;Assert.AreEqual(0,game.State.month);Assert.AreEqual(OpsPhase.Planning,game.State.phase);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 設定はスライダーと表示変更を即時反映し削除前に確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);Click("Menu");yield return new WaitForSecondsRealtime(.6f);
            var s=Find<Slider>("MusicVolume");s.value=.73f;yield return null;Assert.AreEqual("73",Find<TextMeshProUGUI>("MusicVolumePercent").text);Assert.Less(Find<RectTransform>("MusicVolumeFill").rect.width,612);
            Find<Slider>("SoundVolume").value=0;Find<Slider>("VoiceVolume").value=.9f;
            Click("TextSize2");yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual(2,game.TextSize);Assert.Greater(Find<TextMeshProUGUI>("TextSizePreview").fontSize,16);
            Click("CaptionToggle");yield return new WaitForSecondsRealtime(.5f);Assert.IsFalse(game.CaptionsEnabled);Click("ShortenInterruptions");yield return new WaitForSecondsRealtime(.5f);Assert.IsTrue(game.ShortenInterruptions);
            CheckPointer("ReduceMotion");CheckPointer("DeleteRecords");CheckPointer("CloseDialog");Capture("78-settings");CheckText();
            string before=JsonUtility.ToJson(game.State);Click("DeleteRecords");yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual(before,JsonUtility.ToJson(game.State));CheckPointer("ConfirmDeleteRecords");CheckPointer("CloseDialog");Click("CloseDialog");yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ひなたは差分未設定なら一枚絵を保ちゲーム乱数に触れない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.6f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return null;
            var portrait=Find<Image>("NavigatorPortrait");var animator=portrait.GetComponent<OpsPortraitAnimator>();Assert.IsNotNull(animator);Assert.IsFalse(animator.HasFrames);string before=JsonUtility.ToJson(game.State);var original=portrait.sprite;
            yield return new WaitForSecondsRealtime(5.2f);Assert.AreSame(original,portrait.sprite);Assert.AreEqual(before,JsonUtility.ToJson(game.State));Assert.IsFalse(portrait.transform.Find("HinataEyes").GetComponent<Image>().enabled);Assert.IsFalse(portrait.transform.Find("HinataMouth").GetComponent<Image>().enabled);LogAssert.NoUnexpectedReceived();
        }
        [Test] public void 月報の見積もりと月初値は保存でき旧記録には捏造しない()
        {
            var state=new OpsState(14,true);state.Act("listen");state.BeginIncident();var forecast=state.Estimate("scope");state.Resolve("scope");
            var copy=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(state));Assert.IsTrue(copy.Valid());CollectionAssert.AreEqual(state.Latest.metricsBefore,copy.Latest.metricsBefore);Assert.AreEqual(forecast.stopMax,copy.Latest.forecast.stopMax);Assert.IsTrue(copy.Latest.hasClosingState);Assert.AreEqual(copy.budget,copy.Latest.closingBudget);
            var legacy=new OpsState(14){monthStartMetrics=null};legacy.BeginIncident();legacy.Resolve("scope");Assert.IsNull(legacy.Latest.metricsBefore);Assert.AreEqual(0,OpsAnnualSummary.From(legacy).encountered.Count);
            var failed=new OpsOutcome{month=2,hasClosingState=true,closingBudget=-1,closingStability=50};Assert.AreEqual("運営終了",OpsAnnualSummary.MonthLabel(state,failed));
        }
    }
}
