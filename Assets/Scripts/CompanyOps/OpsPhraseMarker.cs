using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // TMPのmarkは文字ごとに分割される。リンクの語句を行ごとの一本の帯として描く。
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OpsPhraseMarker : MaskableGraphic
    {
        public TextMeshProUGUI Label;
        private readonly List<Rect> bands = new List<Rect>();
        public IReadOnlyList<Rect> Bands => bands;
        private string lastText;
        private Vector2 lastSize;
        public static OpsPhraseMarker Attach(TextMeshProUGUI label)
        {
            var go=new GameObject(label.name+"Marker",typeof(RectTransform),typeof(CanvasRenderer));
            var rt=(RectTransform)go.transform;rt.SetParent(label.transform.parent,false);
            var source=label.rectTransform;rt.anchorMin=source.anchorMin;rt.anchorMax=source.anchorMax;
            rt.pivot=source.pivot;rt.sizeDelta=source.sizeDelta;rt.anchoredPosition=source.anchoredPosition;
            rt.SetSiblingIndex(source.GetSiblingIndex());
            var marker=go.AddComponent<OpsPhraseMarker>();marker.Label=label;
            marker.color=new Color32(255,224,102,255);marker.raycastTarget=false;return marker;
        }
        private void LateUpdate()
        {
            if(Label==null)return;
            if(lastText==Label.text && lastSize==Label.rectTransform.rect.size && !Label.havePropertiesChanged)return;
            RebuildBands();
        }
        public void RebuildBands()
        {
            bands.Clear();Label.ForceMeshUpdate();var info=Label.textInfo;
            for(int l=0;l<info.linkCount;l++)
            {
                var link=info.linkInfo[l];if(link.GetLinkID()!="mail-hint")continue;
                int end=link.linkTextfirstCharacterIndex+link.linkTextLength;
                int start=link.linkTextfirstCharacterIndex;
                while(start<end)
                {
                    int line=info.characterInfo[start].lineNumber,stop=start+1;
                    while(stop<end&&info.characterInfo[stop].lineNumber==line)stop++;
                    var first=info.characterInfo[start];var last=info.characterInfo[stop-1];
                    var metrics=info.lineInfo[line];float height=(metrics.ascender-metrics.descender)*.45f;
                    if(last.xAdvance>first.origin)bands.Add(new Rect(first.origin,metrics.descender,last.xAdvance-first.origin,height));
                    start=stop;
                }
            }
            lastText=Label.text;lastSize=Label.rectTransform.rect.size;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();foreach(var band in bands)
            {
                int i=vh.currentVertCount;vh.AddVert(new Vector3(band.xMin,band.yMin),color,Vector2.zero);
                vh.AddVert(new Vector3(band.xMin,band.yMax),color,Vector2.zero);
                vh.AddVert(new Vector3(band.xMax,band.yMax),color,Vector2.zero);
                vh.AddVert(new Vector3(band.xMax,band.yMin),color,Vector2.zero);
                vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
        }
    }
}
