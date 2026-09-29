using System;
using System.Collections.Generic;

namespace PatchWorkSecure.CompanyOps
{
    public sealed class OpsMfaPerson
    {
        public string Name {get;internal set;}
        public string Activity {get;internal set;}
        public bool LoggingIn {get;internal set;}
    }
    public sealed class OpsMfaRequest
    {
        public readonly int Who,Number;
        public readonly bool Legitimate,Spam;
        public readonly string Place,App;
        internal OpsMfaRequest(int who,bool legitimate,bool spam,string place,string app,int number)
        {Who=who;Legitimate=legitimate;Spam=spam;Place=place;App=app;Number=number;}
    }
    public enum OpsMfaAnswer { None, Correct, Breach, Block }
    public sealed class OpsMfaLog
    {
        public readonly string Text;public readonly OpsMfaAnswer Answer;
        internal OpsMfaLog(string text,OpsMfaAnswer answer){Text=text;Answer=answer;}
    }
    // 手本の承認待ち→判断→次の依頼。年の結果・抽選・予算は変更しない。
    public sealed class OpsMfaMinigame:OpsMinigame
    {
        private static readonly string[] Names={"田中","森","大野","相田"};
        private static readonly string[] Places={"本社（東京）","自宅（埼玉）","本社（東京）","大阪の取引先"};
        private static readonly string[] LoginActivities={"PCを開いてログイン中","メールを開こうとしている","経費システムに入ろうとしている","会議室でノートPCを起動"};
        private static readonly string[] IdleActivities={"会議中（PCは閉じている）","昼休み","外出中","電話対応中"};
        private static readonly string[] Apps={"メール","経費システム","社内ポータル","ファイル共有"};
        private static readonly string[] ForeignPlaces={"海外（不明）","ロシア","オランダ","シンガポール"};
        private readonly Random random;
        private readonly bool benign;
        private readonly OpsMfaPerson[] people=new OpsMfaPerson[4];
        private readonly List<OpsMfaLog> logs=new List<OpsMfaLog>();
        private int spamTarget;
        public bool NumberMatch {get;}
        public int Correct {get;private set;}
        public int Breaches {get;private set;}
        public int Blocks {get;private set;}
        public int Count {get;private set;}
        public int Streak {get;private set;}
        public float DelayRemaining {get;private set;}
        public float HitStopRemaining {get;private set;}
        public string Feedback {get;private set;}="";
        public OpsMfaRequest Current {get;private set;}
        public IReadOnlyList<OpsMfaPerson> People=>Array.AsReadOnly(people);
        public IReadOnlyList<OpsMfaLog> Logs=>logs.AsReadOnly();
        public bool CanAnswer=>Phase==OpsMinigamePhase.Playing&&Current!=null&&DelayRemaining<=0;
        public override string Grade=>Score>=OpsCatalog.MfaS?"S":Score>=OpsCatalog.MfaA?"A":Score>=OpsCatalog.MfaB?"B":"C";
        internal OpsMfaMinigame(OpsState state,bool isBenign):base("多要素認証の関所",state,OpsCatalog.MfaSeconds)
        {benign=isBenign;NumberMatch=state.Level("mfa")>=2;random=new Random(unchecked(state.seed^((state.month+1)*7919)^0x6fa2));}
        public override bool Start()
        {
            if(!base.Start())return false;
            for(int i=0;i<people.Length;i++)people[i]=new OpsMfaPerson{Name=Names[i],Activity=IdleActivities[random.Next(4)]};
            spamTarget=random.Next(4);Next();return true;
        }
        private void Next()
        {
            int who=random.Next(4);bool legit=benign||random.NextDouble()<OpsCatalog.MfaLegitimateChance;
            for(int i=0;i<4;i++)if(i!=who&&random.NextDouble()<OpsCatalog.MfaIdleChangeChance){people[i].LoggingIn=false;people[i].Activity=IdleActivities[random.Next(4)];}
            if(legit){people[who].LoggingIn=true;people[who].Activity=LoginActivities[random.Next(4)];}
            else{if(random.NextDouble()<OpsCatalog.MfaSpamChance)who=spamTarget;people[who].LoggingIn=false;people[who].Activity=IdleActivities[random.Next(4)];}
            string place=legit?Places[who]:random.NextDouble()<OpsCatalog.MfaForeignChance?ForeignPlaces[random.Next(4)]:"本社（東京）";
            Current=new OpsMfaRequest(who,legit,!legit&&who==spamTarget,place,legit?Apps[random.Next(4)]:"メール",10+random.Next(89));Count++;
        }
        public OpsMfaAnswer Answer(bool allow)
        {
            if(!CanAnswer)return OpsMfaAnswer.None;
            var req=Current;var person=people[req.Who];OpsMfaAnswer result;
            if(allow==req.Legitimate)
            {
                result=OpsMfaAnswer.Correct;Correct++;Streak++;Feedback="";
                logs.Add(new OpsMfaLog((allow?"許可":"拒否")+"："+person.Name+"さん（正解）",result));
                if(!allow)HitStopRemaining=OpsCatalog.ContainmentHitStop;
            }
            else if(allow)
            {
                result=OpsMfaAnswer.Breach;Breaches++;Streak=0;Feedback=person.Name+"さんは今ログインしてない！ 侵入されちゃった…";
                logs.Add(new OpsMfaLog("侵入："+person.Name+"さんは「"+person.Activity+"」だったのに許可",result));person.Activity="（パスワードの変更が必要）";
            }
            else
            {
                result=OpsMfaAnswer.Block;Blocks++;Streak=0;Feedback=person.Name+"さん本人だったみたい。仕事が止まっちゃう";
                logs.Add(new OpsMfaLog("足止め："+person.Name+"さん本人の依頼を拒否",result));
            }
            Current=null;DelayRemaining=Math.Max(OpsCatalog.MfaMinimumDelay,OpsCatalog.MfaFirstDelay-Count*OpsCatalog.MfaDelayStep);return result;
        }
        public override void Tick(float delta)
        {
            if(Phase!=OpsMinigamePhase.Playing||float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return;
            if(HitStopRemaining>0){float pause=Math.Min(delta,HitStopRemaining);HitStopRemaining-=pause;delta-=pause;}if(delta<=0)return;
            bool waiting=DelayRemaining>0;DelayRemaining=Math.Max(0,DelayRemaining-delta);base.Tick(delta);
            if(Phase==OpsMinigamePhase.Playing&&waiting&&DelayRemaining<=0)Next();
        }
        protected override void OnTimeUp()=>Complete(Correct*OpsCatalog.MfaGoodPoints-Breaches*OpsCatalog.MfaBreachPenalty-Blocks*OpsCatalog.MfaBlockPenalty);
    }
    public partial class OpsState
    {
        public bool SupportsMfa=>Current.kind=="identity"||CurrentProfile!=null&&(CurrentProfile.id=="session"||CurrentProfile.id=="remote");
        public OpsMfaMinigame CreateMfa()=>phase==OpsPhase.Incident&&SupportsMfa?new OpsMfaMinigame(this,Benign):null;
    }
}
