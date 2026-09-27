using System;
using System.Linq;
using System.Text.RegularExpressions;
using PatchWorkSecure.CompanyOps;

// 仮想試遊は行動規則の検証。人間の感想や操作の楽しさを測るものではない。
public static class CompanyOpsPlaytest
{
    static readonly string[] Responses = { "contain", "scope", "recover" };
    static readonly string[][] Orders = {
        new[] { "backup", "drill", "runbook", "inventory", "redundancy", "patch", "automation", "mfa", "education", "monitor", "segment" },
        new[] { "inventory", "education", "monitor", "mfa", "patch", "runbook", "automation", "backup", "drill", "segment", "redundancy" },
        new[] { "automation", "runbook", "education", "inventory", "patch", "mfa", "backup", "drill", "monitor", "segment", "redundancy" }
    };
    static string PublicResponse(OpsState s)
    {
        // UIで見える予測の上限のみ。シード、隠された真相、Previewは参照しない。
        return Responses.OrderBy(r => {
            var n = Regex.Matches(s.Forecast(r), @"\d+").Cast<Match>().Select(x => int.Parse(x.Value)).ToArray();
            return (n[1] + (r == "contain" ? 6 : r == "scope" ? 3 : 4)) * 2 + n[3];
        }).First();
    }
    static void Plan(OpsState s, int policy)
    {
        if (policy == 0) return;
        if (s.fatigue > 35) s.Act("rest");
        if (policy == 4)
        {
            var m = OpsCatalog.Missions[s.month];
            s.Act(m.actionA); s.Act(m.actionB);
        }
        else if (policy == 2) s.Act("audit");
        else s.Act("listen");
        var order = Orders[Math.Min(2, policy - 1)];
        // Lv.1を揃えたら育成好きはLv.2も目指す。固定順と無差別投資の弱さも計測。
        foreach (string key in order)
        {
            int i = OpsCatalog.Index(key);
            if (s.levels[i] >= (policy == 3 ? 2 : 1) || s.UpgradeBlock(i) != "") continue;
            if (s.capacity >= s.WorkCost(i) + 1 && !s.proposed && s.Evidence > 0)
                s.Act("proposal", OpsCatalog.Projects[i].group);
            if (s.budget >= s.Cost(i) + 12) s.Upgrade(i);
        }
        if (s.SituationFatigue > 0 || s.Situation.stopLossCap > 0) s.Act("prepare");
        s.Act("audit"); s.Act("listen");
        if (s.fatigue > 12) s.Act("rest");
        s.Act("map");
    }
    public static void Main()
    {
        string[] labels = { "放置", "復旧優先", "限定対応優先", "設備Lv2優先", "社内依頼優先" };
        for (int policy = 0; policy < labels.Length; policy++)
        {
            int wins=0, points=0, losses=0, stops=0, months=0, unused=0, fatigue=0, missions=0, emptyGrowth=0;
            int[] responseCounts = new int[3];
            int dominated=0, alternatives=0;
            for (int seed=0; seed<500; seed++)
            {
                var s = new OpsState(seed);
                for (int m=0; m<12 && s.phase!=OpsPhase.Ended; m++)
                {
                    int previous=s.levels.Sum();
                    Plan(s, policy);
                    if (m>=6 && previous==s.levels.Sum()) emptyGrowth++;
                    unused+=s.capacity; months++;
                    s.BeginIncident();
                    string response=policy==0?"scope":PublicResponse(s);
                    responseCounts[Array.IndexOf(Responses,response)]++;
                    // 以下は事後の診断専用。行動選択には使わない。
                    var results=Responses.Select(r=>s.Preview(r)).ToArray();
                    for(int i=0;i<3;i++)
                    {
                        alternatives++;
                        if(results.Where((r,j)=>j!=i).Any(r=>r.loss+r.cost<=results[i].loss+results[i].cost && r.downtime<=results[i].downtime &&
                            (r.loss+r.cost<results[i].loss+results[i].cost || r.downtime<results[i].downtime))) dominated++;
                    }
                    s.Resolve(response); s.NextMonth();
                    if(!s.Valid()) throw new Exception("状態破損: "+labels[policy]+"/"+seed+"/"+m);
                }
                wins+=s.IsClear?1:0; points+=s.AnnualScore; losses+=s.totalLoss; stops+=s.totalDowntime; fatigue+=s.fatigue; missions+=s.MissionCount;
            }
            Console.WriteLine(labels[policy]+" / 完走 "+wins+"/500 / 平均点 "+points/500+" / 被害 "+losses/500.0+" / 停止 "+stops/500.0+
                " / 未使用工数/月 "+Math.Round(unused/(double)months,2)+" / 疲労 "+fatigue/500+" / 依頼 "+missions/500.0+
                " / 後半投資なし月 "+emptyGrowth+" / 遮断:限定:復旧 "+string.Join(":",responseCounts)+
                " / 他案より出費・停止で劣る案 "+dominated+"/"+alternatives);
        }
        // 最初の3か月だけ、同じ工数で二つの導入順を比較。正解ルートの証明ではない。
        for (int plan = 0; plan < 2; plan++)
        {
            int[] loss = new int[3], stop = new int[3], cash = new int[3], responseCount = new int[3];
            for (int seed = 0; seed < 500; seed++)
            {
                var s = new OpsState(seed);
                for (int m = 0; m < 3; m++)
                {
                    string[] ids = m == 0 ? (plan == 0 ? new[] { "backup", "drill", "runbook" } : new[] { "inventory", "monitor", "education" }) :
                        m == 1 ? new[] { "mfa", "education" } : new[] { "runbook", "patch" };
                    foreach (string id in ids) if (s.Level(id) == 0) s.Upgrade(OpsCatalog.Index(id));
                    s.Act("listen"); s.Act("audit"); s.Act("map");
                    s.BeginIncident(); string chosen = PublicResponse(s); responseCount[Array.IndexOf(Responses, chosen)]++;
                    s.Resolve(chosen); loss[m] += s.Latest.loss; stop[m] += s.Latest.downtime; cash[m] += s.budget; s.NextMonth();
                    if (!s.Valid()) throw new Exception("3か月比較の状態破損");
                }
            }
            Console.WriteLine("3か月比較 / " + (plan == 0 ? "復元連携から開始" : "台帳・監視・教育から開始") +
                " / 月別被害 " + string.Join("/",loss.Select(x=>x/500.0)) + " / 月別停止 " + string.Join("/",stop.Select(x=>x/500.0)) +
                " / 月末予算 " + string.Join("/",cash.Select(x=>x/500.0)) + " / 遮断:限定:復旧 " + string.Join(":",responseCount));
        }
    }
}
