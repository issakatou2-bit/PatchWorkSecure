using System;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private static readonly Color Sky = Hex("70B4FF"), Rose = Hex("43CEC6"), Lavender = Hex("ABA5FF");
        private static readonly string[] StatNames = { "予算", "工数", "業務の安定", "相談文化", "経営の信頼", "疲労" };
        private int[] statChanges = new int[6];
        private int[] ReadStats() => new[] { State.budget, State.capacity, State.stability, State.culture, State.trust, State.fatigue };
        private void RecordStatChanges(int[] before)
        {
            var after = ReadStats();
            for (int i = 0; i < after.Length; i++) statChanges[i] = after[i] - before[i];
        }
        private Color StatColor(int index) => new[] { Paper, Sky, Mint, Rose, Lavender, Coral }[index];
        private Color GroupColor(string group) => group == "protect" ? Sky : group == "recover" ? Mint : group == "people" ? Rose : Lavender;
        private bool StatWarning(int index) => index == 0 ? State.budget < 6 : index == 2 ? State.stability < 35 : index == 5 && State.fatigue >= 60;
        private string StatHint(int index)
        {
            switch (index)
            {
                case 0: return State.budget < 6 ? "要注意 / 対応費が不足" : "維持 " + State.Upkeep + "万円 / 月";
                case 1: return State.capacity == 0 ? "使い切り / 翌月に回復" : "残り " + State.capacity + "工数 / 今月限り";
                case 2: return State.stability < 35 ? "要注意 / 0で運営終了" : "安定 / 高いほど良い";
                case 3: return State.culture >= 65 ? "相談が定着 / 育成中" : "相談の習慣を育成中";
                case 4: return "次の月次予算 " + State.MonthlyGrant + "万円";
                default: return State.fatigue >= 60 ? "要休息 / 対応に影響" : "余裕あり / 低いほど良い";
            }
        }
        private void StatusCard(int index, float x)
        {
            var color = StatColor(index); bool warning = StatWarning(index);
            var button = Button(screen, "Stat_" + index, "", x, 14, 174, 94, () => StatusDetail(index), warning ? Color.Lerp(Panel, Coral, .15f) : Panel);
            var card = button.transform;
            var rail = Box(card, "StatCategory", 0, 5, 4, 84, color);
            rail.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(card, StatNames[index] + "Label", StatNames[index], 12, 7, 112, 23, 16, Paper);
            int value = ReadStats()[index];
            string amount = value + (index == 0 ? " <size=17>万円</size>" : index == 1 ? " <size=17>/ " + State.MaxCapacity + "</size>" : " <size=16>/ 100</size>");
            Text(card, StatNames[index] + "Value", amount, 12, 29, 152, 35, 29, warning ? Coral : Paper);
            if (statChanges[index] != 0)
            {
                int delta = statChanges[index]; bool good = index == 5 ? delta < 0 : delta > 0;
                Text(card, "StatDelta" + index, (delta > 0 ? "+" : "") + delta, 120, 7, 48, 23, 15, index == 1 ? Sky : good ? Mint : Coral);
            }
            if (index > 0)
            {
                int count = index == 1 ? State.MaxCapacity : 10;
                float unit = index == 1 ? 1 : 10, width = (150 - (count - 1) * 3) / (float)count;
                for (int i = 0; i < count; i++)
                {
                    var segment = Box(card, "StatTrack" + i, 12 + i * (width + 3), 65, width, 5, Edge);
                    segment.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                    float fill = Mathf.Clamp01((value - i * unit) / unit);
                    if (fill <= 0) continue;
                    var bar = Box(segment, "StatFill", 0, 0, width * fill, 5, warning ? Coral : color);
                    bar.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                }
            }
            Text(card, "StatHint" + index, StatHint(index), 12, 73, 155, 18, 13, warning ? Coral : Muted);
        }
        private void StatusDetail(int index)
        {
            string[] descriptions = {
                "設備の導入、毎月の維持、事件対応と被害の支払いに使います。マイナスのまま月を終えると運営終了です。\n\n月次予算 " + State.MonthlyGrant + "万円 − 維持費 " + State.Upkeep + "万円 = 次月の収支 " + (State.MonthlyGrant - State.Upkeep) + "万円。\n事件の被害と対応費はこの収支に含みません。",
                "調査・対話・休息は1工数。設備は1～2工数を消費します。残った工数は翌月に繰り越せません。\n\n翌月の回復量は " + State.MaxCapacity + "工数。自動化のレベルを上げると翌月から1ずつ増えます。合同メンテナンスの月は復旧分野の整備工数が減ります。",
                "業務が続けられている度合いです。事件の停止時間ぶん低下し、対応終了で4、翌月に3回復します（上限100）。\n\n0で運営終了。業務の優先度確認・冗長化・復元訓練などで停止時間を減らせます。",
                "社員が不安やミスを相談しやすい状態を表します。社員との対話で+7、教育の導入で+9。\n\n高いほど詐欺や共有ミスへの予防、対象を絞った対応に役立ちます。低い社員を責めるのではなく、相談できる仕組みを育てます。",
                "改善の実施・社内依頼・追加予算の約束・対応結果で変化します。\n\n月次予算は20＋信頼÷20（端数切捨て）。現在は " + State.MonthlyGrant + "万円です。\n提案後の整備を実施すると信頼+5、約束が未達だと-7。",
                "この数値だけは低いほど良い状態です。事件対応で増え、15ごとに脅威の計算へ1点の負担が加わります。現在の負担は " + State.fatigue / 15 + "点。\n\n休息で-18、社員との対話で-3。自動化と引継ぎ手順は毎月の疲労を軽減します。"
            };
            var d = Dialog(StatNames[index] + " / " + ReadStats()[index] + (index == 0 ? "万円" : ""), descriptions[index], 550);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta = new Vector2(748, 248);
            var color = Box(d, "StatDetailColor", 0, 0, 820, 5, StatColor(index));
            color.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(d, "StatDetailHint", "色は項目の種類、状態の文字は注意点を示します。\n上部の + / - は直前の行動による変化です。", 32, 373, 748, 68, 18, Ink);
            if (State.phase == OpsPhase.Planning)
            {
                string action = index == 5 ? "rest" : index == 3 ? "listen" : "";
                Button(d, "StatDetailAction", action == "rest" ? "休息する / 1工数" : action == "listen" ? "社員と話す / 1工数" : "改善計画を確認する",
                    32, 480, 544, 48, () => { if (action != "") ChooseAction(action); else { filter = "all"; tab = 1; Render(); } },
                    Accent, action == "" || State.ActionBlock(action) == "");
            }
        }
    }
}
