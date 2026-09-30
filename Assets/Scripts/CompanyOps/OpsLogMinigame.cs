using System;
using System.Collections.Generic;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public sealed class OpsLogRow
    {
        public int Id, Kind=-1;
        public string Time, User, Ip, Event, Reason;
        public bool Hit;
        public bool Suspicious=>Kind>=0;
    }
    public sealed class OpsLogMinigame : OpsMinigame
    {
        public int YearSeed {get;}
        public int Month {get;}
        public int Found {get;private set;}
        public int Wrong {get;private set;}
        public int Total {get;private set;}
        public int Revision {get;private set;}
        public int Knowledge=>Math.Min(OpsCatalog.KnowledgeMax,Found*OpsCatalog.KnowledgeMax/OpsCatalog.LogBadCount);
        public IReadOnlyList<OpsLogRow> Visible=>visible;
        public IReadOnlyList<OpsLogRow> Queue=>queue;
        public IReadOnlyList<string> Clues=>clues;
        public override string Grade=>Score>=OpsCatalog.LogS?"S":Score>=OpsCatalog.LogA?"A":Score>=OpsCatalog.LogB?"B":"C";
        private readonly List<OpsLogRow> queue=new List<OpsLogRow>(),visible=new List<OpsLogRow>();
        private readonly List<string> clues=new List<string>();
        private int next;private float feedAt=OpsCatalog.LogFeedSeconds;
        private static readonly string[] Users={"tanaka","mori","ono","aida","ogawa","saeki"};
        private static readonly string[] Normal={"ログイン成功","ログアウト","ファイル共有にアクセス（見積書.xlsx）","パスワード変更","ログイン成功（多要素認証あり）","メールを送信（取引先 1件）","VPN接続 成功"};
        private static readonly string[] Bad={"ログイン失敗（パスワード誤り）×27","ログイン成功","ログイン成功","ファイル共有から一括ダウンロード（顧客名簿 ほか 842件）","管理者権限を付与（admin_tmp）","監査ログの記録を停止","ログイン成功","メール転送ルールを追加（すべて→外部アドレス）"};
        public static readonly string[] Reasons={"短時間に何十回もの失敗。パスワードの総当たり","失敗が続いた同じ海外のIPから、急に成功。突破された可能性","深夜3時のログイン。本人の勤務時間ではない","普段の何百倍ものダウンロード。持ち出しの疑い","見覚えのない管理者アカウントの作成","記録を止めるのは、痕跡を消したい攻撃者の定番","5分前は社内から。移動できない距離の国からログイン（あり得ない移動）","全メールを外部へ転送する設定。情報の持ち出し"};
        public OpsLogMinigame(OpsState state):base("ログを調べる",state,OpsCatalog.LogSeconds)
        {
            YearSeed=state.seed;Month=state.month;
            var rng=new Random(unchecked(state.seed^((state.month+1)*OpsCatalog.WorkSeedStride)^OpsCatalog.LogSeedSalt));
            var others=Enumerable.Range(2,Bad.Length-2).OrderBy(_=>rng.Next()).Take(OpsCatalog.LogBadCount-2).ToList();
            var kinds=Enumerable.Repeat(-1,OpsCatalog.LogNormalCount).ToList();int pos=3;
            kinds.Insert(pos,0);kinds.Insert(pos+1,1);pos+=4+rng.Next(4);
            foreach(int kind in others){kinds.Insert(Math.Min(pos,kinds.Count),kind);pos+=3+rng.Next(4);}
            string attackIp="185.220."+rng.Next(1,255)+"."+rng.Next(1,255),attackUser=Users[rng.Next(Users.Length)];int minute=9*60;
            foreach(int kind in kinds)
            {
                minute+=3+rng.Next(9);int normal=rng.Next(Normal.Length);
                string ip=kind==0||kind==1?attackIp:kind==6?"45.83.64."+rng.Next(1,255):normal==6&&kind<0?"203.0.113.5":"10.0."+rng.Next(1,3)+"."+rng.Next(10,60);
                queue.Add(new OpsLogRow{Id=queue.Count,Kind=kind,Time=(kind==2?3:minute/60%24).ToString("00")+":"+(minute%60).ToString("00"),User=kind==0||kind==1?attackUser:Users[rng.Next(Users.Length)],Ip=ip,Event=kind<0?Normal[normal]:Bad[kind],Reason=kind<0?"":Reasons[kind]});
            }
        }
        public override bool Start(){if(!base.Start())return false;for(int i=0;i<OpsCatalog.LogInitialRows;i++)Push();return true;}
        private void Push()
        {
            if(next>=queue.Count)return;var row=queue[next++];if(row.Suspicious)Total++;
            visible.Insert(0,row);if(visible.Count>OpsCatalog.LogVisibleRows)visible.RemoveAt(visible.Count-1);Revision++;
        }
        public override void Tick(float delta)
        {
            if(Phase!=OpsMinigamePhase.Playing||float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return;
            float target=Math.Min(Duration,Elapsed+delta);
            while(feedAt<=target){Push();feedAt+=OpsCatalog.LogFeedSeconds;}
            base.Tick(delta);
        }
        public bool Hit(int id)
        {
            var row=visible.FirstOrDefault(r=>r.Id==id);
            if(Phase!=OpsMinigamePhase.Playing||row==null||row.Hit)return false;
            row.Hit=true;if(row.Suspicious){Found++;clues.Insert(0,row.Reason);}else Wrong++;
            Revision++;return true;
        }
        protected override void OnTimeUp()=>Complete((int)Math.Round(Found*100.0/Math.Max(1,Total),MidpointRounding.AwayFromZero)-Wrong*OpsCatalog.LogWrongPenalty);
    }
}
