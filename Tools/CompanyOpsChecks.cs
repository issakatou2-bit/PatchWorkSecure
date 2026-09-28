using System;
using System.Linq;
using PatchWorkSecure.CompanyOps;

public static class CompanyOpsChecks
{
    private static void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
    public static void Prepare(OpsState s, int strategy)
    {
        string[] order = strategy == 1 ? new[] { "automation", "backup", "inventory", "drill", "education", "runbook", "mfa", "patch", "redundancy", "monitor", "segment" } :
            new[] { "education", "inventory", "backup", "runbook", "patch", "mfa", "drill", "automation", "monitor", "segment", "redundancy" };
        if (s.fatigue > 40) s.Act("rest");
        s.Act("listen");
        foreach (string key in order)
        {
            int i = OpsCatalog.Index(key);
            if (s.levels[i] != 0 || s.UpgradeBlock(i) != "") continue;
            if (s.capacity >= s.WorkCost(i) + 1 && !s.proposed)
                s.Act("proposal", OpsCatalog.Projects[i].group);
            if (s.budget >= s.Cost(i) + 12) s.Upgrade(i);
        }
        s.Act("audit"); s.Act("rest"); s.Act("map");
    }
    public static OpsState PlayYear(int seed, int strategy)
    {
        var s = new OpsState(seed);
        for (int i = 0; i < 12 && s.phase != OpsPhase.Ended; i++)
        {
            if (strategy != 0) Prepare(s, strategy);
            Check(s.Valid(), "計画時の状態破損");
            s.BeginIncident(); s.Resolve(strategy != 0 && s.Current.kind == "outage" ? "recover" : "scope"); s.NextMonth();
            Check(s.Valid(), "進行時の状態破損");
        }
        return s;
    }
    public static bool Simulate(int seed, int strategy) => PlayYear(seed, strategy).IsClear;
    public static void Main()
    {
        int[] wins = new int[3], scoreSum = new int[3], missionSum = new int[3];
        for (int i = 0; i < 300; i++) for (int j = 0; j < 3; j++)
        {
            var result = PlayYear(i, j);
            if (result.IsClear) wins[j]++;
            scoreSum[j] += result.AnnualScore;
            missionSum[j] += result.MissionCount;
        }
        Console.WriteLine("300年ずつ / 放置=" + wins[0] + ", 運用重視=" + wins[1] + ", 組織重視=" + wins[2]);
        Console.WriteLine("平均点 / 放置=" + scoreSum[0] / 300 + ", 運用重視=" + scoreSum[1] / 300 + ", 組織重視=" + scoreSum[2] / 300);
        Console.WriteLine("依頼達成平均 / 放置=" + missionSum[0] / 300 + ", 運用重視=" + missionSum[1] / 300 + ", 組織重視=" + missionSum[2] / 300);
        Check(wins[1] > wins[0] + 100 && wins[2] > wins[0] + 100, "備えが十分に生存へつながらない");
        Check(wins[1] > 150 && wins[2] > 150, "初回の失敗率が高すぎる");
        var s = new OpsState(14);
        Check(!s.Act("proposal"), "根拠なしで予算獲得");
        Check(!s.Upgrade(OpsCatalog.Index("drill")), "前提条件を無視");
        Check(s.Act("listen") && !s.Act("listen"), "対話の重複");
        s.Act("proposal", "recover"); s.Upgrade(OpsCatalog.Index("backup")); s.BeginIncident(); s.Resolve("scope");
        Check(s.Latest.promise.Contains("+5"), "予算の約束が反映されない");
        int cash = s.budget; Check(!s.Resolve("contain") && cash == s.budget, "二重精算");
        var a = new OpsState(7); var b = new OpsState(7); b.levels[OpsCatalog.Index("backup")] = 1;
        Check(a.Preview("scope").prevention == b.Preview("scope").prevention, "バックアップを侵入防止扱い");
        Check(a.Preview("scope").loss >= b.Preview("scope").loss, "復旧投資が逆効果");
        Console.WriteLine("境界・因果・年間進行: 成功");
        int comparisons=0;
        for(int seed=0;seed<30;seed++)foreach(string response in new[]{"contain","scope","recover"})
        {
            var state=new OpsState(seed,true);state.BeginIncident();var expected=state.Preview(response);int budget=state.budget;
            state.Resolve(response);Check(state.budget==budget-expected.loss-expected.cost,"演出用比較で精算が変わった");
            foreach(var effect in state.Latest.potentialInvestmentEffects)
            {
                var hypothetical=new OpsState(seed,true);hypothetical.levels[OpsCatalog.Index(effect.projectId)]=1;
                var preview=hypothetical.Preview(response);
                Check(effect.avoidedLoss==expected.loss-preview.loss&&effect.avoidedDowntime==expected.downtime-preview.downtime,"未導入の仮定比較が不一致");comparisons++;
            }
            Check(state.levels.All(v=>v==0)&&state.Valid(),"比較で未購入の設備を導入");
        }
        Console.WriteLine("発動演出の仮定比較: "+comparisons+"件一致 / 報酬・経験・精算の追加なし");
    }
}
