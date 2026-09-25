using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure
{
    /// <summary>小さくても区別できる対策アイコン。文字や画像生成に依存せずUIの図形で描く。</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class DefenseGlyph : MaskableGraphic
    {
        private string _key = "mfa";
        public void SetKey(string key) { _key = key; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var ink = new Color(0.88f, 0.93f, 0.88f);
            var dark = new Color(0.12f, 0.20f, 0.23f);
            switch (_key)
            {
                case "firewall":
                    for (int y=2;y<15;y+=4)
                        for(int x=1;x<15;x+=5) Box(mesh,x,y,4,3,ink);
                    break;
                case "training":
                    Box(mesh,1,3,6,11,ink); Box(mesh,9,3,6,11,ink);
                    Box(mesh,7,1,2,12,ink);
                    for(int y=6;y<13;y+=3){Box(mesh,2,y,4,1,dark);Box(mesh,10,y,4,1,dark);}
                    break;
                case "backup":
                    for(int y=2;y<14;y+=5){Box(mesh,2,y,12,4,ink);Box(mesh,3,y+1,2,1,dark);Box(mesh,10,y+1,3,1,dark);}
                    break;
                case "vpn":
                    Box(mesh,3,1,10,8,ink);Box(mesh,4,8,2,5,ink);Box(mesh,10,8,2,5,ink);Box(mesh,5,12,6,2,ink);
                    Box(mesh,7,3,2,4,dark);break;
                case "idsIps":
                    Disc(mesh,7,10,5,ink);Disc(mesh,7,10,3,dark);
                    Box(mesh,10,4,3,3,ink);Box(mesh,12,1,3,3,ink);break;
                case "waf":
                    Disc(mesh,8,8,7,ink);Disc(mesh,8,8,5,dark);
                    Box(mesh,1,7,14,2,ink);Box(mesh,7,1,2,14,ink);
                    Box(mesh,3,4,10,1,ink);Box(mesh,3,11,10,1,ink);break;
                case "passwordPolicy":
                    Disc(mesh,5,11,4,ink);Disc(mesh,5,11,2,dark);
                    Box(mesh,7,6,3,4,ink);Box(mesh,9,3,3,4,ink);Box(mesh,11,2,4,2,ink);break;
                default:
                    Box(mesh,3,1,10,14,ink);Box(mesh,5,4,6,9,dark);Box(mesh,7,2,2,1,dark);break;
            }
        }
        private Vector2 Point(float x,float y)
        {
            var r=rectTransform.rect;
            return new Vector2(r.xMin+x*r.width/16f,r.yMin+y*r.height/16f);
        }
        private void Box(VertexHelper mesh,float x,float y,float w,float h,Color tint)
        {
            int start=mesh.currentVertCount;
            mesh.AddVert(Point(x,y),tint,Vector2.zero);mesh.AddVert(Point(x+w,y),tint,Vector2.zero);
            mesh.AddVert(Point(x+w,y+h),tint,Vector2.zero);mesh.AddVert(Point(x,y+h),tint,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
        private void Disc(VertexHelper mesh,float x,float y,float radius,Color tint)
        {
            int start=mesh.currentVertCount;
            mesh.AddVert(Point(x,y),tint,Vector2.zero);
            for(int i=0;i<16;i++){
                float a=i*Mathf.PI/8;
                mesh.AddVert(Point(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),tint,Vector2.zero);
            }
            for(int i=0;i<16;i++)mesh.AddTriangle(start,start+1+i,start+1+(i+1)%16);
        }
    }
}
