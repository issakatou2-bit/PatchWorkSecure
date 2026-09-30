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
        [Test] public void Next7Blocks_五百週が条件付きで解けて名前が重複しない()
        {
            for(int seed=0;seed<500;seed++)
            {
                var state=new OpsState(seed,true);if(seed%2==0)state.levels[OpsCatalog.Index("automation")]=1;
                var game=new OpsBlockMinigame(state);Assert.That(game.Rows,Is.InRange(3,4));Assert.That(game.Columns,Is.InRange(4,6));Assert.That(game.Tasks.Count,Is.InRange(5,8));
                Assert.AreEqual(game.Tasks.Count,game.Tasks.Select(t=>t.Name).Distinct().Count());Assert.AreEqual(1,game.Tasks.Count(t=>t.Urgent));Assert.AreEqual(2,game.Tasks.Count(t=>t.Deadline>=0));Assert.AreEqual(1,game.Tasks.Count(t=>t.Avoid>=0));
                game.Start();Assert.AreEqual(game.Automation?1:0,game.PlacedCount);
                if(game.Automation){var auto=game.Tasks.Single(t=>t.Auto);Assert.IsFalse(auto.Constrained);Assert.AreEqual(game.Tasks.Where(t=>!t.Constrained).Min(t=>t.Cells.Length),auto.Cells.Length);}
                foreach(var task in game.Tasks.Where(t=>t.Placed==null))Assert.IsTrue(game.Place(task.Id,task.Solution.Min(c=>c.Row),task.Solution.Min(c=>c.Column)),"seed="+seed+" task="+task.Id);
                Assert.IsTrue(game.Tasks.All(t=>!t.Violates));Assert.AreEqual(game.Columns*3,game.Bonus);game.Finish();Assert.AreEqual(System.Math.Min(100,game.Tasks.Sum(t=>t.Value)+game.Columns*3),game.Score);
                var repeat=new OpsBlockMinigame(state);CollectionAssert.AreEqual(game.Tasks.Select(t=>t.Name+string.Join(";",t.Solution.Select(c=>c.Row+","+c.Column))),repeat.Tasks.Select(t=>t.Name+string.Join(";",t.Solution.Select(c=>c.Row+","+c.Column))));
            }
        }
        [Test] public void Next7Blocks_委任と信頼の閾値と置けない場所と回転を守る()
        {
            foreach(int score in new[]{0,39,40,50,84,85,100})
            {
                var s=new OpsState(13,true);s.trust=99;string before=JsonUtility.ToJson(s);var normal=JsonUtility.FromJson<OpsState>(before);normal.Act("map");s.Act("map","recover",score);
                Assert.AreEqual(100,s.trust);if(score>=40&&score<85)Assert.AreEqual(JsonUtility.ToJson(normal),JsonUtility.ToJson(s));
                s=new OpsState(13,true);int trust=s.trust;s.Act("map","recover",score);Assert.AreEqual(trust+(score<40?2:score>=85?6:4),s.trust);Assert.IsTrue(s.Valid());
            }
            var g=new OpsBlockMinigame(new OpsState(14,true));g.Start();var t=g.Tasks.First();var original=t.Cells.ToArray();g.Select(t.Id);
            for(int i=0;i<4;i++)Assert.IsTrue(g.Rotate(t.Id));CollectionAssert.AreEqual(original,t.Cells);
            Assert.IsFalse(g.Place(t.Id,-1,-1));Assert.IsTrue(g.Place(t.Id,t.Solution.Min(c=>c.Row),t.Solution.Min(c=>c.Column)));Assert.IsTrue(g.Lift(t.Id));Assert.IsNull(t.Placed);
            g.Tick(float.NaN);Assert.AreEqual(90,g.Remaining);g.Tick(100);Assert.AreEqual(OpsMinigamePhase.Result,g.Phase);
        }
        [UnityTest] public IEnumerator Next7BlocksUI_配置回転ドラッグ自動化結果委任を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.StartYear(14);string before=JsonUtility.ToJson(game.State);game.ChooseAction("map");yield return new WaitForSecondsRealtime(.4f);Capture("next7-f2-start");Click("MinigameDelegate");yield return null;Capture("next7-f2-delegate");Click("MinigameContinue");
            var baseline=JsonUtility.FromJson<OpsState>(before);baseline.Act("map");Assert.AreEqual(JsonUtility.ToJson(baseline),JsonUtility.ToJson(game.State));
            foreach(bool auto in new[]{false,true})
            {
                game.StartYear(14);if(auto)game.State.levels[OpsCatalog.Index("automation")]=1;game.ChooseAction("map");Click("MinigameStart");yield return new WaitForSecondsRealtime(.5f);
                var blocks=(OpsBlockMinigame)game.Minigame;Capture(auto?"next7-f2-play-auto":"next7-f2-play");
                var task=blocks.Tasks.First(t=>t.Placed==null);game.SelectBlock(task.Id);yield return null;Canvas.ForceUpdateCanvases();CheckPointer("BlockRotate_"+task.Id);for(int i=0;i<4;i++)game.RotateBlock(task.Id);
                int r=task.Solution.Min(c=>c.Row),c=task.Solution.Min(x=>x.Column);Canvas.ForceUpdateCanvases();CheckPointer("BlockCell_"+r+"_"+c);Click("BlockCell_"+r+"_"+c);yield return null;Assert.IsNotNull(task.Placed);
                var cell=Find<RectTransform>("BlockCell_"+r+"_"+c);var corners=new Vector3[4];cell.GetWorldCorners(corners);Vector2 target=RectTransformUtility.WorldToScreenPoint(null,corners[1]+new Vector3(2,-2));
                game.BeginBlockDrag(task.Id,true,target);yield return null;game.MoveBlockDrag(target);game.EndBlockDrag(target);Assert.IsNotNull(task.Placed);
                foreach(var next in blocks.Tasks.Where(t=>t.Placed==null).ToArray()){game.SelectBlock(next.Id);game.PlaceSelectedBlock(next.Solution.Min(x=>x.Row),next.Solution.Min(x=>x.Column));yield return null;}
                Capture("next7-f2-filled"+(auto?"-auto":""));Click("BlockFinish");yield return new WaitForSecondsRealtime(3);Assert.AreEqual(100,blocks.Score);Capture("next7-f2-result"+(auto?"-auto":""));CheckPointer("MinigameContinue");Click("MinigameContinue");Assert.IsTrue(game.State.mapped);Assert.IsTrue(game.State.Valid());
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
