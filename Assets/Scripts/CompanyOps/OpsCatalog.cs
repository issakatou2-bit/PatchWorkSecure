using System;
namespace PatchWorkSecure.CompanyOps
{
// 表示データとルールを分離。金額・影響は教育用の架空の値。
[Serializable] public class OpsProject { public string id, name, tag, group, desc, effect, term, requires; public int cost, time, upkeep, max; }
[Serializable] public class OpsMonth { public string name, season, title, news, boss, person, staff, hint, kind, @event, symptom, finding, lesson, calm; public int @base; }
[Serializable] public class OpsTerm { public string id, name, basic, deep; }
[Serializable] public class OpsMission
{
    public string title, equipmentRoute, fieldRoute, projectA, projectB, actionA, actionB;
}

public static class OpsCatalog
{
public static readonly OpsProject[] Projects = {
new OpsProject { id = "inventory", name = "資産と業務の台帳", tag = "把握", group = "operations", cost = 8, time = 1, upkeep = 0, max = 2, desc = "誰の、どの仕事が、このシステムに依存するか。", effect = "限定対応の精度が上がり、監視・分離・冗長化を導入できる。", term = "asset" },
new OpsProject { id = "backup", name = "分離バックアップ", tag = "復旧", group = "recover", cost = 20, time = 2, upkeep = 2, max = 2, desc = "本番環境とは分けて、戻せるデータを残す。", effect = "侵入は防がない。データ被害を減らす。訓練と組み合わせると復旧が早い。", term = "backup" },
new OpsProject { id = "drill", name = "復元・連絡の訓練", tag = "訓練", group = "recover", cost = 7, time = 1, upkeep = 0, max = 2, requires = "backup", desc = "バックアップの復元手順と緊急連絡先を検証する。", effect = "バックアップからの復旧力が上がる。復旧対応の業務停止を短くする。", term = "rto" },
new OpsProject { id = "mfa", name = "多要素認証の展開", tag = "予防", group = "protect", cost = 17, time = 2, upkeep = 2, max = 2, desc = "パスワードだけに頼らない入口を用意する。", effect = "認証情報の悪用に強くなる。導入直後は利用者の負担が少し増える。", term = "mfa" },
new OpsProject { id = "monitor", name = "監視と通知の整理", tag = "検知", group = "protect", cost = 18, time = 2, upkeep = 2, max = 2, requires = "inventory", desc = "監視対象と異常通知の条件を設定する。", effect = "事件の見通しが明確になり、限定対応の効果が上がる。", term = "detect" },
new OpsProject { id = "segment", name = "ネットワーク分離", tag = "限定", group = "protect", cost = 20, time = 2, upkeep = 2, max = 2, requires = "inventory", desc = "一つの侵入で、全部が巻き込まれないように。", effect = "侵入後の広がりを抑える。メールの詐欺そのものを見抜く仕組みではない。", term = "defense" },
new OpsProject { id = "education", name = "相談できる教育", tag = "組織", group = "people", cost = 9, time = 1, upkeep = 1, max = 2, desc = "間違いを責めず、報告してくれたことを歓迎する。", effect = "相談文化が育ち、なりすまし・誤共有を早く止められる。", term = "culture" },
new OpsProject { id = "automation", name = "定型業務の自動化", tag = "工数", group = "operations", cost = 19, time = 2, upkeep = 2, max = 2, desc = "定型作業を自動化して毎月の工数を増やす。", effect = "次月から1レベルにつき工数+1。毎月の負担も軽くする。", term = "change" },
new OpsProject { id = "patch", name = "更新と検証の運用", tag = "予防", group = "protect", cost = 12, time = 1, upkeep = 1, max = 2, desc = "影響を確かめ、戻し方を用意して更新する。", effect = "脆弱性悪用に強くなる。更新起因の障害も抑える。", term = "patch" },
new OpsProject { id = "redundancy", name = "代替経路と冗長化", tag = "継続", group = "recover", cost = 24, time = 2, upkeep = 3, max = 2, requires = "inventory", desc = "一つ止まっても、仕事まで全部止めない。", effect = "障害時と広域停止時の業務影響を減らす。データの世代保管とは別物。", term = "availability" },
new OpsProject { id = "runbook", name = "引継ぎと対応手順", tag = "運用", group = "operations", cost = 7, time = 1, upkeep = 0, max = 2, desc = "担当者がいない日にも、次の一手が分かる。", effect = "対応力が上がり、毎月の疲労を軽くする。", term = "incident" },
};
public static readonly OpsMonth[] Months = {
new OpsMonth { name = "4月", season = "はじまり", title = "「うちは、戻せるの？」", news = "同業の制作会社でデータが使えなくなり、納品が延期。", boss = "ニュースを見たんだけど、うちは大丈夫？ ソフトを追加すれば済む話かな。", person = "社長・加藤", staff = "制作の森さん：「バックアップはあるはず。でも戻した人、誰だろう？」", hint = "最初は復旧の備えを検討。ただし、調べる時間と導入する時間は同じ工数を使う。", kind = "ransom", @base = 29, @event = "共有ファイルに不審な変更", symptom = "制作フォルダの一部が開けない。何台に広がったかはまだ不明。", finding = "夜間処理とアクセス履歴を照合し、影響のある端末とデータの範囲を確認した。", lesson = "backup", calm = "検証用の変換処理が原因。攻撃ではなかったが、戻せることの確認は役に立つ。" },
new OpsMonth { name = "5月", season = "新しい仲間", title = "新入社員のアカウント管理", news = "使い回された認証情報から、業務サービスへ不正アクセス。", boss = "新人も増えたし、ログイン周りを整えたい。でも面倒だと使ってくれないよね。", person = "総務・小川", staff = "営業の相田さん：「ログイン通知が来たけど、どこに相談すればいい？」", hint = "認証の強さと、相談してもらえる関係の両方を考えよう。", kind = "identity", @base = 33, @event = "見知らぬ端末のセッション", symptom = "本人が使っていない時間帯の接続。既存セッションも確認が必要。", finding = "登録端末とログを照合し、本人の操作かどうかを確認した。", lesson = "mfa", calm = "新しい会社端末の登録漏れだった。確認せず全員を止める必要はなかった。" },
new OpsMonth { name = "6月", season = "繁忙期の入口", title = "「急ぎの振込」を疑えるか", news = "役員を装った連絡を受け、取引先への送金先を変更した事例。", boss = "厳しいルールを増やせばいい？ 急ぎの案件まで止まるのは困るけど。", person = "経理部長・三浦", staff = "経理の森さん：「声は社長っぽかった。でもいつもの承認を飛ばしていいの？」", hint = "技術設備だけでなく、既知の連絡先と承認手順、相談文化が効く。", kind = "social", @base = 35, @event = "秘密の支払先変更依頼", symptom = "至急・内密という連絡。通常の確認手順を飛ばすよう求められている。", finding = "既知の連絡経路で依頼元を確認し、承認記録との食い違いを調べた。", lesson = "culture", calm = "本物の依頼だったが承認手順が抜けていた。手順に戻して処理できる。" },
new OpsMonth { name = "7月", season = "繁忙期", title = "遅いのは、攻撃のせい？", news = "アクセス集中によるサービス停止。原因は攻撃だけとは限らない。", boss = "売上が伸びてるのに、サイトが遅い。何でもセキュリティのせいにせず調べてほしい。", person = "営業部長・相田", staff = "受付担当：「再送を何回も押しています。余計に遅くしているかもしれません」", hint = "今月は性能と継続性も大事。止める以外の選択肢を増やしたい。", kind = "outage", @base = 39, @event = "受注システムが応答しない", symptom = "混雑で注文の処理が停滞。安全に迂回する経路があるか。", finding = "負荷と処理待ちを確認。影響する業務と回避可能な経路を特定した。", lesson = "availability" },
new OpsMonth { name = "8月", season = "夏季休暇", title = "担当者が休んでも回る会社へ", news = "属人化した運用で復旧が遅れ、担当者の休日呼び出しが続く。", boss = "休暇中まで電話するのは避けたい。任せられる仕組みにできる？", person = "社長・加藤", staff = "ひなた：「定型作業を減らせたら、調査や改善に時間を使えるのにね」", hint = "自動化と引継ぎは、次月以降に時間の余裕を生む。", kind = "outage", @base = 32, @event = "定期処理の停止", symptom = "担当者不在で定期処理が止まった。手順と連絡先が分かれば動ける。", finding = "処理の依存関係と担当範囲を確認。切戻しの手順を特定した。", lesson = "incident" },
new OpsMonth { name = "9月", season = "上期の振り返り", title = "「最新にする」は簡単じゃない", news = "未修正の問題を狙った攻撃。一方で更新による障害も報告。", boss = "全部すぐ更新すべき？ でも月末の請求処理は止められない。", person = "経理部長・三浦", staff = "制作の大野さん：「どのソフトが対象か、そもそも一覧がないです」", hint = "台帳があると、対象を絞って変更できる。", kind = "vulnerability", @base = 42, @event = "公開システムの異常な操作", symptom = "公開サービスに未修正の問題が疑われる。停止と限定対処の影響を比べたい。", finding = "利用バージョンと操作履歴を確認し、対象システムを絞り込んだ。", lesson = "patch" },
new OpsMonth { name = "10月", season = "下期の投資", title = "信頼できる取引先だから？", news = "外部サービスへの侵入が、接続先の企業にも影響。", boss = "取引先まで全部調べられないよね。どこまで自社で備えるべきだろう。", person = "社長・加藤", staff = "制作の大野さん：「外注先の更新ファイル、いつもそのまま入れています」", hint = "入口を完全に信用するより、広がらない仕組みも考える。", kind = "supply", @base = 45, @event = "外部接続からの不審な通信", symptom = "正規の接続に混じって普段と異なる動き。切断範囲の判断が必要。", finding = "外部接続の権限と通信先を確認。業務上必要な範囲との差を把握した。", lesson = "defense", calm = "取引先の正規の仕様変更。通知が届いていなかったことが原因。" },
new OpsMonth { name = "11月", season = "納品準備", title = "共有設定の見直し", news = "共有設定の誤りで、想定外の相手が資料を閲覧できる状態に。", boss = "便利さは残したい。禁止するだけだと、別のサービスを使い始めるよね。", person = "総務・小川", staff = "営業の相田さん：「誰まで見えるのか、自信がありません」", hint = "社員が迷った時に聞けることと、資産・権限の把握が支えになる。", kind = "leak", @base = 41, @event = "資料の公開範囲が広すぎる", symptom = "共有リンクに社外アクセス。正規の相手か、設定の誤りかを確認したい。", finding = "共有設定と閲覧履歴を照合し、業務上必要な公開範囲を確認した。", lesson = "least", calm = "承認済みの取引先アクセスだった。正しい共有方法を再確認できた。" },
new OpsMonth { name = "12月", season = "年末の山場", title = "忙しい時ほど、相談できるか", news = "繁忙期を狙い、確認を急がせる詐欺メールが増加。", boss = "注意喚起メールは出したけど、読まれているのかな。現場の負担も気になる。", person = "営業部長・相田", staff = "経理の森さん：「間違えたら怒られそうで、聞くのをためらっていました」", hint = "負担が大きいと相談が遅れる。休息も運用の一部。", kind = "social", @base = 44, @event = "締切直前の偽の修正依頼", symptom = "至急の依頼が複数届く。いつもの確認を省略してよいか迷っている。", finding = "依頼元を別経路で確認し、正式な変更依頼と突き合わせた。", lesson = "culture" },
new OpsMonth { name = "1月", season = "仕組みの見直し", title = "バックアップは、今も戻せる？", news = "バックアップは成功表示でも、復元に必要な条件が欠けていた事例。", boss = "去年整えた仕組みも、放っておけば古くなるんだね。試しておいた方がいい？", person = "社長・加藤", staff = "制作の大野さん：「新しい保存先も訓練の対象に入っていますか？」", hint = "設備の有無だけでなく、復元を確かめたかが差になる。", kind = "outage", @base = 39, @event = "保存装置の障害", symptom = "重要ファイルの保存装置が停止。復元元と復旧までの時間を確認したい。", finding = "保存範囲と復元条件を点検し、復旧の見通しを確認した。", lesson = "rto" },
new OpsMonth { name = "2月", season = "年度末への備え", title = "年度末の予算配分", news = "年度末の業務集中と、情報システムへの複数の侵入経路。", boss = "来期の投資も考えたい。でも今期を乗り切る予算まで使い切らないでほしい。", person = "社長・加藤", staff = "ひなた：「導入費だけじゃなく、毎月の維持費も積み上がってきたね」", hint = "強い設備も継続費がかかる。運用を続けられる構成を選ぼう。", kind = "identity", @base = 49, @event = "複数アカウントの不審な利用", symptom = "利用者の多い時期に認証済みの異常操作。業務を残しながら範囲を絞りたい。", finding = "正規利用と異常操作を分け、影響したアカウントを特定した。", lesson = "risk" },
new OpsMonth { name = "3月", season = "一年の集大成", title = "年度末の運用評価", news = "年度末の納品が集中。重要データの保護と復旧体制の確認が必要。", boss = "今年、何が変わったか教えてほしい。来年はどんな会社にしたい？", person = "社長・加藤", staff = "ひなた：「最初は一人で抱えていた仕事、今はどうなったかな」", hint = "無事故だけが評価ではない。気づく・戻す・支え合う力を振り返る。", kind = "ransom", @base = 53, @event = "共有領域の異常な書換え", symptom = "納品データに異常。影響の拡大を抑え、納品までに復旧させる必要がある。", finding = "影響範囲と復元可能なデータを照合。役割分担を確認した。", lesson = "bcp" },
};
// 各月の社内依頼。設備を整える道と現場で確認する道のどちらでも達成できる。
public static readonly OpsMission[] Missions = {
new OpsMission { title="納品データを戻す手段を示す", equipmentRoute="バックアップ＋復元訓練", fieldRoute="現状調査＋重要業務の確認", projectA="backup", projectB="drill", actionA="audit", actionB="map" },
new OpsMission { title="新人のログインを守る", equipmentRoute="多要素認証を展開", fieldRoute="現状調査＋社員との対話", projectA="mfa", actionA="audit", actionB="listen" },
new OpsMission { title="急ぎの依頼を安全に判断する", equipmentRoute="相談できる教育＋対応手順", fieldRoute="社員との対話＋業務の確認", projectA="education", projectB="runbook", actionA="listen", actionB="map" },
new OpsMission { title="受注を止めない体制を作る", equipmentRoute="代替経路と冗長化", fieldRoute="業務の確認＋当番の休息", projectA="redundancy", actionA="map", actionB="rest" },
new OpsMission { title="担当者が休める運用にする", equipmentRoute="自動化＋引継ぎ手順", fieldRoute="当番の休息＋社員との対話", projectA="automation", projectB="runbook", actionA="rest", actionB="listen" },
new OpsMission { title="更新対象を絞り込む", equipmentRoute="更新の運用＋資産台帳", fieldRoute="現状調査＋重要業務の確認", projectA="patch", projectB="inventory", actionA="audit", actionB="map" },
new OpsMission { title="取引先からの影響を限定する", equipmentRoute="監視＋ネットワーク分離", fieldRoute="現状調査＋重要業務の確認", projectA="monitor", projectB="segment", actionA="audit", actionB="map" },
new OpsMission { title="共有範囲の判断を支える", equipmentRoute="資産台帳＋相談できる教育", fieldRoute="社員との対話＋業務の確認", projectA="inventory", projectB="education", actionA="listen", actionB="map" },
new OpsMission { title="忙しい時も報告できる", equipmentRoute="相談できる教育＋対応手順", fieldRoute="社員との対話＋当番の休息", projectA="education", projectB="runbook", actionA="listen", actionB="rest" },
new OpsMission { title="復元できる状態を確かめる", equipmentRoute="バックアップ＋復元訓練", fieldRoute="現状調査＋重要業務の確認", projectA="backup", projectB="drill", actionA="audit", actionB="map" },
new OpsMission { title="異常なログインを見分ける", equipmentRoute="多要素認証＋監視", fieldRoute="現状調査＋社員との対話", projectA="mfa", projectB="monitor", actionA="audit", actionB="listen" },
new OpsMission { title="年度末の納品を守る", equipmentRoute="バックアップ＋引継ぎ手順", fieldRoute="現状調査＋重要業務の確認", projectA="backup", projectB="runbook", actionA="audit", actionB="map" },
};
public static readonly OpsTerm[] Terms = {
new OpsTerm { id="asset", name="資産管理・業務影響", basic="守る対象と、その停止で困る仕事を把握することが出発点。", deep="台帳には機器名だけでなく、所有者、用途、依存関係、情報の重要性なども関係する。ゲームの台帳レベルは、この把握を簡略化したもの。" },
new OpsTerm { id="backup", name="バックアップと復元", basic="コピーを作ることと、必要なデータを使える状態に戻せることは別。", deep="復元元の保護、保存世代、復元手順、検証が重要。バックアップは主に復旧を支えるもので、侵入そのものを防ぐ対策とは区別する。" },
new OpsTerm { id="rto", name="RTO / RPO", basic="どれくらいで戻す必要があるか、どこまでのデータ損失を許容するか。", deep="RTOは目標復旧時間、RPOは目標復旧時点。業務の要求に沿って決め、復元訓練などで実現可能性を確認する。ゲーム内の数値は実測時間ではない。" },
new OpsTerm { id="mfa", name="多要素認証", basic="異なる種類の認証要素を組み合わせ、パスワードだけに頼らない。", deep="知識・所持・生体などの要素を組み合わせる。同種の秘密を二つ聞くだけでは多要素とは限らない。セッション管理や復旧手順も別途必要で、万能ではない。" },
new OpsTerm { id="detect", name="検知と通知", basic="異常を記録するだけでなく、判断し対応する人へ届いて初めて役立つ。", deep="平常時の状態、ログの相関、通知先、優先順位、誤検知への対処を考える。大量の通知が逆に見落としにつながることもある。" },
new OpsTerm { id="defense", name="多層防御・影響限定", basic="一つの対策が破られても、全部の仕事が巻き込まれないようにする。", deep="入口の防御、権限の制限、ネットワークの分離、監視、復旧などは役割が異なる。対策の数だけを増やすのではなく、守りたい経路と影響を考える。" },
new OpsTerm { id="culture", name="報告しやすい組織", basic="怪しい・間違えたかも、を早く相談できれば対応の選択肢が増える。", deep="教育の受講回数だけでなく、実際に報告できる窓口、報告への反応、業務負担も考える。ゲームの「相談文化」は、これらをまとめた架空の指標。" },
new OpsTerm { id="change", name="変更管理と自動化", basic="同じ仕事を減らす一方、変更の影響と戻し方も確認する。", deep="自動化は誤った処理も速く繰り返す。検証、適用範囲、監視、切戻しを含む運用が必要。この試作では成熟度をレベルにまとめている。" },
new OpsTerm { id="patch", name="脆弱性管理", basic="何が対象かを把握し、リスクと業務影響を考えて対処する。", deep="更新の優先度には悪用状況、公開範囲、資産の重要性などが関係する。更新できない間の緩和策も検討し、更新後の動作を確かめる。" },
new OpsTerm { id="availability", name="可用性・冗長化", basic="必要な時に仕事を続けられるよう、代替手段を用意する。", deep="冗長化とバックアップは役割が違う。同じデータ破損が両系に伝わる可能性もあるため、冗長化だけで世代復元までできるとは限らない。" },
new OpsTerm { id="incident", name="インシデント対応", basic="把握、連絡、影響の限定、復旧をつなぐ。人が替わっても動けるようにする。", deep="役割、連絡先、判断権限、記録、証拠の保全、復旧の確認、振り返りが関係する。ゲームの3択は現実の手順全体を置き換えるものではない。" },
new OpsTerm { id="least", name="最小権限", basic="その仕事に必要な範囲へ権限を絞る。", deep="付与時だけでなく、異動・退職・契約変更などに合わせて見直す。利便性との調整には、業務の実態を把握することも必要。" },
new OpsTerm { id="risk", name="リスクへの対応", basic="すべてに同じ費用をかけず、影響と備える負担を考える。", deep="低減、回避、移転・共有、受容などの選択がある。受容は放置と同義ではなく、残るリスクを把握し、適切な権限で判断する。" },
new OpsTerm { id="bcp", name="事業継続", basic="システムを直すだけでなく、重要な仕事を続ける・戻すことを考える。", deep="代替業務、役割、連絡、復旧の優先順位などを含む。現場と経営の要求をすり合わせ、訓練で確かめる。" },
};
public static int Index(string id) => Array.FindIndex(Projects, p => p.id == id);
public static OpsTerm Term(string id) => Array.Find(Terms, t => t.id == id);
}
}
