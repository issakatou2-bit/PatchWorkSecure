using System;
using System.Linq;
using System.Text.RegularExpressions;
using PatchWorkSecure.CompanyOps;

// 社員支援を使う仮想プレイヤー。公開予測以外の真値で対応を選ばない。
public static class CompanyOpsGrowthChecks
{
    static readonly string[] Responses = { "contain", "scope", "recover" };
    static readonly string[][] Orders = {
        new[] { "education", "runbook", "backup", "drill", "inventory", "mfa", "patch", "automation", "monitor", "redundancy", "segment" },
        new[] { "inventory", "education", "monitor", "runbook", "mfa", "patch", "backup", "drill", "automation", "redundancy", "segment" },
        new[] { "backup", "drill", "runbook", "education", "inventory", "mfa", "patch", "automation", "redundancy", "monitor", "segment" }
    };
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static string Choice(OpsState s) => Responses.OrderBy(r =>
    {
        var n = Regex.Matches(s.Forecast(r), @"\d+").Cast<Match>().Select(m => int.Parse(m.Value)).ToArray();
        return 2 * (n[1] + (r == "contain" ? 6 : r == "scope" ? 3 : 4)) + n[3];
    }).First();
    public static void Main()
    {
        for (int policy = 0; policy < 3; policy++)
        {
            int wins = 0, assistance = 0, extraWork = 0, mentors = 0, trained = 0;
            int[] loss = new int[12], stop = new int[12], samples = new int[12], staffLevels = new int[3], responseCount = new int[3];
            for (int seed = 0; seed < 300; seed++)
            {
                var s = new OpsState(seed);
                for (int m = 0; m < 12 && s.phase != OpsPhase.Ended; m++)
                {
                    if (policy == 0 && s.AssignSupport("routine")) extraWork++;
                    else if (policy == 1) s.AssignSupport("investigate");
                    else if (policy == 2 && (s.DataRecoveryApplies || s.RestartApplies)) s.AssignSupport("recover");
                    if (s.fatigue > 40) s.Act("rest");
                    if (s.StaffLevel(policy) < 2 && s.capacity >= 3) { if (s.Practice(policy)) trained++; }
                    foreach (string id in Orders[policy])
                    {
                        int i = OpsCatalog.Index(id);
                        if (s.Level(id) < (m >= 8 ? 2 : 1) && s.budget >= s.Cost(i) + 12) s.Upgrade(i);
                    }
                    s.Act(policy == 0 ? "listen" : policy == 1 ? "audit" : "map");
                    s.Act("audit"); s.Act("listen"); s.Act("map");
                    if (s.capacity > 0 && s.StaffLevel(policy) < 3 && s.Practice(policy)) trained++;
                    Check(s.Valid(), "育成計画で状態破損");
                    s.BeginIncident(); string response = Choice(s); responseCount[Array.IndexOf(Responses, response)]++;
                    int sum = s.ResponsePower(response).Total;
                    s.Resolve(response);
                    Check(s.Latest.power.Total == sum, "確定結果の加算と表示が不一致");
                    assistance += s.Latest.power.staff;
                    if (!string.IsNullOrEmpty(s.Latest.growth.mentoring)) mentors++;
                    loss[m] += s.Latest.loss; stop[m] += s.Latest.downtime; samples[m]++;
                    if (s.QuarterRewardPending) s.ClaimQuarterReward(policy == 0 ? "capacity" : "budget");
                    s.NextMonth(); Check(s.Valid(), "育成・報酬の進行で状態破損");
                }
                if (s.IsClear) wins++;
                for (int i = 0; i < 3; i++) staffLevels[i] += s.StaffLevel(i);
            }
            Console.WriteLine(new[] { "日常委任", "調査班育成", "復旧支援育成" }[policy] + " / 完走 " + wins + "/300 / 社員抑制力合計 " + assistance +
                " / 日常委任回数 " + extraWork + " / 共同練習 " + trained + " / 社員間共有 " + mentors +
                " / 最終社員Lv平均 " + string.Join("/", staffLevels.Select(n => Math.Round(n / 300.0, 2))) +
                " / 遮断:限定:復旧 " + string.Join(":", responseCount));
            Console.WriteLine("月別被害 / " + string.Join("/", loss.Select((n, i) => samples[i] == 0 ? 0 : Math.Round(n / (double)samples[i], 2))) +
                " / 月別停止 / " + string.Join("/", stop.Select((n, i) => samples[i] == 0 ? 0 : Math.Round(n / (double)samples[i], 2))));
            Check(mentors > 0 && trained > 0, "社員の育成と手順共有が使われていない");
            Check(policy == 0 ? extraWork > 0 : assistance > 0, "方針に応じた助力が活用されていない");
            Check(wins >= 150, "育成投資の初年度が厳しすぎる");
        }
        // 同じ会社・同じ3月で、支援なしとの反実仮想。選択には使わず効果の診断だけ。
        int avoided = 0, stopped = 0;
        for (int seed = 0; seed < 300; seed++)
        {
            var s = new OpsState(seed) { month = 11, playerExperience = 18, supportOrder = "recover" };
            s.staffExperience[2] = 8;
            foreach (string id in new[] { "backup", "drill", "runbook", "patch" }) s.levels[OpsCatalog.Index(id)] = 1;
            var assisted = s.Preview("recover");
            s.staffExperience[2] = 0;
            var alone = s.Preview("recover");
            avoided += alone.loss - assisted.loss; stopped += alone.downtime - assisted.downtime;
        }
        Console.WriteLine("3月の同条件比較 / 熟練社員の復旧支援による平均軽減 被害 " + avoided / 300.0 + "万円・停止 " + stopped / 300.0 + "h");
        Check(avoided > 0 && stopped > 0, "終盤に支援の実効性がない");
    }
}
