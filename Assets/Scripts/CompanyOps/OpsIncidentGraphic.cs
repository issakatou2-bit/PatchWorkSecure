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
        public float StrokeWidth=2;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float w=r.width,h=r.height;
            if(Kind.StartsWith("story-")&&Kind.EndsWith("background"))
            {
                bool fail=Kind=="story-fail-background",dark=Kind=="story-factor-background";
                Color a=dark?new Color(.231f,.310f,.490f):fail?new Color(.933f,.949f,.973f):new Color(1,.957f,.839f);
                Color b=fail?new Color(.902f,.925f,.973f):new Color(1,.890f,.925f),c=fail?new Color(.953f,.910f,.965f):new Color(.863f,.937f,1);
                for(int j=0;j<24;j++)for(int i=0;i<40;i++)
                {
                    int s=vh.currentVertCount;
                    Vector2[] points={new Vector2(i/40f,j/24f),new Vector2((i+1)/40f,j/24f),new Vector2((i+1)/40f,(j+1)/24f),new Vector2(i/40f,(j+1)/24f)};
                    foreach(var p in points){float t=Mathf.Clamp01(p.x*.393f+(1-p.y)*.607f);Color v=dark?Color.Lerp(a,new Color(.114f,.165f,.267f),Mathf.Clamp01(Vector2.Distance(new Vector2((p.x-.5f)*1.6f,p.y-.6f),Vector2.zero)/.7f)):t<.55f?Color.Lerp(a,b,t/.55f):Color.Lerp(b,c,(t-.55f)/.45f);Add(vh,new Vector2(r.xMin+p.x*w,r.yMin+p.y*h),v);}
                    vh.AddTriangle(s,s+1,s+2);vh.AddTriangle(s,s+2,s+3);
                }
            }
            else if(Kind=="rounded-dashed")
            {
                float radius=Mathf.Min(w,h)*.16f;
                for(int corner=0;corner<4;corner++)
                {
                    Vector2 center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                    for(int j=0;j<12;j+=2){float a=(corner*90+j*7.5f)*Mathf.Deg2Rad,b=(corner*90+(j+1)*7.5f)*Mathf.Deg2Rad;Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,5);}
                }
                for(float x=radius;x<w-radius;x+=15){Line(vh,new Vector2(r.xMin+x,r.yMin),new Vector2(r.xMin+Mathf.Min(x+9,w-radius),r.yMin),5);Line(vh,new Vector2(r.xMin+x,r.yMax),new Vector2(r.xMin+Mathf.Min(x+9,w-radius),r.yMax),5);}
                for(float y=radius;y<h-radius;y+=15){Line(vh,new Vector2(r.xMin,r.yMin+y),new Vector2(r.xMin,r.yMin+Mathf.Min(y+9,h-radius)),5);Line(vh,new Vector2(r.xMax,r.yMin+y),new Vector2(r.xMax,r.yMin+Mathf.Min(y+9,h-radius)),5);}
            }
            else if(Kind=="staff-face")
            {
                var center=r.center+Vector2.up*h*.03f;
                for(int i=0;i<40;i++){float a=i*Mathf.PI/20,b=(i+1)*Mathf.PI/20;Line(vh,center+new Vector2(Mathf.Cos(a)*w*.34f,Mathf.Sin(a)*h*.4f),center+new Vector2(Mathf.Cos(b)*w*.34f,Mathf.Sin(b)*h*.4f),2);}
                Line(vh,center+new Vector2(-w*.17f,h*.06f),center+new Vector2(-w*.17f,-h*.02f),2);
                Line(vh,center+new Vector2(w*.17f,h*.06f),center+new Vector2(w*.17f,-h*.02f),2);
                Line(vh,center+new Vector2(-w*.09f,-h*.18f),center+new Vector2(w*.09f,-h*.18f),2);
                if(Offset==1){Line(vh,center+new Vector2(-w*.27f,h*.06f),center+new Vector2(w*.27f,h*.06f),2);}
                else{Line(vh,center+new Vector2(-w*.26f,h*.21f),center+new Vector2(Offset==0?w*.12f:0,h*.33f),3);Line(vh,center+new Vector2(w*.26f,h*.21f),center+new Vector2(Offset==0?w*.12f:0,h*.33f),3);}
            }
            else if(Kind=="bubble-glass"||Kind=="bubble-ring")
            {
                var center=r.center;float radius=Mathf.Min(w,h)*.48f;
                if(Kind=="bubble-glass")
                {
                    int start=vh.currentVertCount;Add(vh,center+new Vector2(-w*.16f,h*.18f),Color.white);
                    for(int i=0;i<=64;i++){float a=i*Mathf.PI*2/64;Add(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,Color.Lerp(Color.white,color,.33f));if(i>0)vh.AddTriangle(start,start+i,start+i+1);}
                }
                for(int i=0;i<64;i++){if(Kind=="bubble-ring"&&i%4>1)continue;float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,Kind=="bubble-ring"?3:2);}
            }
            else if(Kind=="bubble-icon")
            {
                int k=(int)Offset;System.Action<float,float,float,float> line=(a,b,c,d)=>Line(vh,new Vector2(r.xMin+a*w,r.yMin+b*h),new Vector2(r.xMin+c*w,r.yMin+d*h),2.3f);
                if(k==7){for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad,b=(90+(i+1)*36)*Mathf.Deg2Rad;float ra=i%2==0?.45f:.2f,rb=i%2==0?.2f:.45f;Line(vh,r.center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*w*ra,r.center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*w*rb,2.3f);}}
                else if(k==2){for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;Line(vh,new Vector2(r.xMin+w*.3f,r.center.y)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*w*.22f,new Vector2(r.xMin+w*.3f,r.center.y)+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*w*.22f,2.3f);}line(.52f,.5f,.95f,.5f);line(.85f,.5f,.85f,.3f);}
                else if(k==3){for(int ring=0;ring<3;ring++)for(int i=0;i<16;i++){float a=(40+i*6)*Mathf.Deg2Rad,b=(40+(i+1)*6)*Mathf.Deg2Rad,ra=w*(.3f+ring*.25f);Line(vh,new Vector2(r.center.x,r.yMin)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*ra,new Vector2(r.center.x,r.yMin)+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*ra,2.3f);}}
                else{line(.1f,.25f,.9f,.25f);line(.9f,.25f,.9f,.8f);line(.9f,.8f,.1f,.8f);line(.1f,.8f,.1f,.25f);if(k==6){line(.1f,.8f,.5f,.45f);line(.5f,.45f,.9f,.8f);}else if(k==0){line(.25f,.8f,.25f,1);line(.25f,1,.75f,1);line(.75f,1,.75f,.8f);line(.3f,.5f,.7f,.5f);}else{line(.5f,.25f,.5f,.05f);line(.3f,.05f,.7f,.05f);}}
            }
            else if(Kind=="heart")
            {
                var p=new[]{new Vector2(.5f,.73f),new Vector2(.35f,.94f),new Vector2(.18f,.97f),new Vector2(.04f,.83f),new Vector2(.03f,.64f),new Vector2(.17f,.4f),new Vector2(.5f,.05f),new Vector2(.83f,.4f),new Vector2(.97f,.64f),new Vector2(.96f,.83f),new Vector2(.82f,.97f),new Vector2(.65f,.94f)};
                Polygon(vh,System.Array.ConvertAll(p,v=>new Vector2(r.xMin+v.x*w,r.yMin+v.y*h)),color);
            }
            else if(Kind=="play")Polygon(vh,new[]{new Vector2(r.xMin+w*.2f,r.yMin),new Vector2(r.xMax,r.center.y),new Vector2(r.xMin+w*.2f,r.yMax)},color);
            else if(Kind=="arrow"){Line(vh,new Vector2(r.xMin,r.center.y),new Vector2(r.xMax,r.center.y),2.6f);Line(vh,new Vector2(r.center.x,r.yMax),new Vector2(r.xMax,r.center.y),2.6f);Line(vh,new Vector2(r.center.x,r.yMin),new Vector2(r.xMax,r.center.y),2.6f);}
            else if(Kind=="title-bottom-veil")
            {
                int s=vh.currentVertCount;Add(vh,new Vector2(r.xMin,r.yMin),new Color(240/255f,248/255f,1,.7f));Add(vh,new Vector2(r.xMax,r.yMin),new Color(240/255f,248/255f,1,.7f));Add(vh,new Vector2(r.xMax,r.yMax),new Color(240/255f,248/255f,1,0));Add(vh,new Vector2(r.xMin,r.yMax),new Color(240/255f,248/255f,1,0));vh.AddTriangle(s,s+1,s+2);vh.AddTriangle(s,s+2,s+3);
            }
            else if(Kind=="title-veil"||Kind=="title-kv-veil"||Kind=="story-title-veil")
            {
                float[] stops=Kind=="title-kv-veil"?new[]{0,.26f,.4f,.5f}:new[]{0,.34f,.62f,1};
                Color[] colors=Kind=="title-kv-veil"?new[]{new Color(240/255f,248/255f,1,.94f),new Color(240/255f,248/255f,1,.82f),new Color(1,240/255f,245/255f,.25f),new Color(1,240/255f,245/255f,0)}:new[]{new Color(.918f,.961f,1,.97f),new Color(.918f,.961f,1,.9f),new Color(1,.89f,.925f,.15f),new Color(1,.89f,.925f,0)};
                if(Kind=="story-title-veil"){stops=new[]{0,.3f,.44f,1};colors=new[]{new Color(1,244/255f,236/255f,.94f),new Color(1,244/255f,236/255f,.78f),new Color(1,244/255f,236/255f,0),new Color(1,244/255f,236/255f,0)};}
                for(int i=0;i<3;i++)Quad(vh,r.xMin+w*stops[i],r.yMin,w*(stops[i+1]-stops[i]),h,colors[i],colors[i+1]);
            }
            else if(Kind=="trend-up"||Kind=="trend-down")
            {
                bool up=Kind=="trend-up";float tip=up?r.yMax:r.yMin,baseY=up?r.yMin:r.yMax,head=up?r.yMax-h*.4f:r.yMin+h*.4f;
                Line(vh,new Vector2(r.center.x,baseY),new Vector2(r.center.x,head),2.5f);
                Polygon(vh,new[]{new Vector2(r.xMin,head),new Vector2(r.center.x,tip),new Vector2(r.xMax,head)},color);
            }
            else if(Kind=="trend-flat")Line(vh,new Vector2(r.xMin,r.center.y),new Vector2(r.xMax,r.center.y),2.5f);
            else if(Kind=="ripple")
            {
                for(int i=0;i<40;i++)
                {
                    float a=i*Mathf.PI*2/40,b=(i+1)*Mathf.PI*2/40;
                    Line(vh,r.center+new Vector2(Mathf.Cos(a)*w*.47f,Mathf.Sin(a)*h*.4f),
                        r.center+new Vector2(Mathf.Cos(b)*w*.47f,Mathf.Sin(b)*h*.4f),1.5f);
                }
            }
            else if(Kind=="stripes")
            {
                for(float x=-h;x<w;x+=24)
                    Polygon(vh,new[]{new Vector2(r.xMin+x,r.yMin),new Vector2(r.xMin+x+10,r.yMin),new Vector2(r.xMin+x+h+10,r.yMax),new Vector2(r.xMin+x+h,r.yMax)},color);
            }
            else if(Kind=="dashed"||Kind=="round-dashed")
            {
                float line=StrokeWidth;
                for(float x=12;x<w-12;x+=16) {Quad(vh,r.xMin+x,r.yMin,Mathf.Min(9,w-12-x),line,color,color);Quad(vh,r.xMin+x,r.yMax-line,Mathf.Min(9,w-12-x),line,color,color);}
                for(float y=8;y<h-8;y+=16) {Quad(vh,r.xMin,r.yMin+y,line,Mathf.Min(9,h-8-y),color,color);Quad(vh,r.xMax-line,r.yMin+y,line,Mathf.Min(9,h-8-y),color,color);}
                if(Kind=="round-dashed")for(int c=0;c<4;c++)
                {
                    var center=new Vector2(c==0||c==3?r.xMax-6:r.xMin+6,c<2?r.yMax-6:r.yMin+6);
                    for(int k=0;k<5;k++){float a=(c*90+k*18)*Mathf.Deg2Rad,b=a+18*Mathf.Deg2Rad;Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*5,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*5,line);}
                }
            }
            else if(Kind=="finger")
            {
                Polygon(vh,new[]{new Vector2(r.xMin+w*.34f,r.yMax),new Vector2(r.xMin+w*.6f,r.yMax),new Vector2(r.xMin+w*.6f,r.yMin+h*.55f),new Vector2(r.xMax,r.yMin+h*.5f),new Vector2(r.xMin+w*.85f,r.yMin),new Vector2(r.xMin+w*.2f,r.yMin),new Vector2(r.xMin,r.yMin+h*.35f),new Vector2(r.xMin+w*.34f,r.yMin+h*.3f)},color);
            }
            else if(Kind=="alarm"||Kind=="story-outline")
            {
                if(Kind=="alarm")Quad(vh,r.xMin,r.yMin,w,h,new Color(color.r,color.g,color.b,.14f),new Color(color.r,color.g,color.b,.14f));
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
