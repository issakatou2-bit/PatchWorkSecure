using System;

namespace PatchWorkSecure.CompanyOps
{
    public static partial class OpsCatalog
    {
        public const int OriginalStaffCount=3, JuniorStaffCount=4, JuniorYear=3;
        public const int EngineerKnowledge=1, EngineerWork=0, SecretaryPeakBudget=4, JuniorExtraExperience=1;
    }
    public partial class OpsState
    {
        public bool engineerRequested;
        // 0は従来のおまかせ。1〜4は社員の番号＋1。旧保存は0のまま。
        public int supportMemberChoice;
        public bool HasYearAllies=>yearGrowthRules>0&&storyCalendarYear>=2;
        public bool HasJunior=>HasYearAllies&&storyCalendarYear>=OpsCatalog.JuniorYear;
        public int StaffCount=>HasJunior?OpsCatalog.JuniorStaffCount:OpsCatalog.OriginalStaffCount;
        public int SecretaryProposalBonus=>HasYearAllies&&QuarterPeak?OpsCatalog.SecretaryPeakBudget:0;
        public int ProposalOffer=>12+Evidence*3+ProposalRankBonus+SecretaryProposalBonus;
        private int EngineerKnowledgeGain=>HasYearAllies&&engineerRequested?OpsCatalog.EngineerKnowledge:0;
        public string EngineerResearchBlock=>!HasYearAllies?"エンジニアさんの常駐は2年目から":phase!=OpsPhase.Planning?"計画中に調査を頼めます":engineerRequested?"今月は調査済み":SituationKnowledge>=OpsCatalog.KnowledgeMax?"状況の把握は最大です":"";
        public bool RequestEngineerResearch()
        {
            if(EngineerResearchBlock!="")return false;
            engineerRequested=true;capacity-=OpsCatalog.EngineerWork;
            Note("エンジニアさんに調査を依頼 / 工数 "+OpsCatalog.EngineerWork+"・状況の把握 +"+OpsCatalog.EngineerKnowledge+"。記録を整理した。侵害が確定したという意味ではない。");return true;
        }
        public bool SelectSupportMember(int member)
        {
            if(!HasJunior||phase!=OpsPhase.Planning||!string.IsNullOrEmpty(supportOrder)||member<-1||member>=StaffCount)return false;
            supportMemberChoice=member+1;return true;
        }
        public int TicketMember=>supportMemberChoice>0?supportMemberChoice-1:Ticket?.member??0;
        private bool ValidYearAllies()=>supportMemberChoice>=0&&supportMemberChoice<=StaffCount&&
            (HasJunior||supportMemberChoice==0)&&(HasYearAllies||!engineerRequested);
    }
}
