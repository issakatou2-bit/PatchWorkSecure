using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public sealed class OpsCardLift : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public OpsGame Owner;private RectTransform rect;private Vector2 origin;private Shadow shadow;private bool hovered,selected;private float lift;
        private void Start(){rect=(RectTransform)transform;origin=rect.anchoredPosition;shadow=gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(0,-8);shadow.useGraphicAlpha=false;}
        public void SetSelected(bool value){selected=value;}
        public void OnPointerEnter(PointerEventData data){hovered=true;}
        public void OnPointerExit(PointerEventData data){hovered=false;}
        private void Update()
        {
            bool active=hovered||selected;lift=Owner.ReducedMotion?0:Mathf.MoveTowards(lift,active?4:0,Time.unscaledDeltaTime*4/.12f);
            rect.anchoredPosition=origin+Vector2.up*lift;
            shadow.effectColor=new Color(.05f,.08f,.16f,.12f+.2f*(Owner.ReducedMotion?(active?1:0):lift/4));
        }
    }
}
