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
using Object = UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [Test] public void 見積もりの構造化は既存の予測と一致し未調査の真相を漏らさない()
        {
            var s = new OpsState(14) { month = 0, situationRules = 0 };
            foreach (string response in new[] { "contain", "scope", "recover" })
            {
                var a = s.Estimate(response);
                Assert.AreEqual("被害 " + a.lossMin + "～" + a.lossMax + "万円 / 停止 " + a.stopMin + "～" + a.stopMax + "h", s.Forecast(response));
                Assert.AreEqual(response == "contain" ? OpsCatalog.ContainCost : response == "scope" ? OpsCatalog.ScopeCost+s.Blindness*OpsCatalog.ScopeCostPerBlind : OpsCatalog.RecoverCost, a.cost);
                Assert.GreaterOrEqual(a.lossMin, 0); Assert.GreaterOrEqual(a.lossMax, a.lossMin);
                Assert.GreaterOrEqual(a.stopMin, 0); Assert.GreaterOrEqual(a.stopMax, a.stopMin);
                // 公開条件を固定して乱数の真相だけを変更。未調査の幅は同じ。
                for (int seed = 1; seed <= 30; seed++)
                {
                    s.seed = seed; var b = s.Estimate(response);
                    Assert.AreEqual(a.lossMin, b.lossMin); Assert.AreEqual(a.lossMax, b.lossMax);
                    Assert.AreEqual(a.stopMin, b.stopMin); Assert.AreEqual(a.stopMax, b.stopMax);
                }
            }
            Assert.Throws<ArgumentException>(() => s.Estimate("unknown"));
        }

        [UnityTest] public IEnumerator 対応比較は三案の数値とクリックと詳細復帰を保つ()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.5f);
            var game = Object.FindAnyObjectByType<OpsGame>(); game.StartYear(14); game.BeginIncident(); yield return null;
            Assert.IsFalse(Object.FindObjectsByType<UnityEngine.UI.Image>().Any(i => i.name == "CompanyGrowth"), "成長目標が対応中も常設されている");
            foreach (string response in new[] { "contain", "scope", "recover" })
            {
                var e = game.State.Estimate(response);
                Assert.AreEqual(e.cost.ToString(), Find<TextMeshProUGUI>("ResponseCost_" + response).text);
                Assert.AreEqual("万円", Find<TextMeshProUGUI>("EstimateUnit_Loss_" + response).text);
                Assert.AreEqual("時間", Find<TextMeshProUGUI>("EstimateUnit_Stop_" + response).text);
                CheckPointer("Respond_" + response); CheckPointer("Power_" + response);
            }
            Assert.IsNotNull(Find<OpsIncidentGraphic>("WarningIcon"));
            Capture("35-incident-visual", 1280, 720); CheckIncidentText();
            string before = JsonUtility.ToJson(game.State);
            Click("IncidentEvidence"); yield return null; CheckText(); CheckPointer("IncidentKnowledge");
            Click("CloseDialog"); yield return null;
            Click("Power_scope"); yield return WaitForPowerCount(game.State.ResponsePower("scope").Total);
            Assert.AreEqual(game.State.ResponsePower("scope").Total.ToString(), Find<TextMeshProUGUI>("PowerTotalValue").text);
            Click("CloseDialog"); yield return null; Assert.AreEqual(before, JsonUtility.ToJson(game.State));
            Capture("36-incident-1080", 1920, 1080); CheckIncidentText();
            Click("Respond_scope"); yield return null; Assert.AreEqual(OpsPhase.Review, game.State.phase);
            Assert.IsEmpty(glyphWarnings); LogAssert.NoUnexpectedReceived();
        }

        private static void CheckIncidentText()
        {
            CheckText();
            foreach (var text in Object.FindObjectsByType<TextMeshProUGUI>())
                if (text.name.StartsWith("Estimate") || text.name.StartsWith("Response") || text.name.StartsWith("PreparedText_") || text.name.StartsWith("Working") ||
                    new[] { "IncidentSymptom", "RecoveryReadiness", "NoTimer", "NavigatorSpeech", "SupportStatus" }.Contains(text.name))
                {
                    text.ForceMeshUpdate(); Assert.IsFalse(text.isTextOverflowing, text.name + " / " + text.text);
                }
        }
    }
}
#endif
