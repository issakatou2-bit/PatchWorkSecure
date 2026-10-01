using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private void GrowthPlan(int goal)
        {
            if(State.storyCalendarYear>=2){StoryGrowthPlan(goal);return;}
            string[] titles = { "復元を確かめる", "担当者が休める運用", "相談が集まる職場" };
            string[] descriptions = {
                "保存したデータを、実際に戻せる状態へ。バックアップと復元訓練を組み合わせます。",
                "自動化だけで任せきりにせず、止まった時に再開できる手順まで整えます。",
                "教育を入れるだけでなく、社員との対話を積み重ねます。間違いを責めない相談先を作ろう。"
            };
            string[] benefits = {
                "データ復元が必要な月の「復旧を優先」で連携。\n連携Lvごとに復旧対応力 +" + OpsCatalog.RestorePowerPerLevel + "、追加のデータ被害 -" + OpsCatalog.ChainLossPerLevel + "万円、停止 -" + OpsCatalog.ChainStopPerLevel + "h。損失・停止は0未満になりません。\n混雑・認証悪用・送金詐欺を解決する効果ではありません。",
                "性能・処理の障害で「復旧を優先」すると連携。\n連携Lvごとに再開対応力 +" + OpsCatalog.RestartPowerPerLevel + "、停止 -" + OpsCatalog.ChainStopPerLevel + "h。混雑時は定期処理を延期して受注処理を優先します。\n工数増加と月々の疲労軽減もあります。",
                "教育は相談文化を育て、対話は文化 +7・疲労 -3。\n相談文化65と教育導入で成長達成。文化は限定対応や、なりすましへの備えにも役立ちます。\n報告した社員のミスを責めるゲームにはしません。"
            };
            string[][] pairs = { new[] { "backup", "drill" }, new[] { "automation", "runbook" }, new[] { "education" } };
            int level = goal == 0 ? State.RestoreChain : goal == 1 ? State.RestartChain : State.Level("education") > 0 && State.culture >= 65 ? 1 : 0;
            var d = Dialog(titles[goal] + " / 連携計画", descriptions[goal], 710);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta = new Vector2(748, 78);
            var badge = Box(d, "ChainStatus", 32, 196, 752, 44, level > 0 ? Mint : Edge);
            badge.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(badge, "ChainStatusText", level > 0 ? "成立 / " + (goal == 2 ? "相談文化が定着" : "連携 Lv." + level) : "準備中 / 下の項目から計画を確認できます", 16, 7, 720, 30, 20, level > 0 ? Ink : Paper);
            for (int i = 0; i < pairs[goal].Length; i++)
            {
                int index = OpsCatalog.Index(pairs[goal][i]); var p = OpsCatalog.Projects[index];
                string progress = "Lv." + State.levels[index] + " / " + p.max;
                string cost = State.levels[index] == p.max ? "運用定着済み" : "次段階 " + State.Cost(index) + "万円・" + State.WorkCost(index) + "工数";
                Button(d, "ChainProject_" + p.id, p.name + "\n<size=17>" + progress + "\n" + cost + "</size>\n<size=14>導入・強化の詳細 ></size>",
                    32 + i * 388, 260, 364, 122, () => ProjectDialog(index), State.Level(p.id) > 0 ? Edge : Ink);
            }
            if (goal == 2)
                Button(d, "ChainTalk", "相談文化  " + State.culture + " / 65\n<size=17>社員との対話 +7 / 1工数</size>\n<size=14>今月の対話を実行する ></size>",
                    420, 260, 364, 122, () => ChooseAction("listen"), Edge, State.ActionBlock("listen") == "");
            var impact = Box(d, "ChainBenefit", 32, 407, 752, 158, Ink);
            impact.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(impact, "ChainBenefitText", benefits[goal], 18, 14, 716, 132, 21, Paper);
            Text(d, "ChainCaveat", (goal == 2 ? "文化は対話や毎月の教育の積み重ねでも育ちます。" : "連携Lvは組み合わせる2設備の低い方。") +
                "\n初めての成長達成で信頼 +4・年間 +30点。導入費・維持費は別です。", 32, 581, 748, 52, 17, Ink);
        }

        private void StoryGrowthPlan(int index)
        {
            var goal=State.GrowthGoals[index];var steps=State.GrowthSteps(goal);
            var d=Dialog(goal.name,goal.description,560);
            PText(d,"StoryGrowthProgress","達成した条件 "+steps.Count(done=>done)+" / "+steps.Length,32,190,748,36,22,PlanPink);
            var projects=new[]{goal.projectA,goal.projectB}.Where(id=>!string.IsNullOrEmpty(id)).ToArray();
            for(int i=0;i<projects.Length;i++)
            {
                string id=projects[i];int project=OpsCatalog.Index(id),required=i==0?goal.levelA:goal.levelB;
                PButton(d,"StoryGrowthProject"+i,OpsCatalog.Projects[project].name+"　Lv."+State.Level(id)+" / 必要 Lv."+required,32+i*384,244,364,86,()=>ProjectDialog(project),Color.white,PlanInk);
            }
            if(goal.culture>0)PButton(d,"StoryGrowthTalk","相談文化 "+State.culture+" / "+goal.culture+"　社員との対話",416,244,364,86,()=>ChooseAction("listen"),Color.white,PlanInk);
            if(goal.staffLevel>0)PText(d,"StoryGrowthStaff",(goal.allStaff?"社員3人とも":"社員の誰か1人")+" Lv."+goal.staffLevel+"以上\n"+string.Join("　",Enumerable.Range(0,3).Select(i=>OpsGrowthCatalog.StaffNames[i]+" Lv."+State.StaffLevel(i))),32,348,748,76,18,PlanInk);
            PText(d,"StoryGrowthReward","初めての達成で 信頼 +"+OpsCatalog.GrowthTrustReward+"・年間 +"+OpsCatalog.GrowthScoreReward+"点\n設備の導入費・維持費は別です。",32,442,748,65,17,PlanGray);
        }
        private void InvestmentReport(OpsOutcome r)
        {
            var d = Dialog("今月、役立った備え", "同じ出来事・対応・社員状態で比較。効果0は、この場面で金銭被害と停止に差がなかったという意味です。", 770);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta = new Vector2(748, 72);
            string[] stages = { "01 予防", "02 影響限定", "03 復旧の備え" };
            string[] values = r.benign ? new[] { "正常な活動", "広がりなし", "攻撃被害なし" } :
                new[] { "脅威 -" + r.prevention, "広がり -" + r.containment, "データ被害 -" + r.recovery + "万円" };
            Color[] colors = { Mint, Rose, Accent };
            for (int i = 0; i < 3; i++)
            {
                var step = Box(d, "EffectStage" + i, 32 + i * 254, 197, 244, 100, Ink);
                step.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                Text(step, "EffectStageTitle", stages[i], 12, 12, 220, 27, 18, colors[i]);
                Text(step, "EffectStageValue", values[i], 12, 48, 220, 41, 21, Paper);
            }
            Text(d, "EffectChain", string.IsNullOrEmpty(r.recoveryChain) ? "連携の追加効果なし / 未整備・対象外・別の重点配分・正常な活動" :
                r.recoveryChain + "が機能 / 復旧対応力に加え、被害 -" + r.chainLossReduction + "万円・停止 -" + r.chainDowntimeReduction + "h", 32, 316, 752, 49, 19, Ink);
            var list = Scroll(d, 32, 385, 752, 215);
            if (r.investmentEffects == null || r.investmentEffects.Count == 0)
                Text(list, "NoEffectRecord", r.investmentEffects == null ? "以前の記録には、設備別の内訳がありません。" : "今月は導入済みの設備・運用整備がありません。", 16, 14, 690, 70, 20);
            else foreach (var e in r.investmentEffects.OrderByDescending(e => e.avoidedLoss * 2 + e.avoidedDowntime))
            {
                var card = Box(list, "Effect_" + e.projectId, 0, 0, 720, 79, Panel);
                card.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 79;
                card.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                Text(card, "EffectName", OpsCatalog.Projects[OpsCatalog.Index(e.projectId)].name + " Lv." + e.level, 14, 8, 684, 29, 21);
                Text(card, "EffectValue", "この整備がなければ  被害 +" + e.avoidedLoss + "万円 / 停止 +" + e.avoidedDowntime + "h", 14, 42, 684, 29, 19,
                    e.avoidedLoss + e.avoidedDowntime > 0 ? Mint : Muted);
            }
            Text(d, "EffectCaveat", "各行は1設備だけを外した比較。連携があるため、足して総効果にはできません。\n工数・疲労・文化の過去の変化、来月の効果、維持費はこの比較に含みません。", 32, 618, 752, 60, 17, Ink);
        }
    }
}
