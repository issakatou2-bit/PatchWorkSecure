using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // 表示専用。年の状態・点数・抽選には触れない。
    public sealed class OpsMinigameVisual:MonoBehaviour
    {
        public OpsGame Owner;
        public string Kind;
        public float Duration=1;
        public Vector2 Direction;
        private RectTransform rect;
        private Vector2 origin;
        private Vector3 originalScale;
        private Quaternion originalRotation;
        private Graphic graphic;
        private Color color;
        private float started;
        private bool initialized;
        private OpsPresentationWait stampTiming;
        private void Start()
        {rect=(RectTransform)transform;origin=rect.anchoredPosition;originalScale=rect.localScale;originalRotation=rect.localRotation;graphic=GetComponent<Graphic>();if(graphic!=null)color=graphic.color;started=Time.unscaledTime;initialized=true;}
        public void Stop()
        {enabled=false;if(!initialized)return;rect.anchoredPosition=origin;rect.localScale=originalScale;rect.localRotation=originalRotation;if(graphic!=null)graphic.color=color;}
        private void Update()
        {
            float age=Time.unscaledTime-started,p=Mathf.Clamp01(age/Duration);bool reduced=Owner!=null&&Owner.ReducedMotion;
            if(Kind=="stamp"&&Owner!=null){if(stampTiming==null)stampTiming=Owner.BeginResultStamp(Duration,transform);stampTiming.Advance(Time.unscaledDeltaTime,Owner.FastPresentation);p=Mathf.Clamp01(stampTiming.Elapsed/Duration);}
            if(Kind=="grid")
            {
                var grid=GetComponent<OpsMinigameGraphic>();grid.Progress=reduced?0:Mathf.Repeat(age/Duration,1)*48;grid.SetVerticesDirty();return;
            }
            if(Kind=="mote")
            {
                p=Mathf.Repeat(age/Duration+Direction.y,1);rect.anchoredPosition=origin+Vector2.up*(reduced?0:240*p);
                graphic.color=new Color(color.r,color.g,color.b,reduced?0:color.a*Mathf.Min(p*5,(1-p)*5));return;
            }
            if(Kind=="danger")
            {graphic.color=new Color(color.r,color.g,color.b,reduced?.15f:color.a*(.5f+.5f*Mathf.Sin(age*Mathf.PI*2/Duration)));return;}
            if(Kind=="line")
            {
                var line=GetComponent<OpsMinigameGraphic>();line.Progress=reduced?1:Mathf.Clamp01(age/.28f);line.SetVerticesDirty();
                graphic.color=new Color(color.r,color.g,color.b,1-p);
            }
            if(Kind=="ring")
            {rect.localScale=Vector3.one*(reduced?1:Mathf.Lerp(1,6.5f,p));graphic.color=new Color(1,1,1,1-p);}
            if(Kind=="shard")
            {rect.anchoredPosition=origin+(reduced?Vector2.zero:new Vector2(Direction.x,-Direction.y)*p);rect.localScale=Vector3.one*(reduced?1:1-.7f*p);rect.localEulerAngles=new Vector3(0,0,reduced?0:260*p);graphic.color=new Color(color.r,color.g,color.b,reduced?0:1-p);}
            if(Kind=="pop")
            {rect.anchoredPosition=origin+Vector2.up*(reduced?0:60*p);rect.localScale=Vector3.one*(reduced?1:p<.2f?Mathf.Lerp(.6f,1.15f,p/.2f):Mathf.Lerp(1.15f,1,(p-.2f)/.8f));graphic.color=new Color(color.r,color.g,color.b,Mathf.Min(p*10,(1-p)*2));}
            if(Kind=="hit")rect.localScale=originalScale*(reduced?1:1.18f);
            if(Kind=="shake")rect.anchoredPosition=origin+Vector2.right*(reduced?0:Mathf.Sin(p*Mathf.PI*6)*8*(1-p));
            if(Kind=="stamp")
            {
                rect.localScale=originalScale*(reduced?1:p<.6f?Mathf.Lerp(2.4f,.92f,p/.6f):Mathf.Lerp(.92f,1,(p-.6f)/.4f));
                rect.localEulerAngles=new Vector3(0,0,reduced?0:Mathf.Lerp(-12,-4,p));
            }
            if(Kind=="enter")
            {rect.anchoredPosition=origin+Vector2.down*(reduced?0:40*(1-p));rect.localScale=originalScale*(reduced?1:Mathf.Lerp(.95f,1,p));}
            if(Kind=="mail-out")
            {rect.anchoredPosition=origin+Vector2.right*(reduced?0:Direction.x*rect.rect.width*1.2f*p);rect.localEulerAngles=new Vector3(0,0,reduced?0:-Direction.x*10*p);var group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();group.alpha=1-p;}
            if(p<1)return;
            if(Kind=="hit"||Kind=="shake"||Kind=="enter"){Stop();Destroy(this);}
            else if(Kind=="stamp"){rect.localScale=originalScale;Destroy(this);}
            else Destroy(gameObject);
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OpsMinigameGraphic:MaskableGraphic
    {
        public string Kind;
        public Vector2 From,To;
        public float Progress=1;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(Kind=="grid")
            {
                for(float x=r.xMin+Progress-48;x<=r.xMax;x+=48)Line(vh,new Vector2(x,r.yMin),new Vector2(x,r.yMax),1);
                for(float y=r.yMin+Progress-48;y<=r.yMax;y+=48)Line(vh,new Vector2(r.xMin,y),new Vector2(r.xMax,y),1);
            }
            else if(Kind=="danger-edge")
            {
                // 手本の内側の影。中央は透明、丸い縁から内側だけへ発光する。
                EdgeBand(vh,r,0,20,1,.65f);EdgeBand(vh,r,20,90,.65f,0);
            }
            else if(Kind=="line")Line(vh,From,Vector2.Lerp(From,To,Progress),4);
            else if(Kind=="dashed-line")
            {
                float length=Vector2.Distance(From,To);var direction=length>0?(To-From)/length:Vector2.zero;
                for(float d=0;d<length;d+=12)Line(vh,From+direction*d,From+direction*Mathf.Min(length,d+6),3);
            }
            else if(Kind=="slash")Line(vh,new Vector2(r.xMin,r.center.y-6),new Vector2(r.xMax,r.center.y+6),4);
            else if(Kind=="ring")
            {
                for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;float radius=r.width*.45f;Line(vh,r.center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,r.center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,1.2f);}
            }
            else if(Kind=="pc")
            {
                Vector2 a=new Vector2(r.xMin+r.width*.125f,r.yMin+r.height*.33f),b=new Vector2(r.xMax-r.width*.125f,r.yMax-r.height*.17f);
                Line(vh,a,new Vector2(b.x,a.y),2.5f);Line(vh,new Vector2(b.x,a.y),b,2.5f);Line(vh,b,new Vector2(a.x,b.y),2.5f);Line(vh,new Vector2(a.x,b.y),a,2.5f);
                Line(vh,new Vector2(r.center.x,a.y),new Vector2(r.center.x,r.yMin+r.height*.17f),2.5f);
                Line(vh,new Vector2(r.xMin+r.width*.33f,r.yMin+r.height*.17f),new Vector2(r.xMax-r.width*.33f,r.yMin+r.height*.17f),2.5f);
            }
        }
        private static Vector2 EdgePoint(Rect r,float inset,int index)
        {
            int corner=index/9,step=index%9;float radius=Mathf.Max(0,22-inset);
            var center=new Vector2(corner==0||corner==3?r.xMax-inset-radius:r.xMin+inset+radius,
                corner<2?r.yMax-inset-radius:r.yMin+inset+radius);
            float angle=(corner*90+step*90f/8)*Mathf.Deg2Rad;
            return center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
        }
        private void EdgeBand(VertexHelper vh,Rect r,float outer,float inner,float outerAlpha,float innerAlpha)
        {
            for(int i=0;i<36;i++)
            {
                int start=vh.currentVertCount,j=(i+1)%36;
                EdgeVertex(vh,EdgePoint(r,outer,i),outerAlpha);EdgeVertex(vh,EdgePoint(r,outer,j),outerAlpha);
                EdgeVertex(vh,EdgePoint(r,inner,j),innerAlpha);EdgeVertex(vh,EdgePoint(r,inner,i),innerAlpha);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
        private void EdgeVertex(VertexHelper vh,Vector2 point,float alpha)
        {var v=UIVertex.simpleVert;v.position=point;v.color=new Color(color.r,color.g,color.b,color.a*alpha);vh.AddVert(v);}
        private void Line(VertexHelper vh,Vector2 a,Vector2 b,float width)
        {
            var n=new Vector2(-(b-a).y,(b-a).x).normalized*(width*.5f);int start=vh.currentVertCount;
            foreach(var point in new[]{a-n,b-n,b+n,a+n}){var v=UIVertex.simpleVert;v.position=point;v.color=color;vh.AddVert(v);}
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
