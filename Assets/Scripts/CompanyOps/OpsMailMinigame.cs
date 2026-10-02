using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public sealed class OpsMailQuestion
    {
        public readonly string Id, Sender, Address, Subject, Body, Link, LinkLabel, Attachment, Clue, Maxim;
        public readonly bool Suspicious, Urgent, MarkAddress;
        public OpsMailQuestion(string id,bool bad,string sender,string address,string subject,string body,string clue="",string link="",string label="",string attachment="",string maxim="maxim_sender",bool urgent=false,bool markAddress=false)
        {Id=id;Suspicious=bad;Sender=sender;Address=address;Subject=subject;Body=body;Clue=clue;Link=link;LinkLabel=label;Attachment=attachment;Maxim=maxim;Urgent=urgent;MarkAddress=markAddress;}
    }
    public enum OpsMailAnswer { None, Correct, Miss, FalseAlarm }

    // 出題の乱数は年度・月の種から分離。Previewや年の乱数を呼び直さない。
    public sealed class OpsMailMinigame:OpsMinigame
    {
        private readonly Random random;
        private readonly bool benign;
        private readonly List<OpsMailQuestion> questions=new List<OpsMailQuestion>();
        private readonly List<string> learned=new List<string>();
        private int index;
        internal readonly int YearSeed,Month;
        public bool Education {get;}
        public bool Practice {get;}
        public int Correct {get;private set;}
        public int Misses {get;private set;}
        public int FalseAlarms {get;private set;}
        public int Streak {get;private set;}
        public float DelayRemaining {get;private set;}
        public float HitStopRemaining {get;private set;}
        public string Feedback {get;private set;}="";
        public OpsMailQuestion Current=>index<questions.Count?questions[index]:null;
        public int Number=>Math.Min(index+1,OpsCatalog.MailCount);
        public bool CanAnswer=>Phase==OpsMinigamePhase.Playing&&DelayRemaining<=0&&Current!=null;
        public IReadOnlyList<string> Learned=>learned.AsReadOnly();
        public string ResultMaxim {get;private set;}="maxim_sender";
        public override string Grade=>Score>=OpsCatalog.MailS?"S":Score>=OpsCatalog.MailA?"A":Score>=OpsCatalog.MailB?"B":"C";
        internal OpsMailMinigame(OpsState state,bool isBenign,bool practice=false):base("メールの仕分け",state,OpsCatalog.MailSeconds)
        {YearSeed=state.seed;Month=state.month;benign=isBenign;Practice=practice;Education=state.Level("education")>0;random=new Random(unchecked(state.seed^((state.month+1)*7919)^0x7a11));}
        public override bool Start()
        {
            if(!base.Start())return false;
            var bad=Shuffle(Catalog.Where(q=>q.Suspicious).ToList());var good=Shuffle(Catalog.Where(q=>!q.Suspicious).ToList());
            int count=benign?1:5+random.Next(2);questions.AddRange(bad.Take(count));
            var urgent=good.First(q=>q.Urgent);questions.Add(urgent);good.Remove(urgent);
            int safe=OpsCatalog.MailCount-count-1;
            for(int i=0;i<safe;i++)questions.Add(good[i%good.Count]);
            Shuffle(questions);return true;
        }
        private List<T> Shuffle<T>(List<T> list)
        {for(int i=list.Count-1;i>0;i--){int j=random.Next(i+1);T temp=list[i];list[i]=list[j];list[j]=temp;}return list;}
        public OpsMailAnswer Answer(bool report)
        {
            if(!CanAnswer)return OpsMailAnswer.None;
            var mail=Current;OpsMailAnswer result;
            if(report==mail.Suspicious){Correct++;Streak++;result=OpsMailAnswer.Correct;Feedback="";DelayRemaining=OpsCatalog.MailRightDelay;}
            else
            {
                Streak=0;DelayRemaining=OpsCatalog.MailFeedbackSeconds;
                if(mail.Suspicious){Misses++;result=OpsMailAnswer.Miss;Feedback="見逃し（確かめてから信じる）："+mail.Clue;}
                else{FalseAlarms++;result=OpsMailAnswer.FalseAlarm;Feedback="止めすぎ（可用性）：本物のメール。急ぎでも、差出人と行き先を確かめて仕事を進めよう。";}
            }
            if(mail.Suspicious){if(!learned.Contains(mail.Clue))learned.Add(mail.Clue);ResultMaxim=mail.Maxim;}
            if(result==OpsMailAnswer.Correct&&report)HitStopRemaining=OpsCatalog.ContainmentHitStop;
            return result;
        }
        public override void Tick(float delta)
        {
            if(Phase!=OpsMinigamePhase.Playing||float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return;
            if(HitStopRemaining>0){float pause=Math.Min(delta,HitStopRemaining);HitStopRemaining-=pause;delta-=pause;}
            if(delta<=0)return;
            bool waiting=DelayRemaining>0;DelayRemaining=Math.Max(0,DelayRemaining-delta);base.Tick(delta);
            if(Phase!=OpsMinigamePhase.Playing)return;
            if(waiting&&DelayRemaining<=0){index++;Feedback="";if(index>=questions.Count)OnTimeUp();}
        }
        protected override void OnTimeUp()=>Complete(Correct*OpsCatalog.MailGoodPoints-Misses*OpsCatalog.MailMissPenalty-FalseAlarms*OpsCatalog.MailFalsePenalty+(int)Math.Round(Remaining,MidpointRounding.AwayFromZero));
        public static readonly OpsMailQuestion[] Catalog={
            new OpsMailQuestion("expiry",true,"総務部 システム管理","admin@nw-shoji-support.com","【至急】パスワードの有効期限が本日切れます","本日中に下記から再設定しないと、<mark=#ffe06666>アカウントが停止</mark>されます。","差出人が社外のドメイン（nw-shoji-support.com）。リンク先も社外の別サイト","http://nw-shoji.login-check.xyz/reset","パスワードを再設定する",maxim:"maxim_link",markAddress:true),
            new OpsMailQuestion("evacuation",false,"総務 小川","ogawa@nw-shoji.co.jp","来週の避難訓練のお知らせ","来週水曜の10時から避難訓練です。詳細は社内ポータルに掲載しました。",link:"https://portal.nw-shoji.co.jp/notice/1024",label:"社内ポータルを見る"),
            new OpsMailQuestion("ceo",true,"社長 加藤","kato.ceo@nw-shojii.co.jp","内密にお願いしたい振込の件","商談中のため電話に出られません。<mark=#ffe06666>至急、本日中</mark>に下記口座へ320万円を振り込んでください。<mark=#ffe06666>他の人には内密に</mark>。","ドメインが nw-shojii（iが1つ多い）。急がせる・内密に、は典型的な手口",maxim:"maxim_hurry",markAddress:true),
            new OpsMailQuestion("proof",false,"青葉印刷 営業 田中","tanaka@aoba-print.co.jp","パンフレット校正のご確認","先日の校正データをお送りします。修正箇所に赤字を入れています。",attachment:"パンフ_校正2.pdf"),
            new OpsMailQuestion("account",true,"青葉印刷 経理","keiri@aoba-print.co.jp","振込先口座変更のお知らせ","弊社の<mark=#ffe06666>振込先口座が変更</mark>になりました。今月のお支払いから下記の新口座へお願いします。","差出人は本物でも、口座変更はメールだけで信じない。いつもの連絡先へ電話で確認する",maxim:"maxim_account"),
            new OpsMailQuestion("expense",false,"経理 森","mori@nw-shoji.co.jp","【本日締切】経費精算をお願いします","今月分の経費精算は本日25日締めです。申請はいつもの経費システムからお願いします。",urgent:true),
            new OpsMailQuestion("delivery",true,"宅配便のお知らせ","info@delivery-notice.top","お荷物をお届けできませんでした","ご不在のため持ち帰りました。","宅配の通知は注文・配送の記録と照合。リンク先がアプリ（.apk）なので開かない","http://delivery-notice.top/redeliver.apk","再配達の手続き",maxim:"maxim_link",markAddress:true),
            new OpsMailQuestion("meeting",false,"制作 大野","ono@nw-shoji.co.jp","Re: 明日の打ち合わせ","資料ありがとうございます。明日14時で大丈夫です。会議室Bを取っておきます。"),
            new OpsMailQuestion("signin",true,"Microsoft 365","no-reply@micros0ft-365.com","サインインの異常を検知しました","不審なサインインがありました。<mark=#ffe06666>24時間以内</mark>に確認しない場合、アカウントを制限します。","micros0ft（oが数字の0）。本物の通知もリンクから入らず、いつもの入口から確認する","http://micros0ft-365.com/verify","アカウントを確認",markAddress:true),
            new OpsMailQuestion("morning",false,"社長 加藤","kato@nw-shoji.co.jp","月曜の朝会で話したいこと","月曜の朝会で、情シスのみなさんの取り組みを紹介したいと思います。5分ほどお願いできますか。"),
            new OpsMailQuestion("macro",true,"取引先 西村様","nishimura@partner-trade.co.jp","見積書の送付","見積書を添付しました。<mark=#ffe06666>マクロを有効にして</mark>ご確認ください。","マクロを有効にさせる添付は危険。取引先でも、いつもと違う形式なら確認する",attachment:"見積書.xlsm"),
            new OpsMailQuestion("printer",false,"情シス ひなた","hinata@nw-shoji.co.jp","プリンタ設定の変更について","3階のプリンタを新しい機種に入れ替えました。印刷先の選び方は社内ポータルにまとめています。",link:"https://portal.nw-shoji.co.jp/it/printer",label:"手順を見る"),
            new OpsMailQuestion("salary",true,"人事部","jinji@nw-shoji.co.jp.hr-update.net","【要回答】給与振込口座の再登録","システム更新のため、<mark=#ffe06666>全社員</mark>の給与口座を再登録してください。","アドレスの最後が hr-update.net。最後の部分が本当のドメイン（住所）。表示の名前や途中の文字は偽れる","http://nw-shoji.co.jp.hr-update.net/form","再登録フォーム",markAddress:true),
            new OpsMailQuestion("backup",false,"監視システム","alert@monitor.nw-shoji.co.jp","[定期] バックアップ完了（成功）","昨夜のバックアップが正常に完了しました。所要時間42分。")
        };
    }
    public partial class OpsState
    {
        public bool SupportsMail=>CurrentProfile!=null&&(CurrentProfile.id=="bec"||CurrentProfile.id=="targeted");
        public OpsMailMinigame CreateMail()=>phase==OpsPhase.Incident&&SupportsMail?new OpsMailMinigame(this,Benign):null;
        public string MailTrainingBlock=>PracticeBlock(0);
        public OpsMailMinigame CreateMailPractice()=>MailTrainingBlock==""?new OpsMailMinigame(this,false,true):null;
        public bool CompleteMailTraining(OpsMailMinigame session)
        {
            if(session==null||session.YearSeed!=seed||session.Month!=month||!session.Practice||session.Phase!=OpsMinigamePhase.Result||session.Delegated||MailTrainingBlock!="")return false;
            capacity--;practiced=true;int gained=GainStaff(0,OpsCatalog.MailPracticeXp);
            Note("メールの仕分け研修 / 小川の経験 +"+gained+"。見つけた手がかりを共有した。");return true;
        }
    }
}
