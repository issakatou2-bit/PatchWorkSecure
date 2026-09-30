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
        [Test] public void 加算内訳が予測と確定結果に一致し解決後の成長を遡及しない()
        {
            var s = new OpsState(14) { playerExperience = 9 };
            s.staffExperience[2] = 3;
            s.levels[OpsCatalog.Index("backup")] = s.levels[OpsCatalog.Index("drill")] = s.levels[OpsCatalog.Index("runbook")] = 1;
            Assert.IsTrue(s.AssignSupport("recover"));
            var power = s.ResponsePower("recover");
            Assert.AreEqual(5, power.basic); Assert.AreEqual(9, power.equipment);
            Assert.AreEqual(2, power.player); Assert.AreEqual(3, power.staff); Assert.AreEqual(19, power.Total);
            var p = s.Preview("recover"); int xp = s.playerExperience;
            for (int i = 0; i < 10; i++) { s.Forecast("recover"); s.ResponsePower("recover"); }
            Assert.AreEqual(xp, s.playerExperience);
            s.BeginIncident(); s.Resolve("recover");
            Assert.AreEqual(p.loss, s.Latest.loss); Assert.AreEqual(p.downtime, s.Latest.downtime);
            Assert.AreEqual(19, s.Latest.power.Total); Assert.AreEqual(3, s.PlayerLevel);
            Assert.AreEqual(2, s.Latest.growth.playerBefore); Assert.AreEqual(3, s.Latest.growth.playerAfter);
            var saved = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s)); Assert.IsTrue(saved.Valid());
            Assert.AreEqual(JsonUtility.ToJson(s.Latest), JsonUtility.ToJson(saved.Latest));
        }
        [Test] public void 支援は担当と方針に応じて加算し万能にはならない()
        {
            var s = new OpsState(14) { playerExperience = 4 };
            s.staffExperience = new[] { 3, 8, 8 };
            Assert.IsTrue(s.AssignSupport("recover"));
            Assert.AreEqual(6, s.ResponsePower("recover").staff); Assert.AreEqual(0, s.ResponsePower("scope").staff);
            s.month = 1; Assert.AreEqual(0, s.ResponsePower("recover").staff);
            s.supportOrder = "investigate"; Assert.AreEqual(4, s.ResponsePower("scope").staff);
            s.levels[OpsCatalog.Index("inventory")] = s.levels[OpsCatalog.Index("runbook")] = 1;
            Assert.AreEqual(6, s.ResponsePower("scope").staff); Assert.AreEqual(6, s.ResponsePower("contain").staff);
            Assert.AreEqual(0, s.ResponsePower("recover").staff);
            s.month = 2; Assert.AreEqual(4, s.ResponsePower("scope").staff, "経理をフォレンジック担当にしない");
            s.supportOrder = "auto"; s.month = 0; Assert.AreEqual("recover", s.EffectiveSupport);
            s.month = 1; Assert.AreEqual("investigate", s.EffectiveSupport);
        }
        [Test] public void 共同練習と日常委任は解放条件と月内上限を守る()
        {
            var s = new OpsState(14);
            Assert.IsFalse(s.AssignSupport("routine")); Assert.IsFalse(s.Practice(-1));
            s.Upgrade(OpsCatalog.Index("education")); s.Act("listen"); s.Practice(0); s.Upgrade(OpsCatalog.Index("runbook"));
            Assert.AreEqual(2, s.PlayerLevel); Assert.AreEqual(2, s.StaffLevel(0)); Assert.AreEqual(0, s.capacity);
            Assert.IsFalse(s.Practice(1)); Assert.IsTrue(s.AssignSupport("routine")); Assert.AreEqual(1, s.capacity);
            Assert.IsFalse(s.AssignSupport("routine")); Assert.IsFalse(s.AssignSupport("recover")); Assert.AreEqual(1, s.capacity);
            Assert.IsTrue(s.Act("audit")); s.BeginIncident(); s.Resolve("scope");
            Assert.AreEqual(0, s.Latest.power.staff); Assert.AreEqual(1, s.Latest.growth.staffXp[0]);
            int xp = s.playerExperience; Assert.IsFalse(s.Resolve("scope")); Assert.AreEqual(xp, s.playerExperience);
            s.NextMonth(); Assert.AreEqual("", s.supportOrder); Assert.IsFalse(s.practiced); Assert.AreEqual(4, s.capacity);
            Assert.IsTrue(s.AssignSupport("routine")); Assert.AreEqual(5, s.capacity); Assert.IsTrue(s.Valid());
        }
        [Test] public void 熟練社員は手順を共有し他の社員を育てる()
        {
            var s = new OpsState(14); s.staffExperience = new[] { 8, 2, 1 };
            s.levels[OpsCatalog.Index("runbook")] = 1;
            s.BeginIncident(); s.Resolve("scope");
            Assert.AreEqual(2, s.staffExperience[2]); Assert.AreEqual(1, s.Latest.growth.staffXp[2]);
            StringAssert.Contains("小川が森", s.Latest.growth.mentoring);
            Assert.AreEqual(0, s.Latest.growth.staffXp[1]);
            var noBook = new OpsState(14); noBook.staffExperience = new[] { 8, 2, 1 };
            noBook.BeginIncident(); noBook.Resolve("scope"); Assert.AreEqual("", noBook.Latest.growth.mentoring);
        }
        [Test] public void 購入プレビューは社員経験を共有せず取り消しで成長しない()
        {
            var s = new OpsState(14); string json = JsonUtility.ToJson(s);
            var preview = s.PreviewUpgrade(OpsCatalog.Index("education"));
            Assert.AreEqual(1, preview.staffExperience[0]); Assert.AreEqual(0, s.staffExperience[0]);
            Assert.AreEqual(json, JsonUtility.ToJson(s));
            s.Upgrade(OpsCatalog.Index("education")); Assert.AreEqual(JsonUtility.ToJson(preview), JsonUtility.ToJson(s));
        }
        [Test] public void 旧年度には育成と季節負荷を途中で追加せず不正な育成記録は拒否する()
        {
            var old = new OpsState { seed = 14 };
            Assert.IsTrue(old.Valid()); old.Act("audit"); old.BeginIncident(); old.Resolve("scope"); old.NextMonth();
            Assert.AreEqual(0, old.playerExperience); Assert.AreEqual(0, old.SeasonPressure); Assert.IsNull(old.Latest.growth);
            Assert.IsFalse(old.Practice(0)); Assert.IsFalse(old.AssignSupport("auto"));
            var s = new OpsState(14); s.staffExperience = null; Assert.IsFalse(s.Valid());
            s = new OpsState(14); s.staffExperience[0] = 9; Assert.IsFalse(s.Valid());
            s = new OpsState(14) { supportOrder = "invalid" }; Assert.IsFalse(s.Valid());
            s = new OpsState(14) { supportOrder = "routine" }; Assert.IsFalse(s.Valid());
            s = new OpsState(14); s.BeginIncident(); s.Resolve("scope"); s.Latest.growth.staffXp[0] = -1; Assert.IsFalse(s.Valid());
            s.Latest.growth.staffXp[0] = 0; s.Latest.power.staff = 999; Assert.IsFalse(s.Valid());
            s = new OpsState(14); s.levels = null; Assert.IsFalse(s.Valid());
        }
        [Test] public void 季節負荷は成長への追従ではなく年度共通で山場後に下がる()
        {
            var s = new OpsState(14); var skilled = new OpsState(14) { playerExperience = 28 };
            foreach (int peak in new[] { 2, 5, 8 })
            {
                s.month = skilled.month = peak;
                Assert.IsTrue(s.QuarterPeak); Assert.AreEqual(s.SeasonPressure, skilled.SeasonPressure);
                int pressure = s.SeasonPressure;
                s.month++; Assert.Less(s.SeasonPressure, pressure);
            }
            s.month = 11; Assert.AreEqual(20, s.SeasonPressure); Assert.IsTrue(s.QuarterPeak);
            int value = s.ResponsePower("scope").Total; s.playerExperience = 28;
            Assert.AreEqual(value + 8, s.ResponsePower("scope").Total);
        }
        [Test] public void 四半期報酬は一度だけで翌月工数は一か月で期限切れになる()
        {
            var s = new OpsState(14) { budget = 500 };
            for (int m = 0; m < 3; m++) { s.BeginIncident(); s.Resolve("scope"); if (m < 2) s.NextMonth(); }
            Assert.IsTrue(s.QuarterRewardPending);
            Assert.IsTrue(s.ClaimQuarterReward("capacity")); Assert.IsFalse(s.ClaimQuarterReward("budget"));
            var saved = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s)); Assert.IsTrue(saved.Valid());
            saved.NextMonth(); Assert.AreEqual(5, saved.capacity); Assert.AreEqual(1, saved.monthExtraCapacity);
            saved.BeginIncident(); saved.Resolve("scope"); saved.NextMonth(); Assert.AreEqual(4, saved.capacity);
            var noChoice = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s)); noChoice.quarterRewardClaimed = false; noChoice.nextMonthExtraCapacity = 0;
            int cash = noChoice.budget, grant = noChoice.MonthlyGrant, upkeep = noChoice.Upkeep;
            noChoice.NextMonth(); Assert.AreEqual(cash + grant - upkeep + OpsGrowthCatalog.QuarterBudget, noChoice.budget);
            Assert.IsTrue(noChoice.Valid());
        }
        [UnityTest] public IEnumerator 育成と社員支援と四半期報酬を操作して年度末まで確認する()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.5f);
            var game = Object.FindAnyObjectByType<OpsGame>(); game.StartYear(14); yield return null;
            // 白い会話面と無彩色の画面地を確認。角丸は新しい標準9-sliceを使う。
            var speechSurface = Find<UnityEngine.UI.Image>("Navigator");
            Assert.AreEqual(speechSurface.color.b, speechSurface.color.r);
            Assert.Greater(speechSurface.color.r, .95f);
            Assert.IsNotNull(speechSurface.sprite);
            Assert.AreEqual(UnityEngine.UI.Image.Type.Sliced, speechSurface.type);
            Assert.AreSame(game.PlanningArt.gradient,Find<UnityEngine.UI.Image>("PlanningBackground").sprite);
            yield return PreparePointer("OpenTeam"); Click("OpenTeam"); yield return null;
            Capture("24-team-new"); CheckTeamText();
            Assert.IsFalse(Find<Button>("Support_routine").interactable);
            Click("CloseDialog"); game.Buy(OpsCatalog.Index("education")); game.ChooseAction("listen"); yield return null;
            Click("OpenTeam"); yield return null; CheckPointer("Practice_0"); Click("Practice_0"); yield return null;
            Assert.AreEqual(2, game.State.StaffLevel(0)); Click("CloseDialog");
            game.Buy(OpsCatalog.Index("runbook")); yield return null; Click("OpenTeam"); yield return null;
            CheckPointer("Support_routine"); Click("Support_routine"); yield return new WaitForSeconds(2.9f);
            Assert.AreEqual(1, game.State.capacity); Capture("25-routine-unlocked", 1280, 720); CheckTeamText();
            Click("CloseDialog"); ChooseDelegatedWork(game,"audit"); game.BeginIncident(); game.Resolve(PublicTestResponse(game.State)); game.Next(); yield return null;
            Click("OpenTeam"); yield return null; Click("Practice_1"); yield return null; Click("Support_investigate"); yield return null;
            Click("CloseDialog"); game.Buy(OpsCatalog.Index("mfa")); ChooseDelegatedWork(game,"map"); game.BeginIncident(); yield return null;
            Capture("26-staff-assistance"); CheckText(); CheckPointer("Power_scope"); Click("Power_scope"); yield return WaitForPowerCount(game.State.ResponsePower("scope").Total);
            Assert.AreEqual(game.State.ResponsePower("scope").Total.ToString(), Find<TextMeshProUGUI>("PowerTotalValue").text);
            Capture("27-additive-power", 1280, 720); CheckTeamText(); Click("CloseDialog"); yield return null;
            var predicted = game.State.Preview("scope"); CheckPointer("Respond_scope"); Click("Respond_scope"); yield return WaitForResolution(game);
            Assert.AreEqual(predicted.loss, game.State.Latest.loss);
            Capture("28-growth-review"); CheckText(); Click("ReviewPower"); yield return new WaitForSeconds(.7f); CheckTeamText();
            Click("CloseDialog"); game.Next(); yield return null;
            ChooseDelegatedWork(game,"listen"); ChooseDelegatedWork(game,"map"); game.BeginIncident(); game.Resolve(PublicTestResponse(game.State)); yield return WaitForResolution(game);
            Assert.IsTrue(game.State.QuarterRewardPending); CheckPointer("NextMonth"); Click("NextMonth"); yield return null;
            Capture("29-quarter-reward"); CheckTeamText(); CheckPointer("Reward_capacity"); Click("Reward_capacity"); yield return null;
            Assert.AreEqual(3, game.State.month); Assert.AreEqual(1, game.State.monthExtraCapacity);
            Capture("30-after-peak"); CheckText();
            while (game.State.phase != OpsPhase.Ended)
            {
                PlanGrowthTestYear(game.State); game.OpenTab(0); game.BeginIncident(); yield return null;
                if (game.State.month == 11) { Capture("31-year-final", 1280, 720); CheckText(); }
                // 公開された予測だけで選ぶ。行動選択にはPreviewの真値を使わない。
                string choice = PublicGrowthResponse(game.State);
                game.Resolve(choice); yield return WaitForResolution(game);
                if (game.State.month == 11)
                {
                    Click("ReviewPower"); yield return new WaitForSeconds(.7f);
                    Capture("32-final-team-power"); CheckTeamText(); Click("CloseDialog");
                }
                game.Next(); yield return null;
            }
            Assert.IsTrue(game.State.IsClear); Assert.IsTrue(game.State.Valid());
            Assert.GreaterOrEqual(game.State.PlayerLevel, 4);
            Assert.IsEmpty(glyphWarnings, string.Join("\n", glyphWarnings)); LogAssert.NoUnexpectedReceived();
        }
        private static void PlanGrowthTestYear(OpsState s)
        {
            for(int i=0;i<OpsCatalog.BubblesPerMonth;i++)if(s.BubbleAvailable(i)&&s.BubbleKind(i)==6)s.PopBubble(i);
            if (s.SupportBlock("routine") == "" && s.month != 11) s.AssignSupport("routine");
            if (s.fatigue > 40) s.Act("rest");
            foreach (string id in new[] { "backup", "drill", "inventory", "patch", "automation", "monitor", "redundancy", "segment", "education" })
            {
                int i = OpsCatalog.Index(id);
                if (s.Level(id) == 0 && s.budget >= s.Cost(i) + 12) s.Upgrade(i);
            }
            s.Act("audit"); s.Act("map"); s.Act("listen");
            for (int i = 0; i < 3; i++) if (s.PracticeBlock(i) == "") s.Practice(i);
        }
        private static string PublicGrowthResponse(OpsState s) => PublicTestResponse(s);
        private static void CheckTeamText()
        {
            CheckText();
            foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>())
            {
                t.ForceMeshUpdate();
                string parent = t.transform.parent == null ? "" : t.transform.parent.name;
                if (t.name.StartsWith("Team") || t.name.StartsWith("Power") || t.name.StartsWith("Player") || t.name.StartsWith("Response") ||
                    parent.StartsWith("Practice_") || parent.StartsWith("Support_") || t.name == "DialogBody" || t.name == "QuarterRewardHelp")
                    Assert.IsFalse(t.isTextOverflowing, t.name + " / " + t.text);
            }
        }
    }
}
#endif
