using UnityEngine;
using UnityEngine.EventSystems;

namespace PatchWorkSecure
{
    /// <summary>
    /// ボタン押下時に一瞬だけ縮んで戻る「押した感」を付けるコンポーネント。
    /// Buttonのcolors遷移だけでは平坦になりがちな押下フィードバックをスケール変化で補う。
    /// </summary>
    public class UIButtonPunch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.94f;
        [SerializeField] private float speed = 14f;

        private RectTransform _rt;
        private Vector3 _targetScale = Vector3.one;

        private void Awake()
        {
            _rt = (RectTransform)transform;
        }

        private void Update()
        {
            _rt.localScale = Vector3.Lerp(_rt.localScale, _targetScale, Time.deltaTime * speed);
        }

        public void OnPointerDown(PointerEventData eventData) => _targetScale = Vector3.one * pressedScale;
        public void OnPointerUp(PointerEventData eventData) => _targetScale = Vector3.one;
        public void OnPointerExit(PointerEventData eventData) => _targetScale = Vector3.one;
    }
}
