using System;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    [Serializable] public class OpsEventProfile
    {
        public string id, category, kind, lesson, source;
        public string projectA, projectB, actionA, actionB;
        public int[] prevention;
        public int cultureDivisor, containment, stopPower = 23, scopePower = 5, recoverPower = 5;
        public bool dataRecovery, restart;
        public string[] responses;
    }
    [Serializable] public class OpsEvent
    {
        public string id, profile, title, news, boss, person, staff, symptom, finding, hint, calm, source;
        public int[] threats;
        public bool operational;
    }
    [Serializable] public class OpsTicket
    {
        public string id, title, request, lesson, source;
        public int member, cash, culture, trust, relief;
    }
    public static class OpsEventCatalog
    {
        // 順位は発生確率・危険度ではない。公式の脅威区分との対応だけを持つ。
        public static readonly string[] ThreatNames = { "ランサム攻撃", "サプライチェーン・委託先", "AI利用のリスク", "脆弱性悪用", "標的型攻撃",
            "地政学的リスク", "内部不正", "リモートワーク環境", "DDoS", "ビジネスメール詐欺" };
        public const string IpaUrl = "https://www.ipa.go.jp/security/10threats/10threats2026.html";
        // 重みはゲーム用。台帳/バックアップ/訓練/MFA/監視/分離/教育/自動化/更新/冗長化/手順。
        public static readonly OpsEventProfile[] Profiles = {
            P("ransom", "ランサム攻撃", "ransom", "backup", "asahi", "backup", "drill", "audit", "map", new[]{0,0,0,4,0,0,0,0,6,0,0}, contain:6, data:true),
            P("supply", "委託先・外部接続", "supply", "supplier", "ipa", "inventory", "segment", "audit", "map", new[]{3,0,0,0,4,0,0,0,0,0,3}, contain:6),
            P("ai", "AI利用と情報管理", "leak", "ai", "ipa", "inventory", "education", "audit", "listen", new[]{7,0,0,0,2,0,5,0,0,0,3}, culture:12, stop:15),
            P("vulnerability", "公開システムの脆弱性", "vulnerability", "patch", "jpcert", "inventory", "patch", "audit", "map", new[]{2,0,0,0,0,0,0,0,12,0,0}, contain:6),
            P("targeted", "標的型攻撃", "supply", "evidence", "ipa", "monitor", "segment", "audit", "listen", new[]{2,0,0,3,5,0,4,0,5,0,0}, contain:6),
            P("claim", "侵害情報の真偽確認", "social", "verify", "jpcert-quarter", "monitor", "runbook", "audit", "map", new[]{2,0,0,0,5,0,4,0,0,0,4}, culture:12, stop:17),
            P("insider", "権限と持ち出しの管理", "leak", "least", "ipa", "inventory", "monitor", "audit", "listen", new[]{8,0,0,0,6,0,3,0,0,0,2}, culture:16, stop:19),
            P("remote", "リモート接続の管理", "identity", "remote", "jpcert", "mfa", "patch", "audit", "listen", new[]{3,0,0,8,2,0,0,0,8,0,0}, contain:4),
            P("device", "社外端末の紛失", "identity", "remote", "ipa", "inventory", "runbook", "audit", "listen", new[]{7,0,0,0,4,0,4,0,0,0,4}),
            P("session", "認証済みセッションの悪用", "identity", "session", "ipa", "monitor", "education", "audit", "listen", new[]{3,0,0,2,7,0,5,0,0,0,3}),
            P("ddos", "サービスの可用性", "outage", "ddos", "ipa", "redundancy", "monitor", "audit", "map", new[]{2,0,0,0,7,0,0,0,0,7,3}, stop:7, recover:15),
            P("bec", "支払・承認の確認", "social", "verify", "ipa", "education", "runbook", "listen", "map", new[]{2,0,0,0,0,0,7,0,0,0,5}, culture:8, stop:15),
            P("change", "変更・更新トラブル", "outage", "change", "crowdstrike", "patch", "runbook", "audit", "map", new[]{3,0,0,0,2,0,0,3,8,0,5}, stop:7, recover:17, restart:true),
            P("storage", "保存・復元トラブル", "outage", "rto", "sme", "backup", "drill", "audit", "map", new[]{3,0,0,0,3,0,0,2,0,3,3}, stop:7, recover:16, data:true),
            P("service", "業務サービスの障害", "outage", "availability", "sme", "redundancy", "runbook", "audit", "map", new[]{3,0,0,0,4,0,0,3,0,7,4}, stop:7, recover:16, restart:true),
            P("sharing", "共有・アカウントの設定", "leak", "least", "sme", "inventory", "education", "audit", "listen", new[]{7,0,0,0,3,0,5,2,0,0,3}, culture:12, stop:16)
        };
        private static OpsEventProfile P(string id, string category, string kind, string lesson, string source, string a, string b, string aa, string ab,
            int[] weights, int culture=0, int contain=0, int stop=23, int recover=5, bool data=false, bool restart=false)
        {
            bool business = kind == "social" || kind == "leak";
            return new OpsEventProfile { id=id, category=category, kind=kind, lesson=lesson, source=source, projectA=a, projectB=b, actionA=aa, actionB=ab,
                prevention=weights, cultureDivisor=culture, containment=contain, stopPower=stop, recoverPower=recover, dataRecovery=data, restart=restart,
                responses=business ? new[]{"関連する処理を一時保留", "対象・依頼元を確認して処理", "安全な業務経路へ切り替える"} :
                    kind == "outage" ? new[]{"影響サービスを広く停止", "対象を絞って調査・対処", "切戻し・代替業務を優先"} :
                    new[]{"関連環境を広く停止・隔離", "対象を絞って隔離・調査", "安全確認と復旧に人を配分"} };
        }
        // 実例から得た論点を架空の45人企業向けに再構成。実企業の事件の再現ではない。
        public static readonly OpsEvent[] Events = {
            E("intro-ransom", "ransom", "うちのデータ、戻せる？", "共有ファイルが使えず、納品が延期した会社がある。", "ソフトの追加だけでなく、戻せるかを確かめたい。", "制作の森：バックアップはあるはず。でも戻した人は誰だろう？", "共有領域の一部が開けない。影響端末の範囲はまだ不明。", "夜間処理と履歴を照合し、端末とデータの範囲を確認。", "侵入を抑える備えと、戻す備えを組み合わせよう。", new[]{1}, calm:"検証用の変換処理だった。復元手順の点検は役立った。"),
            E("ransom-backup", "ransom", "バックアップも同じ権限？", "侵入後、復元元まで使えなくなる事例。", "本番とバックアップを一緒に管理していて大丈夫？", "制作の大野：管理者のログインは同じものです。", "本番の異常と同時に、バックアップ管理にも不審な操作。", "本番と復元元の権限・接続・保存世代を確認。", "復元元を分け、健全な世代を訓練で確かめよう。", new[]{1}),
            E("ransom-extortion", "ransom", "暗号化だけではない被害", "暗号化と情報の持ち出しを組み合わせた脅迫。", "戻せたら終わり？ 外への説明も必要だよね。", "総務の小川：何が外へ出たか記録を残したいです。", "不審な書換えと大量の外向き通信が重なる。", "記録を保全し、暗号化・持ち出しの範囲を分けて確認。", "復元で流出は取り消せない。調査と関係者への連絡も必要。", new[]{1,5}),
            E("ransom-lateral", "ransom", "一台から全部に広がる？", "拠点からの侵入が複数システムの停止につながった。", "納品を守るため、被害が広がる範囲を小さくしたい。", "佐伯：部門間の接続が全部開いています。", "別部門の端末でも同様のファイル異常を検知。", "拠点・部門の通信と権限を照合し、横展開の経路を確認。", "分離と台帳で範囲を絞り、復元元の健全性を確かめる。", new[]{1,8}),
            E("supplier-access", "supply", "委託先の保守IDは誰が使う？", "委託先の接続や権限から被害が及ぶ事例。", "保守会社を信頼しつつ、接続の範囲も見直したい。", "佐伯：契約が終わった保守IDが残っています。", "保守時間外に、外部接続から管理操作が続く。", "契約・連絡先・許可範囲と接続記録を突き合わせる。", "必要な接続と権限に絞り、別経路で保守会社へ確認。", new[]{2}),
            E("supplier-update", "supply", "正規の更新なら安全？", "配布元の侵害で、正規更新に不審な変更が混じる。", "更新は必要だけど、そのまま全社へ配っていい？", "制作の大野：いつもの配布ページから入手しました。", "更新直後に複数端末が未知の宛先へ接続。", "配布元・更新時刻・対象端末と通信を照合。", "正規の署名だけを過信せず、段階展開と挙動確認を行う。", new[]{2,5}),
            E("supplier-saas", "supply", "外部サービスが侵害されたら", "共有先のサービスで侵害の疑いが公表された。", "止める範囲と代替の仕事を整理して報告してほしい。", "営業の相田：どの資料を預けたか一覧がありません。", "利用中の委託サービスから侵害調査の通知。", "預けた情報・連携権限・代替手段を台帳で確認。", "利用先の範囲を把握し、連携と業務を必要な範囲で切り替える。", new[]{2}),
            E("ai-upload", "ai", "AIに顧客資料を入れていい？", "業務AIへの入力で機密の扱いが問題になる。", "効率化は進めたい。使っていい情報の範囲を決めたい。", "営業の相田：提案書を作るため顧客一覧を入力しました。", "未承認のAIサービスへ顧客データを入力したとの相談。", "入力範囲・利用設定・契約条件を確認し記録。", "一律禁止だけでなく、承認済みの環境と入力ルールを作る。", new[]{3}),
            E("ai-agent", "ai", "AIの回答に操作を任せる？", "外部文書の指示に引きずられるAI利用のリスク。", "自動化したいけど、承認なしで送信しても平気？", "小川：AIが参照した文書に別の送信先が書いてあります。", "AIが作った自動処理が、想定外の宛先へ送信を提案。", "参照資料・実行権限・承認段階を確認。", "外部入力を命令として扱わず、権限と人の承認を絞る。", new[]{3}),
            E("ai-output", "ai", "生成した回答をそのまま配布？", "誤った生成情報が業務判断に混じるリスク。", "便利な社内AIでも、根拠を確認して案内したい。", "小川：存在しない社内規程をAIが案内していました。", "AIの案内を根拠に、通常と違うデータ共有が進む。", "原本の規程と引用元、共有した範囲を確認。", "原本・根拠・権限を確認し、判断できない質問は人へつなぐ。", new[]{3}),
            E("vuln-edge", "vulnerability", "公開機器の緊急更新", "境界機器の問題に、悪用確認を伴う注意喚起。", "在宅接続は残したい。でも公開したままでいい？", "佐伯：機器のバージョンと公開範囲を調べます。", "境界機器で、想定しない管理操作の記録を検知。", "対象バージョン・公開範囲・侵害の兆候を照合。", "更新だけで済ませず、侵害調査と認証情報の見直しも検討。", new[]{4,8}, source:"jpcert-f5"),
            E("vuln-web", "vulnerability", "使っているライブラリが対象？", "Web基盤の脆弱性が公開され、悪用への注意喚起。", "自社が対象か、根拠を持って判断してほしい。", "制作の大野：外注したサイトの構成が分かりません。", "公開サイトに、通常と異なる処理と外向き通信。", "構成・バージョン・公開条件と記録を確認。", "台帳から対象を絞り、暫定策・更新・調査を組み合わせる。", new[]{4}),
            E("vuln-legacy", "vulnerability", "古い端末をすぐ替えられない", "更新が止まったシステムが侵入の入口になる。", "古い業務ソフトも必要。移行までの備えを提案して。", "森：旧端末でしか動かない請求ソフトがあります。", "旧端末に不審な接続。代替端末では業務が動かない。", "依存業務・接続先・更新可能範囲を確認。", "移行計画と、当面の接続制限・監視・代替業務を考える。", new[]{4}),
            E("targeted-mail", "targeted", "会議資料を装う添付", "業務に合わせた偽の資料で端末への侵入を狙う。", "注意だけで防げないなら、被害を小さくする備えも欲しい。", "営業の相田：取引先と似た名前の送信者でした。", "資料を開いた端末から、未知の通信が続く。", "送信経路と端末の記録を保全し、影響範囲を確認。", "報告を責めず、対象端末の隔離と記録の保全につなげる。", new[]{5}),
            E("targeted-consent", "targeted", "便利な連携アプリの権限", "連携アプリへの同意を入口に情報へアクセス。", "追加アプリで仕事が楽になるなら、権限も確認しよう。", "相田：メールを全部読める権限を承認してしまいました。", "見覚えのない連携アプリが業務メールへアクセス。", "付与した権限・アプリ・アクセス履歴を確認。", "アプリの権限取消と影響調査。パスワード変更だけでは足りない。", new[]{5}),
            E("targeted-session", "session", "MFAを通っているから安心？", "認証済みのセッションが悪用される事例。", "多要素認証があるのに、なぜ不審操作が起きる？", "佐伯：本人のログイン後に別の場所で操作されています。", "認証済みの通信で、本人がしていないダウンロード。", "端末・時刻・セッションと操作の整合性を確認。", "MFAは万能ではない。セッション失効と端末の調査も必要。", new[]{5,8}),
            E("geo-ddos", "ddos", "外部情勢で攻撃が増えたら", "情勢の変化に伴い、組織への妨害活動が起きる。", "会社の立場への反応でサイトが狙われる可能性を考えたい。", "相田：問い合わせページもつながりにくいです。", "大量のアクセスで受注サイトが応答しにくい。", "通常の需要・通信の偏り・業務影響を照合。", "攻撃対策サービスと代替窓口を合わせ、重要業務を残す。", new[]{6,9}),
            E("geo-claim", "claim", "侵害を名乗る投稿が出た", "攻撃者の主張だけでは侵害を裏付けられない場合もある。", "当社も被害と書かれている。確認した事実を報告して。", "小川：社員から不安の問い合わせが来ています。", "外部投稿が侵害を主張。証拠の真偽は不明。", "主張と自社の記録を照合し、確認済み・未確認を整理。", "断定を急がず、記録と証拠を確認して関係者へ伝える。", new[]{1}, calm:"投稿の資料は公開情報。自社の侵害は確認されなかった。"),
            E("insider-export", "insider", "退職前の大量ダウンロード", "付与した権限を使った情報持ち出しが問題になる。", "人を決めつけず、業務と権限の範囲を確認したい。", "小川：引継ぎのための取得なのか、分かりません。", "退職予定者の大量取得。正当な引継ぎかは未確認。", "業務目的・承認・取得範囲を確認し、記録を保全。", "最小権限と退職手続き。疑いだけで本人を責めない。", new[]{7}, calm:"承認済みの引継ぎ用取得だった。権限と記録を確認できた。"),
            E("insider-admin", "insider", "共有の管理者ID", "誰の操作か追えない管理者アカウントが問題になる。", "仕事は続けつつ、操作の責任範囲を明確にしてほしい。", "佐伯：複数人で同じ管理IDを使っています。", "共有管理IDから、予定外の権限変更とデータ取得。", "承認記録・担当者・変更範囲を照合。", "個別IDと必要な権限、管理操作の記録を整える。", new[]{7}),
            E("insider-contract", "insider", "外注メンバーの権限が残る", "契約終了後のアクセス権が情報管理の穴になる。", "入退社だけでなく、契約変更も権限に反映したい。", "小川：人事台帳に載らない外注メンバーもいます。", "契約が終わったIDから、共有資料へのアクセス。", "契約・所有者・有効な権限と取得履歴を確認。", "契約終了と権限取消を結び、データの引継ぎも確認。", new[]{7,2}),
            E("remote-vpn", "remote", "在宅接続の認証情報が流出？", "境界機器に関連した認証情報流出への注意喚起。", "漏れた情報が今も使えるのか確かめてほしい。", "佐伯：更新前の設定を引き継いだ接続があります。", "在宅用アカウントに未知の接続先からのログイン。", "接続元・機器・有効な認証情報と記録を照合。", "機器の更新に加え、有効な認証情報・権限を見直す。", new[]{8,4}),
            E("remote-device", "device", "持ち帰った端末が見つからない", "社外で扱う端末の紛失とアクセスのリスク。", "社員がすぐ報告できる連絡先と対処を整えたい。", "相田：移動中に会社端末を置き忘れました。", "紛失端末に不審な接続。遠隔管理の対象か不明。", "端末台帳・保護設定・セッションの状態を確認。", "報告、端末管理、セッション失効。MFAだけで保存データは守れない。", new[]{8}),
            E("remote-help", "remote", "サポートを名乗る電話", "支援担当を装って遠隔操作や認証を誘導する。", "急なサポート連絡も、正規の窓口で確認できるように。", "相田：修理のため認証コードを教えるよう言われました。", "未知の支援者へ遠隔操作の許可を渡したとの相談。", "正規の支援窓口と端末操作・セッションを確認。", "既知の連絡先で確認し、遠隔接続と影響範囲を見直す。", new[]{8,5}),
            E("ddos-web", "ddos", "サイトだけが重い", "大量通信でサービスを使えなくする攻撃。", "社内を全部止めず、顧客の窓口を残したい。", "相田：注文が何度も送られ、重複が心配です。", "外部から大量のリクエスト。受注処理が停滞。", "通信と処理待ちを照合し、正常な注文への影響を確認。", "冗長化だけで帯域を使い切る攻撃は防げない。外部支援と迂回も使う。", new[]{9}),
            E("ddos-link", "ddos", "回線が埋まっている", "アクセスが集中し、外部接続の帯域が足りなくなる。", "別回線で重要な仕事を続けられる？", "森：請求サービスまでつながらなくなっています。", "回線の混雑で複数の業務サービスが利用困難。", "回線利用と通信の偏り、代替経路を確認。", "回線事業者への相談と代替業務。バックアップで帯域は増えない。", new[]{9}),
            E("bec-invoice", "bec", "取引先の振込先が変わった", "メールを使い、送金先変更を信用させる詐欺。", "納期を守りながら、いつもの連絡先で確認したい。", "森：請求書の口座だけが前回と違います。", "取引先を名乗るメールが、至急の振込先変更を要求。", "既知の電話番号と承認記録で変更の有無を確認。", "メールへの返信だけで確認しない。承認手順と別経路を使う。", new[]{10}, calm:"正式な変更だった。別経路で確認し承認を通せた。"),
            E("bec-voice", "bec", "社長の声なら信用する？", "声や映像を使い、本人を装って急がせる手口。", "私の声に似ていても、送金の承認は省略しないで。", "森：内密で急ぐように言われています。", "役員らしい音声が通常の承認を省いた支払を指示。", "既知の連絡先へ折り返し、依頼と承認の有無を確認。", "声や映像だけで本人と判断せず、既知の経路で確認。", new[]{10,3}),
            E("bec-thread", "bec", "いつものメールの続き", "本物のやり取りに不正な依頼が混じる手口。", "差出人の見た目だけでなく、承認と中身を確認して。", "相田：以前からの商談スレッドに添付が増えました。", "取引メールに急な支払条件と共有リンクの変更。", "既知の窓口と承認済みの条件を突き合わせる。", "正規のアカウントやスレッドでも、依頼の正当性は別に確認。", new[]{10,5}),
            E("ops-certificate", "service", "証明書の更新担当がいない", "証明書の期限管理が途切れ、接続が止まる。", "警告を無視させずに、更新と代替の仕事を進めたい。", "佐伯：前任者の通知先だけが登録されています。", "業務サービスで証明書警告が出て接続できない。", "期限・更新担当・依存サービスと切替手順を確認。", "検証を無効化せず、更新と期限監視、引継ぎを整える。", new int[0], operational:true),
            E("ops-sso", "change", "SSOの切替後に入れない", "認証連携の変更で利用者がログインできなくなる。", "全員を戻す前に、対象と戻し方を確認したい。", "小川：営業だけ新しいサービスへ入れません。", "認証設定の切替後、特定部門でログイン失敗。", "変更差分・部門の条件・代替ログインを確認。", "事前検証と段階展開、切戻し手順が再開を支える。", new int[0], operational:true),
            E("ops-update", "change", "更新後に端末が起動しない", "正規のソフト更新による大規模停止の事例。", "セキュリティ更新も、広げる前に確かめたい。", "佐伯：更新した端末だけ起動しません。", "複数の業務端末が更新直後に停止。", "更新時刻・対象端末・既知の問題と復旧手順を照合。", "正常な更新でも障害は起きる。段階展開と戻す備えを使う。", new int[0], operational:true),
            E("ops-capacity", "service", "売上が伸びたら処理が詰まる", "需要の増加でサービスの処理能力が足りない。", "攻撃と決めずに、混雑と改善費を見積もって。", "相田：再送を押すほど処理待ちが増えます。", "正規の注文が集中し、受注処理が進まない。", "通常の需要・処理待ち・ボトルネックを照合。", "容量と再試行の扱い、代替業務で混雑を減らす。", new int[0], operational:true),
            E("ops-batch", "change", "夜間処理が止まった", "担当者不在で、定型処理の再開が遅れる。", "休日の呼出しを減らし、別の人でも再開できるように。", "森：どこからやり直すか分かりません。", "夜間の請求処理が途中で停止。重複処理の恐れ。", "処理の依存関係・完了範囲・再実行条件を確認。", "自動化と手順。再実行する前に重複と整合性を確認。", new int[0], operational:true),
            E("ops-storage", "storage", "保存装置が故障した", "保存先の故障で必要なデータが使えなくなる。", "どの仕事から、どの時点へ戻すか決めたい。", "大野：昨日の修正を失いたくありません。", "共有装置が停止し、納品ファイルを読み出せない。", "健全な世代・保存範囲・重要業務の順を確認。", "冗長化と世代バックアップは別物。復元条件を確かめる。", new int[0], operational:true),
            E("ops-delete", "storage", "共有フォルダを誤って削除", "作業中の誤操作でもデータを失うことがある。", "社員を責めず、復元と再発防止を進めたい。", "大野：整理するつもりで別のフォルダを消しました。", "納品用フォルダが見つからないとの報告。", "削除履歴・対象・復元元と保存時点を確認。", "早い相談と復元の訓練。同期だけでは削除も伝わる。", new int[0], operational:true),
            E("ops-saas", "service", "使っているSaaSが停止", "外部サービスの障害で社内業務も止まる。", "全部を自前にはできない。代替の進め方を決めよう。", "小川：人事の受付もメールも使えません。", "主要クラウドサービスで障害通知。復旧時刻は不明。", "提供元の通知・依存業務・代替窓口を確認。", "依存先と代替業務を把握し、復旧後の整合も確認。", new int[0], operational:true),
            E("ops-network", "service", "オフィスのネットが不安定", "接続機器の障害で複数部門に影響が出る。", "予備の機器と、業務を止めない手順を確認したい。", "相田：会議と受注が同時に止まっています。", "社内の一部区画で通信が断続的に切れる。", "区画・機器・変更履歴と代替経路を確認。", "切分けと予備経路。全体停止の前に対象を確かめる。", new int[0], operational:true),
            E("ops-sharing", "sharing", "共有リンクの範囲が広い", "便利な共有設定が意図しない公開につながる。", "禁止だけでなく、安全な共有方法を案内したい。", "相田：取引先だけが見えると思っていました。", "資料のリンクに、想定外の閲覧者の記録。", "共有設定・閲覧履歴・承認済みの相手を確認。", "設定の変更と範囲確認。流出をバックアップで取り消すことはできない。", new int[0], operational:true, calm:"承認済みの取引先アクセス。共有設定を再確認できた。"),
            E("ops-mail-rule", "sharing", "メールが勝手に転送される", "メール設定の見落としで情報が外へ送られる。", "人事異動に合わせ、設定と管理者を見直したい。", "小川：前の担当の転送設定が残っています。", "業務メールに意図しない自動転送の設定。", "設定時刻・作成者・転送範囲を確認し記録。", "転送停止だけでなく、正規変更か侵害かも調べる。", new int[0], operational:true)
        };
        private static OpsEvent E(string id, string profile, string title, string news, string boss, string staff, string symptom, string finding, string hint,
            int[] threats, bool operational=false, string calm=null, string source=null) => new OpsEvent { id=id, profile=profile, title=title, news=news, boss=boss,
                person="社長・加藤", staff=staff, symptom=symptom, finding=finding, hint=hint, threats=threats, operational=operational, calm=calm, source=source };
        public static OpsEvent Event(string id) => Array.Find(Events, e => e.id == id);
        public static OpsEventProfile Profile(string id) => Array.Find(Profiles, p => p.id == id);
        public static string ProjectNames(OpsEventProfile p) => OpsCatalog.Projects[OpsCatalog.Index(p.projectA)].name + "＋" + OpsCatalog.Projects[OpsCatalog.Index(p.projectB)].name;
        public static string ActionName(string action) => action == "audit" ? "現状調査" : action == "listen" ? "社員との対話" : "重要業務の確認";
        public static string SourceUrl(string id) => id == "asahi" ? "https://www.asahigroup-holdings.com/newsroom/detail/20260218-0101.html" :
            id == "jpcert-f5" ? "https://www.jpcert.or.jp/at/2026/at260028.html" :
            id == "jpcert-quarter" ? "https://www.jpcert.or.jp/qr/2026/QR_FY2025-Q4.pdf" :
            id == "jpcert" ? "https://www.jpcert.or.jp/at/" : id == "crowdstrike" ? "https://www.crowdstrike.com/en-us/blog/channel-file-291-rca-available/" :
            id == "helpdesk" ? "https://techblog.glpgs.com/entry/2022/01/11/152535" : id == "gree" ? "https://note.com/gree_it/n/n584799ef1a86" :
            id == "identity" ? "https://tech.smarthr.jp/entry/2024/08/28/144133" : id == "sme" ? "https://www.ipa.go.jp/security/guide/sme/about.html" : IpaUrl;

        public static readonly OpsTicket[] Tickets = {
            T("onboard", "新人の端末とIDを準備", "入社日に仕事を始められるよう、端末・権限・窓口を確認。", 0, "least", "identity", trust:3),
            T("offboard", "退職者の権限を確認", "退職日と権限取消を照合。資料の引継ぎも忘れずに。", 0, "least", "identity", trust:3),
            T("transfer", "異動後の権限を見直す", "新しい部署の権限を付け、以前の不要な権限を外す。", 0, "least", "identity", culture:3),
            T("contractor", "外注メンバーのID整理", "契約期間・所有者と権限を照合し、終了後の扱いを決める。", 0, "supplier", "identity", trust:3),
            T("licenses", "使っていないSaaSを棚卸し", "利用状況・契約・所有者を確認し、不要な課金を整理。", 2, "asset", "sme", cash:3),
            T("helpdesk", "問い合わせの窓口をまとめる", "個別DMの相談を受付へ集め、担当と対応状況を残す。", 0, "helpdesk", "helpdesk", relief:6),
            T("faq", "よくある質問を手順にする", "同じ問い合わせを記録し、社員が自分で確認できる案内にする。", 0, "helpdesk", "helpdesk", culture:3),
            T("handover", "休暇前の当番引継ぎ", "連絡先・判断権限・未完了の仕事を次の当番へ伝える。", 0, "incident", "sme", relief:6),
            T("certificate", "証明書の期限と担当を確認", "期限・更新担当・通知先を一覧へ。切替の確認日も決める。", 1, "certificate", "sme", trust:3),
            T("backup-check", "バックアップの結果を点検", "成功表示だけでなく、保存対象と復元条件の変更を確認。", 2, "backup", "sme", trust:3),
            T("alert", "不要な通知を整理", "監視対象と重要度を確認し、必要な通知を埋もれさせない。", 1, "detect", "sme", relief:6),
            T("patch-check", "更新対象と作業日を確認", "台帳から対象を確認し、検証・切戻しと業務日程を合わせる。", 1, "patch", "sme", trust:3),
            T("phishing-report", "不審メールの相談を受ける", "報告を歓迎し、元メールと操作の有無を記録して確認。", 1, "evidence", "sme", culture:3),
            T("approval", "支払の確認先を整える", "メールに書かれた連絡先ではなく、既知の経路で確認できるように。", 2, "verify", "ipa", trust:3),
            T("ai-guide", "業務AIの相談会", "入力してよい情報・承認済みの環境・回答の確認方法を共有。", 0, "ai", "gree", culture:3),
            T("ai-helpdesk", "自動回答を人へつなぐ", "分からない質問と危険な操作は、人の担当へ渡す手順を点検。", 1, "helpdesk", "gree", relief:6),
            T("device-return", "返却端末の台帳を更新", "端末の回収・保管情報・再利用前の扱いを確認。", 0, "asset", "sme", cash:3),
            T("recovery-contact", "代替業務と連絡先の点検", "外部サービスが止まった時の受付・優先業務と役割を確認。", 2, "bcp", "sme", trust:3)
        };
        private static OpsTicket T(string id, string title, string request, int member, string lesson, string source, int cash=0, int culture=0, int trust=0, int relief=0) =>
            new OpsTicket { id=id, title=title, request=request, member=member, lesson=lesson, source=source, cash=cash, culture=culture, trust=trust, relief=relief };
        public static OpsTicket Ticket(string id) => Array.Find(Tickets, t => t.id == id);
        public static string[] Schedule(int seed, bool tickets)
        {
            string[] result = new string[12];
            uint random = unchecked((uint)seed) ^ (tickets ? 0x9E3779B9u : 0x7F4A7C15u);
            for (int m=0; m<12; m++)
            {
                if (!tickets && m == 0) { result[m] = "intro-ransom"; continue; }
                bool operational = m == 3 || m == 4 || m == 7 || m == 9;
                var ids = tickets ? Tickets.Select(t=>t.id).ToArray() : Events.Where(e=>e.operational==operational).Select(e=>e.id).ToArray();
                ids = ids.Where(id=>!result.Contains(id)).ToArray();
                // ランタイムのRandom実装に依存しない。抽選結果は保存して再抽選を防ぐ。
                unchecked { random += 0x6D2B79F5u; random ^= random >> 15; random *= 2246822519u; random ^= random >> 13; }
                result[m] = ids[random % (uint)ids.Length];
            }
            return result;
        }
    }
}
