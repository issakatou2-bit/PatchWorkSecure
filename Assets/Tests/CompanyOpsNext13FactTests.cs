#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [Test] public void Next13Fact_ログ1200通りの出題順を変えずIPだけ例示用にする()
        {
            var trace=new StringBuilder();
            for(int seed=1;seed<=100;seed++)for(int month=0;month<12;month++)
            {
                var game=new OpsLogMinigame(new OpsState(seed,true){month=month});
                foreach(var r in game.Queue)
                {
                    trace.Append(seed+":"+month+":"+r.Id+":"+r.Kind+":"+r.Time+":"+r.User+":"+r.Event+"\n");
                    if(r.Kind==0||r.Kind==1||r.Kind==6)StringAssert.StartsWith("198.51.100.",r.Ip);
                }
            }
            using(var sha=SHA256.Create())Assert.AreEqual("49507ADEC99519FDC6B29C2B3A4CD7118473EB16FC6275A1CCDBFB1EB1634A6B",BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(trace.ToString()))).Replace("-",""));
        }
        [Test] public void Next13Fact_点検の指定文と新しい格言IDを使う()
        {
            var macro=OpsMailMinigame.Catalog.Single(q=>q.Id=="macro");Assert.AreEqual("取引先 西村様",macro.Sender);Assert.AreEqual("nishimura@partner-trade.co.jp",macro.Address);
            Assert.AreEqual("アドレスの最後が hr-update.net。最後の部分が本当のドメイン（住所）。表示の名前や途中の文字は偽れる",OpsMailMinigame.Catalog.Single(q=>q.Id=="salary").Clue);
            var bank=OpsReactionBank.ScriptV2();Assert.AreEqual(OpsCatalog.VoiceScriptLineCount,bank.Length);Assert.IsFalse(bank.Any(l=>l.id=="maxim_hurry"||l.id=="maxim_link"));
            StringAssert.Contains("よくある合図",bank.Single(l=>l.id=="maxim_hurry_v2").caption);StringAssert.Contains("似た名前",bank.Single(l=>l.id=="maxim_link_v2").caption);
            Assert.AreEqual("暗号化と持ち出しは別々に調べる。戻す前に、安全を確かめる。",OpsEventCatalog.Event("y3-final").hint);
            var places=(string[])typeof(OpsMfaMinigame).GetField("ForeignPlaces",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            CollectionAssert.AreEqual(new[]{"海外（普段と違う国）","海外のクラウド事業者","匿名化の通信網","海外（不明）"},places);
            var mfa=(OpsMfaMinigame)OpsDailyPractice.Create("D",20261002);mfa.Start();
            while(mfa.Current.Legitimate){mfa.Answer(true);mfa.Tick(1);}
            int who=mfa.Current.Who;mfa.Answer(true);Assert.AreEqual("（パスワードの変更とセッションの失効が必要）",mfa.People[who].Activity);
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next13FactUI_格言は新しい字幕を表示し古い音声を再生しない()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();WithoutPublishedVoices(game);
            foreach(string id in new[]{"maxim_hurry","maxim_link"})
            {
                Assert.IsTrue(game.BeginDailyPractice("C",20261002));game.StartMinigame();var mail=(OpsMailMinigame)game.Minigame;FinishDailyForTest(mail);
                typeof(OpsMailMinigame).GetProperty("ResultMaxim").SetValue(mail,id);game.TickMinigame(0);yield return new WaitForSecondsRealtime(3.8f);
                Assert.AreEqual(id+"_v2",game.LastReactionId);Assert.IsFalse(game.VoicePlaying);Assert.IsNull(game.ActiveVoiceBank.Find(id));
                var label=Find<TextMeshProUGUI>("MinigameMaxim");label.ForceMeshUpdate();Assert.IsFalse(label.isTextOverflowing);Assert.AreEqual(OpsGame.SpeechLines(game.LastReactionCaption),label.text);Capture("next13-fact-"+id);game.ConfirmMinigame();
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
