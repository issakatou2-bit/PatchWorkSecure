using System;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public enum OpsTerminalCut { None, Infected, Normal }
    // 真相は内部に保持。表示側には観測できる状態と、確定後の内訳だけを渡す。
    public sealed class OpsContainmentMinigame:OpsMinigame
    {
        private sealed class PC { public bool infected,cut,suspicious;public float infectedAt; }
        private readonly PC[] pcs=new PC[OpsCatalog.ContainmentRooms*OpsCatalog.ContainmentPCsPerRoom];
        private readonly Random random;
        private readonly bool benign;
        private float accumulated,scanUntil;
        private int scanRoom=-1;
        public int SelectedRoom {get;private set;}
        public int ScansLeft {get;private set;}=OpsCatalog.ContainmentScans;
        public int Streak {get;private set;}
        public float HitStopRemaining {get;private set;}
        public int PCCount=>pcs.Length;
        public int VisibleCount=>Enumerable.Range(0,PCCount).Count(Visible);
        public int StoppedCount=>pcs.Count(p=>p.cut);
        public int NormalStopped=>Phase==OpsMinigamePhase.Result?pcs.Count(p=>p.cut&&!p.infected):0;
        public int TotalInfected=>Phase==OpsMinigamePhase.Result?pcs.Count(p=>p.infected):0;
        public int Uncontained=>Phase==OpsMinigamePhase.Result?ActiveCount:0;
        public string Finding=>Phase!=OpsMinigamePhase.Result||Delegated?"":benign?"調べた結果、感染ではありませんでした。":"感染した端末と停止の内訳を確認しました。";
        private int ActiveCount=>pcs.Count(p=>p.infected&&!p.cut);
        public event Action<int,int> Spread;
        internal OpsContainmentMinigame(OpsState state,bool isBenign):base("感染の封じ込め",state)
        {
            benign=isBenign;random=new Random(unchecked(state.seed^((state.month+1)*7919)^0x351ac));
            for(int i=0;i<PCCount;i++)pcs[i]=new PC();
        }
        public override bool Start()
        {
            if(!base.Start())return false;
            int first=random.Next(PCCount),second=(first+7+random.Next(6))%PCCount;
            if(benign){pcs[first].suspicious=pcs[second].suspicious=true;}
            else{Infect(first,0);Infect(second,-1.5f);}
            return true;
        }
        public bool IsStopped(int index)=>pcs[index].cut;
        public bool StoppedInfection(int index)=>pcs[index].cut&&pcs[index].infected;
        public bool Visible(int index)
        {
            var p=pcs[index];return !p.cut&&p.infected&&(Monitor||Scanning(index/OpsCatalog.ContainmentPCsPerRoom)||Elapsed-p.infectedAt>OpsCatalog.ContainmentRevealSeconds);
        }
        public bool Suspect(int index)
        {
            var p=pcs[index];return !p.cut&&!Visible(index)&&!Scanning(index/OpsCatalog.ContainmentPCsPerRoom)&&
                (p.infected&&Elapsed-p.infectedAt>OpsCatalog.ContainmentSuspectSeconds||p.suspicious&&Elapsed>OpsCatalog.ContainmentSuspectSeconds);
        }
        public bool Scanning(int room)=>room==scanRoom&&Elapsed<scanUntil;
        public bool SelectRoom(int room)
        {if(Phase!=OpsMinigamePhase.Playing||room<0||room>=OpsCatalog.ContainmentRooms)return false;SelectedRoom=room;return true;}
        public bool Scan()
        {
            if(Phase!=OpsMinigamePhase.Playing||ScansLeft<=0)return false;
            ScansLeft--;scanRoom=SelectedRoom;scanUntil=Elapsed+OpsCatalog.ContainmentScanSeconds;return true;
        }
        public OpsTerminalCut Cut(int index)
        {
            if(Phase!=OpsMinigamePhase.Playing||index<0||index>=PCCount||pcs[index].cut)return OpsTerminalCut.None;
            var p=pcs[index];p.cut=true;
            if(p.infected){Streak++;HitStopRemaining=OpsCatalog.ContainmentHitStop;return OpsTerminalCut.Infected;}
            Streak=0;return OpsTerminalCut.Normal;
        }
        public int StopRoom()
        {
            if(Phase!=OpsMinigamePhase.Playing)return 0;
            int stopped=0;for(int i=SelectedRoom*OpsCatalog.ContainmentPCsPerRoom;i<(SelectedRoom+1)*OpsCatalog.ContainmentPCsPerRoom;i++)if(!pcs[i].cut){pcs[i].cut=true;stopped++;}
            Streak=0;return stopped;
        }
        public override void Tick(float delta)
        {
            if(Phase!=OpsMinigamePhase.Playing||float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return;
            if(HitStopRemaining>0){float pause=Math.Min(delta,HitStopRemaining);HitStopRemaining-=pause;delta-=pause;}
            accumulated+=delta;
            while(accumulated>=OpsCatalog.ContainmentStep&&Phase==OpsMinigamePhase.Playing)
            {
                accumulated-=OpsCatalog.ContainmentStep;base.Tick(OpsCatalog.ContainmentStep);
                if(Phase!=OpsMinigamePhase.Playing)break;
                if(!benign)
                {
                    var live=Enumerable.Range(0,PCCount).Where(i=>pcs[i].infected&&!pcs[i].cut).ToArray();
                    foreach(int i in live)
                    {
                        if(random.NextDouble()<OpsCatalog.ContainmentRoomSpread)SpreadTo(i,false);
                        if(!Segment&&random.NextDouble()<OpsCatalog.ContainmentCrossSpread)SpreadTo(i,true);
                    }
                    if(ActiveCount==0||ActiveCount>=OpsCatalog.ContainmentFailureCount)OnTimeUp();
                }
            }
        }
        private void Infect(int index,float at){pcs[index].infected=true;pcs[index].infectedAt=at;}
        private void SpreadTo(int from,bool crossRoom)
        {
            int room=from/OpsCatalog.ContainmentPCsPerRoom,row=from%OpsCatalog.ContainmentPCsPerRoom;
            var targets=Enumerable.Range(0,PCCount).Where(i=>!pcs[i].infected&&!pcs[i].cut&&
                (crossRoom?Math.Abs(i/OpsCatalog.ContainmentPCsPerRoom-room)==1&&i%OpsCatalog.ContainmentPCsPerRoom==row:
                i/OpsCatalog.ContainmentPCsPerRoom==room&&Math.Abs(i%OpsCatalog.ContainmentPCsPerRoom-row)==1)).ToArray();
            if(targets.Length==0)return;int to=targets[random.Next(targets.Length)];Infect(to,Elapsed);Spread?.Invoke(from,to);
        }
        protected override void OnTimeUp()=>Complete(OpsCatalog.MinigameMaxScore-ActiveCount*OpsCatalog.ContainmentLivePenalty-
            pcs.Count(p=>p.infected)*OpsCatalog.ContainmentInfectedPenalty-pcs.Count(p=>p.cut&&!p.infected)*OpsCatalog.ContainmentNormalPenalty);
    }
    public partial class OpsState
    {
        public OpsContainmentMinigame CreateContainment()=>phase==OpsPhase.Incident&&SupportsContainment?new OpsContainmentMinigame(this,Benign):null;
    }
}
