using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        // 通常のビルドでは実装が無く、呼び出しもコンパイラが除去する。
        partial void ObservePromoAudio(AudioSource source,AudioClip clip,string kind,string id);
#if UNITY_INCLUDE_TESTS
        public static event System.Action<AudioSource,AudioClip,string,string> PromoAudioPlayed;
        partial void ObservePromoAudio(AudioSource source,AudioClip clip,string kind,string id)
        {if(TestMode&&OpsPromoClock.Active)PromoAudioPlayed?.Invoke(source,clip,kind,id);}
#endif
    }
#if UNITY_INCLUDE_TESTS
    // captureFramerateはunscaledTimeを固定しない。撮影だけ同じ30Hz時計を使う。
    // この名前はCompanyOps内だけに効き、テスト無しのPlayerには存在しない。
    internal static class Time
    {
        public static float unscaledTime=>OpsPromoClock.Active?OpsPromoClock.Now:UnityEngine.Time.unscaledTime;
        public static double unscaledTimeAsDouble=>OpsPromoClock.Active?OpsPromoClock.Now:UnityEngine.Time.unscaledTimeAsDouble;
        public static float unscaledDeltaTime=>OpsPromoClock.Active?1f/30:UnityEngine.Time.unscaledDeltaTime;
        public static float realtimeSinceStartup=>OpsPromoClock.Active?OpsPromoClock.Now:UnityEngine.Time.realtimeSinceStartup;
        public static int frameCount=>UnityEngine.Time.frameCount;
    }
    internal sealed class WaitForSecondsRealtime:CustomYieldInstruction
    {
        private readonly float seconds;private float until=-1;
        public WaitForSecondsRealtime(float seconds){this.seconds=seconds;}
        public override bool keepWaiting
        {get{if(until<0)until=Time.realtimeSinceStartup+seconds;if(Time.realtimeSinceStartup<until)return true;until=-1;return false;}}
    }
    public static class OpsPromoClock
    {
        public static bool Active {get;private set;}
        public static float Now {get;private set;}
        public static int Frames {get;private set;}
        public static string ResumeFrom="";
        private static int oldCapture;
        public static void Begin()
        {if(Active)throw new System.InvalidOperationException("撮影時計は重複開始しません");oldCapture=UnityEngine.Time.captureFramerate;Now=UnityEngine.Time.unscaledTime;Frames=0;UnityEngine.Time.captureFramerate=30;Active=true;}
        public static void Advance(){if(!Active)throw new System.InvalidOperationException("撮影時計が未開始");Frames++;Now+=1f/30;}
        public static void End(){if(!Active)return;Active=false;UnityEngine.Time.captureFramerate=oldCapture;}
    }
#endif
}
