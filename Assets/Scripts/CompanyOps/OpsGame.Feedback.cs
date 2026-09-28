using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public OpsSoundPalette Sounds;
        public bool ReducedMotion { get; private set; }
        public OpsCue LastCue { get; private set; }
        private bool muted;
        private float soundVolume = .6f, musicVolume = .4f, lastClick = -1;
        private AudioSource buttonAudio, eventAudio, musicA, musicB;
        private AudioSource countAudio, stampAudio, transitionAudio;
        private float lastCount=-1, lastStamp=-1;
        private AudioClip targetMusic;
        private float musicBlend, previousMusicVolume;
        private readonly Dictionary<OpsCue, AudioClip> generatedSounds = new Dictionary<OpsCue, AudioClip>();
        private bool homeVisible;

        private void LoadFeedbackSettings()
        {
            LoadDisplaySettings();
            if (TestMode) return;
            muted = PlayerPrefs.GetInt("pws_ops_mute", 0) != 0;
            ReducedMotion = PlayerPrefs.GetInt("pws_ops_reduce_motion", 0) != 0;
            soundVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("pws_ops_sfx", .6f));
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("pws_ops_music", .4f));
            VoiceEnabled = PlayerPrefs.GetInt("pws_ops_voice_enabled", 1) != 0;
            voiceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("pws_ops_voice_volume", .7f));
        }
        private void StoreFeedbackSettings()
        {
            if (TestMode) return;
            PlayerPrefs.SetInt("pws_ops_mute", muted ? 1 : 0);
            PlayerPrefs.SetInt("pws_ops_reduce_motion", ReducedMotion ? 1 : 0);
            PlayerPrefs.SetFloat("pws_ops_sfx", soundVolume); PlayerPrefs.SetFloat("pws_ops_music", musicVolume);
            PlayerPrefs.SetInt("pws_ops_voice_enabled", VoiceEnabled ? 1 : 0);
            PlayerPrefs.SetFloat("pws_ops_voice_volume", voiceVolume);
            PlayerPrefs.Save();
        }
        private AudioSource NewAudioSource()
        {
            var source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.spatialBlend = 0; source.volume = 0; source.mute = TestMode; return source;
        }
        private void PlayCue(OpsCue cue)
        {
            LastCue = cue;
            if (!Application.isPlaying || muted || soundVolume <= 0) return;
            if (cue == OpsCue.Click && Time.unscaledTime - lastClick < .04f) return;
            if (cue == OpsCue.Click) lastClick = Time.unscaledTime;
            var clip = Sounds == null ? null : Sounds.Clip(cue);
            if (clip == null && !generatedSounds.TryGetValue(cue, out clip))
            {
                var data = OpsSoundDesign.Samples(cue);
                clip = AudioClip.Create("試作効果音_" + cue, data.Length, 1, OpsSoundDesign.SampleRate, false);
                clip.SetData(data, 0); generatedSounds.Add(cue, clip);
            }
            if (buttonAudio == null) buttonAudio = NewAudioSource();
            if (eventAudio == null) eventAudio = NewAudioSource();
            var source = cue == OpsCue.Click ? buttonAudio : eventAudio;
            // 連打で同じ音が積み重ならない。操作と結果だけを別々に再生する。
            source.Stop(); source.clip = clip; source.volume = soundVolume * (cue == OpsCue.Click ? .65f : 1);
            source.pitch=VariedPitch();source.Play();
        }
        private void PlayPresentationCue(OpsCue cue)
        {
            if(!Application.isPlaying||muted||soundVolume<=0)return;
            // 同じフレームの複数の数値・印は一音にまとめ、成功音や発動音を止めない。
            if(cue==OpsCue.Count && Time.unscaledTime-lastCount<.08f)return;
            if(cue==OpsCue.Stamp && Time.unscaledTime-lastStamp<.08f)return;
            var clip=Sounds==null?null:Sounds.Clip(cue);if(clip==null)return;
            // 月替わりの結果音と共通遷移は同じWAV。同時の二重再生を避ける。
            if(cue==OpsCue.Transition&&eventAudio!=null&&eventAudio.clip==clip&&eventAudio.isPlaying)return;
            AudioSource source;
            if(cue==OpsCue.Count){lastCount=Time.unscaledTime;if(countAudio==null)countAudio=NewAudioSource();source=countAudio;}
            else if(cue==OpsCue.Stamp){lastStamp=Time.unscaledTime;if(stampAudio==null)stampAudio=NewAudioSource();source=stampAudio;}
            else {if(transitionAudio==null)transitionAudio=NewAudioSource();source=transitionAudio;}
            source.Stop();source.clip=clip;source.volume=soundVolume*.65f;source.pitch=VariedPitch();source.Play();
        }
        private void SetPresentationVolume(float volume)
        {
            foreach(var source in new[]{countAudio,stampAudio,transitionAudio})
                if(source!=null){source.volume=volume*.65f;if(volume<=0)source.Stop();}
        }
        private void StopPresentationSounds()
        {
            foreach(var source in new[]{countAudio,stampAudio,transitionAudio})if(source!=null)source.Stop();
        }
        private void SetMusic(AudioClip clip)
        {
            if (!Application.isPlaying || targetMusic == clip) return;
            targetMusic = clip;
            if (musicA == null) { musicA = NewAudioSource(); musicA.loop = true; }
            if (musicB == null) { musicB = NewAudioSource(); musicB.loop = true; }
            // 現在の曲を余韻側へ。新しい曲は無音から約0.8秒で切り替える。
            var old = musicB; musicB = musicA; musicA = old;
            musicBlend = 0; previousMusicVolume = musicB.volume;
            musicA.Stop(); musicA.clip = clip; musicA.volume = 0;
            if (clip != null) musicA.Play();
        }
        private void TickMusic()
        {
            if (musicA == null || musicB == null) return;
            musicBlend = Mathf.Clamp01(musicBlend + Time.unscaledDeltaTime / .8f);
            float wanted=voiceAudio!=null&&voiceAudio.isPlaying||Time.unscaledTime<duckUntil?.55f:1;
            duckLevel=Mathf.MoveTowards(duckLevel,wanted,Time.unscaledDeltaTime/(wanted<duckLevel?.08f:.3f));float duck=duckLevel;
            musicA.volume = muted ? 0 : musicVolume * .45f * musicBlend * duck;
            musicB.volume = muted ? 0 : previousMusicVolume * (1 - musicBlend) * duck;
            if (musicB.volume <= 0 && musicB.isPlaying) musicB.Stop();
        }
        private void Feedback(OpsCue cue)
        {
            PlayCue(cue);
            React(cue);
            if (!Application.isPlaying || screen == null) return;
            if (State != null && State.phase == OpsPhase.Review && State.Latest?.power?.staff > 0)
            {
                var support = screen.Find("OfficeStage/OutcomeSupport") as RectTransform;
                if (support != null) StartCoroutine(InstallationPulse(support));
            }
            StartCoroutine(FeedbackRoutine(cue));
        }
        private IEnumerator FeedbackRoutine(OpsCue cue)
        {
            bool warning = cue == OpsCue.Alert || cue == OpsCue.Damage || cue == OpsCue.Failure;
            bool celebration = cue == OpsCue.Growth || cue == OpsCue.Clear;
            Color color = warning ? Coral : celebration ? Accent : Mint;
            var fx = Rect(screen, "ResultEffects", 0, 0, 1600, 900);
            var group = fx.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            bool incident = State != null && State.phase == OpsPhase.Incident;
            bool planning = State != null && State.phase == OpsPhase.Planning;
            var stripe = Box(fx, "ResultAccent", planning ? 600 : incident ? 656 : 980, planning ? 709 : 121, planning ? 420 : incident ? 918 : 594, 5, color);
            stripe.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var panel = screen.Find("DecisionPanel") as RectTransform;
            Vector2 origin = panel == null ? Vector2.zero : panel.anchoredPosition;
            var changed = new List<RectTransform>();
            var deltaTags = new List<RectTransform>();
            var deltaOrigins = new List<Vector2>();
            var hiddenDeltas = new List<TMPro.TextMeshProUGUI>();
            for (int i = 0; i < statChanges.Length; i++)
            {
                if (statChanges[i] == 0) continue;
                var card = screen.GetComponentsInChildren<RectTransform>().FirstOrDefault(r => r.name == "Stat_" + i);
                if (card != null) changed.Add(card.Find(StatNames[i] + "Value") as RectTransform);
                if (card == null) continue;
                int delta = statChanges[i];
                var small = card.Find("StatDelta" + i)?.GetComponent<TMPro.TextMeshProUGUI>();
                if (small != null) { small.alpha = 0; hiddenDeltas.Add(small); }
                // 差分はカード内の同じ場所で一度だけ。下端の補助文を覆わない。
                Vector3 position = screen.InverseTransformPoint(card.TransformPoint(new Vector3(card.rect.width-65,-8,0)));
                var tag = Rect(fx, "StatChangeEffect" + i, position.x, -position.y, 60, 25);
                var text = Text(tag, "StatChangeAmount" + i, (delta > 0 ? "+" : "") + delta + (i == 0 ? "万円" : i == 1 ? "工数" : ""),
                    0, 0, 60, 25, 14, DeltaColor(i, delta));
                text.alignment = TMPro.TextAlignmentOptions.Center;
                deltaTags.Add(tag); deltaOrigins.Add(tag.anchoredPosition);
            }
            var sparks = new List<RectTransform>();
            // 年度結果では粒子を出さず、ランクと実数値を読みやすくする。
            if (!ReducedMotion && celebration && State != null && State.phase != OpsPhase.Ended)
            {
                for (int i = 0; i < 12; i++)
                {
                    var spark = Box(fx, "AchievementSpark" + i, 616, 492, 5, 9, i % 2 == 0 ? Accent : Mint);
                    spark.GetComponent<UnityEngine.UI.Image>().raycastTarget = false; sparks.Add(spark);
                }
            }
            float elapsed = 0, duration = celebration ? .9f : .65f;
            while (elapsed < duration && fx != null)
            {
                float t = elapsed / duration;
                group.alpha = 1 - t * t;
                if (panel != null && cue == OpsCue.Damage && !ReducedMotion && elapsed < .25f)
                    panel.anchoredPosition = origin + Vector2.right * (Mathf.Sin(elapsed * 75) * 4 * (1 - elapsed / .25f));
                else if (panel != null) panel.anchoredPosition = origin;
                foreach (var value in changed) if (value != null)
                    value.localScale = Vector3.one * (ReducedMotion ? 1 : 1 + Mathf.Sin(t * Mathf.PI) * .08f);
                for (int i = 0; i < deltaTags.Count; i++)
                    deltaTags[i].anchoredPosition = deltaOrigins[i] + Vector2.up * (ReducedMotion ? 0 : t * 4);
                for (int i = 0; i < sparks.Count; i++)
                {
                    float angle = i * Mathf.PI * 2 / sparks.Count;
                    sparks[i].anchoredPosition = new Vector2(616 + Mathf.Cos(angle) * t * 185, -492 + Mathf.Sin(angle) * t * 95 - t * t * 48);
                    sparks[i].localEulerAngles = new Vector3(0, 0, i * 30 + t * 100);
                }
                elapsed += Time.unscaledDeltaTime; yield return null;
            }
            if (panel != null) panel.anchoredPosition = origin;
            foreach (var value in changed) if (value != null) value.localScale = Vector3.one;
            foreach (var value in hiddenDeltas) if (value != null) value.alpha = 1;
            if (fx != null) Destroy(fx.gameObject);
        }
        private void OnDestroy()
        {
            foreach (var clip in generatedSounds.Values) if (clip != null) Destroy(clip);
        }
    }
}
