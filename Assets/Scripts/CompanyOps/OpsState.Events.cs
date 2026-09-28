using System;
using System.Linq;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsState
    {
        // 旧セーブは0のまま。新年度でのみ抽選し、イベントIDを保存する。
        public int eventRules;
        public string[] eventSchedule, ticketSchedule;
        public string ticketResolution = "";
        public OpsState(int yearSeed, bool randomEvents) : this(yearSeed)
        {
            if (!randomEvents) return;
            eventRules = 1;
            eventSchedule = OpsEventCatalog.Schedule(yearSeed, false);
            ticketSchedule = OpsEventCatalog.Schedule(yearSeed, true);
        }
        public OpsEvent EventAt(int targetMonth) => eventRules == 0 || eventSchedule == null || targetMonth < 0 || targetMonth >= eventSchedule.Length ? null :
            OpsEventCatalog.Event(eventSchedule[targetMonth]);
        public OpsEvent CurrentEvent => EventAt(month);
        public OpsEventProfile CurrentProfile => CurrentEvent == null ? null : OpsEventCatalog.Profile(CurrentEvent.profile);
        public OpsTicket Ticket => eventRules == 0 || ticketSchedule == null || month >= ticketSchedule.Length ? null : OpsEventCatalog.Ticket(ticketSchedule[month]);
        public OpsMonth MonthAt(int targetMonth)
        {
            var calendar = OpsCatalog.Months[targetMonth]; var e = EventAt(targetMonth);
            if (e == null) return calendar;
            var p = OpsEventCatalog.Profile(e.profile);
            return new OpsMonth { name=calendar.name, season=calendar.season, @base=calendar.@base, title=e.title, news=e.news, boss=e.boss,
                person=e.person, staff=e.staff, hint=e.hint, kind=p.kind, @event=e.title, symptom=e.symptom, finding=e.finding, lesson=p.lesson, calm=e.calm };
        }
        public OpsMission CurrentMission
        {
            get
            {
                if (CurrentProfile == null) return OpsCatalog.Missions[month];
                var p = CurrentProfile;
                return new OpsMission { title=CurrentEvent.title+"への備え", projectA=p.projectA, projectB=p.projectB, actionA=p.actionA, actionB=p.actionB,
                    equipmentRoute=OpsEventCatalog.ProjectNames(p), fieldRoute=OpsEventCatalog.ActionName(p.actionA)+"＋"+OpsEventCatalog.ActionName(p.actionB) };
            }
        }
        private int CulturalPower => CurrentProfile == null ? Current.kind == "social" ? culture/8 : Current.kind == "leak" ? culture/12 : 0 :
            CurrentProfile.cultureDivisor == 0 ? 0 : culture/CurrentProfile.cultureDivisor;
        private int ContainmentPower => (CurrentProfile == null ? Current.kind == "ransom" || Current.kind == "supply" || Current.kind == "vulnerability" ? 6 : 0 :
            CurrentProfile.containment) * Level("segment");
        private int BasicResponsePower(string response) => CurrentProfile == null ? response == "contain" ? Current.kind == "outage" ? 7 : 23 : 5 :
            response == "contain" ? CurrentProfile.stopPower : response == "scope" ? CurrentProfile.scopePower : CurrentProfile.recoverPower;
        public string ResponseName(string response) => CurrentProfile == null ? response == "contain" ? "広範囲の停止・隔離" :
            response == "scope" ? "対象を限定して対応" : "代替業務・復旧を優先" : CurrentProfile.responses[response=="contain" ? 0 : response=="scope" ? 1 : 2];
        public string ResponseFocus => CurrentProfile == null ? Current.hint : CurrentProfile.category+" / "+Current.hint;
        public string TicketEffect => Ticket == null ? "この年度は日常チケットなし" :
            Ticket.cash > 0 ? "予算 +"+Ticket.cash+"万円" : Ticket.culture > 0 ? "相談文化 +"+Ticket.culture :
            Ticket.trust > 0 ? "経営の信頼 +"+Ticket.trust : "疲労 -"+Ticket.relief;
        public string TicketBlock(bool delegateToStaff)
        {
            if (Ticket == null) return "日常チケットは新しい年度で有効です";
            if (phase != OpsPhase.Planning) return "計画中に対応できます";
            if (!string.IsNullOrEmpty(ticketResolution)) return "今月のチケットは対応済み";
            if (!delegateToStaff) return capacity < 1 ? "今月の工数が足りません" : "";
            return StaffLevel(Ticket.member) < 2 || Level("runbook") == 0 ? OpsGrowthCatalog.StaffNames[Ticket.member]+"Lv.2＋引継ぎ手順が必要" : "";
        }
        public bool ResolveTicket(bool delegateToStaff)
        {
            if (TicketBlock(delegateToStaff) != "") return false;
            var t = Ticket;
            if (!delegateToStaff) { capacity--; GainPlayer(1); }
            ticketResolution = delegateToStaff ? "delegate" : "self";
            budget += t.cash; culture=Clamp(culture+t.culture); trust=Clamp(trust+t.trust); fatigue=Clamp(fatigue-t.relief);
            int xp = GainStaff(t.member, 1); Learn(t.lesson);
            Note("日常チケット「"+t.title+"」完了 / "+(delegateToStaff ? OpsGrowthCatalog.StaffNames[t.member]+"に任せた / 担当者の工数0" : "共同対応 / 1工数")+
                " / "+TicketEffect+" / 社員経験 +"+xp+"。");
            CheckMilestones(); return true;
        }
        private bool ValidEvents()
        {
            if (eventRules < 0 || eventRules > 1) return false;
            if (eventRules == 0) return (eventSchedule == null || eventSchedule.Length == 0) && (ticketSchedule == null || ticketSchedule.Length == 0) &&
                string.IsNullOrEmpty(ticketResolution) && history.All(r=>string.IsNullOrEmpty(r.eventId));
            if (eventSchedule == null || eventSchedule.Length != 12 || eventSchedule.Distinct().Count() != 12 || eventSchedule.Any(id=>OpsEventCatalog.Event(id)==null) ||
                ticketSchedule == null || ticketSchedule.Length != 12 || ticketSchedule.Distinct().Count() != 12 || ticketSchedule.Any(id=>OpsEventCatalog.Ticket(id)==null) ||
                (!string.IsNullOrEmpty(ticketResolution) && ticketResolution != "self" && ticketResolution != "delegate")) return false;
            return history.All(r=>r.month>=0 && r.month<12 && r.eventId==eventSchedule[r.month] && !string.IsNullOrEmpty(r.eventTitle) && r.eventTitle.Length<=200 &&
                OpsCatalog.Term(r.lessonId)!=null && r.ticketId==ticketSchedule[r.month] && new[]{"defer","self","delegate"}.Contains(r.ticketMode));
        }
    }
}
