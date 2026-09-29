using System;

namespace PatchWorkSecure.CompanyOps
{
    public enum OpsMinigamePhase { Brief, Playing, Result }

    // 年間の状態を直接変更しない、各ミニゲーム共通の進行と設備スナップショット。
    public class OpsMinigame
    {
        public string Title { get; private set; }
        public OpsMinigamePhase Phase { get; protected set; }
        public int Score { get; private set; } = OpsCatalog.MinigameDelegateScore;
        public bool Delegated { get; private set; }
        public float Elapsed { get; protected set; }
        public float Remaining => Math.Max(0, OpsCatalog.MinigameSeconds - Elapsed);
        public bool Monitor { get; private set; }
        public bool Segment { get; private set; }
        public bool Backup { get; private set; }
        public string Grade => Score >= OpsCatalog.MinigameS ? "S" : Score >= OpsCatalog.MinigameA ? "A" : Score >= OpsCatalog.MinigameB ? "B" : "C";
        public string EndVoice => Score >= OpsCatalog.MinigameGood ? "mg_end_good" : Score >= OpsCatalog.MinigameDelegateScore ? "mg_end_ok" : "mg_end_bad";
        public OpsMinigame(string title, OpsState state)
        {
            Title=title;Monitor=state.Level("monitor")>0;Segment=state.Level("segment")>0;Backup=state.Level("backup")>0;
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
            Elapsed=Math.Min(OpsCatalog.MinigameSeconds,Elapsed+delta);
            if(Remaining<=0)OnTimeUp();
        }
        protected virtual void OnTimeUp()=>Complete(OpsCatalog.MinigameDelegateScore);
        protected void Complete(int score)
        {
            Score=Math.Max(0,Math.Min(OpsCatalog.MinigameMaxScore,score));Phase=OpsMinigamePhase.Result;
        }
    }
}
