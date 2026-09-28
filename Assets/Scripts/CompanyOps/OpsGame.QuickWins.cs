using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private bool budgetGainPending;
        private OpsOutcome PreviousReport(OpsOutcome current)=>State.history.FirstOrDefault(r=>r.month==current.month-1);
        public static bool IsGoodMonthlyChange(int delta,bool higherIsBetter)=>higherIsBetter?delta>0:delta<0;
        private void MonthTrend(Transform parent,string id,int delta,bool higherIsBetter,float x,float y)
        {
            Color tone=delta==0?PlanGray:IsGoodMonthlyChange(delta,higherIsBetter)?Hex("168368"):IncidentRed;
            var row=Rect(parent,id,x,y,62,24);
            IncidentShape(row,id+"Arrow",delta>0?"trend-up":delta<0?"trend-down":"trend-flat",0,5,12,14,tone);
            PText(row,id+"Amount",System.Math.Abs(delta).ToString(),17,0,45,24,14,tone);
        }
        private int[] rankBefore,rankAfter;
        private bool workCompletePending;private int workCompleteMonth=-1;
        private int lastDanger=100;private OpsState dangerState;private AudioSource dangerAudio;
        public int DangerPulseCount {get;private set;}
        private RectTransform blockedTag;private AudioSource rejectAudio;
        public int RejectedPressCount {get;private set;}
        private string ButtonBlockReason(string id)
        {
            if(id=="ContinueYear")return saved==null?"続きの記録はありません":"記録を確認してください";
            if(State!=null)
            {
                if(id.StartsWith("Action_"))return State.ActionBlock(id.Substring(7));
                if(id.StartsWith("Buy_")){int index=OpsCatalog.Index(id.Substring(4));if(index>=0)return State.UpgradeBlock(index);}
                if(id=="Proposal"||id.StartsWith("Propose_"))return State.ActionBlock("proposal");
                if(id=="ChainTalk")return State.ActionBlock("listen");
            }
            return "今は選べません";
        }
        public void RejectButton(UnityEngine.UI.Button button)
        {
            if(button==null||button.IsInteractable())return;
            RejectedPressCount++;if(blockedTag!=null)Destroy(blockedTag.gameObject);
            var rect=(RectTransform)button.transform;var point=screen.InverseTransformPoint(rect.TransformPoint(new Vector3(rect.rect.width/2,0,0)));
            blockedTag=PCard(Surface,"BlockedReason",Mathf.Clamp(point.x-130,12,1328),Mathf.Clamp(-point.y-48,12,840),260,40,PlanInk,16,false);
            PText(blockedTag,"BlockedReasonText",ButtonBlockReason(button.name),10,0,240,40,14,Color.white,true,true);
            blockedTag.gameObject.AddComponent<OpsBlockedTag>();StartCoroutine(RejectedSound());
        }
        private IEnumerator RejectedSound()
        {
            if(muted||soundVolume<=0||Sounds?.damage==null)yield break;
            if(rejectAudio==null)rejectAudio=NewAudioSource();
            for(int i=0;i<2;i++){rejectAudio.clip=Sounds.damage;rejectAudio.volume=soundVolume*.25f;rejectAudio.pitch=.8f;rejectAudio.Play();yield return new WaitForSecondsRealtime(.07f);rejectAudio.Stop();if(i==0)yield return new WaitForSecondsRealtime(.05f);}
        }
        private void CheckDangerSignal()
        {
            if(!Application.isPlaying)return;
            if(dangerState!=State){dangerState=State;lastDanger=100;DangerPulseCount=0;}
            if(State.stability>=35){lastDanger=100;return;}
            if(State.stability>=lastDanger)return;
            lastDanger=State.stability;DangerPulseCount++;StartCoroutine(QuietHeartbeat());
        }
        private IEnumerator QuietHeartbeat()
        {
            // 登録済みB案の低いダメージ音を小音量・短い二打で使う。新しい音源を登録しない。
            if(muted||soundVolume<=0||Sounds==null||Sounds.damage==null)yield break;
            if(dangerAudio==null)dangerAudio=NewAudioSource();
            for(int i=0;i<2;i++)
            {
                dangerAudio.clip=Sounds.damage;dangerAudio.pitch=.65f;dangerAudio.volume=soundVolume*(i==0?.12f:.08f);dangerAudio.Play();
                yield return new WaitForSecondsRealtime(.09f);dangerAudio.Stop();
                if(i==0)yield return new WaitForSecondsRealtime(.1f);
            }
        }
        private void DangerGauge(Transform parent,string id,float x,float y,float width,float height)
        {
            if(State.stability>=35||width<=0)return;
            var overlay=PCard(parent,id,x,y,width,height,IncidentRed,12,false);
            overlay.gameObject.AddComponent<OpsDangerPulse>();
        }
        private void WorkCompleteEffect()
        {
            if(!Application.isPlaying||!workCompletePending)return;workCompletePending=false;workCompleteMonth=State.month;
            var effect=screen.Find("Stat_1").gameObject.AddComponent<OpsWorkComplete>();effect.Owner=this;
            effect.Tokens=screen.Find("Stat_1").GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("WorkToken")).ToArray();
            effect.Portrait=screen.GetComponentsInChildren<OpsPortraitMotion>().FirstOrDefault();
        }
        private int[] CompanyRankMetrics()=>new[]{State.stability,State.Organization,State.trust,State.culture,State.Preparedness,State.Resilience};
        private void RankChangeEffect(TextMeshProUGUI label,Image badge,int before,int after)
        {
            if(!Application.isPlaying||PlanningRank(before)==PlanningRank(after))return;
            var effect=label.gameObject.AddComponent<OpsRankChange>();effect.Owner=this;effect.Before=PlanningRank(before);effect.After=PlanningRank(after);effect.Up=after>before;effect.Badge=badge;
        }
        private void BudgetGainEffect(RectTransform target)
        {
            if(!Application.isPlaying||!budgetGainPending)return;budgetGainPending=false;
            var layer=Rect(screen,"BudgetGainEffect",0,0,1600,900);
            var effect=layer.gameObject.AddComponent<OpsBudgetGain>();effect.Owner=this;effect.Target=target;
            effect.Coins=new RectTransform[5];
            for(int i=0;i<effect.Coins.Length;i++)
            {
                var coin=PCard(layer,"GainCoin"+i,1000,148,22,22,Hex("ffd23f"),20,false);
                PText(coin,"CoinMark","円",0,0,22,22,11,Hex("7a5a00"),true,true);
                coin.gameObject.AddComponent<CanvasGroup>().blocksRaycasts=false;
                effect.Coins[i]=coin;
            }
        }
    }
    public sealed class OpsStaffBounce : MonoBehaviour
    {
        public OpsGame Owner;public string Member;private RectTransform rect;private Vector2 origin;private float started;private CanvasGroup group;
        private void Start(){rect=(RectTransform)transform;origin=rect.anchoredPosition;started=Time.realtimeSinceStartup;group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;}
        private void Update()
        {
            float t=Mathf.Clamp01((Time.realtimeSinceStartup-started)/.3f);
            rect.anchoredPosition=origin+Vector2.up*(Owner.ReducedMotion?0:12*Mathf.Sin(t*Mathf.PI));
            group.alpha=Owner.ReducedMotion?1-.45f*Mathf.Sin(t*Mathf.PI):1;
            if(t>=1){rect.anchoredPosition=origin;group.alpha=1;Destroy(this);}
        }
    }
    public sealed class OpsBlockedTag : MonoBehaviour
    {
        private CanvasGroup group;private float started;
        private void Start(){started=Time.realtimeSinceStartup;group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;}
        private void Update(){float t=Time.realtimeSinceStartup-started;group.alpha=1-Mathf.Clamp01((t-1.1f)/.2f);if(t>=1.3f)Destroy(gameObject);}
    }
    public sealed class OpsDangerPulse : MonoBehaviour
    {
        private Image image;private float started;
        private void Start(){image=GetComponent<Image>();started=Time.realtimeSinceStartup;}
        private void Update(){image.color=new Color(1,.23f,.36f,.65f+.35f*Mathf.Cos((Time.realtimeSinceStartup-started)*Mathf.PI));}
    }
    public sealed class OpsWorkComplete : MonoBehaviour
    {
        public OpsGame Owner;public Image[] Tokens;public OpsPortraitMotion Portrait;
        private Color[] colors;private float started;private bool reacted;
        private void Start(){started=Time.realtimeSinceStartup;colors=Tokens.Select(t=>t.color).ToArray();}
        private void Update()
        {
            float t=Mathf.Clamp01((Time.realtimeSinceStartup-started)/.5f);
            if(!reacted&&t>=.28f){reacted=true;Portrait?.SmallCelebrate();}
            for(int i=0;i<Tokens.Length;i++)
            {
                float local=Mathf.Clamp01((t-i*.5f/Tokens.Length)*2),pulse=Mathf.Sin(local*Mathf.PI);
                Tokens[i].color=Owner.ReducedMotion?new Color(colors[i].r,colors[i].g,colors[i].b,1-.4f*pulse):Color.Lerp(colors[i],new Color(.25f,.75f,1),pulse);
            }
            if(t>=1){for(int i=0;i<Tokens.Length;i++)Tokens[i].color=colors[i];Destroy(this);}
        }
    }
    public sealed class OpsRankChange : MonoBehaviour
    {
        public OpsGame Owner;public string Before,After;public bool Up;public Image Badge;
        private TextMeshProUGUI label;private Color original;private float started;
        private void Start(){started=Time.realtimeSinceStartup;label=GetComponent<TextMeshProUGUI>();original=Badge.color;label.text=Up?Before:After;}
        private void Update()
        {
            float t=Mathf.Clamp01((Time.realtimeSinceStartup-started)/.4f);label.text=Up&&t<.5f?Before:After;
            label.transform.localScale=new Vector3(Up&&!Owner.ReducedMotion?Mathf.Abs(1-2*t):1,1,1);
            label.alpha=Owner.ReducedMotion?Mathf.Abs(1-2*t)*.65f+.35f:1;
            Badge.color=Color.Lerp(original,Up?Color.white:Color.gray,Mathf.Sin(t*Mathf.PI)*.6f);
            if(t>=1){label.text=After;label.alpha=1;label.transform.localScale=Vector3.one;Badge.color=original;Destroy(this);}
        }
    }
    // 表示専用。Unity/ゲームの抽選乱数や資源には触れない。
    public sealed class OpsBudgetGain : MonoBehaviour
    {
        public OpsGame Owner;public RectTransform Target;public RectTransform[] Coins;
        private float started;private Vector2 end;
        private void Start(){started=Time.realtimeSinceStartup;end=((RectTransform)transform).InverseTransformPoint(Target.TransformPoint(new Vector3(37,-32,0)));}
        private void LateUpdate()
        {
            float t=Mathf.Clamp01((Time.realtimeSinceStartup-started)/.6f);
            for(int i=0;i<Coins.Length;i++)
            {
                var start=new Vector2(1000,-148);float a=(i-2)*.6f;
                var scatter=start+new Vector2(Mathf.Sin(a)*85,Mathf.Cos(a)*45);
                float travel=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.25f)/.65f));
                Coins[i].anchoredPosition=Owner.ReducedMotion?start+new Vector2(i*26,0):t<.25f?Vector2.Lerp(start,scatter,t/.25f):Vector2.Lerp(scatter,end,travel);
                Coins[i].GetComponent<CanvasGroup>().alpha=t<.85f?1:1-(t-.85f)/.15f;
            }
            if(Target!=null)Target.localScale=Vector3.one*(Owner.ReducedMotion?1:1+.1f*Mathf.Sin(Mathf.Clamp01((t-.65f)/.35f)*Mathf.PI));
            if(t>=1){if(Target!=null)Target.localScale=Vector3.one;Destroy(gameObject);}
        }
    }
}
