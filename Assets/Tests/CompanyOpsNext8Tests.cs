using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator Next8Detail_題名の粒と数字の間隔とログの字を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.StartYear(9);game.State.levels[OpsCatalog.Index("education")]=1;game.OpenMailTraining();Click("MinigameStart");
            var mail=(OpsMailMinigame)game.Minigame;
            for(int i=0;i<10&&mail.Current.Id!="account";i++){game.AnswerMail(mail.Current.Suspicious);game.TickMinigame(.6f);}
            Assert.AreEqual("account",mail.Current.Id);
            var shards=Object.FindObjectsByType<OpsMinigameVisual>().Where(v=>v.Kind=="shard").ToArray();Assert.IsNotEmpty(shards);
            foreach(var shard in shards)Assert.Greater(-((RectTransform)shard.transform).anchoredPosition.y-Mathf.Abs(shard.Direction.y)-10,100+94+48,"粒の全移動範囲が題名の下にある");
            foreach(var pop in Object.FindObjectsByType<OpsMinigameVisual>().Where(v=>v.Kind=="pop"))Assert.Greater(-((RectTransform)pop.transform).anchoredPosition.y-60,100+94+48,"成功の文字も題名に重ならない");
            yield return new WaitForSecondsRealtime(.15f);Capture("next8-c-play-hint");
            game.StartYear(14);game.State.levels[OpsCatalog.Index("monitor")]=1;game.ChooseAction("audit");Click("MinigameStart");game.TickMinigame(6);yield return null;
            var count=Find<TextMeshProUGUI>("DecisionGood");StringAssert.Contains(" / ",count.text);Assert.IsFalse(count.enableAutoSizing);count.ForceMeshUpdate();Assert.IsFalse(count.isTextOverflowing);
            foreach(var text in Object.FindObjectsByType<TextMeshProUGUI>().Where(t=>t.name=="LogTime"||t.name=="LogUser"||t.name=="LogIp"||t.name=="LogEvent"))
            {Assert.AreEqual(15,text.fontSize);Assert.IsFalse(text.enableAutoSizing);Assert.AreEqual(FontStyles.Normal,text.fontStyle);}
            Capture("next8-e-play-hint");
            game.StartYear(14);game.State.levels[OpsCatalog.Index("automation")]=1;game.ChooseAction("map");Click("MinigameStart");yield return null;
            count=Find<TextMeshProUGUI>("DecisionMiss");count.ForceMeshUpdate();Assert.IsFalse(count.isTextOverflowing);Assert.IsFalse(count.enableAutoSizing);
            Assert.Less(Find<TextMeshProUGUI>("MinigameTime").rectTransform.anchoredPosition.x+42,count.rectTransform.anchoredPosition.x);
            Capture("next8-f2-play-auto");Assert.IsTrue(game.State.Valid());game.StartYear(14);Assert.IsFalse(game.MinigameActive);Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
