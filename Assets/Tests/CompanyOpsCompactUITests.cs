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
            StringAssert.Contains(game.State.Current.news, Find<TextMeshProUGUI>("DialogBody").text);
            Click("EmployeeConsultation"); yield return null;
            StringAssert.Contains(game.State.StaffVoice, Find<TextMeshProUGUI>("DialogBody").text);
            CheckText(); Click("CloseDialog"); yield return null;
            game.ChooseAction("listen"); yield return null;
            Assert.AreEqual("Navigator", Find<RectTransform>("Feedback").parent.name);
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
            game.Resolve("scope"); yield return null;
            Assert.IsFalse(Object.FindObjectsByType<TextMeshProUGUI>().Any(t => t.name == "Causality"));
            string reason = game.State.Latest.explanation;
            CheckPointer("ReviewDetails"); Click("ReviewDetails"); yield return null;
            StringAssert.Contains(reason, Find<TextMeshProUGUI>("DialogBody").text);
            CheckText(); Click("CloseDialog"); yield return null;
            Capture("44-compact-review", 1280, 720);
            game.State.fatigue = 80; game.OpenTab(0); yield return null;
            var fatigue = Find<Button>("Stat_5").transform.Find("StatCategory").GetComponent<Image>();
            Assert.AreEqual("FF7E88", ColorUtility.ToHtmlStringRGB(fatigue.color));
            game.State.budget = -1; game.Next(); yield return null;
            Assert.AreEqual(OpsPhase.Ended, game.State.phase);
            Assert.AreEqual(game.State.totalLoss + " 万円", Find<TextMeshProUGUI>("AnnualLossValue").text);
            Assert.AreEqual(game.State.Rank.Substring(game.State.Rank.Length - 1), Find<TextMeshProUGUI>("CompanyRank").text);
            Assert.AreEqual("FF7E88", ColorUtility.ToHtmlStringRGB(Find<TextMeshProUGUI>("AnnualBudgetValue").color));
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
