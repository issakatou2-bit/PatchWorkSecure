using System.Collections.Generic;

namespace PatchWorkSecure
{
    /// <summary>攻撃の格付け。IPA情報セキュリティ10大脅威2026の継続年数・初選出年に基づく。</summary>
    public enum AttackGrade
    {
        None,       // 定番（格付けなし）
        S,          // 絶対王者：ランサム攻撃（11年連続首位）
        Veteran,    // 古参：標的型攻撃・内部不正（11年連続）
        MidTier,    // 中堅：サプライチェーン・BEC・脆弱性・リモートワーク
        Rising,     // 新興：地政学的リスク
        Rookie      // 新人：AIの利用をめぐるサイバーリスク（2026年初選出）
    }

    /// <summary>攻撃ごとの特殊ギミック。</summary>
    public enum AttackSpecial
    {
        None,
        MultiDefenseRequired,   // 対策が少ないと効果が大幅に割り引かれる
        TechIgnored,            // 技術対策がほぼ効かない（人望/ストレスが本体）
        DefenseDampen,          // 既存対策の効果を一律減衰
        VpnCritical,            // VPN未導入だと大幅に不利
        Composite               // 複合技：対策の種類が少ないほど不利
    }

    [System.Serializable]
    public class AttackType
    {
        public string Key;
        public string DisplayName;
        public AttackGrade Grade;
        public AttackSpecial Special;
        public float BaseChance;
        public int DamageBudget;
        public int DamageTrust;
        public string LineIntro;
        public string LineWin;
        public string LineLose;
        public string[] ExcludeDefenses = new string[0];
        public string ScTerm;
        public string ScNote;
    }

    [System.Serializable]
    public class DefenseLevel
    {
        public int Cost;
        public int StressCost;

        public DefenseLevel(int cost, int stressCost)
        {
            Cost = cost;
            StressCost = stressCost;
        }
    }

    // Dictionary<string,float>を持つためUnityのInspector用シリアライズ非対応。
    // GameDataはInspectorに表示しない純粋staticデータなので[System.Serializable]は不要。
    public class DefenseType
    {
        public string Key;
        public string DisplayName;
        public List<DefenseLevel> Levels;
        public Dictionary<string, float> BaseEffect;
        public bool IsDamageMitigation; // バックアップ：防御率ではなく被害軽減に寄与
        public string ScTerm;
        public string ScNote;
    }

    /// <summary>
    /// 攻撃・防御のマスターデータ。
    /// Reactプロトタイプで検証済みの数値をそのまま移植している。
    /// バランス調整はこのクラスの数値を書き換えるだけで完結する。
    /// </summary>
    public static class GameData
    {
        public static readonly Dictionary<string, AttackType> Attacks = new Dictionary<string, AttackType>
        {
            ["phishing"] = new AttackType
            {
                Key = "phishing", DisplayName = "フィッシング", Grade = AttackGrade.None,
                BaseChance = 0.30f, DamageBudget = 12, DamageTrust = 8,
                LineIntro = "怪しいメールが届いている…",
                LineWin = "よくある手口だ、想定内。",
                LineLose = "まんまと引っかかった社員がいる…",
                ScTerm = "ソーシャルエンジニアリング",
                ScNote = "技術ではなく人の心理的な隙を突く攻撃手法の総称"
            },
            ["ransomware"] = new AttackType
            {
                Key = "ransomware", DisplayName = "ランサム(仮)", Grade = AttackGrade.S,
                Special = AttackSpecial.MultiDefenseRequired,
                BaseChance = 0.18f, DamageBudget = 35, DamageTrust = 18,
                LineIntro = "……もう遅い。データは、頂いた。",
                LineWin = "今回は、退いてやる。",
                LineLose = "もう、遅い。支払っても、戻らないよ。",
                ScTerm = "二重恐喝(ダブルエクストーション)",
                ScNote = "暗号化に加えデータ公開もちらつかせ支払いを迫る手口"
            },
            ["ddos"] = new AttackType
            {
                Key = "ddos", DisplayName = "DDoS攻撃", Grade = AttackGrade.None,
                BaseChance = 0.15f, DamageBudget = 18, DamageTrust = 6,
                LineIntro = "大量のアクセスが押し寄せてくる…",
                LineWin = "今回は数が足りなかったようだな。",
                LineLose = "サービスが完全に落ちた…",
                ScTerm = "可用性(Availability)",
                ScNote = "CIA(機密性・完全性・可用性)のうち、サービス提供の継続性を指す"
            },
            ["insider"] = new AttackType
            {
                Key = "insider", DisplayName = "内部者(仮)", Grade = AttackGrade.Veteran,
                Special = AttackSpecial.TechIgnored,
                BaseChance = 0.12f, DamageBudget = 22, DamageTrust = 22,
                LineIntro = "……ねえ、誰も見てないよね？",
                LineWin = "まあ、今回はやめとくか。",
                LineLose = "別に、恨みなんてないけどさ。",
                ScTerm = "性弱説とゼロトラスト",
                ScNote = "人は不注意を完全には無くせない前提で、社内外問わず都度検証する考え方"
            },
            ["aiRisk"] = new AttackType
            {
                Key = "aiRisk", DisplayName = "模倣者(仮)", Grade = AttackGrade.Rookie,
                Special = AttackSpecial.DefenseDampen,
                BaseChance = 0.15f, DamageBudget = 20, DamageTrust = 12,
                LineIntro = "私、本物の連絡ですよ……たぶん。",
                LineWin = "まだ、これくらいしか化けれない。",
                LineLose = "あれ、本物と区別つかなかったでしょ？",
                ScTerm = "シャドーAI",
                ScNote = "会社が把握・許可していないAIツールを従業員が無断利用するリスク"
            },
            ["supplyChain"] = new AttackType
            {
                Key = "supplyChain", DisplayName = "委託(仮)", Grade = AttackGrade.MidTier,
                BaseChance = 0.12f, DamageBudget = 25, DamageTrust = 14,
                ExcludeDefenses = new[] { "firewall" },
                LineIntro = "お前じゃない、お前の取引先が甘いんだよ。",
                LineWin = "ちっ、監査までされてたか。",
                LineLose = "弱いところから、頂くだけさ。",
                ScTerm = "サードパーティリスク",
                ScNote = "自社ではなく取引先・委託先の脆弱性が侵入経路になるリスク"
            },
            ["bec"] = new AttackType
            {
                Key = "bec", DisplayName = "詐欺(仮)", Grade = AttackGrade.MidTier,
                BaseChance = 0.12f, DamageBudget = 20, DamageTrust = 10,
                LineIntro = "至急のご送金、お願いします。",
                LineWin = "ダブルチェックとは、面倒な。",
                LineLose = "振込、確認しました。ありがとう。",
                ScTerm = "なりすまし(スプーフィング)",
                ScNote = "送信者を偽装し、正規の依頼であるかのように見せかける手口"
            },
            ["vuln"] = new AttackType
            {
                Key = "vuln", DisplayName = "隙間(仮)", Grade = AttackGrade.MidTier,
                BaseChance = 0.13f, DamageBudget = 20, DamageTrust = 8,
                LineIntro = "パッチが当たる前に、失礼するよ。",
                LineWin = "もう塞がれてたか、早いな。",
                LineLose = "一瞬の隙間、頂いた。",
                ScTerm = "ゼロデイ/Nデイ脆弱性",
                ScNote = "修正パッチ公開前(ゼロデイ)か、公開後も未適用(Nデイ)かの違い"
            },
            ["remote"] = new AttackType
            {
                Key = "remote", DisplayName = "裏口(仮)", Grade = AttackGrade.MidTier,
                Special = AttackSpecial.VpnCritical,
                BaseChance = 0.13f, DamageBudget = 22, DamageTrust = 10,
                LineIntro = "在宅の設定、甘いままだよね？",
                LineWin = "裏口は閉まってたか。",
                LineLose = "開けっぱなしの入口、助かるよ。",
                ScTerm = "境界防御の限界",
                ScNote = "社外から働く前提では、社内=安全という境界の考え方が通用しなくなる"
            },
            ["geopolitical"] = new AttackType
            {
                Key = "geopolitical", DisplayName = "使者(仮)", Grade = AttackGrade.Rising,
                Special = AttackSpecial.Composite,
                BaseChance = 0.10f, DamageBudget = 28, DamageTrust = 16,
                LineIntro = "個人的な恨みはない。指示があるだけだ。",
                LineWin = "今回は撤退する。次がある。",
                LineLose = "これは、始まりに過ぎない。",
                ScTerm = "APT(高度で持続的な脅威)",
                ScNote = "組織的な背景を持ち、長期間にわたり執拗に狙い続ける攻撃者像"
            },
        };

        public static readonly Dictionary<string, DefenseType> Defenses = new Dictionary<string, DefenseType>
        {
            ["mfa"] = new DefenseType
            {
                Key = "mfa", DisplayName = "MFA(多要素認証)",
                Levels = new List<DefenseLevel> { new DefenseLevel(15, 2), new DefenseLevel(25, 2), new DefenseLevel(40, 3) },
                BaseEffect = new Dictionary<string, float>
                {
                    ["phishing"] = 0.15f, ["ransomware"] = 0.03f, ["ddos"] = 0f, ["insider"] = 0.06f,
                    ["aiRisk"] = 0.05f, ["supplyChain"] = 0.05f, ["bec"] = 0.18f, ["vuln"] = 0.05f,
                    ["remote"] = 0.10f, ["geopolitical"] = 0.04f
                },
                ScTerm = "多要素認証(MFA)",
                ScNote = "知識・所持・生体など異なる要素を組み合わせる認証強化"
            },
            ["firewall"] = new DefenseType
            {
                Key = "firewall", DisplayName = "Firewall",
                Levels = new List<DefenseLevel> { new DefenseLevel(20, 3), new DefenseLevel(30, 4), new DefenseLevel(45, 5) },
                BaseEffect = new Dictionary<string, float>
                {
                    ["phishing"] = 0.03f, ["ransomware"] = 0.10f, ["ddos"] = 0.20f, ["insider"] = 0f,
                    ["aiRisk"] = 0.03f, ["supplyChain"] = 0.08f, ["bec"] = 0.03f, ["vuln"] = 0.18f,
                    ["remote"] = 0.06f, ["geopolitical"] = 0.06f
                },
                ScTerm = "ファイアウォール",
                ScNote = "通信を許可/拒否のルールで制御する境界防御の基本"
            },
            ["training"] = new DefenseType
            {
                Key = "training", DisplayName = "社員教育",
                Levels = new List<DefenseLevel> { new DefenseLevel(10, -1), new DefenseLevel(18, -1), new DefenseLevel(28, -1) },
                BaseEffect = new Dictionary<string, float>
                {
                    ["phishing"] = 0.10f, ["ransomware"] = 0.03f, ["ddos"] = 0f, ["insider"] = 0.10f,
                    ["aiRisk"] = 0.08f, ["supplyChain"] = 0.12f, ["bec"] = 0.15f, ["vuln"] = 0.05f,
                    ["remote"] = 0.08f, ["geopolitical"] = 0.05f
                },
                ScTerm = "セキュリティ意識向上(SETA)",
                ScNote = "Security Education, Training and Awarenessの略。人的対策の柱"
            },
            ["waf"] = new DefenseType
            {
                Key = "waf", DisplayName = "WAF",
                Levels = new List<DefenseLevel> { new DefenseLevel(18, 2), new DefenseLevel(28, 2), new DefenseLevel(42, 3) },
                BaseEffect = new Dictionary<string, float>
                {
                    ["phishing"] = 0f, ["ransomware"] = 0f, ["ddos"] = 0.15f, ["insider"] = 0f,
                    ["aiRisk"] = 0.02f, ["supplyChain"] = 0.05f, ["bec"] = 0f, ["vuln"] = 0.12f,
                    ["remote"] = 0.03f, ["geopolitical"] = 0.08f
                },
                ScTerm = "WAF(Web Application Firewall)",
                ScNote = "Webアプリ層の通信を検査する専用ファイアウォール"
            },
            ["idsIps"] = new DefenseType
            {
                Key = "idsIps", DisplayName = "IDS/IPS",
                Levels = new List<DefenseLevel> { new DefenseLevel(22, 3), new DefenseLevel(32, 3), new DefenseLevel(48, 4) },
                BaseEffect = new Dictionary<string, float>
                {
                    ["phishing"] = 0.03f, ["ransomware"] = 0.05f, ["ddos"] = 0.05f, ["insider"] = 0.05f,
                    ["aiRisk"] = 0.03f, ["supplyChain"] = 0.08f, ["bec"] = 0f, ["vuln"] = 0.08f,
                    ["remote"] = 0.07f, ["geopolitical"] = 0.10f
                },
                ScTerm = "IDS/IPS",
                ScNote = "侵入検知(Detection)と侵入防御(Prevention)、似て非なる機能"
            },
            ["backup"] = new DefenseType
            {
                Key = "backup", DisplayName = "バックアップ",
                Levels = new List<DefenseLevel> { new DefenseLevel(12, 0), new DefenseLevel(20, 0), new DefenseLevel(32, 0) },
                BaseEffect = new Dictionary<string, float>
                {
                    ["phishing"] = 0f, ["ransomware"] = 0f, ["ddos"] = 0f, ["insider"] = 0f,
                    ["aiRisk"] = 0f, ["supplyChain"] = 0f, ["bec"] = 0f, ["vuln"] = 0f,
                    ["remote"] = 0f, ["geopolitical"] = 0f
                },
                IsDamageMitigation = true,
                ScTerm = "事業継続計画(BCP)",
                ScNote = "被害後も事業を止めずに復旧させるための備え。防御ではなく復旧の手段"
            },
            ["vpn"] = new DefenseType
            {
                Key = "vpn", DisplayName = "VPN",
                Levels = new List<DefenseLevel> { new DefenseLevel(14, 1), new DefenseLevel(22, 1), new DefenseLevel(34, 2) },
                BaseEffect = new Dictionary<string, float>
                {
                    ["phishing"] = 0.05f, ["ransomware"] = 0.02f, ["ddos"] = 0f, ["insider"] = 0f,
                    ["aiRisk"] = 0.03f, ["supplyChain"] = 0.06f, ["bec"] = 0.05f, ["vuln"] = 0.03f,
                    ["remote"] = 0.22f, ["geopolitical"] = 0.05f
                },
                ScTerm = "VPN(Virtual Private Network)",
                ScNote = "公衆回線上に仮想的な専用通信路を作り、通信を保護する技術"
            },
            ["passwordPolicy"] = new DefenseType
            {
                Key = "passwordPolicy", DisplayName = "パスワードポリシー",
                Levels = new List<DefenseLevel> { new DefenseLevel(8, 2), new DefenseLevel(14, 2), new DefenseLevel(22, 3) },
                BaseEffect = new Dictionary<string, float>
                {
                    ["phishing"] = 0.08f, ["ransomware"] = 0.02f, ["ddos"] = 0f, ["insider"] = 0.04f,
                    ["aiRisk"] = 0.06f, ["supplyChain"] = 0.03f, ["bec"] = 0.08f, ["vuln"] = 0.02f,
                    ["remote"] = 0.09f, ["geopolitical"] = 0.04f
                },
                ScTerm = "認証情報管理",
                ScNote = "パスワードの使い回し・単純化を防ぐ運用ルール"
            },
        };

        /// <summary>攻撃対応の選択肢。それぞれコストと防御補正が異なる。</summary>
        public static readonly List<AttackChoice> Choices = new List<AttackChoice>
        {
            new AttackChoice { Id = "block", Label = "即座に遮断する", Description = "緊急対応で確実性重視", DefenseBonus = 0.25f, BudgetCost = 8, StressCost = 6 },
            new AttackChoice { Id = "investigate", Label = "調査してから対応する", Description = "慎重に見極める", DefenseBonus = 0.12f, BudgetCost = 0, StressCost = 1 },
            new AttackChoice { Id = "watch", Label = "静観する", Description = "今は様子を見る", DefenseBonus = -0.10f, BudgetCost = 0, StressCost = -4 },
        };
    }

    [System.Serializable]
    public class AttackChoice
    {
        public string Id;
        public string Label;
        public string Description;
        public float DefenseBonus;
        public int BudgetCost;
        public int StressCost;
    }
}
