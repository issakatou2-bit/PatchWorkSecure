using TMPro;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    // 数字と単位を同じ行で詰める。文字サイズの設定変更にも追従する。
    public sealed class OpsInlineMetric : MonoBehaviour
    {
        public TextMeshProUGUI Number,Unit;
        public float Right;
        private void LateUpdate()
        {
            if(Number==null||Unit==null)return;
            float width=Mathf.Ceil(Unit.GetPreferredValues(Unit.text).x)+1;
            var unit=Unit.rectTransform;var number=Number.rectTransform;
            unit.sizeDelta=new Vector2(width,unit.sizeDelta.y);unit.anchoredPosition=new Vector2(Right-width,number.anchoredPosition.y);
            number.sizeDelta=new Vector2(Mathf.Max(0,unit.anchoredPosition.x-number.anchoredPosition.x),number.sizeDelta.y);
        }
    }
}
