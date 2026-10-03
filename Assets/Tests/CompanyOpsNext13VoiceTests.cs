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
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static void CompanionCall(OpsGame game,string scene,string target="NavigatorSpeech")=>typeof(OpsGame).GetMethod("QueueCompanionScene",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{scene,target,false});
        [Test] public void Next13Voices_台本24本が場面と字幕と元音声に一対一で対応する()
        {
            var bank=Resources.Load<OpsReactionBank>("CompanionVoices");Assert.IsNotNull(bank);Assert.AreEqual(24,bank.lines.Length);
            string[] scenes={"opening","budget","investigate","rival_bec","rival","factor_kanon","factor_engineer","year_clear","year_fail","ending","meeting"};
            CollectionAssert.AreEquivalent(bank.lines.Select(l=>l.id),scenes.SelectMany(OpsGame.CompanionSceneLines));
            foreach(var line in bank.lines)
            {
                Assert.IsNotEmpty(line.caption);Assert.Contains(line.speaker,new[]{"かのん","りりぃ"});Assert.IsTrue(line.fullSpeech);
                // 音声を除いた取得環境も許容。音源がある場合は対応と長さを確認する。
                if(line.clip!=null){Assert.AreEqual(line.id,line.clip.name);Assert.Greater(line.clip.length,.1f);}
            }
            Assert.AreEqual("今日から、情シス。……席、どこ？",bank.Find("eng_opening").caption);
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next13Voices_24本を同じ音源で再生しひなたと重ならず状態を変えない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);game.SkipTutorial();yield return new WaitForSecondsRealtime(3);
            string before=JsonUtility.ToJson(game.State);var bank=Resources.Load<OpsReactionBank>("CompanionVoices");
            foreach(var line in bank.lines)
            {
                Assert.IsTrue(game.SpeakSceneLine("think_01",0));Assert.IsTrue(game.SpeakSceneLine(line.id,0));yield return new WaitForSecondsRealtime(.15f);
                Assert.AreEqual(line.id,game.LastReactionId);Assert.AreEqual(line.speaker,game.CurrentSpeaker);Assert.IsFalse(game.PortraitVoicePlaying,"仲間の声でひなたの口を動かさない");
                Assert.AreEqual(line.speaker,Find<TextMeshProUGUI>("NavigatorName").text);
                var caption=Find<TextMeshProUGUI>("NavigatorSpeech");caption.ForceMeshUpdate();Assert.GreaterOrEqual(caption.maxVisibleCharacters,caption.textInfo.characterCount,"前のひなたの表示文字数で切らない");
                if(line.clip!=null){Assert.IsTrue(game.VoicePlaying,line.id);Assert.AreEqual(1,game.GetComponents<AudioSource>().Count(s=>s.isPlaying&&bank.lines.Any(l=>l.clip==s.clip)),line.id);}
                var face=Find<RawImage>("CompanionFace");Assert.IsNotNull(face.texture);Assert.Less(face.uvRect.width,.5f);Assert.IsNotNull(face.GetComponentInParent<Mask>());
                if(line.id=="eng_investigate")Capture("next13-engineer-planning");
                if(line.id=="kanon_peak_budget")Capture("next13-kanon-planning");
                game.StopVoice();Assert.IsFalse(game.VoicePlaying);Assert.IsFalse(game.VoicePending);
            }
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));game.SpeakSceneLine("think_01",0);yield return null;
            Assert.AreEqual("ひなた",Find<TextMeshProUGUI>("NavigatorName").text);Assert.IsFalse(Object.FindObjectsByType<RawImage>().Any(r=>r.name=="CompanionFace"));
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next13Voices_音声なしでも会話が順に進み字幕オフと操作中断が効く()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.StartYear(14);game.SkipTutorial();yield return new WaitForSecondsRealtime(1);
            var bank=Object.Instantiate(Resources.Load<OpsReactionBank>("CompanionVoices"));foreach(var line in bank.lines)line.clip=null;game.CompanionVoices=bank;
            try
            {
                game.StopVoice();CompanionCall(game,"opening");Assert.AreEqual("kanon_opening",game.LastReactionId);
                yield return new WaitForSecondsRealtime(2.65f);Assert.AreEqual("kanon_opening",game.LastReactionId);
                yield return new WaitForSecondsRealtime(.4f);Assert.AreEqual("eng_opening",game.LastReactionId);Assert.IsFalse(game.VoicePlaying);
                StringAssert.Contains("席",Find<TextMeshProUGUI>("NavigatorSpeech").text);
                typeof(OpsGame).GetProperty("CaptionsEnabled").SetValue(game,false);game.SpeakSceneLine("eng_found",0);yield return null;Assert.IsEmpty(Find<TextMeshProUGUI>("NavigatorSpeech").text);
                typeof(OpsGame).GetProperty("CaptionsEnabled").SetValue(game,true);
                typeof(OpsGame).GetProperty("VoiceEnabled").SetValue(game,false);game.CompanionVoices=Resources.Load<OpsReactionBank>("CompanionVoices");game.SpeakSceneLine("kanon_meeting",0);yield return new WaitForSecondsRealtime(.2f);Assert.IsFalse(game.VoicePlaying);StringAssert.Contains("会議",Find<TextMeshProUGUI>("NavigatorSpeech").text);
                Click("Menu");yield return null;Assert.IsFalse(game.VoicePending);Assert.IsFalse(game.VoicePlaying);
                Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
            finally{Object.Destroy(bank);}
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next13Voices_開幕の自動遷移で会話を保ち会議と調査と山場の予算から呼ぶ()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            var story=AlliesStory(2);game.RestoreProgress(new OpsProgress{story=story});game.PreviewYearOpening(3,false);yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual("kanon_opening",game.LastReactionId);Capture("next13-opening-allies-voice");yield return new WaitForSecondsRealtime(1.6f);
            Assert.AreEqual(4,game.YearOpeningStage);Assert.AreEqual("kanon_opening",game.LastReactionId,"2.2秒で長い台詞を切らない");
            yield return new WaitForSecondsRealtime(3);Assert.IsFalse(game.YearOpeningActive);Assert.IsTrue(game.VoicePlaying||game.VoicePending);Capture("next13-opening-carry-voice");
            game.StopVoice();game.RequestEngineer();yield return new WaitForSecondsRealtime(4);Assert.That(game.LastReactionId,Does.StartWith("eng_"));
            story=AlliesStory(2);for(int m=0;m<2;m++){DiaryTestMonth(story.state);story.state.NextMonth();}story.state.Act("audit");
            Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));game.StopVoice();game.ChooseAction("proposal","protect");Assert.IsTrue(game.State.proposed);
            yield return new WaitForSecondsRealtime(4);Assert.That(game.LastReactionId,Does.StartWith("kanon_"));
            var caption=Find<TextMeshProUGUI>("NavigatorSpeech");caption.ForceMeshUpdate();Assert.IsTrue(caption.enabled);Assert.GreaterOrEqual(caption.maxVisibleCharacters,caption.textInfo.characterCount);Assert.IsNotEmpty(caption.text);Capture("next13-peak-budget-voice");
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next13Voices_年度結果と強敵と会議と因子と特別結末に接続する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            // ひなたの未配置条件を保ち、ここではかのん・りりぃの実音声の接続を確かめる。
            WithoutPublishedVoices(game);
            var story=AlliesStory(3);DiaryTestMonth(story.state);story.state.NextMonth();Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));
            yield return new WaitForSecondsRealtime(4.4f);Assert.AreEqual("kanon_meeting",game.LastReactionId);Capture("next13-meeting-voice");
            DiaryTestMonth(story.state);story.state.NextMonth();game.OpenTab(0);game.BeginIncident();yield return new WaitForSecondsRealtime(6.5f);
            Assert.IsNotNull(game.State.CurrentBoss);Assert.That(game.LastReactionId,Is.EqualTo("kanon_react_hmm").Or.EqualTo("kanon_rival").Or.EqualTo("eng_rival_reveal"));Capture("next13-rival-voice");
            story=AlliesStory(2);FinishStoryTestYear(story.state);Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual("kanon_year_clear",game.LastReactionId);Capture("next13-year-clear-voice");
            story=OpsStory.Begin(14,null);FinishStoryTestYear(story.state);story.state.totalLoss=1000;Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));yield return new WaitForSecondsRealtime(1);
            Assert.AreEqual("kanon_year_fail",game.LastReactionId);Capture("next13-year-fail-voice");
            Click("StoryRecord");yield return new WaitForSecondsRealtime(4);game.SkipFactorReveal();yield return null;Click("ChooseFactor0");yield return new WaitForSecondsRealtime(.8f);Assert.AreEqual("eng_factor",game.LastReactionId);Capture("next13-factor-engineer-voice");
            Click("ChooseFactor2");yield return new WaitForSecondsRealtime(.8f);Assert.AreEqual("kanon_factor",game.LastReactionId);Capture("next13-factor-kanon-voice");
            story=AlliesStory(3);FinishStoryTestYear(story.state);
            // 結末の台詞接続の撮影用。ルール側のランクと記録を合わせる。
            story.state.totalLoss=story.state.totalDowntime=0;Assert.IsTrue(story.RecordYear());
            Assert.AreEqual("SS",story.state.RankCode);Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));yield return new WaitForSecondsRealtime(4);Assert.AreEqual("kanon_ending_ss",game.LastReactionId);Capture("next13-special-ending-voice");
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
