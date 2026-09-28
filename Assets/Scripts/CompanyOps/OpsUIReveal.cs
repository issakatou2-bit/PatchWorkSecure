using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    // 再生成される窓でも出現を一度だけ再生。省演出は透明度だけ。
    public sealed class OpsUIReveal : MonoBehaviour
    {
        public OpsGame Owner;
        public float Duration = .45f, Delay;
        public bool Stamp;
        private CanvasGroup group;
        private float elapsed;
        private Quaternion rotation;
        private void Awake() { group = gameObject.AddComponent<CanvasGroup>(); group.alpha = 0; rotation = transform.localRotation; }
        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01((elapsed - Delay) / Duration);
            group.alpha = t;
            if (Owner != null && Owner.ReducedMotion) transform.localScale = Vector3.one;
            else
            {
                float back = 1 + 2.70158f * Mathf.Pow(t - 1, 3) + 1.70158f * Mathf.Pow(t - 1, 2);
                transform.localScale = Vector3.one * Mathf.LerpUnclamped(Stamp ? 2.4f : .9f, 1, back);
            }
            transform.localRotation = rotation;
            if (t >= 1) { transform.localScale = Vector3.one; enabled = false; }
        }
    }
}
