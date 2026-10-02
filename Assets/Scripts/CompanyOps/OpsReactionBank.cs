using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public enum OpsReaction { Think, Purchase, Growth, Alert, Success, Recover, Month, Clear, Failure }

    [Serializable] public sealed class OpsReactionLine
    {
        public string id, caption;
        // 空欄は従来のひなた。仲間の音声も同じ再生経路を使う。
        public string speaker;
        public OpsReaction reaction;
        public AudioClip clip;
        public string faceId, poseId;
        public string scene;
        public bool fullSpeech, extra;
    }

    // 音声は制作時に収録・生成して取り込む。プレイ中の外部通信・APIキーは不要。
    [CreateAssetMenu(menuName = "PatchWorkSecure/情シスの一年/キャラの短い反応")]
    public sealed class OpsReactionBank : ScriptableObject
    {
        public const string ProductionVoice = "ElevenLabs / Hinata V9-2";
        public OpsReactionLine[] lines = Array.Empty<OpsReactionLine>();
        public bool HasAudio => lines != null && lines.Any(l => l != null && l.clip != null);
        public OpsReactionLine Find(string id) => lines?.FirstOrDefault(l => l!=null && l.id==id);
        public static OpsReactionLine[] Defaults() => ScriptV2().Where(IsGeneralReaction).ToArray();
        public static bool IsGeneralReaction(OpsReactionLine line)=>line!=null&&!line.fullSpeech&&!line.extra&&!(line.id??"").StartsWith("mg_")&&!(line.id??"").StartsWith("maxim_");
        // 台本v2の字幕・表情・ポーズ。音声が無い取得直後の環境でも同じ内容を使う。
        public static OpsReactionLine[] ScriptV2() => new[] {
            new OpsReactionLine { id="think_01", caption="う〜ん……", scene="考える", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_think", fullSpeech=false, extra=false },
            new OpsReactionLine { id="think_02", caption="なるほどね！", scene="考える", reaction=OpsReaction.Think, faceId="face_sparkle", poseId="pose_point", fullSpeech=false, extra=false },
            new OpsReactionLine { id="think_03", caption="そっか〜。", scene="考える", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_think", fullSpeech=false, extra=false },
            new OpsReactionLine { id="think_04", caption="ちょっと考えよっか。", scene="考える", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_think", fullSpeech=false, extra=false },
            new OpsReactionLine { id="think_05", caption="どれにしよっかな〜。", scene="考える", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_think", fullSpeech=false, extra=false },
            new OpsReactionLine { id="think_06", caption="ふむふむ。", scene="考える", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_magnifier", fullSpeech=false, extra=false },
            new OpsReactionLine { id="purchase_01", caption="よしっ、準備オッケー！", scene="設備の導入", reaction=OpsReaction.Purchase, faceId="face_proud", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="purchase_02", caption="これで一安心だね！", scene="設備の導入", reaction=OpsReaction.Purchase, faceId="face_relieved", poseId="pose_laptop", fullSpeech=false, extra=false },
            new OpsReactionLine { id="purchase_03", caption="いい買い物しちゃった！", scene="設備の導入", reaction=OpsReaction.Purchase, faceId="face_sparkle", poseId="pose_peace", fullSpeech=false, extra=false },
            new OpsReactionLine { id="purchase_04", caption="頼りになるぞ〜！", scene="設備の導入", reaction=OpsReaction.Purchase, faceId="face_proud", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="purchase_05", caption="さっそく設定するね！", scene="設備の導入", reaction=OpsReaction.Purchase, faceId="face_determined", poseId="pose_typing", fullSpeech=false, extra=false },
            new OpsReactionLine { id="purchase_06", caption="ふふーん、どうよ！", scene="設備の導入", reaction=OpsReaction.Purchase, faceId="face_doya", poseId="pose_armscross", fullSpeech=false, extra=false },
            new OpsReactionLine { id="growth_01", caption="やった〜！", scene="成長・ランクアップ", reaction=OpsReaction.Growth, faceId="face_sparkle", poseId="pose_jump", fullSpeech=false, extra=false },
            new OpsReactionLine { id="growth_02", caption="いい感じじゃん！", scene="成長・ランクアップ", reaction=OpsReaction.Growth, faceId="face_proud", poseId="pose_peace", fullSpeech=false, extra=false },
            new OpsReactionLine { id="growth_03", caption="会社、強くなってる！", scene="成長・ランクアップ", reaction=OpsReaction.Growth, faceId="face_sparkle", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="growth_04", caption="いえーいっ！", scene="成長・ランクアップ", reaction=OpsReaction.Growth, faceId="face_sparkle", poseId="pose_jump", fullSpeech=false, extra=false },
            new OpsReactionLine { id="growth_05", caption="ね、言ったでしょ！", scene="成長・ランクアップ", reaction=OpsReaction.Growth, faceId="face_doya", poseId="pose_point", fullSpeech=false, extra=false },
            new OpsReactionLine { id="growth_06", caption="すごいすごい！", scene="成長・ランクアップ", reaction=OpsReaction.Growth, faceId="face_sparkle", poseId="pose_jump", fullSpeech=false, extra=false },
            new OpsReactionLine { id="alert_01", caption="えぇっ！？", scene="警戒・事件の兆候", reaction=OpsReaction.Alert, faceId="face_alert", poseId="pose_startled", fullSpeech=false, extra=false },
            new OpsReactionLine { id="alert_02", caption="あれっ？", scene="警戒・事件の兆候", reaction=OpsReaction.Alert, faceId="face_worried", poseId="pose_think", fullSpeech=false, extra=false },
            new OpsReactionLine { id="alert_03", caption="わわっ、ちょっと待って！", scene="警戒・事件の兆候", reaction=OpsReaction.Alert, faceId="face_panic", poseId="pose_startled", fullSpeech=false, extra=false },
            new OpsReactionLine { id="alert_04", caption="ん？　なんか変じゃない？", scene="警戒・事件の兆候", reaction=OpsReaction.Alert, faceId="face_worried", poseId="pose_magnifier", fullSpeech=false, extra=false },
            new OpsReactionLine { id="alert_05", caption="落ち着いて、確認しよ！", scene="警戒・事件の兆候", reaction=OpsReaction.Alert, faceId="face_determined", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="alert_06", caption="うそうそ！？", scene="警戒・事件の兆候", reaction=OpsReaction.Alert, faceId="face_alert", poseId="pose_startled", fullSpeech=false, extra=false },
            new OpsReactionLine { id="success_01", caption="セーフ！", scene="うまくいった", reaction=OpsReaction.Success, faceId="face_relieved", poseId="pose_peace", fullSpeech=false, extra=false },
            new OpsReactionLine { id="success_02", caption="よかった〜！", scene="うまくいった", reaction=OpsReaction.Success, faceId="face_relieved", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="success_03", caption="ばっちりだね！", scene="うまくいった", reaction=OpsReaction.Success, faceId="face_sparkle", poseId="pose_peace", fullSpeech=false, extra=false },
            new OpsReactionLine { id="success_04", caption="ふぅ〜、ひと安心。", scene="うまくいった", reaction=OpsReaction.Success, faceId="face_relieved", poseId="pose_coffee", fullSpeech=false, extra=false },
            new OpsReactionLine { id="success_05", caption="えへへっ、やったね！", scene="うまくいった", reaction=OpsReaction.Success, faceId="face_sparkle", poseId="pose_peace", fullSpeech=false, extra=false },
            new OpsReactionLine { id="success_06", caption="おつかれさま！", scene="うまくいった", reaction=OpsReaction.Success, faceId="face_normal", poseId="pose_wave", fullSpeech=false, extra=false },
            new OpsReactionLine { id="recover_01", caption="よし、立て直そ！", scene="被害から立て直す", reaction=OpsReaction.Recover, faceId="face_determined", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="recover_02", caption="ここからだよ！", scene="被害から立て直す", reaction=OpsReaction.Recover, faceId="face_determined", poseId="pose_salute", fullSpeech=false, extra=false },
            new OpsReactionLine { id="recover_03", caption="大丈夫、まかせて！", scene="被害から立て直す", reaction=OpsReaction.Recover, faceId="face_determined", poseId="pose_shield", fullSpeech=false, extra=false },
            new OpsReactionLine { id="recover_04", caption="次は負けないもん！", scene="被害から立て直す", reaction=OpsReaction.Recover, faceId="face_pout", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="recover_05", caption="一緒に考えよ！", scene="被害から立て直す", reaction=OpsReaction.Recover, faceId="face_normal", poseId="pose_please", fullSpeech=false, extra=false },
            new OpsReactionLine { id="recover_06", caption="まだまだいける！", scene="被害から立て直す", reaction=OpsReaction.Recover, faceId="face_determined", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="month_01", caption="おはよ〜！", scene="月の始まり", reaction=OpsReaction.Month, faceId="face_normal", poseId="pose_wave", fullSpeech=false, extra=false },
            new OpsReactionLine { id="month_02", caption="今月もよろしくね！", scene="月の始まり", reaction=OpsReaction.Month, faceId="face_normal", poseId="pose_wave", fullSpeech=false, extra=false },
            new OpsReactionLine { id="month_03", caption="さ、始めよっか！", scene="月の始まり", reaction=OpsReaction.Month, faceId="face_determined", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="month_04", caption="今月は何しよっかな〜。", scene="月の始まり", reaction=OpsReaction.Month, faceId="face_normal", poseId="pose_think", fullSpeech=false, extra=false },
            new OpsReactionLine { id="month_05", caption="がんばろ〜っ！", scene="月の始まり", reaction=OpsReaction.Month, faceId="face_sparkle", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="month_06", caption="新しい月、スタート！", scene="月の始まり", reaction=OpsReaction.Month, faceId="face_sparkle", poseId="pose_salute", fullSpeech=false, extra=false },
            new OpsReactionLine { id="clear_01", caption="一年、おつかれさま！", scene="年度クリア", reaction=OpsReaction.Clear, faceId="face_relieved", poseId="pose_wave", fullSpeech=false, extra=false },
            new OpsReactionLine { id="clear_02", caption="守りきったね！", scene="年度クリア", reaction=OpsReaction.Clear, faceId="face_sparkle", poseId="pose_jump", fullSpeech=false, extra=false },
            new OpsReactionLine { id="clear_03", caption="みんなのおかげだよ！", scene="年度クリア", reaction=OpsReaction.Clear, faceId="face_crying", poseId="pose_jump", fullSpeech=false, extra=false },
            new OpsReactionLine { id="clear_04", caption="やった〜っ、いえーい！", scene="年度クリア", reaction=OpsReaction.Clear, faceId="face_sparkle", poseId="pose_jump", fullSpeech=false, extra=false },
            new OpsReactionLine { id="clear_05", caption="最高の一年だったね！", scene="年度クリア", reaction=OpsReaction.Clear, faceId="face_sparkle", poseId="pose_peace", fullSpeech=false, extra=false },
            new OpsReactionLine { id="clear_06", caption="来年もよろしくね！", scene="年度クリア", reaction=OpsReaction.Clear, faceId="face_normal", poseId="pose_wave", fullSpeech=false, extra=false },
            new OpsReactionLine { id="failure_01", caption="うぅ……くやしい！", scene="結果が悪かった", reaction=OpsReaction.Failure, faceId="face_sad", poseId="pose_exhausted", fullSpeech=false, extra=false },
            new OpsReactionLine { id="failure_02", caption="ガーン……。", scene="結果が悪かった", reaction=OpsReaction.Failure, faceId="face_shocked", poseId="pose_exhausted", fullSpeech=false, extra=false },
            new OpsReactionLine { id="failure_03", caption="次、がんばろ！", scene="結果が悪かった", reaction=OpsReaction.Failure, faceId="face_determined", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="failure_04", caption="振り返ってみよっか。", scene="結果が悪かった", reaction=OpsReaction.Failure, faceId="face_worried", poseId="pose_think", fullSpeech=false, extra=false },
            new OpsReactionLine { id="failure_05", caption="も〜、ついてないなぁ！", scene="結果が悪かった", reaction=OpsReaction.Failure, faceId="face_pout", poseId="pose_armscross", fullSpeech=false, extra=false },
            new OpsReactionLine { id="failure_06", caption="大丈夫、ここから！", scene="結果が悪かった", reaction=OpsReaction.Failure, faceId="face_determined", poseId="pose_fists", fullSpeech=false, extra=false },
            new OpsReactionLine { id="extra_doya", caption="ふふーん、どうよっ！", scene="備えが効いた・得意", reaction=OpsReaction.Think, faceId="face_doya", poseId="pose_armscross", fullSpeech=false, extra=true },
            new OpsReactionLine { id="extra_akire", caption="……はぁ。", scene="放置・同じ失敗", reaction=OpsReaction.Think, faceId="face_akire", poseId="pose_armscross", fullSpeech=false, extra=true },
            new OpsReactionLine { id="extra_akire_niyake", caption="も〜、しょうがないなぁ！", scene="ちょっと抜けた選択", reaction=OpsReaction.Think, faceId="face_akire_niyake", poseId="pose_armscross", fullSpeech=false, extra=true },
            new OpsReactionLine { id="extra_pout", caption="むぅ〜……。", scene="予算を削られた", reaction=OpsReaction.Think, faceId="face_pout", poseId="pose_armscross", fullSpeech=false, extra=true },
            new OpsReactionLine { id="extra_embarrassed", caption="えっ、そ、そうかな？", scene="ほめられた", reaction=OpsReaction.Think, faceId="face_embarrassed", poseId="pose_please", fullSpeech=false, extra=true },
            new OpsReactionLine { id="extra_panic", caption="わわわっ！", scene="大きな事件の発生", reaction=OpsReaction.Think, faceId="face_panic", poseId="pose_startled", fullSpeech=false, extra=true },
            new OpsReactionLine { id="extra_crying", caption="うわ〜ん！", scene="大きな被害", reaction=OpsReaction.Think, faceId="face_crying", poseId="pose_exhausted", fullSpeech=false, extra=true },
            new OpsReactionLine { id="extra_tease", caption="べーっだ！", scene="冗談", reaction=OpsReaction.Think, faceId="face_tease", poseId="pose_peace", fullSpeech=false, extra=true },
            new OpsReactionLine { id="extra_shocked", caption="ひゃっ！", scene="想定外の被害", reaction=OpsReaction.Think, faceId="face_shocked", poseId="pose_startled", fullSpeech=false, extra=true },
            new OpsReactionLine { id="extra_sleepy", caption="ふぁ……ねむ……。", scene="疲労が高い", reaction=OpsReaction.Think, faceId="face_sleepy", poseId="pose_exhausted", fullSpeech=false, extra=true },
            new OpsReactionLine { id="extra_determined", caption="よし、やるよっ！", scene="山場の月の始まり", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_shield", fullSpeech=false, extra=true },
            new OpsReactionLine { id="tutorial_1", caption="ようこそ、情シスへ！　上の数字が予算とこうすう、左が会社の力だよ！", scene="チュートリアル1", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_wave", fullSpeech=true, extra=false },
            new OpsReactionLine { id="tutorial_2", caption="まずは「調べる」を押してみて！　会社の今の状態が、ぱっと分かるよ！", scene="チュートリアル2", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_point", fullSpeech=true, extra=false },
            new OpsReactionLine { id="tutorial_3", caption="社長から相談が来てる！　「話を聞く」で、依頼をチェックしよ！", scene="チュートリアル3", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_point", fullSpeech=true, extra=false },
            new OpsReactionLine { id="tutorial_4", caption="「設備を導入」から、ひとつ入れてみよっか。備えは、事件のときに効いてくるんだ！", scene="チュートリアル4", reaction=OpsReaction.Think, faceId="face_proud", poseId="pose_laptop", fullSpeech=true, extra=false },
            new OpsReactionLine { id="tutorial_5", caption="準備できたら「月を進める」！　……さあ、どうなるかな？", scene="チュートリアル5", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_fists", fullSpeech=true, extra=false },
            new OpsReactionLine { id="tutorial_6", caption="事件が起きたら、方針を選ぶよ。入れておいた備えが、ここで働くの！", scene="チュートリアル6", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_shield", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_01", caption="納品データを戻す手段、一緒に示そ！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_fists", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_02", caption="新人さんのログイン、しっかり守ってあげよ！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_point", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_03", caption="急ぎの依頼でも、安全に判断できるようにしたいね！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_think", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_04", caption="受注を止めない体制、作っちゃお！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_fists", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_05", caption="担当さんが休めるように、仕組みを整えよ！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_relieved", poseId="pose_laptop", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_06", caption="更新する対象、まずは絞り込もっか！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_magnifier", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_07", caption="取引先からの影響、広がらないようにしよ！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_shield", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_08", caption="どこまで共有していいか、判断の目安を作ろ！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_think", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_09", caption="忙しいときでも、ちゃんと報告できる職場にしたいな！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_please", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_10", caption="本当に戻せるか、確かめておこ！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_magnifier", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_11", caption="あやしいログイン、見分けられるようにしよ！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_magnifier", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_accept_12", caption="年度末の納品、みんなで守りきろ！", scene="依頼を引き受ける", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_salute", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_done", caption="依頼、達成〜っ！　社長も喜んでくれるね！", scene="依頼の達成（月報）", reaction=OpsReaction.Think, faceId="face_sparkle", poseId="pose_jump", fullSpeech=true, extra=false },
            new OpsReactionLine { id="mission_miss", caption="今月は届かなかったか〜。来月、取り返そ！", scene="依頼の未達成（月報）", reaction=OpsReaction.Think, faceId="face_worried", poseId="pose_think", fullSpeech=true, extra=false },
            new OpsReactionLine { id="incident_start", caption="たいへん、何か起きてる！　まずは止める範囲を決めよ！", scene="事件の発生", reaction=OpsReaction.Think, faceId="face_alert", poseId="pose_startled", fullSpeech=true, extra=false },
            new OpsReactionLine { id="incident_unconfirmed", caption="まだ確認中だよ。普通の操作かもしれないから、落ち着いてね。", scene="事件の発生（未確認）", reaction=OpsReaction.Think, faceId="face_worried", poseId="pose_magnifier", fullSpeech=true, extra=false },
            new OpsReactionLine { id="incident_activate", caption="備えが効いた！　入れといて、ほんっとよかった〜！", scene="備えの発動", reaction=OpsReaction.Think, faceId="face_doya", poseId="pose_shield", fullSpeech=true, extra=false },
            new OpsReactionLine { id="incident_missing", caption="あれがあったら、もっと早く戻せたかも……。次は入れとこ！", scene="未導入の備え", reaction=OpsReaction.Think, faceId="face_worried", poseId="pose_think", fullSpeech=true, extra=false },
            new OpsReactionLine { id="rankup", caption="ランクアップ！　会社が、またひとつ強くなったよ！", scene="ランクアップ", reaction=OpsReaction.Think, faceId="face_sparkle", poseId="pose_jump", fullSpeech=true, extra=false },
            new OpsReactionLine { id="annual_a", caption="運用ランクA！　最初より、ずっとずっと強い会社になったね！", scene="年間評価A", reaction=OpsReaction.Think, faceId="face_crying", poseId="pose_jump", fullSpeech=true, extra=false },
            new OpsReactionLine { id="annual_b", caption="運用ランクB！　しっかり守れた一年だったね！", scene="年間評価B", reaction=OpsReaction.Think, faceId="face_sparkle", poseId="pose_peace", fullSpeech=true, extra=false },
            new OpsReactionLine { id="annual_c", caption="運用ランクC。……大変な一年だったけど、次はもっとうまくやれるよ！", scene="年間評価C", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_please", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_04", caption="新年度だね！　桜もきれいだし、がんばろ〜っ！", scene="4月", reaction=OpsReaction.Think, faceId="face_sparkle", poseId="pose_wave", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_05", caption="新緑の季節！　新人さんも、そろそろ慣れてきたかな？", scene="5月", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_wave", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_06", caption="梅雨入りだね。今月は山場だから、気を引きしめてこ！", scene="6月", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_salute", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_07", caption="夏が来た〜！　暑いけど、ひと休みも大事だよ！", scene="7月", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_coffee", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_08", caption="お盆休みの季節。人が少ない時期は、要注意だよ！", scene="8月", reaction=OpsReaction.Think, faceId="face_worried", poseId="pose_think", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_09", caption="上期の締めだよ！　ここ、乗り切ろ！", scene="9月", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_fists", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_10", caption="秋だね〜。後半戦、スタート！", scene="10月", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_salute", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_11", caption="朝晩、冷えてきたね。年末に向けて準備しとこ！", scene="11月", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_laptop", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_12", caption="年末だ〜！　大掃除の前に、会社の守りも見直そ！", scene="12月", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_fists", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_01", caption="あけまして、おめでと〜！　今年もよろしくね！", scene="1月", reaction=OpsReaction.Think, faceId="face_sparkle", poseId="pose_wave", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_02", caption="年度末まで、あと少し！　予算の使い方、慎重にね。", scene="2月", reaction=OpsReaction.Think, faceId="face_normal", poseId="pose_think", fullSpeech=true, extra=false },
            new OpsReactionLine { id="season_03", caption="一年の集大成だよ！　最後まで、一緒に守りきろ！", scene="3月", reaction=OpsReaction.Think, faceId="face_determined", poseId="pose_shield", fullSpeech=true, extra=false },
            new OpsReactionLine { id="peak_goal_06", caption="今月は最初の山場だよ！　目標、ちゃんと見ておいてね！", scene="山場の月の始まり（6月）", faceId="face_determined", poseId="pose_point", fullSpeech=true },
            new OpsReactionLine { id="peak_goal_09", caption="上期のしめくくり、二つ目の山場！　ここは落とせないね！", scene="山場の月の始まり（9月）", faceId="face_determined", poseId="pose_fists", fullSpeech=true },
            new OpsReactionLine { id="peak_goal_12", caption="年末の山場！　みんな忙しい時期だから、気を引きしめていこ！", scene="山場の月の始まり（12月）", faceId="face_alert", poseId="pose_salute", fullSpeech=true },
            new OpsReactionLine { id="peak_goal_03", caption="いよいよ決算の山場！　一年の集大成、見せちゃお！", scene="山場の月の始まり（3月・決算）", faceId="face_determined", poseId="pose_shield", fullSpeech=true },
            new OpsReactionLine { id="peak_clear_01", caption="山場、突破〜っ！　みんなで守りきったね！", scene="山場の目標を達成", faceId="face_sparkle", poseId="pose_jump", fullSpeech=true },
            new OpsReactionLine { id="peak_clear_02", caption="よしっ、目標クリア！　備えてきたかいがあったね！", scene="山場の目標を達成", faceId="face_proud", poseId="pose_peace", fullSpeech=true },
            new OpsReactionLine { id="peak_clear_final", caption="決算の山場、突破！　この一年、ほんとにおつかれさま！", scene="決算の山場を達成", faceId="face_crying", poseId="pose_jump", fullSpeech=true },
            new OpsReactionLine { id="peak_miss_01", caption="う〜ん、今回は届かなかったか……。でも、次の山場で取り返そ！", scene="山場の目標に届かず", faceId="face_worried", poseId="pose_please", fullSpeech=true },
            new OpsReactionLine { id="peak_miss_02", caption="惜しかった〜！　何があれば届いたか、月報で一緒に見てみよ？", scene="山場の目標に届かず", faceId="face_sad", poseId="pose_think", fullSpeech=true },
            new OpsReactionLine { id="next_rank", caption="次のランクまで、あとちょっと！", scene="次のランクが近い", faceId="face_determined", poseId="pose_fists", fullSpeech=true },
            new OpsReactionLine { id="annual_ss", caption="運用ランクSS！　……うそ、ほんとに？　最高の一年だよ！", scene="年間評価SS", faceId="face_crying", poseId="pose_jump", fullSpeech=true },
            new OpsReactionLine { id="annual_s", caption="運用ランクS！　胸を張っていい一年だったね！", scene="年間評価S", faceId="face_proud", poseId="pose_jump", fullSpeech=true },
            new OpsReactionLine { id="mg_start_01", caption="よーし、いくよっ！", scene="ミニゲームの始まり", faceId="face_determined", poseId="pose_fists" },
            new OpsReactionLine { id="mg_start_02", caption="さあ、腕の見せどころ！", scene="ミニゲームの始まり", faceId="face_doya", poseId="pose_point" },
            new OpsReactionLine { id="mg_combo_01", caption="その調子！", scene="ミニゲームで連続成功", faceId="face_sparkle", poseId="pose_fists" },
            new OpsReactionLine { id="mg_combo_02", caption="すごいすごい、止まらないね！", scene="ミニゲームで連続成功", faceId="face_sparkle", poseId="pose_jump" },
            new OpsReactionLine { id="mg_miss_01", caption="あっ、今のは惜しい！", scene="ミニゲームで失敗", faceId="face_worried", poseId="pose_startled" },
            new OpsReactionLine { id="mg_miss_02", caption="大丈夫、次いこ！", scene="ミニゲームで失敗", faceId="face_normal", poseId="pose_please" },
            new OpsReactionLine { id="mg_end_good", caption="完璧〜っ！　プロの仕事だね！", scene="ミニゲームの高評価", faceId="face_sparkle", poseId="pose_jump" },
            new OpsReactionLine { id="mg_end_ok", caption="うん、しっかり守れたよ！", scene="ミニゲームの普通の評価", faceId="face_proud", poseId="pose_peace" },
            new OpsReactionLine { id="mg_end_bad", caption="う〜ん、次はもっとうまくやろ！", scene="ミニゲームの低評価", faceId="face_sad", poseId="pose_think" },
            new OpsReactionLine { id="mg_delegate", caption="じゃあ、ここはみんなに任せちゃお！", scene="社員に任せた", faceId="face_tease", poseId="pose_wave" },
            new OpsReactionLine { id="mg_contain_cut", caption="ナイス遮断！", scene="封じ込めで感染を切り離す", faceId="face_doya", poseId="pose_shield" },
            new OpsReactionLine { id="mg_contain_false", caption="それ、正常な端末だよ〜！", scene="封じ込めで正常な端末を止めた", faceId="face_akire", poseId="pose_startled" },
            new OpsReactionLine { id="mg_mail_catch", caption="見破った！　それ、怪しいメール！", scene="メールの仕分けで見破る", faceId="face_doya", poseId="pose_magnifier" },
            new OpsReactionLine { id="mg_mail_miss", caption="あっ、それ開いちゃだめなやつ……！", scene="メールの仕分けで見逃す", faceId="face_panic", poseId="pose_startled" },
            new OpsReactionLine { id="mg_mfa_block", caption="弾いた！　本人じゃないね！", scene="多要素認証で偽の依頼を弾く", faceId="face_doya", poseId="pose_armscross" },
            new OpsReactionLine { id="mg_mfa_breach", caption="その人、今ログインしてないよ〜！", scene="多要素認証で許可してしまう", faceId="face_shocked", poseId="pose_startled" },
            new OpsReactionLine { id="maxim_backup", caption="バックアップは、戻せてこそバックアップ、だよ！", scene="戻せることを確かめる", faceId="face_doya", poseId="pose_point" },
            new OpsReactionLine { id="maxim_hurry_v2", caption="「急いで」「内密に」は、詐欺によくある合図！　いつもの連絡先で確かめよう！", scene="確かめてから信じる", faceId="face_alert", poseId="pose_point" },
            new OpsReactionLine { id="maxim_sender", caption="名前より中身！　差出人は、確かめるものだよ！", scene="確かめてから信じる", faceId="face_determined", poseId="pose_magnifier" },
            new OpsReactionLine { id="maxim_link_v2", caption="リンクは押す前に、行き先を確かめよう！　似た名前にも気をつけて！", scene="確かめてから信じる", faceId="face_normal", poseId="pose_point" },
            new OpsReactionLine { id="maxim_account", caption="口座の変更は、いつもの連絡先で確かめる。これ鉄則！", scene="確かめてから信じる", faceId="face_determined", poseId="pose_armscross" },
            new OpsReactionLine { id="maxim_mfa", caption="心当たりのない承認は、ぜったい拒否だよ！", scene="確かめてから信じる", faceId="face_alert", poseId="pose_shield" },
            new OpsReactionLine { id="maxim_least", caption="権限は、必要な人に、必要な分だけ！", scene="最小権限", faceId="face_tease", poseId="pose_point" },
            new OpsReactionLine { id="maxim_layers", caption="壁は一枚より、何枚も重ねるのが強いんだよ！", scene="多層防御", faceId="face_proud", poseId="pose_shield" },
            new OpsReactionLine { id="maxim_segment_v2", caption="区切っておけば、広がりを抑えられる！", scene="分離", faceId="face_doya", poseId="pose_armscross" },
            new OpsReactionLine { id="maxim_uptime", caption="守るのと同じくらい、仕事を止めないのも大事！", scene="止めすぎない", faceId="face_normal", poseId="pose_please" },
            new OpsReactionLine { id="maxim_baseline", caption="「いつもと違う」が、いちばんのサインだよ！", scene="普段を知る", faceId="face_determined", poseId="pose_magnifier" },
            new OpsReactionLine { id="maxim_logs", caption="ログは、残しておいてこそ役に立つんだ！", scene="普段を知る", faceId="face_normal", poseId="pose_laptop" },
            new OpsReactionLine { id="maxim_priority", caption="更新は、危ないものから順番に！", scene="優先順位", faceId="face_determined", poseId="pose_fists" },
            new OpsReactionLine { id="maxim_restore", caption="止まったときは、支えてる仕組みから戻すの！", scene="依存関係と業務影響", faceId="face_normal", poseId="pose_typing" },
            new OpsReactionLine { id="maxim_runbook", caption="手順書は、未来の自分への手紙だよ！", scene="手順を残す", faceId="face_sparkle", poseId="pose_peace" },
            new OpsReactionLine { id="maxim_human", caption="人は間違えるもの。だから、仕組みで守ろ！", scene="性弱説", faceId="face_relieved", poseId="pose_please" },
            new OpsReactionLine { id="maxim_report", caption="迷ったら相談！　早い報告ほど、被害は小さいよ！", scene="相談文化", faceId="face_sparkle", poseId="pose_wave" },
            new OpsReactionLine { id="maxim_password", caption="パスワードの使い回しは、ぜったいダメだよ〜！", scene="基本", faceId="face_pout", poseId="pose_armscross" },
            new OpsReactionLine { id="maxim_usb", caption="知らないUSBは、挿さない！", scene="基本", faceId="face_akire", poseId="pose_point" },
            new OpsReactionLine { id="maxim_estimate", caption="見積もりは目安。確率じゃなくて、幅で考えるんだよ！", scene="見積もりの読み方", faceId="face_normal", poseId="pose_think" },
        }.Concat(OpsDiaryCatalog.Entries.Where(e=>!string.IsNullOrEmpty(e.voiceId)).Select(e=>new OpsReactionLine{id=e.voiceId,caption=e.intro,scene="日記 "+e.year+"年目"+OpsCatalog.Months[e.month].name,faceId=e.face,poseId=e.pose,fullSpeech=true})).ToArray();
    }

    // ゲームの乱数と分離。音声の有無や再抽選で出来事・結果を変えない。
    public sealed class OpsReactionDirector
    {
        private readonly System.Random random;
        private readonly Dictionary<OpsReaction, double> recent = new Dictionary<OpsReaction, double>();
        private double lastTime = double.NegativeInfinity;
        private string lastId = "", lastCaption = "";
        private AudioClip lastClip;
        private int lastPriority;
        public OpsReactionDirector(int seed) { random = new System.Random(seed); }
        public static int Priority(OpsReaction cue) => cue == OpsReaction.Clear || cue == OpsReaction.Failure ? 4 :
            cue == OpsReaction.Alert || cue == OpsReaction.Recover ? 3 : cue == OpsReaction.Success || cue == OpsReaction.Growth ? 2 : cue == OpsReaction.Think ? 0 : 1;
        public bool TryChoose(OpsReactionBank bank, OpsReaction cue, double time, out OpsReactionLine line,bool preview=false)
        {
            line = null;
            if (bank == null || bank.lines == null || double.IsNaN(time) || double.IsInfinity(time)) return false;
            if (!preview&&(time - lastTime < .8 || time - lastTime < 4 && Priority(cue) <= lastPriority)) return false;
            if (!preview&&recent.TryGetValue(cue, out double previous) && time - previous < 8) return false;
            var pool = bank.lines.Where(l => OpsReactionBank.IsGeneralReaction(l) && l.reaction == cue && !string.IsNullOrWhiteSpace(l.caption)).ToArray();
            if (pool.Length == 0) return false;
            // 音源が一部だけ届いた段階でも、字幕と再生する台詞を一致させる。
            var alternatives = pool.Where(l => l.id != lastId && l.caption != lastCaption && (l.clip == null || l.clip != lastClip)).ToArray();
            if (alternatives.Length == 0) return false;
            pool=alternatives;
            var recorded=pool.Where(l=>l.clip!=null).ToArray();if(recorded.Length>0)pool=recorded;
            line = pool[random.Next(pool.Length)]; lastId = line.id; lastCaption = line.caption; lastClip = line.clip;
            lastTime = time; lastPriority = Priority(cue); recent[cue] = time; return true;
        }
    }
}
