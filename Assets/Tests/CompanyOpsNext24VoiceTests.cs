#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private readonly List<Action> noVoiceFixtureCleanup=new List<Action>();
        // 実アセットを消さず、音声未配置の回帰条件を専用コピーで残す。
        private void WithoutPublishedVoices(OpsGame game)
        {
            game.StopVoice();var original=game.Navigator;var persona=Object.Instantiate(original);
            var bank=Object.Instantiate(original.Reactions);bank.name="音声なしの検証専用";
            foreach(var line in bank.lines)line.clip=null;
            persona.Reactions=bank;game.Navigator=persona;game.UseLocalTestVoices=false;
            noVoiceFixtureCleanup.Add(()=>{if(game!=null){game.StopVoice();game.Navigator=original;}Object.DestroyImmediate(persona);Object.DestroyImmediate(bank);});
        }
        [Test] public void Next24Voice_公開197本の一覧と台本と取り込み設定が一致し情シスの行だけ除外する()
        {
            var ids=File.ReadAllLines("Docs/Voice/hinata-h09-picks.csv").Skip(1).Where(l=>!string.IsNullOrWhiteSpace(l)).Select(l=>l.Split(',')[0]).ToArray();
            var bank=(OpsReactionBank)VoiceEditorType("UnityEditor.AssetDatabase").GetMethod("LoadAssetAtPath",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{"Assets/Personas/HinataReactions.asset",typeof(OpsReactionBank)});
            Assert.AreEqual(197,ids.Length);Assert.AreEqual(197,ids.Distinct().Count());Assert.AreEqual(198,bank.lines.Length);
            CollectionAssert.AreEquivalent(ids,bank.lines.Where(l=>l.id!="tutorial_1").Select(l=>l.id));
            Assert.IsNull(bank.Find("tutorial_1").clip);Assert.IsFalse(File.Exists("Assets/Audio/CompanyYear/Voice/Hinata/tutorial_1.wav"));
            string settings=File.ReadAllText("Assets/Audio/CompanyYear/Voice/Kanon/kanon_opening.wav.meta").Split(new[]{"AudioImporter:"},StringSplitOptions.None)[1].Replace("\r","");
            foreach(string id in ids){var line=bank.Find(id);Assert.IsNotNull(line.clip,id);Assert.AreEqual(id,line.clip.name);Assert.Greater(line.clip.samples,0,id);
                string meta=File.ReadAllText("Assets/Audio/CompanyYear/Voice/Hinata/"+id+".wav.meta").Split(new[]{"AudioImporter:"},StringSplitOptions.None)[1].Replace("\r","");Assert.AreEqual(settings,meta,id);}
        }
        [UnityTest,Timeout(180000)] public IEnumerator Next24Voice_197行が公開側で鳴り私的優先に戻らず連鎖も重ならない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);game.SkipTutorial();yield return new WaitForSecondsRealtime(3);game.StopVoice();
            game.UseLocalTestVoices=true;string before=JsonUtility.ToJson(game.State);var bank=game.Navigator.Reactions;
            foreach(var line in bank.lines.Where(l=>l.id!="tutorial_1"))
            {
                game.Career.PrepareMaximYear(game.State.seed,game.RunYear,true);
                Assert.AreSame(line.clip,game.ActiveVoiceBank.Find(line.id).clip,line.id);
                Assert.IsTrue(game.SpeakSceneLine(line.id,0));yield return new WaitForSecondsRealtime(.06f);
                Assert.AreEqual(line.id,game.LastReactionId);Assert.AreEqual(line.caption,game.LastReactionCaption);
                var source=game.GetComponents<AudioSource>().Single(s=>s.clip==line.clip);
                Assert.IsTrue(source.isPlaying,line.id);Assert.IsTrue(game.PortraitVoicePlaying,line.id);game.StopVoice();
            }
            var local=Resources.Load<OpsReactionBank>("HinataVoiceTest");Assert.AreSame(local?.Find("tutorial_1")?.clip,game.ActiveVoiceBank.Find("tutorial_1").clip);
            game.UseLocalTestVoices=false;Assert.IsNull(game.ActiveVoiceBank.Find("tutorial_1").clip);
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));game.OpenMailTraining();game.StartMinigame();before=JsonUtility.ToJson(game.State);
            ComboCall(game,"ResetMinigameSuccess");
            for(int n=1;n<=9;n++){ComboCall(game,"PresentMinigameSuccess");yield return null;yield return null;
                string id="combo_"+Math.Min(n,7);Assert.AreEqual(id,game.LastReactionId);Assert.IsTrue(game.VoicePlaying);
                Assert.AreSame(bank.Find(id).clip,game.GetComponents<AudioSource>().Single(s=>s.isPlaying&&bank.lines.Any(l=>l.clip!=null&&l.clip==s.clip)).clip);}
            // Windows版の検証と同じ、実際の回答操作でも九回続けて接続を確認する。
            ComboCall(game,"ResetMinigameSuccess");var mail=game.Minigame as OpsMailMinigame;Assert.IsNotNull(mail);
            for(int n=1;n<=9;n++)
            {
                float limit=Time.realtimeSinceStartup+5;
                while(!mail.CanAnswer&&mail.Phase==OpsMinigamePhase.Playing&&Time.realtimeSinceStartup<limit)yield return null;
                Assert.IsTrue(mail.CanAnswer);game.AnswerMail(mail.Current.Suspicious);yield return new WaitForSecondsRealtime(.1f);
                string id="combo_"+Math.Min(n,7);Assert.AreEqual(id,game.LastReactionId);Assert.AreEqual(n,mail.Correct);
                Assert.AreSame(bank.Find(id).clip,game.GetComponents<AudioSource>().Single(s=>s.isPlaying&&bank.lines.Any(l=>l.clip!=null&&l.clip==s.clip)).clip);
            }
            Assert.AreEqual(before,JsonUtility.ToJson(game.State));game.StopVoice();LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
