using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public sealed class OpsBubbleMotion : MonoBehaviour
    {
        public OpsGame Owner;public string Kind="bubble";public float ArrivalAt,PopAt=-1;public Vector2 Direction;
        private RectTransform rect;private Vector2 origin;private CanvasGroup group;private float started;
        private void Start(){rect=(RectTransform)transform;origin=rect.anchoredPosition;started=Time.unscaledTime;group=gameObject.AddComponent<CanvasGroup>();}
        private void Update()
        {
            float now=Time.unscaledTime,t=now-started;bool reduced=Owner!=null&&Owner.ReducedMotion;
            if(Kind=="drop"||Kind=="reward")
            {
                float duration=Kind=="drop"?.5f:1f,p=Mathf.Clamp01(t/duration);group.blocksRaycasts=false;group.alpha=1-p;
                rect.anchoredPosition=origin+(reduced?Vector2.zero:Kind=="drop"?new Vector2(Direction.x,-Direction.y)*p:Vector2.up*70*p);rect.localScale=Vector3.one*(reduced?1:Kind=="drop"?1-.7f*p:1);
                if(p>=1)Destroy(gameObject);return;
            }
            if(PopAt>=0){float p=Mathf.Clamp01((now-PopAt)/.32f);group.blocksRaycasts=false;rect.localScale=Vector3.one*(reduced?1:p<.3f?1-.18f*p/.3f:.82f+(p-.3f)*1.1f);group.alpha=1-p;if(p>=1)Destroy(gameObject);return;}
            float age=now-ArrivalAt;group.blocksRaycasts=age>=0;group.alpha=age<0?0:1;
            if(age<0){rect.localScale=Vector3.zero;return;}
            rect.localScale=Vector3.one*(reduced?1:EntranceScale(age));
            rect.anchoredPosition=origin+(reduced?Vector2.zero:FloatOffset(age));
        }
        // モックの .5秒 bIn と3.2秒 bFloat。ゲームの乱数・状態には触れない。
        public static float EntranceScale(float age)
        {
            float p=Mathf.Clamp01(age/.5f);
            return p<.7f?Mathf.Lerp(0,1.12f,Mathf.SmoothStep(0,1,p/.7f)):Mathf.Lerp(1.12f,1,Mathf.SmoothStep(0,1,(p-.7f)/.3f));
        }
        public static Vector2 FloatOffset(float age)
        {
            float p=Mathf.Repeat(age/3.2f,1);Vector2 a,b;float t;
            if(p<.33f){a=Vector2.zero;b=new Vector2(3,7);t=p/.33f;}
            else if(p<.66f){a=new Vector2(3,7);b=new Vector2(-3,3);t=(p-.33f)/.33f;}
            else {a=new Vector2(-3,3);b=Vector2.zero;t=(p-.66f)/.34f;}
            return Vector2.Lerp(a,b,Mathf.SmoothStep(0,1,t));
        }
    }
    // UIだけで再現するモックの動き。ゲームの乱数・判定には触れない。
    public sealed class OpsPlanningMotion : MonoBehaviour
    {
        public OpsGame Owner;
        public string Kind;
        public float Period = 3.2f, Delay;
        private RectTransform rect;
        private Vector2 origin;
        private Graphic image;
        private Color color;
        private float started;
        private void Start()
        {
            rect = (RectTransform)transform; origin = rect.anchoredPosition;
            image = GetComponent<Graphic>(); if (image != null) color = image.color;
            started = Time.unscaledTime;
        }
        private void Update()
        {
            if (rect == null) return;
            if (Owner == null || Owner.ReducedMotion)
            {
                rect.anchoredPosition = origin; rect.localScale = Vector3.one*(Kind=="kv"?1.02f:1); rect.localRotation = Quaternion.identity;
                if (image != null) image.color = Kind == "petal" || Kind == "snow" || Kind == "rain" || Kind == "ripple" || Kind == "mote" || Kind == "haze" || Kind == "shine" || Kind == "pulse" ? new Color(color.r,color.g,color.b,0) : color;
                return;
            }
            float t = Mathf.Repeat((Time.unscaledTime - started + Delay) / Period, 1);
            if(Kind=="ending-kv")
            {
                float p=Mathf.Clamp01((Time.unscaledTime-started)/Period);
                rect.localScale=Vector3.one*Mathf.Lerp(1.12f,1,1-Mathf.Pow(1-p,3));
            }
            if (Kind == "bob") rect.anchoredPosition = origin + Vector2.up * (5 - 5 * Mathf.Cos(t * Mathf.PI * 2));
            if (Kind == "drift")
            {
                float drift=.5f-.5f*Mathf.Cos(t*Mathf.PI*2);
                rect.anchoredPosition=origin+new Vector2(-14,-8)*drift;
                rect.localScale=Vector3.one*Mathf.Lerp(1.04f,1.08f,drift);
            }
            if(Kind=="kv")
            {
                float drift=.5f-.5f*Mathf.Cos(t*Mathf.PI*2);
                rect.anchoredPosition=origin+new Vector2(-10,6)*drift;rect.localScale=Vector3.one*Mathf.Lerp(1.02f,1.06f,drift);
            }
            if (Kind == "shake") rect.anchoredPosition = origin + Vector2.right * (t < .35f ? Mathf.Sin(t * Mathf.PI * 18) * 3 : 0);
            if (Kind == "alarm" && image != null) image.color = new Color(color.r,color.g,color.b,.7f+.3f*Mathf.Sin(t*Mathf.PI*2));
            if (Kind == "rotate") rect.localEulerAngles = new Vector3(0,0,-360*t);
            if (Kind == "hop") rect.anchoredPosition = origin + Vector2.up * Mathf.Max(0,Mathf.Sin(t*Mathf.PI*2))*28;
            if (Kind == "pop")
            {
                float hop = Mathf.Max(0, Mathf.Sin(t * Mathf.PI * 2));
                rect.anchoredPosition = origin + Vector2.up * hop * 8; rect.localScale = Vector3.one * (1 + hop * .08f);
            }
            if (Kind == "blink" && image != null) image.color = new Color(color.r,color.g,color.b,t < .5f ? color.a : color.a*.15f);
            if (Kind == "shine") rect.anchoredPosition = new Vector2(Mathf.Lerp(-rect.rect.width, ((RectTransform)rect.parent).rect.width * 1.2f, Mathf.Min(1,t/.6f)),origin.y);
            if (Kind == "pulse")
            {
                rect.localScale = Vector3.one * (1 + .09f*t);
                if (image != null) image.color = new Color(color.r,color.g,color.b,color.a*(1-t));
            }
            if (Kind == "petal")
            {
                rect.anchoredPosition = origin + new Vector2(-120*t, 40 - 860*t); rect.localEulerAngles = new Vector3(0,0,540*t);
                if (image != null) image.color = new Color(color.r,color.g,color.b,color.a*Mathf.Min(t*10,(1-t)*6));
            }
            if (Kind == "snow" || Kind == "rain" || Kind == "mote")
            {
                float height=((RectTransform)rect.parent).rect.height;
                rect.anchoredPosition=origin+new Vector2(Kind=="rain"?-80*t:Mathf.Sin(t*Mathf.PI*3)*20,Kind=="mote"?height*.5f*t:40-(height+80)*t);
                if(image!=null)image.color=new Color(color.r,color.g,color.b,color.a*Mathf.Min(t*10,(1-t)*6));
            }
            if (Kind == "haze" && image != null) image.color=new Color(color.r,color.g,color.b,color.a*(.5f+.5f*Mathf.Sin(t*Mathf.PI*2)));
            if (Kind == "ripple")
            {
                rect.localScale=Vector3.one*Mathf.Lerp(.3f,1.7f,t);
                if(image!=null)image.color=new Color(color.r,color.g,color.b,color.a*Mathf.Min(1,t*8)*(1-t));
            }
        }
    }
}
