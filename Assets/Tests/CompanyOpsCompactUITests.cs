#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [Category("Capture")]
        [UnityTest] public IEnumerator 補足を詳細へ移して通知と決定操作を重ねない()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.5f);
            var game = Object.FindAnyObjectByType<OpsGame>(); game.StartYear(14); yield return null;
            Assert.IsFalse(Object.FindObjectsByType<TextMeshProUGUI>().Any(t => t.name == "Staff" || t.name == "NewsBody"));
            StringAssert.Contains("山場", Find<TextMeshProUGUI>("PeakLegend").text);
            foreach (string goalName in new[] { "Goal0", "Goal2" })
            {
                var label = Find<Button>(goalName).GetComponentInChildren<TextMeshProUGUI>();
                label.ForceMeshUpdate(); Assert.IsFalse(label.isTextOverflowing, goalName);
            }
            Assert.AreEqual("2EC4A0", ColorUtility.ToHtmlStringRGB(Find<TextMeshProUGUI>("StatHint5").color));
            CheckPointer("ConsultationDetails"); Click("ConsultationDetails"); yield return null;
            StringAssert.Contains(game.State.Current.boss, Find<TextMeshProUGUI>("MissionBoss").text);
            Click("EmployeeConsultation"); yield return null;
            StringAssert.Contains(game.State.StaffVoice, Find<TextMeshProUGUI>("DialogBody").text);
            CheckText(); Click("CloseDialog"); yield return null;
            game.ChooseAction("listen"); yield return null;
            var feedback=Find<RectTransform>("Navigator").GetComponentsInChildren<RectTransform>(true).Single(r=>r.name=="Feedback");
            Assert.AreEqual("Navigator",feedback.parent.name);Assert.IsFalse(feedback.gameObject.activeSelf,"字幕に旧通知を重ねない");
            Assert.AreEqual(OpsGame.SpeechLines(game.LastReactionCaption),Find<TextMeshProUGUI>("NavigatorSpeech").text);
            var change = Find<RectTransform>("StatChangeEffect3");
            Assert.Less(-change.anchoredPosition.y + change.rect.height, 429, "差分が相談文化の行からはみ出す");
            Capture("42-compact-planning", 1280, 720); CheckText();
            yield return new WaitForSecondsRealtime(.8f);
            Assert.IsFalse(Object.FindObjectsByType<RectTransform>().Any(r=>r.name=="StatChangeEffect3"));
            game.BeginIncident(); yield return null;
            foreach (string id in new[] { "contain", "scope", "recover" })
            {
                var decide = Find<Button>("Respond_" + id);
                Assert.AreEqual("ResponseCard_" + id, decide.transform.parent.name);
                Assert.GreaterOrEqual(decide.GetComponent<RectTransform>().rect.height, 44);
                CheckPointer("Respond_" + id);
            }
            Capture("43-compact-incident", 1280, 720); CheckText();
            game.Resolve("scope"); yield return WaitForResolution(game);
            Assert.IsFalse(Object.FindObjectsByType<TextMeshProUGUI>().Any(t => t.name == "Causality"));
            string reason = game.State.Latest.explanation;
            CheckPointer("ReviewDetails"); Click("ReviewDetails"); yield return null;
            StringAssert.Contains(reason, Find<TextMeshProUGUI>("DialogBody").text);
            CheckText(); Click("CloseDialog"); yield return null;
            Capture("44-compact-review", 1280, 720);
            game.State.fatigue = 80; game.OpenTab(0); yield return null;
            Assert.AreEqual(OpsPhase.Review,game.State.phase);
            game.State.budget = -1; game.Next(); yield return null;
            Assert.AreEqual(OpsPhase.Ended, game.State.phase);
            Assert.AreEqual("累計被害 "+game.State.totalLoss + " 万円", Find<TextMeshProUGUI>("AnnualLossValue").text);
            Assert.AreEqual(game.State.RankCode, Find<TextMeshProUGUI>("CompanyRank").text);
            Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t=>t.name=="AnnualConfetti0"),"失敗時は年度クリアの紙吹雪を出さない");
            Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t => t.name.StartsWith("AchievementSpark")));
            Capture("45-compact-annual", 1280, 720); CheckText();
            CheckPointer("AnnualDetails"); Click("AnnualDetails"); yield return null;
            StringAssert.Contains("採点", Find<TextMeshProUGUI>("DialogBody").text);
            CheckText();
            Assert.IsEmpty(glyphWarnings, string.Join("\n", glyphWarnings)); LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
