using UnityEngine;
namespace PatchWorkSecure.CompanyOps
{
    // 全身と顔アイコンを混同しない。表示位置は共通キャンバスの足元で固定する。
    public sealed class OpsPortraitIdentity : MonoBehaviour
    {
        public string PoseId="pose_fists";
        public bool FaceIcon;
    }
}
