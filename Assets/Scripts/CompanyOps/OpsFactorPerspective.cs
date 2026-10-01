using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // Overlay CanvasにもCSS perspective:1400pxと同じ遠近感を付ける。既存の絵・マスクを使い、カメラやシェーダーを追加しない。
    public sealed class OpsFactorPerspective : BaseMeshEffect
    {
        public RectTransform Root;
        private Vector3 Project(Vector3 local)
        {
            Vector3 center=Root.TransformPoint(Root.rect.center),world=transform.TransformPoint(local),delta=world-center;
            float focal=1400*Root.parent.TransformVector(Vector3.right).magnitude;
            float factor=focal/Mathf.Max(focal*.25f,focal-delta.z);
            world.x=center.x+delta.x*factor;world.y=center.y+delta.y*factor;return transform.InverseTransformPoint(world);
        }
        public override void ModifyMesh(VertexHelper vh)
        {
            if(!IsActive()||Root==null||graphic is TextMeshProUGUI)return;
            var v=UIVertex.simpleVert;for(int i=0;i<vh.currentVertCount;i++){vh.PopulateUIVertex(ref v,i);v.position=Project(v.position);vh.SetUIVertex(v,i);}
        }
        public void Refresh()
        {
            if(graphic is TextMeshProUGUI text)
            {
                text.ForceMeshUpdate();var info=text.textInfo;
                for(int m=0;m<info.meshInfo.Length;m++){var mesh=info.meshInfo[m];for(int i=0;i<mesh.vertexCount;i++)mesh.vertices[i]=Project(mesh.vertices[i]);}
                text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }
            else graphic.SetVerticesDirty();
        }
    }
}
