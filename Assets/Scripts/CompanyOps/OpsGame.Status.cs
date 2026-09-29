using System;
using System.Linq;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private static readonly Color Sky = Hex("70B4FF"), Rose = Hex("43CEC6"), Lavender = Hex("ABA5FF");
        private static readonly string[] StatNames = { "予算", "工数", "業務の安定", "相談文化", "経営の信頼", "疲労" };
        private int[] statChanges = new int[6];
        private bool expectedResourceSpend;
        private string pendingRankBenefit="";
        private bool statEffectPending;
        private int[] ReadStats()
        {
            rankBefore=CompanyRankMetrics();
            return new[] { State.budget, State.capacity, State.stability, State.culture, State.trust, State.fatigue };
        }
        private void RecordStatChanges(int[] before)
        {
            var previousRanks=rankBefore;
            var after = ReadStats();
            rankBefore=previousRanks;
            for (int i = 0; i < after.Length; i++) statChanges[i] = after[i] - before[i];
            budgetGainPending=statChanges[0]>0;
            rankAfter=CompanyRankMetrics();
            statEffectPending=true;
            foreach(int i in new[]{3,4})if(PlanningRank(after[i])!=PlanningRank(before[i])&&after[i]>before[i])
                pendingRankBenefit=StatNames[i]+" "+PlanningRank(before[i])+" → "+PlanningRank(after[i])+"  / "+RankUnlock(i,after[i]);
            workCompletePending=before[1]>0&&after[1]==0&&workCompleteMonth!=State.month;
            // 購入・作業の支出と、事件の損失を同じ「失敗の赤」にしない。
            expectedResourceSpend = State.phase == OpsPhase.Planning ||
                State.phase == OpsPhase.Review && State.Latest != null && State.Latest.loss == 0;
        }
        private Color DeltaColor(int index, int delta) => index == 1 || index == 0 && delta < 0 && expectedResourceSpend ? Sky :
            (index == 5 ? delta < 0 : delta > 0) ? Mint : Coral;
        private Color StatColor(int index) => new[] { Paper, Sky, Mint, Rose, Lavender, State.fatigue >= 60 ? Coral : Sky }[index];
        private Color GroupColor(string group) => group == "protect" ? Sky : group == "recover" ? Mint : group == "people" ? Rose : Lavender;
        private bool StatWarning(int index) => index == 0 ? State.budget < 6 : index == 2 ? State.stability < 35 : index == 5 && State.fatigue >= 60;
        private string StatHint(int index)
        {
            switch (index)
            {
                case 0: return State.budget < 6 ? "要注意 / 対応費が不足" : "維持 " + State.Upkeep + "万円 / 月";
                case 1: return State.capacity == 0 ? "使い切り" : "今月の行動回数";
                case 2: return State.stability < 35 ? "要注意 / 0で終了" : "安定";
                case 3: return State.culture >= 65 ? "相談が定着" : "";
                case 4: return "次の月次予算 " + State.MonthlyGrant + "万円";
                default: return State.fatigue >= 60 ? "要休息 / 対応に影響" : "";
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
                int delta = statChanges[index];
                Text(card, "StatDelta" + index, (delta > 0 ? "+" : "") + delta, 120, 7, 48, 23, 15, DeltaColor(index, delta));
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
            if(index==3||index==4){RankStatusDetail(index);return;}
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
            if(index==2&&statChanges[2]<0)
            {
                PCard(d,"StabilityDetailTrack",32,450,748,8,PlanTrack,12,false);
                LossTrail(d,"StabilityDetailLossTrail",32,450,748,8,State.stability-statChanges[2],State.stability,100);
                PCard(d,"StabilityDetailFill",32,450,748*State.stability/100f,8,PlanMint,12,false);
            }
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
        private string RankUnlock(int index,int value)=>State.rankBenefitRules==0?"従来の運用効果が成長":value>=80?"C・Bの恩恵を維持 / 基礎効果が成長":index==3?value>=65?"相談報告で見積もり精度UP":value>=50?"社員から次月の兆候が届く":"相談・限定対応の加算が成長":value>=65?"四半期の臨時予算 +1万円":value>=50?"提案予算に +1万円":"月次予算と改善の信頼が成長";
        private void RankStatusDetail(int index)
        {
            int value=index==3?State.culture:State.trust;bool old=State.rankBenefitRules==0;
            string summary=index==3?"社員の相談・報告が、予防と対応の判断を支えます。":"経営の信頼が、提案と月次予算の後押しになります。";
            var d=Dialog(StatNames[index]+" / "+PlanningRank(value)+"ランク  "+value+" / 100",summary+(old?" この年度は従来の効果を維持します。":""),620);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta=new Vector2(748,56);
            string current=index==3?"詐欺・情報の扱いへの予防と限定対応に加算。"+(old?"\n旧年度：追加の兆候・精度の恩恵なし。":value>=65?"\nC：次月の兆候を表示。B：未調査の見積もり幅を1狭める。\n調査済みなら追加の精度加算なし。":value>=50?"\nC：次月の兆候を社員が報告（侵害確定ではない）。":"\n対話 +7、教育の導入 +9で文化が育つ。"):
                "現在の月次予算 "+State.MonthlyGrant+"万円。"+(old?"\n旧年度：追加の提案・四半期加算なし。":value>=65?"\nC：根拠のある提案 +1万円。B：四半期の予算 +1万円。\n提案の約束が未達なら上乗せも返却。":value>=50?"\nC：根拠のある提案に +1万円（約束未達で返却）。":"\n改善・依頼・約束の実行で信頼が育つ。");
            string next=old?"追加の恩恵は新しい年度で有効。":value<50?"C（50）まで あと"+(50-value)+"。\n"+(index==3?"次月の兆候を社員が報告。":"根拠のある提案予算に +1万円。"):
                value<65?"B（65）まで あと"+(65-value)+"。\n"+(index==3?"未調査の見積もり幅を1狭める。":"四半期の臨時予算 +1万円。"):
                "C・Bの恩恵は解放済み。\n"+(index==3?"この先は予防・限定対応への加算が数値に応じて育ちます。":"この先は月次予算が信頼の数値に応じて増えます。");
            PText(d,"CurrentRankBenefitHeading","今の効果",32,208,748,28,16,PlanPink);PText(d,"CurrentRankBenefit",current,32,244,748,122,20,PlanInk,false);
            PText(d,"NextRankBenefitHeading","次のランク",32,382,748,28,16,PlanBlue);PText(d,"NextRankBenefit",next,32,418,748,102,20,PlanInk,false);
            Button(d,"StatDetailAction",index==3?"社員と話す / 1工数":"改善計画を確認する",32,550,544,48,()=>{if(index==3)ChooseAction("listen");else{filter="all";OpenTab(1);}},Accent,State.phase==OpsPhase.Planning&&(index!=3||State.ActionBlock("listen")==""));
        }
        private Vector2 ActionOrigin(string id)
        {
            var button=screen.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.name==id);if(button==null)return new Vector2(1000,-780);
            var rect=(RectTransform)button.transform;return screen.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
        }
        private void ActionStatParticles(Vector2 origin)
        {
            foreach(int i in new[]{3,4,5})
            {
                if(i==5?statChanges[i]>=0:statChanges[i]<=0)continue;
                var target=screen.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.name=="Stat_"+i);if(target==null)continue;
                var rect=(RectTransform)target.transform;Vector2 end=screen.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
                for(int n=0;n<3;n++){var spark=PCard(screen,"ActionStatParticle"+i+"_"+n,origin.x,-origin.y,12,12,i==3?PlanPink:i==4?PlanPurple:PlanMint,12,false);
                    var move=spark.gameObject.AddComponent<OpsStatSpark>();move.Owner=this;move.From=origin;move.To=end;move.Delay=n*.06f;move.Stat=i;}
            }
        }
        private void RankBenefitBand()
        {
            statEffectPending=false;if(pendingRankBenefit==""||!Application.isPlaying)return;
            var band=PCard(screen,"RankBenefitBand",378,118,826,76,PlanPink,20,false);PText(band,"RankBenefitBandText",pendingRankBenefit,18,8,790,60,22,Color.white,true,true);
            band.gameObject.AddComponent<OpsBlockedTag>();Reveal(band);PlayPresentationCue(OpsCue.Growth);pendingRankBenefit="";
        }
    }
}
