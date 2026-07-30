using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PatchWorkSecure.EditorTools
{
    /// <summary>
    /// 仮組みUIを1メニュー操作で自動生成するエディタ拡張。
    /// 見た目(色・フォント・素材)は後で差し替える前提だが、レイアウト・配色・ボタンの反応など
    /// 「ちゃんとデザインされた感」は素材無しでも作れる範囲でここに寄せている。
    /// 角丸・影はUnity標準の"UI/Skin/*.psd"組み込みスプライトとUI.Shadowコンポーネントのみで実現し、
    /// 独自テクスチャ生成は行わない（見た目の検証がエディタ上でしかできないため、実績のある標準機能に寄せている）。
    ///
    /// 使い方: Unity上部メニュー「PatchWorkSecure」→「シーンを自動構築」
    /// 実行前に一度Canvas/GameManagerを削除しておくと、重複生成を避けられます。
    /// </summary>
    public static class SceneBuilder
    {
        private const string PrefabDir = "Assets/Prefabs";

        // ---- 配色パレット(画面カテゴリごとにフェーズタグの色で見分けをつける) ----
        private static readonly Color AccentDay = new Color(0.35f, 0.65f, 0.55f);
        private static readonly Color AccentChore = new Color(0.40f, 0.55f, 0.75f);
        private static readonly Color AccentAttack = new Color(0.80f, 0.40f, 0.35f);
        private static readonly Color AccentParry = new Color(0.85f, 0.65f, 0.25f);
        private static readonly Color AccentResult = new Color(0.55f, 0.50f, 0.65f);
        private static readonly Color AccentTitle = new Color(0.55f, 0.45f, 0.80f);
        private static readonly Color AccentQuiz = new Color(0.40f, 0.70f, 0.50f);
        private static readonly Color AccentEnding = new Color(0.60f, 0.35f, 0.35f);
        private static readonly Color AccentSummary = new Color(0.50f, 0.45f, 0.75f);

        // ---- Unity組み込みの角丸スプライト(誰の環境にも必ず存在する標準アセット) ----
        private static Sprite _panelSprite;
        private static Sprite _buttonSprite;
        private static Sprite PanelSprite => _panelSprite != null ? _panelSprite
            : (_panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"));
        private static Sprite ButtonSprite => _buttonSprite != null ? _buttonSprite
            : (_buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));

        [MenuItem("PatchWorkSecure/シーンを自動構築")]
        public static void BuildScene()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
                esGO.AddComponent<InputSystemUIInputModule>();
#else
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var gmGO = new GameObject("GameManager", typeof(GameManager));
            var gameManager = gmGO.GetComponent<GameManager>();
            var so = new SerializedObject(gameManager);
            SetRef(so, "shakeRoot", canvasGO.GetComponent<RectTransform>());

            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

            BuildBackdrop(canvasGO.transform);
            BuildAudioManager();

            BuildStatusBar(canvasGO.transform, so);
            BuildNavigator(canvasGO.transform, so);
            BuildDayPanel(canvasGO.transform, so);
            BuildChorePanel(canvasGO.transform, so);
            BuildAttackPanel(canvasGO.transform, so);
            BuildParryPanel(canvasGO.transform, so);
            BuildResultPanel(canvasGO.transform, so);
            BuildLogPanel(canvasGO.transform, so);

            BuildTitlePanel(canvasGO.transform, so);
            BuildQuizPanel(canvasGO.transform, so);
            BuildEndingPanel(canvasGO.transform, so);
            BuildSummaryPanel(canvasGO.transform, so);
            BuildSettingsOverlay(canvasGO.transform, so); // 常時最前面に来るよう最後に生成する

            var choicePrefab = BuildButtonPrefab("ChoiceButtonPrefab");
            var defensePrefab = BuildButtonPrefab("DefenseButtonPrefab");
            var quizOptionPrefab = BuildButtonPrefab("QuizOptionButtonPrefab");
            SetRef(so, "choiceButtonPrefab", choicePrefab);
            SetRef(so, "defenseButtonPrefab", defensePrefab);
            SetRef(so, "quizOptionButtonPrefab", quizOptionPrefab);

            so.ApplyModifiedProperties();

            Selection.activeGameObject = gmGO;
            EditorUtility.DisplayDialog(
                "構築完了",
                "シーンを生成しました。\n\nPlayして、タイトル画面→事前クイズ→本編→エンディング→事後クイズ→結果サマリー、の一連の流れを確認してください。\nConsoleにエラーが出たら、そのまま貼ってください。",
                "OK");
        }

        // ================= 背景 =================

        /// <summary>画面全体を覆う単色の背景。何も無い部分にUnityのデフォルト背景が透けて見えるのを防ぐ。</summary>
        private static void BuildBackdrop(Transform parent)
        {
            var go = new GameObject("Backdrop", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.045f, 0.045f, 0.06f, 1f);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ================= 常時表示パネル =================

        private static void BuildStatusBar(Transform parent, SerializedObject so)
        {
            var rt = CreatePanelBase("StatusBarPanel", parent, new Color(0.12f, 0.12f, 0.15f, 0.96f));
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(20, -110);
            rt.offsetMax = new Vector2(-20, -20);
            ApplyRounded(rt.gameObject, PanelSprite);
            AddShadow(rt.gameObject, 5f, 0.4f);

            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 10, 10);
            layout.spacing = 32;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var periodLabel = CreateLabel(rt, "PeriodLabel", "📅 4月上旬（1/36）", 26, 250, bold: true);
            var budgetText = CreateLabel(rt, "BudgetText", "💰 予算 100", 22, 150);
            var budgetBar = CreateBar(rt, "BudgetBar", new Color(0.35f, 0.75f, 0.35f));
            var trustText = CreateLabel(rt, "TrustText", "🤝 人望 30", 22, 150);
            var trustBar = CreateBar(rt, "TrustBar", new Color(0.35f, 0.55f, 0.85f));
            var stressText = CreateLabel(rt, "StressText", "😣 ストレス 20", 22, 160);
            var stressBar = CreateBar(rt, "StressBar", new Color(0.85f, 0.45f, 0.35f));

            SetRef(so, "periodLabel", periodLabel);
            SetRef(so, "budgetText", budgetText);
            SetRef(so, "trustText", trustText);
            SetRef(so, "stressText", stressText);
            SetRef(so, "budgetBar", budgetBar);
            SetRef(so, "trustBar", trustBar);
            SetRef(so, "stressBar", stressBar);
        }

        private static void BuildNavigator(Transform parent, SerializedObject so)
        {
            var rt = CreatePanelBase("NavigatorPanel", parent, new Color(0, 0, 0, 0));
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(320, -110);
            rt.anchoredPosition = new Vector2(0, -55);

            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            // ポートレート用の枠(角丸+影)と、実際の顔スプライトを表示するImageを分離しておく。
            // GameManagerはportraitImgの.spriteだけを書き換えるので、枠側は独立させないと上書きで消えてしまう。
            var slotGO = new GameObject("PortraitSlot", typeof(RectTransform));
            slotGO.transform.SetParent(rt, false);
            var slotLE = slotGO.AddComponent<LayoutElement>();
            slotLE.preferredWidth = 240;
            slotLE.preferredHeight = 240;

            var frameGO = new GameObject("PortraitFrame", typeof(Image));
            frameGO.transform.SetParent(slotGO.transform, false);
            var frameImg = frameGO.GetComponent<Image>();
            frameImg.color = new Color(0.16f, 0.16f, 0.20f, 1f);
            ApplyRounded(frameGO, PanelSprite);
            AddShadow(frameGO, 5f, 0.4f);
            var frameRT = frameGO.GetComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero;
            frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = new Vector2(-8, -8);
            frameRT.offsetMax = new Vector2(8, 8);

            var portraitGO = new GameObject("NavigatorPortrait", typeof(Image));
            portraitGO.transform.SetParent(slotGO.transform, false);
            var portraitImg = portraitGO.GetComponent<Image>();
            portraitImg.color = new Color(0.85f, 0.85f, 0.9f);
            var portraitRT = portraitGO.GetComponent<RectTransform>();
            portraitRT.anchorMin = Vector2.zero;
            portraitRT.anchorMax = Vector2.one;
            portraitRT.offsetMin = Vector2.zero;
            portraitRT.offsetMax = Vector2.zero;

            var line = CreateLabel(rt, "NavigatorLine", "今日も平穏です。備えを進めましょうか。", 20, 0);
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;

            SetRef(so, "navigatorPortrait", portraitImg);
            SetRef(so, "navigatorLine", line);
        }

        // ================= メインフェーズパネル =================

        private static RectTransform CreateMainAreaPanel(
            string name, Transform parent, Color bg, Color accent, string phaseTitle)
        {
            var rt = CreatePanelBase(name, parent, bg);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(340, 20);
            rt.offsetMax = new Vector2(-20, -110);
            ApplyRounded(rt.gameObject, PanelSprite);
            AddShadow(rt.gameObject, 6f, 0.4f);
            AddPhaseTag(rt, phaseTitle, accent);
            rt.gameObject.SetActive(false);
            return rt;
        }

        private static void BuildDayPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateMainAreaPanel(
                "DayPanel", parent, new Color(0.11f, 0.13f, 0.12f, 0.95f), AccentDay, "📅 本日の業務");
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 70, 30);
            layout.spacing = 20;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            var proceedBtn = CreateButton(rt, "ProceedButton", "今日の業務を進める");
            proceedBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;

            var defenseTitle = CreateLabel(rt, "DefenseTitle", "🛡️ セキュリティ対策", 20, 0, bold: true);
            defenseTitle.color = new Color(0.85f, 0.9f, 0.88f);

            var containerGO = new GameObject("DefenseButtonContainer", typeof(RectTransform));
            containerGO.transform.SetParent(rt, false);
            var containerRT = containerGO.GetComponent<RectTransform>();
            var vlayout = containerGO.AddComponent<VerticalLayoutGroup>();
            vlayout.spacing = 8;
            vlayout.childControlHeight = false;
            vlayout.childControlWidth = true;
            containerGO.AddComponent<LayoutElement>().flexibleHeight = 1;

            SetRef(so, "dayPanel", rt.gameObject);
            SetRef(so, "proceedButton", proceedBtn);
            SetRef(so, "defenseButtonContainer", containerRT);
        }

        private static void BuildChorePanel(Transform parent, SerializedObject so)
        {
            var rt = CreateMainAreaPanel(
                "ChorePanel", parent, new Color(0.12f, 0.13f, 0.17f, 0.95f), AccentChore, "📨 雑務対応");
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 70, 30);
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            var choreText = CreateLabel(rt, "ChoreText", "（雑務の内容がここに入る）", 26, 0);
            choreText.alignment = TextAlignmentOptions.Center;
            var solveBtn = CreateButton(rt, "SolveButton", "誠実に対応する");
            var postponeBtn = CreateButton(rt, "PostponeButton", "後回しにする");
            solveBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;
            postponeBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;

            SetRef(so, "chorePanel", rt.gameObject);
            SetRef(so, "choreText", choreText);
            SetRef(so, "solveChoreButton", solveBtn);
            SetRef(so, "postponeChoreButton", postponeBtn);
        }

        private static void BuildAttackPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateMainAreaPanel(
                "AttackPanel", parent, new Color(0.17f, 0.10f, 0.10f, 0.95f), AccentAttack, "🚨 インシデント対応");
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 70, 30);
            layout.spacing = 16;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            var nameText = CreateLabel(rt, "AttackNameText", "（攻撃名）", 32, 0, bold: true);
            var gradeText = CreateLabel(rt, "AttackGradeText", "（グレード）", 20, 0);
            gradeText.color = new Color(0.9f, 0.7f, 0.65f);
            var introLine = CreateLabel(rt, "AttackIntroLine", "「（台詞）」", 22, 0);
            var scTerm = CreateLabel(rt, "ScTermText", "📘（SC用語）", 18, 0);
            scTerm.color = new Color(0.75f, 0.8f, 0.85f);

            var containerGO = new GameObject("ChoiceButtonContainer", typeof(RectTransform));
            containerGO.transform.SetParent(rt, false);
            var containerRT = containerGO.GetComponent<RectTransform>();
            var vlayout = containerGO.AddComponent<VerticalLayoutGroup>();
            vlayout.spacing = 10;
            vlayout.childControlHeight = false;
            vlayout.childControlWidth = true;
            containerGO.AddComponent<LayoutElement>().flexibleHeight = 1;

            SetRef(so, "attackPanel", rt.gameObject);
            SetRef(so, "attackNameText", nameText);
            SetRef(so, "attackGradeText", gradeText);
            SetRef(so, "attackIntroLine", introLine);
            SetRef(so, "scTermText", scTerm);
            SetRef(so, "choiceButtonContainer", containerRT);
        }

        private static void BuildParryPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateMainAreaPanel(
                "ParryPanel", parent, new Color(0.16f, 0.14f, 0.08f, 0.95f), AccentParry, "⚡ 意思決定");
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 70, 30);
            layout.spacing = 30;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            var hint = CreateLabel(rt, "ParryHint", "タイミングよく「ここだ！」を押そう", 20, 0);
            hint.alignment = TextAlignmentOptions.Center;
            hint.color = new Color(0.85f, 0.8f, 0.7f);
            hint.gameObject.AddComponent<LayoutElement>().preferredWidth = 700;

            var trackGO = new GameObject("ParryTrack", typeof(Image));
            trackGO.transform.SetParent(rt, false);
            var trackImg = trackGO.GetComponent<Image>();
            trackImg.color = new Color(0.22f, 0.22f, 0.24f);
            ApplyRounded(trackGO, PanelSprite);
            var trackRT = trackGO.GetComponent<RectTransform>();
            trackRT.sizeDelta = new Vector2(900, 50);
            trackGO.AddComponent<LayoutElement>().preferredWidth = 900;

            // スイートゾーン（GameManagerの判定しきい値と対応する帯を可視化する）
            float goodHalf = (1f - GameManager.ParryGoodThreshold) * 0.5f;
            float perfectHalf = (1f - GameManager.ParryPerfectThreshold) * 0.5f;
            AddParryZone(trackRT, "GoodZone", 0.5f - goodHalf, 0.5f + goodHalf, new Color(0.55f, 0.75f, 0.4f, 0.55f));
            AddParryZone(trackRT, "PerfectZone", 0.5f - perfectHalf, 0.5f + perfectHalf, new Color(0.95f, 0.8f, 0.25f, 0.8f));

            var markerGO = new GameObject("ParryMarker", typeof(Image));
            markerGO.transform.SetParent(trackRT, false);
            var markerImg = markerGO.GetComponent<Image>();
            markerImg.color = new Color(0.95f, 0.85f, 0.25f);
            ApplyRounded(markerGO, ButtonSprite);
            AddShadow(markerGO, 2f, 0.5f);
            var markerRT = markerGO.GetComponent<RectTransform>();
            markerRT.anchorMin = new Vector2(0.5f, 0.5f);
            markerRT.anchorMax = new Vector2(0.5f, 0.5f);
            markerRT.pivot = new Vector2(0.5f, 0.5f);
            markerRT.sizeDelta = new Vector2(20, 50);
            markerRT.anchoredPosition = Vector2.zero;

            var feedbackText = CreateLabel(rt, "ParryFeedbackText", "", 32, 0, bold: true);
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;

            var parryBtn = CreateButton(rt, "ParryButton", "ここだ！");
            var parryLE = parryBtn.gameObject.AddComponent<LayoutElement>();
            parryLE.preferredWidth = 300;
            parryLE.preferredHeight = 80;

            SetRef(so, "parryPanel", rt.gameObject);
            SetRef(so, "parryTrack", trackRT);
            SetRef(so, "parryMarker", markerRT);
            SetRef(so, "parryButton", parryBtn);
            SetRef(so, "parryFeedbackText", feedbackText);
        }

        /// <summary>パリィトラック上に判定帯を描く。マーカーより先に生成し、常にマーカーが手前に来るようにする。</summary>
        private static void AddParryZone(RectTransform trackRT, string name, float xMin, float xMax, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(trackRT, false);
            go.GetComponent<Image>().color = color;
            var zoneRT = go.GetComponent<RectTransform>();
            zoneRT.anchorMin = new Vector2(xMin, 0f);
            zoneRT.anchorMax = new Vector2(xMax, 1f);
            zoneRT.offsetMin = Vector2.zero;
            zoneRT.offsetMax = Vector2.zero;
        }

        private static void BuildResultPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateMainAreaPanel(
                "ResultPanel", parent, new Color(0.12f, 0.12f, 0.15f, 0.95f), AccentResult, "📋 対応結果");
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 70, 30);
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            var resultText = CreateLabel(rt, "ResultText", "（結果メッセージ）", 30, 0, bold: true);
            resultText.alignment = TextAlignmentOptions.Center;
            var resultLine = CreateLabel(rt, "ResultCharacterLine", "「（キャラ台詞）」", 22, 0);
            resultLine.alignment = TextAlignmentOptions.Center;
            var nextBtn = CreateButton(rt, "NextDayButton", "次の日へ");
            nextBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;

            SetRef(so, "resultPanel", rt.gameObject);
            SetRef(so, "resultText", resultText);
            SetRef(so, "resultCharacterLine", resultLine);
            SetRef(so, "nextDayButton", nextBtn);
        }

        private static void BuildLogPanel(Transform parent, SerializedObject so)
        {
            var rt = CreatePanelBase("LogPanel", parent, new Color(0.05f, 0.05f, 0.08f, 0.88f));
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.sizeDelta = new Vector2(420, 220);
            rt.anchoredPosition = new Vector2(-20, 20);
            ApplyRounded(rt.gameObject, PanelSprite);
            AddShadow(rt.gameObject, 4f, 0.35f);

            var logText = CreateLabel(rt, "LogText", "", 16, 0);
            var logRT = logText.GetComponent<RectTransform>();
            logRT.anchorMin = Vector2.zero;
            logRT.anchorMax = Vector2.one;
            logRT.offsetMin = new Vector2(16, 16);
            logRT.offsetMax = new Vector2(-16, -16);
            logText.alignment = TextAlignmentOptions.TopLeft;

            SetRef(so, "logText", logText);
        }

        // ================= フルスクリーン画面 =================

        private static RectTransform CreateFullScreenPanel(string name, Transform parent, Color bg, Color accent)
        {
            var rt = CreatePanelBase(name, parent, bg);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            AddAccentStrip(rt, accent);
            rt.gameObject.SetActive(false);
            return rt;
        }

        /// <summary>フルスクリーン画面の中央に浮かべる、角丸+影付きのダイアログ/コンテンツカード。</summary>
        private static RectTransform CreateCenterCard(Transform parent, string name, Vector2 size, Color bg)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            ApplyRounded(go, PanelSprite);
            AddShadow(go, 8f, 0.5f);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        private static void BuildTitlePanel(Transform parent, SerializedObject so)
        {
            var rt = CreateFullScreenPanel("TitlePanel", parent, new Color(0.07f, 0.07f, 0.10f, 0.98f), AccentTitle);
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 26;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            var title = CreateLabel(rt, "GameTitle", "PatchWorkSecure", 64, 0, bold: true);
            title.alignment = TextAlignmentOptions.Center;
            title.gameObject.AddComponent<LayoutElement>().preferredWidth = 900;

            var tagline = CreateLabel(
                rt, "Tagline",
                "なにごともない、いつもの平穏なオフィスの日常を\nツギハギ（PatchWork）しながら守り抜け",
                20, 0);
            tagline.alignment = TextAlignmentOptions.Center;
            tagline.color = new Color(0.75f, 0.75f, 0.82f);
            var taglineLE = tagline.gameObject.AddComponent<LayoutElement>();
            taglineLE.preferredWidth = 800;
            taglineLE.preferredHeight = 70;

            var startBtn = CreateButton(rt, "StartButton", "はじめる");
            var startLE = startBtn.gameObject.AddComponent<LayoutElement>();
            startLE.preferredWidth = 260;
            startLE.preferredHeight = 72;

            SetRef(so, "titlePanel", rt.gameObject);
            SetRef(so, "startButton", startBtn);
        }

        private static void BuildQuizPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateFullScreenPanel("QuizPanel", parent, new Color(0.08f, 0.11f, 0.09f, 0.98f), AccentQuiz);

            var card = CreateCenterCard(rt, "QuizCard", new Vector2(900, 560), new Color(0.10f, 0.14f, 0.11f, 0.98f));
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(60, 60, 50, 50);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 26;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            var progress = CreateLabel(card, "QuizProgressText", "事前クイズ　1/3", 18, 0);
            progress.alignment = TextAlignmentOptions.Center;
            progress.color = new Color(0.6f, 0.78f, 0.66f);

            var question = CreateLabel(card, "QuizQuestionText", "（設問がここに入る）", 26, 0, bold: true);
            question.alignment = TextAlignmentOptions.Center;
            question.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;

            var containerGO = new GameObject("QuizOptionContainer", typeof(RectTransform));
            containerGO.transform.SetParent(card, false);
            var containerRT = containerGO.GetComponent<RectTransform>();
            var vlayout = containerGO.AddComponent<VerticalLayoutGroup>();
            vlayout.spacing = 14;
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childControlHeight = false;
            vlayout.childControlWidth = true;

            SetRef(so, "quizPanel", rt.gameObject);
            SetRef(so, "quizProgressText", progress);
            SetRef(so, "quizQuestionText", question);
            SetRef(so, "quizOptionContainer", containerRT);
        }

        private static void BuildEndingPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateFullScreenPanel("EndingPanel", parent, new Color(0.10f, 0.07f, 0.08f, 0.98f), AccentEnding);
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            var text = CreateLabel(rt, "EndingText", "（結果メッセージ）", 34, 0, bold: true);
            text.alignment = TextAlignmentOptions.Center;
            text.gameObject.AddComponent<LayoutElement>().preferredWidth = 900;

            var line = CreateLabel(rt, "EndingCharacterLine", "「（台詞）」", 22, 0);
            line.alignment = TextAlignmentOptions.Center;
            line.color = new Color(0.8f, 0.8f, 0.85f);
            line.gameObject.AddComponent<LayoutElement>().preferredWidth = 800;

            var btn = CreateButton(rt, "EndingContinueButton", "結果を振り返る");
            var le = btn.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 280;
            le.preferredHeight = 70;

            SetRef(so, "endingPanel", rt.gameObject);
            SetRef(so, "endingText", text);
            SetRef(so, "endingCharacterLine", line);
            SetRef(so, "endingContinueButton", btn);
        }

        private static void BuildSummaryPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateFullScreenPanel("SummaryPanel", parent, new Color(0.09f, 0.08f, 0.12f, 0.98f), AccentSummary);

            var card = CreateCenterCard(rt, "SummaryCard", new Vector2(720, 460), new Color(0.12f, 0.11f, 0.16f, 0.98f));
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(50, 50, 50, 50);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 30;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            var text = CreateLabel(card, "SummaryText", "（結果サマリーがここに入る）", 22, 0);
            text.alignment = TextAlignmentOptions.Center;
            var textLE = text.gameObject.AddComponent<LayoutElement>();
            textLE.preferredWidth = 600;
            textLE.preferredHeight = 220;

            var btn = CreateButton(card, "SummaryCloseButton", "タイトルへ戻る");
            var btnLE = btn.gameObject.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 260;
            btnLE.preferredHeight = 70;

            SetRef(so, "summaryPanel", rt.gameObject);
            SetRef(so, "summaryText", text);
            SetRef(so, "summaryCloseButton", btn);
        }

        // ================= オーディオ =================

        /// <summary>BGM用/SE用のAudioSourceを1体のGameObjectにまとめて持たせる。</summary>
        private static void BuildAudioManager()
        {
            var go = new GameObject("AudioManager", typeof(AudioManager));

            var bgmSource = go.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;

            var seSource = go.AddComponent<AudioSource>();
            seSource.loop = false;
            seSource.playOnAwake = false;

            var soAudio = new SerializedObject(go.GetComponent<AudioManager>());
            SetRef(soAudio, "bgmSource", bgmSource);
            SetRef(soAudio, "seSource", seSource);
            soAudio.ApplyModifiedProperties();
        }

        // ================= 設定オーバーレイ =================

        /// <summary>右上に常時表示する歯車ボタンと、ミュート切替・タイトルへ戻る導線を持つ設定ダイアログ。</summary>
        private static void BuildSettingsOverlay(Transform parent, SerializedObject so)
        {
            var btnGO = new GameObject("SettingsOpenButton", typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var btnBg = btnGO.GetComponent<Image>();
            btnBg.color = new Color(0.2f, 0.2f, 0.24f, 0.9f);
            ApplyRounded(btnGO, ButtonSprite);
            AddShadow(btnGO, 3f, 0.35f);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(1, 1);
            btnRT.anchorMax = new Vector2(1, 1);
            btnRT.pivot = new Vector2(1, 1);
            btnRT.sizeDelta = new Vector2(56, 56);
            btnRT.anchoredPosition = new Vector2(-16, -16);

            var icon = CreateLabel(btnGO.transform, "Icon", "⚙", 30, 0);
            icon.alignment = TextAlignmentOptions.Center;
            var iconRT = icon.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;

            var openBtn = btnGO.GetComponent<Button>();
            openBtn.targetGraphic = btnBg;
            ApplyButtonColors(openBtn);
            btnGO.AddComponent<UIButtonPunch>();

            // 背景は画面全体を薄暗く覆うだけ(角丸なし)。中身は中央のダイアログカードにまとめる。
            var overlayRT = CreateFullScreenPanelNoAccent(
                "SettingsPanel", parent, new Color(0.03f, 0.03f, 0.05f, 0.75f));

            var card = CreateCenterCard(overlayRT, "SettingsCard", new Vector2(480, 460), new Color(0.13f, 0.13f, 0.17f, 0.98f));
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 40, 30);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 24;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            var title = CreateLabel(card, "SettingsTitle", "⚙ 設定", 36, 0, bold: true);
            title.alignment = TextAlignmentOptions.Center;

            var muteBtn = CreateButton(card, "MuteButton", "🔊 音声：オン");
            var muteLE = muteBtn.gameObject.AddComponent<LayoutElement>();
            muteLE.preferredWidth = 320;
            muteLE.preferredHeight = 64;
            var muteLabel = muteBtn.GetComponentInChildren<TextMeshProUGUI>();

            var backToTitleBtn = CreateButton(card, "BackToTitleButton", "タイトルへ戻る");
            var backLE = backToTitleBtn.gameObject.AddComponent<LayoutElement>();
            backLE.preferredWidth = 320;
            backLE.preferredHeight = 64;

            var closeBtn = CreateButton(card, "CloseButton", "閉じる");
            var closeLE = closeBtn.gameObject.AddComponent<LayoutElement>();
            closeLE.preferredWidth = 320;
            closeLE.preferredHeight = 64;

            SetRef(so, "settingsPanel", overlayRT.gameObject);
            SetRef(so, "settingsOpenButton", openBtn);
            SetRef(so, "settingsCloseButton", closeBtn);
            SetRef(so, "settingsMuteButton", muteBtn);
            SetRef(so, "settingsMuteButtonLabel", muteLabel);
            SetRef(so, "settingsBackToTitleButton", backToTitleBtn);
            // BuildSettingsOverlayはBuildScene()内で最後に呼ばれるため、
            // btnGO/overlayRTは他の全画面パネルより自然に手前へ来る（overlayRTが最後尾＝最前面）。
        }

        /// <summary>CreateFullScreenPanelからフェーズ帯(AddAccentStrip)を省いた版。設定ダイアログの薄暗い背景用。</summary>
        private static RectTransform CreateFullScreenPanelNoAccent(string name, Transform parent, Color bg)
        {
            var rt = CreatePanelBase(name, parent, bg);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.gameObject.SetActive(false);
            return rt;
        }

        // ================= プレハブ =================

        private static GameObject BuildButtonPrefab(string name)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            var bgImage = go.GetComponent<Image>();
            bgImage.color = new Color(0.22f, 0.24f, 0.30f);
            ApplyRounded(go, ButtonSprite);
            AddShadow(go, 3f, 0.3f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 64);

            var label = CreateLabel(rt, "Label", "対策名 Lv.0", 20, 0);
            var labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(20, 6);
            labelRT.offsetMax = new Vector2(-20, -6);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            var button = go.GetComponent<Button>();
            button.targetGraphic = bgImage;
            ApplyButtonColors(button);
            go.AddComponent<UIButtonPunch>();

            string path = $"{PrefabDir}/{name}.prefab";
            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefabAsset;
        }

        // ================= 共通ヘルパー =================

        private static RectTransform CreatePanelBase(string name, Transform parent, Color bg)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            return go.GetComponent<RectTransform>();
        }

        /// <summary>ImageのスプライトをUnity組み込みの角丸アセットに差し替え、9-Sliceで表示する。</summary>
        private static void ApplyRounded(GameObject go, Sprite sprite)
        {
            var img = go.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }

        /// <summary>UI標準のShadowコンポーネントでドロップシャドウを付ける。同一GameObjectのGraphicに自動追従する。</summary>
        private static void AddShadow(GameObject go, float distance = 4f, float alpha = 0.4f)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(distance, -distance);
        }

        /// <summary>パネル左上に、フェーズ名を示す角丸バッジを載せる（旧・全幅アクセント帯の置き換え）。</summary>
        private static void AddPhaseTag(RectTransform panelRT, string text, Color accent)
        {
            var go = new GameObject("PhaseTag", typeof(Image));
            go.transform.SetParent(panelRT, false);
            var img = go.GetComponent<Image>();
            img.color = accent;
            img.raycastTarget = false;
            ApplyRounded(go, ButtonSprite);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(text.Length * 15f + 40f, 40f);
            rt.anchoredPosition = new Vector2(20, -16);
            var le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            var label = CreateLabel(go.transform, "Label", text, 18, 0, bold: true);
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            var labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;
        }

        /// <summary>フルスクリーン画面専用の、画面上端を横断する色帯（画面の「カテゴリ色」を示す）。</summary>
        private static void AddAccentStrip(RectTransform panelRT, Color accent)
        {
            var stripGO = new GameObject("AccentStrip", typeof(Image));
            stripGO.transform.SetParent(panelRT, false);
            stripGO.GetComponent<Image>().color = accent;

            var stripRT = stripGO.GetComponent<RectTransform>();
            stripRT.anchorMin = new Vector2(0, 1);
            stripRT.anchorMax = new Vector2(1, 1);
            stripRT.pivot = new Vector2(0.5f, 1f);
            stripRT.sizeDelta = new Vector2(0, 6);
            stripRT.anchoredPosition = Vector2.zero;

            var le = stripGO.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent, string name, string text, int fontSize, float preferredWidth, bool bold = false)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (preferredWidth > 0)
                go.AddComponent<LayoutElement>().preferredWidth = preferredWidth;
            return tmp;
        }

        private static Image CreateBar(Transform parent, string name, Color fillColor)
        {
            var bgGO = new GameObject(name, typeof(Image));
            bgGO.transform.SetParent(parent, false);
            var bgImg = bgGO.GetComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0.14f);
            ApplyRounded(bgGO, PanelSprite);
            bgGO.AddComponent<LayoutElement>().preferredWidth = 160;

            var fillGO = new GameObject("Fill", typeof(Image));
            fillGO.transform.SetParent(bgGO.transform, false);
            var fillImg = fillGO.GetComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.5f;
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(3, 3);
            fillRT.offsetMax = new Vector2(-3, -3);

            return fillImg;
        }

        private static Button CreateButton(Transform parent, string name, string labelText)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var bgImage = go.GetComponent<Image>();
            bgImage.color = new Color(0.24f, 0.30f, 0.42f);
            ApplyRounded(go, ButtonSprite);
            AddShadow(go, 3f, 0.3f);

            var textGO = new GameObject("Label", typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var button = go.GetComponent<Button>();
            button.targetGraphic = bgImage;
            ApplyButtonColors(button);
            go.AddComponent<UIButtonPunch>();

            return button;
        }

        private static void ApplyButtonColors(Button button)
        {
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 0.90f, 0.96f);
            colors.pressedColor = new Color(0.65f, 0.65f, 0.72f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static void SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneBuilder] フィールド '{fieldName}' がGameManagerに見つかりませんでした。");
                return;
            }
            prop.objectReferenceValue = value;
        }
    }
}
