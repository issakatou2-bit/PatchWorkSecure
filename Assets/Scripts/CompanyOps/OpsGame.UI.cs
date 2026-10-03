using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        // UI-Kitの共通色。成果・未確認・危険の意味は別の色で保つ。
        private static readonly Color Ink = Hex("1d2a44"), Panel = Hex("ffffff"), Edge = Hex("e2e7f0"),
            Paper = Hex("F5F7FA"), Muted = Hex("6b7894"), Accent = Hex("ff6f91"), Mint = Hex("22b08c"), Coral = Hex("c23a60");
        private RectTransform screen, modal, toast;
        private CanvasGroup toastGroup;
        private TextMeshProUGUI toastSpeech;
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
            var r = Rect(parent, name, x, y, w, h); var img = r.gameObject.AddComponent<UnityEngine.UI.Image>();
            if (color == Ink && name != "OpsScreen") color = Hex("f3f6fb");
            img.color = color; img.sprite = null; img.raycastTarget = name == "ModalBlocker";
            // 背景・ゲージは直線、情報のまとまりは角丸。標準の9-sliceを再利用する。
            if (w > 35 && h > 24 && name != "OpsScreen" && name != "ModalBlocker" && name != "CrisisTint" && name != "PhasePresentation")
                RoundSurface(img, name == "Dialog" ? 28 : name == "Navigator" || name == "HomeGreeting" ? 18 : h > 160 ? 14 : 8);
            if (outline || name == "Dialog") KitPanel(r, color, name == "Dialog");
            if (name == "Dialog" || name == "DecisionPanel" || name == "Navigator")
            {
                var shadow = r.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                shadow.effectColor = new Color(0, 0, 0, .22f); shadow.effectDistance = new Vector2(0, -3);
            }
            return r;
        }
        private TextMeshProUGUI Text(Transform parent, string name, string value, float x, float y, float w, float h, float size = 20, Color? color = null)
        {
            var r = Rect(parent, name, x, y, w, h); var label = r.gameObject.AddComponent<TextMeshProUGUI>();
            bool heading = name.EndsWith("Title") || name.EndsWith("Heading") || name.EndsWith("Value") ||
                name == "Title" || name == "Brand" || name == "Month" || name == "CompanyRank" || name.StartsWith("ResponseName_") ||
                name.StartsWith("PowerStepValue") || name.StartsWith("TeamLevel") || name == "PlayerLevel";
            label.font = heading && HeadingFont != null ? HeadingFont : Font;
            label.fontSize = size; label.color = color.HasValue && color.Value == Paper ? Ink : color ?? Ink; label.text = value;
            label.enableAutoSizing = true; label.fontSizeMin = size * .8f; label.fontSizeMax = size;
            label.lineSpacing = 0;
            label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            if(size>=15 && size<=25)label.gameObject.AddComponent<OpsTextPreference>().Initialize(label,TextScale);
            return label;
        }
        private Button Button(Transform parent, string id, string label, float x, float y, float w, float h, Action action, Color? color = null, bool enabled = true)
        {
            // 動的選択肢はルートButton+Image、子TMPのプレハブから生成する。
            var b = Instantiate(ChoicePrefab, parent); b.name = id;
            var r = b.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
            var bg = color ?? Edge; var graphic = b.GetComponent<UnityEngine.UI.Image>();
            graphic.color = bg; RoundSurface(graphic, h >= 80 ? 12 : 8);
            var states = b.colors; states.normalColor = Color.white;
            states.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            states.pressedColor = new Color(.78f, .78f, .78f);
            states.selectedColor = Color.white; b.colors = states;
            var t = b.GetComponentInChildren<TextMeshProUGUI>(); t.font = HeadingFont != null ? HeadingFont : Font; t.text = label;
            t.color = bg == Accent || bg == Mint || bg == Paper || bg == Coral ? Ink : Paper;
            t.fontSize = 19; t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = 19;
            t.lineSpacing = 0; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(16, 6); t.rectTransform.offsetMax = new Vector2(-14, -6);
            b.interactable = enabled;
            b.gameObject.AddComponent<OpsButtonFeedback>().Owner = this;
            KitButton(b, bg);
            if (bg == Edge || bg == Panel || bg == Paper) t.color = Ink;
            if (bg == Accent || bg == PlanPink || bg == Coral || bg == Ink) t.color = Color.white;
            b.onClick.AddListener(() => { StopVoice();if(ConsumePresentationClick())return;PlayCue(OpsCue.Click); action(); TutorialAction(id); }); return b;
        }
        private void RoundSurface(UnityEngine.UI.Image image, float radius)
        {
            image.sprite = PlanningArt==null?PanelSprite:radius>=28?PlanningArt.round28:radius>=24?PlanningArt.round24:radius>=20?PlanningArt.round20:radius>=16?PlanningArt.round16:PlanningArt.round12;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            if (image.sprite != null) image.pixelsPerUnitMultiplier = Mathf.Max(.01f, image.sprite.border.x / radius);
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
            FinishReportCounts();
            recordsActive=false;
            YearOpeningActive=false;
            DiaryActive=false;
            FactorRevealActive=false;
            PhasePresentationRunning=false;
            if(homeVisible||State==null||State.phase!=OpsPhase.Ended){AnnualPresentationCanSkip=false;AnnualPresentationSkipped=false;}
            var departures=BeginPortraitScreen()?PortraitDepartures():new Action[0];
            if (Application.isPlaying) {if(!carryResolutionVoice&&!carryCompanionVoice)StopVoice();StopAllCoroutines();StopPresentationSounds();}
            if(outgoingScreen!=null)Destroy(outgoingScreen.gameObject);
            outgoingScreen=null;
            if(Application.isPlaying&&PortraitEntering&&screen!=null)
            {
                outgoingScreen=screen;outgoingScreen.SetParent(null,false);
                foreach(var child in outgoingScreen.GetComponentsInChildren<Transform>())child.name="Outgoing_"+child.name;
                var group=outgoingScreen.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
            }
            Clear(Surface); modal = null; toast = null; toastGroup = null; toastSpeech = null;
            screen = Box(Surface, "OpsScreen", 0, 0, 1600, 900, Ink);
            if (PlanningArt != null) PImage(screen,"SharedBackground",PlanningArt.gradient,0,0,1600,900);
            if(outgoingScreen!=null)outgoingScreen.SetParent(Surface,false);
            foreach(var depart in departures)depart();
            if (Application.isPlaying&&PortraitEntering&&!buildingYearOpening) StartCoroutine(ScreenWipe(screen));
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
        private void Portrait(Transform parent, string name, float x, float y, float w, float h, string pose="pose_fists")
        {
            var art = Rect(parent, name, x, y, w, h);
            var image = art.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = Navigator != null ? Navigator.Pose(pose) : null;
            image.preserveAspect = true; image.raycastTarget = false;
            art.gameObject.AddComponent<OpsPortraitAnimator>().Owner=this;
            art.gameObject.AddComponent<OpsPortraitIdentity>().PoseId=pose;
            if(Application.isPlaying){var motion=art.gameObject.AddComponent<OpsPortraitMotion>();motion.Owner=this;motion.Enter=PortraitEntering;}
        }
        private RectTransform Dialog(string heading, string body, int height = 480)
        {
            if (modal != null) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); }
            WindowBackdrop();
            var card = Box(modal, "Dialog", 390, (900 - height) / 2, 820, height, Color.white);
            WindowHeader(card,heading,WindowCategory(heading),820); Reveal(card);
            Text(card, "DialogBody", body, 32, 104, 748, height - 195, 21, Ink);
            PButton(card, "CloseDialog", "閉じる", 604, height - 70, 180, 48, CloseDialog, Color.white, Ink,18);
            return card;
        }
        private void CloseDialog()
        {
            if (modal == null) return;
            var old = modal; modal = null;
            if(Application.isPlaying)StartCoroutine(CloseWindow(old));else DestroyImmediate(old.gameObject);
            TutorialWindowClosed();
            // 導入画面から開いた詳細を閉じると、背後の導入一覧と操作を再表示する。
            if (!homeVisible && State != null && State.phase == OpsPhase.Planning && tab != 0) Render();
        }
        private void Toast(string message, bool good = true, OpsCue cue = OpsCue.Action)
        {
            // 対応開始の通知は案件表示・キャラの反応で伝える。比較の数値をポップアップで遮らない。
            if (State != null && State.phase == OpsPhase.Incident) { Feedback(cue); return; }
            if (toast != null) Destroy(toast.gameObject);
            int split = message.IndexOf(" / ", StringComparison.Ordinal);
            string heading = split < 0 ? message : message.Substring(0, split);
            string detail = split < 0 ? "今月の運用に反映しました" : message.Substring(split + 3);
            // 通知はひなたの吹き出し内へ。オフィスの設備・月次事情を覆わない。
            var navigator = screen.Find("Navigator");
            if (navigator == null) { Feedback(cue); return; }
            toastSpeech = navigator.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(t => t.name == "NavigatorSpeech");
            if (toastSpeech != null) toastSpeech.enabled = false;
            if (State != null && State.phase == OpsPhase.Planning)
            {
                toast = Rect(navigator,"Feedback",20,22,380,85);
                toastGroup = toast.gameObject.AddComponent<CanvasGroup>(); toastGroup.blocksRaycasts = false;
                PText(toast,"FeedbackTitle",heading,0,0,380,36,22,good?Hex("1a7c63"):Hex("c23a60"));
                PText(toast,"FeedbackDetail",detail,0,40,380,45,16,PlanInk,false);
                toastUntil = Time.unscaledTime + 2.7f; Feedback(cue); return;
            }
            toast = Box(navigator, "Feedback", 208, 52, 438, 84, Ink);
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false; toastGroup.interactable = false;
            toast.GetComponent<Image>().raycastTarget = false;
            Text(toast, "FeedbackTitle", heading, 12, 7, 414, 32, 22, good ? Mint : Coral);
            Text(toast, "FeedbackDetail", detail, 12, 42, 414, 36, 16, Paper);
            toastUntil = Time.unscaledTime + 2.7f;
            Feedback(cue);
        }
        private void Update()
        {
            TickGamepad();
            TickMusic();
            TickVoice();
            if(YearOpeningActive){TickYearOpening(Time.unscaledDeltaTime);return;}
            if(FactorRevealActive&&!FactorRevealPaused)TickFactorReveal(Time.unscaledDeltaTime);
            AlignDialogFooter(); RefreshTutorial();
            if (!MinigameActive && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                StopVoice();
                if(FactorRevealActive){SkipFactorReveal();return;}
                if (modal != null) CloseDialog(); else Menu();
            }
            if (toast != null)
            {
                float t = toastUntil - Time.unscaledTime;
                toast.localScale = Vector3.one * (ReducedMotion ? 1 : 1f + 0.06f * Mathf.Clamp01((t - 2.45f) / 0.25f));
                if (toastGroup != null) toastGroup.alpha = Mathf.Clamp01(t / 0.35f);
                if (t < 0) { if (toastSpeech != null) toastSpeech.enabled = true; Destroy(toast.gameObject); toast = null; }
            }
        }
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
            var handle = Box(rail, "Handle", 0, 0, 8, 50, Accent);
            var bar = rail.gameObject.AddComponent<Scrollbar>(); bar.handleRect = handle; bar.targetGraphic = handle.GetComponent<Image>(); bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.viewport = viewport; scroll.content = content; scroll.verticalScrollbar = bar;
            scroll.scrollSensitivity = 32; return content;
        }
    }
}
