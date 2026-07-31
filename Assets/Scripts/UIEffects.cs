using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PatchWorkSecure
{
    /// <summary>
    /// 「手応え」を作る画面演出をまとめたコンポーネント。
    /// フラッシュ・シェイク・バナー・浮遊テキスト・バーストなど、
    /// ゲームロジックとは無関係な「気持ちよさ」だけを担当する。
    ///
    /// GameManagerから呼ぶ想定で、未割当のフィールドがあってもその演出だけ黙って省略される
    /// （素材やUI要素が揃っていない段階でも呼び出し側を書き換えずに済むようにするため）。
    /// </summary>
    public class UIEffects : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private RectTransform shakeRoot;    // 揺らす対象（通常はCanvas）
        [SerializeField] private Image flashOverlay;         // 全画面フラッシュ用
        [SerializeField] private RectTransform effectLayer;  // 浮遊テキスト/バーストの生成先
        [SerializeField] private TextMeshProUGUI bannerText; // 中央の大きな結果表示
        [SerializeField] private Sprite circleSprite;        // バースト用の円スプライト
        [SerializeField] private TMP_FontAsset font;         // 動的生成テキスト用（未指定ならTMPの既定）

        // ---- よく使う色 ----
        public static readonly Color Good = new Color(0.42f, 0.88f, 0.50f);
        public static readonly Color Bad = new Color(0.96f, 0.36f, 0.36f);
        public static readonly Color Gold = new Color(1.00f, 0.82f, 0.25f);

        private Vector2 _shakeHome;
        private Coroutine _flashRoutine, _shakeRoutine, _bannerRoutine;

        private void Awake()
        {
            if (shakeRoot != null) _shakeHome = shakeRoot.anchoredPosition;
            if (flashOverlay != null)
            {
                flashOverlay.raycastTarget = false;
                SetAlpha(flashOverlay, 0f);
            }
            if (bannerText != null)
            {
                bannerText.raycastTarget = false;
                bannerText.gameObject.SetActive(false);
            }
        }

        // ================= 全画面フラッシュ =================

        /// <summary>画面全体を一瞬だけ指定色に染める。被弾＝赤、成功＝金、のように使う。</summary>
        public void Flash(Color color, float peakAlpha = 0.35f, float duration = 0.4f)
        {
            if (flashOverlay == null) return;
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(color, peakAlpha, duration));
        }

        private IEnumerator FlashRoutine(Color color, float peakAlpha, float duration)
        {
            // 立ち上がりを速く、余韻を長くすると「衝撃」らしく見える。
            const float attack = 0.06f;
            float t = 0f;
            while (t < attack)
            {
                t += Time.deltaTime;
                SetColorAlpha(flashOverlay, color, Mathf.Lerp(0f, peakAlpha, t / attack));
                yield return null;
            }
            t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetColorAlpha(flashOverlay, color, Mathf.Lerp(peakAlpha, 0f, t / duration));
                yield return null;
            }
            SetAlpha(flashOverlay, 0f);
            _flashRoutine = null;
        }

        // ================= シェイク =================

        /// <summary>画面を短時間揺らす。減衰するので終わり際は自然に収まる。</summary>
        public void Shake(float duration = 0.35f, float magnitude = 16f)
        {
            if (shakeRoot == null) return;
            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                shakeRoot.anchoredPosition = _shakeHome; // 多重呼び出しでも定位置に戻せるようにする
            }
            _shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float damper = 1f - (t / duration);
                shakeRoot.anchoredPosition = _shakeHome + Random.insideUnitCircle * magnitude * damper * damper;
                yield return null;
            }
            shakeRoot.anchoredPosition = _shakeHome;
            _shakeRoutine = null;
        }

        // ================= 中央バナー =================

        /// <summary>「防御成功！」「被弾！」のような大きな文字を中央に叩きつける。</summary>
        public void Banner(string text, Color color, float hold = 0.7f)
        {
            if (bannerText == null) return;
            if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
            _bannerRoutine = StartCoroutine(BannerRoutine(text, color, hold));
        }

        private IEnumerator BannerRoutine(string text, Color color, float hold)
        {
            var rt = bannerText.rectTransform;
            bannerText.gameObject.SetActive(true);
            bannerText.text = text;

            // 大きめから叩き込むように縮めると「着弾した」感じが出る。
            const float punchIn = 0.18f;
            float t = 0f;
            while (t < punchIn)
            {
                t += Time.deltaTime;
                float p = t / punchIn;
                rt.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, EaseOutCubic(p));
                bannerText.color = new Color(color.r, color.g, color.b, p);
                yield return null;
            }
            rt.localScale = Vector3.one;
            bannerText.color = color;

            yield return new WaitForSeconds(hold);

            const float fade = 0.3f;
            t = 0f;
            while (t < fade)
            {
                t += Time.deltaTime;
                float p = t / fade;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, p);
                bannerText.color = new Color(color.r, color.g, color.b, 1f - p);
                yield return null;
            }
            rt.localScale = Vector3.one;
            bannerText.gameObject.SetActive(false);
            _bannerRoutine = null;
        }

        // ================= 浮遊テキスト =================

        /// <summary>指定したUI要素の上に「+12」「-8」などをふわっと浮かせる。</summary>
        public void FloatingText(Transform anchor, string text, Color color, int fontSize = 30)
        {
            if (anchor == null || effectLayer == null) return;

            var go = new GameObject("FloatingText", typeof(TextMeshProUGUI));
            go.transform.SetParent(effectLayer, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 44);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = ToEffectLayerPoint(anchor.position) + new Vector2(0, 24);

            StartCoroutine(FloatingTextRoutine(rt, tmp));
        }

        private IEnumerator FloatingTextRoutine(RectTransform rt, TextMeshProUGUI tmp)
        {
            Vector2 start = rt.anchoredPosition;
            Vector2 end = start + new Vector2(0, 70);
            Color color = tmp.color;

            // 飛び出し（少し大きく出てから戻る）
            const float pop = 0.16f;
            float t = 0f;
            while (t < pop)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.25f, EaseOutCubic(t / pop));
                yield return null;
            }

            const float drift = 0.85f;
            t = 0f;
            while (t < drift)
            {
                t += Time.deltaTime;
                float p = t / drift;
                rt.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, Mathf.Min(1f, p * 4f));
                rt.anchoredPosition = Vector2.Lerp(start, end, EaseOutCubic(p));
                tmp.color = new Color(color.r, color.g, color.b, 1f - p * p);
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        // ================= バースト =================

        /// <summary>指定位置から円が広がって消える。成功・獲得の「効いた感」を出す。</summary>
        public void Burst(Transform anchor, Color color, float maxSize = 300f)
        {
            if (anchor == null || effectLayer == null || circleSprite == null) return;

            var go = new GameObject("Burst", typeof(Image));
            go.transform.SetParent(effectLayer, false);
            var img = go.GetComponent<Image>();
            img.sprite = circleSprite;
            img.color = color;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(maxSize, maxSize);
            rt.anchoredPosition = ToEffectLayerPoint(anchor.position);

            StartCoroutine(BurstRoutine(rt, img, color));
        }

        private IEnumerator BurstRoutine(RectTransform rt, Image img, Color color)
        {
            const float duration = 0.45f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                rt.localScale = Vector3.one * Mathf.Lerp(0.15f, 1f, EaseOutCubic(p));
                img.color = new Color(color.r, color.g, color.b, (1f - p) * 0.55f);
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        // ================= スケールのパンチ =================

        /// <summary>対象を一瞬だけ大きくして戻す。数値が動いた場所を目立たせるのに使う。</summary>
        public void Punch(RectTransform target, float from = 1.25f, float duration = 0.28f)
        {
            if (target == null) return;
            StartCoroutine(PunchRoutine(target, from, duration));
        }

        private IEnumerator PunchRoutine(RectTransform target, float from, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                target.localScale = Vector3.one * Mathf.Lerp(from, 1f, EaseOutCubic(t / duration));
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        // ================= 共通ヘルパー =================

        /// <summary>ワールド座標を、効果レイヤーのローカル座標に変換する。</summary>
        private Vector2 ToEffectLayerPoint(Vector3 worldPosition)
        {
            // Screen Space - Overlay のCanvasではカメラにnullを渡すのが正しい。
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                effectLayer, screenPoint, null, out Vector2 local);
            return local;
        }

        private static float EaseOutCubic(float p)
        {
            p = Mathf.Clamp01(p);
            float inv = 1f - p;
            return 1f - inv * inv * inv;
        }

        private static void SetAlpha(Image img, float a)
        {
            var c = img.color;
            c.a = a;
            img.color = c;
        }

        private static void SetColorAlpha(Image img, Color color, float a)
        {
            img.color = new Color(color.r, color.g, color.b, a);
        }
    }
}
