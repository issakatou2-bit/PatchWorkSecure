using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // 手本のCSSで描かれた机・罫紙・テープ・天気をUIメッシュで再現する。
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OpsDiaryGraphic:MaskableGraphic
    {
        public string Kind;
        public OpsGame Owner;
        private void Update(){if((Kind=="sun"||Kind=="rain")&&(Owner==null||!Owner.ReducedMotion))SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();raycastTarget=false;var r=rectTransform.rect;float w=r.width,h=r.height;
            if(Kind=="page-left"||Kind=="page-right")
            {
                Add(v,r.center,color);
                for(int corner=0;corner<4;corner++)
                {
                    bool right=corner==0||corner==3;float radius=(Kind=="page-left"?right:!right)?4:18;
                    Vector2 center=new Vector2(right?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                    for(int i=0;i<=12;i++){float a=(corner*90+i*7.5f)*Mathf.Deg2Rad;Add(v,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color);}
                }
                for(int i=1;i<v.currentVertCount-1;i++)v.AddTriangle(0,i,i+1);v.AddTriangle(0,v.currentVertCount-1,1);
            }
            else if(Kind=="sticker")
            {
                float radius=Mathf.Min(w,h)*.5f;Vector2 center=r.center+new Vector2(-w*.15f,h*.2f);
                for(int j=0;j<12;j++)for(int i=0;i<64;i++)
                {
                    int start=v.currentVertCount;
                    for(int k=0;k<4;k++)
                    {
                        float a=(i+(k==1||k==2?1:0))*Mathf.PI*2/64,t=(j+(k>=2?1:0))/12f;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),offset=center-r.center;
                        float b=Vector2.Dot(offset,d),length=-b+Mathf.Sqrt(b*b+radius*radius-offset.sqrMagnitude);
                        Add(v,center+d*length*t,Color.Lerp(Color.Lerp(color,Color.white,.4f),color,t));
                    }
                    v.AddTriangle(start,start+1,start+2);v.AddTriangle(start,start+2,start+3);
                }
            }
            else if(Kind=="desk")
            {
                for(float x=0;x<w;x+=12){Quad(v,r,x,0,3,h,new Color32(201,154,107,255));Quad(v,r,x+3,0,4,h,new Color32(196,148,95,255));Quad(v,r,x+7,0,5,h,new Color32(207,159,112,255));}
                for(int y=0;y<24;y++)for(int x=0;x<40;x++)
                {
                    Overlay(v,r,x*w/40,y*h/24,w/40,h/24,true);Overlay(v,r,x*w/40,y*h/24,w/40,h/24,false);
                }
            }
            else if(Kind=="binding-left"||Kind=="binding-right")
            {for(int i=0;i<40;i++){float t=(i+.5f)/40;Quad(v,r,i*w/40,0,w/40,h,new Color(.66f,.52f,.3f,(Kind=="binding-left"?t:1-t)*.14f));}}
            else if(Kind=="pink-tape"||Kind=="blue-tape")
            {
                bool blue=Kind=="blue-tape";
                for(int y=0;y<17;y++)for(int x=0;x<Mathf.CeilToInt(w/2);x++)Quad(v,r,x*2,y*2,Mathf.Min(2,w-x*2),2,blue?((x+y)/5%2==0?new Color32(127,196,255,191):new Color32(180,222,255,191)):((x+y)/5%2==0?new Color32(255,150,180,191):new Color32(255,190,210,191)));
            }
            else if(Kind=="circle")Circle(v,r,w/2,h/2,Mathf.Min(w,h)/2,color);
            else if(Kind=="sun")
            {for(int i=0;i<8;i++){float a=i*Mathf.PI/4+(Owner!=null&&Owner.ReducedMotion?0:Time.unscaledTime*Mathf.PI/6);Circle(v,r,23+Mathf.Cos(a)*21,23+Mathf.Sin(a)*21,2,new Color32(255,192,46,255));}Circle(v,r,23,23,15,new Color32(255,192,46,255));Circle(v,r,19,19,7,new Color32(255,227,138,180));}
            else
            {Circle(v,r,15,24,12,new Color32(185,196,214,255));Circle(v,r,28,17,13,new Color32(201,210,224,255));Circle(v,r,36,26,10,new Color32(185,196,214,255));if(Kind=="rain")for(int i=0;i<3;i++){float phase=Owner!=null&&Owner.ReducedMotion?0:Mathf.Repeat(Time.unscaledTime+i*.3f,1);Quad(v,r,12+i*11,35+14*phase,4,9,new Color(.498f,.706f,.902f,1-phase));}}
        }
        private static void Overlay(VertexHelper v,Rect r,float x,float y,float w,float h,bool warm)
        {
            int n=v.currentVertCount;foreach(var point in new[]{new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h)})
            {
                float fx=point.x/r.width,fy=point.y/r.height;
                float alpha=warm?Mathf.Clamp01(1-Mathf.Sqrt(Mathf.Pow((fx-.3f)/.8f,2)+Mathf.Pow((fy-.2f)/.65f,2)))*.35f:Mathf.Clamp01((Mathf.Sqrt(Mathf.Pow((fx-.5f)*2,2)+Mathf.Pow((fy-.5f)*2,2))-.55f)/.8f)*.45f;
                v.AddVert(new Vector3(r.x+point.x,r.yMax-point.y),warm?new Color(1,.86f,.7f,alpha):new Color(.157f,.078f,.039f,alpha),Vector2.zero);
            }
            v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);
        }
        private static void Quad(VertexHelper v,Rect r,float x,float y,float w,float h,Color c)
        {int n=v.currentVertCount;v.AddVert(new Vector3(r.x+x,r.yMax-y),c,Vector2.zero);v.AddVert(new Vector3(r.x+x+w,r.yMax-y),c,Vector2.zero);v.AddVert(new Vector3(r.x+x+w,r.yMax-y-h),c,Vector2.zero);v.AddVert(new Vector3(r.x+x,r.yMax-y-h),c,Vector2.zero);v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);}
        private static void Add(VertexHelper v,Vector2 position,Color tint){v.AddVert(position,tint,Vector2.zero);}
        private static void Circle(VertexHelper v,Rect r,float x,float y,float radius,Color c)
        {int n=v.currentVertCount;v.AddVert(new Vector3(r.x+x,r.yMax-y),c,Vector2.zero);for(int i=0;i<=48;i++){float a=i*Mathf.PI*2/48;v.AddVert(new Vector3(r.x+x+Mathf.Cos(a)*radius,r.yMax-y+Mathf.Sin(a)*radius),c,Vector2.zero);if(i>0)v.AddTriangle(n,n+i,n+i+1);}}
    }
    public sealed class OpsDiaryMotion:MonoBehaviour
    {
        public string Kind;public OpsGame Owner;public float Delay,Angle;private float time;private RectTransform rect;private Vector2 origin;
        private OpsPresentationWait timing;
        private void Start(){rect=(RectTransform)transform;origin=rect.anchoredPosition;}
        private void Update()
        {
            if(Kind=="wave"){time+=Time.unscaledDeltaTime;rect.localScale=new Vector3(1,Owner!=null&&Owner.ReducedMotion?1:.7f+.3f*Mathf.Cos((time-Delay)*Mathf.PI*2),1);return;}
            float duration=Kind=="open"?OpsPresentationTiming.DiaryOpen:OpsPresentationTiming.DiarySticker;
            if(timing==null&&Owner!=null)timing=Owner.BeginDiaryMotion(name,duration+Delay,transform);
            if(timing!=null){timing.Advance(Time.unscaledDeltaTime,Owner.FastPresentation);time=timing.Elapsed;}else time+=Time.unscaledDeltaTime;
            float t=Owner!=null&&Owner.ReducedMotion?1:Mathf.Clamp01((time-Delay)/duration);float ease=1-Mathf.Pow(1-t,3);
            rect.localScale=Vector3.one*(Kind=="open"?Mathf.Lerp(.96f,1,ease):t<.7f?Mathf.Lerp(1.8f,.94f,t/.7f):Mathf.Lerp(.94f,1,(t-.7f)/.3f));
            if(Kind=="open")rect.anchoredPosition=origin+new Vector2(0,Mathf.Lerp(-30,0,ease));else rect.localEulerAngles=new Vector3(0,0,Mathf.Lerp(-30,Angle,ease));
            if(t>=1)enabled=false;
        }
    }
}
