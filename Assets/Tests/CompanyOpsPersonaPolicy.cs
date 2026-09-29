using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    // 人間の認知モデルではない。画面で公開された情報から選ぶ、再現可能な仮想方針。
    public sealed class PersonaMemory
    {
        public int role, cycle;
        public HashSet<string> readTerms = new HashSet<string>();
        public string Label => new[] { "ゲーマー", "IT初学者", "FE取得者" }[role];
    }
    public sealed class PersonaTurn
    {
        public int purchases, steps;
        public List<string> actions = new List<string>();
    }
    public sealed class PersonaCommand
    {
        public string kind, id;
        public PersonaCommand(string kind, string id) { this.kind=kind; this.id=id; }
        public override string ToString() => kind+":"+id;
    }
    public static class CompanyOpsPersonaPolicy
    {
        public static readonly int[] Seeds = { 14, 71, 203, 14, 71 };
        public static readonly string[] Responses = { "contain", "scope", "recover" };
        private static PersonaCommand Command(string kind, string id) => new PersonaCommand(kind,id);
        public static int[] PublicForecast(OpsState state, string response) => Regex.Matches(state.Forecast(response),@"\d+")
            .Cast<Match>().Select(m=>int.Parse(m.Value)).ToArray();
        public static PersonaCommand Next(OpsState s, PersonaMemory memory, PersonaTurn turn)
        {
            if (s.phase!=OpsPhase.Planning || turn.steps>24) return null;
            int role=memory.role;
            bool familiar=memory.readTerms.Contains(s.Current.lesson);
            bool experienced=memory.cycle>0;
            if(experienced||role==2||s.month%2==0)for(int i=0;i<4;i++)if(s.BubbleAvailable(i)&&s.BubbleKind(i)==6)return Command("bubble",i.ToString());
            if (string.IsNullOrEmpty(s.supportOrder) && (role==2 || experienced))
            {
                string order=role==0 || role==1 ? "routine" : s.DataRecoveryApplies || s.RestartApplies ? "recover" : "investigate";
                if (s.SupportBlock(order)=="") return Command("support",order);
            }
            int restAt=role==0 ? 60 : role==1 ? 40 : 35;
            if (s.fatigue>restAt && s.ActionBlock("rest")=="") return Command("act","rest");
            if (role==1 && s.ActionBlock("listen")=="") return Command("act","listen");
            if (role==2 && s.ActionBlock("audit")=="") return Command("act","audit");
            // 初回ゲーマーは目に付く恒久成長へ先行投資。再挑戦では依頼報酬も狙う。
            bool followMission=role==2 || role==0 && experienced || role==1 && familiar;
            if (followMission)
                foreach(string action in new[]{s.CurrentMission.actionA,s.CurrentMission.actionB})
                    if (s.ActionBlock(action)=="") return Command("act",action);
            if ((role==2 || experienced) && s.Situation.stopLossCap>0 && s.ActionBlock("prepare")=="") return Command("act","prepare");
            if (s.TicketBlock(true)=="" && (role!=0 || experienced)) return Command("ticket","delegate");
            if (experienced && s.Level("runbook")>0 && s.capacity>=2 && !s.practiced && s.StaffLevel(s.Ticket.member)<2 &&
                s.PracticeBlock(s.Ticket.member)=="") return Command("practice",s.Ticket.member.ToString());
            int limit=role==1 && !familiar ? 1 : 2;
            string[] priorities=role==0 ? new[]{"automation","runbook","inventory","education","backup","drill","patch","mfa","monitor","segment","redundancy"} :
                role==1 && !familiar ? new[]{"backup","education","runbook","inventory","mfa","patch","automation","drill","monitor","segment","redundancy"} :
                role==2 ? new[]{"inventory",s.CurrentMission.projectA,s.CurrentMission.projectB,"runbook","patch","backup","drill","mfa","education","automation","monitor","redundancy","segment"} :
                new[]{"runbook","automation",s.CurrentMission.projectA,s.CurrentMission.projectB,"inventory","education","backup","drill","patch","mfa","monitor","redundancy","segment"};
            if (turn.purchases<limit)
                foreach(string id in priorities.Where(x=>!string.IsNullOrEmpty(x)).Distinct())
                {
                    int i=OpsCatalog.Index(id);
                    bool raise=role==0 && experienced && id=="automation" && s.month>=2 && s.month<7;
                    if (s.Level(id)>0 && !(raise && s.Level(id)<2)) continue;
                    int reserve=role==0 ? 12 : role==1 ? 8 : 14;
                    if (s.UpgradeBlock(i)=="" && s.budget>=s.Cost(i)+reserve) return Command("buy",id);
                    var project=OpsCatalog.Projects[i];
                    if ((role==2 || experienced) && s.ActionBlock("proposal")=="" && s.capacity>=s.WorkCost(i)+1 &&
                        s.budget+12+s.Evidence*3>=s.Cost(i)+reserve &&
                        (string.IsNullOrEmpty(project.requires)||s.Level(project.requires)>0)) return Command("proposal",project.group);
                }
            if (s.TicketBlock(false)=="" && (role==1 || experienced || role==2 && s.capacity>=2)) return Command("ticket","self");
            foreach(string action in new[]{"audit","map","listen","rest"})
                if (s.ActionBlock(action)=="" && (action!="rest" || s.fatigue>12)) return Command("act",action);
            return null;
        }
        public static string Response(OpsState s, PersonaMemory memory)
        {
            // 未読の題材に対する初学者の慎重な選択。取得済み用語数を正答率とは呼ばない。
            if (memory.role==1 && !memory.readTerms.Contains(s.Current.lesson))
                return s.Current.kind=="outage" ? "recover" : "contain";
            int lossWeight=memory.role==0 ? 7 : memory.role==1 ? 4 : 3;
            int stopWeight=memory.role==0 ? 4 : memory.role==1 ? 1 : 3;
            return Responses.OrderBy(r=> { var f=PublicForecast(s,r); int fee=s.Estimate(r).cost;
                return lossWeight*f[1]+stopWeight*f[3]+fee*2; }).First();
        }
        public static bool ReadLesson(OpsState s, PersonaMemory memory) => memory.role!=0 || memory.cycle>0 || s.Latest.loss>6;
        public static string Reward(OpsState s, PersonaMemory memory) => memory.role==0 && s.budget>28 ? "capacity" : "budget";
        public static void Applied(PersonaTurn turn, PersonaCommand command)
        {
            turn.steps++; turn.actions.Add(command.ToString()); if(command.kind=="buy") turn.purchases++;
        }
        public static bool ApplyRule(OpsState s, PersonaCommand c) => c.kind=="act" ? s.Act(c.id) : c.kind=="proposal" ? s.Act("proposal",c.id) :
            c.kind=="buy" ? s.Upgrade(OpsCatalog.Index(c.id)) : c.kind=="support" ? s.AssignSupport(c.id) : c.kind=="practice" ?
            s.Practice(int.Parse(c.id)) : c.kind=="bubble"?s.PopBubble(int.Parse(c.id)):s.ResolveTicket(c.id=="delegate");
    }
}
