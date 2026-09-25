using UnityEngine;
using System.Collections.Generic;

namespace PatchWorkSecure
{
    /// <summary>
    /// BGM/SEの再生を一括管理する。AudioClipが未設定でも例外にならず単に無音になるため、
    /// 素材が揃う前から呼び出しだけ組み込んでおける。
    /// 素材が届いたらInspectorの各クリップにドラッグするだけで音が鳴るようになる。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string MuteKey = "pws_audio_muted";

        [Header("出力")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource seSource;

        [Header("BGM")]
        [SerializeField] private AudioClip bgmTitle;
        [SerializeField] private AudioClip bgmDay;
        [SerializeField] private AudioClip bgmTension;  // 攻撃発生中
        [SerializeField] private AudioClip bgmEnding;

        [Header("SE")]
        [SerializeField] private AudioClip seClick;
        [SerializeField] private AudioClip seChoreSolve;
        [SerializeField] private AudioClip seAttackAppear;
        [SerializeField] private AudioClip seParryPerfect;
        [SerializeField] private AudioClip seParryGood;
        [SerializeField] private AudioClip seParryMiss;
        [SerializeField] private AudioClip seDefendSuccess;
        [SerializeField] private AudioClip seDefendFail;
        [SerializeField] private AudioClip seUpgrade;
        [SerializeField] private AudioClip seQuizCorrect;
        [SerializeField] private AudioClip seQuizWrong;
        [SerializeField] private AudioClip seGameOver;
        [SerializeField] private AudioClip seClear;

        public bool IsMuted { get; private set; }
        private readonly List<AudioClip> _generatedClips = new List<AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // 専用音源が無いときだけ、短いオリジナルの電子音を使う。差し替え済み素材は保持する。
            seClick = WithFallback(seClick, "操作", 0.045f, 740);
            seChoreSolve = WithFallback(seChoreSolve, "相談解決", 0.09f, 523, 659, 784);
            seAttackAppear = WithFallback(seAttackAppear, "警報", 0.14f, 392, 311, 392);
            seParryPerfect = WithFallback(seParryPerfect, "最適タイミング", 0.07f, 784, 988, 1175);
            seParryGood = WithFallback(seParryGood, "良好タイミング", 0.08f, 659, 880);
            seParryMiss = WithFallback(seParryMiss, "タイミング外れ", 0.09f, 220, 196);
            seDefendSuccess = WithFallback(seDefendSuccess, "防御成功", 0.12f, 523, 659, 784, 1047);
            seDefendFail = WithFallback(seDefendFail, "被害発生", 0.15f, 196, 147, 110);
            seUpgrade = WithFallback(seUpgrade, "対策強化", 0.075f, 440, 659, 880);
            seQuizCorrect = WithFallback(seQuizCorrect, "正解", 0.11f, 659, 880);
            seQuizWrong = WithFallback(seQuizWrong, "不正解", 0.12f, 294, 262);
            seGameOver = WithFallback(seGameOver, "年度中断", 0.18f, 392, 330, 262);
            seClear = WithFallback(seClear, "年度達成", 0.14f, 523, 659, 784, 1047, 1319);
            IsMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            ApplyMute();
        }

        private AudioClip WithFallback(AudioClip clip, string label, float duration, params float[] notes)
        {
            if (clip != null) return clip;
            const int rate = 22050;
            int noteLength = Mathf.CeilToInt(duration * rate);
            var samples = new float[noteLength * notes.Length];
            for (int note = 0; note < notes.Length; note++)
                for (int i = 0; i < noteLength; i++)
                {
                    float t = (float)i / rate;
                    float envelope = Mathf.Min(1f, t / 0.006f) * Mathf.Pow(1f - (float)i / noteLength, 2);
                    samples[note * noteLength + i] = Mathf.Sin(t * notes[note] * Mathf.PI * 2) * envelope * 0.18f;
                }
            clip = AudioClip.Create(label, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            _generatedClips.Add(clip);
            return clip;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var clip in _generatedClips) if (clip != null) Destroy(clip);
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            PlayerPrefs.SetInt(MuteKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMute();
        }

        private void ApplyMute()
        {
            if (bgmSource != null) bgmSource.mute = IsMuted;
            if (seSource != null) seSource.mute = IsMuted;
        }

        private void PlaySe(AudioClip clip)
        {
            if (clip == null || seSource == null) return;
            seSource.PlayOneShot(clip);
        }

        private void PlayBgm(AudioClip clip)
        {
            if (bgmSource == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            if (clip != null) bgmSource.Play();
            else bgmSource.Stop();
        }

        // ---- SE ショートカット ----
        public void PlayClick() => PlaySe(seClick);
        public void PlayChoreSolve() => PlaySe(seChoreSolve);
        public void PlayAttackAppear() => PlaySe(seAttackAppear);
        public void PlayParryPerfect() => PlaySe(seParryPerfect);
        public void PlayParryGood() => PlaySe(seParryGood);
        public void PlayParryMiss() => PlaySe(seParryMiss);
        public void PlayDefendSuccess() => PlaySe(seDefendSuccess);
        public void PlayDefendFail() => PlaySe(seDefendFail);
        public void PlayUpgrade() => PlaySe(seUpgrade);
        public void PlayQuizCorrect() => PlaySe(seQuizCorrect);
        public void PlayQuizWrong() => PlaySe(seQuizWrong);
        public void PlayGameOver() => PlaySe(seGameOver);
        public void PlayClear() => PlaySe(seClear);

        // ---- BGM ショートカット ----
        public void PlayBgmTitle() => PlayBgm(bgmTitle);
        public void PlayBgmDay() => PlayBgm(bgmDay);
        public void PlayBgmTension() => PlayBgm(bgmTension);
        public void PlayBgmEnding() => PlayBgm(bgmEnding);
    }
}
