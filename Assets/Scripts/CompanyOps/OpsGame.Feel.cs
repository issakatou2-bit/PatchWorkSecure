using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private float holdUntil,duckUntil,duckLevel=1;
        private readonly Dictionary<string,int> presentationVisits=new Dictionary<string,int>();
        private readonly System.Random audioRandom=new System.Random(Environment.TickCount^0x45ac);
        private string portraitScreenKey;
        internal bool PortraitEntering {get;private set;}
        private bool BeginPortraitScreen()
        {
            string key=homeVisible||State==null?"title":ResolutionActive?"resolution_"+State.month:State.phase+"_"+State.month;
            PortraitEntering=key!=portraitScreenKey;portraitScreenKey=key;return PortraitEntering;
        }
        public float PresentationDeltaTime => !ReducedMotion&&Time.unscaledTime<holdUntil?0:Time.unscaledDeltaTime;
        public bool PresentationHeld => !ReducedMotion&&Time.unscaledTime<holdUntil;
        public void HoldPresentation(float seconds=.08f){if(!ReducedMotion)holdUntil=Mathf.Max(holdUntil,Time.unscaledTime+Mathf.Clamp(seconds,.06f,.1f));}
        public void DuckMusic(float seconds=.65f){duckUntil=Mathf.Max(duckUntil,Time.unscaledTime+seconds);}
        private float VariedPitch() => .95f+(float)audioRandom.NextDouble()*.1f;
        private IEnumerator FinishCountSound()
        {
            if(countAudio==null||!countAudio.isPlaying)yield break;float played=lastCount,volume=countAudio.volume;
            for(float t=0;t<.06f;t+=Time.unscaledDeltaTime){if(lastCount!=played)yield break;countAudio.volume=volume*(1-t/.06f);yield return null;}
            if(lastCount==played)countAudio.Stop();
        }
        private float RepeatDuration(string key,float first,float later)
        {
            presentationVisits.TryGetValue(key,out int visits);presentationVisits[key]=visits+1;
            return visits>0?later:first;
        }
        private void LossTrail(Transform parent,string id,float x,float y,float width,float height,float before,float after,float scale)
        {
            if(before<=after||before<=0)return;
            var r=PCard(parent,id,x,y,width*Mathf.Clamp01(before/Mathf.Max(1,scale)),height,Color.white,12,false);
            var trail=r.gameObject.AddComponent<OpsLossTrail>();trail.Owner=this;trail.TargetWidth=width*Mathf.Clamp01(after/Mathf.Max(1,scale));
        }
        private string MissionNearText()
        {
            if(State.MissionReady)return "達成準備OK";
            State.MissionProgress(true,out int a,out int at);State.MissionProgress(false,out int b,out int bt);
            if(at>0&&at-a==1)return "設備 あと1つ";
            return bt>0&&bt-b==1?"現場 あと1手":"";
        }
        private Action[] PortraitDepartures()
        {
            var departures=new List<Action>();if(screen==null||!Application.isPlaying)return departures.ToArray();
            foreach(var animator in screen.GetComponentsInChildren<OpsPortraitAnimator>())
            {
                var original=animator.GetComponent<Image>();if(original==null||original.sprite==null||original.rectTransform.localScale.x<.5f)continue;
                var corners=new Vector3[4];original.rectTransform.GetWorldCorners(corners);
                var a=Surface.InverseTransformPoint(corners[1]);var b=Surface.InverseTransformPoint(corners[3]);
                var sprite=original.sprite;float x=a.x,y=-a.y,w=b.x-a.x,h=a.y-b.y;
                departures.Add(()=>{var r=Rect(Surface,"HinataDeparting",x,y,w,h);
                    var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;StartCoroutine(PortraitExit(r));});
            }
            return departures.ToArray();
        }
        private IEnumerator PortraitExit(RectTransform r)
        {
            var origin=r.anchoredPosition;float direction=origin.x<800?-1:1;var image=r.GetComponent<Image>();
            for(float t=0;t<.2f&&r!=null;t+=Time.unscaledDeltaTime)
            {if(!ReducedMotion)r.anchoredPosition=origin+Vector2.right*direction*1800*(t/.2f);image.color=new Color(1,1,1,1-t/.2f);yield return null;}
            if(r!=null)Destroy(r.gameObject);
        }
    }
    public sealed class OpsLossTrail : MonoBehaviour
    {
        public OpsGame Owner;public float TargetWidth;private float elapsed,startWidth;
        private void Start(){startWidth=((RectTransform)transform).rect.width;}
        private void Update(){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01((elapsed-.2f)/.4f);var rect=(RectTransform)transform;
            if(Owner==null||!Owner.ReducedMotion)rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Lerp(startWidth,TargetWidth,t));
            GetComponent<Image>().color=new Color(1,1,1,1-t);if(t>=1)Destroy(gameObject);}
    }
    public sealed class OpsRoomZoom : MonoBehaviour
    {
        public OpsGame Owner;private Vector2 position,size;private float elapsed;
        private void Start(){var r=(RectTransform)transform;position=r.anchoredPosition;size=r.sizeDelta;}
        private void Update(){elapsed+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.4f));var r=(RectTransform)transform;
            if(Owner!=null&&!Owner.ReducedMotion){r.anchoredPosition=Vector2.Lerp(new Vector2(0,-85),position,t);r.sizeDelta=Vector2.Lerp(new Vector2(600,600),size,t);}
            if(t>=1){r.anchoredPosition=position;r.sizeDelta=size;enabled=false;}}
    }
}
