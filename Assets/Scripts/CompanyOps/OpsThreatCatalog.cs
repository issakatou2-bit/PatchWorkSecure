using System;
using System.Collections.ObjectModel;

namespace PatchWorkSecure.CompanyOps
{
    public enum OpsAttackVector
    {
        Vulnerability, Malware, PasswordTheft, Misconfiguration, DataEncryption, SocialEngineering
    }

    public sealed class OpsAttackVectorDefinition
    {
        public readonly OpsAttackVector vector;
        public readonly string name, basic;
        public readonly ReadOnlyCollection<string> equipmentIds;

        internal OpsAttackVectorDefinition(OpsAttackVector vector, string name, string basic, params string[] equipmentIds)
        {
            this.vector = vector; this.name = name; this.basic = basic;
            this.equipmentIds = Array.AsReadOnly((string[])equipmentIds.Clone());
        }
    }

    public sealed class OpsTechnique
    {
        public readonly string name, summary;
        public readonly OpsAttackVector vector;

        internal OpsTechnique(string name, OpsAttackVector vector, string summary)
        { this.name = name; this.vector = vector; this.summary = summary; }
    }

    public sealed class OpsThreat
    {
        public readonly int rank;
        public readonly string ipaName;
        // 仮の名前・呼び名。重なりの確認と加藤さんの決定までは確定名として扱わない。
        public readonly string charName, nickname;
        public readonly string color, pupil, source, @checked;
        public readonly ReadOnlyCollection<OpsTechnique> techniques;
        public readonly ReadOnlyCollection<string> weaknesses, rivalIds;

        internal OpsThreat(int rank, string ipaName, string charName, string nickname, string color, string pupil,
            OpsTechnique[] techniques, string[] weaknesses, params string[] rivalIds)
        {
            this.rank = rank; this.ipaName = ipaName; this.charName = charName; this.nickname = nickname;
            this.color = color; this.pupil = pupil;
            this.techniques = Array.AsReadOnly((OpsTechnique[])techniques.Clone());
            this.weaknesses = Array.AsReadOnly((string[])weaknesses.Clone());
            this.rivalIds = Array.AsReadOnly((string[])rivalIds.Clone());
            source = OpsThreatCatalog.Source; @checked = OpsThreatCatalog.Checked;
        }
    }

    /// <summary>
    /// IPA「情報セキュリティ10大脅威 2026」解説書［組織編］を基に作成した資料用のカタログ。
    /// 名前・対策の基本は原文、技の説明は要約。糸口への割当・設備との対応はゲーム用の整理。
    /// 防御の成功や完全な防止を保証せず、既存の抽選・計算・強敵・表示には接続しない。
    /// </summary>
    public static class OpsThreatCatalog
    {
        public const string Source = "https://www.ipa.go.jp/security/10threats/10threats2026.html";
        public const string ExplanationSource = "https://www.ipa.go.jp/security/10threats/omgdg50000008fi8-att/kaisetsu_2026_soshiki.pdf";
        public const string Checked = "2026-10-03";

        // 解説書p.8 表1.2。設備idは対策の基本に対応するゲーム内の備え（IPAによる製品推薦ではない）。
        public static readonly ReadOnlyCollection<OpsAttackVectorDefinition> Vectors = Array.AsReadOnly(new[] {
            new OpsAttackVectorDefinition(OpsAttackVector.Vulnerability, "ソフトウェアの脆弱性", "ソフトウェアの更新", "patch"),
            new OpsAttackVectorDefinition(OpsAttackVector.Malware, "マルウェアの利用", "セキュリティソフトの利用", "edr"),
            new OpsAttackVectorDefinition(OpsAttackVector.PasswordTheft, "パスワード窃取", "パスワードの管理・認証の強化", "mfa"),
            new OpsAttackVectorDefinition(OpsAttackVector.Misconfiguration, "設定不備", "設定の見直し", "inventory"),
            new OpsAttackVectorDefinition(OpsAttackVector.DataEncryption, "データの暗号化", "バックアップの取得", "backup"),
            new OpsAttackVectorDefinition(OpsAttackVector.SocialEngineering, "ソーシャルエンジニアリング（罠にはめる）", "脅威・手口を知る", "education")
        });

        // Next-20の表を正とする。未決定の略称は仮名と同じにし、旧案の別名を混ぜない。
        // リスク（シャドーAI・ハルシネーション等）も整理のため技の欄に置くが、攻撃そのものとは限らない。
        public static readonly ReadOnlyCollection<OpsThreat> Threats = Array.AsReadOnly(new[] {
            new OpsThreat(1, "ランサム攻撃による被害", "クリプタ", "クリプ", "#D7263D", "鍵穴", new[] {
                T("ランサムウェア", OpsAttackVector.DataEncryption, "データを暗号化し、復元の見返りとして金銭を要求するマルウェア。"),
                T("二重脅迫", OpsAttackVector.DataEncryption, "データの暗号化に加え、盗んだ情報の公開をちらつかせて金銭を要求する。"),
                T("ノーウェアランサム", OpsAttackVector.SocialEngineering, "データを暗号化せず、盗んだ情報を公開すると脅して金銭を要求する。"),
                T("VPN機器の脆弱性からの侵入", OpsAttackVector.Vulnerability, "外部に接続されたVPN機器などの脆弱性を悪用し、社内への侵入に利用する。")
            }, new[] { "backup", "edr", "segment", "patch", "drill" }, "y2-ransom", "y3-final"),
            new OpsThreat(2, "サプライチェーンや委託先を狙った攻撃", "サプラ", "サプラ", "#14A098", "半開きのドア", new[] {
                T("委託先を経由した侵入", OpsAttackVector.PasswordTheft, "関連会社や委託先などの弱点を足掛かりに、標的組織の情報を狙う。"),
                T("ソフトウェアサプライチェーン攻撃", OpsAttackVector.Malware, "開発元のソフトウェアにマルウェアを仕込み、導入や更新を通じて利用者の機器を感染させる。"),
                T("MSPの管理ソフトの悪用", OpsAttackVector.Misconfiguration, "MSPが使う資産管理ソフトウェアなどにマルウェアを仕込み、顧客の機器を感染させる。")
            }, new[] { "zeroTrust", "inventory", "mfa", "patch" }, "y2-supply"),
            new OpsThreat(3, "AIの利用をめぐるサイバーリスク", "ハルシ", "ハルシ", "#8E44AD", "四角い画素", new[] {
                T("シャドーAI", OpsAttackVector.Misconfiguration, "職場で許可されていないAIを業務に使い、持ち出し禁止の情報の入力などで漏えいにつながるおそれがある。"),
                T("ハルシネーション", OpsAttackVector.SocialEngineering, "対話型AIが架空の情報を事実のように提示し、鵜呑みにした利用者の判断を誤らせるおそれがある。"),
                T("間接プロンプトインジェクション", OpsAttackVector.SocialEngineering, "AIが参照する外部データに指示を埋め込み、本来の指示から外れた動作を誘導する。"),
                T("ディープフェイクによるなりすまし", OpsAttackVector.SocialEngineering, "生成した偽の声や映像などで本人を装い、相手に不正な依頼を信じ込ませる。")
            }, new[] { "education", "runbook" }, "y2-ai", "y3-ai"),
            new OpsThreat(4, "システムの脆弱性を悪用した攻撃", "ヴァルネラ", "ネラ", "#8BC34A", "ひび割れ", new[] {
                T("ゼロデイ攻撃", OpsAttackVector.Vulnerability, "開発元などが脆弱性対策情報を公表する前に、その脆弱性を悪用する。"),
                T("Nデイ攻撃", OpsAttackVector.Vulnerability, "修正や回避策が公開されていても、利用者が対策するまでの間に脆弱性を悪用する。"),
                T("攻撃ツール・攻撃サービスの悪用", OpsAttackVector.Vulnerability, "公開された攻撃ツールや提供される攻撃サービスを利用し、未対策の脆弱性を狙う。")
            }, new[] { "patch", "inventory", "monitor", "segment" }),
            new OpsThreat(5, "機密情報を狙った標的型攻撃", "スピア", "スピ", "#29B6F6", "照準（十字）", new[] {
                T("標的型攻撃メール", OpsAttackVector.SocialEngineering, "標的が信じそうなメールを送り、添付ファイルの実行やリンクのクリックで感染などを狙う。"),
                T("水飲み場型攻撃", OpsAttackVector.Malware, "標的がよく訪れるWebサイトを改ざんし、訪問した機器をマルウェアに感染させる。"),
                T("ネットワーク貫通型攻撃", OpsAttackVector.Vulnerability, "ネットワーク境界の機器の脆弱性を悪用し、侵入や情報窃取、攻撃の中継に利用する。")
            }, new[] { "education", "edr", "monitor", "segment", "threatSharing" }, "y3-targeted"),
            new OpsThreat(6, "地政学的リスクに起因するサイバー攻撃（情報戦を含む）", "コグニ", "コグニ", "#F4511E", "吹き出し", new[] {
                T("偽情報の流布", OpsAttackVector.SocialEngineering, "偽情報やディープフェイクを広め、自国などに有利な状況を作る影響工作を行う。"),
                T("ランサム攻撃を偽装した攻撃", OpsAttackVector.DataEncryption, "業務停止や機密情報の窃取などを狙いながら、金銭要求で通常のランサム攻撃を装う。"),
                T("スピアフィッシングによる情報窃取", OpsAttackVector.SocialEngineering, "特定の個人を狙ったメールなどで添付ファイルやリンクへ誘導し、認証情報や機密情報を盗む。")
            }, new[] { "education", "threatSharing", "backup", "csirt" }),
            new OpsThreat(7, "内部不正による情報漏えい等", "プリヴィ", "プリヴィ", "#C2185B", "縦長の猫の瞳孔に小さな鍵", new[] {
                T("アクセス権限の悪用", OpsAttackVector.Misconfiguration, "正当に付与された権限を悪用し、秘密情報を盗んだり不正に操作したりする。"),
                T("在職中に割り当てられたアカウントの悪用", OpsAttackVector.PasswordTheft, "離職後も残されたアカウントや権限で外部から不正アクセスし、情報窃取や不正操作を行う。"),
                T("内部情報の不正な持ち出し", OpsAttackVector.Misconfiguration, "記録媒体やメール、クラウド、紙などを通じ、組織の情報を外部へ不正に持ち出す。")
            }, new[] { "inventory", "monitor", "runbook", "education" }),
            new OpsThreat(8, "リモートワーク等の環境や仕組みを狙った攻撃", "ヴィピ", "ヴィピ", "#455A64", "電波（扇の形）", new[] {
                T("リモートワーク用製品の脆弱性の悪用", OpsAttackVector.Vulnerability, "VPNなどリモートワーク用製品の脆弱性を悪用して社内ネットワークへの侵入を狙う。"),
                T("アカウント情報の不正利用", OpsAttackVector.PasswordTheft, "不正に得たアカウント情報を使い、リモートワーク環境へ不正アクセスする。"),
                T("リモートワーク用端末への攻撃", OpsAttackVector.Malware, "マルウェア対策などが不十分なリモートワーク用端末を攻撃し、感染などを引き起こす。")
            }, new[] { "patch", "mfa", "zeroTrust", "edr" }),
            new OpsThreat(9, "DDoS攻撃（分散型サービス妨害攻撃）", "フラッダ", "フラ", "#1E63D6", "矢印", new[] {
                T("ボットネットを利用したDDoS攻撃", OpsAttackVector.Malware, "乗っ取った多数の機器に命令し、標的のWebサイトやDNSなどへ大量のアクセスを集中させる。"),
                // フラッド・リフレクションは6分類に完全には収まらず、近い分類として置いた（防御の設定・準備の不足）。
                // 防御側の設定不備が攻撃成立の必須条件だという意味ではない。DNS水責めもNext-20指定の整理に従う。
                T("フラッド攻撃", OpsAttackVector.Misconfiguration, "大量の通信パケットをサーバーなどへ送り、高い負荷をかける。"),
                T("リフレクション攻撃", OpsAttackVector.Misconfiguration, "送信元を標的のIPアドレスに偽装し、多数のサーバーの応答を標的へ集中させる。"),
                T("ランダムサブドメイン攻撃（DNS水責め攻撃）", OpsAttackVector.Misconfiguration, "標的のドメインにランダムなサブドメインを付けて問い合わせ、DNSサーバーに高い負荷をかける。")
            }, new[] { "redundancy", "monitor", "runbook" }),
            new OpsThreat(10, "ビジネスメール詐欺", "スプーフィ", "スプーフィ", "#F5A623", "封筒", new[] {
                T("取引先へのなりすまし", OpsAttackVector.SocialEngineering, "取引先を装って偽の請求書などを送り、攻撃者の口座への振り込みを促す。"),
                T("経営者等へのなりすまし", OpsAttackVector.SocialEngineering, "経営者などを装った業務指示のメールを送り、不正な送金に従わせる。"),
                T("BECの準備としての情報窃取", OpsAttackVector.PasswordTheft, "なりすましや感染、不正ログインなどで個人情報や取引のやり取りを盗み、詐欺の準備に使う。")
            }, new[] { "education", "runbook", "mfa" }, "y3-bec")
        });

        static OpsTechnique T(string name, OpsAttackVector vector, string summary) => new OpsTechnique(name, vector, summary);
    }
}
