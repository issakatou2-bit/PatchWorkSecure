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
                if (label.name == "NavigatorSpeech" || label.name == "VoicePreviewCaption") label.text = line.caption;
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
        }

        private void StopVoice() { if (voiceAudio != null) voiceAudio.Stop(); }
    }
}
