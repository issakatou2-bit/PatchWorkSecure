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
        [UnityTest] public IEnumerator 事件の設備名と一覧と横並びの単位は公開見積もりに一致する()
        {
            SceneManager.LoadScene("CompanyYear");yield return null;yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);
            for(int j=0;j<game.State.levels.Length;j++)game.State.levels[j]=1;
            foreach(var entry in OpsEventCatalog.Events)
            {
                game.StartYear(14);for(int j=0;j<game.State.levels.Length;j++)game.State.levels[j]=1;
                SetEvent(game.State,entry.id);game.BeginIncident();yield return null;
                string before=JsonUtility.ToJson(game.State);
                foreach(string response in new[]{"contain","scope","recover"})
                {
                    var actual=game.State.Estimate(response);
                    var working=Enumerable.Range(0,game.State.levels.Length).Where(j=>
                    {
                        var absent=game.State.EstimateWithoutProject(j,response);
                        return absent.lossMin>actual.lossMin||absent.lossMax>actual.lossMax||absent.stopMin>actual.stopMin||absent.stopMax>actual.stopMax;
                    }).ToArray();
                    var badge=Object.FindObjectsByType<TextMeshProUGUI>().FirstOrDefault(t=>t.name=="PreparedText_"+response);
                    if(working.Length==0){Assert.IsNull(badge);Assert.IsNotNull(Find<TextMeshProUGUI>("WorkingNone_"+response));}
                    else
                    {
                        Assert.IsNotNull(badge);Assert.IsTrue(working.Any(j=>badge.text==OpsCatalog.Projects[j].name+"が効く"));
                        foreach(int j in working.Take(working.Length>6?5:6))CheckPointer("Working_"+response+"_"+OpsCatalog.Projects[j].id);
                        if(working.Length>6)CheckPointer("WorkingMore_"+response);
                    }
                    string[] numbers={"ResponseCost_"+response,"EstimateValue_Stop_"+response,"EstimateValue_Loss_"+response};
                    string[] units={"CostUnit_"+response,"EstimateUnit_Stop_"+response,"EstimateUnit_Loss_"+response};
                    for(int j=0;j<3;j++)
                    {
                        var number=Find<RectTransform>(numbers[j]);var unit=Find<RectTransform>(units[j]);
                        Assert.AreEqual(number.anchoredPosition.y,unit.anchoredPosition.y,.01f);
                        Assert.AreEqual(number.rect.height,unit.rect.height,.01f);
                        Assert.LessOrEqual(number.anchoredPosition.x+number.rect.width,unit.anchoredPosition.x);
                    }
                    CheckPointer("Respond_"+response);
                }
                CheckIncidentText();Assert.AreEqual(before,JsonUtility.ToJson(game.State));
            }
            game.StartYear(14);SetEvent(game.State,"ransom-backup");
            foreach(string key in new[]{"backup","drill","runbook","monitor","segment"})game.State.levels[OpsCatalog.Index(key)]=1;
            game.BeginIncident();yield return new WaitForSecondsRealtime(.4f);Capture("84-incident-working-equipment");
            game.StartYear(14);SetEvent(game.State,"ransom-backup");game.State.levels[OpsCatalog.Index("backup")]=1;game.BeginIncident();yield return new WaitForSecondsRealtime(.4f);
            Capture("80-incident-finish-choose");Capture("81-incident-finish-choose-720",1280,720);
            Click("Working_recover_backup");yield return new WaitForSecondsRealtime(.5f);
            var body=Find<TextMeshProUGUI>("DialogBody");body.ForceMeshUpdate();Assert.IsFalse(body.isTextOverflowing);CheckPointer("CloseDialog");
            Assert.AreEqual(15,body.fontSizeMax,.01f);
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 発動の最後の未導入設備は仮定と分かり設定ボタンが出ない()
        {
            SceneManager.LoadScene("CompanyYear");yield return null;yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);SetEvent(game.State,"ransom-backup");
            game.State.levels[OpsCatalog.Index("backup")]=1;game.BeginIncident();game.Resolve("recover");yield return null;
            var missing=game.State.Latest.potentialInvestmentEffects.OrderByDescending(e=>e.avoidedLoss).ThenByDescending(e=>e.avoidedDowntime).First();
            string saved=JsonUtility.ToJson(game.State);float deadline=Time.realtimeSinceStartup+7;
            OpsIncidentGraphic frame=null;
            while(game.ResolutionActive&&Time.realtimeSinceStartup<deadline)
            {
                Assert.IsFalse(Object.FindObjectsByType<Button>().Any(b=>b.name=="Menu"||b.name=="ResolutionMenu"));
                frame=Object.FindObjectsByType<OpsIncidentGraphic>().FirstOrDefault(g=>g.name=="MissingDashedFrame");
                if(frame!=null)break;yield return null;
            }
            Assert.IsNotNull(frame);Assert.AreEqual("dashed",frame.Kind);Assert.GreaterOrEqual(frame.color.a,.8f);Assert.IsFalse(frame.raycastTarget);
            var row=frame.transform.parent;StringAssert.Contains(OpsCatalog.Projects[OpsCatalog.Index(missing.projectId)].name,row.Find("StepName").GetComponent<TextMeshProUGUI>().text);
            StringAssert.StartsWith("あれば、",row.Find("StepEffect").GetComponent<TextMeshProUGUI>().text);
            Assert.AreEqual("?",row.Find("StepNumber/StepIndex").GetComponent<TextMeshProUGUI>().text);
            yield return new WaitForSecondsRealtime(.1f);Capture("82-incident-finish-missing");
            yield return WaitForResolution(game);Assert.AreEqual(saved,JsonUtility.ToJson(game.State));CheckPointer("Menu");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 六月の雨筋と地面の波紋は見えて省演出で止まり操作を遮らない()
        {
            SceneManager.LoadScene("CompanyYear");yield return null;yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);game.State.month=2;game.OpenTab(0);
            string saved=JsonUtility.ToJson(game.State);yield return new WaitForSecondsRealtime(.6f);
            var layer=Find<RectTransform>("SeasonLayer_雨");
            Assert.AreEqual(48,layer.GetComponentsInChildren<OpsPlanningMotion>().Count(m=>m.Kind=="rain"));
            Assert.AreEqual(8,layer.GetComponentsInChildren<OpsPlanningMotion>().Count(m=>m.Kind=="ripple"));
            Assert.GreaterOrEqual(Find<Image>("SeasonTint").color.a,.15f);
            Assert.IsTrue(layer.GetComponentsInChildren<OpsIncidentGraphic>().Any(g=>g.Kind=="ripple"&&g.color.a>0));
            CheckPointer("Action_listen");Capture("83-june-rain-ripples");Assert.AreEqual(saved,JsonUtility.ToJson(game.State));
            Click("Menu");Click("ReduceMotion");Click("CloseDialog");yield return new WaitForSecondsRealtime(.3f);
            foreach(var motion in layer.GetComponentsInChildren<OpsPlanningMotion>().Where(m=>m.Kind=="rain"||m.Kind=="ripple"))Assert.AreEqual(0,motion.GetComponent<Graphic>().color.a);
            CheckPointer("Action_listen");LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator B案の効果音が全用途に割り当たり設定と結果表示でも使われる()
        {
            SceneManager.LoadScene("CompanyYear");yield return null;yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return null;
            foreach(OpsCue cue in Enum.GetValues(typeof(OpsCue)))
            {
                var clip=game.Sounds.Clip(cue);Assert.IsNotNull(clip,cue.ToString());Assert.Greater(clip.samples,1000);Assert.AreEqual(48000,clip.frequency);
            }
            Assert.AreEqual("01-click",game.Sounds.click.name);Assert.AreEqual("07-shield",game.Sounds.prepared.name);
            Click("Action_listen");yield return null;
            Assert.IsTrue(game.GetComponents<AudioSource>().Any(s=>s.clip==game.Sounds.action&&s.isPlaying));Assert.AreEqual(OpsCue.Action,game.LastCue);
            game.BeginIncident();game.Resolve("scope");yield return WaitForResolution(game);
            Assert.AreEqual(1,game.GetComponents<AudioSource>().Count(s=>s.clip==game.Sounds.count),"3つの数値を一音で数える");
            game.StartYear(14);
            for(int month=0;month<12&&game.State.phase!=OpsPhase.Ended;month++)
            {
                Plan(game.State);game.BeginIncident();game.Resolve(game.State.Current.kind=="outage"?"recover":"scope");game.Next();yield return null;
            }
            Assert.IsTrue(game.State.IsClear);
            // 12か月の点灯の後にランクを押す。固定の0.3秒ではなく実際の判子を待つ。
            float stampLimit=Time.realtimeSinceStartup+3;
            while(!game.GetComponents<AudioSource>().Any(s=>s.clip==game.Sounds.stamp&&s.isPlaying)&&Time.realtimeSinceStartup<stampLimit)yield return null;
            Assert.IsTrue(game.GetComponents<AudioSource>().Any(s=>s.clip==game.Sounds.clear&&s.isPlaying),"判子は年度ファンファーレを止めない");
            Assert.IsTrue(game.GetComponents<AudioSource>().Any(s=>s.clip==game.Sounds.stamp&&s.isPlaying));
            Click("Menu");yield return null;Find<Slider>("SoundVolume").value=0;yield return null;
            foreach(var source in game.GetComponents<AudioSource>().Where(s=>s.clip!=null&&!s.loop&&s.clip!=game.Sounds.titleMusic&&s.clip!=game.Sounds.planningMusic&&s.clip!=game.Sounds.incidentMusic&&s.clip!=game.Sounds.reviewMusic))Assert.AreEqual(0,source.volume);
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 負荷でフレームが遅れても抑制力と月報の数え上げは期限で確定する()
        {
            SceneManager.LoadScene("CompanyYear");yield return null;yield return new WaitForSecondsRealtime(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);
            game.State.levels[OpsCatalog.Index("backup")]=1;game.State.staffExperience[1]=OpsGrowthCatalog.StaffThresholds[1];game.State.supportOrder="investigate";
            game.BeginIncident();Click("Power_scope");
            // 読み込み等で一度だけ長いフレームになった状況。テスト専用で、通常プレイにはない。
            System.Threading.Thread.Sleep(650);yield return null;yield return null;
            Assert.AreEqual(game.State.ResponsePower("scope").Total.ToString(),Find<TextMeshProUGUI>("PowerTotalValue").text);
            Click("CloseDialog");game.Resolve("scope");game.Next();game.BeginIncident();game.Resolve("scope");Click("SkipResolution");
            var outcome=game.State.Latest;System.Threading.Thread.Sleep(650);yield return null;yield return null;
            Assert.AreEqual(outcome.loss+"<size=17>万円</size>",Find<TextMeshProUGUI>("MonthlyLossValue").text);
            Assert.AreEqual(outcome.downtime+"<size=17>時間</size>",Find<TextMeshProUGUI>("MonthlyStopValue").text);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
