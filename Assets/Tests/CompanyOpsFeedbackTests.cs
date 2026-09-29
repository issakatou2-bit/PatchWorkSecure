#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
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
        private static OpsState SituationState(string id)
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var s = new OpsState(seed);
                if (s.SituationAt(1).id != id) continue;
                s.BeginIncident(); s.Resolve("scope"); s.NextMonth(); return s;
            }
            throw new Exception("事情を再現するシードが見つかりません: " + id);
        }
        [Test] public void 社内事情は保存復帰で変わらず四種類が一巡する()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var s = new OpsState(seed);
                Assert.AreEqual("normal", s.Situation.id);
                Assert.AreEqual(4, Enumerable.Range(1, 4).Select(m => s.SituationAt(m).id).Distinct().Count());
                var copy = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s));
                for (int m = 0; m < 12; m++) Assert.AreEqual(s.SituationAt(m).id, copy.SituationAt(m).id);
            }
        }
        [Test] public void 納期集中は停止損失へ反映され調整で防げる()
        {
            var s = SituationState("deadline");
            var before = s.Preview("contain"); Assert.AreEqual(6, before.businessLoss);
            var legacy = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s)); legacy.situationRules = 0;
            Assert.AreEqual(legacy.Preview("contain").loss + 6, before.loss);
            Assert.AreNotEqual(legacy.Forecast("contain"), s.Forecast("contain"));
            int capacity = s.capacity; Assert.IsTrue(s.Act("prepare")); Assert.AreEqual(capacity - 1, s.capacity);
            Assert.IsFalse(s.Act("prepare")); Assert.AreEqual(0, s.Preview("contain").businessLoss);
            s.BeginIncident(); Assert.IsFalse(s.Act("prepare")); s.Resolve("contain");
            Assert.IsTrue(s.Latest.situationPrepared); Assert.AreEqual(0, s.Latest.businessLoss);
            var copy = JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s)); Assert.IsTrue(copy.Valid());
            int cash = copy.budget; Assert.IsFalse(copy.Resolve("contain")); Assert.AreEqual(cash, copy.budget);
            copy.NextMonth(); Assert.IsFalse(copy.situationPrepared); Assert.IsTrue(copy.Valid());
        }
        [Test] public void 少人数対応の疲労は手順と調整で軽減できる()
        {
            var s = SituationState("absence"); Assert.AreEqual(8, s.SituationFatigue);
            s.levels[OpsCatalog.Index("runbook")] = 1; Assert.AreEqual(4, s.SituationFatigue);
            s.levels[OpsCatalog.Index("runbook")] = 2; Assert.AreEqual(0, s.SituationFatigue);
            Assert.IsFalse(s.Act("prepare"));
            s.levels[OpsCatalog.Index("runbook")] = 0;
            int fatigue = s.fatigue; s.BeginIncident(); var p = s.Preview("scope"); s.Resolve("scope");
            Assert.AreEqual(Math.Min(100, fatigue + 5 + p.downtime / 3 + 8), s.fatigue);
            Assert.AreEqual(8, s.Latest.extraFatigue);
            s = SituationState("absence"); Assert.IsTrue(s.Act("prepare")); Assert.AreEqual(0, s.SituationFatigue);
        }
        [Test] public void 導入支援と合同メンテナンスは実際の支払いと比較に一致する()
        {
            foreach (string id in new[] { "support", "maintenance" })
            {
                var s = SituationState(id);
                int i = OpsCatalog.Index(id == "support" ? "mfa" : "backup");
                Assert.AreEqual(id == "support" ? s.BaseCost(i) - 4 : s.BaseCost(i), s.Cost(i));
                Assert.AreEqual(id == "maintenance" ? 1 : OpsCatalog.Projects[i].time, s.WorkCost(i));
                Assert.AreEqual(OpsCatalog.Projects[OpsCatalog.Index("education")].cost, s.Cost(OpsCatalog.Index("education")));
                int upkeep = s.Upkeep;
                var preview = s.PreviewUpgrade(i); Assert.IsNotNull(preview);
                Assert.IsTrue(s.Upgrade(i)); Assert.AreEqual(JsonUtility.ToJson(preview), JsonUtility.ToJson(s));
                Assert.AreEqual(upkeep + OpsCatalog.Projects[i].upkeep, s.Upkeep);
                Assert.GreaterOrEqual(s.WorkCost(OpsCatalog.Index("drill")), 1);
                Assert.IsFalse(s.Act("prepare"));
            }
        }
        [Test] public void 新項目のない旧セーブには途中から追加ルールを適用しない()
        {
            string json = JsonUtility.ToJson(new OpsState(32));
            json = json.Replace("\"situationRules\":1,", "").Replace("\"situationPrepared\":false,", "");
            var s = JsonUtility.FromJson<OpsState>(json); Assert.AreEqual(0, s.situationRules); Assert.IsTrue(s.Valid());
            s.BeginIncident(); s.Resolve("scope"); s.NextMonth();
            Assert.AreEqual("normal", s.Situation.id); Assert.IsFalse(s.Act("prepare")); Assert.IsTrue(s.Valid());
            s.situationRules = 2; Assert.IsFalse(s.Valid());
        }
        [Test] public void 用途別の効果音は無音でもクリップ過大でもなく別の波形になる()
        {
            var fingerprints = new System.Collections.Generic.HashSet<string>();
            foreach (OpsCue cue in Enum.GetValues(typeof(OpsCue)))
            {
                var data = OpsSoundDesign.Samples(cue);
                Assert.Greater(data.Length, 1000); Assert.Less(data.Length, OpsSoundDesign.SampleRate);
                Assert.IsFalse(data.Any(v => float.IsNaN(v) || float.IsInfinity(v)));
                float peak = data.Max(v => Mathf.Abs(v)); Assert.Greater(peak, .01f); Assert.Less(peak, .6f);
                Assert.AreEqual(0, data[0]); Assert.Less(Mathf.Abs(data[data.Length - 1]), .0001f);
                fingerprints.Add(data.Length + ":" + data[511].ToString("R") + ":" + data[1100].ToString("R"));
            }
            Assert.AreEqual(Enum.GetValues(typeof(OpsCue)).Length, fingerprints.Count);
        }
        [UnityTest] public IEnumerator ステータス詳細と月次事情と操作演出を実画面で検証する()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.5f);
            var game = Object.FindAnyObjectByType<OpsGame>(); game.StartYear(14); yield return null;
            CheckPointer("Stat_5"); Click("Stat_5"); yield return null;
            StringAssert.Contains("低いほど良い", Find<TextMeshProUGUI>("DialogBody").text);
            Find<TextMeshProUGUI>("DialogBody").ForceMeshUpdate(); Assert.IsFalse(Find<TextMeshProUGUI>("DialogBody").isTextOverflowing);
            Capture("13-status-detail"); CheckText(); Click("CloseDialog"); yield return null;
            var action = Find<Button>("Action_listen");
            var actionOrigin=action.GetComponent<RectTransform>().anchoredPosition;
            Assert.IsNotNull(action.GetComponent<OpsButtonFeedback>());
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(action.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSeconds(.13f);Assert.AreEqual(actionOrigin.y+4,action.GetComponent<RectTransform>().anchoredPosition.y,.01f);
            ExecuteEvents.Execute(action.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(.12f); Assert.AreEqual(actionOrigin.y,action.GetComponent<RectTransform>().anchoredPosition.y,.01f,"ホバー位置から4px押し込む");
            ExecuteEvents.Execute(action.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(action.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(null);
            yield return new WaitForSeconds(.15f); Assert.AreEqual(actionOrigin,action.GetComponent<RectTransform>().anchoredPosition);
            Click("Action_listen"); yield return null; Assert.AreEqual(OpsCue.Action, game.LastCue);
            StringAssert.Contains("+7", Find<TextMeshProUGUI>("StatChangeAmount3").text);
            CheckPointer("Action_audit"); Capture("14-action-feedback"); CheckText();
            Click("Menu"); yield return null; Click("ReduceMotion"); yield return null;
            Assert.IsTrue(game.ReducedMotion); Capture("15-feedback-settings"); CheckText();
            Click("CloseDialog"); yield return null;
            action = Find<Button>("Action_audit");
            ExecuteEvents.Execute(action.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(.1f); Assert.AreEqual(Vector3.one, action.transform.localScale);
            game.ChooseAction("audit"); game.BeginIncident(); yield return null;
            CheckPointer("Respond_scope"); game.Resolve("scope"); game.Next(); yield return null;
            Click("OpenSituation"); yield return null; Capture("16-month-situation"); CheckText();
            Click("CloseDialog"); yield return null;
            game.State.fatigue = 80; game.State.budget = 5; game.State.stability = 20; game.OpenTab(0); yield return null;
            Capture("17-danger-status", 1280, 720); CheckText();
            StringAssert.Contains("要休息", Find<TextMeshProUGUI>("StatHint5").text);
            Click("Stat_0"); yield return null;
            StringAssert.Contains("予算", Find<TextMeshProUGUI>("DialogHeading").text);
            Assert.IsEmpty(glyphWarnings, string.Join("\n", glyphWarnings)); LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
