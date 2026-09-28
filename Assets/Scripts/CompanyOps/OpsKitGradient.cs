using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    // 標準UIの9-sliceに頂点の色を付ける。影・文字・当たり判定は変えない。
    public sealed class OpsKitGradient : BaseMeshEffect
    {
        public Color Top = Color.white, Bottom = Color.white;
        public bool Horizontal;
        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            var rect = ((RectTransform)transform).rect;
            var v = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref v, i);
                float t = Horizontal ? Mathf.InverseLerp(rect.xMin, rect.xMax, v.position.x) : Mathf.InverseLerp(rect.yMin, rect.yMax, v.position.y);
                v.color *= Color.Lerp(Bottom, Top, t); mesh.SetUIVertex(v, i);
            }
        }
    }
}
