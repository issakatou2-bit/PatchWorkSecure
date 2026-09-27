using UnityEngine;
using UnityEngine.EventSystems;

namespace PatchWorkSecure.CompanyOps
{
    // 操作の受付はButton側。演出の終了を待たせず、無効ボタンには反応しない。
    public sealed class OpsButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        public OpsGame Owner;
        private UnityEngine.UI.Button button;
        private UnityEngine.UI.Outline focus;
        private bool hovered, selected, pressed;
        private float submittedUntil;
        private void Awake()
        {
            button = GetComponent<UnityEngine.UI.Button>();
            focus = gameObject.AddComponent<UnityEngine.UI.Outline>();
            focus.effectDistance = new Vector2(2, -2); focus.useGraphicAlpha = false;
            focus.effectColor = new Color(.92f, .77f, .45f, .7f); focus.enabled = false;
        }
        private bool Available => button != null && button.IsInteractable();
        private void Update()
        {
            bool active = Available && (hovered || selected);
            if (focus != null) focus.enabled = active;
            float target = !Available || Owner == null || Owner.ReducedMotion ? 1 :
                pressed || Time.unscaledTime < submittedUntil ? .977f : active ? 1.009f : 1;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, 1 - Mathf.Exp(-35 * Time.unscaledDeltaTime));
            if (Owner != null && Owner.ReducedMotion) transform.localScale = Vector3.one;
        }
        private void OnDisable()
        {
            hovered = selected = pressed = false; submittedUntil = 0; transform.localScale = Vector3.one;
            if (focus != null) focus.enabled = false;
        }
        public void OnPointerEnter(PointerEventData e) { hovered = Available; }
        public void OnPointerExit(PointerEventData e) { hovered = pressed = false; }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pressed = Available; }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        public void OnSelect(BaseEventData e) { selected = Available; }
        public void OnDeselect(BaseEventData e) { selected = pressed = false; }
        public void OnSubmit(BaseEventData e) { if (Available) submittedUntil = Time.unscaledTime + .085f; }
    }
}
