using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private static readonly string[] ResponseIds = { "contain", "scope", "recover" };

        private void IncidentWorkspace()
        {
            // 対応中は成長目標を常時並べず、案件・比較・現場に視線を集める。
            var map = Box(screen, "OfficeStage", 24, 120, 610, 440, Panel, true);
            var viewport = Rect(map, "OfficeViewport", 6, 6, 598, 428);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            Art(viewport, 0, -74, 598, 598);
            var badge = Box(map, "OfficeBadge", 16, 16, 296, 48, Ink);
            badge.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(badge, "Month", State.Current.name + " / " + (State.month + 1) + "か月目", 14, 8, 270, 32, 24);
            Button(map, "OpenTeam", "社員の支援 >", 390, 16, 202, 48, TeamDialog, Edge);

            bool people = State.Current.kind == "social" || State.Current.kind == "leak";
            var target = Button(map, "IncidentLocation", "", people ? 365 : 196, people ? 222 : 118, 210, 85, IncidentEvidence, Ink);
            Glyph(target.transform, "TargetGlyph", people ? "training" : "backup", 12, 14, 34);
            Text(target.transform, "LocationLabel", "確認対象", 56, 10, 140, 28, 18, Coral);
            Text(target.transform, "LocationName", people ? "社員・情報の扱い" : "システム・設備", 12, 48, 187, 28, 18);
            // 地点は関連領域の案内。未確認の侵害や社員の行動を確定描写しない。
            var strip = Box(map, "OfficeSupport", 16, 341, 578, 81, Ink);
            strip.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(strip, "SupportHeading", "今月の支援", 14, 8, 550, 26, 18, StaffColor);
            Text(strip, "SupportStatus", State.growthRules == 0 ? "旧年度 / 社員育成なし" : State.SupportSummary, 14, 37, 550, 37, 19);

            var navigator = Box(screen, "Navigator", 24, 580, 610, 284, Paper);
            Portrait(navigator, "NavigatorPortrait", 4, 9, 225, 266);
            Text(navigator, "NavigatorName", Navigator != null ? Navigator.DisplayName : "ひなた", 244, 19, 343, 34, 25, Ink);
            Text(navigator, "NavigatorSpeech", State.audited ? "調査できたね。3つの案を比べよう。" : "まだ未確認だね。止める範囲も比べよう。", 244, 67, 343, 96, 25, Ink);
            Text(navigator, "NavigatorRole", State.CurrentMissionCompleted ? "依頼達成 +45点 / 信頼 +3" : "情シスパートナー", 244, 174, 343, 28, 17, Hex("526071"));
            Button(navigator, "IncidentHelp", "根拠・知識を見る >", 244, 216, 343, 48, IncidentEvidence, Edge);

            var right = Box(screen, "DecisionPanel", 654, 120, 922, 744, Panel, true);
            Incident(right);
            Text(screen, "SaveStatus", SaveWarning == "" ? "自動保存済み / 時間制限なし" : SaveWarning, 30, 874, 1510, 22, 15, SaveWarning == "" ? Muted : Coral);
        }

        private void IncidentComparison(RectTransform p)
        {
            Button(p, "IncidentTopic", (State.CurrentProfile == null ? "今月の出来事" : State.CurrentProfile.category) + " / 題材 >", 24, 20, 874, 36, EventBriefDialog, Ink);
            Text(p, "IncidentTitle", State.Current.@event, 24, 71, 874, 69, 33);
            Text(p, "IncidentSymptom", State.Current.symptom, 24, 149, 874, 52, 21, Muted);
            Button(p, "IncidentEvidence", State.audited ? "調査済み / 根拠を見る >" : "未確認 / 正常な活動の可能性もあり >", 24, 215, 486, 44, IncidentEvidence, Ink);
            string recovery = State.DataRecoveryApplies ?
                (State.RestoreChain > 0 ? "復元連携 Lv." + State.RestoreChain : "復元連携 / 未整備") + "\nバックアップ + 復元訓練" :
                State.RestartApplies ? (State.RestartChain > 0 ? "再開連携 Lv." + State.RestartChain : "再開連携 / 未整備") + "\n自動化 + 引継ぎ手順" : "今回は復元・再開連携の対象外";
            Text(p, "RecoveryReadiness", recovery, 530, 207, 367, 60, 18, Accent);

            var estimates = ResponseIds.Select(State.Estimate).ToArray();
            int lossScale = Math.Max(1, estimates.Max(e => e.lossMax));
            int stopScale = Math.Max(1, estimates.Max(e => e.stopMax));
            string[] glyphs = { "firewall", "idsIps", "backup" };
            string[] caution = { "正常な業務も止まる", "範囲を絞る備えが重要", "安全確認後に再開" };
            for (int i = 0; i < ResponseIds.Length; i++)
            {
                string id = ResponseIds[i]; var estimate = estimates[i]; float x = 24 + i * 298;
                var card = Box(p, "ResponseCard_" + id, x, 282, 278, 382, Ink);
                card.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                Glyph(card.transform, "ResponseGlyph_" + id, glyphs[i], 16, 16, 36);
                Text(card.transform, "ResponseType_" + id, "0" + (i + 1) + " / " + (i == 0 ? "停止・隔離" : i == 1 ? "範囲を限定" : "復旧に配分"), 63, 18, 199, 30, 20, Accent);
                Text(card.transform, "ResponseName_" + id, State.ResponseName(id), 16, 62, 246, 59, 21);
                Text(card.transform, "ResponseCost_" + id, "対応費 " + estimate.cost + "万円", 16, 125, 246, 33, 22, State.budget < estimate.cost ? Coral : Paper);
                EstimateMetric(card.transform, "Loss_" + id, "被害見積もり / 万円", estimate.lossMin, estimate.lossMax, lossScale, 162);
                EstimateMetric(card.transform, "Stop_" + id, "停止見積もり / 時間", estimate.stopMin, estimate.stopMax, stopScale, 232);
                Text(card.transform, "ResponseCaution_" + id, State.budget < estimate.cost ? "手元予算が対応費に不足" : caution[i], 16, 303, 246, 24, 17, State.budget < estimate.cost ? Coral : Muted);
                Button(card, "Respond_" + id, "この対応で進む", 10, 334, 258, 44, () => Resolve(id), Accent);
                Button(p, "Power_" + id, "抑制力 " + State.ResponsePower(id).Total + " / 内訳 >", x, 674, 278, 32, () => PowerReport(id), Ink);
            }
            Text(p, "NoTimer", "見積もりの幅 ≠ 確率 / 対応費は被害と別 / 時間制限なし", 24, 714, 874, 26, 16, Muted);
        }

        private void EstimateMetric(Transform parent, string id, string title, int min, int max, int scale, float y)
        {
            Text(parent, "EstimateLabel_" + id, title, 16, y, 246, 22, 16, Muted);
            var value = Text(parent, "EstimateValue_" + id, min == max ? min.ToString() : min + "～" + max, 16, y + 26, 246, 36, 26);
            if (HeadingFont != null) value.font = HeadingFont;
            var track = Box(parent, "EstimateTrack_" + id, 16, y + 62, 246, 6, Edge);
            track.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            // 3案は同一尺度。濃い部分が下限、薄い部分が見積もりの幅。ゼロにも数値を残す。
            if (max > 0)
            {
                var band = Box(track, "EstimateBand_" + id, 0, 0, 246f * max / scale, 6, Hex("6A4653"));
                band.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            }
            if (min > 0)
            {
                var floor = Box(track, "EstimateFloor_" + id, 0, 0, 246f * min / scale, 6, Coral);
                floor.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            }
        }

        private void Glyph(Transform parent, string name, string key, float x, float y, float size)
        {
            var r = Rect(parent, name, x, y, size, size);
            var icon = r.gameObject.AddComponent<DefenseGlyph>(); icon.SetKey(key); icon.raycastTarget = false;
        }

        private void IncidentEvidence()
        {
            var d = Dialog("確認できたこと", State.Current.symptom + "\n\n" + (State.audited ? State.Current.finding :
                "調査前のため、深刻度は未確認です。正常な活動の可能性も残ります。") +
                "\n\n見積もりは公開情報に基づく幅です。発生確率や保証ではありません。\n抑制力の内訳は各対応案から確認できます。", 640);
            Button(d, "IncidentKnowledge", "関連する知識を読む", 32, 554, 420, 48, () => Knowledge(State.Current.lesson), Accent);
        }
    }
}
