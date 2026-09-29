using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    // SceneBuilderが既存のCompanyOpsGameに付ける。時計は画面の再生成とは分離する。
    public sealed class OpsMinigameHost : MonoBehaviour
    {
        public OpsGame Owner;
        private void Update(){if(Owner!=null){Owner.TickMinigame(Time.unscaledDeltaTime);Owner.TickMinigameInput();}}
    }
}
