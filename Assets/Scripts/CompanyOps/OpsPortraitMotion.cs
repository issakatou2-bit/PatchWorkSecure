using System;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // 足元を固定する補正で、呼吸・ポーズ交換・短い感情演技を重ねる。
    public sealed class OpsPortraitMotion : MonoBehaviour
    {
        public OpsGame Owner;
        public bool Enter=true;
        private RectTransform rect;
        private OpsPortraitAnimator animator;
        private Vector2 origin;
        private float time,actTime=-1,swapTime=-1;
        private string act="",queuedPose;
        private Action swap;
        private bool landed,started;
        public Vector2 LayoutPosition => started?origin:((RectTransform)transform).anchoredPosition;
        private bool Reduced => Owner!=null&&Owner.ReducedMotion;
        private void Start(){rect=(RectTransform)transform;origin=rect.anchoredPosition;animator=GetComponent<OpsPortraitAnimator>();time=Enter?0:.35f;started=true;ReactToPose(animator?.PoseId);}
        public void SwitchPose(Action apply,string id){swap=apply;queuedPose=id;swapTime=0;if(Reduced){swap();swap=null;swapTime=-1;ReactToPose(id);}}
        public void ReactToPose(string id)
        {
            act=id=="pose_jump"||id=="pose_peace"||id=="pose_salute"?"joy":id=="pose_startled"||id=="pose_run"?"startle":id=="pose_exhausted"||id=="pose_bow"?"sad":id=="pose_think"||id=="pose_armscross"?"think":"";
            actTime=0;landed=false;
            if(started&&animator?.ActivePersona!=null)Mark(act=="joy"?2:act=="startle"?0:act=="sad"?1:act=="think"?4:-1);
        }
        public void Celebrate(){act="joy";actTime=0;landed=false;Mark(2);}
        public void SmallCelebrate(){act="smalljoy";actTime=0;landed=false;Mark(2);}
        public void ShowEmotion(int type){Mark(type);}
        private void Update()
        {
            if(!started)return;float delta=Owner!=null?Owner.PresentationDeltaTime:Time.unscaledDeltaTime;time+=delta;
            if(swapTime>=0)
            {
                swapTime+=delta;if(swap!=null&&swapTime>=.06f){swap();swap=null;ReactToPose(queuedPose);}
                if(swapTime>=.24f)swapTime=-1;
            }
            if(actTime>=0)actTime+=delta;
            if(Reduced){if(swap!=null){swap();swap=null;swapTime=-1;}rect.anchoredPosition=origin;rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;return;}
            float speaking=animator!=null&&animator.IsSpeaking?1.5f:1;
            Vector2 offset=Vector2.up*(3-3*Mathf.Cos(time*Mathf.PI*2/3.2f*(speaking>1?2:1)))*speaking*(act=="sad"?.5f:1);
            Vector2 scale=new Vector2(1,1+.0075f-.0075f*Mathf.Cos(time*Mathf.PI*2/2.8f));float angle=0;
            // 1800pxの入場距離に対して、行き過ぎが約12pxになるイーズアウトバック。
            if(time<.35f){float t=time/.35f;const float s=.4571699f;float back=1+(s+1)*Mathf.Pow(t-1,3)+s*Mathf.Pow(t-1,2);offset.x+=(origin.x>700?1800:-1800)*(1-back);}
            if(swapTime>=0){float t=swapTime;float s=t<.06f?Mathf.Lerp(1,.9f,t/.06f):t<.14f?Mathf.Lerp(.9f,1.06f,(t-.06f)/.08f):Mathf.Lerp(1.06f,1,(t-.14f)/.1f);scale*=s;}
            if(act=="joy"&&actTime<.4f)
            {
                if(actTime<.08f)scale*=Vector2.Lerp(Vector2.one,new Vector2(1.06f,.92f),actTime/.08f);
                else if(actTime<.26f){float t=(actTime-.08f)/.18f;offset.y+=Mathf.Sin(t*Mathf.PI)*36;scale*=new Vector2(.95f,1.08f);}
                else if(actTime<.32f){scale*=new Vector2(1.05f,.94f);if(!landed){landed=true;Mark(5);}}
                else scale*=Vector2.Lerp(new Vector2(1.05f,.94f),Vector2.one,(actTime-.32f)/.08f);
            }
            if(act=="smalljoy"&&actTime<.35f)offset.y+=12*Mathf.Sin(actTime/.35f*Mathf.PI);
            if(act=="startle"&&actTime<.4f){float t=actTime;offset.x+=t<.1f?12*t/.1f:12*(1-Mathf.Clamp01((t-.1f)/.3f))+Mathf.Sin((t-.1f)/.25f*Mathf.PI*8)*2;angle=3*(1-Mathf.Clamp01(t/.4f));}
            if(act=="sad"){float t=Mathf.Clamp01(actTime/.4f);offset.y-=10*t;scale.y*=Mathf.Lerp(1,.97f,t);}
            var foot=Vector2.Scale(new Vector2(.5f,0)-rect.pivot,rect.rect.size);
            var rotation=Quaternion.Euler(0,0,angle);Vector2 scaledFoot=rotation*Vector2.Scale(foot,scale);
            rect.localScale=new Vector3(scale.x,scale.y,1);rect.localRotation=rotation;rect.anchoredPosition=origin+offset+foot-scaledFoot;
        }
        private void Mark(int type)
        {
            if(animator==null)animator=GetComponent<OpsPortraitAnimator>();
            if(type<0||animator?.ActivePersona==null||animator.ActivePersona.EmotionMarks==null||animator.ActivePersona.EmotionMarks.Length<=type)return;
            int count=type==2?3:type==5?2:1;
            for(int i=0;i<count;i++)
            {
                var go=new GameObject("HinataEmotion",typeof(RectTransform),typeof(Image),typeof(OpsEmotionMotion));go.transform.SetParent(transform,false);
                var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=type==5?new Vector2(.5f,0):new Vector2(.8f,.82f);r.pivot=new Vector2(.5f,.5f);r.sizeDelta=type==5?new Vector2(22,12):new Vector2(28,32);
                r.anchoredPosition=new Vector2(i*24-12,type==5?4:i*14);var image=go.GetComponent<Image>();image.sprite=animator.ActivePersona.EmotionMarks[type];image.raycastTarget=false;
                var motion=go.GetComponent<OpsEmotionMotion>();motion.Owner=Owner;motion.Kind=type;motion.Offset=i;
            }
        }
    }
    public sealed class OpsEmotionMotion : MonoBehaviour
    {
        public OpsGame Owner;public int Kind,Offset;private float time;private Vector2 origin;
        private void Start(){origin=((RectTransform)transform).anchoredPosition;}
        private void Update(){time+=Time.unscaledDeltaTime;float lifetime=Kind==0?1:Kind==5?.25f:.8f;float t=Mathf.Clamp01(time/lifetime);var r=(RectTransform)transform;
            var image=GetComponent<Image>();image.color=new Color(1,1,1,1-t);
            if(Owner==null||!Owner.ReducedMotion){r.anchoredPosition=origin+new Vector2(Kind==5?(Offset==0?-12:12)*t:0,Kind==1?-20*t:Kind==2?28*t:0);
                r.localScale=Vector3.one*(Kind==0?t<.2f?Mathf.Lerp(.5f,1.2f,t/.2f):Mathf.Lerp(1.2f,1,(t-.2f)/.8f):Kind==3?1+.1f*Mathf.Sin(t*Mathf.PI*4):1);
                if(Kind==4)r.localEulerAngles=new Vector3(0,0,t*45);}
            if(t>=1)Destroy(gameObject);}
    }
}
