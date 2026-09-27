using System;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private string SituationEffect()
        {
            var s = State.Situation;
            if (State.situationRules == 0) return "旧ルールで続行 / 月ごとの追加効果なし";
            if (s.stopLossCap > 0) return State.situationPrepared ? "事前調整済み / 停止による追加損失なし" : "停止1hにつき追加損失1万円 / 最大" + s.stopLossCap + "万円";
            if (s.extraFatigue > 0) return "対応後の追加疲労 +" + State.SituationFatigue + (State.situationPrepared ? " / 事前調整済み" : " / 手順Lv.1ごとに4軽減");
            if (s.timeDiscount > 0) return "復旧分野の整備 -" + s.timeDiscount + "工数 / 最低1工数";
            if (s.costDiscount > 0) return "防御分野の導入・強化 -" + s.costDiscount + "万円 / 維持費は通常";
            return "追加負担なし / 調査と投資の配分を試そう";
        }
        private void SituationCard(Transform map)
        {
            bool risk = State.Situation.stopLossCap > 0 || State.SituationFatigue > 0;
            var b = Button(map, "OpenSituation", "", 18, 387, 628, 67, SituationDialog, Ink);
            var accent = Box(b.transform, "SituationRail", 0, 3, 4, 61, risk && !State.situationPrepared ? Coral : Sky);
            accent.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(b.transform, "SituationTitle", "社内事情 / " + (State.situationRules == 0 ? "通常運用" : State.Situation.title) + "   >", 16, 6, 595, 28, 20, risk && !State.situationPrepared ? Coral : Sky);
            Text(b.transform, "SituationEffect", SituationEffect(), 16, 36, 595, 25, 16, Paper);
        }
        private void SituationDialog()
        {
            var s = State.Situation;
            var d = Dialog("今月の社内事情 / " + (State.situationRules == 0 ? "通常運用" : s.title),
                State.situationRules == 0 ? "この年度は以前のルールで進めています。追加の社内事情はニューゲームから適用されます。" : s.description, 610);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta = new Vector2(748, 90);
            var effect = Box(d, "SituationImpact", 32, 218, 752, 88, Ink);
            effect.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(effect, "SituationImpactText", "今月の効果\n" + SituationEffect(), 18, 12, 716, 65, 23, Sky);
            Text(d, "SituationAdvice", s.extraFatigue > 0 ? "応援当番の調整は、通常の休息も兼ねます。疲労 -18と追加疲労の防止を1工数で実施。通常の休息ボタンからも同じ効果です。引継ぎ手順でも追加疲労を減らせます。" : s.advice, 32, 334, 748, 106, 23, Ink);
            Text(d, "SituationDuration", "今月限り。選ばず進むこともできます。\n追加損失は被害予測に含まれます。疲労は対応後に反映。", 32, 455, 748, 57, 17, Ink);
            if (State.phase != OpsPhase.Planning) return;
            if (!string.IsNullOrEmpty(s.action))
            {
                string block = State.ActionBlock("prepare");
                Button(d, "PrepareSituation", block == "" ? s.action + " / 1工数" : block, 32, 540, 544, 48,
                    () => ChooseAction("prepare"), Accent, block == "");
            }
            else if (!string.IsNullOrEmpty(s.group))
                Button(d, "SituationProjects", "対象の改善計画を見る", 32, 540, 544, 48, () => { filter = s.group; tab = 1; Render(); }, Accent);
        }
        private static string OutcomeSituation(OpsOutcome r)
        {
            var s = Array.Find(OpsCatalog.Situations, item => item.id == r.situationId);
            if (s == null) return "旧ルールの記録 / 追加効果なし";
            return s.title + " / " + (s.stopLossCap > 0 ? "追加損失 " + r.businessLoss + "万円（被害に含む）" :
                s.extraFatigue > 0 ? "追加疲労 +" + r.extraFatigue : "今月の整備支援が終了") + (r.situationPrepared ? " / 事前調整済" : "");
        }
    }
}
