using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        // 開いている画面そのものを低解像度でぼかす。別のオフィス絵で代用しない。
        private void WindowBackdrop()
        {
            var texture=CaptureWindowBackground();
            modal=Box(screen,"ModalBlocker",0,0,1600,900,new Color(.06f,.08f,.16f,.62f));
            if(texture==null)return;
            var r=Rect(modal,"WindowBackgroundBlur",0,0,1600,900);
            var image=r.gameObject.AddComponent<RawImage>();image.texture=texture;image.color=new Color(.55f,.55f,.55f,1);image.raycastTarget=false;
            r.gameObject.AddComponent<OpsBackdropTexture>().Texture=texture;
        }
        private Texture2D CaptureWindowBackground()
        {
            if(!Application.isPlaying)return null;
            var canvas=Surface.GetComponentInParent<Canvas>();if(canvas==null)return null;
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float plane=canvas.planeDistance;
            var active=RenderTexture.active;var target=RenderTexture.GetTemporary(1600,900,24);var small=RenderTexture.GetTemporary(320,180,0);
            var go=new GameObject("窓背景の撮影",typeof(Camera));var camera=go.GetComponent<Camera>();camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=PlanInk;
            Texture2D texture=null;
            try
            {
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();
                camera.Render();Graphics.Blit(target,small);RenderTexture.active=small;
                texture=new Texture2D(320,180,TextureFormat.RGB24,false);texture.ReadPixels(new UnityEngine.Rect(0,0,320,180),0,0);texture.Apply();
                var pixels=texture.GetPixels32();var buffer=new Color32[pixels.Length];
                // 2回の分離ボックスフィルター。半径2pxで、画面換算およそ10pxのぼかし。
                for(int pass=0;pass<2;pass++)
                {
                    BlurLine(pixels,buffer,320,180,true);BlurLine(buffer,pixels,320,180,false);
                }
                texture.SetPixels32(pixels);texture.Apply();return texture;
            }
            finally
            {
                canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=plane;camera.targetTexture=null;RenderTexture.active=active;
                RenderTexture.ReleaseTemporary(target);RenderTexture.ReleaseTemporary(small);Destroy(go);Canvas.ForceUpdateCanvases();
            }
        }
        private static void BlurLine(Color32[] input,Color32[] output,int width,int height,bool horizontal)
        {
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                int r=0,g=0,b=0;
                for(int d=-2;d<=2;d++){var c=input[Mathf.Clamp(y+(horizontal?0:d),0,height-1)*width+Mathf.Clamp(x+(horizontal?d:0),0,width-1)];r+=c.r;g+=c.g;b+=c.b;}
                output[y*width+x]=new Color32((byte)(r/5),(byte)(g/5),(byte)(b/5),255);
            }
        }
    }
    public sealed class OpsBackdropTexture:MonoBehaviour
    {
        public Texture2D Texture;
        private void OnDestroy(){if(Texture!=null)Destroy(Texture);}
    }
}
