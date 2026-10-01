using System;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public static partial class OpsCatalog
    {
        // 初年度のカタログ・配列・抽選を変えず、本編の2年目から増やす。
        public const int BaseEquipmentCount=11, YearEquipmentCount=15, YearGrowthVersion=1;
        public const int ZeroTrustCost=17, EdrCost=18, ThreatSharingCost=12, CsirtCost=15;
        public const int ZeroTrustWork=2, EdrWork=2, ThreatSharingWork=1, CsirtWork=2;
        public const int ZeroTrustUpkeep=1, EdrUpkeep=2, ThreatSharingUpkeep=1, CsirtUpkeep=1;
        public const int ZeroTrustPrevention=5, ZeroTrustContainment=3, EdrContainment=5;
        public const int ThreatSharingKnowledge=1, ThreatSharingMarginCut=2, CsirtPeakStopCut=2;
        public const float EdrRevealSeconds=1;
        public const int EdrInstantIsolations=1;
        public static readonly OpsProject[] AdvancedProjects={
            new OpsProject{id="zeroTrust",name="ゼロトラスト接続",tag="入口",group="protect",requires="mfa",cost=ZeroTrustCost,time=ZeroTrustWork,upkeep=ZeroTrustUpkeep,max=2,term="defense",desc="拠点や在宅の接続を、場所だけで信じず確かめる。",effect="委託先・在宅・セッションの侵入被害と広がりを減らす。"},
            new OpsProject{id="edr",name="端末の検知と対応（EDR）",tag="端末",group="protect",requires="monitor",cost=EdrCost,time=EdrWork,upkeep=EdrUpkeep,max=2,term="detect",desc="端末の動きを検知し、対応につなぐ。",effect="ランサム・標的型の封じ込めに加算。感染した部屋を通知・強調。最初の感染端末を時間消費なしで1回切り離せる。監視の即時表示は維持。"},
            new OpsProject{id="threatSharing",name="脅威情報の共有",tag="情報",group="protect",requires="monitor",cost=ThreatSharingCost,time=ThreatSharingWork,upkeep=ThreatSharingUpkeep,max=2,term="detect",desc="業界の仲間と手口や兆候を共有する。",effect="標的型・侵害の主張・BECの把握+1。見積もりの幅も狭まる。真相の確定とは別。"},
            new OpsProject{id="csirt",name="事件対応の体制（CSIRT）",tag="体制",group="operations",requires="runbook",cost=CsirtCost,time=CsirtWork,upkeep=CsirtUpkeep,max=2,term="incident",desc="事件の連絡と判断の役割を決めておく。",effect="山場の月の停止を短縮。総決算にも同じ仕組みで効く。"}
        };
        private static OpsProject[] allProjects;
        public static OpsProject[] AllProjects=>allProjects??(allProjects=Projects.Concat(AdvancedProjects).ToArray());
        public static int EquipmentFirstYear(int index)=>index<BaseEquipmentCount?1:index<BaseEquipmentCount+2?2:3;
    }
    public partial class OpsState
    {
        // 古い途中保存では0。次年度へ進む時から有効にし、進行中の年度は変えない。
        public int yearGrowthRules;
        public bool EquipmentAvailable(int index)=>index>=0&&index<OpsCatalog.YearEquipmentCount&&
            (index<OpsCatalog.BaseEquipmentCount||yearGrowthRules>0&&storyCalendarYear>=OpsCatalog.EquipmentFirstYear(index));
        private string EquipmentProfile=>CurrentProfile?.id??Current.kind;
        public bool ZeroTrustApplies=>new[]{"supply","remote","session"}.Contains(EquipmentProfile);
        public bool EdrApplies=>new[]{"ransom","targeted"}.Contains(EquipmentProfile);
        public bool ThreatSharingApplies=>new[]{"targeted","claim","bec"}.Contains(EquipmentProfile);
        private int AdvancedPrevention=>ZeroTrustApplies?Level("zeroTrust")*OpsCatalog.ZeroTrustPrevention:0;
        private int AdvancedContainment=>(ZeroTrustApplies?Level("zeroTrust")*OpsCatalog.ZeroTrustContainment:0)+(EdrApplies?Level("edr")*OpsCatalog.EdrContainment:0);
        private int AdvancedKnowledge=>ThreatSharingApplies&&Level("threatSharing")>0?OpsCatalog.ThreatSharingKnowledge:0;
        private int AdvancedMarginCut=>ThreatSharingApplies?Level("threatSharing")*OpsCatalog.ThreatSharingMarginCut:0;
        private int AdvancedStopCut=>QuarterPeak?Level("csirt")*OpsCatalog.CsirtPeakStopCut:0;
        private bool ValidYearEquipment()=>yearGrowthRules>=0&&yearGrowthRules<=OpsCatalog.YearGrowthVersion&&levels!=null&&
            (yearGrowthRules==0?levels.Length==OpsCatalog.BaseEquipmentCount:storyCalendarYear>=2&&levels.Length==OpsCatalog.YearEquipmentCount&&
            Enumerable.Range(0,levels.Length).All(i=>EquipmentAvailable(i)||levels[i]==0));
    }
}
