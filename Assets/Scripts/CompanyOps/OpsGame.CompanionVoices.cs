using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public OpsReactionBank CompanionVoices;
        private bool carryCompanionVoice;
        private string companionSceneKey="";
        private OpsReactionLine FindVoiceLine(string id)
        {
            if(id=="maxim_hurry"||id=="maxim_link"||id=="maxim_segment"||id=="maxim_report")id+="_v2";
            if(id=="diary_y1_10"||id=="diary_y1_10_v2")id="diary_y1_10_v3";
            if(id=="diary_y3_06")id="diary_y3_06_v2";
            if(id=="diary_y3_07")id="diary_y3_07_v2";
            if(id=="final_restore_bridge")return new OpsReactionLine{id=id,caption="戻す前に、確かめる、だよ！",reaction=OpsReaction.Think};
            if(CompanionVoices==null)CompanionVoices=Resources.Load<OpsReactionBank>("CompanionVoices");
            return CompanionVoices?.Find(id)??ReactionBank?.Find(id);
        }
        // 合いの手も含め、全24本を実際の場面から呼ぶ。状態・乱数・報酬は変更しない。
        public static string[] CompanionSceneLines(string scene)
        {
            switch(scene)
            {
                case "opening":return new[]{"kanon_opening","eng_opening"};
                case "budget":return new[]{"kanon_peak_budget","kanon_react_ok"};
                case "investigate":return new[]{"eng_investigate","eng_react_hmm","eng_found","eng_react_ok"};
                case "rival_bec":return new[]{"kanon_react_hmm","kanon_rival"};
                case "rival":return new[]{"eng_rival_reveal"};
                case "factor_kanon":return new[]{"kanon_factor","kanon_react_laugh"};
                case "factor_engineer":return new[]{"eng_factor"};
                case "year_clear":return new[]{"kanon_year_clear","eng_year_clear"};
                case "year_fail":return new[]{"kanon_year_fail","eng_year_fail"};
                case "ending":return new[]{"kanon_ending_ss","eng_ending_ss"};
                case "meeting":return new[]{"kanon_meeting","eng_forgot","kanon_tease","eng_react_yawn"};
                default:return Array.Empty<string>();
            }
        }
        private void QueueCompanionScene(string scene,string target="NavigatorSpeech",bool once=false)
        {
            string key=(State?.seed??0)+":"+RunYear+":"+(State?.month??0)+":"+scene;
            if(once&&companionSceneKey==key)return;
            companionSceneKey=key;
            var lines=CompanionSceneLines(scene);
            bool busy=VoicePending||VoicePlaying||Time.unscaledTime<voiceBusyUntil;
            if(!busy)voiceCaptionTarget=target;
            foreach(string id in lines)
            {
                if(!busy){if(SpeakSceneLine(id,.3f,target))busy=true;}
                else followingVoice.Enqueue(id);
            }
        }
        private void EnsureCompanionCaption()
        {
            if(screen.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.name==voiceCaptionTarget))return;
            var story=screen.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(t=>t.name=="StorySpeechText");
            if(story!=null){voiceCaptionTarget=story.name;return;}
            if(screen.Find("CompanionSpeech")==null)
            {
                bool factors=screen.Find("FactorCategory")!=null;
                var bubble=PCard(screen,"CompanionSpeech",300,factors?739:836,1060,factors?42:52,Color.white,20);
                if(factors)
                {
                    var tag=PCard(bubble,"CompanionNameTag",10,8,130,26,PlanPink,12,false);
                    PText(tag,"CompanionName","",0,0,130,26,14,Color.white,true,true);
                }
                else SpeechName(bubble,"CompanionName");
                var label=PText(bubble,"CompanionCaption","",factors?156:18,4,factors?886:1024,factors?34:44,18,PlanInk,false);
                label.textWrappingMode=TextWrappingModes.Normal;
            }
            voiceCaptionTarget="CompanionCaption";
        }
        private void UpdateSpeakerBadge(TextMeshProUGUI label)
        {
            UpdateMeetingPhoto();
            var marker=label.GetComponent<OpsVoiceSpeakerBadge>();
            if(marker==null)
            {
                marker=label.gameObject.AddComponent<OpsVoiceSpeakerBadge>();
                marker.NameLabel=screen.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(t=>
                    t.name=="NavigatorName"||t.name=="StorySpeechName"||t.name=="CompanionName"||t.name=="ResolutionName");
                if(marker.NameLabel!=null)
                {
                    marker.OriginalName=marker.NameLabel.text;
                    marker.NameSize=marker.NameLabel.rectTransform.sizeDelta;
                    marker.TagSize=((RectTransform)marker.NameLabel.transform.parent).sizeDelta;
                }
            }
            bool companion=CurrentSpeaker!="";
            if(marker.NameLabel!=null)
            {
                marker.NameLabel.text=companion?CurrentSpeaker:marker.OriginalName;
                marker.NameLabel.rectTransform.sizeDelta=companion?new Vector2(130,marker.NameSize.y):marker.NameSize;
                ((RectTransform)marker.NameLabel.transform.parent).sizeDelta=companion?new Vector2(130,marker.TagSize.y):marker.TagSize;
            }
            else if(companion&&CaptionsEnabled)label.text=CurrentSpeaker+"："+label.text;
            if(companion&&marker.Badge==null)
            {
                var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);
                Vector3 point=screen.InverseTransformPoint(corners[1]);
                float faceX=label.name=="CompanionCaption"?210:point.x<106?point.x+label.rectTransform.rect.width+16:point.x-106;
                var badge=Rect(screen,"CompanionFaceBadge",Mathf.Clamp(faceX,8,1516),Mathf.Min(820,-point.y),76,76);
                badge.gameObject.AddComponent<OpsVoiceCircle>().raycastTarget=false;
                badge.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=true;
                var raw=Rect(badge,"CompanionFace",2,2,72,72).gameObject.AddComponent<UnityEngine.UI.RawImage>();raw.raycastTarget=false;
                marker.Badge=badge;marker.Face=raw;
            }
            if(marker.Badge!=null)marker.Badge.gameObject.SetActive(companion);
            if(companion&&marker.Face!=null)
            {
                bool engineer=CurrentSpeaker=="りりぃ";
                Sprite art=engineer?PlanningArt.focusEngineer:PlanningArt.focusSecretary;
                marker.Face.texture=art==null?null:art.texture;
                marker.Face.uvRect=engineer?new Rect(.484f,.57f,.23f,.41f):new Rect(.22f,.60f,.235f,.418f);
                label.textWrappingMode=TextWrappingModes.Normal;
            }
        }
    }
    public sealed class OpsVoiceSpeakerBadge:MonoBehaviour
    {
        public TextMeshProUGUI NameLabel;
        public string OriginalName;
        public Vector2 NameSize,TagSize;
        public RectTransform Badge;
        public UnityEngine.UI.RawImage Face;
        private void OnDestroy(){if(Badge!=null)Destroy(Badge.gameObject);}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OpsVoiceCircle:UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;mesh.AddVert(r.center,color,Vector2.zero);
            for(int i=0;i<=64;i++){float a=i*Mathf.PI*2/64;mesh.AddVert(r.center+new Vector2(Mathf.Cos(a)*r.width/2,Mathf.Sin(a)*r.height/2),color,Vector2.zero);if(i>0)mesh.AddTriangle(0,i,i+1);}
        }
    }
}
