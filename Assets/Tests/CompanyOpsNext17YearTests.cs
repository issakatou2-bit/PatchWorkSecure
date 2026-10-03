#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static bool PadButtonExists(string name)=>Object.FindObjectsByType<Button>().Any(b=>b.name==name&&b.IsInteractable());
        private static IEnumerator PadDelegate(OpsGame game,PadFixture input)
        {
            if(!game.MinigameActive)yield break;
            yield return PadClick(game,input,"MinigameDelegate");yield return null;
            yield return PadClick(game,input,"MinigameContinue");yield return null;
        }
        [Category("Long")]
        [UnityTest] public IEnumerator Next17Year_タイトルから四月三月と年間評価までゲームパッドで進む()
        {
            using(var input=new PadFixture())
            {
                SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1);var game=Object.FindAnyObjectByType<OpsGame>();
                yield return input.Tap(input.Pad.dpad.up);yield return PadClick(game,input,"SingleYear");while(game.PhasePresentationRunning)yield return null;
                if(game.TutorialActive)yield return PadClick(game,input,"SkipTutorial");
                Assert.AreEqual(14,game.State.seed);
                yield return input.Tap(input.Pad.startButton);yield return PadClick(game,input,"SpeedFast");yield return null;
                yield return PadTo(game,input,"VoiceVolume");for(int i=0;i<20;i++)yield return input.Tap(input.Pad.dpad.left);
                yield return input.Tap(input.Pad.buttonEast);yield return new WaitForSecondsRealtime(.4f);
                for(int month=0;month<12;month++)
                {
                    Assert.AreEqual(month,game.State.month);Assert.AreEqual(OpsPhase.Planning,game.State.phase);
                    if(game.State.fatigue>40&&game.State.ActionBlock("rest")=="")yield return PadClick(game,input,"Action_rest");
                    if(game.State.ActionBlock("listen")=="")yield return PadClick(game,input,"Action_listen");
                    foreach(string id in new[]{"automation","inventory","backup","drill","education","runbook","mfa","patch","redundancy","monitor","segment"})
                    {
                        int index=OpsCatalog.Index(id);if(game.State.levels[index]!=0||game.State.UpgradeBlock(index)!="")continue;
                        if(game.State.budget<game.State.Cost(index)+12)continue;
                        yield return PadClick(game,input,"OpenProjects");
                        if(!game.State.proposed&&game.State.capacity>=game.State.WorkCost(index)+1&&game.State.ActionBlock("proposal")=="")
                        {
                            yield return PadClick(game,input,"Proposal");yield return PadClick(game,input,"Propose_"+OpsCatalog.Projects[index].group);
                        }
                        yield return PadClick(game,input,"Details_"+id);yield return PadClick(game,input,"Buy_"+id);
                        yield return input.Tap(input.Pad.buttonEast);yield return new WaitForSecondsRealtime(.15f);
                    }
                    foreach(string action in new[]{"audit","rest","map"})if(game.State.ActionBlock(action)=="")
                    {yield return PadClick(game,input,"Action_"+action);yield return PadDelegate(game,input);}
                    yield return PadClick(game,input,"AdvanceMonth");if(PadButtonExists("ConfirmAdvance"))yield return PadClick(game,input,"ConfirmAdvance");
                    while(game.PhasePresentationRunning){yield return input.Tap(input.Pad.buttonSouth);yield return null;}
                    Assert.AreEqual(OpsPhase.Incident,game.State.phase);
                    if(month==0)Next17Shots("incident-selected");
                    yield return PadClick(game,input,"Respond_"+PublicTestResponse(game.State));yield return PadDelegate(game,input);
                    float deadline=Time.realtimeSinceStartup+60;
                    while(game.ResolutionActive&&Time.realtimeSinceStartup<deadline)
                    {if(game.CanSkipResolution)yield return input.Tap(input.Pad.buttonSouth);else yield return null;}
                    Assert.IsFalse(game.ResolutionActive);Assert.AreEqual(OpsPhase.Review,game.State.phase);
                    while(game.PhasePresentationRunning)yield return null;
                    if(month==0)Next17Shots("monthly-selected");
                    yield return PadClick(game,input,"NextMonth");Assert.IsTrue(game.DiaryActive);yield return new WaitForSecondsRealtime(.2f);
                    if(month==0)Next17Shots("diary-selected");yield return input.Tap(input.Pad.buttonEast);
                    if(PadButtonExists("Reward_budget"))yield return PadClick(game,input,"Reward_budget");
                    while(game.PhasePresentationRunning)yield return null;yield return new WaitForSecondsRealtime(.2f);
                }
                Assert.AreEqual(OpsPhase.Ended,game.State.phase);Assert.IsTrue(game.State.IsClear);Assert.AreEqual(12,game.State.history.Count);
                yield return input.Tap(input.Pad.buttonSouth);yield return new WaitForSecondsRealtime(.4f);Next17Shots("annual-selected");
                Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
        }
    }
}
#endif
