#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        [UnityTest] public IEnumerator ゲーマー役で五年度を画面のボタンから通して再挑戦する() => PlayPersona(0);
        [UnityTest] public IEnumerator IT初学者役で五年度を画面のボタンから通して再挑戦する() => PlayPersona(1);
        [UnityTest] public IEnumerator FE取得者役で五年度を画面のボタンから通して再挑戦する() => PlayPersona(2);
        private IEnumerator PlayPersona(int role)
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>(); var memory=new PersonaMemory{role=role};
            var transcript=new StringBuilder("役,周回,月,出来事,行動,対応,被害,停止,予算,工数,社員委任,依頼,知識画面\n");
            var overflow=new HashSet<string>(); int months=0,clicks=0;
            string directory=Path.Combine(Application.dataPath,"../Artifacts/CompanyOps/Persona"); Directory.CreateDirectory(directory);
            for(int cycle=0;cycle<5;cycle++)
            {
                memory.cycle=cycle; game.StartYear(CompanyOpsPersonaPolicy.Seeds[cycle]); yield return null;
                // 種の指定だけは試験用入口。以後の計画・購入・対応・知識・次月は表示ボタンを通す。
                while(game.State.phase!=OpsPhase.Ended)
                {
                    var state=game.State; var turn=new PersonaTurn(); PersonaCommand command;
                    CheckText(); CollectPersonaOverflow(overflow);
                    if((cycle==0 || cycle==4) && state.month==0) Capture("Persona/"+role+"-"+(cycle+1)+"-planning",1280,720);
                    while((command=CompanyOpsPersonaPolicy.Next(state,memory,turn))!=null)
                    {
                        yield return ExecutePersonaCommand(command); clicks++;
                        CompanyOpsPersonaPolicy.Applied(turn,command); Assert.IsTrue(state.Valid(),command.ToString());
                        Assert.AreSame(state,game.State);
                    }
                    Assert.LessOrEqual(turn.steps,24);
                    CheckText(); CollectPersonaOverflow(overflow); int unused=state.capacity;
                    yield return PersonaClick("AdvanceMonth"); yield return null; clicks++;
                    if(state.phase==OpsPhase.Planning) { yield return PersonaClick("ConfirmAdvance"); yield return null; clicks++; }
                    Assert.AreEqual(OpsPhase.Incident,state.phase);
                    foreach(string response in CompanyOpsPersonaPolicy.Responses)
                    {
                        var estimate = state.Estimate(response);
                        Assert.AreEqual(estimate.lossMin == estimate.lossMax ? estimate.lossMin.ToString() : estimate.lossMin+"～"+estimate.lossMax,
                            Find<TextMeshProUGUI>("EstimateValue_Loss_"+response).text);
                        Assert.AreEqual(estimate.stopMin == estimate.stopMax ? estimate.stopMin.ToString() : estimate.stopMin+"～"+estimate.stopMax,
                            Find<TextMeshProUGUI>("EstimateValue_Stop_"+response).text);
                        Assert.AreEqual(estimate.cost.ToString(), Find<TextMeshProUGUI>("ResponseCost_"+response).text);
                    }
                    CheckText(); CollectPersonaOverflow(overflow);
                    if((cycle==0 || cycle==4) && (state.month==2 || state.month==11))
                        Capture("Persona/"+role+"-"+(cycle+1)+"-incident-"+(state.month+1),1280,720);
                    string chosen=CompanyOpsPersonaPolicy.Response(state,memory);
                    yield return PersonaClick("Respond_"+chosen); yield return WaitForResolution(game); clicks++;
                    Assert.AreEqual(OpsPhase.Review,state.phase); CheckText(); CollectPersonaOverflow(overflow);
                    // 画面数は露出の記録。開くことを知識習得とみなさない。
                    bool read=CompanyOpsPersonaPolicy.ReadLesson(state,memory);
                    if(read) { yield return PersonaClick("MonthlyLesson"); yield return null; clicks++; CollectPersonaOverflow(overflow);
                        memory.readTerms.Add(state.Current.lesson); yield return PersonaClick("CloseDialog"); yield return null; clicks++; }
                    transcript.AppendLine(string.Join(",",new object[]{memory.Label,cycle+1,state.month+1,state.CurrentEvent.id,string.Join(" > ",turn.actions),
                        chosen,state.Latest.loss,state.Latest.downtime,state.budget,unused,state.Latest.ticketMode,state.CurrentMissionCompleted,read}.Select(PersonaCell)));
                    months++; yield return PersonaClick("NextMonth"); yield return null; clicks++;
                    if(state.QuarterRewardPending)
                    { yield return PersonaClick("Reward_"+CompanyOpsPersonaPolicy.Reward(state,memory)); yield return null; clicks++; }
                    Assert.IsTrue(state.Valid());
                }
                CheckText(); CollectPersonaOverflow(overflow);
                if(cycle==0 || cycle==4) Capture("Persona/"+role+"-"+(cycle+1)+"-ending",1280,720);
                yield return PersonaClick("EndingHistory"); yield return null; clicks++; CheckText(); CollectPersonaOverflow(overflow);
                var history=Find<ScrollRect>("ProjectScroll"); history.verticalNormalizedPosition=0; yield return null;
                CollectPersonaOverflow(overflow); yield return PersonaClick("CloseDialog"); yield return null; clicks++;
            }
            File.WriteAllText(Path.Combine(directory,"ui-role-"+role+".csv"),transcript.ToString(),new UTF8Encoding(true));
            File.WriteAllLines(Path.Combine(directory,"overflow-role-"+role+".txt"),overflow.OrderBy(x=>x),new UTF8Encoding(true));
            Assert.AreEqual(60,months,"代表の5年度を完走できなかった。敗北は不具合とは別に再評価する");
            Assert.IsEmpty(glyphWarnings,string.Join("\n",glyphWarnings)); LogAssert.NoUnexpectedReceived();
            File.WriteAllText(Path.Combine(directory,"ui-role-"+role+"-summary.txt"),
                memory.Label+" / "+months+"か月 / "+clicks+"主要操作 / 追加の文字収まり指摘 "+overflow.Count,new UTF8Encoding(true));
        }
        private static string PersonaCell(object value) => "\""+(value??"").ToString().Replace("\"","\"\"")+"\"";
        private static IEnumerator PersonaClick(string name)
        {
            NavigatePlanningControl(name); yield return null;
            if(name=="Tab0"&&!Object.FindObjectsByType<Button>().Any(b=>b.name=="Tab0"))
            {Assert.IsNotNull(Find<Button>("Action_audit"));yield break;}
            Assert.IsTrue(Object.FindObjectsByType<Button>().Any(b=>b.name==name),"移行後の操作が見つからない："+name);
            CheckPointer(name);Click(name);
        }
        private static void CollectPersonaOverflow(HashSet<string> target)
        {
            string[] watched={"Boss","Staff","EmployeeGrowth","NavigatorSpeech","MonthlyHint","DialogBody","EventCategory","EventMapping",
                "EventPreparation","EventReference","TicketBenefitText","TicketContext","AnnualNumbers","AnnualLearning"};
            foreach(var t in Object.FindObjectsByType<TextMeshProUGUI>())
                if(watched.Contains(t.name) || t.name.StartsWith("History"))
                { t.ForceMeshUpdate(); if(t.isTextOverflowing) target.Add(t.name+" / "+t.text.Replace("\n"," / ")); }
        }
        private static IEnumerator ExecutePersonaCommand(PersonaCommand c)
        {
            if(c.kind=="act")
            {
                if(c.id=="prepare") { yield return PersonaClick("OpenSituation"); yield return null; yield return PersonaClick("PrepareSituation"); yield return null; }
                else { yield return PersonaClick("Tab0"); yield return null; yield return PersonaClick("Action_"+c.id); yield return null; }
            }
            else if(c.kind=="proposal")
            { yield return PersonaClick("Tab1"); yield return null; yield return PersonaClick("Proposal"); yield return null; yield return PersonaClick("Propose_"+c.id); yield return null; }
            else if(c.kind=="buy")
            {
                yield return PersonaClick("Tab1"); yield return null;
                var details=Find<Button>("Details_"+c.id); var scroll=details.GetComponentInParent<ScrollRect>();
                Canvas.ForceUpdateCanvases();
                var rect=details.GetComponent<RectTransform>(); var content=scroll.content;
                float localY=content.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
                var pos=content.anchoredPosition; pos.y=Mathf.Clamp(-localY-scroll.viewport.rect.height*.5f,0,
                    Mathf.Max(0,content.rect.height-scroll.viewport.rect.height)); content.anchoredPosition=pos;
                yield return null; yield return PersonaClick("Details_"+c.id); yield return null;
                yield return PersonaClick("Buy_"+c.id); yield return null;
            }
            else if(c.kind=="support" || c.kind=="practice")
            { yield return PersonaClick("OpenTeam"); yield return null; yield return PersonaClick((c.kind=="support"?"Support_":"Practice_")+c.id); yield return null;
                yield return PersonaClick("CloseDialog"); yield return null; }
            else
            { yield return PersonaClick("OpenTicket"); yield return null; yield return PersonaClick(c.id=="delegate"?"DelegateTicket":"ResolveTicket"); yield return null; }
        }
    }
}
#endif
