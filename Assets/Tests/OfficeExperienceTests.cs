#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public class OfficeExperienceTests
    {
        private readonly List<string> _glyphWarnings = new List<string>();

        [SetUp]
        public void ログ監視を開始する()
        {
            _glyphWarnings.Clear();
            Application.logMessageReceived += ObserveLog;
        }

        [TearDown]
        public void ログ監視と一時停止を解除する()
        {
            Application.logMessageReceived -= ObserveLog;
            Time.timeScale = 1f;
        }

        private void ObserveLog(string message, string trace, LogType type)
        {
            if (message.Contains("Unicode value") || message.Contains("Unable to add the requested"))
                _glyphWarnings.Add(message);
        }
        [Test]
        public void 相談の選択は人望と負担のトレードオフになる()
        {
            var help = new GameState(1);
            var rest = new GameState(1);
            help.ResolveChore(true, 4);
            rest.ResolveChore(false, 4);
            Assert.AreEqual(34, help.Trust);
            Assert.AreEqual(23, help.Stress);
            Assert.AreEqual(28, rest.Trust);
            Assert.AreEqual(14, rest.Stress);
            Assert.AreEqual(help.Budget, rest.Budget);
            Assert.AreEqual(1, help.HelpedColleagues);
            Assert.AreEqual(0, rest.HelpedColleagues);
        }

        [Test]
        public void プレビューは状態を変更せず全対策の判断材料を返す()
        {
            var state = new GameState(1);
            foreach (var key in GameData.Defenses.Keys)
            {
                string text = GamePresentation.DefenseDetail(state, key);
                StringAssert.Contains("ストレス", text);
                StringAssert.Contains("予測被害", text);
            }
            Assert.AreEqual(100, state.Budget);
            Assert.AreEqual(20, state.Stress);
            Assert.IsEmpty(state.DefenseLevels);
            Assert.IsEmpty(state.Log);
        }

        [Test]
        public void 結果の内訳は判定前の実数値と一致する()
        {
            var state = new GameState(4) { Budget = 999, Trust = 80, Stress = 80 };
            state.DefenseLevels["backup"] = 3;
            var choice = GameData.Choices[0];
            float expected = state.CalcFinalDefenseRate("phishing", choice, 0.1f);
            var result = state.ResolveAttack("phishing", choice, 0.1f);
            Assert.AreEqual(expected, result.FinalDefenseRate);
            Assert.AreEqual(20f / 300f, result.StressPenalty, 0.0001);
            Assert.AreEqual(Math.Clamp(result.EquipmentRate + result.ResponseBonus + result.ParryBonus
                + result.TrustBonus - result.StressPenalty, 0.02f, 0.95f), result.FinalDefenseRate, 0.0001);
            Assert.AreEqual(result.Defended ? 1 : 0, state.DefendedIncidents);
            Assert.AreEqual(result.RecoverySavings, state.RecoverySavings);
        }

        [Test]
        public void 多層防御でも無敵にならず終了後に資源を動かせない()
        {
            var state = new GameState(1) { Budget = 999, Trust = 100, Stress = 0 };
            foreach (var key in GameData.Defenses.Keys) state.DefenseLevels[key] = 3;
            foreach (var key in GameData.Attacks.Keys)
            {
                Assert.LessOrEqual(state.CalcDefenseRate(key), 0.70f);
                Assert.LessOrEqual(state.CalcFinalDefenseRate(key, GameData.Choices[0], 0.15f), 0.95f);
            }
            state.IsGameOver = true;
            state.ResolveChore(true, 6);
            state.AdvanceDay();
            Assert.AreEqual(1, state.Day);
            Assert.AreEqual(999, state.Budget);
            Assert.IsFalse(state.UpgradeDefense("mfa"));
        }

        [Test]
        public void 年間シミュレーションで備えと休息が生存率を改善する()
        {
            const int runs = 500;
            int prepared = 0, unprepared = 0;
            for (int seed = 0; seed < runs; seed++)
            {
                if (Simulate(seed, true)) prepared++;
                if (Simulate(seed, false)) unprepared++;
            }
            Debug.Log($"[Balance] {runs}試行: 備えあり {prepared}/{runs}, 備えなし {unprepared}/{runs}");
            Assert.Greater(prepared, unprepared + runs * 0.15f);
        }

        private static bool Simulate(int seed, bool prepared)
        {
            var state = new GameState(seed);
            for (int turn = 0; turn < GameState.TotalPeriods && !state.IsGameOver; turn++)
            {
                if (prepared)
                {
                    string key = GameData.Defenses.Keys
                        .Where(k => (!state.DefenseLevels.TryGetValue(k, out int lv) || lv < 3)
                            && GameData.Defenses[k].Levels[state.DefenseLevels.TryGetValue(k, out int v) ? v : 0].Cost <= state.Budget - 24)
                        .OrderBy(k => state.DefenseLevels.TryGetValue(k, out int v) ? v : 0)
                        .ThenBy(k => k == "training" ? 0 : k == "backup" ? 1 : 2).FirstOrDefault();
                    if (key != null) state.UpgradeDefense(key);
                }
                state.ResolveChore(!prepared || state.Stress < 62 || state.Trust < 25, 4);
                if (state.IsGameOver) break;
                if (state.RollAttackOccurrence())
                {
                    var key = state.RollAttackType();
                    var choice = prepared && state.Budget > 40 && state.Stress < 70 ? GameData.Choices[0] : GameData.Choices[1];
                    state.ResolveAttack(key, choice, prepared ? 0.10f : 0f);
                }
                state.AdvanceDay();
                Assert.That(state.Budget, Is.InRange(0, 999));
                Assert.That(state.Trust, Is.InRange(0, 100));
                Assert.That(state.Stress, Is.InRange(0, 100));
            }
            return state.IsCleared;
        }

        [UnityTest]
        [Category("Capture")]
        public IEnumerator 実画面の描画とフォントと中断復帰を検証する()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return new WaitForSeconds(0.7f);
            Capture("01-title", 1600, 900);
            Find<Button>("QuickStartButton").onClick.Invoke();
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(Find<Transform>("DayPanel").gameObject.activeSelf);
            Assert.IsNotNull(Find<Image>("OfficeIllustration").sprite);
            var font = Find<TextMeshProUGUI>("DayGuide").font;
            Assert.IsTrue(font.atlasTextures.All(t => t != null && t.isReadable));
            const string fontProbe = "ひなた　人望予算緒測録返学習費用警戒→";
            font.TryAddCharacters(fontProbe, out _);
            Assert.IsTrue(font.HasCharacters(fontProbe), "日本語の必要文字が欠けています。");
            Capture("02-office", 1600, 900);
            Capture("03-office-compact", 1280, 720);
            var gm = Object.FindAnyObjectByType<GameManager>();
            gm.OnClickProceedDay();
            yield return new WaitForSeconds(0.5f);
            Capture("04-consultation", 1600, 900);
            var method = typeof(GameManager).GetMethod("ToggleSettingsPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(gm, new object[] { true });
            Assert.AreEqual(0f, Time.timeScale);
            gm.OnClickResolveChore(true);
            Assert.IsTrue(Find<Transform>("ChorePanel").gameObject.activeSelf);
            method.Invoke(gm, new object[] { false });
            Assert.AreEqual(1f, Time.timeScale);

            // 乱数に依存せず攻撃・パリィ・結果の実描画を通す。
            typeof(GameManager).GetField("_currentAttackKey", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(gm, "phishing");
            typeof(GameManager).GetMethod("ShowAttackPhase", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gm, null);
            yield return new WaitForSeconds(0.7f);
            Capture("05-incident", 1600, 900);
            Find<Transform>("ChoiceButtonContainer").GetChild(0).GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSeconds(0.7f);
            Capture("06-response", 1600, 900);
            gm.OnClickParry();
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(Find<Transform>("ResultPanel").gameObject.activeSelf);
            StringAssert.Contains("設備", Find<TextMeshProUGUI>("ResultCharacterLine").text);
            Capture("07-result", 1600, 900);
            gm.OnClickNextDay();
            yield return new WaitForSeconds(0.4f);
            gm.OnClickProceedDay();
            gm.OnClickResolveChore(true);
            typeof(GameManager).GetMethod("ShowTitle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gm, null);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(Find<Transform>("TitlePanel").gameObject.activeSelf, "中断前のコルーチンがタイトルを上書きした");
            Assert.IsEmpty(_glyphWarnings, string.Join("\n", _glyphWarnings));
            LogAssert.NoUnexpectedReceived();
        }

        private static T Find<T>(string name) where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Include).First(t => t.name == name);

        [UnityTest]
        [Category("Capture")]
        public IEnumerator ゲージと透過立ち絵と購入前の説明を確認する()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return new WaitForSeconds(0.5f);
            Find<Button>("QuickStartButton").onClick.Invoke();
            yield return new WaitForSeconds(0.8f);
            var gm=Object.FindAnyObjectByType<GameManager>();
            foreach(var field in new[]{"budgetBar","trustBar","stressBar","riskBar"})
            {
                var bar=(Image)typeof(GameManager).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(gm);
                Assert.IsNotNull(bar.sprite, field+"が全幅表示になる原因: Filledにスプライト未設定");
                if(field=="trustBar")Assert.AreEqual(0.30f,bar.fillAmount,0.001f);
                if(field=="stressBar")Assert.AreEqual(0.20f,bar.fillAmount,0.001f);
            }
            var portrait=Find<Image>("NavigatorPortrait");
            Assert.IsTrue(portrait.enabled && portrait.sprite!=null,"暫定ひなたの画像が未割当");
            Assert.IsFalse(Find<Transform>("PortraitPlaceholder").gameObject.activeSelf);
            var texture=new Texture2D(2,2);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,"Sprites/Hinata/v2/pose_fists.png"))));
            var pixels=texture.GetPixels32();
            Assert.Greater(pixels.Count(p=>p.a==0),pixels.Length/10,"背景が真に透明ではありません");
            Object.DestroyImmediate(texture);
            var rows=Find<Transform>("DefenseButtonContainer").GetComponentsInChildren<DefenseDetailsTrigger>();
            Assert.AreEqual(8,rows.Length);
            Assert.AreEqual(8,Find<Transform>("DefenseButtonContainer").GetComponentsInChildren<DefenseGlyph>().Length);
            rows[0].FocusChanged(true);
            StringAssert.Contains("予測被害",Find<TextMeshProUGUI>("DayGuide").text);
            Capture("08-defense-detail",1600,900);
            gm.OnClickUpgradeDefense("mfa");
            yield return new WaitForSeconds(0.8f);
            Capture("09-upgrade",1600,900);
            Assert.IsEmpty(_glyphWarnings,string.Join("\n",_glyphWarnings));
        }

        private static void Capture(string name, int width, int height)
        {
            var canvas = Find<Canvas>("Canvas");
            var cameraObject = new GameObject("検証用カメラ", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            var target = new RenderTexture(width, height, 24);
            var oldActive = RenderTexture.active;
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            foreach (var label in canvas.GetComponentsInChildren<TextMeshProUGUI>()) label.ForceMeshUpdate();
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            string directory = Path.Combine(Application.dataPath, "../Artifacts/OfficeReview");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), texture.EncodeToPNG());
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            camera.targetTexture = null;
            RenderTexture.active = oldActive;
            target.Release();
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
        }
    }
}
#endif
