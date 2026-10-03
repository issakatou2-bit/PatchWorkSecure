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
using Object = UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [Test] public void 復元連携は侵入予防を増やさず復旧方針で機能する()
        {
            var s = new OpsState(14);
            s.Upgrade(OpsCatalog.Index("backup")); s.Upgrade(OpsCatalog.Index("drill")); s.Upgrade(OpsCatalog.Index("runbook"));
            Assert.AreEqual(0, s.capacity); Assert.AreEqual(1, s.RestoreChain);
            var scope = s.Preview("scope"); var recover = s.Preview("recover");
            Assert.IsFalse(recover.benign);
            Assert.AreEqual(scope.prevention, recover.prevention);
            Assert.Less(recover.loss, scope.loss); Assert.Less(recover.downtime, scope.downtime);
            Assert.AreEqual("復元連携", recover.recoveryChain);
            s.BeginIncident(); s.Resolve("recover");
            Assert.AreEqual(recover.loss, s.Latest.loss); Assert.AreEqual(recover.downtime, s.Latest.downtime);
            Assert.IsTrue(s.Valid());
        }
        [Test] public void 混雑と詐欺と認証悪用にバックアップや復元訓練を適用しない()
        {
            foreach (int month in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 10 })
            foreach (string response in new[] { "contain", "scope", "recover" })
            {
                var a = new OpsState(14) { month = month }; var b = new OpsState(14) { month = month };
                b.levels[OpsCatalog.Index("backup")] = b.levels[OpsCatalog.Index("drill")] = 2;
                Assert.AreEqual(a.Preview(response).loss, b.Preview(response).loss, month + "/" + response);
                Assert.AreEqual(a.Preview(response).downtime, b.Preview(response).downtime, month + "/" + response);
                Assert.IsFalse(b.DataRecoveryApplies);
            }
        }
        [Test] public void 再開連携と復元連携は対象の月だけで成立する()
        {
            var s = new OpsState(14); s.levels[OpsCatalog.Index("automation")] = s.levels[OpsCatalog.Index("runbook")] = 1;
            for (int month = 0; month < 12; month++)
            {
                s.month = month; var r = s.Preview("recover");
                Assert.AreEqual(month == 3 || month == 4 ? "再開連携" : "", r.recoveryChain);
            }
            for (int seed = 0; seed < 100; seed++)
            {
                s = new OpsState(seed); s.levels[OpsCatalog.Index("backup")] = s.levels[OpsCatalog.Index("drill")] = 2;
                var r = s.Preview("recover");
                if (r.benign) { Assert.IsEmpty(r.recoveryChain); Assert.AreEqual(0, r.chainLossReduction + r.chainDowntimeReduction); }
                Assert.GreaterOrEqual(r.loss, 0); Assert.GreaterOrEqual(r.downtime, 0);
            }
        }
        [Test] public void 設備別の効果は一つ外した差分を記録し保存後も残る()
        {
            var s = new OpsState(14);
            s.levels[OpsCatalog.Index("backup")] = s.levels[OpsCatalog.Index("drill")] = 1;
            s.BeginIncident(); string before = JsonUtility.ToJson(s); s.Resolve("recover");
            foreach (var e in s.Latest.investmentEffects)
            {
                var absent = JsonUtility.FromJson<OpsState>(before); absent.levels[OpsCatalog.Index(e.projectId)] = 0;
                Assert.AreEqual(absent.Preview("recover").loss - s.Latest.loss, e.avoidedLoss);
                Assert.AreEqual(absent.Preview("recover").downtime - s.Latest.downtime, e.avoidedDowntime);
            }
            var copy = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s));
            Assert.IsTrue(copy.Valid()); Assert.AreEqual(2, copy.Latest.investmentEffects.Count);
            copy.Latest.investmentEffects = null; Assert.IsTrue(copy.Valid(), "以前の記録を読めなくしない");
            copy.Latest.chainLossReduction = -1; Assert.IsFalse(copy.Valid());
        }
        [Test] public void 整備しない追加予算は返却し保存復帰でも二重に返さない()
        {
            var s = new OpsState(14); s.Act("audit"); int before = s.budget;
            s.Act("proposal"); Assert.AreEqual(15, s.proposalGrant);
            s = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s)); Assert.IsTrue(s.Valid());
            s.BeginIncident(); s.Resolve("scope");
            Assert.AreEqual(before - s.Latest.loss - s.Latest.cost, s.budget);
            StringAssert.Contains("15万円を返却", s.Latest.promise);
            int settled = s.budget; Assert.IsFalse(s.Resolve("scope")); Assert.AreEqual(settled, s.budget);
            s.NextMonth(); Assert.AreEqual(0, s.proposalGrant);
            s = new OpsState(14); s.Act("listen"); s.Act("proposal", "people"); s.Upgrade(OpsCatalog.Index("education"));
            before = s.budget; s.BeginIncident(); s.Resolve("scope");
            Assert.AreEqual(before - s.Latest.loss - s.Latest.cost, s.budget); StringAssert.Contains("+5", s.Latest.promise);
        }
        [Test] public void 休暇の応援調整と通常の休息は同じ効果で重複しない()
        {
            var a = SituationState("absence"); var b = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(a));
            a.Act("prepare"); b.Act("rest");
            Assert.AreEqual(a.fatigue, b.fatigue); Assert.AreEqual(a.capacity, b.capacity);
            Assert.IsTrue(a.rested && b.rested); Assert.IsTrue(a.situationPrepared && b.situationPrepared);
            Assert.IsFalse(a.Act("rest")); Assert.IsFalse(b.Act("prepare"));
            a.BeginIncident(); b.BeginIncident(); a.Resolve("scope"); b.Resolve("scope");
            Assert.AreEqual(a.fatigue, b.fatigue); Assert.AreEqual(0, a.Latest.extraFatigue); Assert.IsTrue(a.Valid());
        }
        [Test] public void 社員の相談内容は教育と相談文化の成長で変わる()
        {
            var s = new OpsState(14) { month = 8, culture = 65 };
            Assert.AreEqual(s.Current.staff, s.StaffVoice);
            s.levels[OpsCatalog.Index("education")] = 1;
            Assert.AreNotEqual(s.Current.staff, s.StaffVoice); StringAssert.Contains("いつもの連絡先", s.StaffVoice);
            s.culture = 64; Assert.AreEqual(s.Current.staff, s.StaffVoice);
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator 成長計画から購入比較と連携結果を実画面で試遊する()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.5f);
            var game = Object.FindAnyObjectByType<OpsGame>(); game.StartYear(14); yield return null;
            CheckPointer("Goal0"); Click("Goal0"); yield return null;
            Capture("18-growth-plan"); CheckGrowthText(); CheckPointer("ChainProject_backup");
            Click("ChainProject_backup"); yield return null; Click("Compare_recover"); yield return null;
            StringAssert.Contains(game.State.Forecast("recover"), Find<TextMeshProUGUI>("InvestmentForecast").text);
            Capture("19-response-comparison"); CheckText(); CheckPointer("Buy_backup"); Click("Buy_backup"); yield return null;
            Click("Goal0"); yield return null; Click("ChainProject_drill"); yield return null; Click("Buy_drill"); yield return null;
            Click("Goal0"); yield return null;
            StringAssert.Contains("成立", Find<TextMeshProUGUI>("ChainStatusText").text); Capture("20-chain-ready", 1280, 720); CheckGrowthText();
            Click("CloseDialog"); yield return null;
            game.Buy(OpsCatalog.Index("runbook")); game.BeginIncident(); yield return null;
            Assert.IsNotNull(Find<RectTransform>("PreparedBadge_recover"));
            CheckPointer("Respond_recover"); Click("Respond_recover"); yield return WaitForResolution(game);
            CheckPointer("EffectDetails"); Click("EffectDetails"); yield return null;
            StringAssert.Contains("復元連携が機能", Find<TextMeshProUGUI>("EffectChain").text);
            Capture("21-investment-effects", 1280, 720); CheckGrowthText(); CheckPointer("CloseDialog");
            var scroll = Find<ScrollRect>("ProjectScroll"); Canvas.ForceUpdateCanvases();
            Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
            scroll.verticalNormalizedPosition = 0; yield return null; Capture("22-effects-scrolled");
            Click("CloseDialog"); yield return null; CheckPointer("NextMonth");
            game.StartYear(14); game.State.month = 8; SetEvent(game.State, "bec-invoice"); game.State.culture = 65; game.State.levels[OpsCatalog.Index("education")] = 1; game.OpenTab(0); yield return null;
            CheckPointer("ConsultationDetails"); Click("ConsultationDetails"); yield return null;
            Click("EmployeeConsultation"); yield return null;
            StringAssert.Contains("いつもの連絡先", Find<TextMeshProUGUI>("DialogBody").text); Capture("23-staff-growth"); CheckGrowthText();
            Assert.IsEmpty(glyphWarnings, string.Join("\n", glyphWarnings)); LogAssert.NoUnexpectedReceived();
        }
        private static void CheckGrowthText()
        {
            CheckText();
            foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>())
                if (t.name.StartsWith("Chain") || t.name.StartsWith("Effect") || t.name == "DialogBody" || t.name == "Staff" || t.name == "RecoveryReadiness")
                { t.ForceMeshUpdate(); Assert.IsFalse(t.isTextOverflowing, t.name + " / " + t.text); }
        }
    }
}
#endif
