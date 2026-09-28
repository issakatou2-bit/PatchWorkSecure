using TMPro;
using UnityEngine;

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
        private OpsReactionBank ReactionBank => Navigator == null ? null : Navigator.Reactions;

        private void React(OpsCue cue)
        {
            if (cue == OpsCue.Click || !Application.isPlaying) return;
            OpsReaction reaction = cue == OpsCue.Alert ? OpsReaction.Alert : cue == OpsCue.Damage ? OpsReaction.Recover :
                cue == OpsCue.Growth ? OpsReaction.Growth : cue == OpsCue.Purchase ? OpsReaction.Purchase : cue == OpsCue.Success ? OpsReaction.Success :
                cue == OpsCue.Month ? OpsReaction.Month : cue == OpsCue.Clear ? OpsReaction.Clear : cue == OpsCue.Failure ? OpsReaction.Failure : OpsReaction.Think;
            if (voiceAudio != null && voiceAudio.isPlaying && OpsReactionDirector.Priority(reaction) <= speakingPriority) return;
            if (reactionDirector == null) reactionDirector = new OpsReactionDirector(System.Environment.TickCount);
            if (!reactionDirector.TryChoose(ReactionBank, reaction, Time.unscaledTimeAsDouble, out var line)) return;
            LastReactionId = line.id;
            LastReactionCaption = line.caption;
            // 短い反応と字幕を一致させる。判断の根拠・数値は別の表示に残す。
            var speech = screen == null ? null : screen.GetComponentsInChildren<TextMeshProUGUI>();
            if (speech != null) foreach (var label in speech)
                if (label.name == "NavigatorSpeech" || label.name == "VoicePreviewCaption")
                {
                    if(!CaptionsEnabled){if(label.name=="VoicePreviewCaption")label.text="字幕はOFF";continue;}
                    label.text = line.caption; PortraitSpeech(label);
                }
            ApplyReactionFace(reaction);
            if (!VoiceEnabled || muted || voiceVolume <= 0 || line.clip == null) return;
            if (voiceAudio == null) voiceAudio = NewAudioSource();
            voiceAudio.Stop(); voiceAudio.clip = line.clip; voiceAudio.volume = voiceVolume;
            speakingPriority = OpsReactionDirector.Priority(reaction); voiceAudio.Play();
        }

        private void ApplyReactionFace(OpsReaction reaction)
        {
            if (Navigator == null || screen == null) return;
            Sprite face = reaction == OpsReaction.Alert ? Navigator.FaceAlert :
                reaction == OpsReaction.Success ? Navigator.FaceRelieved : reaction == OpsReaction.Growth || reaction == OpsReaction.Clear ? Navigator.FaceProud :
                reaction == OpsReaction.Recover || reaction == OpsReaction.Failure ? Navigator.FaceSad : Navigator.FaceNormal;
            foreach (var image in screen.GetComponentsInChildren<UnityEngine.UI.Image>())
                if (image.name == "NavigatorPortrait") image.sprite = face != null ? face : Navigator.FaceNormal;
            string expression=reaction==OpsReaction.Alert?"alert":reaction==OpsReaction.Success?"relieved":reaction==OpsReaction.Growth||reaction==OpsReaction.Clear?"proud":reaction==OpsReaction.Recover||reaction==OpsReaction.Failure?"sad":"normal";
            foreach(var animator in screen.GetComponentsInChildren<OpsPortraitAnimator>())animator.SetExpression(expression);
        }

        private void StopVoice() { if (voiceAudio != null) voiceAudio.Stop(); }
    }
}
