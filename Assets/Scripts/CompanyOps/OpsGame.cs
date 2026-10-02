using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame : MonoBehaviour
    {
        public TMP_FontAsset Font, HeadingFont;
        public Sprite OfficeArt, PanelSprite;
        public NavigatorPersona Navigator;
        public Button ChoicePrefab;
        public RectTransform Surface;
        public static bool TestMode;
        public OpsState State { get; private set; }
        public string SaveWarning { get; private set; } = "";
        private OpsState saved;
        private OpsProgress savedProgress;
        public OpsStory Story {get;private set;}
        public OpsEndless Endless {get;private set;}
        public int RunYear=>Endless?.year??Story?.year??1;
        public OpsCareer Career {get;private set;}=new OpsCareer();
        private int tab;
        private string filter = "all";
        private string SavePath => Path.Combine(Application.persistentDataPath, "company-ops-year-v1.json");
        private void Start()
        {
            LoadFeedbackSettings(); ApplyScreenMode(); ReadSave(); RenderHome();
        }
        public void BuildPreview() => RenderHome();

        private void ReadSave()
        {
            if (TestMode) return;
            savedProgress=OpsSaveStore.ReadProgress(SavePath,out string warning);saved=savedProgress?.Current;
            if(savedProgress!=null)Career=savedProgress.career;SaveWarning=warning;
        }
        private void Save()
        {
            if (TestMode || State == null) return;
            var progress=ExportProgress();
            if(OpsSaveStore.WriteProgress(SavePath,progress,out string warning)){savedProgress=progress;saved=State;}
            SaveWarning = warning;
        }
        private void SaveCareer()
        {
            if(TestMode)return;
            var progress=State!=null?ExportProgress():savedProgress??new OpsProgress{careerOnly=true};progress.career=Career;
            if(OpsSaveStore.WriteProgress(SavePath,progress,out string warning)){savedProgress=progress;saved=progress.Current;}
            SaveWarning=warning;
        }
        public void StartYear(int seed){Story=null;Endless=null;EnterYear(new OpsState(seed,true),true);}
        public void StartStory(int seed){Endless=null;Career.startedStoryAttempts=Math.Max(Career.startedStoryAttempts,Career.finishedAttempts)+1;Story=OpsStory.Begin(seed,Career.factors);EnterYear(Story.state,true);}
        public bool StartEndless(int seed)
        {if(!Career.endlessUnlocked)return false;Story=null;Endless=OpsEndless.Begin(seed,Career.factors);EnterYear(Endless.state,true);return true;}
        private void EnterYear(OpsState next,bool tutorial)
        {storyAnnualDetails=false;CancelMinigame();StopVoice();SkipTutorialVisual();tutorialStep=-1;voiceScreenKey="";lastTutorialVoice="";rankVoicePending=false;resolutionActive=false;pendingRankBenefit="";statEffectPending=false;roomFilter="";budgetGainPending=false;rankBefore=rankAfter=null;rankedReports.Clear();workCompletePending=false;workCompleteMonth=-1;State=next;Career.PrepareMaximYear(State.seed,RunYear,tutorial);statChanges=new int[6];tab=0;Save();Render();if(tutorial)TutorialNewYear();}
        public OpsProgress ExportProgress()=>new OpsProgress{single=Story==null&&Endless==null?State:null,story=Story,endless=Endless,storyMode=Story!=null,endlessMode=Endless!=null,career=Career,careerOnly=State==null&&Story==null&&Endless==null};
        public bool RestoreProgress(OpsProgress progress)
        {if(progress==null||!progress.Valid())return false;Story=progress.story;Endless=progress.endless;Career=progress.career;if(progress.careerOnly){State=null;RenderHome();}else EnterYear(progress.Current,false);return true;}
        private void ContinueSaved()=>RestoreProgress(savedProgress??new OpsProgress{single=saved,career=Career});
        private void RecordStoryOutcome()
        {
            if(State!=null&&Career.RecordBoss(State,State.Latest))Save();
            if(Endless!=null){bool changedEndless=Endless.RecordYear();changedEndless|=Career.RecordEndless(Endless);if(changedEndless)Save();return;}
            if(Story==null||State.phase!=OpsPhase.Ended)return;
            bool changed=Story.RecordYear();
            if(Story.cleared&&!Career.endlessUnlocked){Career.endlessUnlocked=true;changed=true;}
            changed|=Career.RecordStoryTitles(Story);
            if(changed)Save();
        }
        public bool NextStoryYear()
        {if(Story==null||MinigameActive)return false;RecordStoryOutcome();if(!Story.AdvanceYear())return false;EnterYear(Story.state,false);BeginYearOpening();return true;}
        public bool NextEndlessYear()
        {if(Endless==null||MinigameActive)return false;RecordStoryOutcome();if(!Endless.AdvanceYear())return false;EnterYear(Endless.state,false);BeginYearOpening();return true;}
        public bool RetireEndless()
        {if(Endless==null||MinigameActive||!Endless.Retire())return false;Save();Render();return true;}
        public void OpenTab(int next) { if(MinigameActive)return;roomFilter="";tab = next; Render(); }
        public void ChooseAction(string action, string group = "recover")
        {
            if(MinigameActive || State==null)return;
            if(action=="audit"||action=="map")
            {
                if(State.ActionBlock(action)!="")return;
                OpsMinigame session=action=="audit"?(OpsMinigame)new OpsLogMinigame(State):new OpsBlockMinigame(State);
                OpenMinigame(session,"",s=>ApplyWorkAction(action,group,s.Score));return;
            }
            ApplyWorkAction(action,group,OpsCatalog.MinigameDelegateScore);
        }
        private void ApplyWorkAction(string action,string group,int score)
        {
            var origin=ActionOrigin("Action_"+action);
            var previous = ReadStats();
            var oldLevels = State.GrowthLevels;
            int before = State.milestones.Count;
            if (!State.Act(action, group,score)) return;
            RecordStatChanges(previous);
            string feedback = action == "audit" ? "現状調査 完了 / 見積もりの幅が狭まりました" :
                action == "listen" ? "社員との対話 / 相談文化が育ちました" :
                action == "map" ? "重要業務を確認 / 停止を短くする準備ができました" :
                action == "rest" ? "当番を調整 / 疲労が軽くなりました" :
                action == "prepare" ? "事前調整 完了 / 今月の追加負担に備えました" : "追加予算 獲得 / 今月の整備を約束しました";
            bool growth = State.milestones.Count > before;
            string levelUp = LevelUpNotice(oldLevels);
            Save(); Render(); Toast(levelUp != "" ? "LEVEL UP / " + levelUp : growth ? "成長達成 / " + State.milestones.Last() + "・年間 +30点" : feedback, true, growth || levelUp != "" ? OpsCue.Growth : OpsCue.Action);
            ActionStatParticles(origin);
            SpeakRankUp();
            // 調査は結果確定まで非同期。開始時でなく、反映後にガイドを進める。
            if(action=="proposal"&&State.SecretaryProposalBonus>0)QueueCompanionScene("budget");
            TutorialAction("Action_"+action);
            if(action=="rest"&&pendingVoice==null)foreach(var identity in screen.GetComponentsInChildren<OpsPortraitIdentity>())if(identity.name=="NavigatorPortrait")
            {identity.GetComponent<OpsPortraitAnimator>().ChangePose("pose_coffee");}
        }
        public void Buy(int index)
        {
            if(MinigameActive)return;
            var previous = ReadStats();
            var oldLevels = State.GrowthLevels;
            int before = State.milestones.Count;
            if (!State.Upgrade(index)) return;
            RecordStatChanges(previous);
            bool growth = State.milestones.Count > before;
            string levelUp = LevelUpNotice(oldLevels);
            Save(); Render(); Toast(levelUp != "" ? "LEVEL UP / " + levelUp : growth ? "成長達成 / " + State.milestones.Last() + "・年間 +30点" :
                OpsCatalog.AllProjects[index].name + " Lv." + State.levels[index] + " / 会社の備えを更新しました", true, growth || levelUp != "" ? OpsCue.Growth : OpsCue.Purchase);
            ShowInstallation(index);
            SpeakRankUp();
        }
        public void BeginIncident()
        {
            if(MinigameActive)return;
            var previous = ReadStats();
            int before = State.MissionCount;
            if (!State.BeginIncident()) return;
            RecordStatChanges(previous);
            Save(); Render();
            bool achieved = State.MissionCount > before;
            Toast(achieved ? "社内依頼 達成 / 信頼 +3・年間 +45点"+(State.missionBudgetPaid>0?"・予算 +"+State.missionBudgetPaid+"万円":"") : "状況発生 / 対応方針を選択", achieved, achieved ? OpsCue.Growth : OpsCue.Alert);
        }
        public void Resolve(string response)=>Resolve(response,OpsCatalog.MinigameDelegateScore,true);
        private void Resolve(string response,int score,bool delegated,int? recoveryScore=null,bool recoveryDelegated=true)
        {
            if(State==null||State.phase!=OpsPhase.Incident||!ResponseIds.Contains(response))return;
            var previous = ReadStats();
            var oldLevels = State.GrowthLevels;
            var estimate = State.Estimate(response);
            if (!(recoveryScore.HasValue?State.ResolveFinal(response,score,delegated,recoveryScore.Value,recoveryDelegated):State.Resolve(response,score,delegated))) return;
            CancelMinigame();
            RecordStatChanges(previous);
            Save(); resolutionEstimate = estimate; resolutionLevelUp = LevelUpNotice(oldLevels);
            resolutionActive = true; resolutionCount++; Render();
        }
        public void Next()
        {
            AdvanceMonth(ReadStats());
        }
        private void AdvanceMonth(int[] previous)
        {
            if (!State.NextMonth()) return;
            RecordStatChanges(previous); tab = 0; Save(); Render();
            if (State.phase == OpsPhase.Ended) Feedback(State.IsClear ? OpsCue.Clear : OpsCue.Failure);
            else Toast(State.Current.name + "の計画 / 月次予算・工数を更新しました", true, OpsCue.Month);
        }

        private void RenderHome()
        {
            CancelMinigame();
            StopVoice();
            resolutionActive = false;
            homeVisible = true; NewScreen(); SetMusic(Sounds == null ? null : Sounds.titleMusic);
            if(PlanningArt!=null){TitleScreen();return;}
            Art(screen, 615, -95, 1030, 1030);
            if(PlanningArt!=null) SeasonLayer(Rect(screen,"TitleSeason",615,0,985,900),985,900,saved==null?0:saved.month);
            var intro = Box(screen, "Welcome", 44, 54, 580, 786, Ink, true);
            Text(intro, "Eyebrow", "PATCHWORK SECURE  /  情シスの一年", 38, 34, 500, 34, 17, Accent);
            Text(intro, "Title", "情シスの一年", 38, 99, 510, 163, 53);
            Text(intro, "Intro", "社員45人の会社で情報システム部門を担当。\n予算と工数を割り振り、設備と社内の運用を整備しよう。\n目標は3月までの事業継続。", 38, 292, 500, 135, 25, Muted);
            Text(intro, "Loop", "相談・調査  →  設備投資  →  事件対応  →  月次評価", 38, 451, 500, 56, 20, Accent);
            Button(intro, "NewYear", "ニューゲーム", 38, 550, 504, 66, () =>
            {
                if (saved == null && string.IsNullOrEmpty(SaveWarning)) StartYear(Environment.TickCount);
                else
                {
                    var d = Dialog("新しい一年を始めますか？", "現在の試作の進行を置き換えます。既存のUnity版のセーブには影響しません。", 340);
                    Button(d, "ConfirmNewYear", "新しい一年を始める", 32, 270, 420, 48, () => StartYear(Environment.TickCount), Accent);
                }
            }, Accent);
            Button(intro, "ContinueYear", saved == null ? "続きの記録はありません" : saved.Current.name + " の記録から続ける", 38, 632, 504, 58,
                ContinueSaved, Edge, saved != null);
            Text(intro, "Disclaimer", "1年12か月 / 40種の出来事・18種の日常チケット\n公表事例を参考にした架空の会社と数値です。", 38, 712, 504, 58, 16, Muted);
            if (Navigator != null && Navigator.FaceNormal != null)
            {
                Portrait(screen, "HomePortrait", 594, 480, 270, 310);
                var greeting = Box(screen, "HomeGreeting", 878, 630, 390, 120, Paper, true);
                greeting.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                Text(greeting, "HomeGreetingName", "ひなた / 情シスパートナー", 18, 12, 350, 31, 18, Hex("245BA6"));
                Text(greeting, "HomeGreetingLine", "今月の相談、一緒に確認しよう！", 18, 52, 350, 54, 21, Ink);
            }
            Text(screen, "HomeFooter", "ねっとわーく商事  /  会社運営・セキュリティ育成シミュレーション", 674, 826, 866, 48, 21);
            Button(screen, "HomeSettings", "音・演出の設定", 1352, 28, 218, 48, Menu);
            if (SaveWarning != "") Text(screen, "SaveWarning", SaveWarning, 40, 866, 1500, 28, 18, Coral);
        }
        private void Render()
        {
            RecordStoryOutcome();
            homeVisible = false;
            SetMusic(Sounds == null ? null : State.phase == OpsPhase.Planning ? Sounds.planningMusic :
                State.phase == OpsPhase.Incident || ResolutionActive ? Sounds.incidentMusic : Sounds.reviewMusic);
            NewScreen();CheckDangerSignal();
            if (State.phase == OpsPhase.Planning) { PlanningScreen(); ScreenVoice();return; }
            if (State.phase == OpsPhase.Incident) { IncidentWorkspace(); ScreenVoice();return; }
            if (State.phase == OpsPhase.Review && resolutionActive) { ResolutionScreen(); return; }
            resolutionActive = false;
            if (State.phase == OpsPhase.Review) { MonthlyScreen();ScreenVoice();return; }
            if (State.phase == OpsPhase.Ended) { if(Endless!=null&&!storyAnnualDetails){if(Endless.CanAdvance)StoryRenewScreen();else EndlessEndScreen();return;}if(Story!=null&&!storyAnnualDetails){if(Story.CanAdvance){StoryRenewScreen();return;}if(!Story.cleared){StoryFailScreen();return;}StoryEndingScreen();return;}AnnualScreen();ScreenVoice();return; }
            Header();
            Sidebar(); Office();
            if (State.phase == OpsPhase.Ended) { Ending(); return; }
            var right = Box(screen, "DecisionPanel", 978, 120, 598, 744, Panel, true);
            if (State.phase == OpsPhase.Planning) Planning(right);
            else if (State.phase == OpsPhase.Incident) Incident(right);
            else Review(right);
            Text(screen, "SaveStatus", SaveWarning == "" ? "自動保存 / 試作 v0.9" : SaveWarning, 30, 874, 1510, 22, 14, SaveWarning == "" ? Muted : Coral);
        }
        private void Header()
        {
            Text(screen, "Brand", "PATCHWORK\n情シスの一年", 30, 20, 238, 64, 23, Accent);
            Text(screen, "PhaseName", State.phase == OpsPhase.Planning ? "計画  >  対応  >  月報" : State.phase == OpsPhase.Incident ?
                "計画済  >  対応中  >  月報" : State.phase == OpsPhase.Review ? "計画済  >  対応済  >  月報" : "年度の記録", 30, 83, 242, 23, 14, Muted);
            for (int i = 0; i < 6; i++) StatusCard(i, 294 + i * 186);
            Button(screen, "Menu", "記録\n設定", 1422, 20, 154, 86, Menu);
        }
        private void Sidebar()
        {
            var left = Box(screen, "CompanyGrowth", 24, 120, 254, 744, Panel, true);
            Text(left, "Month", State.Current.name + " / " + (State.month + 1) + "か月目", 20, 18, 218, 52, 32, Accent);
            Text(left, "Season", State.SeasonLabel + (State.SeasonPressure > 0 ? " / 負荷+" + State.SeasonPressure : ""), 20, 72, 218, 35, 15, State.QuarterPeak ? Coral : Muted);
            for (int i = 0; i < 12; i++)
            {
                var c = Box(left, "Month" + i, 20 + i % 4 * 54, 113 + i / 4 * 37, 46, 29, i == State.month ? Accent : i < State.month ? Edge : Ink);
                if (State.growthRules > 0 && i % 3 == 2)
                    Box(c, "PeakMark", 3, 25, 40, 3, Accent).GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                Text(c, "MonthLabel", OpsCatalog.Months[i].name, 5, 3, 39, 25, 15, i == State.month ? Ink : Muted);
            }
            Text(left, "MissionCounter", "社内依頼の達成  " + State.MissionCount + " / 12", 20, 220, 218, 24, 14, Accent);
            Text(left, "MaturityHeading", "設備 Lv." + State.EquipmentLevel + " / 導入 " + State.levels.Sum(), 20, 244, 218, 35, 19, EquipmentColor);
            Bar(left, "備え", State.Preparedness, 20, 293, 211, Mint);
            Bar(left, "立て直す力", State.Resilience, 20, 348, 211, Accent);
            Bar(left, "チームの力", State.Organization, 20, 403, 211, Rose);
            Text(left, "MilestoneHeading", "成長目標", 20, 472, 218, 33, 20);
            string[] goals = State.GrowthGoals.Select(g=>g.name).ToArray();
            string[] labels = { "復元の確認", "仕事を分担", "相談しやすい職場" };
            if(State.storyCalendarYear>=2)labels=goals;
            for (int i = 0; i < 3; i++)
            {
                bool done = State.milestones.Contains(goals[i]);
                int goal = i;
                Button(left, "Goal" + i, "<size=15>" + (done ? "達成 / " : "計画 / ") + labels[i] + "  ></size>",
                    16, 510 + i * 50, 222, 46, () => GrowthPlan(goal), done ? Edge : Ink);
            }
            Text(left, "PeakLegend", "月の下線 = 山場", 20, 660, 218, 24, 14, Muted);
            Button(left, "OpenGuide", "遊び方", 18, 688, 218, 40, Guide);
        }
        private void Office()
        {
            var map = Box(screen, "OfficeStage", 294, 120, 664, 470, Panel, true);
            var viewport = Rect(map, "OfficeViewport", 6, 6, 652, 458); viewport.gameObject.AddComponent<RectMask2D>();
            Art(viewport, 0, -80, 652, 652);
            if (State.phase == OpsPhase.Incident)
            {
                var crisisTint = Box(map, "CrisisTint", 6, 6, 652, 458, new Color(.22f, .04f, .04f, .23f));
                crisisTint.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                var crisisRail = Box(map, "CrisisRail", 6, 6, 652, 7, Coral);
                crisisRail.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            }
            var badge = Box(map, "OfficeBadge", 18, 16, 336, 56, Ink);
            Text(badge, "OfficeStatus", State.phase == OpsPhase.Incident ? "インシデント対応中" : "ねっとわーく商事 / 社員45人", 15, 14, 306, 36, 19, State.phase == OpsPhase.Incident ? Coral : Paper);
            Button(map, "OpenTeam", State.growthRules > 0 ? "運用チーム / あなた Lv." + State.PlayerLevel +
                "\n<size=14>小川 " + State.StaffLevel(0) + "  佐伯 " + State.StaffLevel(1) + "  森 " + State.StaffLevel(2) + "  ></size>" : "運用チーム / 旧年度", 368, 16, 278, 56, TeamDialog, Edge);
            MapPin(map, "復旧基盤", State.Level("backup") + State.Level("drill"), 250, 119, "backup", "recover");
            MapPin(map, "相談できる現場", State.Level("education"), 443, 258, "culture", "people");
            MapPin(map, "運用のしくみ", State.Level("automation") + State.Level("runbook"), 31, 300, "change", "operations");
            SituationCard(map);
            if (State.phase == OpsPhase.Review) OfficeOutcome(map, State.Latest);
            var message = Box(screen, "Navigator", 294, 610, 664, 254, Paper);
            Portrait(message, "NavigatorPortrait", 12, 16, 186, 222);
            Text(message, "NavigatorName", (Navigator != null ? Navigator.DisplayName : "ひなた") + " / 情シスパートナー", 216, 18, 420, 32, 20, Ink);
            string line = State.phase == OpsPhase.Planning ? State.lastMessage : State.phase == OpsPhase.Incident ?
                "広く止めれば被害は抑えやすいけど、正常な業務も止まるよ。今の設備で範囲を絞れるかな？" :
                State.phase == OpsPhase.Ended ? "年度の結果が出たよ。設備と運用の評価を確認しよう。" :
                State.Latest.loss == 0 ? "金銭被害はゼロ！ 停止時間と対応費も確認しておこう。" : "被害は" + State.Latest.loss + "万円。来月の予算で何を改善できるかな？";
            if (State.phase == OpsPhase.Planning)
            {
                Text(message, "NavigatorSpeech", line, 216, 54, 420, 79, 20, Ink);
                var mission = State.CurrentMission;
                State.MissionProgress(true, out int equipped, out int equipmentTotal);
                State.MissionProgress(false, out int checkedWork, out int fieldTotal);
                var board = Box(message, "MissionBoard", 208, 141, 438, 97, Ink);
                board.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                Text(board, "MissionTitle", "社内依頼 / " + mission.title, 12, 7, 415, 28, 17, Accent);
                Text(board, "MissionEquipment", "設備  " + equipped + "/" + equipmentTotal, 12, 42, 120, 27, 20, equipped == equipmentTotal ? Mint : Paper);
                Text(board, "MissionField", "現場  " + checkedWork + "/" + fieldTotal, 147, 42, 120, 27, 20, checkedWork == fieldTotal ? Mint : Paper);
                Button(board, "MissionDetails", "条件 >", 281, 39, 141, 40, EventBriefDialog, Edge);
            }
            else
            {
                Text(message, "NavigatorSpeech", line, 216, 64, 420, 114, 21, Ink);
                Button(message, "MonthlyHint", State.CurrentMissionCompleted ? "依頼達成 / 条件 >" : "今月の備え・知識 >",
                    216, 188, 420, 46, EventBriefDialog, Edge);
            }
        }
        private void MapPin(Transform parent, string name, int level, float x, float y, string term, string group)
        {
            var pin = Button(parent, "Pin_" + term, name + "\n" + (level > 0 ? "整備 Lv." + level : "未整備"), x, y, 183, 68,
                () => { if (State.phase == OpsPhase.Planning) OfficePinDialog(name, level, term, group); else Knowledge(term); },
                Ink);
            Box(pin.transform, "ReadinessRail", 0, 0, 4, 68, level > 0 ? Mint : Edge).GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        }
        private void OfficePinDialog(string name, int level, string term, string group)
        {
            string details = group == "recover" ? "分離バックアップ Lv." + State.Level("backup") + "  /  復元訓練 Lv." + State.Level("drill") :
                group == "people" ? "相談できる教育 Lv." + State.Level("education") :
                "自動化 Lv." + State.Level("automation") + "  /  引継ぎ手順 Lv." + State.Level("runbook");
            var dialog = Dialog(name + " / 現在 Lv." + level,
                details + "\n\n設備と現場の対応を組み合わせて、今月の社内依頼を進められます。\n改善計画で導入条件と効果を確認できます。", 440);
            Button(dialog, "OpenProjectFromOffice", "改善計画を見る", 32, 304, 336, 50,
                () => { filter = group; tab = 1; Render(); }, Accent);
            Button(dialog, "OpenKnowledgeFromOffice", "関連知識", 388, 304, 196, 50, () => Knowledge(term));
        }
        private void Planning(RectTransform panel)
        {
            string[] labels = { "今月の相談", "改善計画", "運用ノート" };
            for (int i = 0; i < 3; i++) { int j = i; Button(panel, "Tab" + i, labels[i], 16 + i * 188, 16, 180, 46, () => OpenTab(j), tab == i ? Accent : Edge); }
            if (tab == 0) Briefing(panel);
            else if (tab == 1) Projects(panel);
            else Notebook(panel);
            Text(panel, "AdvanceHint", State.capacity > 0 ? "残り " + State.capacity + "工数 / 改善計画にも使える" : "工数を使い切りました", 24, 638, 548, 30, 17, Muted);
            Button(panel, "AdvanceMonth", "今月の運用へ進む  →", 22, 683, 554, 44, () =>
            {
                if (State.capacity == 0) { BeginIncident(); return; }
                var d = Dialog("工数を残して進みますか？", "残り " + State.capacity + " 工数は翌月に繰り越せません。\n調査・対話・改善・休息に使うこともできます。", 360);
                Button(d, "ConfirmAdvance", "この計画で進む", 32, 290, 420, 48, BeginIncident, Accent);
            }, Accent);
        }
        private void Briefing(RectTransform p)
        {
            Text(p, "CaseTitle", State.Current.title, 24, 84, 550, 74, 28);
            Button(p, "OpenEventBrief", "ニュース・備え >", 24, 171, 268, 46, EventBriefDialog, Ink);
            Button(p, "ConsultationDetails", "相談・社員の声 >", 306, 171, 268, 46, ConsultationDetails, Ink);
            Text(p, "Boss", State.Current.person + "\n「" + State.Current.boss + "」", 24, 244, 550, 136, 22);
            ActionButton(p, "audit", "調査する", "見積もり・限定対応を改善", 24, 427);
            ActionButton(p, "listen", "社員と話す", "相談 +7 / 疲労 -3", 306, 427);
            ActionButton(p, "map", "業務を確認", "信頼 +4 / 停止 -2h", 24, 538);
            ActionButton(p, "rest", "休息する", "疲労 -18" + (State.Situation.extraFatigue > 0 ? " / 少人数の負担も防止" : ""), 306, 538);
        }
        private void ActionButton(Transform p, string id, string title, string effect, float x, float y)
        {
            var block = State.ActionBlock(id);
            Button(p, "Action_" + id, title + "  <size=15>1工数</size>\n<size=15>" + (block == "" ? effect : block) + "</size>", x, y, 268, 89, () => ChooseAction(id), Edge, block == "");
        }
        private void Projects(RectTransform p)
        {
            Text(p, "ProjectIntro", roomFilter==""?"設備・運用の導入と強化":RoomName(roomFilter)+"の設備・運用", 24, 78, 554, 32, 20);
            string[] keys = { "all", "protect", "recover", "people", "operations" }, titles = { "すべて", "防御", "復旧", "組織", "運用" };
            for (int i = 0; i < keys.Length; i++) { string key = keys[i]; Button(p, "Filter_" + key, titles[i], 24 + i * 111, 120, 103, 38, () => { roomFilter="";filter = key; Render(); }, filter == key ? Mint : Edge); }
            var list = Scroll(p, 24, 174, 550, 368);
            for (int i = 0; i < OpsCatalog.AllProjects.Length; i++)
            {
                if(!State.EquipmentAvailable(i))continue;
                int index = i; var project = OpsCatalog.AllProjects[i]; if (roomFilter!=""&&ProjectRoom(project.id)!=roomFilter||filter != "all" && project.group != filter) continue;
                var card = Box(list, "Project_" + project.id, 0, 0, 532, 147, Panel, true); card.gameObject.AddComponent<LayoutElement>().preferredHeight = 147;
                Text(card, "ProjectName", project.name + "  <color=#70B4FF>Lv." + State.levels[i] + "</color>", 16, 12, 502, 36, 21);
                Text(card, "ProjectDescription", project.desc, 16, 51, 502, 34, 16, Muted);
                string blocked = State.UpgradeBlock(i);
                var rail = Box(card, "ProjectCategory", 0, 6, 4, 135, GroupColor(project.group));
                rail.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                string benefit = State.Cost(i) < State.BaseCost(i) || State.WorkCost(i) < project.time ? " [今月の支援]" : "";
                Button(card, "Details_" + project.id, blocked == "" ? "計画を見る  / " + State.Cost(i) + "万円・" + State.WorkCost(i) + "工数" + benefit : blocked + " / 詳細", 16, 92, 502, 42, () => ProjectDialog(index), blocked == "" ? Edge : Ink);
            }
            Button(p, "Proposal", "根拠を示して追加予算を相談する  / 1工数", 24, 568, 550, 52, Proposal, Edge, State.ActionBlock("proposal") == "");
            if (State.proposed) Text(p, "Promise", "今月の約束 / " + GroupName(State.promiseGroup) + "を提案後に1段階整備", 24, 543, 550, 25, 15, Accent);
        }
        private void ProjectDialog(int index, string response = "scope")
        {
            if(!State.EquipmentAvailable(index))return;
            var p = OpsCatalog.AllProjects[index]; string block = State.UpgradeBlock(index);
            var after = State.PreviewUpgrade(index);
            var d = Dialog(p.name + " / Lv." + State.levels[index] + " → " + Math.Min(2, State.levels[index] + 1),
                p.effect + (State.Cost(index) < State.BaseCost(index) || State.WorkCost(index) < p.time ?
                    "\n今月の支援：通常 " + State.BaseCost(index) + "万円・" + p.time + "工数 → " + State.Cost(index) + "万円・" + State.WorkCost(index) + "工数" : ""), 720);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta = new Vector2(748, 84);
            var comparison = Box(d, "InvestmentComparison", 32, 200, 752, 176, Ink);
            comparison.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            string metrics = after == null ? block + "\n\n導入費 " + State.Cost(index) + "万円 / " + State.WorkCost(index) + "工数\n維持費 +" + p.upkeep + "万円 / 月" :
                "導入前 → 導入後\n手元予算  " + State.budget + " → " + after.budget + "万円    今月の工数  " + State.capacity + " → " + after.capacity +
                "\n毎月の維持費  " + State.Upkeep + " → " + after.Upkeep + "万円    翌月の工数  " + State.MaxCapacity + " → " + after.MaxCapacity +
                "\n備え  " + State.Preparedness + " → " + after.Preparedness + "    立て直す力  " + State.Resilience + " → " + after.Resilience + "    チームの力  " + State.Organization + " → " + after.Organization;
            Text(comparison, "InvestmentNumbers", metrics, 18, 16, 716, 148, 22, Mint);
            string[] responses = { "contain", "scope", "recover" }, names = { "広範囲の停止", "対象を限定", "復旧を優先" };
            for (int i = 0; i < responses.Length; i++)
            {
                string selected = responses[i];
                Button(d, "Compare_" + selected, names[i], 32 + i * 254, 394, 244, 37, () => ProjectDialog(index, selected), response == selected ? Accent : Edge);
            }
            Text(d, "InvestmentForecast", after == null ? "先に条件を満たすと、導入前後の被害予測を比較できます。" :
                "今月・選択した重点配分の予測\n現在    " + State.Forecast(response) + "\n導入後  " + after.Forecast(response), 32, 447, 748, 87, 20, Ink);
            Text(d, "InvestmentCaveat", "予測は対応費を含みません。事件の種類や対応方針で効果は変わります。\n" +
                (after != null && after.budget < 6 ? "注意：手元予算が少なく、一部の対応費も賄えません。" : "今月効かない整備でも、別の脅威や来月以降に役立つことがあります。"), 32, 547, 748, 74, 18, Ink);
            Button(d, "Buy_" + p.id, "この整備を実施する", 32, 650, 350, 48, () => Buy(index), Accent, block == "");
            Button(d, "Learn_" + p.id, "知識を読む", 400, 650, 184, 48, () => Knowledge(p.term));
        }
        private static string GroupName(string group) => group == "protect" ? "防御" : group == "recover" ? "復旧" : group == "people" ? "組織" : "運用";
        private void Proposal()
        {
            var d = Dialog("追加予算の申請", "追加予算 +" + State.ProposalOffer + "万円 / 1工数\n今月、提案後に選んだ分野の整備が必要。\n達成で信頼 +5。未達は信頼 -7・交付額を全額返却。"+(State.SecretaryProposalBonus>0?"\nかのんの後押し / 山場の予算 +"+State.SecretaryProposalBonus+"万円を含む":""), 560);
            string[] groups = { "recover", "protect", "people", "operations" };
            for (int i = 0; i < groups.Length; i++)
            {
                string g = groups[i];
                bool possible = OpsCatalog.AllProjects.Select((p, j) => new { p, j }).Where(x=>State.EquipmentAvailable(x.j)).Any(x => x.p.group == g && State.levels[x.j] < 2 &&
                    State.capacity - 1 >= State.WorkCost(x.j) && State.budget + State.ProposalOffer >= State.Cost(x.j) &&
                    (string.IsNullOrEmpty(x.p.requires) || State.Level(x.p.requires) > 0));
                Button(d, "Propose_" + g, GroupName(g) + "を改善する" + (possible ? "" : " / 今月は工数等が不足"), 32, 260 + i * 51, 552, 44, () => ChooseAction("proposal", g), Edge, possible);
            }
        }
        private void Notebook(RectTransform p)
        {
            Text(p, "NotebookTitle", "運用・セキュリティの知識", 24, 84, 550, 40, 27);
            Text(p, "NotebookIntro", "読むための工数は不要。数値はゲーム用の簡略モデル。", 24, 136, 550, 56, 18, Muted);
            if(State.yearGrowthRules>0||(Career.defeatedBosses?.Count??0)>0)
                Button(p,"BossArchive","強敵の記録 / "+(Career.defeatedBosses?.Count??0)+"体",24,640,550,48,BossArchive,Edge);
            var list = Scroll(p, 24, 199, 550, 419);
            foreach (var term in OpsCatalog.Terms)
            {
                string id = term.id;
                var b = Button(list, "Term_" + id, (State.learned.Contains(id) ? "経験済 / " : "読む / ") + term.name, 0, 0, 532, 52, () => Knowledge(id), State.learned.Contains(id) ? Edge : Ink);
                b.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
            }
        }
        private void Knowledge(string id)
        {
            var t = OpsCatalog.Term(id); if (t == null) return;
            Dialog(t.name, t.basic + "\n\nもう一歩深く\n" + t.deep + "\n\n実際の対応手順・資格試験範囲のすべてを扱うものではありません。", 520);
        }
        private void Incident(RectTransform p)
        {
            IncidentComparison(p);
        }
        private void Review(RectTransform p)
        {
            var r = State.Latest;
            Text(p, "ReviewTag", "月次ふりかえり / " + State.Current.name, 26, 22, 550, 34, 20, Accent);
            Text(p, "ReviewTitle", r.loss == 0 ? "対応完了 / 金銭被害なし" : "対応完了 / 被害発生", 26, 76, 550, 85, 31);
            Text(p, "MissionResult", State.CurrentMissionCompleted ? "社内依頼を達成  +45点 / 経営の信頼 +3" : "社内依頼は未達成  /  来月の方針に活かそう", 26, 143, 550, 25, 17, State.CurrentMissionCompleted ? Mint : Muted);
            Text(p, "ReviewNumbers", "被害 " + r.loss + "万円  /  停止 " + r.downtime + "h\n対応費 " + r.cost + "万円", 26, 172, 550, 92, 29, r.loss == 0 ? Mint : Coral);
            var impact = Box(p, "InvestmentImpact", 24, 276, 550, 105, Ink);
            impact.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(impact, "ImpactNumbers", r.hasInvestmentComparison ? "設備・運用整備の効果\n被害 " + r.avoidedLoss + "万円 / 停止 " + r.avoidedDowntime + "h を削減" : "この記録には整備効果の比較がありません", 14, 10, 522, 63, 23, Mint);
            Text(impact, "ImpactBasis", string.IsNullOrEmpty(r.recoveryChain) ? "同じ対応・社員状態で、全整備Lv.0の場合と比較" :
                r.recoveryChain + "が機能 / 下の「効いた整備」で詳しく確認", 14, 77, 522, 23, 15, string.IsNullOrEmpty(r.recoveryChain) ? Muted : Accent);
            Button(p, "ReviewDetails", "対応の根拠・経験・収支 >", 24, 404, 550, 53, () => ReviewDetails(r), Edge);
            ReviewPower(p, r);
            string growth = State.Level("education") > 0 ? "社員の声 /「不安な時は、早めに相談していいんですね」" : "社員の声 /「次は、どこに相談すればいいか教えてください」";
            Text(p, "EmployeeGrowth", r.growth != null ? !string.IsNullOrEmpty(r.growth.mentoring) ? r.growth.mentoring :
                    "経験  あなた +" + r.growth.playerXp + " / 社員 +" + r.growth.staffXp.Sum() :
                !string.IsNullOrEmpty(r.situationId) && r.situationId != "normal" ? OutcomeSituation(r) : growth, 26, 545, 550, 46, 17, Muted);
            Button(p, "EffectDetails", "効いた整備・連携を見る", 24, 600, 320, 42, () => InvestmentReport(r), Edge);
            Button(p, "MonthlyLesson", "今回の知識", 358, 600, 216, 42, () => Knowledge(State.Current.lesson));
            Text(p, "NextBudget", State.month == 11 ? "12か月の結果へ" : "翌月の収支 " + (State.MonthlyGrant >= State.Upkeep ? "+" : "") + (State.MonthlyGrant - State.Upkeep) + "万円", 26, 647, 550, 30, 18, Muted);
            Button(p, "NextMonth", State.QuarterRewardPending ? "山場クリア / 報酬を選んで来月へ" :
                State.month == 11 || State.budget < 0 || State.stability == 0 ? "一年の記録を見る" : "来月へ / " + OpsCatalog.Months[State.month + 1].name, 24, 683, 550, 44,
                () => { if (State.QuarterRewardPending) QuarterRewardDialog(); else Next(); }, Accent);
        }
        private void Ending()
        {
            var p = Box(screen, "AnnualReport", 294, 120, 1282, 744, Paper);
            Text(p, "EndingTag", "年度の結果", 42, 30, 900, 36, 20, Ink);
            Text(p, "EndingTitle", State.IsClear ? "12か月 クリア！" : "運営終了", 42, 84, 930, 76, 43, Ink);
            var rank = Box(p, "RankBadge", 966, 34, 264, 187, Ink);
            Text(rank, "RankHeading", "運用ランク", 16, 8, 232, 26, 18, Muted);
            var rankText = Text(rank, "CompanyRank", State.RankCode, 16, 33, 232, 86, 72, Accent);
            rankText.alignment = TextAlignmentOptions.Center;
            Text(rank, "AnnualScoreValue", State.AnnualScore + " 点", 24, 126, 218, 40, 29);
            Text(p, "AnnualNumbers", "乗り越えた月  " + State.history.Count + " / 12", 42, 181, 874, 42, 28, Ink);
            AnnualMetric(p, "AnnualLoss", "累計被害", State.totalLoss + " 万円", 42, 262, Coral);
            AnnualMetric(p, "AnnualStop", "累計停止", State.totalDowntime + " h", 442, 262, Accent);
            AnnualMetric(p, "AnnualBudget", "残った予算", State.budget + " 万円", 842, 262, State.budget < 0 ? Coral : Mint);
            AnnualMetric(p, "AnnualMission", "社内依頼", State.MissionCount + " / 12", 42, 393, Mint);
            AnnualMetric(p, "AnnualGrowth", "成長目標", State.milestones.Count + " / 3", 442, 393, StaffColor);
            AnnualMetric(p, "AnnualKnowledge", "経験した知識", State.learned.Count + " 種", 842, 393, Accent);
            Button(p, "AnnualDetails", "評価の内訳 >", 42, 548, 360, 54, AnnualDetails, Edge);
            Button(p, "EndingHistory", "月ごとの記録 >", 438, 548, 360, 54, History, Edge);
            Button(p, "BackHome", "タイトルへ", 842, 650, 360, 56, RenderHome, Accent);
        }
        private void DiagnosticMenu()
        {
            var d = Dialog("記録と設定", "操作と結果には別々の効果音。演出中も操作できます。\n" +
                (homeVisible ? "設定は次回起動時も引き継ぎます。" : SaveWarning == "" ? "進行は行動ごとに自動保存済み。" : SaveWarning), 760);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta = new Vector2(748, 84);
            Button(d, "ToggleSound", muted ? "音声 / 消音中" : "音声 / 有効", 32, 211, 350, 48, () =>
            {
                muted = !muted;
                if (muted) { if (buttonAudio != null) buttonAudio.Stop(); if (eventAudio != null) eventAudio.Stop(); SetPresentationVolume(0); StopVoice(); }
                StoreFeedbackSettings(); DiagnosticMenu();
            });
            Button(d, "ReduceMotion", ReducedMotion ? "動きを減らす / 有効" : "動きを減らす / 無効", 410, 211, 350, 48,
                () => { ReducedMotion = !ReducedMotion; StoreFeedbackSettings(); DiagnosticMenu(); });
            Button(d, "DiagnosticSoundVolume", "効果音 " + Mathf.RoundToInt(soundVolume * 100) + "% / 変更", 32, 274, 350, 48,
                () => { soundVolume = soundVolume >= .99f ? .2f : Mathf.Min(1, soundVolume + .2f); StoreFeedbackSettings(); DiagnosticMenu(); PlayCue(OpsCue.Action); });
            Button(d, "DiagnosticMusicVolume", "BGM " + Mathf.RoundToInt(musicVolume * 100) + "% / 変更", 410, 274, 350, 48,
                () => { musicVolume = musicVolume >= .99f ? 0 : Mathf.Min(1, musicVolume + .2f); StoreFeedbackSettings(); DiagnosticMenu(); });
            Button(d, "VoiceToggle", VoiceEnabled ? "反応ボイス / 有効" : "反応ボイス / 無効", 32, 338, 350, 48,
                () => { VoiceEnabled = !VoiceEnabled; if (!VoiceEnabled) StopVoice(); StoreFeedbackSettings(); DiagnosticMenu(); });
            Button(d, "DiagnosticVoiceVolume", "ボイス " + Mathf.RoundToInt(voiceVolume * 100) + "% / 変更", 410, 338, 350, 48,
                () => { voiceVolume = voiceVolume >= .99f ? 0 : Mathf.Min(1, voiceVolume + .1f); if (voiceAudio != null) voiceAudio.volume = voiceVolume; if (voiceVolume <= 0) StopVoice(); StoreFeedbackSettings(); DiagnosticMenu(); });
            Text(d, "VoiceStatus", ReactionBank != null && ReactionBank.HasAudio ? "Hinata V9-2 / ローカル試遊用・配布と公開は不可" :
                "ひなたの声は音源未投入 / 台本v2の字幕で進行", 32, 402, 748, 32, 18, Ink);
            int musicCount = Sounds == null ? 0 : new[] { Sounds.titleMusic, Sounds.planningMusic, Sounds.incidentMusic, Sounds.reviewMusic }.Where(c => c != null).Distinct().Count();
            Text(d, "AudioStatus", musicCount > 0 ? "BGM " + musicCount + "曲 / 場面に応じて切り替え" : "効果音は試作の合成音。BGM素材は未設定です。", 32, 446, 748, 38, 18, Ink);
            Button(d, "PreviewSuccess", "試聴 / 達成", 32, 500, 232, 46, () => PlayCue(OpsCue.Growth));
            Button(d, "PreviewAlert", "試聴 / 警告", 280, 500, 232, 46, () => PlayCue(OpsCue.Alert));
            Button(d, "PreviewDamage", "試聴 / 被害", 528, 500, 232, 46, () => PlayCue(OpsCue.Damage));
            Button(d, "ViewHistory", "月ごとの記録", 32, 570, 350, 48, History, Edge, !homeVisible && State != null);
            Button(d, "PreviewVoice", "試聴 / 反応ボイス", 410, 570, 350, 48, () => React(OpsCue.Success), Edge,
                ReactionBank != null && ReactionBank.HasAudio && VoiceEnabled && !muted && voiceVolume > 0);
            Text(d, "VoicePreviewCaption", !CaptionsEnabled?"字幕はOFF":LastReactionCaption == "" ? "試聴の字幕はここに表示" : LastReactionCaption, 32, 631, 748, 42, 22, Ink);
            if (!homeVisible) Button(d, "Home", "保存してタイトルへ", 32, 685, 420, 48, () => { Save(); RenderHome(); }, Accent);
            if (!homeVisible && State != null && State.phase == OpsPhase.Planning) PlanningMenuLinks(d);
        }
        private void History()
        {
            var d = Dialog("一年の運用記録", "月別の出来事・被害額・業務停止時間", 660);
            var content = Scroll(d, 32, 175, 752, 383);
            foreach (var r in State.history)
            {
                string response = r.response == "contain" ? "広範囲を停止・隔離" : r.response == "scope" ? "対象を限定" : "代替業務・復旧";
                string result = OpsCatalog.Months[r.month].name + " / " + (string.IsNullOrEmpty(r.eventTitle) ? OpsCatalog.Months[r.month].@event : r.eventTitle) +
                    (State.completedMissions != null && State.completedMissions.Contains(r.month) ? "  社内依頼達成" : "") +
                    "\n対応 " + response + "  /  被害 " + r.loss + "万円・停止 " + r.downtime + "h" +
                    "\n" + (r.hasInvestmentComparison ? "整備効果  被害 -" + r.avoidedLoss + "万円・停止 -" + r.avoidedDowntime + "h" : "整備効果  過去の記録には比較なし") +
                    "\n今回の知識  " + OpsCatalog.Term(string.IsNullOrEmpty(r.lessonId) ? OpsCatalog.Months[r.month].lesson : r.lessonId).name +
                    (!string.IsNullOrEmpty(r.situationId) && r.situationId != "normal" ? "\n" + OutcomeSituation(r) : "") + "\n" + TicketRecord(r);
                var card = Box(content, "HistoryCard" + r.month, 0, 0, 720, 176, Panel, true);
                card.gameObject.AddComponent<LayoutElement>().preferredHeight = 176;
                card.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                Text(card, "History" + r.month, result, 14, 10, 688, 156, 17);
            }
        }
        private void Guide() => Dialog("遊び方", "1. 相談とニュースを読み、今月の優先順位を決める。\n2. 工数を使って調査・対話・休息。改善計画から導入する。\n3. 社内依頼は設備と現場の二つの道から選べる。達成すると信頼と年間得点が増える。\n4. 余裕があれば根拠付きの追加予算を提案する。\n5. 出来事に対応し、効いた備えと不足を振り返る。\n\n目標は4月から3月まで事業を継続すること。予算がマイナス、または業務の安定が0になるとゲームオーバー。\n自動化の工数増加は翌月から。維持費と対応費を残そう。", 640);
    }
}
