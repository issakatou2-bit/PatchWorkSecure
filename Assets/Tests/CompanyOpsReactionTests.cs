#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [Test] public void 短い反応は九場面六候補で連続重複と乱数汚染を避ける()
        {
            var bank = ScriptableObject.CreateInstance<OpsReactionBank>(); bank.lines = OpsReactionBank.Defaults();
            try
            {
                Assert.AreEqual(54, bank.lines.Length); Assert.AreEqual(54, bank.lines.Select(l => l.id).Distinct().Count());
                Assert.IsFalse(bank.HasAudio);
                var state = new OpsState(14); string before = JsonUtility.ToJson(state);
                foreach (OpsReaction cue in Enum.GetValues(typeof(OpsReaction)))
                {
                    Assert.AreEqual(6, bank.lines.Count(l => l.reaction == cue));
                    var director = new OpsReactionDirector(42); string last = "";
                    for (int i = 0; i < 30; i++)
                    {
                        Assert.IsTrue(director.TryChoose(bank, cue, i * 9, out var line));
                        Assert.AreNotEqual(last, line.id); Assert.IsNull(line.clip); last = line.id;
                    }
                }
                Assert.AreEqual(before, JsonUtility.ToJson(state));
            }
            finally { Object.DestroyImmediate(bank); }
        }

        [Test] public void 反応の間隔と優先順位と未投入音源を扱う()
        {
            var bank = ScriptableObject.CreateInstance<OpsReactionBank>(); bank.lines = OpsReactionBank.Defaults();
            var clip = AudioClip.Create("検証用の無音", 24000, 1, 24000, false);
            try
            {
                var director = new OpsReactionDirector(9);
                Assert.IsFalse(director.TryChoose(null, OpsReaction.Month, 0, out _));
                Assert.IsFalse(director.TryChoose(bank, OpsReaction.Month, double.NaN, out _));
                Assert.IsTrue(director.TryChoose(bank, OpsReaction.Month, 0, out _));
                Assert.IsFalse(director.TryChoose(bank, OpsReaction.Alert, .7, out _));
                Assert.IsTrue(director.TryChoose(bank, OpsReaction.Alert, 1, out _));
                Assert.IsFalse(director.TryChoose(bank, OpsReaction.Think, 2, out _));
                Assert.IsFalse(director.TryChoose(bank, OpsReaction.Alert, 8.9, out _));
                Assert.IsTrue(director.TryChoose(bank, OpsReaction.Alert, 9, out _));
                var recorded=bank.lines.First(l=>l.reaction==OpsReaction.Success);recorded.clip = clip; Assert.IsTrue(bank.HasAudio);
                Assert.IsTrue(director.TryChoose(bank, OpsReaction.Success, 14, out var line));
                Assert.AreSame(recorded, line, "一部投入でも再生と字幕を一致させる");
                Assert.IsTrue(director.TryChoose(bank,OpsReaction.Success,23,out line));Assert.AreNotSame(recorded,line,"唯一の音源を連続で使わず、別行の字幕へ進む");
                bank.lines = Array.Empty<OpsReactionLine>();
                Assert.IsFalse(director.TryChoose(bank, OpsReaction.Clear, 20, out _));
            }
            finally { Object.DestroyImmediate(bank); Object.DestroyImmediate(clip); }
        }

        [UnityTest] public IEnumerator 反応ボイスは未投入を明示し字幕と個別消音を保つ()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.5f);
            var game = Object.FindAnyObjectByType<OpsGame>(); Assert.IsNotNull(game.Navigator.Reactions);
            game.UseLocalTestVoices=false;
            Assert.IsFalse(game.Navigator.Reactions.HasAudio, "リポジトリの台本には非公開音声を参照させない");
            Click("HomeSettings"); yield return null;
            Click("AdvancedSettings");yield return null;
            StringAssert.Contains("音源未投入", Find<TextMeshProUGUI>("VoiceStatus").text);
            Assert.IsFalse(Find<Button>("PreviewVoice").interactable);
            CheckPointer("VoiceToggle"); CheckPointer("DiagnosticVoiceVolume"); Click("CloseDialog");
            var original = game.Navigator;
            var persona = Object.Instantiate(original);
            var bank = ScriptableObject.CreateInstance<OpsReactionBank>(); bank.lines = OpsReactionBank.Defaults();
            var clip = AudioClip.Create("検証用の無音", 48000, 1, 24000, false);var recorded=bank.lines.First(l=>l.reaction==OpsReaction.Success);recorded.clip = clip;
            try
            {
                persona.Reactions = bank; game.Navigator = persona;
                Click("HomeSettings"); yield return null; CheckText();
                Click("AdvancedSettings");yield return null;
                Assert.IsTrue(Find<Button>("PreviewVoice").interactable);
                Click("PreviewVoice"); yield return new WaitForSecondsRealtime(.3f);
                Assert.AreEqual(recorded.id, game.LastReactionId);
                Assert.AreEqual(recorded.caption, Find<TextMeshProUGUI>("VoicePreviewCaption").text);
                var source = game.GetComponents<AudioSource>().Single(s => s.clip == clip);
                Assert.IsTrue(source.isPlaying);
                Click("VoiceToggle"); yield return null; Assert.IsFalse(game.VoiceEnabled); Assert.IsFalse(source.isPlaying);
                Assert.IsFalse(Find<Button>("PreviewVoice").interactable);
                Capture("39-voice-settings", 1280, 720); CheckText();
                foreach (var label in Object.FindObjectsByType<TextMeshProUGUI>().Where(t => t.name.StartsWith("Voice")))
                { label.ForceMeshUpdate(); Assert.IsFalse(label.isTextOverflowing, label.name); }
                Click("VoiceToggle"); yield return null; Assert.IsTrue(game.VoiceEnabled);
                Assert.IsEmpty(glyphWarnings); LogAssert.NoUnexpectedReceived();
            }
            finally { game.Navigator = original; Object.Destroy(persona); Object.Destroy(bank); Object.Destroy(clip); }
        }
    }
}
#endif
