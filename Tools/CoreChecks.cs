using System;
using System.Linq;
using PatchWorkSecure;

// Unityのライセンスや描画に依存しない、実物のGameStateを使う再現可能な検証。
public static class CoreChecks
{
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
    }

    public static int Main()
    {
        var help = new GameState(1);
        var rest = new GameState(1);
        help.ResolveChore(true, 4);
        rest.ResolveChore(false, 4);
        Check(help.Trust == 34 && help.Stress == 23 && rest.Trust == 28 && rest.Stress == 14, "相談の効果");
        Check(help.Budget == rest.Budget && help.HelpedColleagues == 1, "収入と実績");
        var state = new GameState(2);
        foreach (var key in GameData.Defenses.Keys) Check(GamePresentation.DefenseDetail(state, key).Contains("予測被害"), "対策の説明");
        Check(state.Budget == 100 && state.DefenseLevels.Count == 0 && state.Stress == 20, "プレビューの非破壊性");
        state.Budget = 999;
        state.Trust = 80;
        state.Stress = 80;
        foreach (var key in GameData.Defenses.Keys) state.DefenseLevels[key] = 3;
        foreach (var key in GameData.Attacks.Keys)
        {
            Check(state.CalcDefenseRate(key) <= 0.70f, "基礎率の上限");
            Check(state.CalcFinalDefenseRate(key, GameData.Choices[0], 0.15f) <= 0.95f, "最終率の上限");
        }
        var result = state.ResolveAttack("phishing", GameData.Choices[0], 0.1f);
        Check(Math.Abs(result.StressPenalty - 20f / 300) < 0.0001, "判定前のストレス");
        Check(Math.Abs(result.FinalDefenseRate - Math.Clamp(result.EquipmentRate + result.ResponseBonus + result.ParryBonus
            + result.TrustBonus - result.StressPenalty, 0.02f, 0.95f)) < 0.0001, "結果内訳の一致");
        state.IsGameOver = true;
        int budget = state.Budget;
        state.ResolveChore(true, 6);
        state.AdvanceDay();
        Check(state.Budget == budget && state.Day == 1 && !state.UpgradeDefense("training"), "終了後の変更防止");
        int prepared = 0, unprepared = 0;
        const int runs = 1000;
        for (int seed = 0; seed < runs; seed++)
        {
            if (Simulate(seed, true)) prepared++;
            if (Simulate(seed, false)) unprepared++;
        }
        Check(prepared > unprepared + runs * 0.15, "備えによる生存率の改善");
        Console.WriteLine($"CORE CHECKS PASS / seeded annual runs: {runs} per strategy");
        Console.WriteLine($"Prepared + recovery: {prepared}/{runs}; no equipment: {unprepared}/{runs}");
        return 0;
    }

    private static bool Simulate(int seed, bool prepared)
    {
        var state = new GameState(seed);
        for (int turn = 0; turn < GameState.TotalPeriods && !state.IsGameOver; turn++)
        {
            if (prepared)
            {
                string key = GameData.Defenses.Keys
                    .Where(k => (!state.DefenseLevels.TryGetValue(k, out int lv) || lv < 3)
                        && GameData.Defenses[k].Levels[state.DefenseLevels.TryGetValue(k, out int v) ? v : 0].Cost <= state.Budget - 24)
                    .OrderBy(k => state.DefenseLevels.TryGetValue(k, out int v) ? v : 0)
                    .ThenBy(k => k == "training" ? 0 : k == "backup" ? 1 : 2).FirstOrDefault();
                if (key != null) state.UpgradeDefense(key);
            }
            state.ResolveChore(!prepared || state.Stress < 62 || state.Trust < 25, 4);
            if (state.IsGameOver) break;
            if (state.RollAttackOccurrence())
            {
                var key = state.RollAttackType();
                var choice = prepared && state.Budget > 40 && state.Stress < 70 ? GameData.Choices[0] : GameData.Choices[1];
                state.ResolveAttack(key, choice, prepared ? 0.10f : 0);
            }
            state.AdvanceDay();
            Check(state.Budget >= 0 && state.Budget <= 999 && state.Stress >= 0 && state.Stress <= 100
                && state.Trust >= 0 && state.Trust <= 100, "資源の範囲");
        }
        return state.IsCleared;
    }
}
