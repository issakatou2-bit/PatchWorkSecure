using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private const string TutorialKey = "pws_ops_tutorial_seen";
        private int tutorialStep = -1, tutorialPurchases;
        private bool tutorialAwaitClose;
        private RectTransform tutorialRoot;
        private string tutorialTarget;
        public bool TutorialActive => tutorialStep >= 0;
        public int TutorialStep => tutorialStep;
        // テスト・設定からの再表示も同じ操作を通す。進行中の年度を巻き戻さない。
        public bool StartTutorial()
        {
            if (State == null || State.month != 0 || State.phase != OpsPhase.Planning || State.audited || State.capacity < 3) return false;
            tutorialStep = 0; tutorialAwaitClose = false; tutorialPurchases = State.levels.Sum(); tutorialTarget = null;
            filter="all";
            if(!TestMode){PlayerPrefs.SetInt(TutorialKey,1);PlayerPrefs.Save();}
            RefreshTutorial(); return true;
        }
        public void SkipTutorial()
        {
            tutorialStep = -1; tutorialAwaitClose = false;
            SkipTutorialVisual();
            if (!TestMode) { PlayerPrefs.SetInt(TutorialKey,1); PlayerPrefs.Save(); }
        }
        private void TutorialNewYear()
        {
            SkipTutorialVisual();
            tutorialStep = -1;
            if (!TestMode && PlayerPrefs.GetInt(TutorialKey,0) == 0) StartTutorial();
        }
        private void SkipTutorialVisual()
        {
            if(screen!=null)
            {
                var nav=screen.Find("Navigator");if(nav!=null)nav.gameObject.SetActive(true);
                var character=screen.Find("OfficeStage/PlanningCharacter");if(character!=null)character.gameObject.SetActive(true);
                var portrait=screen.Find("NavigatorPortrait");if(portrait!=null)portrait.gameObject.SetActive(true);
            }
            if (tutorialRoot != null) { tutorialRoot.gameObject.SetActive(false); Destroy(tutorialRoot.gameObject); }
            tutorialRoot = null; tutorialTarget = null;
        }
        partial void TutorialAction(string id)
        {
            if (!TutorialActive) return;
            if (tutorialStep == 0 && id.StartsWith("Stat")) tutorialAwaitClose = true;
            if (tutorialStep == 1 && id == "Action_audit" && State.audited) tutorialStep = 2;
            if (tutorialStep == 2 && id == "ConsultationDetails") tutorialAwaitClose = true;
            if (tutorialStep == 3 && State.levels.Sum() > tutorialPurchases) tutorialStep = 4;
            if (tutorialStep == 4 && State.phase == OpsPhase.Incident) tutorialStep = 5;
            tutorialTarget = null; RefreshTutorial();
        }
        partial void TutorialWindowClosed()
        {
            if (tutorialAwaitClose) { tutorialStep++; tutorialAwaitClose = false; }
            tutorialTarget = null;
        }
        partial void RefreshTutorial()
        {
            if (!TutorialActive || screen == null || homeVisible) return;
            AlignDialogFooter();
            if (State.month != 0 || State.phase == OpsPhase.Ended) { SkipTutorial(); return; }
            if (tutorialStep == 5 && State.phase == OpsPhase.Review)
            {
                SkipTutorialVisual();
                if (!ResolutionActive) SkipTutorial();
                return;
            }
            string target = tutorialAwaitClose ? "CloseDialog" : tutorialStep == 0 ? "Stat_0" : tutorialStep == 1 ? "Action_audit" :
                tutorialStep == 2 ? "ConsultationDetails" : tutorialStep == 4 ? (modal == null ? "AdvanceMonth" : "ConfirmAdvance") :
                tutorialStep == 5 ? "Respond_scope" : tab == 0 ? "OpenProjects" : modal != null && modal.Find("Dialog") != null ?
                    "Buy_" + OpsCatalog.Projects.FirstOrDefault(p => modal.GetComponentsInChildren<Button>().Any(b=>b.name == "Buy_"+p.id))?.id :
                    "Details_" + OpsCatalog.Projects.FirstOrDefault(p=>State.UpgradeBlock(OpsCatalog.Index(p.id))=="")?.id;
            // 描画し直した時は古いガイドも破棄される。毎フレーム生成しない。
            var button = screen.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == target && b.interactable);
            if (button == null)
            {
                // 対象が一時的に無い時でも、ガイドを終了する操作は失わない。
                SkipTutorialVisual();tutorialRoot=Rect(screen,"TutorialGuide",0,0,1600,900);
                PButton(tutorialRoot,"SkipTutorial","説明を飛ばす",1320,10,256,46,SkipTutorial,Color.white,PlanGray,16);return;
            }
            if (tutorialRoot != null && tutorialTarget == target) return;
            SkipTutorialVisual(); tutorialTarget = target;
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
            var a = screen.InverseTransformPoint(corners[1]); var btm = screen.InverseTransformPoint(corners[3]);
            float x=Mathf.Clamp(a.x-8,0,1600), y=Mathf.Clamp(-a.y-8,0,900), w=Mathf.Min(1600-x,btm.x-a.x+16), h=Mathf.Min(900-y,a.y-btm.y+16);
            tutorialRoot = Rect(screen,"TutorialGuide",0,0,1600,900);
            var normalNav=screen.Find("Navigator");if(normalNav!=null)normalNav.gameObject.SetActive(false);
            var normalCharacter=screen.Find("OfficeStage/PlanningCharacter");if(normalCharacter!=null)normalCharacter.gameObject.SetActive(false);
            var normalPortrait=screen.Find("NavigatorPortrait");if(normalPortrait!=null)normalPortrait.gameObject.SetActive(false);
            TutorialShade(0,0,1600,y); TutorialShade(0,y+h,1600,900-y-h); TutorialShade(0,y,x,h); TutorialShade(x+w,y,1600-x-w,h);
            var glow=IncidentShape(tutorialRoot,"TutorialPulse","dashed",x,y,w,h,Hex("ffd85c")); Motion(glow,"pulse",1.2f);
            string[] titles={"会社の状態を確認しよう","まずは現状を調べよう","社内の依頼を聞こう","設備をひとつ導入しよう","今月の運用へ進もう","対応方針を選ぼう"};
            string[] lines={"上の数値を押してみて！\n予算は導入と対応に使うお金。工数は今月できる作業の量だよ。会社の力は左にあるよ。",
                "「調べる」を押そう！\n1工数で調査できるよ。何が起きそうか、見積もりの幅が狭くなるんだ。",
                "「話を聞く」で社内の依頼を開こう！\n設備の導入か、現場の取り組みで達成できるよ。閉じたら次に進もう。",
                "「設備を導入」を開いて、整備をひとつ選んでみよう！\n費用と工数、導入前後の違いを確認してから決めてね。",
                "「月を進める」を押そう！\n工数を残して進めても大丈夫。確認画面で、この計画を決定しよう。",
                "今の備えで対応方針を選ぼう！\n被害と停止の見積もりを比べてね。選ぶと、設備や社員がどこで効いたか見られるよ。"};
            var header=PCard(tutorialRoot,"TutorialHeading",400,60,800,82,Color.white,28);
            var medal=PCard(header,"StepMedal",24,17,48,48,PlanPink,24,false);PText(medal,"StepNumber",(tutorialStep+1).ToString(),0,0,48,48,26,Color.white,true,true);
            PText(header,"StepLabel","STEP "+(tutorialStep+1)+" / 6",90,8,660,24,11,Hex("d94a70"));
            PText(header,"StepTitle",titles[tutorialStep],90,29,660,43,22);
            float bubbleY = tutorialStep==5 ? 160 : 380;
            Portrait(tutorialRoot,"TutorialPortrait",240,340,408,560,"pose_point");
            var bubble=PCard(tutorialRoot,"TutorialSpeech",660,bubbleY,560,280,Color.white,28);
            var outline=bubble.gameObject.AddComponent<Outline>();outline.effectColor=PlanPink;outline.effectDistance=new Vector2(3,-3);
            PText(bubble,"TutorialName","ひなた  /  TUTORIAL",26,16,480,30,16,PlanPink);
            PText(bubble,"TutorialLine",tutorialAwaitClose?"内容を確認できたら「閉じる」を押してね！":lines[tutorialStep],26,62,508,138,22,null,false);
            for(int i=0;i<6;i++)PCard(bubble,"StepDot"+i,26+i*20,218,10,10,i<=tutorialStep?PlanPink:PlanTrack,12,false);
            PButton(bubble,"SkipTutorial","説明を飛ばす",332,213,200,44,SkipTutorial,Color.white,PlanGray,16);
            // 絵文字フォントに頼らない指示マーカー。
            var finger=IncidentShape(tutorialRoot,"TutorialFinger","finger",Mathf.Clamp(x+w/2-28,20,1544),Mathf.Clamp(y-80,170,810),56,66,Hex("ffd85c"));Motion(finger,"bob",1.2f);
        }
        private void TutorialShade(float x,float y,float w,float h)
        {
            if(w<=0||h<=0)return;
            var shade=Box(tutorialRoot,"TutorialShade",x,y,w,h,new Color(.05f,.07f,.14f,.58f));shade.GetComponent<Image>().raycastTarget=true;
        }
    }
}
