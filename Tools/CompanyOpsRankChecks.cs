using System;
using System.Linq;
using PatchWorkSecure.CompanyOps;
public static class CompanyOpsRankChecks
{
    public static void Main()
    {
        foreach(int v in new[]{49,50,64,65})
        {
            var s=new OpsState(14,true){culture=v,trust=v};
            if(s.ProposalRankBonus!=(v>=50?1:0)||s.QuarterTrustBonus!=(v>=65?1:0)||s.CultureEarlySignal!=(v>=50)||s.ForecastRankPrecision!=(v>=65?1:0))throw new Exception("恩恵境界");
            s.audited=true;if(s.EstimateMargin!=2)throw new Exception("調査と精度の二重加算");
            s.rankBenefitRules=0;if(s.ProposalRankBonus!=0||s.QuarterTrustBonus!=0||s.CultureEarlySignal||s.ForecastRankPrecision!=0)throw new Exception("旧年度への恩恵混入");
        }
        var quarter=new OpsState(14){month=2,phase=OpsPhase.Review,trust=65};int cash=quarter.budget;quarter.ClaimQuarterReward("capacity");if(quarter.budget!=cash+1||quarter.ClaimQuarterReward("budget"))throw new Exception("四半期二重付与");
        var proposal=new OpsState(14){trust=50};proposal.Act("audit");cash=proposal.budget;proposal.Act("proposal");if(proposal.budget!=cash+16)throw new Exception("信頼の提案上乗せ");
        var maximum=new OpsState(14){trust=65,capacity=8};maximum.Act("audit");maximum.Act("listen");maximum.Act("map");maximum.Act("proposal");maximum.capacity=0;if(maximum.proposalGrant!=22||!maximum.Valid())throw new Exception("最大提案の保存上限");
        proposal.BeginIncident();proposal.Resolve("scope");if(proposal.Latest.promise.IndexOf("返却")<0)throw new Exception("約束未達の返却");
        string[] order={"inventory","mfa","backup","education","runbook","patch","monitor","drill","automation","segment","redundancy"};
        foreach(bool enabled in new[]{false,true})for(int policy=0;policy<3;policy++)
        {
            int wins=0,cashSum=0,loss=0;
            for(int seed=0;seed<300;seed++)
            {
                var s=new OpsState(seed,true);s.rankBenefitRules=enabled?1:0;
                for(int m=0;m<12&&s.phase!=OpsPhase.Ended;m++)
                {
                    if(policy>0)
                    {
                        if(s.fatigue>40)s.Act("rest");if(policy==2){s.Act(s.CurrentMission.actionA);s.Act(s.CurrentMission.actionB);}else s.Act("listen");
                        foreach(string id in order){int i=OpsCatalog.Index(id);if(s.Level(id)<(policy==2?2:1)&&s.budget>=s.Cost(i)+12)s.Upgrade(i);}
                        s.Act("audit");s.Act("listen");s.Act("map");
                    }
                    s.BeginIncident();string response=policy==0?"scope":new[]{"contain","scope","recover"}.OrderBy(r=>{var e=s.Estimate(r);return (e.lossMax+e.cost)*2+e.stopMax;}).First();
                    s.Resolve(response);s.NextMonth();if(!s.Valid())throw new Exception("ランク比較の状態破損");
                }
                wins+=s.IsClear?1:0;cashSum+=s.budget;loss+=s.totalLoss;
            }
            Console.WriteLine("ランク恩恵="+enabled+" 方針="+policy+" 完走="+wins+"/300 残予算="+cashSum/300.0+" 被害="+loss/300.0);
        }
        Console.WriteLine("境界・旧年度・二重加算・四半期・約束返却の検証成功");
    }
}
