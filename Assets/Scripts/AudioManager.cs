using UnityEngine;

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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            IsMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            ApplyMute();
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
