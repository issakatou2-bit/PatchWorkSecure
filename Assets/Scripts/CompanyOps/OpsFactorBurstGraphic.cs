using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // 手本の青／金の放射状グラデーションと虹の円錐グラデーション。
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OpsFactorBurstGraphic : MaskableGraphic
    {
        public int Stars;
        public float Progress;
        public static Color Tone(int stars,float angle=0)
        {
            if(stars==1)return new Color(.498f,.769f,1);
            if(stars==2)return new Color(1,.843f,.353f);
            Color[] stops={new Color(1,.580f,.682f),new Color(1,.824f,.247f),new Color(.412f,.816f,.694f),new Color(.498f,.769f,1),new Color(.694f,.608f,.973f),new Color(1,.580f,.682f)};
            float p=Mathf.Repeat(angle,1)*5;int index=Mathf.Min(4,(int)p);return Color.Lerp(stops[index],stops[index+1],p-index);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float alpha=Progress<.25f?Mathf.Clamp01(Progress/.25f):1-Mathf.Clamp01((Progress-.25f)/.75f);
            var r=rectTransform.rect;float radius=r.width*.62f;
            for(int j=0;j<12;j++)for(int i=0;i<96;i++)
            {
                int s=vh.currentVertCount;
                for(int k=0;k<4;k++)
                {
                    float p=(i+(k==1||k==2?1:0))/96f,t=(j+(k>=2?1:0))/12f;
                    Color c=Tone(Stars,p);c.a=alpha*(1-Mathf.SmoothStep(0,1,Stars>=3?Mathf.InverseLerp(.32f,1,t):t))*.26f;
                    float a=p*Mathf.PI*2;var v=UIVertex.simpleVert;v.position=r.center+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*radius*t;v.color=c;vh.AddVert(v);
                }
                vh.AddTriangle(s,s+1,s+2);vh.AddTriangle(s,s+2,s+3);
            }
        }
    }
}
