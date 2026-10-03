using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // 承認モックのCSS/SVGを頂点で再現。影の正体に事件の未確認情報は使わない。
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OpsOpeningGraphic : MaskableGraphic
    {
        public string Kind="growth";
        public int Shape;
        public float Radius=30,Clock;
        private static Color H(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(Kind=="rounded") {Rounded(vh,r,Radius,color);return;}
            if(Kind=="shadow") {for(int n=20;n>=0;n--){var a=r;a.xMin+=n;a.xMax-=n;a.yMin+=n;a.yMax-=n;Rounded(vh,a,Radius+20-n,new Color(color.r,color.g,color.b,color.a/21));}return;}
            if(Kind=="scan") {for(float y=r.yMin;y<r.yMax;y+=5)Quad(vh,new Rect(r.xMin,y,r.width,2),new Color(1,1,1,.004f));return;}
            if(Kind=="caution") {Quad(vh,r,H("1d2a44"));for(float x=-r.height;x<r.width;x+=52)Poly(vh,new[]{new Vector2(r.xMin+x,r.yMin),new Vector2(r.xMin+x+26,r.yMin),new Vector2(r.xMin+x+r.height+26,r.yMax),new Vector2(r.xMin+x+r.height,r.yMax)},H("ffc02e"));return;}
            if(Kind=="link")
            {
                for(int n=0;n<80;n++){float t=n/80f,u=(n+1)/80f;if((n+(int)(Clock*30))%10<5)Line(vh,Link(r,t),Link(r,u),6,H("3fa9f5"));}
                Ellipse(vh,Link(r,Mathf.Repeat(Clock/1.2f,1)),8,8,H("ffc02e"));return;
            }
            if(Kind=="rival"||Kind=="bossRival") {if(Kind=="bossRival"){float margin=r.height/17;r.yMin+=margin;r.yMax-=margin;}Rival(vh,r,Shape);return;}
            if(Kind=="ally-veil")
            {
                var a=H("1d2a44");a.a=.92f;var b=a;b.a=0;int s=vh.currentVertCount;
                Add(vh,new Vector2(r.xMin,r.yMin),a);Add(vh,new Vector2(r.xMax,r.yMin),a);Add(vh,new Vector2(r.xMax,r.yMin+r.height*.65f),a);Add(vh,new Vector2(r.xMin,r.yMin+r.height*.65f),a);vh.AddTriangle(s,s+1,s+2);vh.AddTriangle(s,s+2,s+3);
                s=vh.currentVertCount;Add(vh,new Vector2(r.xMin,r.yMin+r.height*.65f),a);Add(vh,new Vector2(r.xMax,r.yMin+r.height*.65f),a);Add(vh,new Vector2(r.xMax,r.yMax),b);Add(vh,new Vector2(r.xMin,r.yMax),b);vh.AddTriangle(s,s+1,s+2);vh.AddTriangle(s,s+2,s+3);return;
            }
            for(int y=0;y<30;y++)for(int x=0;x<50;x++)
            {
                int start=vh.currentVertCount;
                Vector2[] p={new Vector2(x/50f,y/30f),new Vector2((x+1)/50f,y/30f),new Vector2((x+1)/50f,(y+1)/30f),new Vector2(x/50f,(y+1)/30f)};
                foreach(var v in p)
                {
                    Color c;float d=Vector2.Distance(new Vector2((v.x-.5f)*1.2f,(v.y-.5f)*1.1f),Vector2.zero);
                    if(Kind=="rivals"||Kind=="appear")c=Color.Lerp(H(Kind=="appear"?"4a1026":"3a0f22"),H("0d0710"),Mathf.Clamp01(d/.7f));
                    else if(Kind=="title-veil"){c=H("1d2a44");c.a=Mathf.Lerp(.65f,.96f,Mathf.Clamp01(d/.7f));}
                    else if(Kind=="unlock")c=d<.6f?Color.Lerp(H("fff6d6"),H("ffe3ec"),d/.6f):Color.Lerp(H("ffe3ec"),H("f2e6ff"),(d-.6f)/.4f);
                    else {float t=Mathf.Clamp01(v.x*.342f+(1-v.y)*.94f);c=t<.55f?Color.Lerp(H(Kind=="allies"?"e3f2ff":"fff4ec"),H(Kind=="allies"?"fff4ec":"ffe3ec"),t/.55f):Color.Lerp(H(Kind=="allies"?"fff4ec":"ffe3ec"),H(Kind=="allies"?"ffe3ec":"dcefff"),(t-.55f)/.45f);}
                    Add(vh,new Vector2(r.xMin+v.x*r.width,r.yMin+v.y*r.height),c);
                }
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
        private static Vector2 Link(Rect r,float t)
        {float u=1-t;return new Vector2(r.xMin+u*u*u*10+3*u*u*t*80+3*u*t*t*160+t*t*t*230,r.yMax-(u*u*u*40+3*u*u*t*0+3*u*t*t*80+t*t*t*40));}
        private void Rival(VertexHelper vh,Rect r,int shape)
        {
            System.Func<float,float,Vector2> p=(x,y)=>new Vector2(r.xMin+x/220*r.width,r.yMax-y/300*r.height);
            System.Action<Vector2[],Color> draw=(points,c)=>Poly(vh,System.Array.ConvertAll(points,a=>p(a.x,a.y)),c);
            var edge=H("ff4d73");var dark=H("1a0a12");
            var hood=new List<Vector2>();
            System.Action<Vector2,Vector2,Vector2,Vector2> curve=(a,b,c,d)=>{for(int i=0;i<12;i++){float t=i/12f,u=1-t;hood.Add(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);}};
            curve(new Vector2(110,20),new Vector2(60,20),new Vector2(46,70),new Vector2(52,110));
            curve(new Vector2(52,110),new Vector2(30,130),new Vector2(18,170),new Vector2(20,300));hood.Add(new Vector2(20,300));hood.Add(new Vector2(200,300));
            curve(new Vector2(200,300),new Vector2(202,170),new Vector2(190,130),new Vector2(168,110));
            curve(new Vector2(168,110),new Vector2(174,70),new Vector2(160,20),new Vector2(110,20));
            System.Action<float,float,float> h=(scale,dx,dy)=>
            {
                var pts=hood.ConvertAll(a=>p((a.x-110)*scale+110+dx,a.y==300?300:a.y*scale+dy)).ToArray();Poly(vh,pts,dark);
                for(int i=0;i<pts.Length;i++)Line(vh,pts[i],pts[(i+1)%pts.Length],2,edge);
            };
            if(shape==2){h(.5f,-50,55);h(.5f,50,55);h(.6f,0,18);foreach(float x in new[]{52f,68f,152f,168f})Ellipse(vh,p(x,92),3*r.width/220,3*r.height/300,edge);Ellipse(vh,p(100,70),4*r.width/220,4*r.height/300,edge);Ellipse(vh,p(120,70),4*r.width/220,4*r.height/300,edge);}
            else
            {
                h(1,0,0);
                if(shape==1){var vr=new Rect(p(62,128),new Vector2(96*r.width/220,58*r.height/300));Rounded(vh,vr,10,edge);vr.xMin+=2;vr.xMax-=2;vr.yMin+=2;vr.yMax-=2;Rounded(vh,vr,8,H("2a1220"));for(int n=0;n<25;n++){float t=n/25f,u=(n+1)/25f;Line(vh,p(72+76*t,100-60*t*(1-t)),p(72+76*u,100-60*u*(1-u)),3,H("ffd0da"));}}
                var eye=edge;eye.a=Mathf.Lerp(.25f,1,.5f+.5f*Mathf.Cos(Clock*Mathf.PI*2/1.4f));
                Ellipse(vh,p(shape==1?92:88,shape==1?92:96),(shape==1?5:9)*r.width/220,4*r.height/300,eye);Ellipse(vh,p(shape==1?128:132,shape==1?92:96),(shape==1?5:9)*r.width/220,4*r.height/300,eye);
                if(shape==0&&Kind!="bossRival")for(int x=40;x<180;x+=14)Line(vh,p(x,210),p(x+6,210),2,new Color(edge.r,edge.g,edge.b,.6f));
            }
        }
        private static void Add(VertexHelper vh,Vector2 p,Color c){var v=UIVertex.simpleVert;v.position=p;v.color=c;vh.AddVert(v);}
        private static void Quad(VertexHelper vh,Rect r,Color c)=>Poly(vh,new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax),new Vector2(r.xMin,r.yMax)},c);
        private static void Poly(VertexHelper vh,Vector2[] p,Color c){int s=vh.currentVertCount;foreach(var a in p)Add(vh,a,c);for(int i=1;i<p.Length-1;i++)vh.AddTriangle(s,s+i,s+i+1);}
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float w,Color c){var n=new Vector2(-(b-a).y,(b-a).x).normalized*w/2;Poly(vh,new[]{a-n,b-n,b+n,a+n},c);}
        private static void Ellipse(VertexHelper vh,Vector2 p,float x,float y,Color c){int s=vh.currentVertCount;Add(vh,p,c);for(int i=0;i<=40;i++){float a=i*Mathf.PI/20;Add(vh,p+new Vector2(Mathf.Cos(a)*x,Mathf.Sin(a)*y),c);if(i>0)vh.AddTriangle(s,s+i,s+i+1);}}
        private static void Rounded(VertexHelper vh,Rect r,float radius,Color c)
        {
            int s=vh.currentVertCount;Add(vh,r.center,c);radius=Mathf.Min(radius,Mathf.Min(r.width,r.height)/2);
            for(int k=0;k<4;k++)for(int i=0;i<=12;i++){float a=(k*90+i*7.5f)*Mathf.Deg2Rad;var center=new Vector2(k==0||k==3?r.xMax-radius:r.xMin+radius,k<2?r.yMax-radius:r.yMin+radius);Add(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c);}
            int count=vh.currentVertCount-s-1;for(int i=0;i<count;i++)vh.AddTriangle(s,s+1+i,s+1+(i+1)%count);
        }
    }
    public sealed class OpsOpeningMotion : MonoBehaviour
    {
        public OpsGame Owner;public string Kind;public float Duration=.6f,Delay,Angle;private float elapsed;private Vector2 start;private CanvasGroup group;
        private OpsPresentationWait timing;
        private void Awake(){start=((RectTransform)transform).anchoredPosition;group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;}
        private void Update()
        {
            if(timing==null)timing=Owner.BeginOpeningMotion(name,Duration+Delay,transform);
            timing.Advance(Time.unscaledDeltaTime,Owner.FastPresentation);elapsed=timing.Elapsed;float t=Mathf.Clamp01((elapsed-Delay)/Duration),e=1-Mathf.Pow(1-t,3);var r=(RectTransform)transform;
            if(Owner.ReducedMotion){group.alpha=Kind=="flash"?0:1;return;}
            group.alpha=Kind=="flash"?1-t:Mathf.Clamp01(t*4);
            if(Kind=="rise")r.anchoredPosition=start+Vector2.down*26*(1-e);
            if(Kind=="left"||Kind=="right")r.anchoredPosition=start+Vector2.right*(Kind=="left"?-120:160)*(1-e);
            if(Kind=="zoom")r.localScale=Vector3.one*Mathf.Lerp(1.25f,1,e);
            if(Kind=="pop"||Kind=="big"||Kind=="stamp")
            {float scale=t<.6f?Mathf.Lerp(Kind=="pop"?.4f:Kind=="stamp"?2.4f:2.6f,.95f,t/.6f):Mathf.Lerp(.95f,1,(t-.6f)/.4f);r.localScale=Vector3.one*scale;if(Kind=="stamp")r.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(18,8,e));}
            if(Kind=="link"||Kind=="rival"||Kind=="bossRival"){var g=GetComponent<OpsOpeningGraphic>();g.Clock=elapsed;g.SetVerticesDirty();}
        }
    }
}
