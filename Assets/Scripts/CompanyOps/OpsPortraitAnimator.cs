using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // 原画は変えず、同じキャンバスの目と口を別のマスクで合成する。
    public sealed class OpsPortraitAnimator : MonoBehaviour
    {
        public OpsGame Owner;
        public NavigatorPersona Persona;
        private Image eyes,mouth,portrait;
        private RectTransform eyeMask,mouthMask;
        private System.Random random;
        private float blinkAt,blinkStarted=-1,mouthAt;
        private bool mouthOpen,externalSpeech,doubleBlink,followUpBlink;
        private string expression="normal",pose="pose_fists";
        private TextMeshProUGUI speech;
        private float speechAt;
        private int characters;
        public bool IsSpeaking {get;private set;}
        public int BlinkCount {get;private set;}
        public int EyePhase {get;private set;}
        public bool MouthIsOpen => mouthOpen;
        public NavigatorPersona ActivePersona => Owner!=null?Owner.Navigator:Persona;
        public string PoseId => GetComponent<OpsPortraitIdentity>()?.PoseId??pose;
        public string ExpressionId => expression;
        private NavigatorPersona.FaceAnimationFrames Frames => ActivePersona?.AnimationFrames?.FirstOrDefault(f=>f!=null &&
            (!string.IsNullOrEmpty(f.PoseId)?f.PoseId==PoseId:f.Expression==expression));
        public bool HasFrames => Frames!=null && (Frames.EyesClosed!=null || Frames.MouthClosed!=null || Frames.MouthOpen!=null);
        private void Awake()
        {
            random=new System.Random(unchecked(Environment.TickCount^System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)));
            portrait=GetComponent<Image>();eyes=Overlay("HinataEyes",out eyeMask);mouth=Overlay("HinataMouth",out mouthMask);ScheduleBlink();
        }
        private Image Overlay(string name,out RectTransform mask)
        {
            var root=new GameObject(name+"Mask",typeof(RectTransform),typeof(RectMask2D));root.transform.SetParent(transform,false);mask=(RectTransform)root.transform;
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(root.transform,false);
            var image=go.GetComponent<Image>();image.raycastTarget=false;image.enabled=false;return image;
        }
        public void SetExpression(string value){expression=value;ResetFrames();}
        public void ChangePose(string id)
        {
            if(id==PoseId && portrait.sprite==ActivePersona?.Pose(id))return;
            Action apply=()=>{pose=id;var identity=GetComponent<OpsPortraitIdentity>();if(identity!=null)identity.PoseId=id;
                portrait.sprite=ActivePersona?.Pose(id);ResetFrames();};
            var motion=GetComponent<OpsPortraitMotion>();if(motion!=null)motion.SwitchPose(apply,id);else apply();
        }
        private void ResetFrames(){eyes.enabled=mouth.enabled=false;blinkStarted=-1;EyePhase=0;doubleBlink=followUpBlink=mouthOpen=false;ScheduleBlink();}
        public void Speak(TextMeshProUGUI label,bool observeOnly=false)
        {
            if(speech!=null&&!externalSpeech)speech.maxVisibleCharacters=int.MaxValue;
            speech=label;externalSpeech=observeOnly;speechAt=Time.unscaledTime;speech.ForceMeshUpdate();characters=speech.textInfo.characterCount;
            if(!externalSpeech)speech.maxVisibleCharacters=Owner!=null&&Owner.ReducedMotion?int.MaxValue:0;
        }
        public void StopSpeaking()
        {
            if(speech!=null&&!externalSpeech)speech.maxVisibleCharacters=int.MaxValue;
            speech=null;characters=0;IsSpeaking=false;mouthOpen=false;
        }
        private void ScheduleBlink(){blinkAt=Time.unscaledTime+3+(float)random.NextDouble()*2;}
        public void Blink(){if(Frames?.EyesClosed==null)return;blinkStarted=Time.unscaledTime;doubleBlink=!followUpBlink&&random.NextDouble()<.2;followUpBlink=false;BlinkCount++;}
        private void FitOverlay(RectTransform mask,Image overlay,Rect region)
        {
            var area=((RectTransform)transform).rect;float w=area.width,h=area.height;
            if(portrait.sprite!=null&&portrait.preserveAspect){float ratio=portrait.sprite.rect.width/portrait.sprite.rect.height;
                if(w/h>ratio)w=h*ratio;else h=w/ratio;}
            mask.anchorMin=mask.anchorMax=new Vector2(.5f,.5f);mask.pivot=Vector2.zero;
            // Image.preserveAspectの余白は、RectTransformの基準点に合わせて寄せられる。
            var inset=Vector2.Scale(new Vector2(area.width-w,area.height-h),((RectTransform)transform).pivot-new Vector2(.5f,.5f));
            mask.anchoredPosition=new Vector2(-w/2+w*region.x,-h/2+h*region.y)+inset;mask.sizeDelta=new Vector2(w*region.width,h*region.height);
            var rect=overlay.rectTransform;rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=Vector2.zero;
            rect.anchoredPosition=new Vector2(-w*region.x,-h*region.y);rect.sizeDelta=new Vector2(w,h);
        }
        private void Update()
        {
            IsSpeaking=false;
            if(!portrait.enabled){eyes.enabled=mouth.enabled=false;return;}
            if(speech!=null)
            {
                if(!externalSpeech)speech.maxVisibleCharacters=Owner!=null&&Owner.ReducedMotion?int.MaxValue:
                    Mathf.Min(characters,Mathf.FloorToInt((Time.unscaledTime-speechAt)*34));
                IsSpeaking=speech.maxVisibleCharacters<characters;
            }
            var frames=Frames;if(frames==null){eyes.enabled=mouth.enabled=false;EyePhase=0;return;}
            FitOverlay(eyeMask,eyes,frames.EyeRegion);FitOverlay(mouthMask,mouth,frames.MouthRegion);
            if(blinkStarted<0&&Time.unscaledTime>=blinkAt)Blink();
            if(blinkStarted>=0)
            {
                float t=Time.unscaledTime-blinkStarted;
                if(t>=.15f){eyes.enabled=false;EyePhase=0;blinkStarted=-1;if(doubleBlink){doubleBlink=false;followUpBlink=true;blinkAt=Time.unscaledTime+.12f;}else ScheduleBlink();}
                else {EyePhase=t<.04f||t>=.11f?1:2;eyes.sprite=EyePhase==1?frames.EyesHalf:frames.EyesClosed;eyes.enabled=eyes.sprite!=null;}
            }
            bool voiced=Owner!=null&&Owner.PortraitVoicePlaying;
            bool punctuation=speech!=null&&speech.maxVisibleCharacters>0&&speech.maxVisibleCharacters<=characters&&
                "、。！？!?…\n".IndexOf(speech.textInfo.characterInfo[speech.maxVisibleCharacters-1].character)>=0;
            if(!voiced&&(!IsSpeaking||punctuation))mouthOpen=false;
            else if(Time.unscaledTime>=mouthAt){mouthAt=Time.unscaledTime+.08f+(float)random.NextDouble()*.04f;mouthOpen=voiced?Owner.PortraitVoiceLevel>.012f:!mouthOpen;}
            mouth.sprite=mouthOpen?(voiced&&Owner.PortraitVoiceLevel<.04f&&frames.MouthMid!=null?frames.MouthMid:frames.MouthOpen):frames.MouthClosed;
            mouth.enabled=mouth.sprite!=null;
        }
        private void OnDisable(){if(speech!=null&&!externalSpeech)speech.maxVisibleCharacters=int.MaxValue;}
    }
}
