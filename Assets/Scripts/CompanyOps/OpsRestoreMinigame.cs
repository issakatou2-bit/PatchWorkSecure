using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public enum OpsRestoreBoot { None, Started, Failed }
    public sealed class OpsRestoreNode
    {
        public string Id,Name,Sub;public string[] Need;
        public int Work,Cost,Worker=-1;public double Left,Finished=-1,Rto=-1;
        public bool Up,Encrypted;public bool Busy=>Worker>=0;
    }
    public sealed class OpsRestoreSimulation
    {
        public double Loss,Hour;public Dictionary<string,double> Finished=new Dictionary<string,double>();
    }
    public sealed class OpsRestoreMinigame:OpsMinigame
    {
        public string Scenario {get;}
        public bool Runbook {get;}
        public bool Benign {get;}
        public bool MonthEnd {get;}
        public double LateJoin {get;}
        public double Hour {get;private set;}
        public double Loss {get;private set;}
        public double BestLoss {get;private set;}
        public int Failures {get;private set;}
        public int Reinfections {get;private set;}
        public int Starts {get;private set;}
        public int Revision {get;private set;}
        public float HitStopRemaining {get;private set;}
        public IReadOnlyList<OpsRestoreNode> Nodes=>nodes;
        public IReadOnlyList<string> Logs=>logs;
        public IReadOnlyList<string> BestOrder=>bestOrder;
        public IReadOnlyCollection<string> Revealed=>revealed;
        public int Overdue=>nodes.Count(n=>n.Rto>=0&&(n.Finished>=0?n.Finished:Hour)>n.Rto);
        public string ScenarioTitle=>Scenario=="change"?"更新トラブル":Scenario=="storage"?"保存領域の故障":Scenario=="ransom"?"ランサムウェア":"停電";
        public override string Grade=>Score>=OpsCatalog.RestoreS?"S":Score>=OpsCatalog.RestoreA?"A":Score>=OpsCatalog.RestoreB?"B":"C";
        private readonly List<OpsRestoreNode> nodes=new List<OpsRestoreNode>();private readonly List<string> logs=new List<string>();
        private readonly HashSet<string> initialUp=new HashSet<string>(),revealed=new HashSet<string>();
        private string[] bestOrder;private float tickRemainder;
        private static readonly string[] Ids={"check","net","dns","auth","fs","db","mail","order","acct"};
        private static readonly string[] Names={"安全確認","社内ネットワーク","名前解決（DNS）","認証サーバー","ファイル共有","データベース","メール","受注システム","会計システム"};
        private static readonly string[] Subs={"復元元と侵入経路の点検","スイッチ・ルーター","住所録のような仕組み","ログインの受付","共有フォルダ","受注・在庫の記録","社外との連絡","注文の受付","請求・支払い"};
        private static readonly string[][] Needs={new string[0],new string[0],new[]{"net"},new[]{"net"},new[]{"dns","auth"},new[]{"net"},new[]{"dns","auth"},new[]{"db","auth","dns"},new[]{"db","auth"}};
        public OpsRestoreMinigame(OpsState state,string scenario,bool benign=false):base("復旧の順番",state,float.MaxValue)
        {
            if(!new[]{"power","storage","change","ransom"}.Contains(scenario))throw new ArgumentException("不明な事故の種類");
            Scenario=scenario;Benign=benign;Runbook=state.Level("runbook")>0;
            var rng=new Random(unchecked(state.seed^((state.month+1)*OpsCatalog.WorkSeedStride)^OpsCatalog.RestoreSeedSalt));
            MonthEnd=rng.NextDouble()<OpsCatalog.RestoreMonthEndChance;LateJoin=rng.NextDouble()<OpsCatalog.RestoreLateChance?OpsCatalog.RestoreLateHours:0;
            string[] down=scenario=="storage"?new[]{"fs","db","order","acct"}:scenario=="change"?new[]{"auth","fs","mail","order","acct"}:scenario=="ransom"?new[]{"check","fs","db","mail","order","acct"}:Ids.Skip(1).ToArray();
            if(benign)down=down.OrderBy(_=>rng.Next()).Take(OpsCatalog.RestoreBenignMinimum+rng.Next(OpsCatalog.RestoreBenignVariation)).ToArray();
            for(int i=0;i<Ids.Length;i++)
            {
                if(i==0&&scenario!="ransom")continue;
                int work=rng.Next(OpsCatalog.RestoreWorkMin[i],OpsCatalog.RestoreWorkMax[i]+1)+(scenario=="storage"&&Ids[i]=="db"?OpsCatalog.RestoreStorageExtra:0);
                int cost=benign?0:rng.Next(OpsCatalog.RestoreCostMin[i],OpsCatalog.RestoreCostMax[i]+1)*(MonthEnd&&Ids[i]=="acct"?OpsCatalog.RestoreMonthEndMultiplier:1);
                var node=new OpsRestoreNode{Id=Ids[i],Name=Names[i],Sub=Subs[i],Need=Needs[i],Work=work,Cost=cost,Up=!down.Contains(Ids[i]),Encrypted=scenario=="ransom"&&(Ids[i]=="fs"||Ids[i]=="db")};
                nodes.Add(node);if(node.Up)initialUp.Add(node.Id);
            }
            var best=Search();BestLoss=best.Loss;
            foreach(var node in nodes.Where(n=>!n.Up&&n.Cost>0))node.Rto=Math.Ceiling((best.Finished[node.Id]+(rng.NextDouble()<OpsCatalog.RestoreRtoShortChance?OpsCatalog.RestoreRtoSlackMin:OpsCatalog.RestoreRtoSlackMax))/OpsCatalog.RestoreStepHours)*OpsCatalog.RestoreStepHours;
            if(Runbook)foreach(var node in nodes)foreach(string parent in Dependencies(node))revealed.Add(parent+"-"+node.Id);
        }
        public IEnumerable<string> Dependencies(OpsRestoreNode node)=>node.Encrypted?node.Need.Concat(new[]{"check"}):node.Need;
        public OpsRestoreSimulation Simulate(IEnumerable<string> ordering)
        {
            var order=ordering.ToArray();var up=new HashSet<string>(initialUp);var busy=new Dictionary<string,Tuple<double,int>>();var result=new OpsRestoreSimulation();
            for(int tick=0;up.Count<nodes.Count&&tick<OpsCatalog.RestoreSimulationLimit;tick++)
            {
                for(int w=0;w<OpsCatalog.RestoreWorkers;w++)
                {
                    if(w==1&&result.Hour<LateJoin||busy.Values.Any(v=>v.Item2==w))continue;
                    var node=order.Select(id=>nodes.First(n=>n.Id==id)).FirstOrDefault(n=>!up.Contains(n.Id)&&!busy.ContainsKey(n.Id)&&Dependencies(n).All(d=>up.Contains(d)));
                    if(node!=null)busy.Add(node.Id,Tuple.Create((double)node.Work,w));
                }
                result.Hour+=OpsCatalog.RestoreStepHours;result.Loss+=nodes.Where(n=>!up.Contains(n.Id)).Sum(n=>n.Cost)*OpsCatalog.RestoreStepHours;
                foreach(var id in busy.Keys.ToArray())
                {
                    var job=busy[id];double left=job.Item1-OpsCatalog.RestoreStepHours;
                    if(left<=0){up.Add(id);result.Finished[id]=result.Hour;busy.Remove(id);}else busy[id]=Tuple.Create(left,job.Item2);
                }
            }
            if(up.Count!=nodes.Count)throw new InvalidOperationException("復旧できない順序です。");return result;
        }
        private OpsRestoreSimulation Search()
        {
            OpsRestoreSimulation best=null;int count=0;var order=new List<string>();var remaining=nodes.Where(n=>!n.Up).ToList();
            Action<List<OpsRestoreNode>> visit=null;visit=left=>
            {
                if(count>=OpsCatalog.RestoreSearchLimit)return;
                if(left.Count==0){count++;var trial=Simulate(order);if(best==null||trial.Loss<best.Loss){best=trial;bestOrder=order.ToArray();}return;}
                foreach(var n in left)if(Dependencies(n).All(d=>initialUp.Contains(d)||order.Contains(d)))
                {order.Add(n.Id);visit(left.Where(x=>x!=n).ToList());order.RemoveAt(order.Count-1);}
            };
            visit(remaining);if(best==null)throw new InvalidOperationException("最善の復旧順序が見つかりません。");return best;
        }
        public override bool Start()
        {
            if(!base.Start())return false;if(MonthEnd&&!Benign)Log("今日は月末。会計システムの損失が2倍");if(LateJoin>0)Log("大野さんは移動中。1時間後に合流");Revision++;return true;
        }
        private void Log(string text){logs.Insert(0,text);}
        public OpsRestoreBoot Boot(string id)
        {
            var n=nodes.FirstOrDefault(node=>node.Id==id);if(Phase!=OpsMinigamePhase.Playing||n==null||n.Up||n.Busy)return OpsRestoreBoot.None;
            int worker=Enumerable.Range(0,OpsCatalog.RestoreWorkers).Where(w=>!(w==1&&Hour<LateJoin)&&nodes.All(node=>node.Worker!=w)).DefaultIfEmpty(-1).First();
            if(worker<0)return OpsRestoreBoot.None;
            var missing=n.Need.Where(d=>!nodes.First(node=>node.Id==d).Up).ToArray();
            if(missing.Length>0)
            {
                Failures++;Hour+=OpsCatalog.RestoreStepHours;Accrue();foreach(string d in missing)revealed.Add(d+"-"+id);
                Log("失敗："+n.Name+"は「"+string.Join("・",missing.Select(d=>nodes.First(node=>node.Id==d).Name))+"」が先（30分むだに）");Revision++;return OpsRestoreBoot.Failed;
            }
            n.Worker=worker;n.Left=n.Work;Starts++;Log("開始："+n.Name+"（"+(worker==0?"ひなた":"大野さん")+"）");Revision++;return OpsRestoreBoot.Started;
        }
        private void Accrue()=>Loss+=nodes.Where(n=>!n.Up).Sum(n=>n.Cost)*OpsCatalog.RestoreStepHours;
        public override void Tick(float delta)
        {
            if(Phase!=OpsMinigamePhase.Playing||float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return;
            float paused=Math.Min(delta,HitStopRemaining);HitStopRemaining-=paused;delta-=paused;
            Elapsed+=delta;tickRemainder+=delta;
            while(tickRemainder>=OpsCatalog.RestoreTickSeconds&&Phase==OpsMinigamePhase.Playing)
            {
                tickRemainder-=OpsCatalog.RestoreTickSeconds;Hour+=OpsCatalog.RestoreStepHours;Accrue();
                foreach(var n in nodes.Where(n=>n.Busy).ToArray())
                {
                    n.Left-=OpsCatalog.RestoreStepHours;if(n.Left>0)continue;n.Worker=-1;
                    if(n.Encrypted&&!nodes.First(node=>node.Id=="check").Up){Reinfections++;Failures++;revealed.Add("check-"+n.Id);Log("再暗号化："+n.Name+"。先に復元元の安全確認が必要");}
                    else{n.Up=true;n.Finished=Hour;HitStopRemaining=OpsCatalog.ContainmentHitStop;Log("復旧："+n.Name+(n.Rto<0?"":n.Finished>n.Rto?"（目標超過）":"（目標内）"));}
                }
                Revision++;
                if(nodes.All(n=>n.Up))Complete((int)Math.Round(100-(Loss-BestLoss)/Math.Max(BestLoss,OpsCatalog.RestoreMinimumBest)*OpsCatalog.RestoreLossPenalty-Failures*OpsCatalog.RestoreFailurePenalty-Overdue*OpsCatalog.RestoreOverduePenalty-(Benign?Starts*OpsCatalog.RestoreBenignClickPenalty:0),MidpointRounding.AwayFromZero));
            }
        }
    }
    public partial class OpsState
    {
        public bool SupportsRestore=>CurrentProfile!=null?new[]{"change","storage","service"}.Contains(CurrentProfile.id)||CurrentProfile.kind=="outage":Current.kind=="outage";
        public OpsRestoreMinigame CreateRestore()=>phase==OpsPhase.Incident&&SupportsRestore?new OpsRestoreMinigame(this,CurrentProfile?.id=="change"?"change":CurrentProfile?.id=="storage"?"storage":"power",Benign):null;
        // エンドレスの4年目以降は3年目の暦を使うが、総決算の2本立てにはしない。
        public bool SupportsFinalRecovery=>storyCalendarYear==3&&(endlessYear==0||endlessYear==3)&&month==11&&CurrentEvent?.id=="y3-final";
        public OpsRestoreMinigame CreateFinalRestore()=>phase==OpsPhase.Incident&&SupportsFinalRecovery?new OpsRestoreMinigame(this,"ransom",Benign):null;
    }
}
