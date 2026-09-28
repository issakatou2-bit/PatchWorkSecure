using TMPro;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public sealed class OpsTextPreference : MonoBehaviour
    {
        private TextMeshProUGUI label;
        private float initial,minimum,maximum;
        public void Initialize(TextMeshProUGUI text,float scale)
        {label=text;initial=text.fontSize;minimum=text.fontSizeMin;maximum=text.fontSizeMax;Apply(scale);}
        public void Apply(float scale)
        {if(label==null)return;label.fontSize=initial*scale;label.fontSizeMin=minimum*scale;label.fontSizeMax=maximum*scale;}
    }
}
