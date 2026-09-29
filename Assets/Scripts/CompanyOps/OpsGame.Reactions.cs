using TMPro;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public bool VoiceEnabled { get; private set; } = true;
        private float voiceVolume = .7f;
        private AudioSource voiceAudio;
        private OpsReactionDirector reactionDirector;
        private int speakingPriority;
        private readonly float[] portraitSamples=new float[64];
        private int portraitSampleFrame=-1;
        private float portraitLevel;
        public bool PortraitVoicePlaying => voiceAudio!=null && voiceAudio.isPlaying && !muted && VoiceEnabled && voiceVolume>0;
        public float PortraitVoiceLevel
        {
            get
            {
                if(!PortraitVoicePlaying)return 0;
                if(portraitSampleFrame!=Time.frameCount){portraitSampleFrame=Time.frameCount;voiceAudio.GetOutputData(portraitSamples,0);float sum=0;foreach(float v in portraitSamples)sum+=v*v;portraitLevel=Mathf.Sqrt(sum/portraitSamples.Length);}
                return portraitLevel;
            }
        }
        private void PortraitSpeech(TMPro.TextMeshProUGUI label)
        {
            if(screen==null)return;
            foreach(var animator in screen.GetComponentsInChildren<OpsPortraitAnimator>())animator.Speak(label);
        }
        public string LastReactionId { get; private set; } = "";
        public string LastReactionCaption { get; private set; } = "";
        public bool UseLocalTestVoices {get;set;}=true;
        private OpsReactionBank localVoiceBank;
        private bool localVoiceChecked;
        private OpsReactionLine pendingVoice;
        private float voiceStartAt,voiceBusyUntil;
        private string voiceScreenKey="",lastTutorialVoice="",voiceCaptionTarget="NavigatorSpeech";
        private bool rankVoicePending;
        private bool carryResolutionVoice;
        private readonly Queue<string> followingVoice=new Queue<string>();
        public bool VoicePending => pendingVoice!=null;
        public OpsReactionBank ActiveVoiceBank => ReactionBank;
        private OpsReactionBank ReactionBank
        {
            get
            {
                var metadata=Navigator==null?null:Navigator.Reactions;
                if(!UseLocalTestVoices||metadata==null||metadata.HasAudio||metadata.name!="HinataReactions")return metadata;
                if(!localVoiceChecked){localVoiceChecked=true;localVoiceBank=Resources.Load<OpsReactionBank>("HinataVoiceTest");}
                return localVoiceBank!=null?localVoiceBank:metadata;
            }
        }

        private void React(OpsCue cue)
        {
            if (cue == OpsCue.Click || !Application.isPlaying) return;
            OpsReaction reaction = cue == OpsCue.Alert ? OpsReaction.Alert : cue == OpsCue.Damage ? OpsReaction.Recover :
                cue == OpsCue.Growth ? OpsReaction.Growth : cue == OpsCue.Purchase ? OpsReaction.Purchase : cue == OpsCue.Success ? OpsReaction.Success :
                cue == OpsCue.Month ? OpsReaction.Month : cue == OpsCue.Clear ? OpsReaction.Clear : cue == OpsCue.Failure ? OpsReaction.Failure : OpsReaction.Think;
            if ((VoicePending||PortraitVoicePlaying||Time.unscaledTime<voiceBusyUntil) && OpsReactionDirector.Priority(reaction) <= speakingPriority) return;
            if (reactionDirector == null) reactionDirector = new OpsReactionDirector(System.Environment.TickCount);
            bool preview=screen.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.name=="VoicePreviewCaption");
            if (!reactionDirector.TryChoose(ReactionBank, reaction, Time.unscaledTimeAsDouble, out var line,preview)) return;
            // 現在の数値で疲労を確認できる場合だけ追加の反応を使う。他の追加台詞は登録のみ。
            if(cue==OpsCue.Action&&State!=null&&State.phase==OpsPhase.Planning&&State.fatigue>=70&&LastReactionId!="extra_sleepy")
                line=ReactionBank.Find("extra_sleepy")??line;
            StopVoice();BeginVoice(line,.16f,OpsReactionDirector.Priority(reaction),preview?"VoicePreviewCaption":"NavigatorSpeech");
        }

        public bool SpeakSceneLine(string id,float delay=.2f,string target="NavigatorSpeech")
        {
            // 全文は同じ場面が再発したら読み直す。掛け声の連続防止はDirector側で行う。
            var line=ReactionBank?.Find(id);if(line==null)return false;
            StopVoice();BeginVoice(line,delay,5,target);return true;
        }
        private void BeginVoice(OpsReactionLine line,float delay,int priority,string target)
        {
            LastReactionId=line.id;LastReactionCaption=line.caption;voiceCaptionTarget=homeVisible&&target=="NavigatorSpeech"?"TitleCaption":target;speakingPriority=priority;
            ApplyVoiceCaption();ApplyReactionFace(line.reaction,line);
            pendingVoice=line;voiceStartAt=Time.unscaledTime+Mathf.Max(0,delay);
            // 音声なしでも字幕を読める。操作や次の画面への進行は待たせない。
            voiceBusyUntil=voiceStartAt+(line.clip!=null&&VoiceEnabled&&!muted&&voiceVolume>0?line.clip.length:2.5f);
        }
        private void ApplyVoiceCaption()
        {
            if(screen==null||LastReactionId=="")return;
            foreach(var label in screen.GetComponentsInChildren<TextMeshProUGUI>())if(label.name==voiceCaptionTarget)
            {
                label.enabled=true;label.text=CaptionsEnabled?(label.name=="VoicePreviewCaption"?LastReactionCaption:label.name=="TitleCaption"?LastReactionCaption.Replace("\r","").Replace("\n"," "):SpeechLines(LastReactionCaption)):label.name=="VoicePreviewCaption"?"字幕はOFF":"";
                if(label.name=="ResolutionReaction")label.fontSizeMin=12;
                // 字幕に数値通知を重ねない。成果の数値はHUD・月報・発動内訳に残る。
                if(label==toastSpeech&&toast!=null)toast.gameObject.SetActive(false);
                PortraitSpeech(label);
            }
        }
        private void TickVoice()
        {
            if(pendingVoice!=null&&Time.unscaledTime>=voiceStartAt)
            {
                var line=pendingVoice;pendingVoice=null;
                if(VoiceEnabled&&!muted&&voiceVolume>0&&line.clip!=null)
                {
                    if(voiceAudio==null)voiceAudio=NewAudioSource();voiceAudio.Stop();voiceAudio.clip=line.clip;voiceAudio.volume=voiceVolume;voiceAudio.Play();
                }
            }
            if(pendingVoice==null&&!PortraitVoicePlaying&&Time.unscaledTime>=voiceBusyUntil&&followingVoice.Count>0)
            {
                var line=ReactionBank?.Find(followingVoice.Dequeue());if(line!=null)BeginVoice(line,.18f,5,ResolutionActive?"ResolutionReaction":"NavigatorSpeech");
            }
        }
        private void ScreenVoice()
        {
            if(!Application.isPlaying||State==null||ResolutionActive)return;
            string key=State.seed+":"+State.month+":"+State.phase;
            if(key==voiceScreenKey)return;voiceScreenKey=key;
            if(State.phase==OpsPhase.Planning)SpeakSceneLine("season_"+(((State.month+3)%12)+1).ToString("00"),1.65f);
            else if(State.phase==OpsPhase.Incident)
            {
                SpeakSceneLine("incident_start",.9f);followingVoice.Enqueue("incident_unconfirmed");
            }
            else if(State.phase==OpsPhase.Review)
            {
                string id=State.CurrentMissionCompleted?"mission_done":"mission_miss";
                if(carryResolutionVoice){carryResolutionVoice=false;voiceCaptionTarget="NavigatorSpeech";ApplyVoiceCaption();ApplyReactionFace(OpsReaction.Think,ReactionBank.Find(LastReactionId));followingVoice.Enqueue(id);}
                else SpeakSceneLine(id,.65f);
            }
            else if(State.phase==OpsPhase.Ended)
            {
                string rank=State.Rank.Substring(State.Rank.Length-1);SpeakSceneLine("annual_"+rank.ToLowerInvariant(),State.history.Count*.08f+.95f);
            }
        }
        private void TutorialVoice()
        {
            string id="tutorial_"+(tutorialStep+1);if(tutorialAwaitClose)return;
            if(id==lastTutorialVoice)
            {
                var label=tutorialRoot.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(t=>t.name=="TutorialLine");
                if(label!=null)label.text=CaptionsEnabled?SpeechLines(ReactionBank?.Find(id)?.caption??label.text):"";
                return;
            }
            lastTutorialVoice=id;SpeakSceneLine(id,.2f,"TutorialLine");
        }
        private void ResolutionVoice(string id)
        {
            if(speakingPriority==5&&(VoicePending||PortraitVoicePlaying||Time.unscaledTime<voiceBusyUntil))followingVoice.Enqueue(id);
            else SpeakSceneLine(id,.18f,"ResolutionReaction");
        }
        private void SpeakRankUp()
        {
            if(!rankVoicePending)return;rankVoicePending=false;
            if(State.phase==OpsPhase.Planning)SpeakSceneLine("rankup",.3f);
        }
        public static string MissionVoiceId(OpsState state)
        {
            if(state.CurrentProfile==null)return "mission_accept_"+(state.month+1).ToString("00");
            // 抽選された題材を優先し、暦だけで別の依頼の声を当てない。
            string profile=state.CurrentProfile.id;
            int n=profile=="ransom"?1:profile=="storage"?10:profile=="vulnerability"||profile=="change"?6:
                profile=="supply"?7:profile=="remote"||profile=="session"?11:
                profile=="ai"||profile=="sharing"||profile=="insider"?8:profile=="bec"?3:
                profile=="ddos"||profile=="service"?4:3;
            return "mission_accept_"+n.ToString("00");
        }

        private void ApplyReactionFace(OpsReaction reaction,OpsReactionLine line)
        {
            if (Navigator == null || screen == null) return;
            foreach(var identity in screen.GetComponentsInChildren<OpsPortraitIdentity>())
                if(identity.FaceIcon)identity.GetComponent<UnityEngine.UI.Image>().sprite=Navigator.Face(string.IsNullOrEmpty(line.faceId)?"face_normal":line.faceId);
                else if(!string.IsNullOrEmpty(line.poseId))identity.GetComponent<OpsPortraitAnimator>()?.ChangePose(line.poseId);
            string expression=string.IsNullOrEmpty(line.faceId)?"normal":line.faceId.Substring("face_".Length);
            foreach(var animator in screen.GetComponentsInChildren<OpsPortraitAnimator>())animator.SetExpression(expression);
            if(line.faceId=="face_pout")foreach(var motion in screen.GetComponentsInChildren<OpsPortraitMotion>())motion.ShowEmotion(3);
        }

        public void StopVoice()
        {
            if(voiceAudio!=null)voiceAudio.Stop();pendingVoice=null;followingVoice.Clear();voiceBusyUntil=0;speakingPriority=0;carryResolutionVoice=false;
        }
    }
}
