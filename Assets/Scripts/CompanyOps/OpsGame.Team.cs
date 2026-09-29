using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private static readonly Color PlayerColor = Hex("70B4FF"), StaffColor = Hex("43CEC6"), EquipmentColor = Hex("47D7A0");

        private string LevelUpNotice(int[] before)
        {
            var after = State.GrowthLevels;
            return string.Join("・", Enumerable.Range(0, 4).Where(i => after[i] > before[i]).Select(i =>
                (i == 0 ? "担当者" : OpsGrowthCatalog.StaffNames[i - 1]) + " Lv." + before[i] + " → " + after[i]));
        }

        private void TeamDialog()
        {
            if (State.growthRules == 0)
            {
                Dialog("旧年度の記録", "この年度のルールは保存時のままです。設備・担当者・社員の育成はニューゲームで有効になります。過去の経験値は後付けしません。", 400);
                return;
            }
            var d = Dialog("運用チーム / 育成と支援方針", "調査・対話・業務確認で、担当者と関わった社員が成長。\n共同練習は1工数・月1回。重要な判断はあなたが担当します。", 820);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta = new Vector2(748, 60);
            var lead = Box(d, "PlayerGrowth", 32, 177, 752, 86, Ink);
            lead.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(lead, "PlayerLevel", "あなた / 情シス担当  Lv." + State.PlayerLevel, 16, 8, 460, 32, 24, PlayerColor);
            Text(lead, "PlayerExperience", OpsGrowthCatalog.ExperienceText(State.playerExperience, OpsGrowthCatalog.PlayerThresholds) +
                "   全対応の抑制力 +" + (State.PlayerLevel - 1) * OpsGrowthCatalog.PlayerPowerPerLevel, 16, 47, 490, 29, 18);
            Text(lead, "PlayerUnlock", State.PlayerLevel >= 2 ? "解放済み\n支援方針の指定" : "次の解放 Lv.2\n支援方針の指定", 535, 14, 198, 58, 18, PlayerColor);
            string[] jobs = {
                "Lv.2＋手順：日常対応 / 工数+1\n対話・手順導入で経験+1",
                "Lv.2：調査補助 / 抑制力+2\n調査・監視導入で経験+1",
                "Lv.2：復旧+3 / 依頼照合+2\n業務確認・復元訓練で経験+1"
            };
            for (int i = 0; i < 3; i++)
            {
                int member = i;
                var card = Box(d, "TeamMember" + i, 32 + 254 * i, 281, 244, 201, Panel, true);
                card.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                Text(card, "TeamRole" + i, OpsGrowthCatalog.StaffRoles[i], 14, 10, 216, 24, 15, Muted);
                Text(card, "TeamLevel" + i, OpsGrowthCatalog.StaffNames[i] + "  Lv." + State.StaffLevel(i), 14, 40, 216, 36, 27, StaffColor);
                Text(card, "TeamExperience" + i, OpsGrowthCatalog.ExperienceText(State.staffExperience[i], OpsGrowthCatalog.StaffThresholds), 14, 81, 216, 23, 17);
                var track = Box(card, "ExperienceTrack", 14, 110, 216, 5, Edge); track.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                float fill = State.StaffLevel(i) == 3 ? 1 : (State.staffExperience[i] - OpsGrowthCatalog.StaffThresholds[State.StaffLevel(i) - 1]) /
                    (float)(OpsGrowthCatalog.StaffThresholds[State.StaffLevel(i)] - OpsGrowthCatalog.StaffThresholds[State.StaffLevel(i) - 1]);
                if (fill > 0) Box(card, "ExperienceFill", 14, 110, 216 * fill, 5, StaffColor).GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                Text(card, "TeamJob" + i, jobs[i], 14, 119, 216, 40, 14, Muted);
                string block = State.PracticeBlock(i);
                Button(card, "Practice_" + i, block == "" ? "共同練習 / 経験+2" : block, 10, 163, 224, 34, () => PracticeWith(member), Edge, block == "");
            }
            Text(d, "TeamOrderHeading", string.IsNullOrEmpty(State.supportOrder) ? "今月の支援方針 / 確定は1回・工数不要" :
                "今月は「" + OpsGrowthCatalog.OrderName(State.supportOrder) + "」 / 来月変更できます", 32, 493, 750, 29, 19, Ink);
            string[] descriptions = { "相談内容に応じて調査／復旧を選ぶ", "社員の手順案内 / 今月の工数+1", "記録整理・照合 / 停止か限定対応に加算", "業務データ・再開確認 / 復旧対応に加算" };
            for (int i = 0; i < 4; i++)
            {
                string order = OpsGrowthCatalog.Orders[i], block = State.SupportBlock(order);
                string detail = block == "" || !string.IsNullOrEmpty(State.supportOrder) ? descriptions[i] : block;
                Button(d, "Support_" + order, OpsGrowthCatalog.OrderName(order) + "\n<size=14>" + detail + "</size>",
                    32 + i % 2 * 384, 532 + i / 2 * 61, 368, 54, () => SetSupport(order), State.supportOrder == order ? Mint : Edge, block == "");
            }
            Text(d, "TeamOrderHelp", "未指定ならおまかせ。日常対応を選んだ月は事件対応への加算なし。\n社員Lv.3＋手順で他の社員へ経験+1。佐伯Lv.3＋台帳＋手順で記録整理+2。\n教育の導入・強化は全員の経験+1。レベルはゲーム内の習熟度です。", 32, 665, 752, 72, 16, Ink);
        }
        private void PracticeWith(int member)
        {
            var stats = ReadStats(); var before = State.GrowthLevels;
            if (!State.Practice(member)) return;
            RecordStatChanges(stats); Save(); Render(); TeamDialog();
            string levelUp = LevelUpNotice(before);
            Toast(levelUp != "" ? "LEVEL UP / " + levelUp : "共同練習 完了 / " + OpsGrowthCatalog.StaffNames[member] + "の経験が増えました", true,
                levelUp != "" ? OpsCue.Growth : OpsCue.Action);
        }
        private void SetSupport(string order)
        {
            var stats = ReadStats();
            if (!State.AssignSupport(order)) return;
            RecordStatChanges(stats); Save(); Render(); TeamDialog();
            Toast("支援方針を確定 / " + State.SupportSummary, true, order == "routine" ? OpsCue.Growth : OpsCue.Action);
        }
        private void QuarterRewardDialog()
        {
            var d = Dialog("四半期の山場をクリア", "6・9・12月の完了報酬。次の整備期間に欲しいものを一つ選ぼう。\n翌月は季節負荷が下がります。3月は一年の総力対応です。", 480);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta = new Vector2(748, 89);
            Button(d, "Reward_budget", "改善予算 +" + (OpsGrowthCatalog.QuarterBudget+State.QuarterTrustBonus) + "万円\n<size=17>導入費・維持費に使える"+(State.QuarterTrustBonus>0?" / 信頼の恩恵 +1":"")+"</size>", 32, 223, 364, 93, () => TakeQuarterReward("budget"), Accent);
            Button(d, "Reward_capacity", "翌月の支援枠 +1工数\n<size=17>共同練習や整備をもう一つ"+(State.QuarterTrustBonus>0?" / 予算 +1万円":"")+"</size>", 420, 223, 364, 93, () => TakeQuarterReward("capacity"), Mint);
            Text(d, "QuarterRewardHelp", "どちらも1回限り。支援枠は翌月だけ有効で、繰り越せません。\n選ばず自動進行する場合の既定報酬は改善予算です。", 32, 336, 748, 57, 18, Ink);
        }
        private void TakeQuarterReward(string reward)
        {
            var previous=ReadStats();
            if (!State.ClaimQuarterReward(reward)) return;
            int trustBonus=State.rankQuarterBonusPaid;
            Save(); AdvanceMonth(previous);
            Toast("山場クリア報酬 / " + (reward == "budget" ? "改善予算 +" + OpsGrowthCatalog.QuarterBudget + "万円" : "今月の支援枠 +1工数")+(trustBonus>0?" / 信頼の臨時予算 +1万円":""), true, OpsCue.Growth);
        }

        private string PowerLine(OpsResponsePower p) => "設備 +" + p.equipment + "  / 自分 +" + p.player + "  / 社員 +" + p.staff;
        private void PowerReport(string response, OpsOutcome record = null)
        {
            var p = record == null ? State.ResponsePower(response) : record.power;
            if (p == null) { Dialog("加算の内訳", "以前の記録には、この加算の内訳がありません。", 320); return; }
            string title = record == null ? "対応前の加算 / " : "今回の加算 / ";
            title += response == "contain" ? "停止・隔離" : response == "scope" ? "限定対応" : "復旧優先";
            var d = Dialog(title, "抑制力は脅威を減らすゲーム内の値。成功率ではありません。\n被害軽減・停止短縮は別に計算します。設備Lvは導入段階の目安です。", 780);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta = new Vector2(748, 62);
            Text(d, "PowerBase", "対応の基本 " + p.basic + "  ＋  相談文化・現場確認 " + p.field, 32, 181, 752, 32, 22, Ink);
            string[] names = { "設備・運用", "あなたの熟練", "社員の支援" };
            int[] values = { p.equipment, p.player, p.staff };
            Color[] colors = { EquipmentColor, PlayerColor, StaffColor };
            var numbers = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                var step = Box(d, "PowerStep" + i, 32 + 254 * i, 229, 244, 107, Ink);
                step.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                Text(step, "PowerStepTitle" + i, names[i], 16, 12, 212, 28, 19, colors[i]);
                numbers[i] = Text(step, "PowerStepValue" + i, "+" + values[i], 16, 43, 212, 56, 43, colors[i]);
            }
            var totalCard = Box(d, "PowerTotalCard", 32, 354, 752, 84, Ink);
            totalCard.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(totalCard, "PowerTotalLabel", "合計抑制力", 18, 14, 275, 30, 23, Paper);
            var total = Text(totalCard, "PowerTotalValue", p.Total.ToString(), 525, 6, 205, 69, 52, Accent);
            Text(totalCard, "PowerTotalHint", record != null && record.benign ? "正常な活動だったため、攻撃の抑制には未使用" : "脅威から差し引く値 / 疲労による負担は別", 18, 49, 505, 26, 16, Muted);
            Text(d, "PowerSupport", p.support + (p.staff == 0 ? "\nこの対応への社員加算は0 / 未習熟・担当外・日常対応のいずれか" : "\n今回の重点配分で社員の助力が有効"), 32, 457, 752, 64, 19, Ink);
            string growth = record == null ? "現在のレベルで計算。解決後の成長は次の仕事から有効です。\n社員の成長・支援の指定は、オフィス上部の「運用チーム」へ。" : GrowthResultText(record);
            Text(d, "PowerGrowth", growth, 32, 538, 752, 94, 20, Ink);
            Text(d, "PowerResult", record == null ? "内訳は公開情報だけ。出来事の未確認の深刻度は表示しません。" :
                "確定結果 / 被害 " + record.loss + "万円・停止 " + record.downtime + "h・対応費 " + record.cost + "万円", 32, 653, 752, 38, 19, Ink);
            if (Application.isPlaying) StartCoroutine(PowerCountUp(p, numbers, total));
        }
        private IEnumerator PowerCountUp(OpsResponsePower p, TextMeshProUGUI[] numbers, TextMeshProUGUI total)
        {
            if (ReducedMotion) yield break;
            int sum = p.basic + p.field;
            total.text = sum.ToString();
            foreach (var n in numbers) n.text = "+0";
            int[] values = { p.equipment, p.player, p.staff };
            float started=Time.realtimeSinceStartup;
            for (int i = 0; i < values.Length; i++)
            {
                // 待ち時間を積み上げず、開始時刻からの期限にする。低FPSでも途中値を残さない。
                while(Time.realtimeSinceStartup-started<(i+1)*.18f)yield return null;
                if (total == null || !total.gameObject.activeInHierarchy || numbers[i] == null) yield break;
                numbers[i].text = "+" + values[i]; sum += values[i]; total.text = sum.ToString();
                if (values[i] > 0) PlayCue(OpsCue.Click);
            }
            for(float t=0;t<.18f;t+=Time.unscaledDeltaTime){if(total==null)yield break;total.transform.localScale=Vector3.one*(ReducedMotion?1:1+.1f*Mathf.Sin(t/.18f*Mathf.PI));yield return null;}
            if(total!=null)total.transform.localScale=Vector3.one;
        }
        private string GrowthResultText(OpsOutcome r)
        {
            var g = r.growth;
            if (g == null) return "この記録には社員・担当者の経験値の内訳がありません。";
            string result = g.playerXp == 0 && g.playerAfter == 5 ? "あなた Lv.5 / 習熟MAX" :
                "あなた 経験 +" + g.playerXp + " / Lv." + g.playerBefore + (g.playerAfter > g.playerBefore ? " → " + g.playerAfter + "  LEVEL UP" : "");
            for (int i = 0; i < 3; i++) if (g.staffXp[i] > 0)
                result += "\n" + OpsGrowthCatalog.StaffNames[i] + " 経験 +" + g.staffXp[i] + " / Lv." + g.staffBefore[i] + (g.staffAfter[i] > g.staffBefore[i] ? " → " + g.staffAfter[i] + "  LEVEL UP" : "");
            return result;
        }
        private void ReviewPower(Transform p, OpsOutcome r)
        {
            string label = r.power == null ? "加算の内訳 / 過去の記録なし" : "合計抑制力 " + r.power.Total + "  /  加算を見る >\n<size=15>" + PowerLine(r.power) + "</size>";
            Button(p, "ReviewPower", label, 24, 482, 550, 57, () => PowerReport(r.response, r), Edge);
        }
        private IEnumerator ReviewGrowthFeedback(OpsOutcome r, string levelUp)
        {
            var button = screen.Find("DecisionPanel/ReviewPower");
            var label = button == null ? null : button.GetComponentInChildren<TextMeshProUGUI>();
            if (r.power != null && label != null && !ReducedMotion)
            {
                int sum = r.power.basic + r.power.field;
                int[] additions = { r.power.equipment, r.power.player, r.power.staff };
                string[] sources = { "設備・運用", "担当者の熟練", "社員の支援" };
                for (int i = 0; i < 3; i++)
                {
                    sum += additions[i];
                    label.text = "抑制力 " + sum + "  <color=#70B4FF>＋" + additions[i] + " " + sources[i] + "</color>\n<size=15>" + PowerLine(r.power) + "</size>";
                    yield return new WaitForSecondsRealtime(.25f);
                    if (label == null) yield break;
                }
                label.text = "合計抑制力 " + r.power.Total + "  /  加算を見る >\n<size=15>" + PowerLine(r.power) + "</size>";
            }
            // 被害表示を成長通知で即座に隠さない。画面を進めた場合はNewScreenで中断する。
            if (levelUp != "")
            {
                yield return new WaitForSecondsRealtime(2.7f);
                if (modal == null) Toast("LEVEL UP / " + levelUp, true, OpsCue.Growth);
            }
        }
    }
}
