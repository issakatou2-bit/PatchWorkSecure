using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // 危険テープではなく、手帳の予定に使う淡い金色の斜線。セル内で切り取る。
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OpsBlockStripe:MaskableGraphic
    {
        public override Texture mainTexture=>Texture2D.whiteTexture;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            for(float x=r.xMin-r.height;x<r.xMax;x+=18)
            {
                var polygon=new List<Vector2>{new Vector2(x,r.yMin),new Vector2(x+9,r.yMin),new Vector2(x+r.height+9,r.yMax),new Vector2(x+r.height,r.yMax)};
                polygon=Clip(polygon,r.xMin,true);polygon=Clip(polygon,r.xMax,false);if(polygon.Count<3)continue;
                int first=vh.currentVertCount;foreach(var p in polygon)vh.AddVert(p,color,Vector2.zero);
                for(int i=1;i<polygon.Count-1;i++)vh.AddTriangle(first,first+i,first+i+1);
            }
        }
        private static List<Vector2> Clip(List<Vector2> polygon,float edge,bool minimum)
        {
            var result=new List<Vector2>();if(polygon.Count==0)return result;
            for(int i=0;i<polygon.Count;i++)
            {
                var a=polygon[i];var b=polygon[(i+1)%polygon.Count];bool inside=minimum?a.x>=edge:a.x<=edge,next=minimum?b.x>=edge:b.x<=edge;
                if(inside)result.Add(a);if(inside!=next)result.Add(Vector2.Lerp(a,b,(edge-a.x)/(b.x-a.x)));
            }
            return result;
        }
    }
}
