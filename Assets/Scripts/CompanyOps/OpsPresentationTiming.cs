using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    // 見せる時間だけ。モデルの制限時間・出現間隔・復旧・ヒットストップはここに入れない。
    public static class OpsPresentationTiming
    {
        public const float FastRate=2, Reveal=.45f, RepeatReveal=.27f, Close=.2f;
        public const float Wipe=.3f, WipeMid=.15f, Month=1.5f, Incident=.8f, Boss=1.5f;
        public const float CalendarCover=.5f, FadeIn=.12f, FadeOut=.18f, CalendarTurn=.2f, InputGuard=.35f;
        public const float ResolutionFirst=3.2f, ResolutionRepeat=2.3f, ResolutionShort=1.15f;
        public const float ResolutionNeedle=1.05f, NeedleSweep=.85f, ResolutionResult=.75f;
        public const float Finish=1.1f, ScoreStep=.035f, ResultStamp=.5f;
        public const float Factor=3.25f, FactorStart=.2f, FactorStep=.8f, FactorFlip=.9f, FactorLight=1.1f;
        public const float DiaryOpen=.7f, DiarySticker=.6f, DiaryWrite=2.4f;
        public const float ReportCount=.6f, ReportBounce=.18f, CountFade=.06f, AnnualStep=.08f;
        public const float TitleLogo=.9f, PortraitExit=.2f;
        public const float PortraitEnter=.35f, PortraitSwap=.24f, PortraitAct=.4f;
        public const float StaffTravel=.45f, StatSpark=.6f, GaugeGrow=.45f, WorkComplete=.5f, RankFlip=.4f, BudgetCoins=.6f;
        public const float LossHold=.2f, LossFade=.4f, RoomZoom=.4f, InstallationPulse=.7f, NoticeHold=1.1f, NoticeFade=.35f;
        // 既存の年度開幕と同じ秒数。Unity非依存のルール側の参照は変更しない。
        public static readonly float[] Opening={2.4f,2.2f,2.4f,2.2f,2.6f};
    }

    public sealed class OpsPresentationWait
    {
        public readonly string Kind;
        public readonly float Duration;
        public readonly bool CanSkip;
        public float Elapsed {get;private set;}
        public bool Skipped {get;private set;}
        internal bool ManualCompletion;
        private bool finished;
        public bool Done=>finished||!ManualCompletion&&Elapsed>=Duration;
        public OpsPresentationWait(string kind,float duration,bool canSkip)
        {Kind=kind;Duration=Math.Max(0,duration);CanSkip=canSkip;}
        public void Advance(float delta,bool fast)
        {Elapsed=Math.Min(Duration,Elapsed+Math.Max(0,delta)*(fast?OpsPresentationTiming.FastRate:1));}
        public bool Skip()
        {if(!CanSkip||Done)return false;Skipped=true;Elapsed=Duration;finished=true;return true;}
        internal void Finish(){Elapsed=Duration;finished=true;}
    }

    public partial class OpsGame
    {
        private readonly List<OpsPresentationWait> presentationWaits=new List<OpsPresentationWait>();
        private readonly Dictionary<string,int> timingVisits=new Dictionary<string,int>();
        private readonly Dictionary<OpsPresentationWait,Transform> presentationRoots=new Dictionary<OpsPresentationWait,Transform>();
        private int presentationConsumedFrame=-1;
        private OpsPresentationWait annualTiming,reportTiming,diaryTiming,factorTiming,resolutionTiming,phaseTiming,finishTiming,openingTiming;
        private IEnumerable<OpsPresentationWait> ActivePresentationWaits=>presentationWaits.Where(w=>!w.Done&&(modal==null||presentationRoots.TryGetValue(w,out var root)&&root!=null&&root.IsChildOf(modal)));
        public bool PresentationWaiting=>ActivePresentationWaits.Any();
        private OpsPresentationWait BeginPresentation(string kind,float seconds,bool? repeat=null,Transform root=null)
        {
            // 遊びの最中の成功表示などは入力を奪わない。設定窓の待ちは別に扱う。
            if(Minigame?.Phase==OpsMinigamePhase.Playing&&modal==null)return new OpsPresentationWait(kind,seconds,false);
            timingVisits.TryGetValue(kind,out int visits);timingVisits[kind]=visits+1;
            var wait=new OpsPresentationWait(kind,seconds,repeat??visits>0);presentationWaits.Add(wait);if(root==null)root=screen;if(root!=null)presentationRoots[wait]=root;return wait;
        }
        private void ClearPresentationWaits()
        {foreach(var w in presentationWaits)w.Finish();presentationWaits.Clear();presentationRoots.Clear();annualTiming=reportTiming=diaryTiming=null;}
        // 入力の一回を演出だけに使い、同じクリックで次の画面のボタンまで押さない。
        public bool TrySkipPresentation()
        {
            if(Minigame?.Phase==OpsMinigamePhase.Playing&&modal==null)return false;
            bool active=PresentationWaiting;if(!active)return false;
            bool skipped=false;foreach(var w in ActivePresentationWaits.ToArray())skipped|=w.Skip();
            if(skipped){StopVoice();if(Minigame?.Phase==OpsMinigamePhase.Result)minigameMaximSpoken=true;}
            presentationInputGuardUntil=Time.unscaledTime+OpsPresentationTiming.InputGuard;presentationConsumedFrame=UnityEngine.Time.frameCount;
            return true; // 初回も押した操作は消費するが最後へは飛ばさない。
        }
        private void TickPresentationInput()
        {
            foreach(var item in presentationRoots.ToArray())if(item.Value==null||!item.Value.gameObject.activeInHierarchy){item.Key.Finish();presentationRoots.Remove(item.Key);}
            annualTiming?.Advance(Time.unscaledDeltaTime,FastPresentation);
            reportTiming?.Advance(Time.unscaledDeltaTime,FastPresentation);
            diaryTiming?.Advance(Time.unscaledDeltaTime,FastPresentation);
            presentationWaits.RemoveAll(w=>w.Done);
            if(PresentationPressed()&&PresentationWaiting)TrySkipPresentation();
        }
        private bool PresentationInputConsumed=>presentationConsumedFrame==UnityEngine.Time.frameCount;
        internal OpsPresentationWait BeginReveal(string name,float duration,Transform root)=>BeginPresentation("reveal_"+name,duration,annualTiming?.CanSkip??reportTiming?.CanSkip,root);
        internal OpsPresentationWait BeginDiaryMotion(string name,float duration,Transform root)=>BeginPresentation("diary_motion_"+name,duration,diaryTiming?.CanSkip,root);
        internal OpsPresentationWait BeginOpeningMotion(string name,float duration,Transform root)=>BeginPresentation("opening_motion_"+name,duration,openingTiming?.CanSkip,root);
        internal OpsPresentationWait BeginResultStamp(float duration,Transform root)=>BeginPresentation("result_stamp",duration,finishTiming?.CanSkip,root);
        internal OpsPresentationWait BeginFeedback(string name,float duration,Transform root)=>BeginPresentation("feedback_"+name,duration,null,root);
    }
}
