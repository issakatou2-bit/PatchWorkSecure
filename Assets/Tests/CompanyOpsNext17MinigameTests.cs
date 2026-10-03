#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator Next17Minigames_六本をゲームパッドだけで最高ランクまで遊ぶ()
        {
            using(var input=new PadFixture())
            {
                SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1);var game=Object.FindAnyObjectByType<OpsGame>();
                yield return input.Tap(input.Pad.dpad.up);yield return PadClick(game,input,"HomeRecords");yield return new WaitForSecondsRealtime(.3f);
                foreach(string id in OpsDailyPractice.Ids)
                {
                    yield return PadClick(game,input,"PracticePlay"+id);yield return new WaitForSecondsRealtime(.2f);Next17Shots(id+"-intro");
                    yield return PadClick(game,input,"MinigameStart");var session=game.Minigame;float deadline=Time.realtimeSinceStartup+session.Duration+30;
                    while(session.Phase==OpsMinigamePhase.Playing&&Time.realtimeSinceStartup<deadline)
                    {
                        if(session is OpsContainmentMinigame b)
                        {
                            int pc=Enumerable.Range(0,b.PCCount).Where(i=>b.Visible(i)).DefaultIfEmpty(-1).First();
                            if(pc>=0)yield return PadClick(game,input,"MinigamePC_"+pc);else yield return null;
                        }
                        else if(session is OpsMailMinigame c)
                        {
                            if(c.CanAnswer)yield return PadClick(game,input,c.Current.Suspicious?"MailReport":"MailSafe");else yield return null;
                        }
                        else if(session is OpsMfaMinigame d)
                        {
                            if(d.CanAnswer)yield return PadClick(game,input,d.Current.Legitimate?"MfaAllow":"MfaDeny");else yield return null;
                        }
                        else if(session is OpsLogMinigame e)
                        {
                            var row=e.Visible.FirstOrDefault(r=>r.Suspicious&&!r.Hit);
                            if(row!=null)yield return PadClick(game,input,"LogRow_"+row.Id);else yield return null;
                        }
                        else if(session is OpsBlockMinigame f)
                        {
                            var task=f.Tasks.FirstOrDefault(t=>t.Placed==null);
                            if(task==null)yield return PadClick(game,input,"BlockFinish");
                            else
                            {
                                yield return PadClick(game,input,"BlockTask_"+task.Id);
                                // 答えの位置は検証側だけが使う。実装に解答・自動配置を足さない。
                                int row=task.Solution.Min(c=>c.Row),column=task.Solution.Min(c=>c.Column);
                                yield return PadClick(game,input,"BlockCell_"+row+"_"+column);
                                Assert.IsNotNull(task.Placed);
                            }
                        }
                        else if(session is OpsRestoreMinigame g)
                        {
                            var node=g.BestOrder.Select(n=>g.Nodes.First(x=>x.Id==n)).FirstOrDefault(n=>!n.Up&&!n.Busy&&g.Dependencies(n).All(p=>g.Nodes.First(x=>x.Id==p).Up));
                            if(node!=null&&g.Nodes.Count(n=>n.Busy)<2)yield return PadClick(game,input,"RestoreNode_"+node.Id);else yield return null;
                        }
                    }
                    Assert.AreEqual(OpsMinigamePhase.Result,session.Phase,id);Assert.AreEqual("S",session.Grade,id+" / "+session.Score);
                    yield return new WaitForSecondsRealtime(1.3f);
                    while(game.MinigameCounting)yield return null;
                    yield return new WaitForSecondsRealtime(.3f);Next17Shots(id+"-result");
                    yield return PadClick(game,input,"MinigameContinue");Assert.IsFalse(game.MinigameActive);
                }
                Assert.IsNull(game.State,"練習は本編を開始しない");Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
        }
    }
}
#endif
