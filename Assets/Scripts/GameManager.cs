using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PatchWorkSecure
{
    /// <summary>
    /// ゲーム全体の進行を管理し、GameStateの状態をUIに反映するクラス。
    /// GameState（純粋C#）がルールを持ち、このクラスは「見せ方」だけを担当する。
    ///
    /// UI要素の生成とInspector参照の割当は Assets/Editor/SceneBuilder.cs が自動で行うため、
    /// 新しいフィールドを足したら必ずSceneBuilder側にも生成＋SetRef()を追加すること。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // パリィ判定のしきい値。SceneBuilder側のスイートゾーン表示もこの値を参照する。
        public const float ParryPerfectThreshold = 0.85f;
        public const float ParryGoodThreshold = 0.5f;

        // 被害予測バーの上限。これを超えると「危険」表示になる（EstimateExpectedDamageの実測レンジに合わせた値）。
        private const float RiskBarMax = 18f;

        // ---- ステータス表示 ----
        [Header("ステータスバー")]
        [SerializeField] private TextMeshProUGUI periodLabel;   // 「4月上旬」
        [SerializeField] private TextMeshProUGUI turnLabel;     // 「ターン 1 / 36」
        [SerializeField] private TextMeshProUGUI budgetText;
        [SerializeField] private TextMeshProUGUI trustText;
        [SerializeField] private TextMeshProUGUI stressText;
        [SerializeField] private Image budgetBar;
        [SerializeField] private Image trustBar;
        [SerializeField] private Image stressBar;
        [SerializeField] private RectTransform budgetChip;      // 増減演出の発生位置
        [SerializeField] private RectTransform trustChip;
        [SerializeField] private RectTransform stressChip;

        // ---- ナビゲーターキャラ ----
        [Header("ナビゲーター")]
        [SerializeField] private Image navigatorPortrait;
        [SerializeField] private GameObject portraitPlaceholder;  // 立ち絵が無い間に出す簡易キャラ
        [SerializeField] private Image[] placeholderHairImages;   // プレースホルダーの髪（イメージカラーで着色）
        [SerializeField] private Image portraitFrame;             // 立ち絵の背後の枠（イメージカラーで着色）
        [SerializeField] private GameObject speechBubble;
        [SerializeField] private TextMeshProUGUI navigatorLine;
        [SerializeField] private TextMeshProUGUI navigatorNameText;
        [SerializeField] private Image navigatorNameChip;         // 名前チップ（イメージカラーで着色）
        [SerializeField] private Image speechBubbleAccent;        // 吹き出しの縁（イメージカラーで着色）

        [Header("ナビゲーター候補（3人から1人を選ぶ想定）")]
        [SerializeField] private NavigatorPersona[] personas;

        [Header("キャラ選択（タイトル画面）")]
        [SerializeField] private Transform personaSelectContainer;
        [SerializeField] private GameObject personaSelectButtonPrefab;

        // ---- サイドバー（常時表示） ----
        [Header("サイドバー：対策リスト")]
        [SerializeField] private Transform defenseButtonContainer;
        [SerializeField] private GameObject defenseButtonPrefab;

        [Header("サイドバー：リスク表示")]
        [SerializeField] private TextMeshProUGUI riskLevelText;
        [SerializeField] private TextMeshProUGUI riskDamageText;
        [SerializeField] private Image riskBar;

        [Header("サイドバー：ログ")]
        [SerializeField] private TextMeshProUGUI logText;

        // ---- メインパネル ----
        [Header("メインパネル")]
        [SerializeField] private GameObject dayPanel;
        [SerializeField] private GameObject chorePanel;
        [SerializeField] private GameObject attackPanel;
        [SerializeField] private GameObject parryPanel;
        [SerializeField] private GameObject resultPanel;

        [Header("タイトル画面")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private Button startButton;

        [Header("クイズ画面（事前・事後クイズ共通で使い回す）")]
        [SerializeField] private GameObject quizPanel;
        [SerializeField] private TextMeshProUGUI quizProgressText;
        [SerializeField] private TextMeshProUGUI quizQuestionText;
        [SerializeField] private Transform quizOptionContainer;
        [SerializeField] private GameObject quizOptionButtonPrefab;

        [Header("エンディング画面（ゲームオーバー/クリア共通）")]
        [SerializeField] private GameObject endingPanel;
        [SerializeField] private TextMeshProUGUI endingText;
        [SerializeField] private TextMeshProUGUI endingCharacterLine;
        [SerializeField] private Button endingContinueButton; // 事後クイズへ進む

        [Header("結果サマリー画面（事後クイズ後）")]
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private Button summaryCloseButton; // タイトルに戻る

        [Header("設定オーバーレイ")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button settingsOpenButton;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private Button settingsMuteButton;
        [SerializeField] private TextMeshProUGUI settingsMuteButtonLabel;
        [SerializeField] private Button settingsBackToTitleButton;

        [Header("演出")]
        [SerializeField] private UIEffects effects;

        [Header("操作ボタン")]
        [SerializeField] private Button proceedButton;       // 「今日の業務を進める」
        [SerializeField] private Button solveChoreButton;     // 雑務：誠実に対応する
        [SerializeField] private Button postponeChoreButton;  // 雑務：後回しにする
        [SerializeField] private Button parryButton;          // 「ここだ！」
        [SerializeField] private Button nextDayButton;        // 「次の日へ」

        [Header("雑務パネル")]
        [SerializeField] private TextMeshProUGUI choreText;

        [Header("攻撃パネル")]
        [SerializeField] private TextMeshProUGUI attackNameText;
        [SerializeField] private TextMeshProUGUI attackGradeText;
        [SerializeField] private Image attackGradeChip;
        [SerializeField] private TextMeshProUGUI attackIntroLine;
        [SerializeField] private TextMeshProUGUI scTermText;
        [SerializeField] private Transform choiceButtonContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("パリィパネル")]
        [SerializeField] private RectTransform parryMarker;
        [SerializeField] private RectTransform parryTrack;
        [SerializeField] private float parrySpeed = 250f;
        [SerializeField] private TextMeshProUGUI parryFeedbackText; // PERFECT!!/GOOD!/MISS...の表示

        [Header("結果パネル")]
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI resultCharacterLine;

        // ---- 内部状態 ----
        private GameState _state;
        private string _currentAttackKey;
        private AttackChoice _pendingChoice;
        private float _parryPosition;   // 0〜1
        private int _parryDirection = 1;
        private bool _parryActive;
        private float _currentParrySpeed;
        private bool _isDayPhase;       // 対策強化を受け付けてよいフェーズか

        // 前回表示時の数値（増減演出の差分計算に使う）
        private int _prevBudget, _prevTrust, _prevStress;

        // クイズ進行状態（事前/事後で使い回す）
        private List<QuizQuestion> _quizQueue;
        private int _quizIndex;
        private int _quizCorrectCount;
        private int _quizTotalCount;
        private bool _isPreQuiz;
        private bool _quizAnswering;    // 正誤演出中の二重回答を防ぐ
        private int _preCorrect, _preTotal, _postCorrect, _postTotal;

        // 同じ対象に複数のアニメーションが重ならないよう、実行中のコルーチンを覚えておく
        private readonly Dictionary<TextMeshProUGUI, Coroutine> _numberRoutines = new Dictionary<TextMeshProUGUI, Coroutine>();
        private readonly Dictionary<Image, Coroutine> _barRoutines = new Dictionary<Image, Coroutine>();
        private Coroutine _typeRoutine;
        private string _currentSpokenLine;

        private const string PersonaPrefKey = "pws_selected_persona_index";
        private NavigatorPersona _activePersona;

        // 雑務のマスターデータ（後でScriptableObject化しても良い）
        private readonly (string text, int trustGain)[] _chores =
        {
            ("プリンターに繋がらないと問い合わせが来た", 4),
            ("社員がパスワードを忘れて困っている", 3),
            ("会議室のWi-Fiが不安定という報告", 4),
            ("「このメール怪しいですか？」と相談が来た", 6),
        };
        private (string text, int trustGain) _currentChore;

        private void Start()
        {
            WireButtons();
            LoadActivePersona();
            BuildPersonaSelectButtons();
            ShowTitle();
        }

        private void Update()
        {
            if (_parryActive) UpdateParryMarker();
        }

        // ================= ナビゲーター =================

        private void LoadActivePersona()
        {
            if (personas == null || personas.Length == 0) { _activePersona = null; return; }
            int idx = Mathf.Clamp(PlayerPrefs.GetInt(PersonaPrefKey, 0), 0, personas.Length - 1);
            _activePersona = personas[idx];
            ApplyPersonaTheme();
        }

        /// <summary>タイトル画面のキャラ選択ボタンを動的に生成する（BuildChoiceButtonsと同じパターン）。</summary>
        private void BuildPersonaSelectButtons()
        {
            if (personaSelectContainer == null || personaSelectButtonPrefab == null || personas == null) return;

            foreach (Transform child in personaSelectContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < personas.Length; i++)
            {
                int idx = i; // クロージャ対策
                var go = Instantiate(personaSelectButtonPrefab, personaSelectContainer);
                var persona = personas[idx];

                // 横並びのコンテナは子のサイズを制御しない設定なので、ここで明示的に整える
                var rect = go.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(240f, 54f);

                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = persona != null ? persona.DisplayName : "（未設定）";

                // 選択中のキャラだけイメージカラーで塗り、どれを選んでいるか一目で分かるようにする。
                var image = go.GetComponent<Image>();
                if (image != null && persona != null)
                    image.color = persona == _activePersona ? persona.ThemeColor : new Color(0.22f, 0.24f, 0.30f);

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        AudioManager.Instance?.PlayClick();
                        SelectPersona(idx);
                    });
                }
            }
        }

        private void SelectPersona(int index)
        {
            if (personas == null || index < 0 || index >= personas.Length) return;
            _activePersona = personas[index];
            PlayerPrefs.SetInt(PersonaPrefKey, index);
            PlayerPrefs.Save();

            ApplyPersonaTheme();
            BuildPersonaSelectButtons(); // 選択中の強調表示を更新
            UpdateNavigator(force: true);
            if (effects != null && navigatorNameChip != null)
                effects.Punch(navigatorNameChip.rectTransform, 1.3f);
        }

        /// <summary>選択中キャラのイメージカラーを、名前チップ・吹き出しの縁・立ち絵枠に反映する。</summary>
        private void ApplyPersonaTheme()
        {
            Color theme = _activePersona != null ? _activePersona.ThemeColor : new Color(0.5f, 0.5f, 0.6f);

            if (navigatorNameText != null)
                navigatorNameText.text = _activePersona != null ? _activePersona.DisplayName : "";
            if (navigatorNameChip != null) navigatorNameChip.color = theme;
            if (speechBubbleAccent != null) speechBubbleAccent.color = theme;
            if (portraitFrame != null) portraitFrame.color = new Color(theme.r * 0.35f, theme.g * 0.35f, theme.b * 0.35f, 1f);

            // プレースホルダーの髪を選択中キャラの色に塗り替える（誰を選んだか一目で分かるように）
            if (placeholderHairImages != null)
            {
                foreach (var hair in placeholderHairImages)
                    if (hair != null) hair.color = theme;
            }
        }

        /// <summary>
        /// 表情とセリフを差し替える。セリフは1文字ずつ表示して「しゃべっている」感を出す。
        /// 同じセリフのままなら打ち直さない（RefreshUIから頻繁に呼ばれるため）。
        /// </summary>
        private void Speak(Sprite face, string line, bool force = false)
        {
            ApplyFace(face);
            if (speechBubble != null && !speechBubble.activeSelf) speechBubble.SetActive(true);
            if (navigatorLine == null) return;
            if (!force && line == _currentSpokenLine) return;

            _currentSpokenLine = line;
            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            _typeRoutine = StartCoroutine(Typewriter(navigatorLine, line));
        }

        private IEnumerator Typewriter(TextMeshProUGUI label, string text, float charsPerSecond = 42f)
        {
            label.text = text;
            label.ForceMeshUpdate();
            int total = label.textInfo.characterCount;

            label.maxVisibleCharacters = 0;
            float shown = 0f;
            while (shown < total)
            {
                shown += Time.deltaTime * charsPerSecond;
                label.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                yield return null;
            }
            label.maxVisibleCharacters = total;
            _typeRoutine = null;
        }

        /// <summary>
        /// 立ち絵を差し替える。素材が未実装（スプライトがnull）の間は、
        /// 組み込みスプライトで組んだ簡易キャラをプレースホルダーとして表示する。
        /// </summary>
        private void ApplyFace(Sprite face)
        {
            bool hasArt = face != null;

            if (navigatorPortrait != null)
            {
                navigatorPortrait.sprite = face;
                navigatorPortrait.color = Color.white;
                navigatorPortrait.enabled = hasArt;
            }
            if (portraitPlaceholder != null && portraitPlaceholder.activeSelf != !hasArt)
                portraitPlaceholder.SetActive(!hasArt);
        }

        /// <summary>キャラ固有のセリフを使う。未入力なら共通のセリフにフォールバックする。</summary>
        private static string Pick(string personaLine, string fallback)
            => string.IsNullOrWhiteSpace(personaLine) ? fallback : personaLine;

        /// <summary>状況に応じてナビゲーターの表情とセリフを変える。</summary>
        private void UpdateNavigator(bool force = false)
        {
            if (navigatorPortrait == null || navigatorLine == null) return;
            var p = _activePersona;

            // タイトル画面（キャラ選択時点）ではまだ_stateが無いため、状態を見ないセリフだけを使う。
            if (_state == null)
            {
                Speak(p?.FaceNormal, Pick(p?.LineNormal, "よろしくお願いします。一緒に会社を守りましょう。"), force);
                return;
            }

            if (attackPanel != null && attackPanel.activeSelf)
            {
                var grade = GameData.Attacks[_currentAttackKey].Grade;
                if (grade == AttackGrade.S)
                    Speak(p?.FaceAlert, Pick(p?.LineAttackSevere, "これ、一番まずいやつです……対策、足りてますか？"), force);
                else if (grade == AttackGrade.Rookie || grade == AttackGrade.Rising)
                    Speak(p?.FaceWorried, Pick(p?.LineAttackNew, "見たことない手口です。慎重にいきましょう。"), force);
                else
                    Speak(p?.FaceWorried, Pick(p?.LineAttackNormal, "来ましたね。落ち着いて対応しましょう。"), force);
                return;
            }

            if (parryPanel != null && parryPanel.activeSelf)
            {
                Speak(p?.FaceAlert, Pick(p?.LineParry, "タイミング、いきますよ……！"), force);
                return;
            }

            if (_state.Stress > 65)
                Speak(p?.FaceWorried, Pick(p?.LineHighStress, "みんな疲れてます。締めすぎも危険ですよ。"), force);
            else if (_state.Trust < 20)
                Speak(p?.FaceWorried, Pick(p?.LineLowTrust, "社内での信頼が薄いです。雑務対応、大事ですよ。"), force);
            else if (_state.Budget < 30)
                Speak(p?.FaceWorried, Pick(p?.LineLowBudget, "予算が心もとないですね。慎重に使いましょう。"), force);
            else if (_state.DefenseLevels.Count == 0)
                Speak(p?.FaceWorried, Pick(p?.LineNoDefense, "まだ何も対策がありません。何か入れませんか？"), force);
            else if (_state.Day > GameState.TotalPeriods * 0.7f)
                Speak(p?.FaceAlert, Pick(p?.LineEndgame, "年度末が近いです。攻撃も激しくなってきました。"), force);
            else if (_state.Trust > 60)
                Speak(p?.FaceProud, Pick(p?.LineGood, "社内の空気、いい感じですね。"), force);
            else
                Speak(p?.FaceNormal, Pick(p?.LineNormal, "今日も平穏です。備えを進めましょうか。"), force);
        }

        private void UpdateNavigatorForResult(AttackResult result)
        {
            var p = _activePersona;
            if (result.Defended)
            {
                bool narrow = result.Flavor != null && result.Flavor.Contains("紙一重");
                if (narrow) Speak(p?.FaceRelieved, Pick(p?.LineWinNarrow, "あぶなかった……！ でも、守りきりました。"), force: true);
                else Speak(p?.FaceProud, Pick(p?.LineWin, "危なげなかったですね。備えの成果です。"), force: true);
            }
            else
            {
                Speak(p?.FaceSad, Pick(p?.LineLose, "……やられました。次はもっと備えましょう。"), force: true);
            }
        }

        // ================= ボタン配線 =================

        /// <summary>
        /// ボタンのクリックイベントをコードから配線する。
        /// InspectorのOnClick()を手動設定する代わりに、ここでまとめて済ませる。
        /// </summary>
        private void WireButtons()
        {
            AddClickListener(proceedButton, OnClickProceedDay);
            AddClickListener(solveChoreButton, () => OnClickResolveChore(true));
            AddClickListener(postponeChoreButton, () => OnClickResolveChore(false));
            AddClickListener(parryButton, OnClickParry);
            AddClickListener(nextDayButton, OnClickNextDay);
            AddClickListener(startButton, OnClickStartGame);
            AddClickListener(endingContinueButton, () => BeginQuiz(false));
            AddClickListener(summaryCloseButton, ShowTitle);
            AddClickListener(settingsOpenButton, () => ToggleSettingsPanel(true));
            AddClickListener(settingsCloseButton, () => ToggleSettingsPanel(false));
            AddClickListener(settingsMuteButton, OnClickToggleMute);
            AddClickListener(settingsBackToTitleButton, () => { ToggleSettingsPanel(false); ShowTitle(); });
        }

        /// <summary>クリックSEを鳴らしてからactionを実行するリスナーを登録する。buttonがnullなら何もしない。</summary>
        private void AddClickListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayClick();
                action();
            });
        }

        // ================= フェーズ制御 =================

        private void HideAllPanels()
        {
            _isDayPhase = false;
            if (titlePanel != null) titlePanel.SetActive(false);
            if (dayPanel != null) dayPanel.SetActive(false);
            if (chorePanel != null) chorePanel.SetActive(false);
            if (attackPanel != null) attackPanel.SetActive(false);
            if (parryPanel != null) parryPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (quizPanel != null) quizPanel.SetActive(false);
            if (endingPanel != null) endingPanel.SetActive(false);
            if (summaryPanel != null) summaryPanel.SetActive(false);
        }

        /// <summary>
        /// パネルを下からせり上げつつフェードインさせる。
        /// 「切り替わった瞬間にパッと出る」冷たさを減らすための演出。
        /// </summary>
        private IEnumerator FadeInPanel(GameObject panel, float duration = 0.25f)
        {
            if (panel == null) yield break;
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            var rt = panel.GetComponent<RectTransform>();
            Vector2 home = rt != null ? rt.anchoredPosition : Vector2.zero;

            cg.alpha = 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                cg.alpha = p;
                if (rt != null) rt.anchoredPosition = home + new Vector2(0, Mathf.Lerp(24f, 0f, p * p));
                yield return null;
            }
            cg.alpha = 1f;
            if (rt != null) rt.anchoredPosition = home;
        }

        private void ShowTitle()
        {
            HideAllPanels();
            titlePanel.SetActive(true);
            StartCoroutine(FadeInPanel(titlePanel));
            AudioManager.Instance?.PlayBgmTitle();
            UpdateNavigator(force: true);
        }

        /// <summary>タイトル画面の「はじめる」ボタンから呼ぶ。事前クイズへ進む。</summary>
        public void OnClickStartGame()
        {
            BeginQuiz(isPre: true);
        }

        private void ShowDayPhase()
        {
            HideAllPanels();
            _isDayPhase = true;
            dayPanel.SetActive(true);
            StartCoroutine(FadeInPanel(dayPanel));
            BuildDefensePanel();
            UpdateNavigator();
            AudioManager.Instance?.PlayBgmDay();
        }

        /// <summary>「今日の業務を進める」ボタンから呼ぶ。</summary>
        public void OnClickProceedDay()
        {
            _currentChore = _chores[Random.Range(0, _chores.Length)];
            HideAllPanels();
            chorePanel.SetActive(true);
            StartCoroutine(FadeInPanel(chorePanel));
            choreText.text = _currentChore.text;
            BuildDefensePanel(); // 日常フェーズを抜けたので購入不可の見た目に更新する
            UpdateNavigator();
        }

        /// <summary>雑務への対応ボタンから呼ぶ（誠実に対応=true / 後回し=false）。</summary>
        public void OnClickResolveChore(bool solved)
        {
            _state.ResolveChore(solved, _currentChore.trustGain);
            AudioManager.Instance?.PlayChoreSolve();
            RefreshUI();

            if (_state.IsGameOver)
            {
                ShowGameOver();
                return;
            }
            StartCoroutine(TransitionToAttackCheck());
        }

        private IEnumerator TransitionToAttackCheck()
        {
            HideAllPanels();
            yield return new WaitForSeconds(0.45f); // 増減演出を見せてから次へ

            if (_state.RollAttackOccurrence())
            {
                _currentAttackKey = _state.RollAttackType();
                ShowAttackPhase();
            }
            else
            {
                _state.AdvanceDay();
                if (_state.IsCleared) ShowClear();
                else { ShowDayPhase(); RefreshUI(); }
            }
        }

        private void ShowAttackPhase()
        {
            HideAllPanels();
            attackPanel.SetActive(true);
            StartCoroutine(FadeInPanel(attackPanel));
            AudioManager.Instance?.PlayBgmTension();
            AudioManager.Instance?.PlayAttackAppear();

            var attack = GameData.Attacks[_currentAttackKey];
            attackNameText.text = attack.DisplayName;

            bool hasGrade = attack.Grade != AttackGrade.None;
            attackGradeText.text = hasGrade ? GradeLabel(attack.Grade) : "";
            if (attackGradeChip != null)
            {
                attackGradeChip.gameObject.SetActive(hasGrade);
                attackGradeChip.color = GradeColor(attack.Grade);
            }

            attackIntroLine.text = $"「{attack.LineIntro}」";
            scTermText.text = $"{attack.ScTerm} — {attack.ScNote}";

            // 攻撃の登場は毎回「来た」と分かるように、赤い明滅と軽い揺れで知らせる。
            if (effects != null)
            {
                effects.Flash(UIEffects.Bad, attack.Grade == AttackGrade.S ? 0.34f : 0.20f, 0.45f);
                effects.Shake(0.3f, attack.Grade == AttackGrade.S ? 16f : 9f);
                if (attackNameText != null) effects.Punch(attackNameText.rectTransform, 1.5f, 0.35f);
            }

            BuildChoiceButtons();
            UpdateNavigator();
        }

        private static string GradeLabel(AttackGrade grade)
        {
            switch (grade)
            {
                case AttackGrade.S: return "S級";
                case AttackGrade.Veteran: return "古参";
                case AttackGrade.MidTier: return "中堅";
                case AttackGrade.Rising: return "新興";
                case AttackGrade.Rookie: return "新人";
                default: return "";
            }
        }

        /// <summary>格付けごとの色。危険なほど赤に寄せる。</summary>
        private static Color GradeColor(AttackGrade grade)
        {
            switch (grade)
            {
                case AttackGrade.S: return new Color(0.85f, 0.20f, 0.25f);
                case AttackGrade.Veteran: return new Color(0.75f, 0.40f, 0.25f);
                case AttackGrade.MidTier: return new Color(0.70f, 0.55f, 0.25f);
                case AttackGrade.Rising: return new Color(0.45f, 0.55f, 0.70f);
                case AttackGrade.Rookie: return new Color(0.45f, 0.65f, 0.55f);
                default: return new Color(0.45f, 0.45f, 0.50f);
            }
        }

        /// <summary>対応選択肢のボタンを動的に生成する。</summary>
        private void BuildChoiceButtons()
        {
            foreach (Transform child in choiceButtonContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < GameData.Choices.Count; i++)
            {
                var choice = GameData.Choices[i];
                var go = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                bool affordable = _state.Budget >= choice.BudgetCost;

                var view = go.GetComponent<ChoiceRowView>();
                if (view != null)
                {
                    if (view.NumberText != null) view.NumberText.text = (i + 1).ToString();
                    if (view.NumberBadge != null)
                        view.NumberBadge.color = affordable ? ChoiceBadgeColor(choice.Id) : new Color(0.4f, 0.4f, 0.44f);
                    if (view.LabelText != null) view.LabelText.text = choice.Label;
                    if (view.DetailText != null)
                    {
                        string cost = choice.BudgetCost > 0 ? $"費用 {Money.Yen(choice.BudgetCost)}" : "費用なし";
                        string stress = choice.StressCost >= 0
                            ? $"ストレス +{choice.StressCost}"
                            : $"ストレス {choice.StressCost}";
                        view.DetailText.text = $"{choice.Description}　{cost} / {stress}";
                    }
                }

                var button = go.GetComponent<Button>();
                var captured = choice; // クロージャ対策
                button.interactable = affordable;
                button.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayClick();
                    OnSelectChoice(captured);
                });
            }
        }

        /// <summary>対応方針ごとの番号バッジ色。積極的な対応ほど緑に寄せる。</summary>
        private static Color ChoiceBadgeColor(string choiceId)
        {
            switch (choiceId)
            {
                case "block": return new Color(0.45f, 0.80f, 0.50f);
                case "investigate": return new Color(0.40f, 0.62f, 0.92f);
                default: return new Color(0.72f, 0.72f, 0.78f);
            }
        }

        /// <summary>
        /// 対策強化ボタンを動的に生成する。サイドバーに常時表示され、
        /// 日常フェーズ以外では「見えるが押せない」状態になる（今の備えは常に確認できる）。
        /// </summary>
        private void BuildDefensePanel()
        {
            if (defenseButtonContainer == null || defenseButtonPrefab == null || _state == null) return;

            foreach (Transform child in defenseButtonContainer)
                Destroy(child.gameObject);

            foreach (var kv in GameData.Defenses)
            {
                string key = kv.Key;
                var def = kv.Value;
                int currentLvl = _state.DefenseLevels.TryGetValue(key, out int lv) ? lv : 0;
                int maxLvl = def.Levels.Count;
                bool maxed = currentLvl >= maxLvl;

                var go = Instantiate(defenseButtonPrefab, defenseButtonContainer);
                var button = go.GetComponent<Button>();

                bool affordable = false;
                string rightText;
                if (maxed)
                {
                    rightText = "最大Lv";
                }
                else
                {
                    var next = def.Levels[currentLvl];
                    affordable = _state.Budget >= next.Cost;
                    rightText = Money.Yen(next.Cost);
                }

                var view = go.GetComponent<DefenseRowView>();
                if (view != null)
                {
                    Color iconColor = DefenseIconColor(key);
                    bool installed = currentLvl > 0;

                    if (view.NameText != null) view.NameText.text = def.DisplayName;

                    // レベルは導入済みなら緑、未導入は灰色。参考UIの「Lv.2が緑」の見せ方に合わせる
                    if (view.LevelText != null)
                    {
                        view.LevelText.text = $"Lv.{currentLvl}";
                        view.LevelText.color = installed ? new Color(0.50f, 0.82f, 0.52f) : new Color(0.52f, 0.54f, 0.60f);
                    }

                    if (view.CostText != null)
                    {
                        view.CostText.text = rightText;
                        view.CostText.color = maxed
                            ? new Color(0.50f, 0.82f, 0.52f)
                            : (affordable ? new Color(0.92f, 0.93f, 0.96f) : new Color(0.48f, 0.48f, 0.54f));
                    }

                    // アイコンは対策ごとに色を変える。導入前はくすませて、入れると鮮やかになる
                    if (view.IconFrame != null)
                        view.IconFrame.color = installed ? iconColor : new Color(iconColor.r * 0.35f, iconColor.g * 0.35f, iconColor.b * 0.35f);
                    if (view.IconGlyph != null)
                        view.IconGlyph.color = installed ? new Color(1f, 1f, 1f, 0.92f) : new Color(1f, 1f, 1f, 0.35f);
                    if (view.SelectedEdge != null)
                        view.SelectedEdge.color = installed ? iconColor : new Color(1f, 1f, 1f, 0.06f);
                    if (view.Background != null)
                        view.Background.color = installed ? new Color(0.165f, 0.190f, 0.205f) : new Color(0.140f, 0.150f, 0.190f);
                }

                if (button != null)
                {
                    button.interactable = _isDayPhase && !maxed && affordable;
                    string capturedKey = key;
                    var capturedRect = go.GetComponent<RectTransform>();
                    button.onClick.AddListener(() =>
                    {
                        AudioManager.Instance?.PlayClick();
                        OnClickUpgradeDefense(capturedKey, capturedRect);
                    });
                }
            }
        }

        /// <summary>
        /// 対策ごとのアイコン色。専用のアイコン画像が用意できるまでは、
        /// 色そのものを識別子として使う（8種を色で見分けられるようにする）。
        /// </summary>
        private static Color DefenseIconColor(string defenseKey)
        {
            switch (defenseKey)
            {
                case "firewall": return new Color(0.86f, 0.42f, 0.32f); // レンガの赤
                case "mfa": return new Color(0.38f, 0.62f, 0.92f); // 端末の青
                case "training": return new Color(0.92f, 0.76f, 0.36f); // 教本の黄
                case "waf": return new Color(0.36f, 0.74f, 0.72f); // Webの水色
                case "idsIps": return new Color(0.66f, 0.48f, 0.88f); // 監視の紫
                case "backup": return new Color(0.55f, 0.62f, 0.72f); // 記憶装置の灰青
                case "vpn": return new Color(0.42f, 0.78f, 0.52f); // 経路の緑
                case "passwordPolicy": return new Color(0.90f, 0.52f, 0.66f); // 認証の桃
                default: return new Color(0.60f, 0.62f, 0.68f);
            }
        }

        private void OnSelectChoice(AttackChoice choice)
        {
            _pendingChoice = choice;
            HideAllPanels();
            parryPanel.SetActive(true);
            StartCoroutine(FadeInPanel(parryPanel));
            _parryPosition = 0f;
            _parryDirection = 1;
            _currentParrySpeed = parrySpeed * ParrySpeedMultiplier(GameData.Attacks[_currentAttackKey].Grade);
            _parryActive = true;
            if (parryFeedbackText != null) parryFeedbackText.text = "";
            UpdateNavigator();
        }

        // ================= パリィ =================

        /// <summary>攻撃の格が高いほどマーカーを速くし、パリィの難度を上げる。</summary>
        private static float ParrySpeedMultiplier(AttackGrade grade)
        {
            switch (grade)
            {
                case AttackGrade.S: return 1.4f;
                case AttackGrade.Veteran: return 1.2f;
                case AttackGrade.Rising: return 1.1f;
                case AttackGrade.Rookie: return 0.9f;
                default: return 1.0f;
            }
        }

        private void UpdateParryMarker()
        {
            _parryPosition += _parryDirection * (_currentParrySpeed / 1000f) * Time.deltaTime;
            if (_parryPosition >= 1f) { _parryPosition = 1f; _parryDirection = -1; }
            if (_parryPosition <= 0f) { _parryPosition = 0f; _parryDirection = 1; }

            if (parryMarker != null && parryTrack != null)
            {
                float width = parryTrack.rect.width;
                parryMarker.anchoredPosition = new Vector2(_parryPosition * width - width / 2f, 0f);
            }
        }

        /// <summary>「ここだ！」ボタンから呼ぶ。</summary>
        public void OnClickParry()
        {
            if (!_parryActive) return;
            _parryActive = false;

            // 中央(0.5)に近いほど高ボーナス。最大+0.15
            float distanceFromCenter = Mathf.Abs(0.5f - _parryPosition);
            float quality = Mathf.Clamp01(1f - distanceFromCenter / 0.5f);
            float parryBonus = quality * 0.15f;

            ShowParryFeedback(quality);
            StartCoroutine(PunchMarker());
            StartCoroutine(ResolveWithSuspense(parryBonus));
        }

        /// <summary>判定の質に応じてPERFECT!!/GOOD!/MISS...を表示し、SEと演出を合わせる。</summary>
        private void ShowParryFeedback(float quality)
        {
            if (parryFeedbackText == null) return;

            if (quality >= ParryPerfectThreshold)
            {
                parryFeedbackText.text = "PERFECT!!";
                parryFeedbackText.color = UIEffects.Gold;
                AudioManager.Instance?.PlayParryPerfect();
                if (effects != null)
                {
                    effects.Flash(UIEffects.Gold, 0.22f, 0.3f);
                    effects.Burst(parryMarker, UIEffects.Gold, 380f);
                    effects.Punch(parryFeedbackText.rectTransform, 1.9f, 0.35f);
                }
            }
            else if (quality >= ParryGoodThreshold)
            {
                parryFeedbackText.text = "GOOD!";
                parryFeedbackText.color = UIEffects.Good;
                AudioManager.Instance?.PlayParryGood();
                if (effects != null)
                {
                    effects.Burst(parryMarker, UIEffects.Good, 260f);
                    effects.Punch(parryFeedbackText.rectTransform, 1.5f, 0.3f);
                }
            }
            else
            {
                parryFeedbackText.text = "MISS...";
                parryFeedbackText.color = new Color(0.78f, 0.78f, 0.8f);
                AudioManager.Instance?.PlayParryMiss();
                if (effects != null) effects.Shake(0.2f, 7f);
            }
        }

        /// <summary>マーカーを一瞬だけ拡大させて、パリィ入力の手応えを出す。</summary>
        private IEnumerator PunchMarker()
        {
            if (parryMarker == null) yield break;
            const float duration = 0.25f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(1.8f, 1f, t / duration);
                parryMarker.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            parryMarker.localScale = Vector3.one;
        }

        /// <summary>結果をすぐ出さず、一瞬の「溜め」を挟んでから開示する。</summary>
        private IEnumerator ResolveWithSuspense(float parryBonus)
        {
            yield return new WaitForSeconds(0.55f); // パリィ判定(PERFECT!等)を見せる間
            HideAllPanels();
            yield return new WaitForSeconds(0.6f);  // 結果発表前の溜め

            var result = _state.ResolveAttack(_currentAttackKey, _pendingChoice, parryBonus);

            if (result.Defended)
            {
                AudioManager.Instance?.PlayDefendSuccess();
                bool narrow = result.Flavor != null && result.Flavor.Contains("紙一重");
                if (effects != null)
                {
                    effects.Flash(narrow ? UIEffects.Good : UIEffects.Gold, 0.24f, 0.45f);
                    effects.Banner(narrow ? "ぎりぎり防御！" : "防御成功！", narrow ? UIEffects.Good : UIEffects.Gold);
                }
            }
            else
            {
                AudioManager.Instance?.PlayDefendFail();
                if (effects != null)
                {
                    effects.Flash(UIEffects.Bad, 0.45f, 0.55f);
                    effects.Shake(0.5f, 26f);
                    effects.Banner("被弾！", UIEffects.Bad);
                }
            }

            yield return new WaitForSeconds(0.35f); // バナーを一瞬見せてから数値を動かす
            RefreshUI();

            if (_state.IsGameOver)
            {
                yield return new WaitForSeconds(0.6f);
                ShowGameOver();
                yield break;
            }

            resultPanel.SetActive(true);
            StartCoroutine(FadeInPanel(resultPanel));
            resultText.color = result.Defended ? UIEffects.Good : UIEffects.Bad;
            resultText.text = result.Defended
                ? $"{result.Flavor}（防御率 {Mathf.RoundToInt(result.FinalDefenseRate * 100)}%）"
                : $"{result.Flavor}（被害 {Money.Yen(result.BudgetDamage)} / 人望 -{result.TrustDamage}）";
            resultCharacterLine.text = $"「{result.CharacterLine}」";
            if (effects != null) effects.Punch(resultText.rectTransform, 1.25f);

            UpdateNavigatorForResult(result);
        }

        /// <summary>「次の日へ」ボタンから呼ぶ。</summary>
        public void OnClickNextDay()
        {
            _state.AdvanceDay();
            if (_state.IsCleared) { ShowClear(); return; }
            ShowDayPhase();
            RefreshUI();
        }

        // ================= 対策の強化 =================

        /// <summary>対策強化ボタンから呼ぶ。引数には "mfa" などのキーを指定する。</summary>
        public void OnClickUpgradeDefense(string defenseKey)
        {
            OnClickUpgradeDefense(defenseKey, null);
        }

        private void OnClickUpgradeDefense(string defenseKey, RectTransform sourceButton)
        {
            if (!_isDayPhase) return;
            if (!_state.UpgradeDefense(defenseKey)) return;

            AudioManager.Instance?.PlayUpgrade();

            // 「買えた」手応え：押したボタンから緑の波紋を出し、リスクが下がったことを直後の表示で見せる。
            if (effects != null && sourceButton != null)
            {
                effects.Burst(sourceButton, UIEffects.Good, 260f);
                effects.FloatingText(sourceButton, "強化！", UIEffects.Good, 26);
            }

            RefreshUI();
            BuildDefensePanel(); // Lv・コスト表示・購入可否を更新
        }

        // ================= UI更新 =================

        private void RefreshUI()
        {
            if (_state == null) return;

            if (periodLabel != null) periodLabel.text = _state.PeriodLabel();
            if (turnLabel != null) turnLabel.text = $"ターン {_state.Day} / {GameState.TotalPeriods}";

            ApplyStat(budgetText, budgetChip, budgetBar,
                v => $"予算　{Money.Yen(v)}", _prevBudget, _state.Budget,
                Mathf.Clamp01(_state.Budget / 100f), higherIsBetter: true);
            ApplyStat(trustText, trustChip, trustBar,
                v => $"人望　{v} / 100", _prevTrust, _state.Trust,
                Mathf.Clamp01(_state.Trust / 100f), higherIsBetter: true);
            ApplyStat(stressText, stressChip, stressBar,
                v => $"ストレス　{v} / 100", _prevStress, _state.Stress,
                Mathf.Clamp01(_state.Stress / 100f), higherIsBetter: false);

            _prevBudget = _state.Budget;
            _prevTrust = _state.Trust;
            _prevStress = _state.Stress;

            RefreshRisk();

            if (logText != null) logText.text = string.Join("\n", _state.Log);

            UpdateNavigator();
        }

        /// <summary>
        /// ステータス1項目分の表示を更新する。
        /// 数字はカウントアップし、増減があればチップを弾ませて浮遊テキストを出す
        /// （どこが動いたのか一目で分かるようにするため）。
        /// </summary>
        private void ApplyStat(TextMeshProUGUI label, RectTransform chip, Image bar,
                               System.Func<int, string> format, int prev, int value,
                               float barTarget, bool higherIsBetter)
        {
            if (label != null)
            {
                if (_numberRoutines.TryGetValue(label, out var running) && running != null) StopCoroutine(running);
                _numberRoutines[label] = StartCoroutine(CountNumber(label, format, prev, value));
            }
            if (bar != null) SetBarAnimated(bar, barTarget);

            int delta = value - prev;
            if (delta == 0 || effects == null || chip == null) return;

            bool good = higherIsBetter ? delta > 0 : delta < 0;
            Color color = good ? UIEffects.Good : UIEffects.Bad;
            effects.FloatingText(chip, delta > 0 ? $"+{delta}" : delta.ToString(), color, 34);
            effects.Punch(chip, good ? 1.16f : 1.22f);
            if (good) effects.Burst(chip, color, 190f);
        }

        private IEnumerator CountNumber(TextMeshProUGUI label, System.Func<int, string> format,
                                        int from, int to, float duration = 0.45f)
        {
            if (from != to)
            {
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    label.text = format(Mathf.RoundToInt(Mathf.Lerp(from, to, t / duration)));
                    yield return null;
                }
            }
            label.text = format(to);
            _numberRoutines[label] = null;
        }

        /// <summary>バーの数値変化を滑らかにアニメーションさせる。</summary>
        private void SetBarAnimated(Image bar, float target)
        {
            if (_barRoutines.TryGetValue(bar, out var running) && running != null) StopCoroutine(running);
            _barRoutines[bar] = StartCoroutine(AnimateBarFill(bar, target));
        }

        private IEnumerator AnimateBarFill(Image bar, float target, float duration = 0.4f)
        {
            float start = bar.fillAmount;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                bar.fillAmount = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            bar.fillAmount = target;
            _barRoutines[bar] = null;
        }

        /// <summary>
        /// 今の備えで見込まれる被害額を表示する。
        /// 対策を買った直後にこの数字が下がるので、投資した意味がその場で伝わる。
        /// </summary>
        private void RefreshRisk()
        {
            if (_state == null) return;
            float expected = _state.EstimateExpectedDamage();

            string level;
            Color color;
            if (expected < 6f) { level = "低"; color = UIEffects.Good; }
            else if (expected < 10f) { level = "中"; color = new Color(0.85f, 0.78f, 0.35f); }
            else if (expected < 14f) { level = "高"; color = new Color(0.92f, 0.55f, 0.25f); }
            else { level = "危険"; color = UIEffects.Bad; }

            if (riskLevelText != null)
            {
                riskLevelText.text = $"リスクレベル　{level}";
                riskLevelText.color = color;
            }
            if (riskDamageText != null)
                riskDamageText.text = $"被害予測　{Money.Yen(Mathf.RoundToInt(expected))}";
            if (riskBar != null)
            {
                riskBar.color = color;
                SetBarAnimated(riskBar, Mathf.Clamp01(expected / RiskBarMax));
            }
        }

        // ================= 設定オーバーレイ =================

        /// <summary>右上の歯車ボタンから呼ぶ。現在のフェーズを問わず開閉できる独立オーバーレイ。</summary>
        private void ToggleSettingsPanel(bool show)
        {
            if (settingsPanel == null) return;
            settingsPanel.SetActive(show);
            if (show) RefreshMuteButtonLabel();
        }

        private void OnClickToggleMute()
        {
            AudioManager.Instance?.ToggleMute();
            RefreshMuteButtonLabel();
        }

        private void RefreshMuteButtonLabel()
        {
            if (settingsMuteButtonLabel == null) return;
            bool muted = AudioManager.Instance != null && AudioManager.Instance.IsMuted;
            settingsMuteButtonLabel.text = muted ? "音声：オフ" : "音声：オン";
        }

        // ================= クイズ（事前・事後共通） =================

        /// <summary>
        /// クイズを開始する。isPre=trueなら事前クイズ（この後ゲーム本編を開始）、
        /// falseなら事後クイズ（この後結果サマリーを表示）。
        /// </summary>
        private void BeginQuiz(bool isPre)
        {
            _isPreQuiz = isPre;
            _quizQueue = EducationTracker.SampleQuestions(3);
            _quizIndex = 0;
            _quizCorrectCount = 0;
            _quizTotalCount = 0;
            _quizAnswering = false;
            ShowQuizQuestion();
        }

        private void ShowQuizQuestion()
        {
            HideAllPanels();
            quizPanel.SetActive(true);
            StartCoroutine(FadeInPanel(quizPanel));

            var q = _quizQueue[_quizIndex];
            if (quizProgressText != null)
                quizProgressText.text = $"{(_isPreQuiz ? "事前クイズ" : "事後クイズ")} {_quizIndex + 1} / {_quizQueue.Count}";
            if (quizQuestionText != null)
                quizQuestionText.text = q.Question;

            BuildQuizOptions(q);
        }

        /// <summary>選択肢ボタンを動的に生成する（BuildChoiceButtonsと同じパターン）。</summary>
        private void BuildQuizOptions(QuizQuestion q)
        {
            if (quizOptionContainer == null || quizOptionButtonPrefab == null) return;

            foreach (Transform child in quizOptionContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < q.Options.Length; i++)
            {
                int optionIndex = i; // クロージャ対策
                var go = Instantiate(quizOptionButtonPrefab, quizOptionContainer);
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = q.Options[i];

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    var capturedRect = go.GetComponent<RectTransform>();
                    button.onClick.AddListener(() =>
                    {
                        AudioManager.Instance?.PlayClick();
                        OnAnswerQuiz(optionIndex == q.AnswerIndex, capturedRect);
                    });
                }
            }
        }

        private void OnAnswerQuiz(bool correct, RectTransform sourceButton)
        {
            if (_quizAnswering) return; // 演出中の連打で2問飛ばさないようにする
            _quizAnswering = true;

            _quizTotalCount++;
            if (correct) _quizCorrectCount++;
            _quizIndex++;

            if (correct)
            {
                AudioManager.Instance?.PlayQuizCorrect();
                if (effects != null)
                {
                    effects.Flash(UIEffects.Good, 0.18f, 0.3f);
                    effects.Banner("正解！", UIEffects.Good, 0.35f);
                    effects.Burst(sourceButton, UIEffects.Good, 300f);
                }
            }
            else
            {
                AudioManager.Instance?.PlayQuizWrong();
                if (effects != null)
                {
                    effects.Flash(UIEffects.Bad, 0.22f, 0.35f);
                    effects.Banner("不正解…", UIEffects.Bad, 0.35f);
                    effects.Shake(0.25f, 10f);
                }
            }

            StartCoroutine(AdvanceQuizAfterFeedback());
        }

        /// <summary>正誤の演出を見せてから次の設問へ進む。</summary>
        private IEnumerator AdvanceQuizAfterFeedback()
        {
            // 回答直後に選択肢が押せたままだと二重回答になるので、先に無効化する
            if (quizOptionContainer != null)
            {
                foreach (Transform child in quizOptionContainer)
                {
                    var b = child.GetComponent<Button>();
                    if (b != null) b.interactable = false;
                }
            }

            yield return new WaitForSeconds(0.8f);
            _quizAnswering = false;

            if (_quizIndex < _quizQueue.Count) ShowQuizQuestion();
            else FinishQuiz();
        }

        private void FinishQuiz()
        {
            if (_isPreQuiz)
            {
                _preCorrect = _quizCorrectCount;
                _preTotal = _quizTotalCount;
                _state = new GameState();
                // 初期値を基準にしておき、初回RefreshUI()で無意味な+100等の増減演出が出ないようにする
                _prevBudget = _state.Budget;
                _prevTrust = _state.Trust;
                _prevStress = _state.Stress;
                ShowDayPhase();
                RefreshUI();
            }
            else
            {
                _postCorrect = _quizCorrectCount;
                _postTotal = _quizTotalCount;
                var stats = EducationTracker.RecordSession(_preCorrect, _preTotal, _postCorrect, _postTotal);
                ShowSummary(stats);
            }
        }

        private void ShowSummary(EduStats stats)
        {
            HideAllPanels();
            summaryPanel.SetActive(true);
            StartCoroutine(FadeInPanel(summaryPanel));

            if (summaryText != null)
            {
                int improvementPt = Mathf.RoundToInt(stats.Improvement * 100);
                string sign = improvementPt >= 0 ? "+" : "";
                summaryText.text =
                    "今回のセッション\n" +
                    $"事前 {_preCorrect}/{_preTotal} → 事後 {_postCorrect}/{_postTotal}\n\n" +
                    $"累計（全{stats.Sessions}回プレイ）\n" +
                    $"正答率 {Mathf.RoundToInt(stats.PreRate * 100)}% → {Mathf.RoundToInt(stats.PostRate * 100)}%（{sign}{improvementPt}pt）";
            }

            // 伸びていれば祝う。落ちていたら静かに出す（責めない）。
            if (effects != null && stats.Improvement > 0f)
            {
                effects.Flash(UIEffects.Gold, 0.22f, 0.6f);
                effects.Banner("成長！", UIEffects.Gold, 0.8f);
            }
        }

        // ================= 終了処理 =================

        private void ShowGameOver()
        {
            HideAllPanels();
            endingPanel.SetActive(true);
            StartCoroutine(FadeInPanel(endingPanel));
            AudioManager.Instance?.PlayBgmEnding();
            AudioManager.Instance?.PlayGameOver();

            if (endingText != null)
            {
                endingText.text = _state.GameOverReason;
                endingText.color = UIEffects.Bad;
            }
            if (endingCharacterLine != null) endingCharacterLine.text = "";

            var p = _activePersona;
            Speak(p?.FaceSad, Pick(p?.LineGameOver, "……力になれませんでした。"), force: true);
        }

        private void ShowClear()
        {
            HideAllPanels();
            endingPanel.SetActive(true);
            StartCoroutine(FadeInPanel(endingPanel));
            AudioManager.Instance?.PlayBgmEnding();
            AudioManager.Instance?.PlayClear();

            if (endingText != null)
            {
                endingText.text = "1年間、無事に会社を守り抜いた";
                endingText.color = UIEffects.Gold;
            }
            if (endingCharacterLine != null) endingCharacterLine.text = "気づけば1年が経っていた。";

            if (effects != null)
            {
                effects.Flash(UIEffects.Gold, 0.3f, 0.9f);
                effects.Banner("CLEAR！", UIEffects.Gold, 1.2f);
            }

            var p = _activePersona;
            Speak(p?.FaceProud, Pick(p?.LineClear, "お疲れさまでした。立派な情シスです。"), force: true);
        }
    }
}
