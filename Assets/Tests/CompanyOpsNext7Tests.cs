using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static void DelegateWork(OpsGame game)
        {
            if(!game.MinigameActive)return;
            Assert.AreEqual(OpsPhase.Planning,game.State.phase);game.DelegateMinigame();game.ConfirmMinigame();
        }
        private static void ChooseDelegatedWork(OpsGame game,string action,string group="recover")
        {game.ChooseAction(action,group);DelegateWork(game);}
        [Test] public void Next7Logs_六手口と総当たりの連続と可視範囲と採点を守る()
        {
            for(int seed=0;seed<120;seed++)
            {
                var state=new OpsState(seed,true);string before=JsonUtility.ToJson(state);
                var a=new OpsLogMinigame(state);var b=new OpsLogMinigame(state);
                CollectionAssert.AreEqual(a.Queue.Select(r=>r.Time+r.User+r.Ip+r.Event),b.Queue.Select(r=>r.Time+r.User+r.Ip+r.Event));
                Assert.AreEqual(6,a.Queue.Count(r=>r.Suspicious));int idx=a.Queue.ToList().FindIndex(r=>r.Kind==0);
                Assert.AreEqual(1,a.Queue[idx+1].Kind);Assert.AreEqual(a.Queue[idx].Ip,a.Queue[idx+1].Ip);Assert.AreEqual(a.Queue[idx].User,a.Queue[idx+1].User);
                a.Start();while(a.Phase==OpsMinigamePhase.Playing)
                {
                    foreach(var row in a.Visible.Where(r=>r.Suspicious&&!r.Hit).ToArray())Assert.IsTrue(a.Hit(row.Id));
                    a.Tick(.4f);
                }
                Assert.AreEqual(100,a.Score);Assert.AreEqual(6,a.Total);Assert.AreEqual(6,a.Clues.Count);Assert.IsFalse(a.Hit(0));Assert.AreEqual(before,JsonUtility.ToJson(state));
            }
            var miss=new OpsLogMinigame(new OpsState(9,true));miss.Start();Assert.IsTrue(miss.Hit(miss.Visible.First(r=>!r.Suspicious).Id));
            Assert.IsFalse(miss.Hit(miss.Visible.First(r=>r.Hit).Id));miss.Tick(20);Assert.IsFalse(miss.Hit(0));miss.Tick(20);Assert.AreEqual(0,miss.Score);Assert.AreEqual(1,miss.Wrong);
        }
        [Test] public void Next7Logs_委任は完全互換で把握だけを閾値通り変える()
        {
            foreach(int score in new[]{0,34,35,50,79,80,100})
            {
                var baseline=new OpsState(24,true);string source=JsonUtility.ToJson(baseline);baseline.Act("audit");
                var scored=JsonUtility.FromJson<OpsState>(source);Assert.IsTrue(scored.Act("audit","recover",score));
                Assert.AreEqual(2,scored.EstimateMargin);Assert.AreEqual(baseline.capacity,scored.capacity);
                Assert.AreEqual(baseline.Blindness+(score<35?1:score>=80?-1:0),scored.Blindness);
                if(score>=35&&score<80)Assert.AreEqual(JsonUtility.ToJson(baseline),JsonUtility.ToJson(scored));
                Assert.IsTrue(scored.Valid());Assert.IsFalse(scored.Act("audit","recover",score));
            }
            var prepared=new OpsState(3,true);prepared.levels[OpsCatalog.Index("monitor")]=1;prepared.levels[OpsCatalog.Index("inventory")]=1;prepared.Act("audit","recover",100);Assert.AreEqual(0,prepared.Blindness);
        }
        [UnityTest] public IEnumerator Next7LogsUI_開始備え結果委任と行の操作を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.StartYear(9);string before=JsonUtility.ToJson(game.State);game.ChooseAction("audit");yield return new WaitForSecondsRealtime(.4f);
            Capture("next7-e-start");Assert.AreEqual(before,JsonUtility.ToJson(game.State));Click("MinigameDelegate");yield return null;Capture("next7-e-delegate");Click("MinigameContinue");
            var baseline=JsonUtility.FromJson<OpsState>(before);baseline.Act("audit");Assert.AreEqual(JsonUtility.ToJson(baseline),JsonUtility.ToJson(game.State));
            foreach(bool monitor in new[]{false,true})
            {
                game.StartYear(9);if(monitor)game.State.levels[OpsCatalog.Index("monitor")]=1;
                game.ChooseAction("audit");Click("MinigameStart");var logs=(OpsLogMinigame)game.Minigame;
                game.TickMinigame(6);yield return new WaitForSecondsRealtime(.4f);Capture(monitor?"next7-e-play-hint":"next7-e-play");
                foreach(var row in logs.Visible.Where(r=>r.Suspicious&&!r.Hit).ToArray()){yield return null;Canvas.ForceUpdateCanvases();CheckPointer("LogRow_"+row.Id);Click("LogRow_"+row.Id);}
                while(logs.Phase==OpsMinigamePhase.Playing)
                {
                    foreach(var row in logs.Visible.Where(r=>r.Suspicious&&!r.Hit).ToArray())game.HitLog(row.Id);game.TickMinigame(.5f);yield return null;
                }
                yield return new WaitForSecondsRealtime(3);Capture(monitor?"next7-e-result-hint":"next7-e-result");CheckPointer("MinigameContinue");Click("MinigameContinue");Assert.IsTrue(game.State.audited);Assert.IsTrue(game.State.Valid());
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next7Detail_帯は語句単位で道具のポーズを保つ()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.StartYear(9);game.State.levels[OpsCatalog.Index("education")]=1;
            game.OpenMailTraining();Click("MinigameStart");
            var mail=(OpsMailMinigame)game.Minigame;
            for(int i=0;i<10&&mail.Current.Id!="account";i++){game.AnswerMail(mail.Current.Suspicious);game.TickMinigame(.6f);}
            Assert.AreEqual("account",mail.Current.Id);yield return new WaitForSecondsRealtime(.4f);
            var label=Find<TextMeshProUGUI>("MailBodyText");StringAssert.DoesNotContain("<mark",label.text);
            var marker=Find<OpsPhraseMarker>("MailBodyTextMarker");marker.RebuildBands();
            Assert.AreEqual(label.textInfo.linkCount,marker.Bands.Count);Assert.Greater(marker.Bands.Count,0);
            foreach(var band in marker.Bands){Assert.Greater(band.width,label.fontSize);Assert.Greater(band.height,0);}
            Assert.IsFalse(marker.raycastTarget);Assert.Less(marker.transform.GetSiblingIndex(),label.transform.GetSiblingIndex());
            Assert.AreEqual("pose_magnifier",Find<OpsPortraitAnimator>("NavigatorPortrait").PoseId);
            yield return null;Canvas.ForceUpdateCanvases();
            var mesh=marker.canvasRenderer.GetMesh();Assert.IsNotNull(mesh);Assert.Greater(mesh.vertexCount,0,"帯の描画メッシュ");
            Capture("next7-c-play-hint");
            label.text="<b><link=\"mail-hint\">振込先口座が変更されました</link></b>";
            label.rectTransform.sizeDelta=new Vector2(110,100);yield return null;marker.RebuildBands();
            Assert.Greater(marker.Bands.Count,1);Assert.AreEqual(label.textInfo.lineCount,marker.Bands.Count);
            var fixture=DecisionFixture("remote",false);game.StartYear(fixture.seed);SetEvent(game.State,fixture.CurrentEvent.id);
            game.BeginIncident();while(game.PhasePresentationRunning)yield return null;game.ChooseResponse("scope");Click("MinigameStart");
            yield return new WaitForSecondsRealtime(.4f);Assert.AreEqual("pose_laptop",Find<OpsPortraitAnimator>("NavigatorPortrait").PoseId);
            var mfa=(OpsMfaMinigame)game.Minigame;game.AnswerMfa(!mfa.Current.Legitimate);yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual("pose_laptop",Find<OpsPortraitAnimator>("NavigatorPortrait").PoseId);Capture("next7-d-play");
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
