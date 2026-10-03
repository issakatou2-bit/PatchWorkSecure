using UnityEngine;
using UnityEngine.EventSystems;

namespace PatchWorkSecure.CompanyOps
{
    // 操作の受付はButton側。無効時は理由だけを示し、onClickやルールを実行しない。
    public sealed class OpsButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        public OpsGame Owner;
        public float PressDepth;
        public bool LiftOnFocus;
        private Vector2 origin;
        private bool capturedOrigin;
        private UnityEngine.UI.Button button;
        private UnityEngine.UI.Outline focus;
        private bool hovered, selected, pressed;
        private float submittedUntil;
        private float rejectedAt=-10;
        private float lift;private UnityEngine.UI.Shadow depth;private Color shadowColor;
        private void Awake()
        {
            button = GetComponent<UnityEngine.UI.Button>();
        }
        private void Start()
        {
            // 共通の枠をグラデーション・警告枠の後に描く。色を暗く乗算せず、
            // 感染/部屋の枠からも独立させる。ホバーと選択は同じ枠のまま。
            focus = gameObject.AddComponent<UnityEngine.UI.Outline>();
            focus.effectDistance = new Vector2(2, -2); focus.useGraphicAlpha = false;
            focus.effectColor = new Color(.44f, .71f, 1f, .85f); focus.enabled = false;
        }
        private bool Available => button != null && button.IsInteractable();
        private void Update()
        {
            if (!capturedOrigin) { origin = ((RectTransform)transform).anchoredPosition; capturedOrigin = true; }
            if(LiftOnFocus&&depth==null)
            {
                depth=gameObject.AddComponent<UnityEngine.UI.Shadow>();depth.effectDistance=new Vector2(0,-8);depth.useGraphicAlpha=false;
                shadowColor=new Color(.05f,.08f,.16f,0);depth.effectColor=shadowColor;
            }
            bool active = Available && (hovered || selected);
            if (focus != null) focus.enabled = active;
            float target = !Available || Owner == null || Owner.ReducedMotion ? 1 :
                pressed || Time.unscaledTime < submittedUntil ? .96f : active&&!LiftOnFocus ? 1.02f : 1;
            if (GetComponent<OpsPlanningMotion>() == null)
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, 1 - Mathf.Exp(-35 * Time.unscaledDeltaTime));
            if (Owner != null && Owner.ReducedMotion) transform.localScale = Vector3.one;
            lift=Owner==null||Owner.ReducedMotion?0:Mathf.MoveTowards(lift,LiftOnFocus&&active?4:0,Time.unscaledDeltaTime*4/.12f);
            if(depth!=null&&LiftOnFocus)depth.effectColor=new Color(shadowColor.r,shadowColor.g,shadowColor.b,Mathf.Lerp(shadowColor.a,Mathf.Min(1,shadowColor.a+.2f),Owner!=null&&Owner.ReducedMotion?(active?1:0):lift/4));
            if (PressDepth > 0 && GetComponent<OpsPlanningMotion>() == null)
            {
                ((RectTransform)transform).anchoredPosition = origin+Vector2.up*lift + Vector2.down *
                    (Available && Owner != null && !Owner.ReducedMotion && (pressed || Time.unscaledTime < submittedUntil) ? PressDepth : 0);
            }
            if(!Available&&Time.realtimeSinceStartup-rejectedAt<.25f)
            {
                float t=(Time.realtimeSinceStartup-rejectedAt)/.25f;
                ((RectTransform)transform).anchoredPosition=origin+Vector2.right*(Owner!=null&&!Owner.ReducedMotion?Mathf.Sin(t*Mathf.PI*8)*4*(1-t):0);
            }
        }
        private void OnDisable()
        {
            hovered = selected = pressed = false; submittedUntil = 0; transform.localScale = Vector3.one;
            if (focus != null) focus.enabled = false;
            if (capturedOrigin && PressDepth > 0) ((RectTransform)transform).anchoredPosition = origin;
        }
        public void OnPointerEnter(PointerEventData e) { hovered = Available; }
        public void OnPointerExit(PointerEventData e) { hovered = pressed = false; }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left){pressed=Available;if(!Available)Reject();} }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        public void OnSelect(BaseEventData e) { selected = Available;GetComponentInParent<OpsCardLift>()?.SetSelected(Available); }
        public void OnDeselect(BaseEventData e) { selected = pressed = false;GetComponentInParent<OpsCardLift>()?.SetSelected(false); }
        public void OnSubmit(BaseEventData e) { if (Available) submittedUntil = Time.unscaledTime + .085f;else Reject(); }
        private void Reject()
        {
            if(Owner==null||Time.realtimeSinceStartup-rejectedAt<.25f)return;
            if(!capturedOrigin){origin=((RectTransform)transform).anchoredPosition;capturedOrigin=true;}
            rejectedAt=Time.realtimeSinceStartup;Owner.RejectButton(button);
        }
    }
}
