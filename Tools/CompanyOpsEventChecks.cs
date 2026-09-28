using System;
using System.Linq;
using System.Text.RegularExpressions;
using PatchWorkSecure.CompanyOps;

// 仮想プレイは公開された予測だけで判断する。人間の楽しさの評価とは別。
public static class CompanyOpsEventChecks
{
    static readonly string[] Responses = { "contain", "scope", "recover" };
    static int ForecastCost(OpsState s, string response)
    {
        var n=Regex.Matches(s.Forecast(response),@"\d+").Cast<Match>().Select(m=>int.Parse(m.Value)).ToArray();
        return (n[1]+(response=="contain"?6:response=="scope"?3:4))*2+n[3];
    }
    static string Choice(OpsState s) => Responses.OrderBy(r=>ForecastCost(s,r)).First();
    static void Check(bool ok,string reason) { if (!ok) throw new Exception(reason); }
    static void Plan(OpsState s,int policy)
    {
        if (policy==0) return;
        if (s.fatigue>40) s.Act("rest");
        if (policy==3)
        {
            if (s.Level("runbook")==0) s.Upgrade(OpsCatalog.Index("runbook"));
            if (!s.ResolveTicket(true) && s.capacity>=3) s.ResolveTicket(false);
        }
        if (policy==1) s.Act("listen");
        else { s.Act(s.CurrentMission.actionA); s.Act(s.CurrentMission.actionB); }
        string[] ids=policy==1 ? new[]{"automation","inventory","backup","drill","education","runbook","mfa","patch","redundancy","monitor","segment"} :
            new[]{"automation","inventory",s.CurrentMission.projectA,s.CurrentMission.projectB,"runbook","education","patch","mfa","backup","drill","redundancy","monitor","segment"};
        foreach (string id in ids.Distinct())
        {
            int i=OpsCatalog.Index(id);
            if (s.Level(id)>0 || s.UpgradeBlock(i)!="") continue;
            if (s.capacity>=s.WorkCost(i)+1 && !s.proposed && s.Evidence>0) s.Act("proposal",OpsCatalog.Projects[i].group);
            if (s.budget>=s.Cost(i)+12) s.Upgrade(i);
        }
        s.Act("audit"); s.Act("listen"); s.Act("map");
        if (s.Situation.stopLossCap>0 || s.SituationFatigue>0) s.Act("prepare");
        if (s.fatigue>12) s.Act("rest");
    }
    public static void Main()
    {
        Check(OpsEventCatalog.Events.Length==40 && OpsEventCatalog.Tickets.Length==18,"内容の件数不一致");
        int[] encountered=new int[40], allChoices=new int[3]; string[] labels={"放置","運用優先","状況と依頼優先","日常仕事と社員育成優先"};
        for(int policy=0;policy<4;policy++)
        {
            int wins=0,loss=0,stop=0,missions=0,tickets=0,delegated=0; int[] choices=new int[3];
            for(int seed=0;seed<300;seed++)
            {
                var s=new OpsState(seed,true);
                Check(s.Valid(),"年度抽選で状態破損");
                for(int m=0;m<12 && s.phase!=OpsPhase.Ended;m++)
                {
                    string id=s.CurrentEvent.id; encountered[Array.FindIndex(OpsEventCatalog.Events,e=>e.id==id)]++;
                    Plan(s,policy); Check(s.Valid(),"計画で状態破損");
                    string schedule=string.Join(",",s.eventSchedule);
                    foreach(string r in Responses)
                    {
                        var outcome=s.Preview(r);
                        Check(outcome.loss>=0 && outcome.downtime>=0 && outcome.eventId==id,"出来事の予測不一致");
                    }
                    Check(schedule==string.Join(",",s.eventSchedule),"予測で再抽選");
                    s.BeginIncident(); string response=policy==0?"scope":Choice(s); choices[Array.IndexOf(Responses,response)]++;
                    s.Resolve(response); if(s.Latest.ticketMode!="defer") tickets++; if(s.Latest.ticketMode=="delegate") delegated++;
                    Check(s.Valid(),"月報で状態破損"); s.NextMonth(); Check(s.Valid(),"月次進行で状態破損");
                }
                wins+=s.IsClear?1:0; loss+=s.totalLoss; stop+=s.totalDowntime; missions+=s.MissionCount;
            }
            Console.WriteLine("ランダム年度 / "+labels[policy]+" / 完走 "+wins+"/300 / 平均被害 "+Math.Round(loss/300.0,2)+
                "万円 / 停止 "+Math.Round(stop/300.0,2)+"h / 依頼 "+Math.Round(missions/300.0,2)+
                " / 日常対応 "+tickets+"件（委任 "+delegated+"件） / 停止:限定:復旧 "+string.Join(":",choices));
            Check(policy==0 || wins>0,"準備しても完走できない");
            if (policy>0) for(int i=0;i<3;i++) allChoices[i]+=choices[i];
        }
        Check(encountered.All(n=>n>0),"抽選されない出来事がある");
        Check(allChoices.All(n=>n>0),"どの準備方針でも使われない重点配分がある");
        Console.WriteLine("40種全てが発生 / 保存対象の月報と工数・因果の検証: 成功");
    }
}
