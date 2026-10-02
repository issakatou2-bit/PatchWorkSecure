using TMPro;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public void ChooseTicket(bool delegateToStaff)
        {
            var previous = ReadStats(); var oldLevels = State.GrowthLevels;
            if (!State.ResolveTicket(delegateToStaff)) return;
            RecordStatChanges(previous); string levelUp = LevelUpNotice(oldLevels);
            Save(); Render();
            Toast(levelUp != "" ? "LEVEL UP / "+levelUp : "日常チケット 完了 / "+State.TicketEffect+
                (delegateToStaff ? "・社員に任せて工数を確保" : "・社員と手順を確認"), true, levelUp != "" ? OpsCue.Growth : OpsCue.Action);
        }
        private void TicketDialog()
        {
            var t = State.Ticket; if (t == null) return;
            var d = Dialog("日常チケット / "+t.title, t.request, 610);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta = new Vector2(748, 80);
            var info = Box(d, "TicketBenefit", 32, 204, 752, 112, Ink);
            info.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            int member=State.TicketMember,xp=1+(State.HasJunior&&member==OpsCatalog.OriginalStaffCount?OpsCatalog.JuniorExtraExperience:0);
            Text(info, "TicketBenefitText", "対応の効果 / "+State.TicketEffect+"\n担当："+OpsGrowthCatalog.StaffNames[member]+" / 社員経験 +"+xp+"\n共同対応：1工数・担当者経験 +1 / 委任：0工数", 18, 12, 716, 90, 21, Mint);
            Text(d, "TicketContext", "月1件の任意の仕事です。今月の改善投資と工数を比べて選べます。\n"+
                "委任には担当社員Lv.2と引継ぎ手順が必要です。\n後回しでも罰則はありません。対応済みの仕事は月報へ残ります。", 32, 341, 748, 105, 20, Ink);
            for (int i=0;i<2;i++)
            {
                bool delegated = i == 1; string block = State.TicketBlock(delegated);
                Button(d, delegated ? "DelegateTicket" : "ResolveTicket", block != "" ? block : delegated ? "社員に任せる / 0工数" : "一緒に対応する / 1工数",
                    32+i*380, 465, 364, 48, ()=>ChooseTicket(delegated), delegated ? Edge : Accent, block=="");
            }
            Button(d, "TicketKnowledge", "関連する知識", 32, 540, 350, 48, ()=>Knowledge(t.lesson));
            if(t.source=="identity") Text(d,"TicketReference","参考：IdP（社員の認証をまとめる仕組み）の説明",400,540,190,48,13,Muted);
        }
        private void EventBriefDialog()
        {
            var e = State.CurrentEvent; var p = State.CurrentProfile;
            var d = Dialog(State.Current.title, State.Current.hint, 650);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta = new Vector2(748, 86);
            Text(d, "EventCategory", p==null ? "旧年度 / 月固定の出来事" : p.category+" / "+(e.operational ? "運用トラブルの題材" : "攻撃・不正利用の題材"), 32, 212, 748, 38, 24, Ink);
            string mapping = e==null || e.threats.Length==0 ? "IPAの順位とは別に、日常運用の題材として構成。" :
                "IPA 10大脅威2026 / "+string.Join("・", System.Array.ConvertAll(e.threats, rank=>OpsEventCatalog.ThreatNames[rank-1]));
            Text(d, "EventMapping", mapping, 32, 266, 748, 64, 20, Ink);
            Text(d, "EventPreparation", "社内依頼 / "+State.CurrentMission.title+"\n設備："+State.CurrentMission.equipmentRoute+"\n現場："+State.CurrentMission.fieldRoute, 32, 344, 748, 106, 21, Ink);
            Text(d, "EventReference", "公表資料の論点を、架空の会社向けに組み直した出来事です。\n抑制力・費用・停止時間はゲーム用のモデルです。", 32, 475, 748, 63, 18, Ink);
            Button(d, "EventKnowledge", "関連する知識", 32, 579, 350, 48, ()=>Knowledge(State.Current.lesson));
            Text(d,"EventFictionNote","この出来事は架空の想定です。資料は考え方の参考です。",232,579,158,48,12,Muted);
            if (p!=null) Button(d, "EventSource", "参考資料を開く", 400, 579, 190, 48, ()=>Application.OpenURL(OpsEventCatalog.SourceUrl(e.source ?? p.source)));
        }
        private static string TicketRecord(OpsOutcome result)
        {
            var t = OpsEventCatalog.Ticket(result.ticketId);
            if (t==null) return "日常チケット / 以前の記録には記載なし";
            return "日常 / "+t.title+" / "+(result.ticketMode=="delegate" ? "社員が対応" : result.ticketMode=="self" ? "共同対応" : "後回し");
        }
    }
}
