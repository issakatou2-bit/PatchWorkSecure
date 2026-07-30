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
    /// 【Unityエディタでの設定手順】
    /// 1. 空のGameObjectを作り、名前を "GameManager" にする
    /// 2. このスクリプトをアタッチする
    /// 3. インスペクタに表示される各項目に、対応するUI要素をドラッグして割り当てる
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ---- ステータス表示 ----
        [Header("ステータス表示")]
        [SerializeField] private TextMeshProUGUI periodLabel;   // 「4月上旬」
        [SerializeField] private TextMeshProUGUI budgetText;
        [SerializeField] private TextMeshProUGUI trustText;
        [SerializeField] private TextMeshProUGUI stressText;
        [SerializeField] private Image budgetBar;
        [SerializeField] private Image trustBar;
        [SerializeField] private Image stressBar;

        // ---- ナビゲーターキャラ ----
        [Header("ナビゲーター")]
        [SerializeField] private Image navigatorPortrait;
        [SerializeField] private TextMeshProUGUI navigatorLine;
        [SerializeField] private Sprite faceNormal;
        [SerializeField] private Sprite faceWorried;
        [SerializeField] private Sprite faceAlert;
        [SerializeField] private Sprite faceRelieved;
        [SerializeField] private Sprite faceProud;
        [SerializeField] private Sprite faceSad;

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

        [Header("操作ボタン（未割当でも動作するが、割り当てると自動配線される）")]
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
        [SerializeField] private TextMeshProUGUI attackIntroLine;
        [SerializeField] private TextMeshProUGUI scTermText;
        [SerializeField] private Transform choiceButtonContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("対策強化パネル（dayPanelの子として配置想定）")]
        [SerializeField] private Transform defenseButtonContainer;
        [SerializeField] private GameObject defenseButtonPrefab;

        [Header("パリィパネル")]
        [SerializeField] private RectTransform parryMarker;
        [SerializeField] private RectTransform parryTrack;
        [SerializeField] private float parrySpeed = 250f;

        [Header("結果パネル")]
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI resultCharacterLine;

        [Header("ログ")]
        [SerializeField] private TextMeshProUGUI logText;

        // ---- 内部状態 ----
        private GameState _state;
        private string _currentAttackKey;
        private AttackChoice _pendingChoice;
        private float _parryPosition;   // 0〜1
        private int _parryDirection = 1;
        private bool _parryActive;

        // クイズ進行状態（事前/事後で使い回す）
        private List<QuizQuestion> _quizQueue;
        private int _quizIndex;
        private int _quizCorrectCount;
        private int _quizTotalCount;
        private bool _isPreQuiz;
        private int _preCorrect, _preTotal, _postCorrect, _postTotal;

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
            ShowTitle();
        }

        /// <summary>
        /// ボタンのクリックイベントをコードから配線する。
        /// InspectorのOnClick()を手動設定する代わりに、ここでまとめて済ませる。
        /// 各Buttonフィールドが未割当(null)の場合は何もしない（Inspector側で個別に設定していても問題ない）。
        /// </summary>
        private void WireButtons()
        {
            if (proceedButton != null) proceedButton.onClick.AddListener(OnClickProceedDay);
            if (solveChoreButton != null) solveChoreButton.onClick.AddListener(() => OnClickResolveChore(true));
            if (postponeChoreButton != null) postponeChoreButton.onClick.AddListener(() => OnClickResolveChore(false));
            if (parryButton != null) parryButton.onClick.AddListener(OnClickParry);
            if (nextDayButton != null) nextDayButton.onClick.AddListener(OnClickNextDay);
            if (startButton != null) startButton.onClick.AddListener(OnClickStartGame);
            if (endingContinueButton != null) endingContinueButton.onClick.AddListener(() => BeginQuiz(false));
            if (summaryCloseButton != null) summaryCloseButton.onClick.AddListener(ShowTitle);
        }

        private void Update()
        {
            if (_parryActive) UpdateParryMarker();
        }

        // ---- フェーズ制御 ----

        private void HideAllPanels()
        {
            if (titlePanel != null) titlePanel.SetActive(false);
            dayPanel.SetActive(false);
            chorePanel.SetActive(false);
            attackPanel.SetActive(false);
            parryPanel.SetActive(false);
            resultPanel.SetActive(false);
            if (quizPanel != null) quizPanel.SetActive(false);
            if (endingPanel != null) endingPanel.SetActive(false);
            if (summaryPanel != null) summaryPanel.SetActive(false);
        }

        /// <summary>
        /// パネルをフェードインさせる（演出用）。CanvasGroupが無ければ自動で付ける。
        /// 「切り替わった瞬間にパッと出る」冷たさを減らすための最小限の演出。
        /// </summary>
        private IEnumerator FadeInPanel(GameObject panel, float duration = 0.25f)
        {
            if (panel == null) yield break;
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 1f;
        }

        private void ShowTitle()
        {
            HideAllPanels();
            titlePanel.SetActive(true);
            StartCoroutine(FadeInPanel(titlePanel));
        }

        /// <summary>タイトル画面の「はじめる」ボタンから呼ぶ。事前クイズへ進む。</summary>
        public void OnClickStartGame()
        {
            BeginQuiz(isPre: true);
        }

        private void ShowDayPhase()
        {
            HideAllPanels();
            dayPanel.SetActive(true);
            StartCoroutine(FadeInPanel(dayPanel));
            BuildDefensePanel();
            UpdateNavigator();
        }

        /// <summary>「今日の業務を進める」ボタンから呼ぶ。</summary>
        public void OnClickProceedDay()
        {
            _currentChore = _chores[Random.Range(0, _chores.Length)];
            HideAllPanels();
            chorePanel.SetActive(true);
            StartCoroutine(FadeInPanel(chorePanel));
            choreText.text = _currentChore.text;
            UpdateNavigator();
        }

        /// <summary>雑務への対応ボタンから呼ぶ（誠実に対応=true / 後回し=false）。</summary>
        public void OnClickResolveChore(bool solved)
        {
            _state.ResolveChore(solved, _currentChore.trustGain);
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
            yield return new WaitForSeconds(0.35f);

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

            var attack = GameData.Attacks[_currentAttackKey];
            attackNameText.text = attack.DisplayName;
            attackGradeText.text = attack.Grade == AttackGrade.None ? "" : GradeLabel(attack.Grade);
            attackIntroLine.text = $"「{attack.LineIntro}」";
            scTermText.text = $"📘 {attack.ScTerm} — {attack.ScNote}";

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

        /// <summary>対応選択肢のボタンを動的に生成する。</summary>
        private void BuildChoiceButtons()
        {
            foreach (Transform child in choiceButtonContainer)
                Destroy(child.gameObject);

            foreach (var choice in GameData.Choices)
            {
                var go = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    string costText = choice.BudgetCost > 0 ? $"¥{choice.BudgetCost}" : "無料";
                    string stressText = choice.StressCost >= 0 ? $"+{choice.StressCost}" : choice.StressCost.ToString();
                    label.text = $"{choice.Label}\n<size=70%>{choice.Description}  {costText} / ストレス{stressText}</size>";
                }

                var button = go.GetComponent<Button>();
                var captured = choice; // クロージャ対策
                button.interactable = _state.Budget >= choice.BudgetCost;
                button.onClick.AddListener(() => OnSelectChoice(captured));
            }
        }

        /// <summary>
        /// 対策強化ボタンを動的に生成する（Reactプロトタイプの「セキュリティ対策」パネルと同じ設計）。
        /// 攻撃対応ボタン(BuildChoiceButtons)と同じパターンで、GameData.Defensesの内容をそのまま反映する。
        /// dayPanel配下に置く想定なので、攻撃/パリィ/結果フェーズ中は自動的に非表示になる。
        /// </summary>
        private void BuildDefensePanel()
        {
            if (defenseButtonContainer == null || defenseButtonPrefab == null) return;

            foreach (Transform child in defenseButtonContainer)
                Destroy(child.gameObject);

            foreach (var kv in GameData.Defenses)
            {
                string key = kv.Key;
                var def = kv.Value;
                int currentLvl = _state.DefenseLevels.TryGetValue(key, out int lv) ? lv : 0;
                bool maxed = currentLvl >= def.Levels.Count;

                var go = Instantiate(defenseButtonPrefab, defenseButtonContainer);
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
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
                    rightText = $"¥{next.Cost}";
                }

                if (label != null)
                {
                    string lvText = currentLvl > 0 ? $" Lv.{currentLvl}" : "";
                    label.text = $"{def.DisplayName}{lvText}\n<size=65%>📘{def.ScTerm}　{rightText}</size>";
                }

                if (button != null)
                {
                    button.interactable = !maxed && affordable;
                    string capturedKey = key; // クロージャ対策
                    button.onClick.AddListener(() => OnClickUpgradeDefense(capturedKey));
                }
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
            _parryActive = true;
            UpdateNavigator();
        }

        // ---- パリィ ----

        private void UpdateParryMarker()
        {
            _parryPosition += _parryDirection * (parrySpeed / 1000f) * Time.deltaTime * 60f / 60f;
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

            StartCoroutine(ResolveWithSuspense(parryBonus));
        }

        /// <summary>結果をすぐ出さず、一瞬の「溜め」を挟んでから開示する。</summary>
        private IEnumerator ResolveWithSuspense(float parryBonus)
        {
            HideAllPanels();
            yield return new WaitForSeconds(0.9f);

            var result = _state.ResolveAttack(_currentAttackKey, _pendingChoice, parryBonus);
            RefreshUI();

            if (_state.IsGameOver)
            {
                ShowGameOver();
                yield break;
            }

            resultPanel.SetActive(true);
            StartCoroutine(FadeInPanel(resultPanel));
            resultText.text = result.Defended
                ? $"🛡️ {result.Flavor}（防御率{Mathf.RoundToInt(result.FinalDefenseRate * 100)}%）"
                : $"💥 {result.Flavor}（予算-{result.BudgetDamage}, 人望-{result.TrustDamage}）";
            resultCharacterLine.text = $"「{result.CharacterLine}」";

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

        // ---- 対策の強化 ----

        /// <summary>対策強化ボタンから呼ぶ。引数には "mfa" などのキーを指定する。</summary>
        public void OnClickUpgradeDefense(string defenseKey)
        {
            if (_state.UpgradeDefense(defenseKey))
            {
                RefreshUI();
                BuildDefensePanel(); // Lv・コスト表示・購入可否を更新
            }
        }

        // ---- UI更新 ----

        private void RefreshUI()
        {
            periodLabel.text = $"{_state.PeriodLabel()}（{_state.Day}/{GameState.TotalPeriods}）";
            budgetText.text = _state.Budget.ToString();
            trustText.text = _state.Trust.ToString();
            stressText.text = _state.Stress.ToString();

            if (budgetBar != null) SetBarAnimated(budgetBar, Mathf.Clamp01(_state.Budget / 100f));
            if (trustBar != null) SetBarAnimated(trustBar, Mathf.Clamp01(_state.Trust / 100f));
            if (stressBar != null) SetBarAnimated(stressBar, Mathf.Clamp01(_state.Stress / 100f));

            if (logText != null)
                logText.text = string.Join("\n", _state.Log);

            UpdateNavigator();
        }

        /// <summary>
        /// バーの数値変化を滑らかにアニメーションさせる。
        /// 予算・人望・ストレスがカクッと変わる冷たさを減らすための演出。
        /// </summary>
        private void SetBarAnimated(Image bar, float target)
        {
            StartCoroutine(AnimateBarFill(bar, target));
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
        }

        /// <summary>状況に応じてナビゲーターの表情とセリフを変える。</summary>
        private void UpdateNavigator()
        {
            if (navigatorPortrait == null || navigatorLine == null) return;

            Sprite face = faceNormal;
            string line = "今日も平穏です。備えを進めましょうか。";

            if (attackPanel != null && attackPanel.activeSelf)
            {
                var grade = GameData.Attacks[_currentAttackKey].Grade;
                if (grade == AttackGrade.S) { face = faceAlert; line = "これ、一番まずいやつです……対策、足りてますか？"; }
                else if (grade == AttackGrade.Rookie || grade == AttackGrade.Rising) { face = faceWorried; line = "見たことない手口です。慎重にいきましょう。"; }
                else { face = faceWorried; line = "来ましたね。落ち着いて対応しましょう。"; }
            }
            else if (parryPanel != null && parryPanel.activeSelf)
            {
                face = faceAlert; line = "タイミング、いきますよ……！";
            }
            else if (_state.Stress > 65)
            {
                face = faceWorried; line = "みんな疲れてます。締めすぎも危険ですよ。";
            }
            else if (_state.Trust < 20)
            {
                face = faceWorried; line = "社内での信頼が薄いです。雑務対応、大事ですよ。";
            }
            else if (_state.Budget < 30)
            {
                face = faceWorried; line = "予算が心もとないですね。慎重に使いましょう。";
            }
            else if (_state.DefenseLevels.Count == 0)
            {
                face = faceWorried; line = "まだ何も対策がありません。何か入れませんか？";
            }
            else if (_state.Day > GameState.TotalPeriods * 0.7f)
            {
                face = faceAlert; line = "年度末が近いです。攻撃も激しくなってきました。";
            }
            else if (_state.Trust > 60)
            {
                face = faceProud; line = "社内の空気、いい感じですね。";
            }

            navigatorPortrait.sprite = face;
            navigatorLine.text = line;
        }

        private void UpdateNavigatorForResult(AttackResult result)
        {
            if (navigatorPortrait == null) return;

            if (result.Defended)
            {
                bool narrow = result.Flavor != null && result.Flavor.Contains("紙一重");
                navigatorPortrait.sprite = narrow ? faceRelieved : faceProud;
                navigatorLine.text = narrow
                    ? "あぶなかった……！ でも、守りきりました。"
                    : "危なげなかったですね。備えの成果です。";
            }
            else
            {
                navigatorPortrait.sprite = faceSad;
                navigatorLine.text = "……やられました。次はもっと備えましょう。";
            }
        }

        // ---- クイズ（事前・事後共通） ----

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
            ShowQuizQuestion();
        }

        private void ShowQuizQuestion()
        {
            HideAllPanels();
            quizPanel.SetActive(true);
            StartCoroutine(FadeInPanel(quizPanel));

            var q = _quizQueue[_quizIndex];
            if (quizProgressText != null)
                quizProgressText.text = $"{(_isPreQuiz ? "事前クイズ" : "事後クイズ")}　{_quizIndex + 1}/{_quizQueue.Count}";
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
                    button.onClick.AddListener(() => OnAnswerQuiz(optionIndex == q.AnswerIndex));
            }
        }

        private void OnAnswerQuiz(bool correct)
        {
            _quizTotalCount++;
            if (correct) _quizCorrectCount++;
            _quizIndex++;

            if (_quizIndex < _quizQueue.Count)
                ShowQuizQuestion();
            else
                FinishQuiz();
        }

        private void FinishQuiz()
        {
            if (_isPreQuiz)
            {
                _preCorrect = _quizCorrectCount;
                _preTotal = _quizTotalCount;
                _state = new GameState();
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
        }

        // ---- 終了処理 ----

        private void ShowGameOver()
        {
            HideAllPanels();
            endingPanel.SetActive(true);
            StartCoroutine(FadeInPanel(endingPanel));

            if (endingText != null) endingText.text = $"📉 {_state.GameOverReason}";
            if (endingCharacterLine != null) endingCharacterLine.text = "";
            if (navigatorPortrait != null)
            {
                navigatorPortrait.sprite = faceSad;
                navigatorLine.text = "……力になれませんでした。";
            }
        }

        private void ShowClear()
        {
            HideAllPanels();
            endingPanel.SetActive(true);
            StartCoroutine(FadeInPanel(endingPanel));

            if (endingText != null) endingText.text = "🏢✨ 1年間、無事に会社を守り抜いた";
            if (endingCharacterLine != null) endingCharacterLine.text = "気づけば1年が経っていた。";
            if (navigatorPortrait != null)
            {
                navigatorPortrait.sprite = faceProud;
                navigatorLine.text = "お疲れさまでした。立派な情シスです。";
            }
        }
    }
}
