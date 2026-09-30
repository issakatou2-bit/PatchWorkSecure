using System.Collections;
using System.IO;
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
        private static void FinishStoryTestYear(OpsState s)
        {
            // 引き継ぎテスト用の強い年度。事件計算と12か月の履歴は実際のルールで作る。
            for(int i=0;i<s.levels.Length;i++)s.levels[i]=2;s.culture=s.trust=100;s.fatigue=0;s.budget=200;
            while(s.phase!=OpsPhase.Ended)
            {
                Assert.IsTrue(s.BeginIncident());
                string response=CompanyOpsPersonaPolicy.Responses.OrderBy(r=>s.Estimate(r).lossMax*7+s.Estimate(r).stopMax*4+s.Estimate(r).cost).First();
                Assert.IsTrue(s.Resolve(response));if(s.QuarterRewardPending)s.ClaimQuarterReward("budget");Assert.IsTrue(s.NextMonth());
            }
            Assert.IsTrue(s.IsClear);Assert.IsTrue(s.Valid());Assert.GreaterOrEqual(OpsStory.RankValue(s.RankCode),OpsStory.RankValue("A"));
        }
        [Test] public void Next8Story_初年度の完全一致と全額繰越と設備見直し()
        {
            var story=OpsStory.Begin(14,new string[0]);Assert.AreEqual(JsonUtility.ToJson(new OpsState(14+OpsCatalog.StorySeedStride,true)),JsonUtility.ToJson(story.state));
            FinishStoryTestYear(story.state);story.state.budget=5000;story.state.culture=84;story.state.trust=83;
            story.state.staffExperience[0]=8;var old=story.state;Assert.IsTrue(story.RecordYear());Assert.IsFalse(story.RecordYear());Assert.IsTrue(story.AdvanceYear());
            Assert.AreEqual(5076,story.state.budget);Assert.AreEqual(64,story.state.trust);Assert.AreEqual(84,story.state.culture);
            CollectionAssert.AreEqual(old.staffExperience,story.state.staffExperience);Assert.AreNotSame(old.staffExperience,story.state.staffExperience);
            Assert.IsTrue(story.state.levels.All(v=>v==1));Assert.AreEqual(12,story.state.yearPressure);Assert.AreEqual(24,story.state.fatigue);Assert.AreEqual(0,story.state.totalLoss);
            CollectionAssert.AreEqual(story.state.ReportMetrics,story.state.monthStartMetrics);Assert.IsTrue(story.Valid());
            FinishStoryTestYear(story.state);Assert.IsTrue(story.AdvanceYear());Assert.AreEqual(24,story.state.yearPressure);Assert.AreEqual(0,story.state.month);Assert.IsTrue(story.Valid());
        }
        [Test] public void Next8Story_目標未達と運営終了と三年クリアを区別する()
        {
            var miss=OpsStory.Begin(9,null);FinishStoryTestYear(miss.state);miss.state.totalLoss=1000;
            Assert.IsTrue(miss.RecordYear());Assert.IsTrue(miss.finished);Assert.IsFalse(miss.cleared);Assert.IsFalse(miss.AdvanceYear());Assert.IsTrue(miss.Valid());Assert.IsTrue(miss.records[0].operated);Assert.IsFalse(miss.records[0].goalMet);
            var ended=OpsStory.Begin(9,null);ended.state.stability=0;ended.state.phase=OpsPhase.Ended;Assert.IsTrue(ended.RecordYear());Assert.IsTrue(ended.Valid());Assert.IsFalse(ended.records[0].operated);
            var clear=OpsStory.Begin(14,null);
            for(int y=1;y<=3;y++){FinishStoryTestYear(clear.state);Assert.IsTrue(clear.RecordYear());if(y<3)Assert.IsTrue(clear.AdvanceYear());}
            Assert.IsTrue(clear.finished&&clear.cleared);Assert.IsFalse(clear.AdvanceYear());Assert.AreEqual(3,clear.records.Count);Assert.IsTrue(clear.Valid());
            var career=new OpsCareer();Assert.IsTrue(career.Claim(clear,"backup"));Assert.IsTrue(career.endlessUnlocked);Assert.IsFalse(career.Claim(clear,"mfa"));Assert.AreEqual(1,career.finishedAttempts);
        }
        [Test] public void Next8Story_因子三枠と次の挑戦だけへの反映()
        {
            var career=new OpsCareer();foreach(string id in new[]{"backup","education","inventory","mfa"})
            {var story=OpsStory.Begin(3,career.factors);story.state.phase=OpsPhase.Ended;story.state.stability=0;story.RecordYear();Assert.IsTrue(career.Claim(story,id,2));Assert.IsTrue(career.Valid());}
            Assert.AreEqual(3,career.factors.Count);Assert.AreEqual(4,career.finishedAttempts);Assert.IsFalse(career.endlessUnlocked);
            var next=OpsStory.Begin(3,career.factors);foreach(string id in career.factors)Assert.AreEqual(1,next.state.Level(id));Assert.IsTrue(next.Valid());
            Assert.IsTrue(new OpsState(3,true).levels.All(v=>v==0));Assert.IsFalse(OpsCareer.ValidFactors(new[]{"backup","backup"}));Assert.IsFalse(OpsCareer.ValidFactors(new[]{"unknown"}));
        }
        [Test] public void Next8Story_年度途中の保存復帰と旧保存と破損を守る()
        {
            string dir=Path.Combine(Application.temporaryCachePath,"next8-save-tests");Directory.CreateDirectory(dir);string path=Path.Combine(dir,"progress.json");
            var story=OpsStory.Begin(71,new[]{"backup"});FinishStoryTestYear(story.state);Assert.IsTrue(story.AdvanceYear());story.state.Act("audit");story.state.BeginIncident();
            var progress=new OpsProgress{story=story,career=new OpsCareer{factors=new System.Collections.Generic.List<string>{"backup"}}};
            Assert.IsTrue(OpsSaveStore.WriteProgress(path,progress,out string warning),warning);var loaded=OpsSaveStore.ReadProgress(path,out warning);Assert.IsNotNull(loaded,warning);
            Assert.AreEqual(JsonUtility.ToJson(progress),JsonUtility.ToJson(loaded));story.state.Resolve("scope");loaded.story.state.Resolve("scope");Assert.AreEqual(JsonUtility.ToJson(story.state),JsonUtility.ToJson(loaded.story.state));
            Assert.IsTrue(OpsSaveStore.WriteProgress(path,loaded,out warning),warning);Assert.IsNotNull(OpsSaveStore.ReadProgress(path+".bak",out warning));
            loaded.story.state.yearPressure=0;Assert.IsFalse(OpsSaveStore.WriteProgress(path,loaded,out warning));Assert.IsNotNull(OpsSaveStore.ReadProgress(path,out warning));
            var single=new OpsState(88,true);single.Act("listen");Assert.IsTrue(OpsSaveStore.Write(path,single,out warning));var legacy=OpsSaveStore.ReadProgress(path,out warning);Assert.IsNotNull(legacy,warning);
            Assert.IsNull(legacy.story);Assert.AreEqual(0,legacy.single.yearPressure);Assert.AreEqual(JsonUtility.ToJson(single),JsonUtility.ToJson(legacy.single));
            var oneYear=new OpsProgress{single=single,career=new OpsCareer{endlessUnlocked=true,finishedAttempts=1,factors=new System.Collections.Generic.List<string>{"education"}}};
            Assert.IsTrue(OpsSaveStore.WriteProgress(path,oneYear,out warning),warning);var switched=OpsSaveStore.ReadProgress(path,out warning);Assert.IsNotNull(switched,warning);Assert.IsNull(switched.story);
            Assert.IsTrue(switched.career.endlessUnlocked);CollectionAssert.AreEqual(oneYear.career.factors,switched.career.factors);Assert.AreEqual(JsonUtility.ToJson(single),JsonUtility.ToJson(switched.single));
            File.WriteAllText(path,"{broken");Assert.IsNull(OpsSaveStore.ReadProgress(path,out warning));Assert.IsNotEmpty(warning);Assert.AreEqual("{broken",File.ReadAllText(path));
        }
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
