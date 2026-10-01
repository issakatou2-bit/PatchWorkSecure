using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private void AnnualScreen()
        {
            AnnualPresentationCanSkip=presentationVisits.ContainsKey("annual_entry");AnnualPresentationSkipped=false;RepeatDuration("annual_entry",1,1);
            ReportBackground(true);var summary=OpsAnnualSummary.From(State);
            if(State.IsClear)
            {
                var rays=IncidentShape(screen,"AnnualRays","rays",-470,-500,1600,1600,new Color(1,.824f,.247f,.18f));rays.pivot=new Vector2(.5f,.5f);rays.anchoredPosition=new Vector2(330,-300);Motion(rays,"rotate",40);
                Color[] colors={PlanPink,PlanBlue,Hex("ffd23f"),PlanMint,PlanPink};float[] periods={7,9,8,10,8.5f};float[] delays={0,1,2.5f,.5f,3};
                for(int i=0;i<5;i++){var conf=PCard(screen,"AnnualConfetti"+i,200+i*320,0,12,18,colors[i],12,false);Motion(conf,"petal",periods[i],delays[i]);}
            }
            PText(screen,"AnnualCategory","ANNUAL REPORT",60,32,900,26,12,PlanPink);
            PText(screen,"EndingTitle",State.IsClear?"一年の記録　年度クリア":"一年の記録　運営終了",60,59,1300,66,44);
            if(Story!=null)
            {
                string result=Story.records.Last().goalMet?"届いた":"届かない";
                PText(screen,"StoryGoalResult",Story.year+"年目の目標 "+Story.Goal+"以上："+result+(Story.finished?(Story.cleared?"  本編クリア・エンドレス解放":"  挑戦の終わり"):"  次の年度へ進めます"),60,119,1180,27,17,Story.cleared?PlanMint:PlanInk);
            }
            var logo=PImage(screen,"AnnualLogoWordmark",PlanningArt.logoWordmark,1250,18,300,115);logo.localEulerAngles=new Vector3(0,0,-2);
            PButton(screen,"Menu","設定",1460,836,90,40,Menu,Color.white,PlanInk);
            var rank=PCard(screen,"RankBadge",150,150,360,360,PlanPink,28);rank.localEulerAngles=new Vector3(0,0,-6);KitGradient(rank.GetComponent<Image>(),Hex("ff94ae"),Hex("f45a80"));
            rank.GetComponent<Image>().pixelsPerUnitMultiplier=Mathf.Max(.01f,PlanningArt.round28.border.x/60);
            var border=rank.gameObject.AddComponent<Outline>();border.effectColor=Color.white;border.effectDistance=new Vector2(8,-8);
            IncidentShape(rank,"RankStitch","dashed",16,16,328,328,new Color(1,1,1,.7f));Reveal(rank,State.history.Count*.08f+.45f,true);
            PText(rank,"RankHeading","運用ランク",0,46,360,44,26,Color.white,true,true);
            StyleAnnualRank(rank);
            var rankText=PText(rank,"CompanyRank",State.RankCode,0,103,360,225,State.RankCode=="SS"?150:200,Color.white,true,true);var shadow=rankText.gameObject.AddComponent<Shadow>();shadow.effectColor=State.RankCode=="SS"?Hex("986000"):Hex("c23a60");shadow.effectDistance=new Vector2(0,-8);
            PText(screen,"ScoreHeading","年間得点",110,540,440,32,16,PlanGray,true,true);
            ReportNumber(screen,"AnnualScoreValue",State.AnnualScore,"<size=26>点</size>",110,580,440,72,PlanInk);
            PButton(screen,"AnnualDetails","評価の内訳を見る",110,690,440,48,AnnualDetails,Color.white,PlanInk,20);
            var timeline=ReportPanel("AnnualTimeline",610,140,940,190,.04f);ReportHeading(timeline,"12 MONTHS","乗り越えた "+State.history.Count+"か月",PlanBlue);
            PText(timeline,"AnnualLossValue","累計被害 "+State.totalLoss+" 万円",465,19,206,30,15,PlanGray);
            PText(timeline,"AnnualStopValue","停止 "+State.totalDowntime+" 時間",688,19,230,30,15,PlanGray);
            for(int i=0;i<12;i++)
            {
                var record=State.history.FirstOrDefault(r=>r.month==i);string label=OpsAnnualSummary.MonthLabel(State,record);
                Color bg=label=="被害大"||label=="停止長"||label=="運営終了"?Hex("ffe9ee"):label=="山場突破"?Hex("fff6d6"):label=="達成"?Hex("e3faf3"):Hex("eef2f8");
                Color fg=label=="被害大"||label=="停止長"||label=="運営終了"?Hex("c23a60"):label=="山場突破"?Hex("7a5a00"):label=="達成"?Hex("1a7c63"):Hex("52607a");
                var cell=PCard(timeline,"AnnualMonth"+i,22+i*75.33f,67,67.33f,74,bg,16,false);if(record!=null)Reveal(cell,i*.08f);
                PText(cell,"AnnualMonthName"+i,OpsCatalog.Months[i].name,0,9,67,29,15,fg,true,true);PText(cell,"AnnualMonthResult"+i,label,0,40,67,25,12,fg,true,true);
            }
            // 盾の見出しをタイムラインの背景より後に描画し、白いパネルで隠さない。
            PeakAnnualMedals();
            var mvp=ReportPanel("AnnualMvp",610,356,301.33f,174,.12f);
            PText(mvp,"MvpCategory","MVP",20,14,260,26,12,PlanMint);PText(mvp,"MvpHeading","一番効いた備え",20,46,260,26,16,PlanGray);
            var best=summary.Mvp;
            PText(mvp,"MvpProject",best==null?"有効な備えの記録なし":OpsCatalog.Projects[OpsCatalog.Index(best.id)].name,20,77,260,48,22);
            PText(mvp,"MvpEffects",best==null?"比較は記録がある月のみ":best.activations+"回発動・被害 −"+best.loss+"万円\n停止 −"+best.downtime+"時間（個別比較）",20,126,260,42,14,Hex("1a7c63"));
            var team=ReportPanel("AnnualTeam",929.33f,356,301.33f,174,.16f);PText(team,"TeamCategory","TEAM",20,14,260,26,12,PlanBlue);PText(team,"TeamHeading","活躍した社員",20,46,260,26,16,PlanGray);
            int member=summary.StaffMvp;
            PText(team,"AnnualStaffName",member<0?"社員の支援記録なし":OpsGrowthCatalog.StaffNames[member]+"  Lv."+State.StaffLevel(member),20,77,260,48,22);
            PText(team,"AnnualStaffSupport",member<0?"支援が無い年度もそのまま表示":"支援 "+summary.staffSupport[member]+"回",20,128,260,32,15,Hex("1f6fb0"));
            var collection=ReportPanel("AnnualCollection",1248.67f,356,301.33f,174,.2f);PText(collection,"CollectionCategory","COLLECTION",20,14,260,26,12,PlanPink);PText(collection,"CollectionHeading","攻撃手口図鑑",20,46,260,26,16,PlanGray);
            PText(collection,"CollectionValue",summary.encountered.Count+" / "+OpsEventCatalog.Events.Length,20,77,260,39,22);ReportGauge(collection,"CollectionGauge",20,123,260,summary.encountered.Count/(float)OpsEventCatalog.Events.Length,PlanPink);
            PText(collection,"CollectionScope","この一年の遭遇 / 図鑑画面は準備中",20,139,260,25,12,PlanGray,false);
            var goals=ReportPanel("AnnualGoals",610,560,940,72,.24f);PText(goals,"GoalsCategory","GOALS",22,18,76,34,12,Hex("ca8900"));PText(goals,"GoalsCount","成長目標 "+State.milestones.Count+" / 3",108,18,164,34,17);
            for(int i=0;i<3;i++){string[] names={"戻せることを確かめた","ひとりで抱えない運用","相談が集まる職場"};bool achieved=State.milestones.Contains(names[i]);ReportChip(goals,"AnnualGoal"+i,(achieved?"":"未達：")+names[i],282+i*214,20,204,achieved?Hex("fff6d6"):Hex("eef2f8"),achieved?Hex("7a5a00"):PlanGray);}
            // 承認された70px下げを保ち、260px高で足元も画面内に収める。
            Portrait(screen,"NavigatorPortrait",1275,630,266,260,State.IsClear?"pose_jump":"pose_exhausted");
            ReportSpeech(State.IsClear?"一年、おつかれさま！\n会社の成長を振り返ってみよう。":"ここまでの対応、おつかれさま。\n次は何を備えるか、記録を見よう。",950,650,300,State.IsClear?"face_crying":"face_sad");
            PButton(screen,"EndingHistory","一年を振り返る",110,780,232.73f,62,History,Color.white,PlanInk,20);
            PButton(screen,"BackHome",Story!=null&&Story.CanAdvance?"年度替わりへ":"タイトルへ",358.73f,780,232.73f,62,()=>{if(Story!=null&&Story.CanAdvance){ReturnStoryOutcome();return;}SkipTutorialVisual();tutorialStep=-1;RenderHome();},Color.white,PlanInk,20);
            if(Story==null)PButton(screen,"ReplayYear","もう一年挑戦",607.46f,780,302.54f,62,()=>StartYear(Environment.TickCount),PlanPink,Color.white,20);
            else PButton(screen,Story.CanAdvance?"NextStoryYear":"StoryRecord",Story.CanAdvance?"次の年度へ":"挑戦の記録へ",607.46f,780,302.54f,62,()=>{if(Story.CanAdvance)NextStoryYear();else StoryRecord();},PlanPink,Color.white,20);
        }
        private void StoryRecord()
        {
            StopVoice();string rows=string.Join("\n",Story.records.Select(r=>r.year+"年目  "+r.rank+" / "+r.score+"点 / 被害 "+r.loss+"万円・停止 "+r.stop+"時間 / "+(r.goalMet?"目標達成":"目標未達")));
            string factor=string.IsNullOrEmpty(Story.earnedFactor)?"":OpsCatalog.Projects[OpsCatalog.Index(Story.earnedFactor)].name;
            var d=Dialog(Story.cleared?"3年の本編クリア":"挑戦の記録",rows+"\n\n因子："+factor+"（自動選択）\n次の挑戦の1年目からLv1。最大3枠。"+(Story.cleared?"\nエンドレスの解放を記録しました。本体は今後追加します。":""),560);
            PButton(d,"StoryTitle","タイトルへ",32,490,420,48,()=>{SkipTutorialVisual();tutorialStep=-1;RenderHome();},PlanPink,Color.white);
        }
    }
}
