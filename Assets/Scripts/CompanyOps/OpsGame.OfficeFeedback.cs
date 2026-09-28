using System.Collections;
using TMPro;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private void ShowInstallation(int index)
        {
            if (!Application.isPlaying) return;
            var project = OpsCatalog.Projects[index];
            var map = screen.Find("OfficeStage") as RectTransform;
            if (map == null) return;
            string target = project.group == "people" ? "Pin_culture" : project.group == "recover" ? "Pin_backup" : "Pin_change";
            var pin = map.Find(target) as RectTransform;
            if (pin != null) StartCoroutine(InstallationPulse(pin));
            var marker = Box(map, "InstallationNotice", 18, 86, 628, 44, Ink);
            marker.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            marker.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            Text(marker, "InstallationLabel", project.name + "  Lv." + State.levels[index] + "  導入完了", 14, 7, 600, 30, 22, Mint);
            StartCoroutine(FadeOfficeNotice(marker));
        }

        private IEnumerator InstallationPulse(RectTransform pin)
        {
            float elapsed = 0;
            while (pin != null && elapsed < .7f)
            {
                pin.localScale = Vector3.one * (ReducedMotion ? 1 : 1 + .06f * Mathf.Sin(elapsed / .7f * Mathf.PI));
                elapsed += Time.unscaledDeltaTime; yield return null;
            }
            if (pin != null) pin.localScale = Vector3.one;
        }

        private IEnumerator FadeOfficeNotice(RectTransform notice)
        {
            var group = notice.GetComponent<CanvasGroup>();
            yield return new WaitForSecondsRealtime(1.1f);
            float elapsed = 0;
            while (notice != null && elapsed < .35f)
            {
                group.alpha = 1 - elapsed / .35f;
                elapsed += Time.unscaledDeltaTime; yield return null;
            }
            if (notice != null) Destroy(notice.gameObject);
        }

        private void OfficeOutcome(RectTransform map, OpsOutcome result)
        {
            if (result == null) return;
            // 停止時間は今月の確定記録。現在も停止中、侵害地点が特定済みとは描写しない。
            var record = Box(map, "OfficeOutcome", 18, 86, 230, 104, Ink);
            record.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Color tone = result.loss > 0 || result.downtime > 4 ? Coral : Mint;
            Box(record, "OutcomeRail", 0, 0, 4, 105, tone).GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(record, "OfficeOutcomeTitle", "今月の対応完了", 13, 9, 204, 26, 20, tone);
            Text(record, "OfficeOutcomeNumbers", "被害 " + result.loss + "万円 / 停止 " + result.downtime + "h", 13, 40, 204, 28, 17);
            Text(record, "OfficeOutcomeNote", "今月の記録 / 現在の障害ではない", 13, 77, 204, 21, 12, Muted);
            if (result.power == null) return;
            var support = Box(map, "OutcomeSupport", 18, 209, 404, 74, Ink);
            support.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(support, "OutcomeSupportTitle", result.power.staff > 0 ? "社員の助力  +" + result.power.staff + " 抑制力" : "社員の対応加算 0 / 担当・習熟に応じる", 14, 8, 376, 28, 20, result.power.staff > 0 ? StaffColor : Muted);
            Text(support, "OutcomeSupportDetail", result.power.support, 14, 39, 376, 27, 15, Paper);
        }
    }
}
