using System;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        // 補足を削除せず、プレイヤーが必要な時だけ開ける場所へ移す。
        private void ConsultationDetails()
        {
            Dialog("今月の相談", State.Current.person + "\n「" + State.Current.boss + "」\n\n社員の声\n" + State.StaffVoice +
                "\n\n業界ニュース / 架空\n" + State.Current.news, 620);
        }

        private void ReviewDetails(OpsOutcome result)
        {
            Dialog("対応の振り返り", result.explanation + "\n" + result.promise + "\n\n" + GrowthResultText(result) +
                "\n\n" + TicketRecord(result) + "\n" + OutcomeSituation(result) +
                (State.month == 11 ? "" : "\n\n翌月の予算 +" + State.MonthlyGrant + "万円 / 維持費 -" + State.Upkeep + "万円\n来月の事情 / " + State.SituationAt(State.month + 1).title), 740);
        }

        private void AnnualMetric(Transform parent, string name, string title, string value, float x, float y, Color color)
        {
            var tile = Box(parent, name, x, y, 360, 106, Ink);
            tile.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(tile, name + "Label", title, 18, 10, 322, 30, 20, Muted);
            Text(tile, name + "Value", value, 18, 47, 322, 45, 34, color);
        }

        private void AnnualDetails()
        {
            Dialog("年間評価の内訳", "備え " + State.Preparedness + " / 立て直す力 " + State.Resilience + " / チームの力 " + State.Organization +
                "\n\n成長目標\n" + (State.milestones.Count == 0 ? "未達成" : string.Join("\n", State.milestones)) +
                "\n\n採点\n基礎1000 − 被害 " + State.totalLoss * 7 + " − 停止 " + State.totalDowntime * 4 +
                "\n依頼 +" + State.MissionCount * 45 + " / 成長 +" + State.milestones.Count * 30 +
                "\n会社の能力 +" + (State.Preparedness + State.Resilience + State.Organization) * 2 +
                " / 残予算 +" + Math.Max(0, Math.Min(200, State.budget)), 630);
        }
    }
}
