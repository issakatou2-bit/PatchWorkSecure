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
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static void ComboCall(OpsGame game,string method)=>typeof(OpsGame).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        [Test] public void Next21Combo_七段階の台本を通常の反応抽選に混ぜない()
        {
            string[] captions={"いいね！","その調子！","すごい！","やるぅ！","てんさいっ！","さいきょー！","パーフェクト！"};
            var lines=OpsReactionBank.ScriptV2();Assert.AreEqual(198,lines.Length);
            for(int n=1;n<=7;n++){var line=lines.Single(l=>l.id=="combo_"+n);Assert.AreEqual(captions[n-1],line.caption);Assert.IsNull(line.clip);Assert.IsFalse(OpsReactionBank.IsGeneralReaction(line));}
            Assert.AreEqual(54,OpsReactionBank.Defaults().Length);
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next21Combo_既存の字幕で七段階と七以上と消音と即時差替を確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(9);game.OpenMailTraining();game.StartMinigame();
            string before=JsonUtility.ToJson(game.State);var original=game.Navigator;var persona=Object.Instantiate(original);
            var bank=ScriptableObject.CreateInstance<OpsReactionBank>();bank.lines=OpsReactionBank.ScriptV2();persona.Reactions=bank;game.Navigator=persona;
            var clip=AudioClip.Create("連鎖の検証用無音",240000,1,24000,false);
            try
            {
                for(int n=1;n<=9;n++)
                {
                    ComboCall(game,"PresentMinigameSuccess");Assert.AreEqual("combo_"+Math.Min(n,7),game.LastReactionId);
                    Assert.AreEqual(game.LastReactionCaption,Find<TextMeshProUGUI>("NavigatorSpeech").text);Assert.IsFalse(game.VoicePlaying);
                    yield return null;Assert.GreaterOrEqual(Find<TextMeshProUGUI>("NavigatorSpeech").maxVisibleCharacters,game.LastReactionCaption.Length);Capture("next21-combo-"+n);
                }
                ComboCall(game,"ResetMinigameSuccess");
                foreach(var line in bank.lines.Where(l=>l.id.StartsWith("combo_")))line.clip=clip;
                ComboCall(game,"PresentMinigameSuccess");yield return null;yield return null;Assert.IsTrue(game.VoicePlaying);
                ComboCall(game,"PresentMinigameSuccess");Assert.IsFalse(game.VoicePlaying,"前の声を直ちに停止");Assert.AreEqual("combo_2",game.LastReactionId);
                ComboCall(game,"PresentMinigameSuccess");yield return null;yield return null;
                Assert.AreEqual("combo_3",game.LastReactionId);Assert.AreEqual(1,game.GetComponents<AudioSource>().Count(s=>s.isPlaying&&s.clip==clip));
                typeof(OpsGame).GetProperty("VoiceEnabled").SetValue(game,false);ComboCall(game,"PresentMinigameSuccess");yield return null;
                Assert.IsFalse(game.VoicePlaying);Assert.AreEqual("やるぅ！",Find<TextMeshProUGUI>("NavigatorSpeech").text);
                typeof(OpsGame).GetProperty("VoiceEnabled").SetValue(game,true);typeof(OpsGame).GetField("muted",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,true);
                ComboCall(game,"PresentMinigameSuccess");yield return null;Assert.IsFalse(game.VoicePlaying);Assert.AreEqual("てんさいっ！",Find<TextMeshProUGUI>("NavigatorSpeech").text);
                Assert.AreEqual(before,JsonUtility.ToJson(game.State));
            }
            finally{game.StopVoice();game.Navigator=original;Object.Destroy(persona);Object.Destroy(bank);Object.Destroy(clip);}
        }
        [UnityTest] public IEnumerator Next21Combo_六本の操作で成功を数え失敗と次のゲームで戻る()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            foreach(string id in OpsDailyPractice.Ids)
            {
                game.StartYear(9);Assert.IsTrue(game.BeginDailyPractice(id,20261003));game.StartMinigame();
                var session=game.Minigame;
                Assert.AreEqual(0,typeof(OpsGame).GetField("minigameSuccessChain",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game),"開始前から動いていた設備は成功に数えない");
                if(session is OpsMailMinigame mail){game.AnswerMail(mail.Current.Suspicious);game.TickMinigame(.6f);Assert.AreEqual("combo_1",game.LastReactionId);game.AnswerMail(!mail.Current.Suspicious);game.TickMinigame(2);game.AnswerMail(mail.Current.Suspicious);}
                else if(session is OpsMfaMinigame mfa){game.AnswerMfa(mfa.Current.Legitimate);game.TickMinigame(1);Assert.AreEqual("combo_1",game.LastReactionId);game.AnswerMfa(!mfa.Current.Legitimate);game.TickMinigame(1);game.AnswerMfa(mfa.Current.Legitimate);}
                else if(session is OpsContainmentMinigame b){game.TickMinigame(.1f);int pc=Enumerable.Range(0,b.PCCount).First(b.Visible);typeof(OpsGame).GetMethod("CutMinigamePC",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,new object[]{pc});}
                else if(session is OpsLogMinigame log){for(int g=0;g<20&&!log.Visible.Any(r=>r.Suspicious);g++)game.TickMinigame(.5f);game.HitLog(log.Visible.First(r=>r.Suspicious).Id);}
                else if(session is OpsBlockMinigame blocks){var t=blocks.Tasks.First();game.SelectBlock(t.Id);game.PlaceSelectedBlock(t.Solution.Min(c=>c.Row),t.Solution.Min(c=>c.Column));}
                else if(session is OpsRestoreMinigame restore){var node=restore.Nodes.First(n=>!n.Up&&n.Need.All(d=>restore.Nodes.First(x=>x.Id==d).Up));game.BootRestore(node.Id);for(int g=0;g<30&&!node.Up;g++)game.TickMinigame(.5f);Assert.IsTrue(node.Up);}
                Assert.AreEqual("combo_1",game.LastReactionId,id);Assert.IsFalse(game.LastReactionId.StartsWith("mg_combo"));
                yield return null;
            }
        }
    }
}
#endif
