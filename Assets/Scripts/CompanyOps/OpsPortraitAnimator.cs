using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // 同じキャンバスの透過差分を重ねる。差分無しでは元の絵を変形・描き直ししない。
    public sealed class OpsPortraitAnimator : MonoBehaviour
    {
        public OpsGame Owner;
        private Image eyes,mouth;
        private System.Random random;
        private float blinkAt,blinkStarted=-1,mouthAt;
        private bool mouthOpen;
        private string expression="normal";
        private TextMeshProUGUI speech;
        private float speechAt;
        private int characters;
        public bool HasFrames => Frames != null && (Frames.EyesClosed!=null || Frames.MouthOpen!=null);
        private NavigatorPersona.FaceAnimationFrames Frames => Owner?.Navigator?.AnimationFrames?.FirstOrDefault(f=>f!=null && f.Expression==expression);
        private void Awake()
        {
            random=new System.Random(unchecked(Environment.TickCount^System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)));
            eyes=Overlay("HinataEyes");mouth=Overlay("HinataMouth");ScheduleBlink();
        }
        private Image Overlay(string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);
            var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var image=go.GetComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;image.enabled=false;return image;
        }
        public void SetExpression(string value){expression=value;eyes.enabled=mouth.enabled=false;blinkStarted=-1;ScheduleBlink();}
        public void Speak(TextMeshProUGUI label)
        {
            if(speech!=null)speech.maxVisibleCharacters=int.MaxValue;
            speech=label;speechAt=Time.unscaledTime;speech.ForceMeshUpdate();characters=speech.textInfo.characterCount;
            speech.maxVisibleCharacters=Owner!=null && !Owner.ReducedMotion?0:int.MaxValue;
        }
        private void ScheduleBlink(){blinkAt=Time.unscaledTime+3+(float)random.NextDouble()*2;}
        private void Update()
        {
            if(Owner==null)return;
            bool typing=false;
            if(speech!=null)
            {
                int visible=Mathf.FloorToInt((Time.unscaledTime-speechAt)*34);typing=visible<characters;
                speech.maxVisibleCharacters=Owner.ReducedMotion?int.MaxValue:Mathf.Min(characters,visible);
            }
            var frames=Frames;
            if(Owner.ReducedMotion || frames==null){eyes.enabled=mouth.enabled=false;return;}
            if(frames.EyesClosed!=null && blinkStarted<0 && Time.unscaledTime>=blinkAt)blinkStarted=Time.unscaledTime;
            if(blinkStarted>=0)
            {
                float t=Time.unscaledTime-blinkStarted;
                if(t>=.15f){eyes.enabled=false;blinkStarted=-1;ScheduleBlink();}
                else {eyes.sprite=t<.04f || t>=.11f?frames.EyesHalf:frames.EyesClosed;eyes.enabled=eyes.sprite!=null;}
            }
            bool voiced=Owner.PortraitVoicePlaying;
            if(Time.unscaledTime>=mouthAt)
            {
                mouthAt=Time.unscaledTime+.08f+(float)random.NextDouble()*.04f;
                mouthOpen=voiced?Owner.PortraitVoiceLevel>.012f:typing&&!mouthOpen;
                mouth.sprite=voiced && Owner.PortraitVoiceLevel<.04f && frames.MouthMid!=null?frames.MouthMid:frames.MouthOpen;
            }
            mouth.enabled=(voiced || typing) && mouthOpen && mouth.sprite!=null;
        }
        private void OnDisable(){if(speech!=null)speech.maxVisibleCharacters=int.MaxValue;}
    }
}
