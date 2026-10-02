using System;

namespace PatchWorkSecure.CompanyOps
{
    // 日付以外の進行・設備・端末固有の値を使わない練習。年度の状態とは完全に別の実体。
    public static class OpsDailyPractice
    {
        public static readonly string[] Ids={"B","C","D","E","F2","G"};
        public static readonly string[] Names={"感染の封じ込め","メールの仕分け","多要素認証の関所","ログを調べる","作業のはめ込み","復旧の順番"};
        public static int Day(DateTime date)=>date.Year*10000+date.Month*100+date.Day;
        public static bool ValidDay(int day){try{var date=new DateTime(day/10000,day/100%100,day%100);return Day(date)==day;}catch(ArgumentOutOfRangeException){return false;}}
        public static OpsMinigame Create(string id,int day)
        {
            if(!ValidDay(day))return null;
            var state=new OpsState(day,true);
            foreach(string equipment in new[]{"monitor","education","runbook","automation"})state.levels[OpsCatalog.Index(equipment)]=1;
            state.levels[OpsCatalog.Index("mfa")]=2;
            switch(id)
            {
                case "B":return new OpsContainmentMinigame(state,day%5==0);
                case "C":return new OpsMailMinigame(state,false);
                case "D":return new OpsMfaMinigame(state,false);
                case "E":return new OpsLogMinigame(state);
                case "F2":return new OpsBlockMinigame(state);
                case "G":return new OpsRestoreMinigame(state,new[]{"power","storage","change","ransom"}[day%4]);
                default:return null;
            }
        }
        public static string Id(OpsMinigame game)=>game is OpsContainmentMinigame?"B":game is OpsMailMinigame?"C":game is OpsMfaMinigame?"D":game is OpsLogMinigame?"E":game is OpsBlockMinigame?"F2":game is OpsRestoreMinigame?"G":"";
        public static string Grade(string id,int score)
        {
            int s=OpsCatalog.MinigameS,a=OpsCatalog.MinigameA,b=OpsCatalog.MinigameB;
            switch(id)
            {
                case "C":s=OpsCatalog.MailS;a=OpsCatalog.MailA;b=OpsCatalog.MailB;break;
                case "D":s=OpsCatalog.MfaS;a=OpsCatalog.MfaA;b=OpsCatalog.MfaB;break;
                case "E":s=OpsCatalog.LogS;a=OpsCatalog.LogA;b=OpsCatalog.LogB;break;
                case "F2":s=OpsCatalog.BlockS;a=OpsCatalog.BlockA;b=OpsCatalog.BlockB;break;
                case "G":s=OpsCatalog.RestoreS;a=OpsCatalog.RestoreA;b=OpsCatalog.RestoreB;break;
            }
            return score>=s?"S":score>=a?"A":score>=b?"B":"C";
        }
    }
    public enum OpsMinigamePhase { Brief, Playing, Result }

    // 年間の状態を直接変更しない、各ミニゲーム共通の進行と設備スナップショット。
    public class OpsMinigame
    {
        public string Title { get; private set; }
        public OpsMinigamePhase Phase { get; protected set; }
        public int Score { get; private set; } = OpsCatalog.MinigameDelegateScore;
        public bool Delegated { get; private set; }
        public float Elapsed { get; protected set; }
        public float Duration { get; private set; }
        public float Remaining => Math.Max(0, Duration - Elapsed);
        public bool Monitor { get; private set; }
        public bool Segment { get; private set; }
        public bool Backup { get; private set; }
        public virtual string Grade => Score >= OpsCatalog.MinigameS ? "S" : Score >= OpsCatalog.MinigameA ? "A" : Score >= OpsCatalog.MinigameB ? "B" : "C";
        public string EndVoice => Score >= OpsCatalog.MinigameGood ? "mg_end_good" : Score >= OpsCatalog.MinigameDelegateScore ? "mg_end_ok" : "mg_end_bad";
        public OpsMinigame(string title, OpsState state, float duration=OpsCatalog.MinigameSeconds)
        {
            Title=title;Duration=duration;Monitor=state.Level("monitor")>0;Segment=state.Level("segment")>0;Backup=state.Level("backup")>0;
        }
        public virtual bool Start()
        {
            if(Phase!=OpsMinigamePhase.Brief)return false;
            Phase=OpsMinigamePhase.Playing;return true;
        }
        public bool Delegate()
        {
            if(Phase!=OpsMinigamePhase.Brief)return false;
            Delegated=true;Complete(OpsCatalog.MinigameDelegateScore);return true;
        }
        public virtual void Tick(float delta)
        {
            if(Phase!=OpsMinigamePhase.Playing||float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return;
            Elapsed=Math.Min(Duration,Elapsed+delta);
            if(Remaining<=0)OnTimeUp();
        }
        protected virtual void OnTimeUp()=>Complete(OpsCatalog.MinigameDelegateScore);
        protected void Complete(int score)
        {
            Score=Math.Max(0,Math.Min(OpsCatalog.MinigameMaxScore,score));Phase=OpsMinigamePhase.Result;
        }
    }
}
