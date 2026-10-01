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

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static OpsStory AlliesStory(int year)
        {
            var story=OpsStory.Begin(14,null);
            for(int y=1;y<year;y++){FinishStoryTestYear(story.state);Assert.IsTrue(story.AdvanceYear());}
            return story;
        }
        [Test] public void Next11Allies_エンジニアの調査は月一回工数零で把握だけ増える()
        {
            var one=new OpsState(14,true);string original=JsonUtility.ToJson(one);
            Assert.IsFalse(one.RequestEngineerResearch());Assert.IsFalse(one.SelectSupportMember(3));Assert.AreEqual(original,JsonUtility.ToJson(one));
            var story=AlliesStory(2);var s=story.state;s.levels[OpsCatalog.Index("monitor")]=s.levels[OpsCatalog.Index("inventory")]=0;
            int work=s.capacity,cash=s.budget,xp=s.playerExperience,knowledge=s.SituationKnowledge;var before=s.Estimate("scope");
            Assert.IsTrue(s.RequestEngineerResearch());Assert.AreEqual(knowledge+1,s.SituationKnowledge);Assert.AreEqual(work,s.capacity);
            Assert.AreEqual(cash,s.budget);Assert.AreEqual(xp,s.playerExperience);Assert.IsFalse(s.audited);Assert.IsFalse(s.RequestEngineerResearch());
            Assert.LessOrEqual(s.Estimate("scope").lossMax,before.lossMax);Assert.IsTrue(story.Valid());
            var restored=JsonUtility.FromJson<OpsStory>(JsonUtility.ToJson(story));Assert.IsTrue(restored.Valid());Assert.IsFalse(restored.state.RequestEngineerResearch());
            s.BeginIncident();s.Resolve(PublicTestResponse(s));s.NextMonth();Assert.IsFalse(s.engineerRequested);Assert.IsTrue(s.RequestEngineerResearch());
            s.levels[OpsCatalog.Index("monitor")]=s.levels[OpsCatalog.Index("inventory")]=2;s.audited=true;
            Assert.AreEqual(OpsCatalog.KnowledgeMax,s.SituationKnowledge);Assert.IsFalse(s.RequestEngineerResearch());
        }
        [Test] public void Next11Allies_山場だけかのんの提案予算が増え未達なら全額返す()
        {
            var s=AlliesStory(2).state;Assert.AreEqual(0,s.SecretaryProposalBonus);int initial=s.ProposalOffer;
            s.month=2;Assert.AreEqual(OpsCatalog.SecretaryPeakBudget,s.SecretaryProposalBonus);Assert.AreEqual(initial+OpsCatalog.SecretaryPeakBudget,s.ProposalOffer);
            s.capacity=4;s.budget=100;s.audited=true;int before=s.budget,offer=s.ProposalOffer;
            Assert.IsTrue(s.Act("proposal","protect"));Assert.AreEqual(before+offer,s.budget);Assert.AreEqual(offer,s.proposalGrant);
            s.BeginIncident();var expected=s.Preview("scope");s.Resolve("scope");
            Assert.AreEqual(before-expected.loss-expected.cost+s.missionBudgetPaid+s.Latest.peakBudgetBonus,s.budget,"約束未達の追加予算は恩恵分も返却する");
            var old=new OpsState(14,true){month=2};Assert.AreEqual(0,old.SecretaryProposalBonus);
        }
        [Test] public void Next11Allies_後輩を四人目として育て担当支援と日常委任ができる()
        {
            var story=AlliesStory(3);var s=story.state;Assert.AreEqual(4,s.StaffCount);Assert.AreEqual(0,s.staffExperience[3]);Assert.AreEqual(1,s.StaffLevel(3));
            s.budget=400;s.capacity=4;s.playerExperience=4;s.levels[OpsCatalog.Index("runbook")]=1;
            Assert.IsTrue(s.Practice(3));Assert.AreEqual(OpsGrowthCatalog.PracticeXp+OpsCatalog.JuniorExtraExperience,s.staffExperience[3]);Assert.AreEqual(2,s.StaffLevel(3));
            Assert.IsTrue(s.SelectSupportMember(3));Assert.AreEqual(3,s.TicketMember);int work=s.capacity;
            Assert.IsTrue(s.ResolveTicket(true));Assert.AreEqual(work,s.capacity);Assert.AreEqual(5,s.staffExperience[3]);
            Assert.IsTrue(s.AssignSupport("investigate"));Assert.IsFalse(s.SelectSupportMember(0));StringAssert.StartsWith("後輩：",s.SupportSummary);
            Assert.Greater(s.ResponsePower("scope").staff,0);Assert.IsTrue(story.Valid());
            var restored=JsonUtility.FromJson<OpsStory>(JsonUtility.ToJson(story));Assert.IsTrue(restored.Valid());Assert.AreEqual(4,restored.state.supportMemberChoice);
            s.BeginIncident();var expected=s.Preview("scope");s.Resolve("scope",50,true);Assert.AreEqual(expected.loss,s.Latest.loss);Assert.AreEqual(expected.downtime,s.Latest.downtime);
            Assert.AreEqual(4,s.Latest.growth.staffXp.Length);Assert.GreaterOrEqual(s.Latest.growth.staffXp[3],2);Assert.IsTrue(story.Valid());
            if(!s.Latest.benign)Assert.AreEqual(3,OpsAnnualSummary.From(s).StaffMvp);
            s.NextMonth();Assert.AreEqual(0,s.supportMemberChoice);Assert.IsTrue(story.Valid());
        }
        [Test] public void Next11Allies_旧年度の社員三人を維持し壊れた担当番号は拒否する()
        {
            var s=AlliesStory(2).state;s.yearGrowthRules=0;s.yearThreatRules=0;s.yearPressure=OpsCatalog.StoryPressures[1];
            s.levels=s.levels.Take(OpsCatalog.BaseEquipmentCount).ToArray();
            s.eventSchedule=OpsEventCatalog.StorySchedule(s.seed,2,s.previousStoryEvents);
            s.monthStartMetrics=s.ReportMetrics;Assert.IsTrue(s.Valid());
            Assert.AreEqual(3,s.StaffCount);Assert.IsFalse(s.HasYearAllies);Assert.IsFalse(s.RequestEngineerResearch());Assert.IsFalse(s.Practice(3));
            s.staffExperience=new int[4];Assert.IsFalse(s.Valid());s.staffExperience=new int[3];s.engineerRequested=true;Assert.IsFalse(s.Valid());
            var junior=AlliesStory(3).state;Assert.IsFalse(junior.SelectSupportMember(4));Assert.IsFalse(junior.SelectSupportMember(-2));
            junior.supportMemberChoice=5;Assert.IsFalse(junior.Valid());
        }
        [UnityTest] public IEnumerator Next11AlliesUI_調査と四人チームと後輩の支援を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);
            var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            foreach(int year in new[]{2,3})
            {
                var story=AlliesStory(year);var s=story.state;s.budget=400;s.capacity=4;s.playerExperience=4;
                Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));yield return new WaitForSecondsRealtime(3);
                game.PreviewYearOpening(3);yield return new WaitForSecondsRealtime(3);Capture("next11-allies-y"+year+"-opening");game.AdvanceYearOpening();game.AdvanceYearOpening();yield return new WaitForSecondsRealtime(1);
                CheckPointer("EngineerResearch");Capture("next11-allies-y"+year+"-before-research");
                int knowledge=s.SituationKnowledge,work=s.capacity;Click("EngineerResearch");yield return null;
                Assert.AreEqual(knowledge+1,s.SituationKnowledge);Assert.AreEqual(work,s.capacity);Assert.IsFalse(Find<Button>("EngineerResearch").interactable);
                Capture("next11-allies-y"+year+"-after-research");
                yield return PreparePointer("OpenTeam");Click("OpenTeam");yield return new WaitForSecondsRealtime(1);CheckTeamText();
                Assert.AreEqual(year==3,UnityEngine.Object.FindObjectsByType<Button>().Any(b=>b.name=="Practice_3"));Capture("next11-allies-y"+year+"-team");
                if(year==3)
                {
                    CheckPointer("Practice_3");Click("Practice_3");yield return null;Assert.AreEqual(2,s.StaffLevel(3));
                    CheckPointer("SupportMember_3");Click("SupportMember_3");yield return null;Assert.AreEqual(4,s.supportMemberChoice);
                    CheckPointer("Support_investigate");Click("Support_investigate");yield return null;StringAssert.StartsWith("後輩：",s.SupportSummary);
                    yield return new WaitForSecondsRealtime(1);CheckTeamText();Capture("next11-allies-y3-support");
                    var help=Find<TextMeshProUGUI>("TeamOrderHelp").rectTransform;var close=(RectTransform)Find<Button>("CloseDialog").transform;
                    Assert.GreaterOrEqual(-close.anchoredPosition.y,-help.anchoredPosition.y+help.rect.height,"閉じるボタンを説明に重ねない");
                }
                Assert.IsTrue(game.ExportProgress().Valid());Click("CloseDialog");yield return null;
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
