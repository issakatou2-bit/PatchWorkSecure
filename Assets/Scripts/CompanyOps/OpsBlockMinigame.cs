using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public struct OpsBlockCell : IEquatable<OpsBlockCell>
    {
        public int Row,Column;
        public OpsBlockCell(int row,int column){Row=row;Column=column;}
        public bool Equals(OpsBlockCell other)=>Row==other.Row&&Column==other.Column;
        public override bool Equals(object other)=>other is OpsBlockCell cell&&Equals(cell);
        public override int GetHashCode()=>Row*31+Column;
    }
    public sealed class OpsBlockTask
    {
        public int Id,Value,Deadline=-1,Avoid=-1;
        public string Name,Color,Kind;
        public OpsBlockCell[] Cells,Solution,Placed;
        public bool Urgent,Auto;
        public bool Constrained=>Deadline>=0||Avoid>=0;
        public bool Violates=>Placed!=null&&(Deadline>=0&&Placed.Max(c=>c.Column)>Deadline||Avoid>=0&&Placed.Any(c=>c.Column==Avoid));
    }
    public sealed class OpsBlockMinigame : OpsMinigame
    {
        public int YearSeed {get;}
        public int Month {get;}
        public int Rows {get;private set;}
        public int Columns {get;private set;}
        public string[,] Locks {get;private set;}
        public IReadOnlyList<OpsBlockTask> Tasks=>tasks;
        public bool Automation {get;}
        public int Selected {get;private set;}=-1;
        public int Revision {get;private set;}
        public int Bonus {get;private set;}
        public float HitStopRemaining {get;private set;}
        public int PlacedCount=>tasks.Count(t=>t.Placed!=null);
        public string Feedback {get;private set;}="";
        public IReadOnlyList<int> NewlyFull=>newlyFull;
        public override string Grade=>Score>=OpsCatalog.BlockS?"S":Score>=OpsCatalog.BlockA?"A":Score>=OpsCatalog.BlockB?"B":"C";
        private readonly List<OpsBlockTask> tasks=new List<OpsBlockTask>();
        private readonly List<int> newlyFull=new List<int>();private bool[] full;
        private readonly Random random;
        private static readonly string[] Kinds={"I2","I3","O4","L3","L4","T4","I4","1"};
        private static OpsBlockCell[] Shape(params int[] pairs)=>Enumerable.Range(0,pairs.Length/2).Select(i=>new OpsBlockCell(pairs[i*2],pairs[i*2+1])).ToArray();
        private static readonly OpsBlockCell[][] Shapes={Shape(0,0,0,1),Shape(0,0,0,1,0,2),Shape(0,0,0,1,1,0,1,1),Shape(0,0,1,0,1,1),Shape(0,0,1,0,2,0,2,1),Shape(0,0,0,1,0,2,1,1),Shape(0,0,0,1,0,2,0,3),Shape(0,0)};
        private static readonly string[][] Names={new[]{"VPN機器の緊急修正","研修の準備","ルーターの更新","ログ設定の見直し","権限の見直し"},new[]{"社員PCの更新","資産台帳の整備","古いアカウントの削除"},new[]{"基幹サーバーの更新","データベースの移行"},new[]{"メール移行（準備→本番）","設定変更→動作確認","新入社員のPC準備"},new[]{"バックアップの強化","クラウドへの移行"},new[]{"ネットワークの切替","多要素認証の導入"},new[]{"全社のOS更新"},new[]{"ファイルサーバーの再起動","証明書の更新","アカウントの棚卸し","プリンタの入れ替え","無線LANの設定変更"}};
        private static readonly string[] Colors={"2bb673","3fa9f5","ff8a3d","6c63ff","ff8a3d","3fa9f5","1fb5b0","e0a100"};
        private static readonly string[] LockNames={"朝会","来客対応","定例会議","給与の計算","健康診断","全社の研修","監査の立ち会い","取引先の訪問","月末の締め"};
        public OpsBlockMinigame(OpsState state):base("作業のはめ込み",state,OpsCatalog.BlockSeconds)
        {
            YearSeed=state.seed;Month=state.month;Automation=state.Level("automation")>0;
            random=new Random(unchecked(state.seed^((state.month+1)*OpsCatalog.WorkSeedStride)^OpsCatalog.BlockSeedSalt));
            Generate();full=new bool[Columns];
        }
        private static OpsBlockCell[] Normalize(IEnumerable<OpsBlockCell> cells)
        {var a=cells.ToArray();int r=a.Min(c=>c.Row),col=a.Min(c=>c.Column);return a.Select(c=>new OpsBlockCell(c.Row-r,c.Column-col)).OrderBy(c=>c.Row).ThenBy(c=>c.Column).ToArray();}
        private static OpsBlockCell[] Turn(OpsBlockCell[] cells)=>Normalize(cells.Select(c=>new OpsBlockCell(c.Column,-c.Row)));
        private void Generate()
        {
            for(int attempt=0;attempt<OpsCatalog.BlockGenerationAttempts;attempt++)
            {
                Rows=random.NextDouble()<OpsCatalog.BlockThreeRowsChance?3:4;Columns=OpsCatalog.BlockColumnChoices[random.Next(OpsCatalog.BlockColumnChoices.Length)];
                Locks=new string[Rows,Columns];tasks.Clear();
                int target=(int)Math.Round(Rows*Columns*(OpsCatalog.BlockLockMinimum+random.NextDouble()*OpsCatalog.BlockLockVariation)),count=0;
                while(count<target){int r=random.Next(Rows),c=random.Next(Columns);if(Locks[r,c]!=null)continue;Locks[r,c]=LockNames[count%LockNames.Length];count++;
                    if(random.NextDouble()<OpsCatalog.BlockLongLockChance&&r+1<Rows&&Locks[r+1,c]==null&&count<target){Locks[r+1,c]=Locks[r,c];count++;}}
                var occupied=new bool[Rows,Columns];for(int r=0;r<Rows;r++)for(int c=0;c<Columns;c++)occupied[r,c]=Locks[r,c]!=null;
                int nodes=0;if(!Tile(occupied,0,ref nodes)||tasks.Count<OpsCatalog.BlockMinTasks||tasks.Count>OpsCatalog.BlockMaxTasks)continue;
                var early=tasks.Where(t=>t.Solution.Max(c=>c.Column)<Columns-1).OrderBy(_=>random.Next()).ToArray();if(early.Length<2)continue;
                var urgent=early[0];urgent.Urgent=true;urgent.Deadline=urgent.Solution.Max(c=>c.Column);urgent.Name="緊急修正と影響調査";urgent.Color="e0405f";urgent.Value+=OpsCatalog.BlockUrgentValue;
                var deadline=early[1];deadline.Deadline=Math.Min(Columns-2,deadline.Solution.Max(c=>c.Column)+random.Next(2));
                var avoid=tasks.FirstOrDefault(t=>t!=urgent&&t!=deadline&&t.Solution.Select(c=>c.Column).Distinct().Count()<Columns);if(avoid==null)continue;
                var empty=Enumerable.Range(0,Columns).Where(c=>!avoid.Solution.Any(s=>s.Column==c)).ToArray();avoid.Avoid=empty[random.Next(empty.Length)];
                var used=new HashSet<string>();foreach(var t in tasks){string name=t.Name;int suffix=2;while(!used.Add(t.Name))t.Name=name+"（"+(suffix++)+"件目）";}
                return;
            }
            throw new InvalidOperationException("正解のある予定表を生成できませんでした。");
        }
        private bool Tile(bool[,] occupied,int singles,ref int nodes)
        {
            if(++nodes>OpsCatalog.BlockSearchNodes)return false;
            int er=-1,ec=-1;for(int r=0;r<Rows&&er<0;r++)for(int c=0;c<Columns;c++)if(!occupied[r,c]){er=r;ec=c;break;}
            if(er<0)return true;if(tasks.Count>=OpsCatalog.BlockMaxTasks)return false;
            var order=Enumerable.Range(0,Shapes.Length-1).OrderBy(_=>random.Next()).Concat(new[]{Shapes.Length-1});
            foreach(int kind in order)
            {
                if(kind==Shapes.Length-1&&singles>=OpsCatalog.BlockMaxSingles)continue;
                var shape=Shapes[kind];var seen=new HashSet<string>();
                for(int turn=0;turn<4;turn++,shape=Turn(shape))
                {
                    var norm=Normalize(shape);if(!seen.Add(string.Join(";",norm.Select(c=>c.Row+","+c.Column))))continue;
                    int or=er-norm[0].Row,oc=ec-norm[0].Column;
                    var cells=norm.Select(c=>new OpsBlockCell(c.Row+or,c.Column+oc)).ToArray();
                    if(cells.Any(c=>c.Row<0||c.Column<0||c.Row>=Rows||c.Column>=Columns||occupied[c.Row,c.Column]))continue;
                    foreach(var c in cells)occupied[c.Row,c.Column]=true;
                    tasks.Add(new OpsBlockTask{Id=tasks.Count,Kind=Kinds[kind],Name=Names[kind][random.Next(Names[kind].Length)],Color=Colors[kind],Value=OpsCatalog.BlockBaseValue+cells.Length*OpsCatalog.BlockCellValue,Cells=Normalize(cells),Solution=cells});
                    if(Tile(occupied,singles+(kind==Shapes.Length-1?1:0),ref nodes))return true;
                    tasks.RemoveAt(tasks.Count-1);foreach(var c in cells)occupied[c.Row,c.Column]=false;
                }
            }
            return false;
        }
        public override bool Start()
        {
            if(!base.Start())return false;
            if(Automation){var t=tasks.Where(t=>!t.Constrained).OrderBy(t=>t.Cells.Length).First();t.Auto=true;Place(t.Id,t.Solution.Min(c=>c.Row),t.Solution.Min(c=>c.Column));}
            HitStopRemaining=0;Revision++;return true;
        }
        public bool Select(int id)
        {var t=tasks.FirstOrDefault(x=>x.Id==id);if(Phase!=OpsMinigamePhase.Playing||t==null||t.Placed!=null)return false;Selected=id;Revision++;return true;}
        public bool Lift(int id)
        {var t=tasks.FirstOrDefault(x=>x.Id==id);if(Phase!=OpsMinigamePhase.Playing||t==null)return false;if(t.Placed!=null)t.Cells=Normalize(t.Placed);t.Placed=null;Selected=id;CheckDays();Revision++;return true;}
        public bool Rotate(int id)
        {var t=tasks.FirstOrDefault(x=>x.Id==id);if(Phase!=OpsMinigamePhase.Playing||t==null||t.Placed!=null)return false;t.Cells=Turn(t.Cells);Revision++;return true;}
        public bool Fits(int id,int row,int column)
        {
            var t=tasks.FirstOrDefault(x=>x.Id==id);return t!=null&&!t.Cells.Any(c=>c.Row+row<0||c.Column+column<0||c.Row+row>=Rows||c.Column+column>=Columns||Locks[c.Row+row,c.Column+column]!=null||tasks.Any(o=>o.Id!=id&&o.Placed!=null&&o.Placed.Contains(new OpsBlockCell(c.Row+row,c.Column+column))));
        }
        public bool Place(int id,int row,int column)
        {
            if(Phase!=OpsMinigamePhase.Playing||!Fits(id,row,column)){Feedback="その位置には置けないよ";return false;}
            var t=tasks.First(x=>x.Id==id);t.Placed=t.Cells.Select(c=>new OpsBlockCell(c.Row+row,c.Column+column)).ToArray();Selected=-1;
            Feedback=t.Violates?"はまったけど、期限・避ける日を確認しよう":"ぴったり！";HitStopRemaining=OpsCatalog.ContainmentHitStop;CheckDays();Revision++;return true;
        }
        private void CheckDays()
        {
            newlyFull.Clear();for(int c=0;c<Columns;c++)
            {
                bool filled=Enumerable.Range(0,Rows).All(r=>Locks[r,c]!=null||tasks.Any(t=>t.Placed!=null&&t.Placed.Contains(new OpsBlockCell(r,c))));
                if(filled&&!full[c]){Bonus+=OpsCatalog.BlockDayBonus;newlyFull.Add(c);}full[c]=filled;
            }
        }
        public bool Finish(){if(Phase!=OpsMinigamePhase.Playing)return false;OnTimeUp();return true;}
        public override void Tick(float delta)
        {
            if(Phase!=OpsMinigamePhase.Playing||float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return;
            float paused=Math.Min(delta,HitStopRemaining);HitStopRemaining-=paused;base.Tick(delta-paused);
        }
        protected override void OnTimeUp()=>Complete(Bonus+tasks.Where(t=>t.Placed!=null).Sum(t=>t.Violates?(int)Math.Round(t.Value*OpsCatalog.BlockBrokenFraction,MidpointRounding.AwayFromZero):t.Value));
    }
}
