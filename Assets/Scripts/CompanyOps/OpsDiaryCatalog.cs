using System;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public sealed class OpsDiaryEntry
    {
        public int year,month;
        public string body,intro,memo,voiceId,face,pose;
    }
    [Serializable] public sealed class OpsDiaryRecord
    {
        public int key,content,mood;
        public string recap,thought,rank,minigame,season;
        public bool yearEnd,continues;
        public string[] monthNotes;
        public string bestEquipment,bestSupport;
        public bool Valid()=>key>=0&&key<OpsDiaryCatalog.Entries.Length&&content>=0&&content<OpsDiaryCatalog.Entries.Length&&mood>=0&&mood<5&&recap!=null&&recap.Length<=1800&&thought!=null&&thought.Length<=800&&rank!=null&&OpsStory.RankValue(rank)>=0&&minigame!=null&&minigame.Length<=40&&(season==null||season.Length<=80)&&(bestEquipment==null||bestEquipment.Length<=140)&&(bestSupport==null||bestSupport.Length<=140)&&(monthNotes==null||monthNotes.Length<=12&&monthNotes.All(s=>s!=null&&s.Length<=140));
        public static readonly string[] PageTitles={"こっそり復元","付箋","かのん","温度計","手順書","過去問","ポテト","お菓子","相談","だめだった","毎晩5分","付箋だらけ","受かった！","問い合わせ","AIのルール","引っ越し","講習会","ログ読み","お休み","会場の試験","後輩の準備","ケーキ","書き直し","振り返り","後輩ちゃん","ありがとう","判断","夜が短い","お祭り","質問票","ひとりで対応","試験","3年分","先輩へ","手紙","おわり"};
    }
    // 承認設計表の本文・冒頭・あるあるをそのまま保持。実記録と連載は別欄で読む。
    public static class OpsDiaryCatalog
    {
        public const int FailEnding=36,ClearEnding=37,SSEnding=38;
        public static readonly OpsDiaryEntry[] Entries={
            new OpsDiaryEntry{year=1,month=0,body="夜に一人で復元を試したら、戻す先を本番のフォルダにしかけて冷や汗。先輩に「復元は、まず別の場所へ」と教わる",intro="こっそり復元を試したら、本番に上書きしかけた……",memo="「バックアップあります」と「戻せます」は、別の話",voiceId="diary_y1_04",face="face_embarrassed",pose="pose_think"},
            new OpsDiaryEntry{year=1,month=1,body="新入社員のPCを20台、初期設定（キッティング）。1台ずつ手作業で、名前の付け間違いが2台。モニターにパスワードの付箋を見つけ、言い方に悩む",intro="新人さんのPC、20台セットアップした！……付箋、見つけちゃった",memo="4月は、PCとアカウントとお問い合わせが一度に来る",voiceId="diary_y1_05",face="face_worried",pose="pose_laptop"},
            new OpsDiaryEntry{year=1,month=2,body="かのん初登場。予算の資料を直してもらう。「“入れたら何が減るか”で語るのよ。止まる時間、失う売上」",intro="社長秘書のかのんさんに、予算の書き方を教わったよ！",memo="何も起きなかった年ほど、「その予算いる？」と言われる",voiceId="diary_y1_06_kanon",face="face_sparkle",pose="pose_fists"},
            new OpsDiaryEntry{year=1,month=3,body="「ネットが遅い」の原因は、会議室のハブにLANケーブルが輪っかにつながっていたこと（ループ）。抜いたら全部直った",intro="ネットが遅い原因、会議室のケーブルが輪っかになってたの",memo="「ネットが遅い」の正体は、だいたいWi-FiかDNSかケーブル",voiceId="diary_y1_07",face="face_doya",pose="pose_point"},
            new OpsDiaryEntry{year=1,month=4,body="夏休み中に、サイトの証明書の期限が切れて警告が出た。期限を誰の予定表にも入れていなかった。休み明けに全部の期限を一覧にする",intro="休み中に、証明書の期限が切れちゃってた……",memo="証明書の期限は、担当者がいなくなった年に切れる",voiceId="diary_y1_08",face="face_sad",pose="pose_exhausted"},
            new OpsDiaryEntry{year=1,month=5,body="試験の勉強を始める。「更新はすぐ全台に」ではなく、まず数台で試す理由（業務ソフトが動かなくなることがある）が分かった",intro="試験の勉強、はじめました！　過去問、むずかしい……",memo="更新のあと再起動しない人、だいたい忙しいだけ",voiceId="diary_y1_09",face="face_determined",pose="pose_magnifier"},
            new OpsDiaryEntry{year=1,month=6,body="開発部のセキュリティエンジニア、りりぃさんが情シスを手伝いに来て、初めて一緒に仕事をした。ポテトを食べながら、ログの中の総当たり（同じ所からの何十回ものログイン失敗）を一瞬で見つける。弟子入りを頼んだら「弟子はいらない。一緒にやればいい」と言われた",intro="開発部のりりぃさん、すごかった。ポテト食べてたけど",memo="夜中のアラートは、8割が誤報。でも残りの2割のために起きる",voiceId="diary_y1_10_v3",face="face_sparkle",pose="pose_point"},
            new OpsDiaryEntry{year=1,month=7,body="共有フォルダの権限の棚卸し。台帳（Excel）と実際の設定が合わない。おかしな設定を見つけたら、設定したのは去年の自分だった",intro="権限の棚卸しで、変な設定を見つけた。……設定したの、わたしだった",memo="台帳のExcelは、更新した人しか信じていない",voiceId="diary_y1_11",face="face_embarrassed",pose="pose_think"},
            new OpsDiaryEntry{year=1,month=8,body="年末の大掃除で、サーバー室のケーブルに全部ラベルを貼った。その日、3人が相談に来てくれた",intro="サーバー室のケーブルに、ぜんぶラベル貼ったよ！",memo="年末は「来年でいいや」の相談が、全部年内に来る",voiceId="diary_y1_12",face="face_proud",pose="pose_peace"},
            new OpsDiaryEntry{year=1,month=9,body="試験に落ちた。悔しくて帰り道で泣いた。りりぃさんは「試験は一回で受かったけど、会議は三回すっぽかした。……得意と苦手は、人それぞれ」と言ってくれた",intro="……試験、だめだった",memo="再起動で直ると、ほっとするけど、ちょっと不安",voiceId="diary_y1_01",face="face_crying",pose="pose_exhausted"},
            new OpsDiaryEntry{year=1,month=10,body="毎晩5分、ログを読む練習を始める。「いつも」を知らないと「いつもと違う」は分からない、とりりぃさん",intro="毎晩5分、ログを読むことにしたよ。続けられるかな",memo="「何もしてないのに壊れた」は、本人も本当に心当たりがない。だから記録が大事",voiceId="diary_y1_02",face="face_determined",pose="pose_fists"},
            new OpsDiaryEntry{year=1,month=11,body="手順書の第1版ができた。先輩から付箋だらけで戻ってきた。いちばん大きな付箋は「元に戻す手順がない」",intro="手順書、できた！……付箋だらけで戻ってきたけど",memo="手順書は、書いた本人がいちばん読まない",voiceId="diary_y1_03",face="face_relieved",pose="pose_peace"},
            new OpsDiaryEntry{year=2,month=0,body="2月の再受験で合格していた！ 新しい拠点の担当に。拠点のルーターの管理パスワードが、出荷時のままだった",intro="受かってたーっ！……拠点のルーター、パスワードが初期のままだった",memo="機器の管理画面のパスワード、箱に書いてある",voiceId="diary_y2_04",face="face_sparkle",pose="pose_jump"},
            new OpsDiaryEntry{year=2,month=1,body="在宅の人が増えて「VPNがつながらない」の問い合わせが倍に。半分は家のWi-Fiだった。よくある質問の一覧を作る",intro="VPNがつながらない問い合わせ、半分は家のWi-Fiだった",memo="「画面が真っ黒です」の半分は、電源が入っていない。もう半分は、本当に大変",voiceId="diary_y2_05",face="face_akire",pose="pose_think"},
            new OpsDiaryEntry{year=2,month=2,body="かのんさんとAIの使い方の決まりを作る。「全部禁止すると、みんな隠れて使うわ。使っていい道を作るの」",intro="かのんさんと、AIのルールを考えたよ",memo="禁止すると、隠れて使われる",voiceId="diary_y2_06_kanon",face="face_normal",pose="pose_point"},
            new OpsDiaryEntry{year=2,month=3,body="クラウドへの引っ越しの夜。名前の切り替え（DNS）は、前の日に「覚えておく時間」（TTL）を短くしておく、とりりぃさんに教わる。二人で朝までやった",intro="DNSの切り替え、前の日にTTLを短くしておくの、覚えた！",memo="「ちょっとした変更」は、金曜の夕方にやらない",voiceId="diary_y2_07",face="face_doya",pose="pose_laptop"},
            new OpsDiaryEntry{year=2,month=4,body="拠点で講習会。「パスワード付きのZIPはなぜだめ？」に、うまく答えられなかった（パスワードを同じメールの経路で送ると、一緒に盗まれる）。準備ノートを作る",intro="講習会、質問にちゃんと答えられなかった。悔しい",memo="説明会でいちばん鋭い質問は、いちばん後ろの席から来る",voiceId="diary_y2_08",face="face_pout",pose="pose_think"},
            new OpsDiaryEntry{year=2,month=5,body="応用情報の勉強。毎晩のログ読みのおかげで、午後の問題の図が読めるようになってきた",intro="毎晩のログ読み、役に立ってる気がする！",memo="「とりあえず全部管理者権限で」は、あとで必ず困る",voiceId="diary_y2_09",face="face_sparkle",pose="pose_magnifier"},
            new OpsDiaryEntry{year=2,month=6,body="熱を出して休んだ。でも、手順書とよくある質問のおかげで、みんなで回してくれた",intro="休んじゃった。でも、会社はちゃんと回ってた",memo="担当者が休んだ日に、その担当のシステムが止まる",voiceId="diary_y2_10",face="face_relieved",pose="pose_coffee"},
            new OpsDiaryEntry{year=2,month=7,body="応用情報を、試験会場のパソコンで受けてきた（CBT）。手応えは、半分くらい",intro="試験、終わった……。半分くらい、できたかな",memo="試験の日に限って、会社から電話が来る",voiceId="diary_y2_11",face="face_worried",pose="pose_typing"},
            new OpsDiaryEntry{year=2,month=8,body="結果待ちの年末。来年、後輩が入ると聞いて緊張。新しいPCの初期設定を自動にする仕組み（端末管理）を準備する。去年の20台の手作業を思い出す",intro="来年、後輩ができるんだって。どうしよう、緊張する",memo="自動化の準備には、手作業の3倍かかる。でも2回目から楽",voiceId="diary_y2_12",face="face_worried",pose="pose_please"},
            new OpsDiaryEntry{year=2,month=9,body="応用情報の結果が届いた。合格。かのんさんがケーキを買ってきた",intro="合格してた！　かのんさんがケーキ買ってきてくれたの",memo="年度末の駆け込みのライセンス購入、毎年ある",voiceId="diary_y2_01_kanon",face="face_sparkle",pose="pose_jump"},
            new OpsDiaryEntry{year=2,month=10,body="手順書を他の人に試してもらったら「どこを押すのか分からない」。画面の写真と、「なぜこうするか」を書き足す",intro="手順書、分かりにくいって言われちゃった。書き直し！",memo="「たぶん大丈夫」は、大丈夫じゃない",voiceId="diary_y2_02",face="face_determined",pose="pose_typing"},
            new OpsDiaryEntry{year=2,month=11,body="2年目の障害の振り返りを書く。「誰が悪いか」ではなく「なぜ起きたか、次に何を変えるか」で書く",intro="来年は、わたしが教える番なんだね",memo="障害の振り返りで「気をつける」と書いたら、だいたいまた起きる",voiceId="diary_y2_03",face="face_relieved",pose="pose_wave"},
            new OpsDiaryEntry{year=3,month=0,body="後輩が来た。最初に伝えたのは「分からないときは、すぐ聞いていい」。自分が先輩に言われたこと",intro="後輩ちゃんが来たよ。わたし、先輩になっちゃった",memo="新人さんの「すみません」は、だいたい謝らなくていいこと",voiceId="diary_y3_04",face="face_embarrassed",pose="pose_wave"},
            new OpsDiaryEntry{year=3,month=1,body="後輩が怪しいメールのリンクを押して、すぐ報告に来た。端末をネットから外して調べたら、被害なし。責めずに「すぐ言ってくれてありがとう」と言えた",intro="後輩ちゃん、すぐ報告してくれた。ちゃんと、ありがとうって言えたよ",memo="報告が早い職場は、事故が小さい",voiceId="diary_y3_05",face="face_relieved",pose="pose_peace"},
            new OpsDiaryEntry{year=3,month=2,body="大きな山場。りりぃさんに「判断が早くなったね」と言われる。止める範囲を、台帳を見てすぐ決められた",intro="りりぃさんに、判断が早くなったって言われた！",memo="「全部止めて」は簡単。「ここだけ止める」が仕事",voiceId="diary_y3_06_v2",face="face_sparkle",pose="pose_fists"},
            new OpsDiaryEntry{year=3,month=3,body="支援士の勉強。午後の問題は、長い事例の文章を読んで答える。夜が短い",intro="支援士の午後問題、長い……夜が足りない",memo="夏はサーバー室の空調が、いちばん大事な設備",voiceId="diary_y3_07",face="face_sleepy",pose="pose_coffee"},
            new OpsDiaryEntry{year=3,month=4,body="3人で夏祭り。かのんさんの浴衣。仕事の話を一回もしない日",intro="今日は3人でお祭り。仕事の話、一回もしなかった",memo="休みの日に通知を切れるようになったら、一人前",voiceId="diary_y3_08",face="face_normal",pose="pose_peace"},
            new OpsDiaryEntry{year=3,month=5,body="取引先のセキュリティ確認の質問票、200問。3年前なら半分も答えられなかった。台帳と手順書があったから、全部に根拠つきで答えられた",intro="監査の質問、全部答えられたの。3年前のわたしに見せたい",memo="質問票の「はい」には、全部根拠を聞かれる",voiceId="diary_y3_09",face="face_proud",pose="pose_shield"},
            new OpsDiaryEntry{year=3,month=6,body="後輩が初めて一人で障害に対応できた。まず影響の範囲を確かめて、次に元に戻す手順を確認してから作業していた",intro="後輩ちゃんが、ひとりで直せたの！",memo="「切り戻し手順あります」の一言で、夜が平和になる",voiceId="diary_y3_10",face="face_sparkle",pose="pose_jump"},
            new OpsDiaryEntry{year=3,month=7,body="支援士を、試験会場のパソコンで受けてきた（CBT）。終わってから、少しだけ泣いた",intro="試験、終わった。ちょっとだけ、泣いちゃった",memo="「一時的な設定」は、3年後も残っている",voiceId="diary_y3_11",face="face_crying",pose="pose_bow"},
            new OpsDiaryEntry{year=3,month=8,body="結果待ちの年末。3年分の日記を読み返す。最初のページの字がぐちゃぐちゃ",intro="最初の日記、読み返したら、字がぐちゃぐちゃだった",memo="3年前の自分の設定に、助けられる日もある",voiceId="diary_y3_12",face="face_embarrassed",pose="pose_think"},
            new OpsDiaryEntry{year=3,month=9,body="支援士の結果が届いた。合格。最初に先輩に伝えた",intro="合格だって。最初に、先輩に言いたかったの",memo="合格しても、問い合わせの電話は鳴る",voiceId="diary_y3_01",face="face_sparkle",pose="pose_please"},
            new OpsDiaryEntry{year=3,month=10,body="先輩への手紙のような日記。教わったことを、全部書き出す",intro="先輩。いつもありがとう、って、ここに書いておくね",memo="情シスの仕事は、うまくいくほど誰にも気づかれない",voiceId="diary_y3_02",face="face_relieved",pose="pose_bow"},
            new OpsDiaryEntry{year=3,month=11,body="SS：何も起きない朝が、いちばん嬉しい。／A・S：守り抜いた。次の目標を見つけた。／途中で終了：それでも、明日も会社は続く。もう一回",intro="",memo="「なにごともない」は、だれかが毎日つくっている",voiceId="",face="face_relieved",pose="pose_peace"},
            new OpsDiaryEntry{year=3,month=11,body="それでも、明日も会社は続く。もう一回",intro="ここで終わりじゃないよ。明日も、会社は続くから",memo="「なにごともない」は、だれかが毎日つくっている",voiceId="diary_y3_03_fail",face="face_determined",pose="pose_fists"},
            new OpsDiaryEntry{year=3,month=11,body="守り抜いた。次の目標を見つけた。",intro="3年間、守り抜いたよ。次は、何を目指そうかな",memo="「なにごともない」は、だれかが毎日つくっている",voiceId="diary_y3_03_clear",face="face_proud",pose="pose_salute"},
            new OpsDiaryEntry{year=3,month=11,body="何も起きない朝が、いちばん嬉しい。",intro="なにごともない朝って、こんなに嬉しいんだね",memo="「なにごともない」は、だれかが毎日つくっている",voiceId="diary_y3_03_ss",face="face_relieved",pose="pose_peace"},
        };
        public static int Page(int year,int month)=>Math.Max(0,Math.Min(2,year-1))*12+month;
        public static int Mood(OpsOutcome r)=>r.benign?r.response=="contain"&&r.downtime>0?3:4:r.loss>10||r.downtime>8?2:r.loss<=3&&r.downtime<=3?0:1;
        private static readonly string[][] Thoughts={
            new[]{"被害を小さくできた。戻す備えも、大事だね。","被害は小さく済んだ。接続先も見直そう。","被害は小さく済んだね。使っていい道を作ろう。","影響は小さく済んだ。対象と戻し方も残そう。","被害は小さく済んだ。いつもとの違いを覚えておこう。","被害を抑えられた。急ぐときほど、ひと呼吸。","被害を小さくできた。台帳も直しておこう。","被害は小さく済んだ。つながる道を守ろう。","停止は短く済んだ。次も準備しておこう。","設定のまちがいって、こわい。次の作業でも確かめよう。"},
            new[]{"止める備えと戻す備え。どちらも見直してみよう。","相手も自分たちも、接続の範囲を確認したいね。","便利な道具だからこそ、見える範囲を確かめよう。","更新したあとの業務の確認まで、手順に残そう。","いつもを知らないと、違いには気づけないんだね。","依頼を確かめる窓口を、先に決めておこう。","必要な権限と、残った権限を分けて見よう。","つながらないときの問い合わせ先も、用意しておこう。","止まったときの代替業務も、もっと練習したい。","設定と使い方を、もう一度いっしょに確認しよう。"},
            new[]{"くやしい。でも、記録から次に備えることを探そう。","どこまでつながっていたか、次はすぐ分かるようにしたい。","禁止だけでは防げない。安全に使える方法を考えたい。","良い更新でも、業務は止まる。戻す段取りを残そう。","被害が大きかった。通知と記録を見直そう。","顔や声も確かとは限らない。別の経路を作ろう。","必要な権限を見つけられる台帳にしておきたい。","接続できない人にも、代替の手段が必要なんだね。","影響が大きかった。戻す段取りも訓練しておこう。","設定の確認を、作業の最後に忘れないようにしたい。"},
            new[]{"止めすぎたかも。次は、確かめてから範囲を決めよう。","必要な接続も止めてしまった。台帳で範囲を確かめよう。","安全な利用まで止めない方法を、考えておこう。","更新の確認と全停止は違う。対象を先に確かめよう。","疑わしいだけで決めつけない。記録を調べてからにしよう。","大事な依頼も止まった。別の窓口で先に確かめよう。","必要な仕事まで止めないよう、権限を確かめよう。","正しい接続も止めてしまった。本人の確認を先にしよう。","止めることで困る人もいる。影響の範囲を確かめよう。","正常な共有まで止めないよう、設定と相手を確認しよう。"},
            new[]{"異常に見えたけど正常だった。確かめた記録を残しておこう。","正常だったね。相手と時間の確認も続けよう。","正常だったね。ルールを確かめることも大切だね。","正常だった。作業の記録も残しておこう。","正常だった。いつもの動きを知ることも大事だね。","正常だった。依頼を確かめる道も用意したいね。","正常だった。利用者に聞くと分かることもあるね。","正常だった。接続の確認も続けよう。","正常だった。作業の連絡も残しておこう。","正常だった。設定と利用者も確かめておこう。"}
        };
        public static string Thought(OpsOutcome r)
        {
            string p=OpsEventCatalog.Event(r.eventId)?.profile??"service";
            int topic=p=="ransom"?0:p=="supply"?1:p=="ai"?2:p=="vulnerability"||p=="change"?3:p=="targeted"||p=="claim"?4:p=="bec"?5:p=="insider"||p=="session"?6:p=="remote"||p=="device"?7:p=="sharing"?9:8;
            return Thoughts[Mood(r)][topic];
        }
        public static string Recap(OpsState state,OpsOutcome r)
        {
            string response=r.response=="contain"?"広範囲を停止":r.response=="scope"?"対象を限定して対応":"復旧を優先";
            string support=r.power!=null&&r.power.staff>0&&!r.benign?"\n"+r.power.support+"で助けてくれた。":"";
            return (r.eventTitle??state.MonthAt(r.month).title)+"。\n"+response+"。\n被害は "+r.loss+"万円、止まったのは "+r.downtime+"時間。"+(r.benign?"\n調べた結果は正常だった。":"")+support;
        }
    }
}

