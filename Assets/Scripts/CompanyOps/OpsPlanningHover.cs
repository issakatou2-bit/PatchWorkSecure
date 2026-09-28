using UnityEngine;
using UnityEngine.EventSystems;

namespace PatchWorkSecure.CompanyOps
{
    // オフィスの文字ラベルは、ポインター・キーボードの選択時だけ見せる。
    public sealed class OpsPlanningHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public GameObject Tooltip;
        private bool hovered, selected;
        private void Refresh() { if (Tooltip != null) Tooltip.SetActive(hovered || selected); }
        public void OnPointerEnter(PointerEventData e) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { hovered = false; Refresh(); }
        public void OnSelect(BaseEventData e) { selected = true; Refresh(); }
        public void OnDeselect(BaseEventData e) { selected = false; Refresh(); }
    }
}
