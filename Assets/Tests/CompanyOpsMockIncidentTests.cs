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
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [Test] public void 備えの札の比較は公開情報だけで状態を変えない()
        {
            var state=new OpsState(14,true);SetEvent(state,"ransom-backup");state.levels[OpsCatalog.Index("backup")]=1;
            string before=JsonUtility.ToJson(state);
            foreach(string response in new[]{"contain","scope","recover"})
            {
                var estimate=state.EstimateWithoutProject(OpsCatalog.Index("backup"),response);
                var baseline=JsonUtility.FromJson<OpsState>(before);baseline.levels[OpsCatalog.Index("backup")]=0;
                Assert.AreEqual(baseline.Forecast(response),"被害 "+estimate.lossMin+"～"+estimate.lossMax+"万円 / 停止 "+estimate.stopMin+"～"+estimate.stopMax+"h");
            }
            Assert.AreEqual(before,JsonUtility.ToJson(state));
        }
        [Test] public void 未導入の仮定比較は確定値に一致し購入や報酬を発生させない()
        {
            foreach(var entry in OpsEventCatalog.Events)
            foreach(string response in new[]{"contain","scope","recover"})
            {
                var state=new OpsState(14,true);SetEvent(state,entry.id);state.BeginIncident();
                string before=JsonUtility.ToJson(state);int cash=state.budget;var expected=state.Preview(response);
                Assert.IsTrue(state.Resolve(response));Assert.AreEqual(cash-expected.cost-expected.loss,state.budget);
                foreach(var effect in state.Latest.potentialInvestmentEffects)
                {
                    var hypothetical=JsonUtility.FromJson<OpsState>(before);hypothetical.levels[OpsCatalog.Index(effect.projectId)]=1;
                    var comparison=hypothetical.Preview(response);
                    Assert.AreEqual(expected.loss-comparison.loss,effect.avoidedLoss);
                    Assert.AreEqual(expected.downtime-comparison.downtime,effect.avoidedDowntime);
                    Assert.IsTrue(effect.avoidedLoss>0||effect.avoidedDowntime>0);
                }
                Assert.IsTrue(state.levels.All(level=>level==0));Assert.IsTrue(state.Valid());
                var saved=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(state));Assert.IsTrue(saved.Valid());
                Assert.AreEqual(state.Latest.potentialInvestmentEffects.Count,saved.Latest.potentialInvestmentEffects.Count);
                saved.Latest.potentialInvestmentEffects=null;Assert.IsTrue(saved.Valid(),"旧記録の未保存項目を許容する");
            }
        }
        [UnityTest] public IEnumerator 事件モックの配置と公開帯と発動から確定結果を確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return null;yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);SetEvent(game.State,"ransom-backup");
            game.Buy(OpsCatalog.Index("backup"));game.BeginIncident();yield return null;
            CheckRect("OfficeStage",24,120,600,770);CheckRect("DecisionPanel",648,120,928,770);
            CheckRect("WarningTape",0,0,1600,14);CheckIncidentText();
            Assert.IsNotNull(Find<OpsIncidentGraphic>("WarningTape").GetComponent<CanvasRenderer>());
            Assert.IsNotNull(Find<RectTransform>("PreparedBadge_recover"));
            foreach(string id in new[]{"contain","scope","recover"})
            {
                var forecast=game.State.Estimate(id);var track=Find<RectTransform>("EstimateTrack_Loss_"+id);var band=Find<RectTransform>("EstimateBand_Loss_"+id);
                int scale=Math.Max(1,new[]{"contain","scope","recover"}.Max(r=>game.State.Estimate(r).lossMax));
                Assert.AreEqual(track.rect.width*forecast.lossMin/scale,band.anchoredPosition.x,.01f);
                Assert.AreEqual(track.rect.width*(forecast.lossMax-forecast.lossMin)/scale,band.rect.width,.01f);
                CheckPointer("Respond_"+id);
            }
            Capture("60-mock-incident-choose");var expected=game.State.Preview("recover");
            Click("Respond_recover");yield return null;
            Assert.IsTrue(game.ResolutionActive);Assert.IsFalse(game.CanSkipResolution);
            string resolved=JsonUtility.ToJson(game.State);game.Resolve("recover");Assert.AreEqual(resolved,JsonUtility.ToJson(game.State));
            game.SkipResolution();Assert.IsTrue(game.ResolutionActive,"初回は省略しない");
            float deadline=Time.realtimeSinceStartup+4;
            while(!Object.FindObjectsByType<Transform>().Any(t=>t.name=="ResolutionCutin")&&Time.realtimeSinceStartup<deadline)yield return null;
            var cutin=Find<RectTransform>("ResolutionCutin");Assert.IsTrue(cutin.gameObject.activeInHierarchy);
            CheckRect("CutinShineMask",0,0,1100,190);Assert.IsFalse(Find<Mask>("CutinShineMask").showMaskGraphic);
            while(Mathf.Abs(cutin.anchoredPosition.x-380)>1&&Time.realtimeSinceStartup<deadline)yield return null;
            Capture("61-mock-incident-resolve");
            Assert.AreEqual(expected.loss.ToString(),Find<TextMeshProUGUI>("ResolvedValue_Loss").text);
            Assert.AreEqual(expected.downtime.ToString(),Find<TextMeshProUGUI>("ResolvedValue_Stop").text);
            StringAssert.Contains((game.State.Latest.loss+game.State.Latest.avoidedLoss).ToString(),Find<TextMeshProUGUI>("ResolutionBaselineValue").text);
            yield return WaitForResolution(game);Assert.AreEqual(resolved,JsonUtility.ToJson(game.State));
            CheckPointer("NextMonth");Capture("62-mock-incident-review");
            game.Next();game.BeginIncident();game.Resolve("scope");yield return null;
            Assert.IsTrue(game.CanSkipResolution);string second=JsonUtility.ToJson(game.State);
            Click("SkipResolution");yield return null;Assert.IsFalse(game.ResolutionActive);Assert.AreEqual(second,JsonUtility.ToJson(game.State));
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 十二か月の季節表示は操作と状態を変えず省演出に従う()
        {
            SceneManager.LoadScene("CompanyYear");yield return null;yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);
            for(int month=0;month<12;month++)
            {
                game.State.month=month;string before=JsonUtility.ToJson(game.State);game.OpenTab(0);yield return null;
                Assert.IsTrue(Object.FindObjectsByType<RectTransform>().Any(r=>r.name.StartsWith("SeasonLayer_")));
                Assert.AreEqual(before,JsonUtility.ToJson(game.State));CheckPointer("Action_listen");
                if(month==9)CheckRect("SeasonOrnament",513,380.16f,44,54);
                if(month==2)yield return new WaitForSecondsRealtime(.6f);
                if(month==2||month==9)Capture(month==2?"64-season-rain":"65-season-newyear");
            }
            game.State.month=2;game.OpenTab(0);yield return null;
            Click("Menu");if(!game.ReducedMotion)Click("ReduceMotion");Click("CloseDialog");yield return null;
            var rain=Find<Image>("SeasonParticle0");yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(0,rain.color.a);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 動きを減らしても発動内容が残り全事件の文字が切れない()
        {
            SceneManager.LoadScene("CompanyYear");yield return null;yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);
            Click("Menu");yield return null;if(!game.ReducedMotion)Click("ReduceMotion");Click("CloseDialog");yield return null;
            foreach(var entry in OpsEventCatalog.Events)
            {
                game.StartYear(14);SetEvent(game.State,entry.id);game.BeginIncident();yield return null;
                CheckIncidentText();CheckPointer("Respond_scope");
            }
            SetEvent(game.State,"ransom-backup");game.State.levels[OpsCatalog.Index("backup")]=1;game.OpenTab(0);yield return null;
            var tape=Find<OpsIncidentGraphic>("WarningTape");var emergency=Find<RectTransform>("EmergencyBadge");var origin=emergency.anchoredPosition;
            yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(0,tape.Offset);Assert.AreEqual(origin,emergency.anchoredPosition);
            game.Resolve("recover");yield return new WaitForSecondsRealtime(1.35f);
            Assert.IsTrue(game.ResolutionActive);Assert.IsNotEmpty(Find<TextMeshProUGUI>("StepName").text);
            Capture("63-mock-incident-reduced-motion");yield return WaitForResolution(game);
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
