#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;

namespace PatchWorkSecure.Tests
{
    /// <summary>
    /// 実際にシーンを再生して、一連の流れが例外なく進むかを確認する通しテスト。
    ///
    /// Claude Codeはエディタ画面を見られないため「Playした瞬間に出るNullReference」を
    /// 発見できない。それを機械的に検出するための安全網として置いている。
    /// UnityTestは実行中にエラーログが出ると自動的に失敗するので、
    /// 明示的なAssertが無くても例外はここで捕まる。
    ///
    /// 実行: Unity上ならTest Runner、コマンドラインなら
    ///   Unity.exe -batchmode -runTests -testPlatform PlayMode -projectPath ...
    /// </summary>
    public class GameFlowSmokeTest
    {
        [UnityTest]
        public IEnumerator タイトルから本編まで例外なく進行できる()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;

            var gm = Object.FindAnyObjectByType<GameManager>();
            Assert.IsNotNull(gm, "GameManagerがシーンに存在しない");

            // ---- タイトル画面 ----
            var startButton = FindByName<Button>("StartButton");
            Assert.IsNotNull(startButton, "「はじめる」ボタンが見つからない");
            Assert.IsTrue(FindByName<Transform>("TitlePanel").gameObject.activeInHierarchy,
                "起動直後にタイトル画面が表示されていない");

            startButton.onClick.Invoke();
            yield return null;

            // ---- 事前クイズ（3問）----
            for (int i = 1; i <= 3; i++)
            {
                var option = FindActiveQuizOption();
                Assert.IsNotNull(option, $"事前クイズ{i}問目の選択肢が生成されていない");
                option.onClick.Invoke();
                // 正誤演出を見せてから次に進む作りなので、その分だけ待つ
                yield return new WaitForSeconds(1.0f);
            }

            // ---- 本編（日常フェーズ）----
            var dayPanel = FindByName<Transform>("DayPanel");
            Assert.IsNotNull(dayPanel, "DayPanelが見つからない");
            Assert.IsTrue(dayPanel.gameObject.activeInHierarchy, "事前クイズ後に日常フェーズへ進んでいない");

            AssertStatLabelsHaveNames();

            // 対策リストが並んでいるか（サイドバーに常時表示される）
            var defenseContainer = FindByName<Transform>("DefenseButtonContainer");
            Assert.IsNotNull(defenseContainer, "対策リストのコンテナが見つからない");
            Assert.AreEqual(GameData.Defenses.Count, defenseContainer.childCount,
                "対策リストの行数がマスターデータと一致しない");

            // ---- 雑務フェーズへ ----
            var proceed = FindByName<Button>("ProceedButton");
            Assert.IsNotNull(proceed, "「今日の業務を進める」ボタンが見つからない");
            proceed.onClick.Invoke();
            yield return null;

            Assert.IsTrue(FindByName<Transform>("ChorePanel").gameObject.activeInHierarchy,
                "雑務フェーズに遷移していない");

            var solve = FindByName<Button>("SolveButton");
            Assert.IsNotNull(solve, "「誠実に対応する」ボタンが見つからない");
            solve.onClick.Invoke();

            // 攻撃判定を挟むので、次のフェーズが出るまで少し待つ
            yield return new WaitForSeconds(1.2f);

            // 攻撃が起きたかどうかは乱数次第。どちらに転んでも例外なく画面が出ていればよい
            bool attacked = FindByName<Transform>("AttackPanel").gameObject.activeInHierarchy;
            bool backToDay = FindByName<Transform>("DayPanel").gameObject.activeInHierarchy;
            Assert.IsTrue(attacked || backToDay,
                "雑務対応のあと、攻撃フェーズにも日常フェーズにも遷移していない");
        }

        [UnityTest]
        public IEnumerator ナビゲーターが表示されセリフが入っている()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return new WaitForSeconds(0.6f); // タイプライター演出が流れる分だけ待つ

            var nameText = FindByName<TextMeshProUGUI>("NameText");
            Assert.IsNotNull(nameText, "キャラ名の表示が見つからない");
            Assert.IsNotEmpty(nameText.text, "キャラ名が空になっている");

            var line = FindByName<TextMeshProUGUI>("NavigatorLine");
            Assert.IsNotNull(line, "セリフの表示が見つからない");
            Assert.IsNotEmpty(line.text, "セリフが空になっている");

            // 立ち絵が未実装の間は、代役のプレースホルダーが出ていること
            var placeholder = FindByName<Transform>("PortraitPlaceholder");
            var portrait = FindByName<Image>("NavigatorPortrait");
            Assert.IsTrue(
                (placeholder != null && placeholder.gameObject.activeInHierarchy) ||
                (portrait != null && portrait.enabled && portrait.sprite != null),
                "立ち絵も代役キャラも表示されていない");
        }

        // ================= 補助 =================

        /// <summary>非アクティブなオブジェクトも含めて名前で探す。</summary>
        private static T FindByName<T>(string name) where T : Component
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include)
                .FirstOrDefault(c => c.gameObject.name == name);
        }

        private static Button FindActiveQuizOption()
        {
            var container = FindByName<Transform>("QuizOptionContainer");
            if (container == null || container.childCount == 0) return null;
            return container.GetChild(0).GetComponent<Button>();
        }

        /// <summary>
        /// ステータス表示に項目名が入っているかを確認する。
        /// 数値だけを書き込んで「予算 100」が「100」に化けた不具合が実際にあったため、
        /// その再発をここで検出する。
        /// </summary>
        private static void AssertStatLabelsHaveNames()
        {
            var texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include)
                .Where(t => t.gameObject.name == "ValueText")
                .Select(t => t.text)
                .ToList();

            Assert.IsNotEmpty(texts, "ステータス表示が見つからない");
            foreach (string keyword in new[] { "予算", "人望", "ストレス" })
            {
                Assert.IsTrue(texts.Any(t => t.Contains(keyword)),
                    $"ステータス表示に「{keyword}」の項目名が出ていない（実際の表示: {string.Join(" / ", texts)}）");
            }
        }
    }
}
#endif
