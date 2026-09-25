using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private static readonly Color Ink = Hex("102D33"), Panel = Hex("173940"), Edge = Hex("36565A"),
            Paper = Hex("F2EDD9"), Muted = Hex("A7BEB7"), Gold = Hex("EAC573"), Mint = Hex("A2D9B2"), Coral = Hex("EF917B");
        private RectTransform screen, modal, toast;
        private CanvasGroup toastGroup;
        private float toastUntil;
        private static Color Hex(string code) { ColorUtility.TryParseHtmlString("#" + code, out var c); return c; }
        private RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        private RectTransform Box(Transform parent, string name, float x, float y, float w, float h, Color color, bool outline = false)
        {
            var r = Rect(parent, name, x, y, w, h); var img = r.gameObject.AddComponent<Image>();
            img.color = color; img.sprite = PanelSprite; img.type = Image.Type.Sliced;
            if (outline) { var edge = r.gameObject.AddComponent<Outline>(); edge.effectColor = Edge; edge.effectDistance = new Vector2(1, -1); }
            return r;
        }
        private TextMeshProUGUI Text(Transform parent, string name, string value, float x, float y, float w, float h, float size = 20, Color? color = null)
        {
            var r = Rect(parent, name, x, y, w, h); var label = r.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font; label.fontSize = size; label.color = color ?? Paper; label.text = value;
            label.enableAutoSizing = true; label.fontSizeMin = size * .8f; label.fontSizeMax = size;
            label.lineSpacing = -8;
            label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false; return label;
        }
        private Button Button(Transform parent, string id, string label, float x, float y, float w, float h, Action action, Color? color = null, bool enabled = true)
        {
            // 動的選択肢はルートButton+Image、子TMPのプレハブから生成する。
            var b = Instantiate(ChoicePrefab, parent); b.name = id;
            var r = b.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
            var bg = color ?? Edge; b.GetComponent<Image>().color = bg;
            var t = b.GetComponentInChildren<TextMeshProUGUI>(); t.font = Font; t.text = label;
            t.color = bg == Gold || bg == Mint || bg == Paper || bg == Coral ? Ink : Paper;
            t.fontSize = 19; t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = 19;
            t.lineSpacing = -8; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(16, 6); t.rectTransform.offsetMax = new Vector2(-14, -6);
            b.interactable = enabled; b.onClick.AddListener(() => { PlayTone(540, 0.035f); action(); }); return b;
        }
        private void Clear(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var go = root.GetChild(i).gameObject; go.SetActive(false);
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            }
        }
        private void NewScreen()
        {
            Clear(Surface); modal = null; toast = null; toastGroup = null;
            screen = Box(Surface, "OpsScreen", 0, 0, 1600, 900, Ink);
        }
        private void Bar(Transform parent, string title, int value, float x, float y, float w, Color color)
        {
            Text(parent, title, title + "  " + value + " / 100", x, y, w, 30, 18);
            Box(parent, "Track", x, y + 32, w, 6, Edge);
            if (value > 0) Box(parent, "Fill", x, y + 32, w * value / 100f, 6, color);
        }
        private void Art(Transform parent, float x, float y, float w, float h)
        {
            var r = Rect(parent, "OfficeArt", x, y, w, h); var image = r.gameObject.AddComponent<Image>();
            image.sprite = OfficeArt; image.preserveAspect = true; image.raycastTarget = false;
        }
        private void Portrait(Transform parent, string name, float x, float y, float w, float h)
        {
            var art = Rect(parent, name, x, y, w, h);
            var image = art.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = Navigator != null ? Navigator.FaceNormal : null;
            image.preserveAspect = true; image.raycastTarget = false;
        }
        private RectTransform Dialog(string heading, string body, int height = 480)
        {
            if (modal != null) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); }
            modal = Box(screen, "ModalBlocker", 0, 0, 1600, 900, new Color(0.015f, 0.05f, 0.06f, 0.9f));
            var card = Box(modal, "Dialog", 390, (900 - height) / 2, 820, height, Paper);
            Text(card, "DialogHeading", heading, 32, 28, 740, 64, 31, Ink);
            Text(card, "DialogBody", body, 32, 104, 748, height - 195, 21, Ink);
            Button(card, "CloseDialog", "閉じる", 604, height - 70, 180, 48, CloseDialog, Edge);
            return card;
        }
        private void CloseDialog() { if (modal == null) return; modal.gameObject.SetActive(false); Destroy(modal.gameObject); modal = null; }
        private void Toast(string message, bool good = true)
        {
            if (toast != null) Destroy(toast.gameObject);
            int split = message.IndexOf(" / ", StringComparison.Ordinal);
            string heading = split < 0 ? message : message.Substring(0, split);
            string detail = split < 0 ? "今月の運用に反映しました" : message.Substring(split + 3);
            toast = Box(screen, "Feedback", 312, 466, 628, 112, good ? Mint : Coral, true);
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false; toastGroup.interactable = false;
            var plate = Box(toast, "FeedbackPlate", 6, 6, 616, 100, Ink);
            plate.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(plate, "FeedbackKicker", good ? "OPERATION COMPLETE" : "INCIDENT REPORT", 18, 6, 576, 22, 14, good ? Mint : Coral);
            Text(plate, "FeedbackTitle", heading, 18, 27, 576, 40, 28, Paper);
            Text(plate, "FeedbackDetail", detail, 18, 68, 576, 27, 18, Muted);
            toastUntil = Time.unscaledTime + 2.7f;
            PlayTone(good ? 740 : 210, 0.12f);
        }
        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (modal != null) CloseDialog(); else if (State != null) Menu();
            }
            if (toast != null)
            {
                float t = toastUntil - Time.unscaledTime;
                toast.localScale = Vector3.one * (1f + 0.06f * Mathf.Clamp01((t - 2.45f) / 0.25f));
                if (toastGroup != null) toastGroup.alpha = Mathf.Clamp01(t / 0.35f);
                if (t < 0) { Destroy(toast.gameObject); toast = null; }
            }
        }
        private AudioSource audioSource;
        private AudioClip clickClip;
        private bool muted;
        private void PlayTone(float frequency, float duration)
        {
            if (!Application.isPlaying || muted || TestMode) return;
            if (audioSource == null) { audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; }
            if (clickClip == null)
            {
                const int count = 8000; var samples = new float[count];
                for (int i = 0; i < count; i++) samples[i] = Mathf.Sin(i * 440f * 2 * Mathf.PI / 44100) * Mathf.Exp(-i / 1600f) * 0.12f;
                clickClip = AudioClip.Create("試作の操作音", count, 1, 44100, false); clickClip.SetData(samples, 0);
            }
            audioSource.pitch = frequency / 440; audioSource.PlayOneShot(clickClip, duration < .1f ? .4f : .8f);
        }
        private void OnDestroy() { if (clickClip != null) Destroy(clickClip); }
        private RectTransform Scroll(Transform parent, float x, float y, float w, float h)
        {
            var root = Box(parent, "ProjectScroll", x, y, w, h, Ink);
            var scroll = root.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Rect(root, "Viewport", 0, 0, w, h); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect(viewport, "Content", 0, 0, w - 12, 0);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 10;
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rail = Box(root, "ScrollRail", w - 8, 0, 8, h, Edge);
            var handle = Box(rail, "Handle", 0, 0, 8, 50, Gold);
            var bar = rail.gameObject.AddComponent<Scrollbar>(); bar.handleRect = handle; bar.targetGraphic = handle.GetComponent<Image>(); bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.viewport = viewport; scroll.content = content; scroll.verticalScrollbar = bar;
            scroll.scrollSensitivity = 32; return content;
        }
    }
}
