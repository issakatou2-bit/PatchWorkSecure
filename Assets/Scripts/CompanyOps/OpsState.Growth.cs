using System;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    /// <summary>同じ単位の抑制力だけを加算。金額・停止時間・経験値は混ぜない。</summary>
    [Serializable] public class OpsResponsePower
    {
        public int basic, equipment, field, player, staff;
        public string support;
        public int Total => basic + equipment + field + player + staff;
    }

    [Serializable] public class OpsGrowthResult
    {
        public int playerBefore, playerAfter, playerXp;
        public int[] staffBefore, staffAfter, staffXp;
        public string support, mentoring;
    }

    /// <summary>育成ルールは新規年度で有効。旧セーブの経験や結果を捏造しない。</summary>
    public partial class OpsState
    {
        public int growthRules, playerExperience;
        public int[] staffExperience;
        public string supportOrder = "";
        public bool practiced;
        public bool quarterRewardClaimed;
        public int monthExtraCapacity, nextMonthExtraCapacity;
        public int PlayerLevel => growthRules == 0 ? 1 : OpsGrowthCatalog.Level(playerExperience, OpsGrowthCatalog.PlayerThresholds);
        public int StaffLevel(int index) => growthRules == 0 || staffExperience == null ? 1 : OpsGrowthCatalog.Level(staffExperience[index], OpsGrowthCatalog.StaffThresholds);
        public int EquipmentLevel => 1 + levels.Sum() / 4;
        // 複数年の本編で、2年目以降の脅威の上乗せ。1年目・旧保存は0で、従来と同じ計算。
        public int yearPressure;
        public int SeasonPressure => (growthRules > 0 ? OpsGrowthCatalog.SeasonPressure[month] : 0) + yearPressure;
        public bool QuarterPeak => growthRules > 0 && new[] { 2, 5, 8, 11 }.Contains(month);
        public string SeasonLabel => growthRules == 0 ? "標準の運用" : QuarterPeak ? month == 11 ? "年度末の総力対応" : "四半期の山場" :
            month == 3 || month == 6 || month == 9 ? "山場後の整備期間" : month == 10 ? "年度末への備え" : "通常の運用";
        public bool QuarterRewardPending => growthRules > 0 && phase == OpsPhase.Review && month < 11 && QuarterPeak &&
            !quarterRewardClaimed && budget >= 0 && stability > 0;
        public bool ClaimQuarterReward(string reward)
        {
            if (!QuarterRewardPending || (reward != "budget" && reward != "capacity")) return false;
            quarterRewardClaimed = true;
            if (reward == "budget") budget += OpsGrowthCatalog.QuarterBudget;
            else nextMonthExtraCapacity = 1;
            rankQuarterBonusPaid=QuarterTrustBonus;budget+=rankQuarterBonusPaid;
            if(rankQuarterBonusPaid>0)Note("経営の信頼B以上 / 四半期の臨時予算 +"+rankQuarterBonusPaid+"万円。");
            Note("四半期の山場を完了。報酬 / " + (reward == "budget" ? "改善予算 +" + OpsGrowthCatalog.QuarterBudget + "万円" : "翌月の支援枠 +1工数") + "。");
            return true;
        }
        public int[] GrowthLevels => new[] { PlayerLevel, StaffLevel(0), StaffLevel(1), StaffLevel(2) };
        public string EffectiveSupport => growthRules == 0 ? "none" : supportOrder == "routine" ? "routine" :
            supportOrder == "investigate" || supportOrder == "recover" ? supportOrder :
            (DataRecoveryApplies || RestartApplies) && StaffLevel(2) >= 2 ? "recover" : "investigate";
        private int SupportMember => EffectiveSupport == "routine" ? 0 : EffectiveSupport == "recover" || Current.kind == "social" ? 2 : 1;
        public string SupportSummary => growthRules == 0 ? "旧年度 / 育成はニューゲームから" : EffectiveSupport == "routine" ?
            "小川：社員の手順案内を担当 / 今月の工数 +1" : EffectiveSupport == "recover" ?
            "森：業務データ・再開確認 / 復旧対応を支援" : Current.kind == "social" ?
            "森：依頼・承認記録の照合 / 調査対応を支援" : "佐伯：記録の整理・調査補助 / 調査対応を支援";
        public string SupportBlock(string order)
        {
            if (growthRules == 0) return "育成ルールは新しい年度で有効です";
            if (phase != OpsPhase.Planning) return "方針は計画中に決めます";
            if (!string.IsNullOrEmpty(supportOrder)) return "今月の方針は確定済み / 来月変更できます";
            if (!OpsGrowthCatalog.Orders.Contains(order)) return "不明な支援方針です";
            if (order != "auto" && PlayerLevel < 2) return "担当者Lv.2で方針指定を解放";
            if (order == "routine" && (StaffLevel(0) < 2 || Level("runbook") == 0)) return "小川Lv.2＋引継ぎ手順が必要";
            return "";
        }
        public bool AssignSupport(string order)
        {
            if (SupportBlock(order) != "") return false;
            supportOrder = order;
            if (order == "routine") capacity++;
            Note("支援方針「" + OpsGrowthCatalog.OrderName(order) + "」を確定。" + SupportSummary);
            return true;
        }
        public string PracticeBlock(int member)
        {
            if (growthRules == 0) return "育成ルールは新しい年度で有効です";
            if (member < 0 || member >= 3) return "不明な社員です";
            if (phase != OpsPhase.Planning) return "計画中に練習できます";
            if (practiced) return "共同練習は月1回です";
            if (staffExperience[member] >= OpsGrowthCatalog.StaffThresholds.Last()) return "この試作での習熟上限です";
            return capacity < 1 ? "今月の工数が足りません" : "";
        }
        public bool Practice(int member)
        {
            if (PracticeBlock(member) != "") return false;
            capacity--; practiced = true;
            int gained = GainStaff(member, OpsGrowthCatalog.PracticeXp);
            int playerGained = GainPlayer(1);
            Note(OpsGrowthCatalog.StaffNames[member] + "と共同練習。社員経験 +" + gained + " / 担当者経験 +" + playerGained + "。担当分野の手順を一緒に確かめた。");
            return true;
        }
        private int GainPlayer(int amount)
        {
            if (growthRules == 0) return 0;
            int old = playerExperience;
            playerExperience = Math.Min(OpsGrowthCatalog.PlayerThresholds.Last(), playerExperience + amount);
            return playerExperience - old;
        }
        private int GainStaff(int index, int amount)
        {
            if (growthRules == 0) return 0;
            int old = staffExperience[index];
            staffExperience[index] = Math.Min(OpsGrowthCatalog.StaffThresholds.Last(), old + amount);
            return staffExperience[index] - old;
        }
        private void GainFromWork(string action, string project = "")
        {
            if (growthRules == 0) return;
            if (action == "upgrade" || action == "audit" || action == "listen" || action == "map") GainPlayer(1);
            if (action == "listen" || project == "runbook") GainStaff(0, 1);
            if (action == "audit" || project == "monitor") GainStaff(1, 1);
            if (action == "map" || project == "drill") GainStaff(2, 1);
            if (project == "education") for (int i = 0; i < 3; i++) GainStaff(i, 1);
        }
        public OpsResponsePower ResponsePower(string response)
        {
            if (!new[] { "contain", "scope", "recover" }.Contains(response)) throw new ArgumentException("不明な対応");
            int cultural = CulturalPower;
            int containment = ContainmentPower;
            var p = new OpsResponsePower {
                basic = BasicResponsePower(response),
                equipment = Prevention() - cultural + containment,
                field = cultural,
                player = growthRules == 0 ? 0 : (PlayerLevel - 1) * OpsGrowthCatalog.PlayerPowerPerLevel,
                support = SupportSummary
            };
            if (response == "scope") { p.equipment += 4 * Level("inventory") + 5 * Level("monitor"); p.field += culture / 12 + (audited ? 8 : 0); }
            if (response == "recover") p.equipment += 3 * Level("runbook") +
                (DataRecoveryApplies ? OpsCatalog.RestorePowerPerLevel * RestoreChain : RestartApplies ? OpsCatalog.RestartPowerPerLevel * RestartChain : 0);
            if (growthRules > 0 && EffectiveSupport != "routine")
            {
                int skill = StaffLevel(SupportMember) - 1;
                if (EffectiveSupport == "recover" && response == "recover" && (DataRecoveryApplies || RestartApplies))
                    p.staff = skill * OpsGrowthCatalog.RecoverySupportPower;
                else if (EffectiveSupport == "investigate" && response != "recover")
                    p.staff = skill * OpsGrowthCatalog.InvestigationSupportPower;
                // 詳細な記録整理は、台帳・手順と熟練した情シス担当がそろった時だけ。
                if (EffectiveSupport == "investigate" && SupportMember == 1 && StaffLevel(1) == 3 && Level("inventory") > 0 && Level("runbook") > 0 && response != "recover")
                    p.staff += OpsGrowthCatalog.ForensicsSupportPower;
            }
            return p;
        }
        private void CompleteGrowth(OpsOutcome result)
        {
            if (growthRules == 0) return;
            var g = new OpsGrowthResult { playerBefore = PlayerLevel, staffBefore = new[] { StaffLevel(0), StaffLevel(1), StaffLevel(2) },
                staffXp = new int[3], support = SupportSummary, mentoring = "" };
            // 対応を振り返って成長。今回の結果には解決前のレベルを使う。
            g.playerXp = GainPlayer(OpsGrowthCatalog.ReviewXp);
            bool participated = EffectiveSupport == "routine" || result.power.staff > 0;
            if (participated) g.staffXp[SupportMember] += GainStaff(SupportMember, 1);
            int mentor = Array.FindIndex(g.staffBefore, level => level == 3);
            if (mentor >= 0 && Level("runbook") > 0)
            {
                int junior = Enumerable.Range(0, 3).Where(i => i != mentor).OrderBy(i => staffExperience[i]).First();
                int xp = GainStaff(junior, 1); g.staffXp[junior] += xp;
                if (xp > 0) g.mentoring = OpsGrowthCatalog.StaffNames[mentor] + "が" + OpsGrowthCatalog.StaffNames[junior] + "に手順を共有 / 経験 +1";
            }
            g.playerAfter = PlayerLevel; g.staffAfter = new[] { StaffLevel(0), StaffLevel(1), StaffLevel(2) };
            result.growth = g;
        }
        private bool ValidGrowth()
        {
            if(yearPressure<0||yearPressure>OpsCatalog.StoryPressures[OpsCatalog.StoryYears-1])return false;
            if (growthRules < 0 || growthRules > 1 || monthExtraCapacity < 0 || monthExtraCapacity > 1 || nextMonthExtraCapacity < 0 || nextMonthExtraCapacity > 1 ||
                (nextMonthExtraCapacity > 0 && !quarterRewardClaimed) || (quarterRewardClaimed && (!QuarterPeak || month == 11))) return false;
            if (history != null && !history.All(r => r != null && ValidGrowthResult(r))) return false;
            if (growthRules == 0) return monthExtraCapacity == 0 && nextMonthExtraCapacity == 0;
            if (playerExperience < 0 || playerExperience > OpsGrowthCatalog.PlayerThresholds.Last() || staffExperience == null || staffExperience.Length != 3 ||
                staffExperience.Any(n => n < 0 || n > OpsGrowthCatalog.StaffThresholds.Last()) ||
                (!string.IsNullOrEmpty(supportOrder) && !OpsGrowthCatalog.Orders.Contains(supportOrder))) return false;
            if (supportOrder == "routine" && (PlayerLevel < 2 || StaffLevel(0) < 2 || Level("runbook") == 0)) return false;
            return true;
        }
        private static bool ValidGrowthResult(OpsOutcome r)
        {
            if (r.power != null && (new[] { r.power.basic, r.power.equipment, r.power.field, r.power.player, r.power.staff }.Any(n => n < 0 || n > 200) ||
                r.power.player > 8 || r.power.staff > 6 || r.power.support == null || r.power.support.Length > 200)) return false;
            var g = r.growth; if (g == null) return true;
            return g.playerBefore >= 1 && g.playerAfter >= g.playerBefore && g.playerAfter <= 5 && g.playerXp >= 0 && g.playerXp <= OpsGrowthCatalog.ReviewXp &&
                g.staffBefore != null && g.staffAfter != null && g.staffXp != null && g.staffBefore.Length == 3 && g.staffAfter.Length == 3 && g.staffXp.Length == 3 &&
                Enumerable.Range(0, 3).All(i => g.staffBefore[i] >= 1 && g.staffAfter[i] >= g.staffBefore[i] && g.staffAfter[i] <= 3 && g.staffXp[i] >= 0 && g.staffXp[i] <= 2) &&
                g.support != null && g.support.Length <= 200 && g.mentoring != null && g.mentoring.Length <= 200;
        }
    }

    /// <summary>育成の数値・表示名。資格や現実の能力を測るレベルではない。</summary>
    public static class OpsGrowthCatalog
    {
        public static readonly int[] PlayerThresholds = { 0, 4, 10, 18, 28 };
        public static readonly int[] StaffThresholds = { 0, 3, 8 };
        public const int PlayerPowerPerLevel = 2, InvestigationSupportPower = 2, RecoverySupportPower = 3, ForensicsSupportPower = 2;
        public const int PracticeXp = 2, ReviewXp = 1;
        public const int QuarterBudget = 10;
        // 成長に合わせた追従補正ではなく、全プレイヤー共通の年間負荷。
        public static readonly int[] SeasonPressure = { 0, 0, 4, 0, 0, 6, 4, 6, 12, 4, 14, 20 };
        public static readonly string[] StaffNames = { "小川", "佐伯", "森" };
        public static readonly string[] StaffRoles = { "総務 / 社員の窓口", "情シス / 調査補助", "経理 / 業務確認" };
        public static readonly string[] Orders = { "auto", "routine", "investigate", "recover" };
        public static int Level(int xp, int[] thresholds) => thresholds.Count(n => xp >= n);
        public static string OrderName(string order) => order == "routine" ? "日常対応" : order == "investigate" ? "調査補助" : order == "recover" ? "復旧支援" : "おまかせ";
        public static string ExperienceText(int xp, int[] thresholds)
        {
            int lv = Level(xp, thresholds);
            return lv == thresholds.Length ? "経験 MAX" : "経験 " + (xp - thresholds[lv - 1]) + " / " + (thresholds[lv] - thresholds[lv - 1]);
        }
    }
}
