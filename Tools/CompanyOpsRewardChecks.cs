using System;
using System.Linq;
using PatchWorkSecure.CompanyOps;
// 公開見積もりで選択する同じ方針・シードを比較。楽しさの測定ではない。
public static class CompanyOpsRewardChecks
{
    static readonly string[] Order={"inventory","mfa","backup","education","runbook","patch","monitor","drill","automation","segment","redundancy"};
    public static void Main()
    {
        foreach(bool modern in new[]{false,true})
        {
            var state=new OpsState(14);state.missionBudgetRules=modern?1:0;state.Act(state.CurrentMission.actionA);state.Act(state.CurrentMission.actionB);int money=state.budget;
            state.BeginIncident();if(state.budget-money!=(modern?OpsState.MissionBudgetReward:0))throw new Exception("予算の一度だけの付与");
            money=state.budget;state.BeginIncident();if(state.budget!=money)throw new Exception("予算の二重付与");state.Resolve("scope");
            if(state.Latest.missionBonus!=(modern?OpsState.MissionBudgetReward:0))throw new Exception("月報の予算記録");
        }
        foreach(int reward in new[]{0,1,3,5})for(int policy=0;policy<3;policy++)
        {
            int wins=0,loss=0,cash=0,missions=0,levels=0;
            for(int seed=0;seed<300;seed++)
            {
                var s=new OpsState(seed,true);
                s.missionBudgetRules=0;
                s.rankBenefitRules=0;
                for(int m=0;m<12&&s.phase!=OpsPhase.Ended;m++)
                {
                    if(policy>0)
                    {
                        if(s.fatigue>40)s.Act("rest");
                        if(policy==2){s.Act(s.CurrentMission.actionA);s.Act(s.CurrentMission.actionB);}else s.Act("listen");
                        foreach(var id in Order){int i=OpsCatalog.Index(id);if(s.Level(id)<(policy==2?2:1)&&s.budget>=s.Cost(i)+12)s.Upgrade(i);}
                        s.Act("audit");s.Act("listen");s.Act("map");
                    }
                    int before=s.MissionCount;s.BeginIncident();if(s.MissionCount>before)s.budget+=reward;
                    string response=policy==0?"scope":new[]{"contain","scope","recover"}.OrderBy(r=>{var e=s.Estimate(r);return (e.lossMax+e.cost)*2+e.stopMax;}).First();
                    s.Resolve(response);s.NextMonth();if(!s.Valid())throw new Exception("比較で状態破損");
                }
                wins+=s.IsClear?1:0;loss+=s.totalLoss;cash+=s.budget;missions+=s.MissionCount;levels+=s.levels.Sum();
            }
            Console.WriteLine("報酬="+reward+" 方針="+policy+" 完走="+wins+"/300 被害="+loss/300.0+" 残予算="+cash/300.0+" 依頼="+missions/300.0+" 導入段階="+levels/300.0);
        }
    }
}
