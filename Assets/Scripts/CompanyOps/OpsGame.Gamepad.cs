using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private InputSystemUIInputModule padModule;
        private InputActionAsset padOriginalActions, padUiActions;
        private Transform padScope;
        private string padSelectionName;
        private Vector2 padLastDirection;
        private float padRepeatAt;
        private bool padInUse;
        private OpsDiaryRecord padDiaryPage;
        private Action padDiaryClose;
        private bool padDiaryArchive;

        // UIモジュールのゲームパッド処理と二重決定しない。元アセットは変更しない。
        private void ConfigureGamepadUI()
        {
            if(padModule!=null||EventSystem.current==null)return;
            padModule=EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if(padModule==null||padModule.actionsAsset==null)return;
            padOriginalActions=padModule.actionsAsset;
            padUiActions=Instantiate(padOriginalActions);
            foreach(string name in new[]{"UI/Navigate","UI/Submit","UI/Cancel"})
            {
                var action=padUiActions.FindAction(name);
                if(action!=null)action.bindingMask=InputBinding.MaskByGroup("Keyboard&Mouse");
            }
            // 既定の */{Submit} は全グループ所属で、グループ制限だけではAが残る。
            foreach(string name in new[]{"Submit","Cancel"})
            {
                var action=padUiActions.FindAction("UI/"+name);if(action==null)continue;
                for(int i=0;i<action.bindings.Count;i++)action.ApplyBindingOverride(i,new InputBinding{overridePath=""});
                action.AddBinding(name=="Submit"?"<Keyboard>/enter":"<Keyboard>/escape",groups:"Keyboard&Mouse");
                if(name=="Submit")action.AddBinding("<Keyboard>/numpadEnter",groups:"Keyboard&Mouse");
            }
            padModule.actionsAsset=padUiActions;
        }
        private void ReleaseGamepadUI()
        {
            if(padModule!=null&&padOriginalActions!=null)padModule.actionsAsset=padOriginalActions;
            if(padUiActions!=null)Destroy(padUiActions);
        }
        private Transform GamepadScope=>modal!=null?modal:MinigameActive&&minigameModal!=null?minigameModal:screen;
        private Selectable[] GamepadChoices()
        {
            var root=GamepadScope;if(root==null)return new Selectable[0];
            var choices=root.GetComponentsInChildren<Selectable>().Where(s=>s.IsActive()&&s.IsInteractable()).ToArray();
            if(TutorialActive&&modal==null&&tutorialTarget!=null&&!MinigameActive)
                choices=choices.Where(s=>s.name==tutorialTarget||s.name=="SkipTutorial").ToArray();
            return choices;
        }
        private void EnsureGamepadSelection()
        {
            if(EventSystem.current==null)return;
            var choices=GamepadChoices();if(choices.Length==0)return;
            var current=EventSystem.current.currentSelectedGameObject;
            bool changed=padScope!=GamepadScope;padScope=GamepadScope;
            if(!changed&&current!=null&&choices.Any(s=>s.gameObject==current))return;
            string[] defaults={"MinigameStart","MinigameContinue","NextMonth","DiaryClose","ConfirmAdvance","AcceptMission","ConfirmNewYear","SingleYearStart","Action_audit","NewYear","Respond_scope","CloseDialog"};
            var first=!changed?choices.FirstOrDefault(s=>s.name==padSelectionName):null;
            first=first??defaults.Select(id=>choices.FirstOrDefault(s=>s.name==id)).FirstOrDefault(s=>s!=null)??choices[0];
            SelectGamepad(first);
        }
        private void SelectGamepad(Selectable next)
        {
            EventSystem.current.SetSelectedGameObject(next.gameObject);
            padSelectionName=next.name;
            // 一覧の画面外にある項目も、選択に合わせて既存のスクロールを動かす。
            var scroll=next.GetComponentInParent<ScrollRect>();
            if(scroll==null||scroll.content==null||scroll.viewport==null)return;
            Canvas.ForceUpdateCanvases();var corners=new Vector3[4];((RectTransform)next.transform).GetWorldCorners(corners);
            var top=scroll.viewport.InverseTransformPoint(corners[1]);var bottom=scroll.viewport.InverseTransformPoint(corners[0]);
            float offset=top.y>scroll.viewport.rect.yMax?scroll.viewport.rect.yMax-top.y:bottom.y<scroll.viewport.rect.yMin?scroll.viewport.rect.yMin-bottom.y:0;
            if(offset!=0)scroll.content.anchoredPosition+=Vector2.up*offset;
        }
        private void MoveGamepad(Vector2 direction)
        {
            var choices=GamepadChoices();var current=EventSystem.current?.currentSelectedGameObject;
            if(current==null)return;
            var slider=current.GetComponent<Slider>();
            if(slider!=null&&Mathf.Abs(direction.x)>Mathf.Abs(direction.y))
            {slider.value=Mathf.Clamp01(slider.value+Mathf.Sign(direction.x)*.05f);return;}
            Vector2 origin=((RectTransform)current.transform).TransformPoint(((RectTransform)current.transform).rect.center);
            Selectable best=null;float score=float.PositiveInfinity;
            foreach(var candidate in choices)
            {
                if(candidate.gameObject==current)continue;
                Vector2 delta=(Vector2)((RectTransform)candidate.transform).TransformPoint(((RectTransform)candidate.transform).rect.center)-origin;
                float forward=Vector2.Dot(delta,direction);if(forward<=1)continue;
                float side=Mathf.Abs(delta.x*direction.y-delta.y*direction.x);
                float distance=delta.magnitude+side*2;
                if(distance<score){score=distance;best=candidate;}
            }
            if(best!=null)SelectGamepad(best);
        }
        private void GamepadBack()
        {
            StopVoice();
            if(modal!=null){CloseDialog();return;}
            if(FactorRevealActive){SkipFactorReveal();return;}
            if(YearOpeningActive){SkipYearOpening();return;}
            if(DiaryActive){var close=GamepadChoices().FirstOrDefault(s=>s.name=="DiaryClose") as Button;close?.onClick.Invoke();return;}
            if(MinigameActive)return;
            if(tab!=0&&!homeVisible){OpenTab(0);return;}
            if(recordsActive){RenderHome();return;}
            Menu();
        }
        private void GamepadDiaryTurn(int delta)
        {
            if(!DiaryActive||!padDiaryArchive||padDiaryPage==null||modal!=null)return;
            var pages=DiaryPages.OrderBy(p=>p.key).ToList();int index=pages.FindIndex(p=>p.key==padDiaryPage.key)+delta;
            if(index>=0&&index<pages.Count)DiarySpread(pages[index],padDiaryClose,false,true);
        }
        private void TickGamepad()
        {
            ConfigureGamepadUI();var pad=Gamepad.current;if(pad==null)return;
            var direction=pad.dpad.ReadValue();if(direction.sqrMagnitude<.3f)direction=pad.leftStick.ReadValue();
            bool active=direction.sqrMagnitude>.3f||pad.buttonSouth.wasPressedThisFrame||pad.buttonEast.wasPressedThisFrame||pad.startButton.wasPressedThisFrame||pad.leftShoulder.wasPressedThisFrame||pad.rightShoulder.wasPressedThisFrame;
            if(Mouse.current?.delta.ReadValue().sqrMagnitude>1||Mouse.current?.leftButton.wasPressedThisFrame==true)padInUse=false;
            if(active)padInUse=true;
            if(!padInUse&&!active)return;
            if(pad.startButton.wasPressedThisFrame){StopVoice();if(modal!=null&&modal.Find("SettingsWindow")!=null)CloseDialog();else Menu();return;}
            if(pad.buttonEast.wasPressedThisFrame){GamepadBack();return;}
            if(YearOpeningActive)
            {if(pad.buttonSouth.wasPressedThisFrame)AdvanceYearOpening();return;}
            if(pad.buttonSouth.wasPressedThisFrame&&(PhasePresentationRunning||Time.unscaledTime<presentationInputGuardUntil))
            {SkipPhasePresentation();return;}
            if(pad.buttonSouth.wasPressedThisFrame&&AnnualPresentationCanSkip&&!AnnualPresentationSkipped)
            {FinishReportCounts();SkipAnnualPresentation();return;}
            if(DiaryActive){if(pad.leftShoulder.wasPressedThisFrame)GamepadDiaryTurn(-1);if(pad.rightShoulder.wasPressedThisFrame)GamepadDiaryTurn(1);}
            EnsureGamepadSelection();
            if(direction.sqrMagnitude>.3f)
            {
                direction=Mathf.Abs(direction.x)>Mathf.Abs(direction.y)?new Vector2(Mathf.Sign(direction.x),0):new Vector2(0,Mathf.Sign(direction.y));
                if(direction!=padLastDirection||Time.unscaledTime>=padRepeatAt){MoveGamepad(direction);padRepeatAt=Time.unscaledTime+(direction!=padLastDirection?.32f:.12f);}
            }
            padLastDirection=direction;
            if(pad.buttonSouth.wasPressedThisFrame&&EventSystem.current?.currentSelectedGameObject!=null)
            {
                FinishReportCounts();
                ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            }
        }
    }
}
