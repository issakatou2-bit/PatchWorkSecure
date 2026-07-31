using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PatchWorkSecure.EditorTools
{
    /// <summary>
    /// UIシーン全体を1メニュー操作で自動生成するエディタ拡張。
    ///
    /// 画面構成（参考にした情シス系ゲームのダッシュボード風レイアウト）:
    ///   上部    … ステータスバー（期間 / 予算 / 人望 / ストレス。各ゲージに項目名を必ず付ける）
    ///   左サイド … 導入済み対策・リスク表示・ログ（常時表示。今の備えがいつでも見える）
    ///   中央    … フェーズごとのメインパネル
    ///   下部    … ナビゲーターの立ち絵と吹き出し
    ///   最前面  … 演出レイヤー（フラッシュ・バナー・浮遊テキスト）
    ///
    /// 角丸・影はUnity標準の"UI/Skin/*.psd"組み込みスプライトとUI.Shadowコンポーネントのみで実現し、
    /// 独自テクスチャ生成は行わない（見た目の検証がエディタ上でしかできないため、実績のある標準機能に寄せている）。
    /// 絵文字はフォント(meiryo SDF)がグリフを持たないため一切使わない。アイコンは色チップで表現する。
    ///
    /// 使い方: Unity上部メニュー「PatchWorkSecure」→「シーンを自動構築」
    /// 実行前にCanvas/GameManager/AudioManagerを削除しておくと、重複生成を避けられます。
    /// </summary>
    public static class SceneBuilder
    {
        private const string PrefabDir = "Assets/Prefabs";
        private const string PersonaDir = "Assets/Personas";

        // ---- 配色パレット ----
        private static readonly Color BgDeep = new Color(0.055f, 0.060f, 0.085f, 1f);
        private static readonly Color CardBg = new Color(0.105f, 0.115f, 0.150f, 0.98f);
        private static readonly Color CardBgSoft = new Color(0.135f, 0.145f, 0.185f, 0.98f);
        private static readonly Color TextMain = new Color(0.92f, 0.94f, 0.97f);
        private static readonly Color TextSub = new Color(0.58f, 0.62f, 0.70f);

        private static readonly Color ColBudget = new Color(0.45f, 0.80f, 0.50f);
        private static readonly Color ColTrust = new Color(0.40f, 0.62f, 0.92f);
        private static readonly Color ColStress = new Color(0.90f, 0.50f, 0.38f);
        private static readonly Color ColRisk = new Color(0.90f, 0.62f, 0.30f);

        private static readonly Color AccentDay = new Color(0.35f, 0.70f, 0.58f);
        private static readonly Color AccentChore = new Color(0.42f, 0.58f, 0.80f);
        private static readonly Color AccentAttack = new Color(0.85f, 0.35f, 0.33f);
        private static readonly Color AccentParry = new Color(0.90f, 0.68f, 0.25f);
        private static readonly Color AccentResult = new Color(0.58f, 0.52f, 0.72f);
        private static readonly Color AccentTitle = new Color(0.58f, 0.46f, 0.85f);
        private static readonly Color AccentQuiz = new Color(0.40f, 0.74f, 0.52f);
        private static readonly Color AccentEnding = new Color(0.62f, 0.35f, 0.38f);
        private static readonly Color AccentSummary = new Color(0.52f, 0.46f, 0.78f);

        // ---- レイアウト定数（1920x1080基準） ----
        private const float TopBarHeight = 100f;
        private const float SidebarWidth = 340f;
        private const float CharacterStripHeight = 220f;
        private const float Margin = 20f;

        // ---- Unity組み込みスプライト（どの環境にも必ず存在する標準アセット） ----
        private static Sprite _panelSprite, _buttonSprite, _circleSprite;
        private static TMP_FontAsset _uiFont;

        private static Sprite PanelSprite => _panelSprite != null ? _panelSprite
            : (_panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"));
        private static Sprite ButtonSprite => _buttonSprite != null ? _buttonSprite
            : (_buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));
        private static Sprite CircleSprite => _circleSprite != null ? _circleSprite
            : (_circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));

        /// <summary>日本語グリフを持つフォント。全ラベルに明示的に割り当て、既定フォント任せにしない。</summary>
        private static TMP_FontAsset UiFont => _uiFont != null ? _uiFont
            : (_uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/meiryo SDF.asset"));

        [MenuItem("PatchWorkSecure/シーンを自動構築")]
        public static void BuildScene()
        {
            // 前回生成した分を必ず消してから作り直す。
            // これをやらないと実行のたびにCanvas/GameManagerが二重三重に積み上がり、
            // 古いUIが上に乗って「デザインが変わっていない」ように見える（実際に起きた不具合）。
            int removed = ClearGeneratedObjects();

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

            // Screen Space - OverlayのCanvasはRectTransformがUnity側に固定されて動かせないため、
            // 画面シェイクは「Canvas直下の入れ物」を揺らして実現する。
            var shakeRoot = CreateStretched("ShakeRoot", canvasGO.transform);

            var gmGO = new GameObject("GameManager", typeof(GameManager));
            var so = new SerializedObject(gmGO.GetComponent<GameManager>());

            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

            BuildAudioManager();
            BuildNavigatorPersonas(so);
            // 立ち絵がAssets/Sprites/<キャラ名>/に置かれていれば、この時点で自動的に割り当てる
            PersonaSpriteImporter.ImportAllInternal(verbose: false);

            BuildBackdrop(shakeRoot);
            BuildStatusBar(shakeRoot, so);
            BuildSidebar(shakeRoot, so);
            BuildCharacterStrip(shakeRoot, so);

            BuildDayPanel(shakeRoot, so);
            BuildChorePanel(shakeRoot, so);
            BuildAttackPanel(shakeRoot, so);
            BuildParryPanel(shakeRoot, so);
            BuildResultPanel(shakeRoot, so);

            BuildTitlePanel(shakeRoot, so);
            BuildQuizPanel(shakeRoot, so);
            BuildEndingPanel(shakeRoot, so);
            BuildSummaryPanel(shakeRoot, so);
            BuildSettingsOverlay(shakeRoot, so);

            // 演出レイヤーはShakeRootの外（Canvas直下の最後）に置き、
            // 画面が揺れてもフラッシュとバナーだけは揺れないようにする。
            BuildEffectLayer(canvasGO.transform, shakeRoot, so);

            SetRef(so, "choiceButtonPrefab", BuildChoiceRowPrefab());
            SetRef(so, "defenseButtonPrefab", BuildDefenseRowPrefab());
            SetRef(so, "quizOptionButtonPrefab", BuildButtonPrefab("QuizOptionButtonPrefab", 64f, 22, TextAlignmentOptions.Center));
            SetRef(so, "personaSelectButtonPrefab", BuildButtonPrefab("PersonaSelectButtonPrefab", 54f, 20, TextAlignmentOptions.Center));

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            // ここでシーンを保存しないと、生成したUIはメモリ上にしか存在せず、
            // Unityを閉じた時点で全部消える（実際にSampleScene.unityが空のままだった）。
            string savedPath = SaveActiveScene();

            Selection.activeGameObject = gmGO;
            EditorUtility.DisplayDialog(
                "構築完了",
                $"シーンを生成して保存しました。\n\n保存先: {savedPath}\n" +
                (removed > 0 ? $"（前回生成された{removed}個のオブジェクトを削除してから作り直しました）\n" : "") +
                "\nPlayして、タイトル→事前クイズ→本編→エンディング→事後クイズ→サマリー、の流れを確認してください。\n" +
                "Consoleにエラーが出たら、そのまま貼ってください。",
                "OK");
        }

        /// <summary>
        /// 前回このスクリプトが生成したオブジェクトを取り除く。
        /// 目印はGameManager / AudioManager / UIEffects の各コンポーネントと、ルートの"Canvas"。
        /// EventSystemは他と衝突しないので残す。
        /// </summary>
        private static int ClearGeneratedObjects()
        {
            var targets = new System.Collections.Generic.HashSet<GameObject>();

            foreach (var c in Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include))
                targets.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<AudioManager>(FindObjectsInactive.Include))
                targets.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
                targets.Add(c.transform.root.gameObject); // Canvas配下のUIごと消す

            int count = 0;
            foreach (var go in targets)
            {
                if (go == null) continue;
                Object.DestroyImmediate(go);
                count++;
            }
            return count;
        }

        /// <summary>
        /// 現在開いているシーンをディスクに保存する。名前が未設定なら既定のパスに保存する。
        /// </summary>
        private static string SaveActiveScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);

            if (string.IsNullOrEmpty(scene.path))
            {
                const string defaultPath = "Assets/Scenes/SampleScene.unity";
                Directory.CreateDirectory("Assets/Scenes");
                EditorSceneManager.SaveScene(scene, defaultPath);
                return defaultPath;
            }

            EditorSceneManager.SaveScene(scene);
            return scene.path;
        }

        // ================= 背景 =================

        private static void BuildBackdrop(Transform parent)
        {
            var rt = CreateStretched("Backdrop", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = BgDeep;
            img.raycastTarget = false;
        }

        // ================= ステータスバー =================

        private static void BuildStatusBar(Transform parent, SerializedObject so)
        {
            var rt = CreatePanelBase("StatusBarPanel", parent, CardBg);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(Margin, -(TopBarHeight + Margin));
            rt.offsetMax = new Vector2(-Margin, -Margin);
            ApplyRounded(rt.gameObject, PanelSprite);
            AddShadow(rt.gameObject, 5f, 0.45f);

            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // 期間チップ（大きく期間、小さくターン数）
            var periodChip = CreatePanelBase("PeriodChip", rt, CardBgSoft);
            ApplyRounded(periodChip.gameObject, PanelSprite);
            var periodLE = periodChip.gameObject.AddComponent<LayoutElement>();
            periodLE.preferredWidth = 300f;
            periodLE.flexibleWidth = 0f;
            AddAccentBar(periodChip, AccentTitle);

            var periodLabel = CreateLabel(periodChip, "PeriodLabel", "4月上旬", 28, 0, bold: true);
            StretchTo(periodLabel.rectTransform, new Vector2(0, 0.42f), new Vector2(1, 1), new Vector2(26, 0), new Vector2(-14, -4));
            periodLabel.alignment = TextAlignmentOptions.MidlineLeft;

            var turnLabel = CreateLabel(periodChip, "TurnLabel", "ターン 1 / 36", 17, 0);
            turnLabel.color = TextSub;
            StretchTo(turnLabel.rectTransform, new Vector2(0, 0), new Vector2(1, 0.44f), new Vector2(26, 6), new Vector2(-14, 0));
            turnLabel.alignment = TextAlignmentOptions.MidlineLeft;

            // ステータスチップ（項目名 + 数値 + ゲージ）
            var budgetChip = CreateStatChip(rt, "予算　¥100", ColBudget, 300f, out var budgetText, out var budgetBar);
            var trustChip = CreateStatChip(rt, "人望　30 / 100", ColTrust, 300f, out var trustText, out var trustBar);
            var stressChip = CreateStatChip(rt, "ストレス　20 / 100", ColStress, 300f, out var stressText, out var stressBar);

            SetRef(so, "periodLabel", periodLabel);
            SetRef(so, "turnLabel", turnLabel);
            SetRef(so, "budgetText", budgetText);
            SetRef(so, "trustText", trustText);
            SetRef(so, "stressText", stressText);
            SetRef(so, "budgetBar", budgetBar);
            SetRef(so, "trustBar", trustBar);
            SetRef(so, "stressBar", stressBar);
            SetRef(so, "budgetChip", budgetChip);
            SetRef(so, "trustChip", trustChip);
            SetRef(so, "stressChip", stressChip);
        }

        /// <summary>
        /// 「項目名 + 数値」と、その下のゲージをひとまとめにしたチップ。
        /// 数値だけだと何のゲージか分からないので、ラベルは必ず項目名込みの文字列を入れる
        /// （GameManager.ApplyStat側も "予算　¥100" のような書式で更新する）。
        /// </summary>
        private static RectTransform CreateStatChip(Transform parent, string initialText, Color accent, float width,
                                                    out TextMeshProUGUI valueText, out Image barFill)
        {
            var chip = CreatePanelBase("StatChip", parent, CardBgSoft);
            ApplyRounded(chip.gameObject, PanelSprite);
            var le = chip.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.flexibleWidth = 0f;

            AddAccentBar(chip, accent);

            valueText = CreateLabel(chip, "ValueText", initialText, 21, 0, bold: true);
            StretchTo(valueText.rectTransform, new Vector2(0, 0.40f), new Vector2(1, 1), new Vector2(26, 0), new Vector2(-14, -4));
            valueText.alignment = TextAlignmentOptions.MidlineLeft;

            var barBg = new GameObject("BarBg", typeof(Image));
            barBg.transform.SetParent(chip, false);
            var barBgImg = barBg.GetComponent<Image>();
            barBgImg.color = new Color(1f, 1f, 1f, 0.10f);
            barBgImg.raycastTarget = false;
            ApplyRounded(barBg, PanelSprite);
            StretchTo(barBg.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 0), new Vector2(26, 14), new Vector2(-14, 28));

            var fillGO = new GameObject("Fill", typeof(Image));
            fillGO.transform.SetParent(barBg.transform, false);
            barFill = fillGO.GetComponent<Image>();
            barFill.color = accent;
            barFill.raycastTarget = false;
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillAmount = 0.5f;
            StretchTo(fillGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));

            return chip;
        }

        // ================= 左サイドバー =================

        private static void BuildSidebar(Transform parent, SerializedObject so)
        {
            var root = new GameObject("Sidebar", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(Margin, Margin);
            rt.offsetMax = new Vector2(Margin + SidebarWidth, -(TopBarHeight + Margin * 2));

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            // 導入済み対策（残りの高さを全部使う）
            var defenseCard = CreateSidebarCard(rt, "DefenseCard", "導入済み対策", AccentDay, 0f, 1f, out var defenseContent);
            var defenseContainer = new GameObject("DefenseButtonContainer", typeof(RectTransform));
            defenseContainer.transform.SetParent(defenseContent, false);
            StretchTo(defenseContainer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dLayout = defenseContainer.AddComponent<VerticalLayoutGroup>();
            dLayout.spacing = 4;
            dLayout.childAlignment = TextAnchor.UpperCenter;
            dLayout.childControlWidth = true;
            dLayout.childForceExpandWidth = true;
            dLayout.childControlHeight = false;
            dLayout.childForceExpandHeight = false;

            // リスク表示（対策を買うと下がるので、投資の効果がその場で分かる）
            CreateSidebarCard(rt, "RiskCard", "リスク", ColRisk, 132f, 0f, out var riskContent);
            var riskLevel = CreateLabel(riskContent, "RiskLevelText", "リスクレベル　—", 20, 0, bold: true);
            riskLevel.color = ColRisk;
            StretchTo(riskLevel.rectTransform, new Vector2(0, 0.54f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            riskLevel.alignment = TextAlignmentOptions.MidlineLeft;

            var riskDamage = CreateLabel(riskContent, "RiskDamageText", "被害予測　¥0", 17, 0);
            riskDamage.color = TextSub;
            StretchTo(riskDamage.rectTransform, new Vector2(0, 0.22f), new Vector2(1, 0.56f), Vector2.zero, Vector2.zero);
            riskDamage.alignment = TextAlignmentOptions.MidlineLeft;

            var riskBarBg = new GameObject("RiskBarBg", typeof(Image));
            riskBarBg.transform.SetParent(riskContent, false);
            var riskBgImg = riskBarBg.GetComponent<Image>();
            riskBgImg.color = new Color(1f, 1f, 1f, 0.10f);
            riskBgImg.raycastTarget = false;
            ApplyRounded(riskBarBg, PanelSprite);
            StretchTo(riskBarBg.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 12));

            var riskFillGO = new GameObject("Fill", typeof(Image));
            riskFillGO.transform.SetParent(riskBarBg.transform, false);
            var riskFill = riskFillGO.GetComponent<Image>();
            riskFill.color = ColRisk;
            riskFill.raycastTarget = false;
            riskFill.type = Image.Type.Filled;
            riskFill.fillMethod = Image.FillMethod.Horizontal;
            riskFill.fillAmount = 0f;
            StretchTo(riskFillGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));

            // ログ
            CreateSidebarCard(rt, "LogCard", "ログ", AccentResult, 210f, 0f, out var logContent);
            var logText = CreateLabel(logContent, "LogText", "", 15, 0);
            logText.color = TextSub;
            logText.alignment = TextAlignmentOptions.TopLeft;
            StretchTo(logText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            SetRef(so, "defenseButtonContainer", defenseContainer.GetComponent<RectTransform>());
            SetRef(so, "riskLevelText", riskLevel);
            SetRef(so, "riskDamageText", riskDamage);
            SetRef(so, "riskBar", riskFill);
            SetRef(so, "logText", logText);
        }

        /// <summary>見出し付きのサイドバーカードを作り、中身を置くための領域を返す。</summary>
        private static RectTransform CreateSidebarCard(Transform parent, string name, string title, Color accent,
                                                       float preferredHeight, float flexibleHeight,
                                                       out RectTransform content)
        {
            var card = CreatePanelBase(name, parent, CardBg);
            ApplyRounded(card.gameObject, PanelSprite);
            AddShadow(card.gameObject, 4f, 0.4f);

            var le = card.gameObject.AddComponent<LayoutElement>();
            if (preferredHeight > 0f) le.preferredHeight = preferredHeight;
            le.flexibleHeight = flexibleHeight;

            var dot = new GameObject("TitleAccent", typeof(Image));
            dot.transform.SetParent(card, false);
            var dotImg = dot.GetComponent<Image>();
            dotImg.color = accent;
            dotImg.raycastTarget = false;
            ApplyRounded(dot, PanelSprite);
            var dotRT = dot.GetComponent<RectTransform>();
            dotRT.anchorMin = new Vector2(0, 1);
            dotRT.anchorMax = new Vector2(0, 1);
            dotRT.pivot = new Vector2(0, 1);
            dotRT.sizeDelta = new Vector2(5, 18);
            dotRT.anchoredPosition = new Vector2(14, -14);

            var titleLabel = CreateLabel(card, "Title", title, 17, 0, bold: true);
            titleLabel.color = TextMain;
            titleLabel.alignment = TextAlignmentOptions.MidlineLeft;
            var titleRT = titleLabel.rectTransform;
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.offsetMin = new Vector2(26, -34);
            titleRT.offsetMax = new Vector2(-14, -10);

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(card, false);
            content = contentGO.GetComponent<RectTransform>();
            StretchTo(content, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -40));

            return card;
        }

        // ================= ナビゲーター（立ち絵 + 吹き出し） =================

        private static void BuildCharacterStrip(Transform parent, SerializedObject so)
        {
            var strip = new GameObject("CharacterStrip", typeof(RectTransform));
            strip.transform.SetParent(parent, false);
            var rt = strip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(Margin * 2 + SidebarWidth, Margin);
            rt.offsetMax = new Vector2(-Margin, Margin + CharacterStripHeight);

            // 立ち絵の枠（イメージカラーで着色。素材が入るまではここが額縁になる）
            var frameGO = new GameObject("PortraitFrame", typeof(Image));
            frameGO.transform.SetParent(rt, false);
            var frameImg = frameGO.GetComponent<Image>();
            frameImg.color = new Color(0.20f, 0.14f, 0.18f, 1f);
            ApplyRounded(frameGO, PanelSprite);
            AddShadow(frameGO, 6f, 0.5f);
            var frameRT = frameGO.GetComponent<RectTransform>();
            frameRT.anchorMin = new Vector2(0, 0.5f);
            frameRT.anchorMax = new Vector2(0, 0.5f);
            frameRT.pivot = new Vector2(0, 0.5f);
            frameRT.sizeDelta = new Vector2(196, 196);
            frameRT.anchoredPosition = new Vector2(0, 0);

            // 実際に表情スプライトを差し替えるImage。素材が無い間はイメージカラーで塗られる。
            var portraitGO = new GameObject("NavigatorPortrait", typeof(Image));
            portraitGO.transform.SetParent(frameRT, false);
            var portraitImg = portraitGO.GetComponent<Image>();
            portraitImg.color = Color.white;
            portraitImg.preserveAspect = true;
            portraitImg.raycastTarget = false;
            portraitImg.enabled = false; // 立ち絵が割り当てられるまでは代わりにプレースホルダーを出す
            StretchTo(portraitGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));

            var placeholder = BuildPortraitPlaceholder(frameRT, new Color(0.96f, 0.55f, 0.70f), out var hairParts);

            // 吹き出し
            var bubble = CreatePanelBase("SpeechBubble", rt, new Color(0.145f, 0.155f, 0.200f, 0.98f));
            ApplyRounded(bubble.gameObject, PanelSprite);
            AddShadow(bubble.gameObject, 6f, 0.45f);
            StretchTo(bubble, Vector2.zero, Vector2.one, new Vector2(220, 34), new Vector2(0, -10));

            var accentGO = new GameObject("BubbleAccent", typeof(Image));
            accentGO.transform.SetParent(bubble, false);
            var accentImg = accentGO.GetComponent<Image>();
            accentImg.color = new Color(0.96f, 0.55f, 0.70f);
            accentImg.raycastTarget = false;
            ApplyRounded(accentGO, PanelSprite);
            StretchTo(accentGO.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1), new Vector2(10, 12), new Vector2(17, -12));

            var line = CreateLabel(bubble, "NavigatorLine", "", 23, 0);
            line.color = TextMain;
            line.alignment = TextAlignmentOptions.TopLeft;
            StretchTo(line.rectTransform, Vector2.zero, Vector2.one, new Vector2(34, 30), new Vector2(-26, -20));

            // 名前チップ（吹き出しの下端に半分かかるように出す）
            var nameChipGO = new GameObject("NameChip", typeof(Image));
            nameChipGO.transform.SetParent(bubble, false);
            var nameChipImg = nameChipGO.GetComponent<Image>();
            nameChipImg.color = new Color(0.96f, 0.55f, 0.70f);
            nameChipImg.raycastTarget = false;
            ApplyRounded(nameChipGO, ButtonSprite);
            AddShadow(nameChipGO, 3f, 0.4f);
            var nameChipRT = nameChipGO.GetComponent<RectTransform>();
            nameChipRT.anchorMin = new Vector2(0, 0);
            nameChipRT.anchorMax = new Vector2(0, 0);
            nameChipRT.pivot = new Vector2(0, 0.5f);
            nameChipRT.sizeDelta = new Vector2(150, 36);
            nameChipRT.anchoredPosition = new Vector2(28, 0);

            var nameText = CreateLabel(nameChipGO.transform, "NameText", "", 18, 0, bold: true);
            nameText.color = new Color(0.12f, 0.10f, 0.14f);
            nameText.alignment = TextAlignmentOptions.Center;
            StretchTo(nameText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            SetRef(so, "navigatorPortrait", portraitImg);
            SetRef(so, "portraitPlaceholder", placeholder);
            SetRefArray(so, "placeholderHairImages", hairParts);
            SetRef(so, "portraitFrame", frameImg);
            SetRef(so, "speechBubble", bubble.gameObject);
            SetRef(so, "speechBubbleAccent", accentImg);
            SetRef(so, "navigatorLine", line);
            SetRef(so, "navigatorNameChip", nameChipImg);
            SetRef(so, "navigatorNameText", nameText);
        }

        /// <summary>
        /// 立ち絵素材が届くまでの代役となる、簡易的なチビキャラ。
        /// Unity組み込みの円(Knob)と角丸(UISprite)だけで組み立てるので追加素材は要らない。
        /// 髪のパーツだけ配列で返し、選択中キャラのイメージカラーで塗り分けられるようにする。
        /// 立ち絵が割り当てられたらGameManager側でこの階層ごと非表示にする。
        /// </summary>
        private static GameObject BuildPortraitPlaceholder(RectTransform parent, Color theme, out Object[] hairParts)
        {
            var root = new GameObject("PortraitPlaceholder", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            StretchTo(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));

            var skin = new Color(0.99f, 0.88f, 0.83f);
            var uniform = new Color(0.24f, 0.27f, 0.38f);
            var ink = new Color(0.16f, 0.14f, 0.20f);
            var t = root.transform;

            // 奥から手前の順に置く（後に作ったものが手前に描画される）
            var hairBack = AddPortraitPart(t, "HairBack", CircleSprite, theme, new Vector2(128, 128), new Vector2(0, 96));
            var tailL = AddPortraitPart(t, "TwinTailL", CircleSprite, theme, new Vector2(44, 76), new Vector2(-58, 86));
            var tailR = AddPortraitPart(t, "TwinTailR", CircleSprite, theme, new Vector2(44, 76), new Vector2(58, 86));
            AddPortraitPart(t, "Body", ButtonSprite, uniform, new Vector2(108, 70), new Vector2(0, 36));
            AddPortraitPart(t, "Head", CircleSprite, skin, new Vector2(102, 102), new Vector2(0, 100));
            AddPortraitPart(t, "EyeL", CircleSprite, ink, new Vector2(15, 19), new Vector2(-22, 100));
            AddPortraitPart(t, "EyeR", CircleSprite, ink, new Vector2(15, 19), new Vector2(22, 100));
            AddPortraitPart(t, "Mouth", ButtonSprite, new Color(0.88f, 0.46f, 0.52f), new Vector2(17, 7), new Vector2(0, 84));
            var bangs = AddPortraitPart(t, "Bangs", CircleSprite, theme, new Vector2(110, 56), new Vector2(0, 136));

            hairParts = new Object[] { hairBack, tailL, tailR, bangs };
            return root;
        }

        /// <summary>プレースホルダーの部品を1つ置く。下端中央を原点とした座標で指定する。</summary>
        private static Image AddPortraitPart(Transform parent, string name, Sprite sprite, Color color,
                                             Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            // 円(Knob)はSimpleのまま拡縮して楕円にする。角丸(UISprite)だけ9-Sliceにする。
            img.type = sprite == ButtonSprite ? Image.Type.Sliced : Image.Type.Simple;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return img;
        }

        // ================= メインフェーズパネル =================

        /// <summary>サイドバー・ステータスバー・キャラ帯を避けた中央の作業領域にパネルを作る。</summary>
        private static RectTransform CreateMainAreaPanel(string name, Transform parent, Color bg, Color accent, string phaseTitle)
        {
            var rt = CreatePanelBase(name, parent, bg);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(Margin * 2 + SidebarWidth, Margin * 2 + CharacterStripHeight);
            rt.offsetMax = new Vector2(-Margin, -(TopBarHeight + Margin * 2));
            ApplyRounded(rt.gameObject, PanelSprite);
            AddShadow(rt.gameObject, 6f, 0.45f);
            AddPhaseTag(rt, phaseTitle, accent);
            rt.gameObject.SetActive(false);
            return rt;
        }

        /// <summary>
        /// メインパネル共通の縦積みレイアウト。
        /// childControlHeightをtrueにしてあるので、子には必ずLayoutElementで高さを与えること
        /// （与えないと高さ0になって見えなくなる）。
        /// </summary>
        private static VerticalLayoutGroup AddPanelLayout(RectTransform rt, int spacing, TextAnchor align)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 78, 34);
            layout.spacing = spacing;
            layout.childAlignment = align;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return layout;
        }

        private static void BuildDayPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateMainAreaPanel("DayPanel", parent, new Color(0.09f, 0.13f, 0.12f, 0.96f), AccentDay, "本日の業務");
            AddPanelLayout(rt, 22, TextAnchor.UpperCenter);

            var heading = CreateLabel(rt, "DayHeading", "今日をどう過ごす？", 34, 0, bold: true);
            heading.alignment = TextAlignmentOptions.Center;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;

            var guide = CreateLabel(
                rt, "DayGuide",
                "左の「導入済み対策」から備えを強化できる。\n強化するほど、左下の被害予測が下がっていく。",
                21, 0);
            guide.color = TextSub;
            guide.alignment = TextAlignmentOptions.Center;
            guide.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;

            var proceedBtn = CreateButton(rt, "ProceedButton", "今日の業務を進める", AccentDay);
            proceedBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 86;

            SetRef(so, "dayPanel", rt.gameObject);
            SetRef(so, "proceedButton", proceedBtn);
        }

        private static void BuildChorePanel(Transform parent, SerializedObject so)
        {
            var rt = CreateMainAreaPanel("ChorePanel", parent, new Color(0.10f, 0.11f, 0.16f, 0.96f), AccentChore, "雑務対応");
            AddPanelLayout(rt, 24, TextAnchor.UpperCenter);

            var choreText = CreateLabel(rt, "ChoreText", "（雑務の内容がここに入る）", 28, 0);
            choreText.alignment = TextAlignmentOptions.Center;
            choreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 110;

            var solveBtn = CreateButton(rt, "SolveButton", "誠実に対応する", AccentDay);
            var postponeBtn = CreateButton(rt, "PostponeButton", "後回しにする", new Color(0.45f, 0.45f, 0.52f));
            solveBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 78;
            postponeBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 78;

            SetRef(so, "chorePanel", rt.gameObject);
            SetRef(so, "choreText", choreText);
            SetRef(so, "solveChoreButton", solveBtn);
            SetRef(so, "postponeChoreButton", postponeBtn);
        }

        private static void BuildAttackPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateMainAreaPanel("AttackPanel", parent, new Color(0.16f, 0.085f, 0.09f, 0.96f), AccentAttack, "インシデント対応");
            AddPanelLayout(rt, 12, TextAnchor.UpperLeft);

            var nameText = CreateLabel(rt, "AttackNameText", "（攻撃名）", 38, 0, bold: true);
            nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 54;

            // 格付けバッジは横並びの行に入れて、幅がテキストに引きずられないようにする
            var gradeRow = new GameObject("GradeRow", typeof(RectTransform));
            gradeRow.transform.SetParent(rt, false);
            var gradeRowLayout = gradeRow.AddComponent<HorizontalLayoutGroup>();
            gradeRowLayout.childAlignment = TextAnchor.MiddleLeft;
            gradeRowLayout.childControlWidth = false;
            gradeRowLayout.childForceExpandWidth = false;
            gradeRowLayout.childControlHeight = false;
            gradeRowLayout.childForceExpandHeight = false;
            gradeRow.AddComponent<LayoutElement>().preferredHeight = 38;

            var gradeChipGO = new GameObject("GradeChip", typeof(Image));
            gradeChipGO.transform.SetParent(gradeRow.transform, false);
            var gradeChip = gradeChipGO.GetComponent<Image>();
            gradeChip.color = AccentAttack;
            gradeChip.raycastTarget = false;
            ApplyRounded(gradeChipGO, ButtonSprite);
            gradeChipGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 34);

            var gradeText = CreateLabel(gradeChipGO.transform, "AttackGradeText", "", 18, 0, bold: true);
            gradeText.alignment = TextAlignmentOptions.Center;
            StretchTo(gradeText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var introLine = CreateLabel(rt, "AttackIntroLine", "「（台詞）」", 24, 0);
            introLine.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;

            var scTerm = CreateLabel(rt, "ScTermText", "（SC用語）", 18, 0);
            scTerm.color = new Color(0.70f, 0.78f, 0.86f);
            scTerm.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;

            var containerGO = new GameObject("ChoiceButtonContainer", typeof(RectTransform));
            containerGO.transform.SetParent(rt, false);
            var containerRT = containerGO.GetComponent<RectTransform>();
            var vlayout = containerGO.AddComponent<VerticalLayoutGroup>();
            vlayout.spacing = 10;
            vlayout.childControlWidth = true;
            vlayout.childForceExpandWidth = true;
            vlayout.childControlHeight = false;
            vlayout.childForceExpandHeight = false;
            containerGO.AddComponent<LayoutElement>().flexibleHeight = 1;

            SetRef(so, "attackPanel", rt.gameObject);
            SetRef(so, "attackNameText", nameText);
            SetRef(so, "attackGradeText", gradeText);
            SetRef(so, "attackGradeChip", gradeChip);
            SetRef(so, "attackIntroLine", introLine);
            SetRef(so, "scTermText", scTerm);
            SetRef(so, "choiceButtonContainer", containerRT);
        }

        private static void BuildParryPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateMainAreaPanel("ParryPanel", parent, new Color(0.15f, 0.13f, 0.07f, 0.96f), AccentParry, "意思決定");
            var layout = AddPanelLayout(rt, 26, TextAnchor.MiddleCenter);
            // このパネルだけは幅を引き伸ばさず、トラックやボタンの指定幅をそのまま使う
            layout.childForceExpandWidth = false;

            var hint = CreateLabel(rt, "ParryHint", "黄色いゾーンで「ここだ！」を押すと防御率が上がる", 21, 0);
            hint.alignment = TextAlignmentOptions.Center;
            hint.color = new Color(0.88f, 0.82f, 0.70f);
            var hintLE = hint.gameObject.AddComponent<LayoutElement>();
            hintLE.preferredWidth = 900;
            hintLE.preferredHeight = 34;

            var trackGO = new GameObject("ParryTrack", typeof(Image));
            trackGO.transform.SetParent(rt, false);
            var trackImg = trackGO.GetComponent<Image>();
            trackImg.color = new Color(0.20f, 0.20f, 0.23f);
            ApplyRounded(trackGO, PanelSprite);
            var trackRT = trackGO.GetComponent<RectTransform>();
            trackRT.sizeDelta = new Vector2(900, 54);
            var trackLE = trackGO.AddComponent<LayoutElement>();
            trackLE.preferredWidth = 900;
            trackLE.preferredHeight = 54;

            // スイートゾーン（GameManagerの判定しきい値と対応する帯を可視化する）
            float goodHalf = (1f - GameManager.ParryGoodThreshold) * 0.5f;
            float perfectHalf = (1f - GameManager.ParryPerfectThreshold) * 0.5f;
            AddParryZone(trackRT, "GoodZone", 0.5f - goodHalf, 0.5f + goodHalf, new Color(0.45f, 0.72f, 0.38f, 0.5f));
            AddParryZone(trackRT, "PerfectZone", 0.5f - perfectHalf, 0.5f + perfectHalf, new Color(0.95f, 0.80f, 0.25f, 0.85f));

            var markerGO = new GameObject("ParryMarker", typeof(Image));
            markerGO.transform.SetParent(trackRT, false);
            var markerImg = markerGO.GetComponent<Image>();
            markerImg.color = Color.white;
            markerImg.raycastTarget = false;
            ApplyRounded(markerGO, ButtonSprite);
            AddShadow(markerGO, 2f, 0.6f);
            var markerRT = markerGO.GetComponent<RectTransform>();
            markerRT.anchorMin = new Vector2(0.5f, 0.5f);
            markerRT.anchorMax = new Vector2(0.5f, 0.5f);
            markerRT.pivot = new Vector2(0.5f, 0.5f);
            markerRT.sizeDelta = new Vector2(16, 62);
            markerRT.anchoredPosition = Vector2.zero;

            var feedbackText = CreateLabel(rt, "ParryFeedbackText", "", 40, 0, bold: true);
            feedbackText.alignment = TextAlignmentOptions.Center;
            var fbLE = feedbackText.gameObject.AddComponent<LayoutElement>();
            fbLE.preferredWidth = 600;
            fbLE.preferredHeight = 58;

            var parryBtn = CreateButton(rt, "ParryButton", "ここだ！", AccentParry);
            var parryLE = parryBtn.gameObject.AddComponent<LayoutElement>();
            parryLE.preferredWidth = 340;
            parryLE.preferredHeight = 92;

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
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var zoneRT = go.GetComponent<RectTransform>();
            zoneRT.anchorMin = new Vector2(xMin, 0f);
            zoneRT.anchorMax = new Vector2(xMax, 1f);
            zoneRT.offsetMin = Vector2.zero;
            zoneRT.offsetMax = Vector2.zero;
        }

        private static void BuildResultPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateMainAreaPanel("ResultPanel", parent, new Color(0.10f, 0.10f, 0.14f, 0.96f), AccentResult, "対応結果");
            AddPanelLayout(rt, 26, TextAnchor.MiddleCenter);

            var resultText = CreateLabel(rt, "ResultText", "（結果メッセージ）", 34, 0, bold: true);
            resultText.alignment = TextAlignmentOptions.Center;
            resultText.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;

            var resultLine = CreateLabel(rt, "ResultCharacterLine", "「（キャラ台詞）」", 23, 0);
            resultLine.alignment = TextAlignmentOptions.Center;
            resultLine.color = TextSub;
            resultLine.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;

            var nextBtn = CreateButton(rt, "NextDayButton", "次の日へ", AccentResult);
            nextBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 82;

            SetRef(so, "resultPanel", rt.gameObject);
            SetRef(so, "resultText", resultText);
            SetRef(so, "resultCharacterLine", resultLine);
            SetRef(so, "nextDayButton", nextBtn);
        }

        // ================= フルスクリーン画面 =================

        private static RectTransform CreateFullScreenPanel(string name, Transform parent, Color bg, Color accent)
        {
            var rt = CreatePanelBase(name, parent, bg);
            StretchTo(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (accent.a > 0f) AddAccentStrip(rt, accent);
            rt.gameObject.SetActive(false);
            return rt;
        }

        /// <summary>フルスクリーン画面の中央に浮かべる、角丸+影付きのカード。</summary>
        private static RectTransform CreateCenterCard(Transform parent, string name, Vector2 size, Color bg)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            ApplyRounded(go, PanelSprite);
            AddShadow(go, 10f, 0.55f);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        private static void BuildTitlePanel(Transform parent, SerializedObject so)
        {
            var rt = CreateFullScreenPanel("TitlePanel", parent, new Color(0.055f, 0.055f, 0.085f, 1f), AccentTitle);
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 22;
            // 子はそれぞれLayoutElementで幅・高さを指定してあるので、引き伸ばさずその値を使う
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            var title = CreateLabel(rt, "GameTitle", "PatchWorkSecure", 76, 0, bold: true);
            title.alignment = TextAlignmentOptions.Center;
            var titleLE = title.gameObject.AddComponent<LayoutElement>();
            titleLE.preferredWidth = 1000;
            titleLE.preferredHeight = 96;

            var tagline = CreateLabel(
                rt, "Tagline",
                "なにごともない、いつもの平穏なオフィスの日常を\nツギハギ（PatchWork）しながら守り抜け",
                21, 0);
            tagline.alignment = TextAlignmentOptions.Center;
            tagline.color = TextSub;
            var taglineLE = tagline.gameObject.AddComponent<LayoutElement>();
            taglineLE.preferredWidth = 900;
            taglineLE.preferredHeight = 76;

            var personaLabel = CreateLabel(rt, "PersonaSelectLabel", "ナビゲーターを選ぶ", 17, 0);
            personaLabel.alignment = TextAlignmentOptions.Center;
            personaLabel.color = TextSub;
            var personaLabelLE = personaLabel.gameObject.AddComponent<LayoutElement>();
            personaLabelLE.preferredWidth = 900;
            personaLabelLE.preferredHeight = 28;

            var personaContainerGO = new GameObject("PersonaSelectContainer", typeof(RectTransform));
            personaContainerGO.transform.SetParent(rt, false);
            var personaContainerRT = personaContainerGO.GetComponent<RectTransform>();
            var personaLayout = personaContainerGO.AddComponent<HorizontalLayoutGroup>();
            personaLayout.spacing = 14;
            personaLayout.childAlignment = TextAnchor.MiddleCenter;
            personaLayout.childControlWidth = false;
            personaLayout.childForceExpandWidth = false;
            personaLayout.childControlHeight = false;
            personaLayout.childForceExpandHeight = false;
            var personaContainerLE = personaContainerGO.AddComponent<LayoutElement>();
            personaContainerLE.preferredWidth = 900;
            personaContainerLE.preferredHeight = 60;

            var startBtn = CreateButton(rt, "StartButton", "はじめる", AccentTitle);
            var startLE = startBtn.gameObject.AddComponent<LayoutElement>();
            startLE.preferredWidth = 300;
            startLE.preferredHeight = 82;

            SetRef(so, "titlePanel", rt.gameObject);
            SetRef(so, "startButton", startBtn);
            SetRef(so, "personaSelectContainer", personaContainerRT);
        }

        private static void BuildQuizPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateFullScreenPanel("QuizPanel", parent, new Color(0.06f, 0.095f, 0.075f, 1f), AccentQuiz);

            var card = CreateCenterCard(rt, "QuizCard", new Vector2(980, 600), new Color(0.09f, 0.13f, 0.10f, 0.99f));
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(64, 64, 54, 54);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 24;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            var progress = CreateLabel(card, "QuizProgressText", "事前クイズ 1 / 3", 19, 0);
            progress.alignment = TextAlignmentOptions.Center;
            progress.color = new Color(0.58f, 0.80f, 0.65f);
            progress.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

            var question = CreateLabel(card, "QuizQuestionText", "（設問がここに入る）", 28, 0, bold: true);
            question.alignment = TextAlignmentOptions.Center;
            question.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;

            var containerGO = new GameObject("QuizOptionContainer", typeof(RectTransform));
            containerGO.transform.SetParent(card, false);
            var containerRT = containerGO.GetComponent<RectTransform>();
            var vlayout = containerGO.AddComponent<VerticalLayoutGroup>();
            vlayout.spacing = 14;
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childControlWidth = true;
            vlayout.childForceExpandWidth = true;
            vlayout.childControlHeight = false;
            vlayout.childForceExpandHeight = false;
            containerGO.AddComponent<LayoutElement>().flexibleHeight = 1;

            SetRef(so, "quizPanel", rt.gameObject);
            SetRef(so, "quizProgressText", progress);
            SetRef(so, "quizQuestionText", question);
            SetRef(so, "quizOptionContainer", containerRT);
        }

        private static void BuildEndingPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateFullScreenPanel("EndingPanel", parent, new Color(0.08f, 0.055f, 0.065f, 1f), AccentEnding);

            var card = CreateCenterCard(rt, "EndingCard", new Vector2(1000, 460), new Color(0.115f, 0.085f, 0.100f, 0.99f));
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(60, 60, 56, 48);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 26;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            var text = CreateLabel(card, "EndingText", "（結果メッセージ）", 38, 0, bold: true);
            text.alignment = TextAlignmentOptions.Center;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;

            var line = CreateLabel(card, "EndingCharacterLine", "", 22, 0);
            line.alignment = TextAlignmentOptions.Center;
            line.color = TextSub;
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;

            var btn = CreateButton(card, "EndingContinueButton", "結果を振り返る", AccentEnding);
            btn.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;

            SetRef(so, "endingPanel", rt.gameObject);
            SetRef(so, "endingText", text);
            SetRef(so, "endingCharacterLine", line);
            SetRef(so, "endingContinueButton", btn);
        }

        private static void BuildSummaryPanel(Transform parent, SerializedObject so)
        {
            var rt = CreateFullScreenPanel("SummaryPanel", parent, new Color(0.070f, 0.065f, 0.100f, 1f), AccentSummary);

            var card = CreateCenterCard(rt, "SummaryCard", new Vector2(800, 500), new Color(0.110f, 0.100f, 0.150f, 0.99f));
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(56, 56, 50, 46);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 26;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            var heading = CreateLabel(card, "SummaryHeading", "学習の記録", 30, 0, bold: true);
            heading.alignment = TextAlignmentOptions.Center;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;

            var text = CreateLabel(card, "SummaryText", "（結果サマリーがここに入る）", 22, 0);
            text.alignment = TextAlignmentOptions.Center;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 220;

            var btn = CreateButton(card, "SummaryCloseButton", "タイトルへ戻る", AccentSummary);
            btn.gameObject.AddComponent<LayoutElement>().preferredHeight = 78;

            SetRef(so, "summaryPanel", rt.gameObject);
            SetRef(so, "summaryText", text);
            SetRef(so, "summaryCloseButton", btn);
        }

        // ================= 設定オーバーレイ =================

        private static void BuildSettingsOverlay(Transform parent, SerializedObject so)
        {
            var btnGO = new GameObject("SettingsOpenButton", typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var btnBg = btnGO.GetComponent<Image>();
            btnBg.color = new Color(0.20f, 0.21f, 0.26f, 0.95f);
            ApplyRounded(btnGO, ButtonSprite);
            AddShadow(btnGO, 3f, 0.4f);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = btnRT.anchorMax = btnRT.pivot = new Vector2(1, 1);
            btnRT.sizeDelta = new Vector2(84, 40);
            btnRT.anchoredPosition = new Vector2(-(Margin + 14), -(TopBarHeight + Margin + 12));

            var icon = CreateLabel(btnGO.transform, "Label", "設定", 17, 0, bold: true);
            icon.alignment = TextAlignmentOptions.Center;
            StretchTo(icon.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var openBtn = btnGO.GetComponent<Button>();
            openBtn.targetGraphic = btnBg;
            ApplyButtonColors(openBtn);
            btnGO.AddComponent<UIButtonPunch>();

            // 背景は画面全体を薄暗く覆うだけ。中身は中央のダイアログカードにまとめる。
            var overlayRT = CreateFullScreenPanel("SettingsPanel", parent, new Color(0.02f, 0.02f, 0.04f, 0.80f), new Color(0, 0, 0, 0));

            var card = CreateCenterCard(overlayRT, "SettingsCard", new Vector2(520, 470), CardBg);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(44, 44, 44, 40);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 22;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            var title = CreateLabel(card, "SettingsTitle", "設定", 34, 0, bold: true);
            title.alignment = TextAlignmentOptions.Center;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;

            var muteBtn = CreateButton(card, "MuteButton", "音声：オン", AccentDay);
            muteBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;
            var muteLabel = muteBtn.GetComponentInChildren<TextMeshProUGUI>();

            var backToTitleBtn = CreateButton(card, "BackToTitleButton", "タイトルへ戻る", new Color(0.50f, 0.45f, 0.62f));
            backToTitleBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;

            var closeBtn = CreateButton(card, "CloseButton", "閉じる", new Color(0.40f, 0.42f, 0.50f));
            closeBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;

            SetRef(so, "settingsPanel", overlayRT.gameObject);
            SetRef(so, "settingsOpenButton", openBtn);
            SetRef(so, "settingsCloseButton", closeBtn);
            SetRef(so, "settingsMuteButton", muteBtn);
            SetRef(so, "settingsMuteButtonLabel", muteLabel);
            SetRef(so, "settingsBackToTitleButton", backToTitleBtn);
        }

        // ================= 演出レイヤー =================

        /// <summary>
        /// フラッシュ・バナー・浮遊テキストの置き場。ShakeRootの外側に置くので、
        /// 画面が揺れている間もフラッシュとバナーだけは画面に固定されて読める。
        /// </summary>
        private static void BuildEffectLayer(Transform canvas, RectTransform shakeRoot, SerializedObject so)
        {
            var layerRT = CreateStretched("EffectLayer", canvas);
            var effects = layerRT.gameObject.AddComponent<UIEffects>();

            var flashGO = new GameObject("FlashOverlay", typeof(Image));
            flashGO.transform.SetParent(layerRT, false);
            var flashImg = flashGO.GetComponent<Image>();
            flashImg.color = new Color(1f, 1f, 1f, 0f);
            flashImg.raycastTarget = false;
            StretchTo(flashGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var banner = CreateLabel(layerRT, "BannerText", "", 84, 0, bold: true);
            banner.alignment = TextAlignmentOptions.Center;
            banner.raycastTarget = false;
            var bannerRT = banner.rectTransform;
            bannerRT.anchorMin = bannerRT.anchorMax = bannerRT.pivot = new Vector2(0.5f, 0.5f);
            bannerRT.sizeDelta = new Vector2(1200, 160);
            bannerRT.anchoredPosition = new Vector2(0, 120);
            banner.gameObject.SetActive(false);

            var soEffects = new SerializedObject(effects);
            SetRef(soEffects, "shakeRoot", shakeRoot);
            SetRef(soEffects, "flashOverlay", flashImg);
            SetRef(soEffects, "effectLayer", layerRT);
            SetRef(soEffects, "bannerText", banner);
            SetRef(soEffects, "circleSprite", CircleSprite);
            SetRef(soEffects, "font", UiFont);
            soEffects.ApplyModifiedProperties();

            SetRef(so, "effects", effects);
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

        // ================= ナビゲーター候補 =================

        /// <summary>
        /// 「3人から1人選べる」の器を用意する。今のところ中身が固まっているのは「ひなた」だけで、
        /// 残り2人はイメージカラーと枠だけのプレースホルダー（セリフ未入力＝共通セリフで動く）。
        /// 既にアセットがある場合は上書きしない（立ち絵を割り当てた後の再構築で消えないようにするため）。
        /// </summary>
        private static void BuildNavigatorPersonas(SerializedObject so)
        {
            var hinata = GetOrCreatePersona("Persona_Hinata", p =>
            {
                p.DisplayName = "ひなた";
                p.Description = "明るく元気なナビゲーター。難しい話も勢いよく噛み砕いて、背中を押してくれる。";
                p.ThemeColor = new Color(0.96f, 0.55f, 0.70f);

                p.LineNormal = "今日も平和だね！ 今のうちに備えちゃお！";
                p.LineNoDefense = "まだ対策ゼロだよ！？ 何かひとつ入れよ、ね？";
                p.LineLowBudget = "お財布ピンチ……！ ここは我慢のしどころだね";
                p.LineLowTrust = "みんなの信頼、下がっちゃってる……雑務もちゃんとやろ！";
                p.LineHighStress = "みんな疲れてるみたい。締めつけすぎは逆に危ないよ？";
                p.LineEndgame = "年度末が近いよ！ ここが踏ん張りどころっ！";
                p.LineGood = "いい感じ！ この調子でいこー！";

                p.LineAttackSevere = "うわっ、これヤバいやつ……！ 落ち着いて、ね！";
                p.LineAttackNew = "見たことない手口だよ！ 慎重にいこ！";
                p.LineAttackNormal = "来た来た！ 大丈夫、いつもの手口だよ！";
                p.LineParry = "タイミング合わせて……いっけー！";

                p.LineWin = "やったー！ さすがだね！";
                p.LineWinNarrow = "あぶなー……！ ギリギリセーフだよ〜";
                p.LineLose = "うぅ……守りきれなかった……ごめん";
                p.LineGameOver = "ごめん……力になれなかったよ……";
                p.LineClear = "1年間おつかれさま！ ほんとに立派だったよ！";
            });

            var aria = GetOrCreatePersona("Persona_Aria", p =>
            {
                p.DisplayName = "アリア";
                p.Description = "（キャラ未確定のプレースホルダー）冷静に数字で語る分析役を想定。セリフ未入力のため共通セリフで話す。";
                p.ThemeColor = new Color(0.45f, 0.72f, 0.95f);
            });

            var chloe = GetOrCreatePersona("Persona_Chloe", p =>
            {
                p.DisplayName = "クロエ";
                p.Description = "（キャラ未確定のプレースホルダー）攻撃者目線で弱点を突く役を想定。セリフ未入力のため共通セリフで話す。";
                p.ThemeColor = new Color(0.64f, 0.46f, 0.88f);
            });

            SetRefArray(so, "personas", new Object[] { hinata, aria, chloe });
        }

        /// <summary>アセットが無ければ作って初期設定を流し込む。既にあればそのまま返す。</summary>
        private static NavigatorPersona GetOrCreatePersona(string assetName, System.Action<NavigatorPersona> configure)
        {
            string path = $"{PersonaDir}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NavigatorPersona>(path);
            if (existing != null) return existing;

            if (!Directory.Exists(PersonaDir)) Directory.CreateDirectory(PersonaDir);

            var persona = ScriptableObject.CreateInstance<NavigatorPersona>();
            configure(persona);
            AssetDatabase.CreateAsset(persona, path);
            return persona;
        }

        // ================= プレハブ =================

        /// <summary>
        /// 対策リストの1行。左にアイコン、中央に名前とレベル、右端に金額を置く。
        /// 参考UIの「アイコン + 2行テキスト + 右寄せ金額」の並びを再現している。
        /// </summary>
        private static GameObject BuildDefenseRowPrefab()
        {
            var go = new GameObject("DefenseRowPrefab", typeof(Image), typeof(Button), typeof(DefenseRowView));
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.145f, 0.155f, 0.195f);
            ApplyRounded(go, ButtonSprite);
            // 8種がサイドバーに収まる高さにしてある（8行 x 58 + 行間 = 約500）
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 58);

            // 左端の縦線（導入済みかどうかの目印）
            var edge = new GameObject("SelectedEdge", typeof(Image));
            edge.transform.SetParent(go.transform, false);
            var edgeImg = edge.GetComponent<Image>();
            edgeImg.raycastTarget = false;
            ApplyRounded(edge, PanelSprite);
            StretchTo(edge.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1), new Vector2(4, 8), new Vector2(8, -8));

            // アイコン（角丸の枠＋内側の図形。対策ごとに色を変える）
            var iconFrame = new GameObject("IconFrame", typeof(Image));
            iconFrame.transform.SetParent(go.transform, false);
            var iconFrameImg = iconFrame.GetComponent<Image>();
            iconFrameImg.raycastTarget = false;
            ApplyRounded(iconFrame, ButtonSprite);
            var iconRT = iconFrame.GetComponent<RectTransform>();
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(0, 0.5f);
            iconRT.pivot = new Vector2(0, 0.5f);
            iconRT.sizeDelta = new Vector2(44, 44);
            iconRT.anchoredPosition = new Vector2(14, 0);

            var glyph = new GameObject("IconGlyph", typeof(Image));
            glyph.transform.SetParent(iconFrame.transform, false);
            var glyphImg = glyph.GetComponent<Image>();
            glyphImg.raycastTarget = false;
            ApplyRounded(glyph, PanelSprite);
            StretchTo(glyph.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(11, 11), new Vector2(-11, -11));

            // 対策名（上段）
            var nameText = CreateLabel(go.transform, "NameText", "対策名", 19, 0, bold: true);
            nameText.alignment = TextAlignmentOptions.BottomLeft;
            StretchTo(nameText.rectTransform, new Vector2(0, 0.45f), new Vector2(1, 1), new Vector2(70, 0), new Vector2(-150, -6));

            // レベル（下段左）
            var levelText = CreateLabel(go.transform, "LevelText", "Lv.0", 15, 0, bold: true);
            levelText.alignment = TextAlignmentOptions.TopLeft;
            StretchTo(levelText.rectTransform, new Vector2(0, 0), new Vector2(1, 0.48f), new Vector2(70, 6), new Vector2(-150, 0));

            // 金額（右端に寄せる）
            var costText = CreateLabel(go.transform, "CostText", "¥0", 17, 0, bold: true);
            costText.alignment = TextAlignmentOptions.Right;
            StretchTo(costText.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-148, 6), new Vector2(-14, -6));

            var button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            ApplyButtonColors(button);
            go.AddComponent<UIButtonPunch>();

            var view = go.GetComponent<DefenseRowView>();
            view.Background = bg;
            view.IconFrame = iconFrameImg;
            view.IconGlyph = glyphImg;
            view.SelectedEdge = edgeImg;
            view.NameText = nameText;
            view.LevelText = levelText;
            view.CostText = costText;

            string path = $"{PrefabDir}/DefenseRowPrefab.prefab";
            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefabAsset;
        }

        /// <summary>
        /// 攻撃への対応選択肢1つ分。左に番号バッジ、右に見出しとコストを置く。
        /// </summary>
        private static GameObject BuildChoiceRowPrefab()
        {
            var go = new GameObject("ChoiceRowPrefab", typeof(Image), typeof(Button), typeof(ChoiceRowView));
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.185f, 0.200f, 0.255f);
            ApplyRounded(go, ButtonSprite);
            AddShadow(go, 3f, 0.35f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 78);

            var badge = new GameObject("NumberBadge", typeof(Image));
            badge.transform.SetParent(go.transform, false);
            var badgeImg = badge.GetComponent<Image>();
            badgeImg.raycastTarget = false;
            ApplyRounded(badge, ButtonSprite);
            var badgeRT = badge.GetComponent<RectTransform>();
            badgeRT.anchorMin = badgeRT.anchorMax = new Vector2(0, 0.5f);
            badgeRT.pivot = new Vector2(0, 0.5f);
            badgeRT.sizeDelta = new Vector2(44, 44);
            badgeRT.anchoredPosition = new Vector2(14, 0);

            var numberText = CreateLabel(badge.transform, "NumberText", "1", 24, 0, bold: true);
            numberText.alignment = TextAlignmentOptions.Center;
            numberText.color = new Color(0.10f, 0.10f, 0.13f);
            StretchTo(numberText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var labelText = CreateLabel(go.transform, "LabelText", "選択肢", 23, 0, bold: true);
            labelText.alignment = TextAlignmentOptions.BottomLeft;
            StretchTo(labelText.rectTransform, new Vector2(0, 0.44f), new Vector2(1, 1), new Vector2(70, 0), new Vector2(-18, -8));

            var detailText = CreateLabel(go.transform, "DetailText", "", 15, 0);
            detailText.color = TextSub;
            detailText.alignment = TextAlignmentOptions.TopLeft;
            StretchTo(detailText.rectTransform, new Vector2(0, 0), new Vector2(1, 0.48f), new Vector2(70, 8), new Vector2(-18, 0));

            var button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            ApplyButtonColors(button);
            go.AddComponent<UIButtonPunch>();

            var view = go.GetComponent<ChoiceRowView>();
            view.Background = bg;
            view.NumberBadge = badgeImg;
            view.NumberText = numberText;
            view.LabelText = labelText;
            view.DetailText = detailText;

            string path = $"{PrefabDir}/ChoiceRowPrefab.prefab";
            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefabAsset;
        }

        private static GameObject BuildButtonPrefab(string name, float height, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            var bgImage = go.GetComponent<Image>();
            bgImage.color = new Color(0.18f, 0.19f, 0.24f);
            ApplyRounded(go, ButtonSprite);
            AddShadow(go, 3f, 0.35f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, height);

            var label = CreateLabel(rt, "Label", "ラベル", fontSize, 0);
            label.alignment = align;
            StretchTo(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 6), new Vector2(-20, -6));

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

        /// <summary>親いっぱいに広がるRectTransformだけのGameObjectを作る。</summary>
        private static RectTransform CreateStretched(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            StretchTo(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rt;
        }

        private static void StretchTo(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

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

        /// <summary>UI標準のShadowコンポーネントでドロップシャドウを付ける。</summary>
        private static void AddShadow(GameObject go, float distance = 4f, float alpha = 0.4f)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(distance, -distance);
        }

        /// <summary>
        /// カード左端に立てる細い色帯。絵文字を使わずに項目の種類を色で示すための手段
        /// （meiryo SDFは絵文字グリフを持たないため、アイコンは常に図形で表現する）。
        /// </summary>
        private static void AddAccentBar(RectTransform parent, Color color)
        {
            var go = new GameObject("Accent", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            ApplyRounded(go, PanelSprite);
            StretchTo(go.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1), new Vector2(10, 12), new Vector2(16, -12));
        }

        /// <summary>パネル左上に、フェーズ名を示す角丸バッジを載せる。</summary>
        private static void AddPhaseTag(RectTransform panelRT, string text, Color accent)
        {
            var go = new GameObject("PhaseTag", typeof(Image));
            go.transform.SetParent(panelRT, false);
            var img = go.GetComponent<Image>();
            img.color = accent;
            img.raycastTarget = false;
            ApplyRounded(go, ButtonSprite);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(text.Length * 17f + 40f, 40f);
            rt.anchoredPosition = new Vector2(24, -20);
            go.AddComponent<LayoutElement>().ignoreLayout = true;

            var label = CreateLabel(go.transform, "Label", text, 18, 0, bold: true);
            label.color = new Color(0.10f, 0.10f, 0.13f);
            label.alignment = TextAlignmentOptions.Center;
            StretchTo(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        /// <summary>フルスクリーン画面の上端を横断する色帯。</summary>
        private static void AddAccentStrip(RectTransform panelRT, Color accent)
        {
            var go = new GameObject("AccentStrip", typeof(Image));
            go.transform.SetParent(panelRT, false);
            var img = go.GetComponent<Image>();
            img.color = accent;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0, 6);
            rt.anchoredPosition = Vector2.zero;
            go.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent, string name, string text, int fontSize, float preferredWidth, bool bold = false)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (UiFont != null) tmp.font = UiFont; // 日本語グリフを持つフォントを明示的に使う
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color = TextMain;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            if (preferredWidth > 0)
                go.AddComponent<LayoutElement>().preferredWidth = preferredWidth;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string labelText, Color accent)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var bgImage = go.GetComponent<Image>();
            bgImage.color = new Color(accent.r * 0.42f, accent.g * 0.42f, accent.b * 0.42f, 1f);
            ApplyRounded(go, ButtonSprite);
            AddShadow(go, 4f, 0.4f);

            // 左端にアクセント色の帯を入れて、ボタンの役割を色でも伝える
            var bar = new GameObject("Accent", typeof(Image));
            bar.transform.SetParent(go.transform, false);
            var barImg = bar.GetComponent<Image>();
            barImg.color = accent;
            barImg.raycastTarget = false;
            ApplyRounded(bar, PanelSprite);
            StretchTo(bar.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1), new Vector2(10, 10), new Vector2(16, -10));

            var tmp = CreateLabel(go.transform, "Label", labelText, 24, 0, bold: true);
            tmp.alignment = TextAlignmentOptions.Center;
            StretchTo(tmp.rectTransform, Vector2.zero, Vector2.one, new Vector2(24, 0), new Vector2(-16, 0));

            var button = go.GetComponent<Button>();
            button.targetGraphic = bgImage;
            ApplyButtonColors(button);
            go.AddComponent<UIButtonPunch>();

            return button;
        }

        /// <summary>
        /// ボタンの状態色。ColorTintは元のImage色に乗算されるため、
        /// 通常時をやや暗めにしておき、ホバーで白（＝元色そのまま）まで明るくなるようにしている
        /// （1を超える色は結局クランプされるので、明るくする余地は通常時側で確保する）。
        /// </summary>
        private static void ApplyButtonColors(Button button)
        {
            var colors = button.colors;
            colors.normalColor = new Color(0.86f, 0.87f, 0.90f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.62f, 0.63f, 0.68f);
            colors.selectedColor = new Color(0.86f, 0.87f, 0.90f);
            colors.disabledColor = new Color(0.40f, 0.40f, 0.44f, 0.5f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        private static void SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[SceneBuilder] フィールド '{fieldName}' が見つかりませんでした。");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static void SetRefArray(SerializedObject so, string fieldName, Object[] values)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null || !prop.isArray)
            {
                Debug.LogWarning($"[SceneBuilder] 配列フィールド '{fieldName}' が見つかりませんでした。");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
