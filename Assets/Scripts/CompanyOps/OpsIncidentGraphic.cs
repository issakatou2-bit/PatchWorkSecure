using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // モックのCSS/SVG図形をUIの頂点で描く。判定・画像素材・独自シェーダーに依存しない。
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OpsIncidentGraphic : MaskableGraphic
    {
        public string Kind;
        public float Offset;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float w=r.width,h=r.height;
            if(Kind=="dashed")
            {
                for(float x=12;x<w-12;x+=16) {Quad(vh,r.xMin+x,r.yMin,Mathf.Min(9,w-12-x),2,color,color);Quad(vh,r.xMin+x,r.yMax-2,Mathf.Min(9,w-12-x),2,color,color);}
                for(float y=8;y<h-8;y+=16) {Quad(vh,r.xMin,r.yMin+y,2,Mathf.Min(9,h-8-y),color,color);Quad(vh,r.xMax-2,r.yMin+y,2,Mathf.Min(9,h-8-y),color,color);}
            }
            else if(Kind=="alarm")
            {
                Quad(vh,r.xMin,r.yMin,w,h,new Color(color.r,color.g,color.b,.14f),new Color(color.r,color.g,color.b,.14f));
                const float radius=16;
                var centers=new[]{new Vector2(r.xMax-radius,r.yMax-radius),new Vector2(r.xMin+radius,r.yMax-radius),new Vector2(r.xMin+radius,r.yMin+radius),new Vector2(r.xMax-radius,r.yMin+radius)};
                for(int corner=0;corner<4;corner++)
                {
                    float start=corner*90;
                    for(int i=0;i<8;i++)
                    {
                        float a=(start+i*90f/8)*Mathf.Deg2Rad,b=(start+(i+1)*90f/8)*Mathf.Deg2Rad;
                        Line(vh,centers[corner]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,centers[corner]+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,4);
                    }
                }
                Line(vh,new Vector2(r.xMin+radius,r.yMax),new Vector2(r.xMax-radius,r.yMax),4);
                Line(vh,new Vector2(r.xMin+radius,r.yMin),new Vector2(r.xMax-radius,r.yMin),4);
                Line(vh,new Vector2(r.xMin,r.yMin+radius),new Vector2(r.xMin,r.yMax-radius),4);
                Line(vh,new Vector2(r.xMax,r.yMin+radius),new Vector2(r.xMax,r.yMax-radius),4);
            }
            else if(Kind=="tape")
            {
                Quad(vh,r.xMin,r.yMin,w,h,color,color);
                for(float x=-56+Offset;x<w+28;x+=28)
                    Polygon(vh,new[]{new Vector2(x+r.xMin,r.yMax),new Vector2(x+14+r.xMin,r.yMax),new Vector2(x+28+r.xMin,r.yMin),new Vector2(x+14+r.xMin,r.yMin)},new Color(.114f,.165f,.267f,1));
            }
            else if(Kind=="cutin")
            {
                float skew=h*.21256f;
                var points=new[]{new Vector2(r.xMin-skew/2,r.yMin),new Vector2(r.xMax-skew/2,r.yMin),new Vector2(r.xMax+skew/2,r.yMax),new Vector2(r.xMin+skew/2,r.yMax)};
                int start=vh.currentVertCount;
                for(int i=0;i<4;i++) Add(vh,points[i],i==0||i==3?color:new Color(1,.827f,.871f,color.a));
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
            else if(Kind=="rays")
            {
                var center=r.center;float radius=Mathf.Min(w,h)*.5f;
                for(int i=0;i<18;i++)
                {
                    float a=i*20*Mathf.Deg2Rad,b=(i*20+10)*Mathf.Deg2Rad;
                    Polygon(vh,new[]{center,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius},color);
                }
            }
            else if(Kind=="vignette")
            {
                Vector2 center=r.center;int c=vh.currentVertCount;Add(vh,center,new Color(color.r,color.g,color.b,0));
                for(int i=0;i<=64;i++)
                {
                    float a=i*Mathf.PI*2/64;Add(vh,center+new Vector2(Mathf.Cos(a)*w*.71f,Mathf.Sin(a)*h*.71f),color);
                    if(i>0)vh.AddTriangle(c,c+i,c+i+1);
                }
            }
            else if(Kind=="warning")
            {
                Line(vh,new Vector2(r.xMin+2,r.yMin+3),new Vector2(r.center.x,r.yMax-3),2.8f);
                Line(vh,new Vector2(r.center.x,r.yMax-3),new Vector2(r.xMax-2,r.yMin+3),2.8f);
                Line(vh,new Vector2(r.xMax-2,r.yMin+3),new Vector2(r.xMin+2,r.yMin+3),2.8f);
                Line(vh,new Vector2(r.center.x,r.yMin+h*.4f),new Vector2(r.center.x,r.yMin+h*.64f),2.8f);
                Quad(vh,r.center.x-1.4f,r.yMin+h*.24f,2.8f,2.8f,color,color);
            }
            else if(Kind=="check")
            {
                Line(vh,new Vector2(r.xMin+w*.15f,r.yMin+h*.5f),new Vector2(r.xMin+w*.42f,r.yMin+h*.22f),3);
                Line(vh,new Vector2(r.xMin+w*.42f,r.yMin+h*.22f),new Vector2(r.xMin+w*.85f,r.yMin+h*.8f),3);
            }
            else if(Kind=="stop"||Kind=="restore")
            {
                float radius=Mathf.Min(w,h)*.375f;Vector2 center=r.center;
                int count=Kind=="stop"?40:33;
                for(int i=0;i<count;i++)
                {
                    float a=i*Mathf.PI*2/40,b=(i+1)*Mathf.PI*2/40;
                    Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,3);
                }
                if(Kind=="stop") Line(vh,center+new Vector2(-radius*.7f,radius*.7f),center+new Vector2(radius*.7f,-radius*.7f),3);
                else {Line(vh,new Vector2(r.xMin+w*.12f,r.yMax-h*.12f),new Vector2(r.xMin+w*.12f,r.yMax-h*.36f),3);Line(vh,new Vector2(r.xMin+w*.12f,r.yMax-h*.36f),new Vector2(r.xMin+w*.36f,r.yMax-h*.36f),3);}
            }
            else Quad(vh,r.xMin,r.yMin,w,h,Kind=="lossGradient"?new Color(1,.878f,.761f,1):new Color(1,.827f,.871f,1),color);
        }
        private static void Add(VertexHelper vh,Vector2 p,Color c) {var v=UIVertex.simpleVert;v.position=p;v.color=c;vh.AddVert(v);}
        private static void Polygon(VertexHelper vh,Vector2[] points,Color color)
        {int start=vh.currentVertCount;foreach(var p in points)Add(vh,p,color);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);}
        private static void Quad(VertexHelper vh,float x,float y,float w,float h,Color left,Color right)
        {int s=vh.currentVertCount;Add(vh,new Vector2(x,y),left);Add(vh,new Vector2(x+w,y),right);Add(vh,new Vector2(x+w,y+h),right);Add(vh,new Vector2(x,y+h),left);vh.AddTriangle(s,s+1,s+2);vh.AddTriangle(s,s+2,s+3);}
        private void Line(VertexHelper vh,Vector2 a,Vector2 b,float width)
        {var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Polygon(vh,new[]{a-n,b-n,b+n,a+n},color);}
    }
    public sealed class OpsIncidentTape : MonoBehaviour
    {
        public OpsGame Owner;
        private void Update()
        {var graphic=GetComponent<OpsIncidentGraphic>();graphic.Offset=Owner==null||Owner.ReducedMotion?0:Mathf.Repeat(Time.unscaledTime*56,28);graphic.SetVerticesDirty();}
    }
}
