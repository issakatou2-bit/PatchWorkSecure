using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace PatchWorkSecure.EditorTools
{
    public static partial class SceneBuilder
    {
        private const string OfficeArtPath = "Assets/Art/Office/office-topdown.png";

        private static void PlaceOfficeArt(Transform parent, Vector2 min, Vector2 max)
        {
            AssetDatabase.ImportAsset(OfficeArtPath);
            var importer = AssetImporter.GetAtPath(OfficeArtPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
            var art = new GameObject("OfficeIllustration", typeof(Image));
            art.transform.SetParent(parent, false);
            var img = art.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OfficeArtPath);
            img.preserveAspect = true;
            img.raycastTarget = false;
            StretchTo(art.GetComponent<RectTransform>(), min, max, Vector2.zero, Vector2.zero);
        }

        private static void BuildOfficeTitle(Transform parent, SerializedObject so)
        {
            var root = CreateFullScreenPanel("TitlePanel", parent, BgDeep, AccentDay);
            PlaceOfficeArt(root, new Vector2(0.40f, 0.10f), new Vector2(0.96f, 0.94f));
            var intro = CreatePanelBase("WelcomeCard", root, CardBg);
            ApplyRounded(intro.gameObject, PanelSprite);
            AddShadow(intro.gameObject, 8, 0.35f);
            StretchTo(intro, new Vector2(0.045f, 0.16f), new Vector2(0.37f, 0.85f), Vector2.zero, Vector2.zero);
            var layout = intro.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 34, 34);
            layout.spacing = 10;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            TitleLine(intro, "EditionLabel", "オフィス防衛シミュレーション", 18, 30, TextSub);
            TitleLine(intro, "GameTitle", "PatchWork\nSecure", 60, 140, TextMain);
            TitleLine(intro, "Tagline", "平穏を、つなごう。", 34, 60, TextMain);
            TitleLine(intro, "Concept", "困った人に手を差し伸べる。\n会社を守る備えを選ぶ。\nそんなあなたの、情シスの一年。", 21, 100, TextSub);
            var quick = CreateButton(intro, "QuickStartButton", "オフィスへ出勤する", AccentDay);
            quick.gameObject.AddComponent<LayoutElement>().preferredHeight = 72;
            var study = CreateButton(intro, "StartButton", "学習クイズ付きで始める", AccentChore);
            study.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            study.GetComponentInChildren<TextMeshProUGUI>().fontSize = 19;
            var personas = new GameObject("PersonaSelectContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            personas.transform.SetParent(intro, false);
            personas.AddComponent<LayoutElement>().preferredHeight = 54;
            var row = personas.GetComponent<HorizontalLayoutGroup>();
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            var footer = CreateLabel(root, "TitleFooter", "人のためのセキュリティ。ミスを責めず、致命傷にならない備えを。", 19, 0);
            StretchTo(footer.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.10f), Vector2.zero, Vector2.zero);
            SetRef(so, "titlePanel", root.gameObject);
            SetRef(so, "startButton", study);
            SetRef(so, "quickStartButton", quick);
            SetRef(so, "personaSelectContainer", personas.transform);
        }

        private static void TitleLine(Transform parent, string name, string text, int size, float height, Color color)
        {
            var label = CreateLabel(parent, name, text, size, 0, true);
            label.color = color;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private static void BuildOfficeStage(RectTransform parent, SerializedObject so)
        {
            var stage = CreateStretched("OfficeStage", parent);
            stage.SetSiblingIndex(1);
            StretchTo(stage, Vector2.zero, Vector2.one,
                new Vector2(SidebarWidth + Margin * 2, CharacterStripHeight + Margin * 2),
                new Vector2(-620, -(TopBarHeight + Margin * 2)));
            var mapViewport = CreatePanelBase("MapViewport", stage, CardBg);
            StretchTo(mapViewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            mapViewport.gameObject.AddComponent<RectMask2D>();
            PlaceOfficeArt(mapViewport, Vector2.zero, Vector2.one);
            var art = mapViewport.Find("OfficeIllustration");
            var aspect = art.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = 1f;
            AddShadow(mapViewport.gameObject, 4, 0.7f);
            var badge = CreatePanelBase("OfficeStatus", stage, CardBg);
            ApplyRounded(badge.gameObject, PanelSprite);
            StretchTo(badge, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -66), new Vector2(470, -8));
            var light = CreatePanelBase("StatusLight", badge, AccentDay);
            ApplyRounded(light.gameObject, CircleSprite);
            StretchTo(light, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, -7), new Vector2(34, 7));
            var status = CreateLabel(badge, "OfficeLabel", "いつものオフィス\n<size=16>この日常を、年度末まで守り抜こう</size>", 22, 0, true);
            StretchTo(status.rectTransform, Vector2.zero, Vector2.one, new Vector2(50, 8), new Vector2(-12, -8));
            var strip = parent.Find("CharacterStrip").GetComponent<RectTransform>();
            var recordCard = CreatePanelBase("RecordCard", strip, CardBg);
            ApplyRounded(recordCard.gameObject, PanelSprite);
            AddShadow(recordCard.gameObject);
            StretchTo(recordCard, Vector2.zero, new Vector2(0.40f, 1), new Vector2(0, 8), new Vector2(-16, -12));
            var record = CreateLabel(recordCard, "OfficeRecord", "", 21, 0, true);
            StretchTo(record.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -20));
            var frame = strip.Find("PortraitFrame").GetComponent<RectTransform>();
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(1, 0.5f);
            frame.anchoredPosition = new Vector2(0, 16);
            frame.sizeDelta = new Vector2(280, 280);
            frame.GetComponent<Image>().enabled = false;
            var bubble = strip.Find("SpeechBubble").GetComponent<RectTransform>();
            StretchTo(bubble, new Vector2(0.41f, 0), Vector2.one, new Vector2(0, 30), new Vector2(-300, -12));
            // 角を回転させた口で、カードではなくひなたの吹き出しとして見せる。
            var tail = CreatePanelBase("SpeechTail", bubble, bubble.GetComponent<Image>().color);
            ApplyRounded(tail.gameObject, PanelSprite);
            tail.anchorMin = tail.anchorMax = new Vector2(1, 0.55f);
            tail.sizeDelta = new Vector2(26, 26);
            tail.anchoredPosition = new Vector2(-6, 0);
            tail.localRotation = Quaternion.Euler(0, 0, 45);
            tail.GetComponent<Image>().raycastTarget = false;
            SetRef(so, "officeRecord", record);
            SetRef(so, "officeStatusLight", light.GetComponent<Image>());

            MovePhaseToRight(parent, "DayPanel", 580);
            MovePhaseToRight(parent, "ChorePanel", 580);
            MovePhaseToRight(parent, "AttackPanel", 580);
            MovePhaseToRight(parent, "ParryPanel", 580);
            MovePhaseToRight(parent, "ResultPanel", 580);
            var heading = parent.Find("DayPanel/DayHeading").GetComponent<TextMeshProUGUI>();
            heading.text = "今日の備えを決めよう";
            heading.fontSize = 28;
            var guide = parent.Find("DayPanel/DayGuide").GetComponent<TextMeshProUGUI>();
            guide.alignment = TextAlignmentOptions.TopLeft;
            var proceed = parent.Find("DayPanel/ProceedButton/Label").GetComponent<TextMeshProUGUI>();
            proceed.text = "社員からの相談を開く";

            var brand = CreateLabel(parent.Find("StatusBarPanel"), "Brand", "PatchWork Secure\n<size=16>情シス室 / オフィスの一年</size>", 24, 0, true);
            brand.gameObject.AddComponent<LayoutElement>().preferredWidth = 350;
            brand.transform.SetAsFirstSibling();
            var parryHint = parent.Find("ParryPanel/ParryHint").GetComponent<TextMeshProUGUI>();
            parryHint.fontSize = 18;
            parryHint.GetComponent<LayoutElement>().preferredHeight = 70;
            parryHint.GetComponent<LayoutElement>().preferredWidth = 520;
            parent.Find("ParryPanel/ParryTrack").GetComponent<LayoutElement>().preferredWidth = 500;
            var feedback = parent.Find("ParryPanel/ParryFeedbackText").GetComponent<TextMeshProUGUI>();
            feedback.fontSize = 23;
            feedback.GetComponent<LayoutElement>().preferredWidth = 510;
            var outline = parent.Find("AttackPanel").GetComponent<Outline>();
            if (outline != null) outline.effectColor = new Color(0.65f, 0.24f, 0.18f, 1f);
        }

        private static void MovePhaseToRight(Transform parent, string name, float width)
        {
            var rect = parent.Find(name).GetComponent<RectTransform>();
            StretchTo(rect, new Vector2(1, 0), Vector2.one,
                new Vector2(-Margin - width, Margin * 2 + CharacterStripHeight),
                new Vector2(-Margin, -(TopBarHeight + Margin * 2)));
        }
    }
}
