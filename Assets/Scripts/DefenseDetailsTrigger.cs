using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PatchWorkSecure
{
    /// <summary>マウスでもキーボード選択でも、購入前に対策の説明を確認できる。</summary>
    public class DefenseDetailsTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public Action<bool> FocusChanged;
        private bool _hovered, _selected;
        public void OnPointerEnter(PointerEventData data) { _hovered=true; Notify(); }
        public void OnPointerExit(PointerEventData data) { _hovered=false; Notify(); }
        public void OnSelect(BaseEventData data) { _selected=true; Notify(); }
        public void OnDeselect(BaseEventData data) { _selected=false; Notify(); }
        private void Notify() { FocusChanged?.Invoke(_hovered || _selected); }
    }
}
