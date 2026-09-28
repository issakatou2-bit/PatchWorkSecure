#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private readonly List<string> glyphWarnings = new List<string>();
        [SetUp] public void Setup() { OpsGame.TestMode = true; glyphWarnings.Clear(); Application.logMessageReceived += Observe; }
        [TearDown] public void Teardown() { OpsGame.TestMode = false; Application.logMessageReceived -= Observe; }
        private void Observe(string message, string stack, LogType type) { if (message.Contains("Unicode value")) glyphWarnings.Add(message); }

        [Test] public void 工数と前提条件と二重操作を守る()
        {
            var s = new OpsState(45); Assert.IsFalse(s.Act("proposal"));
            Assert.IsFalse(s.Upgrade(OpsCatalog.Index("drill")));
            Assert.IsFalse(s.Upgrade(-1)); Assert.IsFalse(s.Upgrade(50));
            Assert.IsTrue(s.Upgrade(OpsCatalog.Index("automation"))); Assert.AreEqual(2, s.capacity);
            Assert.IsTrue(s.Act("listen")); Assert.IsFalse(s.Act("listen"));
            s.Act("audit"); Assert.IsFalse(s.Act("rest"));
            s.BeginIncident(); Assert.IsFalse(s.Upgrade(0)); Assert.IsFalse(s.Act("map"));
            s.Resolve("scope"); int cash = s.budget; Assert.IsFalse(s.Resolve("scope")); Assert.AreEqual(cash, s.budget);
            s.NextMonth(); Assert.AreEqual(5, s.capacity);
        }
        [Test] public void バックアップは侵入を防がず復旧に効く()
        {
            var plain = new OpsState(14); var backup = new OpsState(14); backup.levels[OpsCatalog.Index("backup")] = 1;
            Assert.AreEqual(plain.Preview("scope").prevention, backup.Preview("scope").prevention);
            Assert.Less(backup.Preview("scope").loss, plain.Preview("scope").loss);
            plain.month = backup.month = 1;
            Assert.AreEqual(plain.Preview("scope").loss, backup.Preview("scope").loss);
        }
        [Test] public void 追加予算は提案後の整備で約束を果たす()
        {
            var s = new OpsState(2); s.Upgrade(OpsCatalog.Index("education")); s.Act("listen"); s.Act("proposal", "people");
            s.BeginIncident(); s.Resolve("scope"); StringAssert.Contains("未達", s.Latest.promise);
            s = new OpsState(2); s.Act("listen"); s.Act("proposal", "people"); s.Upgrade(OpsCatalog.Index("education"));
            s.BeginIncident(); s.Resolve("scope"); StringAssert.Contains("+5", s.Latest.promise);
        }
        [Test] public void 保存復帰で出来事を引き直さない()
        {
            var s = new OpsState(88); s.Act("audit"); s.BeginIncident();
            var copy = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s)); Assert.IsTrue(copy.Valid());
            foreach (string r in new[] { "scope", "recover", "contain" })
                Assert.AreEqual(JsonUtility.ToJson(s.Preview(r)), JsonUtility.ToJson(copy.Preview(r)));
            copy.levels = null; Assert.IsFalse(copy.Valid());
            copy = new OpsState(3) { month = 12 }; Assert.IsFalse(copy.Valid());
            copy = new OpsState(3) { phase = OpsPhase.Review }; Assert.IsFalse(copy.Valid());
        }
        [Test] public void 調査の予測に未確認の真値を使わない()
        {
            var a = new OpsState(12); var b = new OpsState(13);
            Assert.AreEqual(a.Forecast("scope"), b.Forecast("scope"));
            a.Act("audit");
            var copy = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(a));
            Assert.AreEqual(a.Forecast("scope"), copy.Forecast("scope"));
        }
        [Test] public void 運営終了後に行動も二重進行もできない()
        {
            var s = new OpsState(14); s.BeginIncident(); s.Resolve("contain"); s.budget = -1; s.NextMonth();
            Assert.AreEqual(OpsPhase.Ended, s.phase); Assert.IsFalse(s.IsClear);
            Assert.IsFalse(s.Act("listen")); Assert.IsFalse(s.NextMonth()); Assert.IsFalse(s.BeginIncident());
        }
        [Test] public void 実ファイルの保存復帰と破損時の保護()
        {
            string dir = Path.Combine(Application.dataPath, "../Artifacts/CompanyOps/SaveTests", Guid.NewGuid().ToString("N"));
            string path = Path.Combine(dir, "year.json");
            var s = new OpsState(72); s.Act("listen");
            Assert.IsTrue(OpsSaveStore.Write(path, s, out string warning), warning);
            s.BeginIncident(); Assert.IsTrue(OpsSaveStore.Write(path, s, out warning), warning);
            var loaded = OpsSaveStore.Read(path, out warning); Assert.IsNotNull(loaded, warning);
            Assert.AreEqual(JsonUtility.ToJson(s), JsonUtility.ToJson(loaded));
            Assert.IsTrue(File.Exists(path + ".bak"));
            s.Resolve("scope"); OpsSaveStore.Write(path, s, out warning);
            loaded = OpsSaveStore.Read(path, out warning); Assert.AreEqual(OpsPhase.Review, loaded.phase);
            Assert.AreEqual(s.Latest.loss, loaded.Latest.loss);
            File.WriteAllText(path, "{broken"); Assert.IsNull(OpsSaveStore.Read(path, out warning));
            Assert.IsNotEmpty(warning); Assert.AreEqual("{broken", File.ReadAllText(path));
            Assert.IsNotNull(OpsSaveStore.Read(path + ".bak", out warning));
        }
        [Test] public void 成長目標の報酬を重複取得できない()
        {
            var s = new OpsState(3); s.Upgrade(OpsCatalog.Index("backup")); s.Upgrade(OpsCatalog.Index("drill"));
            Assert.Contains("戻せることを確かめた", s.milestones);
            int trust = s.trust; s.Act("rest"); Assert.AreEqual(trust, s.trust); Assert.AreEqual(1, s.milestones.Count);
        }
        [Test] public void 購入比較は実際の導入と一致して元の進行を変更しない()
        {
            foreach (string id in new[] { "backup", "drill", "education", "mfa", "automation", "runbook" })
            {
                var s = new OpsState(14); s.levels[OpsCatalog.Index("backup")] = 1;
                string original = JsonUtility.ToJson(s);
                int index = OpsCatalog.Index(id);
                var preview = s.PreviewUpgrade(index);
                Assert.IsNotNull(preview);
                Assert.AreEqual(original, JsonUtility.ToJson(s), "比較で本番の状態が変わった: " + id);
                Assert.IsTrue(s.Upgrade(index));
                Assert.AreEqual(JsonUtility.ToJson(s), JsonUtility.ToJson(preview));
            }
            var blocked = new OpsState(14) { capacity = 0 };
            Assert.IsNull(blocked.PreviewUpgrade(0)); Assert.IsNull(blocked.PreviewUpgrade(-1));
        }
        [Test] public void 整備効果は同じ対応での被害差に一致し保存で残る()
        {
            var plain = new OpsState(14); var equipped = new OpsState(14);
            equipped.levels[OpsCatalog.Index("backup")] = 1;
            equipped.levels[OpsCatalog.Index("drill")] = 1;
            plain.BeginIncident(); equipped.BeginIncident();
            plain.Resolve("recover"); equipped.Resolve("recover");
            Assert.AreEqual(plain.Latest.loss - equipped.Latest.loss, equipped.Latest.avoidedLoss);
            Assert.AreEqual(plain.Latest.downtime - equipped.Latest.downtime, equipped.Latest.avoidedDowntime);
            Assert.Greater(equipped.Latest.avoidedLoss, 0); Assert.Greater(equipped.Latest.avoidedDowntime, 0);
            Assert.AreEqual(0, plain.Latest.avoidedLoss);
            var saved = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(equipped));
            Assert.IsTrue(saved.Valid()); Assert.IsTrue(saved.Latest.hasInvestmentComparison);
            Assert.AreEqual(equipped.Latest.avoidedLoss, saved.Latest.avoidedLoss);
        }
        [Test] public void 社内依頼は設備と現場のどちらでも達成でき重複報酬はない()
        {
            var equipment = new OpsState(14);
            equipment.levels[OpsCatalog.Index("backup")] = 1;
            equipment.levels[OpsCatalog.Index("drill")] = 1;
            Assert.IsTrue(equipment.MissionReady);
            int trust = equipment.trust;
            Assert.IsTrue(equipment.BeginIncident());
            Assert.AreEqual(trust + 3, equipment.trust);
            Assert.AreEqual(1, equipment.MissionCount);
            Assert.IsFalse(equipment.BeginIncident()); Assert.AreEqual(1, equipment.MissionCount);
            var field = new OpsState(14);
            field.Act("audit"); field.Act("map");
            Assert.IsTrue(field.MissionReady);
            field.BeginIncident(); Assert.AreEqual(1, field.MissionCount);
            var resumed = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(field));
            Assert.IsTrue(resumed.Valid()); Assert.AreEqual(1, resumed.MissionCount);
            var olderSave = new OpsState(14) { completedMissions = null };
            Assert.IsTrue(olderSave.Valid());
            olderSave.Act("audit"); olderSave.Act("map"); olderSave.BeginIncident();
            Assert.AreEqual(1, olderSave.MissionCount);
        }
        [UnityTest] public IEnumerator 実画面の操作から年間評価まで進める()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.8f);
            Capture("01-title");
            var game = Object.FindAnyObjectByType<OpsGame>(); Assert.IsNotNull(game);
            Assert.IsNotNull(Find<Image>("HomePortrait").sprite, "ひなたの暫定立ち絵が設定されていない");
            Find<Button>("NewYear").onClick.Invoke(); yield return null;
            // 年度の乱数を固定してスクリーンショットと年間検証を再現可能にする。
            game.StartYear(14); yield return null;
            Assert.AreEqual(OpsPhase.Planning, game.State.phase);
            Assert.IsNotNull(Find<Image>("NavigatorPortrait").sprite, "プレイ中にひなたが表示されていない");
            Assert.IsNotNull(Find<TextMeshProUGUI>("CaseTitle"));
            Capture("02-april"); CheckText();
            Click("Pin_backup"); yield return null;
            CheckPointer("OpenProjectFromOffice");
            Click("OpenProjectFromOffice"); yield return null;
            Assert.IsNotNull(Find<Button>("Details_backup"));
            game.OpenTab(0); yield return null;
            Click("Action_listen"); yield return null;
            Click("Tab1"); yield return null;
            Capture("03-improvements");
            CheckPointer("Proposal");
            Click("Proposal"); yield return null;
            Click("Propose_recover"); yield return null;
            Click("Details_backup"); yield return null;
            Capture("04-investment");
            CheckText();
            StringAssert.Contains("導入前 → 導入後", Find<TextMeshProUGUI>("InvestmentNumbers").text);
            Assert.IsNotNull(Find<Button>("Buy_backup"));
            CheckPointer("Buy_backup");
            Click("Buy_backup"); yield return null;
            Assert.AreEqual(1, game.State.Level("backup")); Assert.AreEqual(0, game.State.capacity);
            Click("AdvanceMonth"); yield return new WaitForSeconds(.2f);
            Capture("05-incident"); CheckText();
            CheckPointer("Respond_recover");
            Click("Respond_scope"); yield return WaitForResolution(game);
            Capture("06-review"); CheckText();
            Assert.IsTrue(game.State.Latest.hasInvestmentComparison);
            StringAssert.Contains("被害 −"+game.State.Latest.avoidedLoss, Find<TextMeshProUGUI>("ImpactSummary").text);
            Click("NextMonth"); yield return null;
            Capture("07-may-compact", 1280, 720);
            while (game.State.phase != OpsPhase.Ended)
            {
                Plan(game.State); game.OpenTab(0); yield return null;
                game.BeginIncident(); yield return null;
                game.Resolve(game.State.Current.kind == "outage" ? "recover" : "scope"); yield return null;
                game.Next(); yield return null;
            }
            Assert.IsTrue(game.State.IsClear, "一年を完走できない");
            Assert.AreEqual(12, game.State.history.Count);
            Capture("08-annual-report"); CheckText();
            Click("EndingHistory"); yield return null; Capture("09-history");
            StringAssert.Contains("整備効果", Find<TextMeshProUGUI>("History0").text);
            Assert.IsEmpty(glyphWarnings, string.Join("\n", glyphWarnings));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 社内依頼の進捗と結果を画面で確認できる()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.5f);
            var game = Object.FindAnyObjectByType<OpsGame>(); game.StartYear(14); yield return null;
            Capture("10-mission-start");
            Click("Action_audit"); yield return null;
            Click("ConsultationDetails"); yield return null;
            StringAssert.Contains("現場 1/2", Find<TextMeshProUGUI>("MissionProgress").text);
            Click("CloseDialog"); yield return null;
            Click("Action_map"); yield return null;
            Assert.IsTrue(game.State.MissionReady);
            Capture("11-mission-ready"); CheckText();
            game.BeginIncident(); yield return null;
            Assert.IsTrue(game.State.CurrentMissionCompleted);
            Capture("12-mission-complete");
            game.Resolve("scope"); yield return WaitForResolution(game);
            StringAssert.Contains("達成", Find<TextMeshProUGUI>("MissionResult").text);
            CheckText(); LogAssert.NoUnexpectedReceived();
        }
        private static void Plan(OpsState s)
        {
            if (s.fatigue > 40) s.Act("rest"); s.Act("listen");
            foreach (string key in new[] { "automation", "inventory", "drill", "education", "runbook", "mfa", "patch", "redundancy", "monitor", "segment" })
            {
                int i = OpsCatalog.Index(key); var p = OpsCatalog.Projects[i];
                if (s.levels[i] != 0 || s.UpgradeBlock(i) != "") continue;
                if (s.capacity >= s.WorkCost(i) + 1 && !s.proposed) s.Act("proposal", p.group);
                if (s.budget >= s.Cost(i) + 12) s.Upgrade(i);
            }
            s.Act("audit"); s.Act("rest"); s.Act("map");
        }
        private static void Click(string name)
        {
            NavigatePlanningControl(name);
            var b = Find<Button>(name); Assert.IsTrue(b.interactable, "押せないボタン: " + name);
            b.onClick.Invoke();
        }
        private static void CheckPointer(string name)
        {
            NavigatePlanningControl(name);
            Canvas.ForceUpdateCanvases();
            var button = Find<Button>(name); var rect = button.GetComponent<RectTransform>();
            var center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = center };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, "クリック位置にUIがない: " + name);
            Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(), "別のUIにクリックを遮られている: " + name);
        }
        private static T Find<T>(string name) where T : Component => Object.FindObjectsByType<T>().First(t => t.name == name);
        // 常設をやめた情報は、実際に表示されているメニュー／相談から辿る。
        // Findは純粋な検索のままにして、存在・配置の検証を隠さない。
        private static void NavigatePlanningControl(string name)
        {
            if (Object.FindObjectsByType<Button>().Any(b=>b.name==name)) return;
            var game=Object.FindAnyObjectByType<OpsGame>();
            if(new[]{"ToggleSound","VoiceToggle","DiagnosticVoiceVolume","PreviewVoice","PreviewSuccess","PreviewAlert","PreviewDamage","ViewHistory","OpenTeam","OpenTicket","Tab2","Goal1","OpenSituation","OpenGuide"}.Contains(name))
            {
                var advanced=Object.FindObjectsByType<Button>().FirstOrDefault(b=>b.name=="AdvancedSettings");
                if(advanced!=null){advanced.onClick.Invoke();Canvas.ForceUpdateCanvases();return;}
            }
            if(name=="MonthlyLesson")
            {
                var record=Object.FindObjectsByType<Button>().FirstOrDefault(b=>b.name=="ReviewDetails");
                if(record!=null){record.onClick.Invoke();Canvas.ForceUpdateCanvases();return;}
            }
            if(game==null||game.State==null||game.State.phase!=OpsPhase.Planning) return;
            if(name=="Tab0") {var close=Object.FindObjectsByType<Button>().FirstOrDefault(b=>b.name=="ClosePlanner");if(close!=null)close.onClick.Invoke();return;}
            if(name=="Tab1") {Find<Button>("OpenProjects").onClick.Invoke();return;}
            if(new[]{"OpenTeam","OpenTicket","Tab2","Goal1","OpenSituation","OpenGuide"}.Contains(name))
            {Find<Button>("Menu").onClick.Invoke();Find<Button>("AdvancedSettings").onClick.Invoke();}
            else if(name=="OpenEventBrief"||name=="EmployeeConsultation") Find<Button>("ConsultationDetails").onClick.Invoke();
            Canvas.ForceUpdateCanvases();
        }
        private static IEnumerator PreparePointer(string name)
        { NavigatePlanningControl(name); yield return null; CheckPointer(name); }
        private static IEnumerator WaitForResolution(OpsGame game)
        {
            // 実際のスキップボタンだけを使う。初回は省略せず最後まで再生する。
            float deadline=Time.realtimeSinceStartup+8;
            while(game.ResolutionActive && Time.realtimeSinceStartup<deadline)
            {
                if(game.CanSkipResolution) { Click("SkipResolution"); yield return null; }
                else yield return null;
            }
            Assert.IsFalse(game.ResolutionActive,"発動演出が完了しない");yield return new WaitForSecondsRealtime(.85f);
        }
        private static IEnumerator WaitForPowerCount(int expected)
        {
            float deadline=Time.realtimeSinceStartup+2;
            while(Find<TextMeshProUGUI>("PowerTotalValue").text!=expected.ToString()&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(expected.ToString(),Find<TextMeshProUGUI>("PowerTotalValue").text,"抑制力の数え上げが完了しない");
        }
        private static void CheckText()
        {
            foreach (var text in Object.FindObjectsByType<TextMeshProUGUI>())
            {
                text.ForceMeshUpdate();
                if (text.name.EndsWith("Value") || text.name.StartsWith("StatHint") || new[] { "CaseTitle", "IncidentTitle", "ReviewTitle", "EndingTitle", "NavigatorName", "InvestmentNumbers", "InvestmentForecast", "ImpactNumbers", "Causality", "MissionTitle", "MissionEquipment", "MissionField", "ScoreBreakdown", "SituationTitle", "SituationEffect", "SituationImpactText", "SituationAdvice", "StatDetailHint", "AudioStatus", "FeedbackDetail" }.Contains(text.name))
                    Assert.IsFalse(text.isTextOverflowing, "重要な文字が欠ける: " + text.name + " / " + text.text);
            }
        }
        private static void Capture(string name, int width = 1600, int height = 900, string canvasName = "CompanyOpsCanvas")
        {
            var canvas = Find<Canvas>(canvasName);
            var cameraObject = new GameObject("検証用カメラ", typeof(Camera)); var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.04f, .12f, .14f);
            var target = new RenderTexture(width, height, 24); var old = RenderTexture.active;
            camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); foreach (var t in canvas.GetComponentsInChildren<TextMeshProUGUI>()) t.ForceMeshUpdate();
            camera.Render(); RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            string dir = Path.Combine(Application.dataPath, "../Artifacts/CompanyOps"); Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), texture.EncodeToPNG());
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; camera.targetTexture = null; RenderTexture.active = old;
            target.Release(); Object.DestroyImmediate(texture); Object.DestroyImmediate(target); Object.DestroyImmediate(cameraObject);
        }
    }
}
#endif
