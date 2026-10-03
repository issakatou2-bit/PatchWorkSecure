using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator Next4Voice_追加十二行の字幕ポーズと素材なしの進行と年度一回を確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);
            Assert.AreEqual(OpsCatalog.VoiceScriptLineCount,game.ActiveVoiceBank.lines.Length);Assert.IsFalse(game.ActiveVoiceBank.HasAudio);
            foreach(var line in OpsReactionBank.ScriptV2().Skip(105).Take(12))
            {
                var actual=game.ActiveVoiceBank.Find(line.id);Assert.AreEqual(line.caption,actual.caption);Assert.AreEqual(line.poseId,actual.poseId);Assert.AreEqual(line.faceId,actual.faceId);
                game.SpeakSceneLine(line.id,0);yield return new WaitForSecondsRealtime(.1f);
                Assert.AreEqual(OpsGame.SpeechLines(line.caption),Find<TextMeshProUGUI>("NavigatorSpeech").text);Assert.AreEqual(line.poseId,Find<OpsPortraitAnimator>("NavigatorPortrait").PoseId);
                Assert.IsFalse(game.PortraitVoicePlaying);game.StopVoice();
            }
            foreach(int month in OpsCatalog.PeakMonths){game.StartYear(14);game.State.month=month;game.OpenTab(0);Assert.AreEqual("peak_goal_"+(((month+3)%12)+1).ToString("00"),game.LastReactionId);}
            game.StartYear(14);game.State.budget=0;game.State.culture=100;game.State.trust=80;game.State.fatigue=0;for(int i=0;i<game.State.levels.Length;i++)game.State.levels[i]=2;
            // 閾値の更新後も、予算の得点加算上限200に収まる差で試験用の状態を作る。
            for(int attempt=0;attempt<100&&(game.State.NextRankPoints<40||game.State.NextRankPoints>200);attempt++)game.State.totalLoss++;
            Assert.That(game.State.NextRankPoints,Is.InRange(40,200));
            game.State.budget+=game.State.NextRankPoints-40;Assert.AreEqual(40,game.State.NextRankPoints);game.StopVoice();yield return new WaitForSecondsRealtime(.5f);
            Assert.AreEqual("next_rank",game.LastReactionId);Assert.IsTrue(game.State.nextRankVoicePlayed);
            var loaded=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(game.State));Assert.IsTrue(loaded.Valid());Assert.IsTrue(loaded.nextRankVoicePlayed);
            game.StopVoice();game.SpeakSceneLine("think_01",0);game.StopVoice();yield return new WaitForSecondsRealtime(.5f);Assert.AreEqual("think_01",game.LastReactionId);
            LogAssert.NoUnexpectedReceived();
        }
        [Test] public void Next4Voice_達成未達は別々に交互で決算は専用の声()
        {
            var state=new OpsState(14);Assert.AreEqual("",OpsGame.PeakResultVoiceId(state));
            state.history.Add(new OpsOutcome{month=2,peakGoalRecorded=true,peakGoalMet=true});Assert.AreEqual("peak_clear_01",OpsGame.PeakResultVoiceId(state));
            state.history.Add(new OpsOutcome{month=5,peakGoalRecorded=true});Assert.AreEqual("peak_miss_01",OpsGame.PeakResultVoiceId(state));
            state.history.Add(new OpsOutcome{month=8,peakGoalRecorded=true,peakGoalMet=true});Assert.AreEqual("peak_clear_02",OpsGame.PeakResultVoiceId(state));
            state.history.Add(new OpsOutcome{month=11,peakGoalRecorded=true,peakGoalMet=true});Assert.AreEqual("peak_clear_final",OpsGame.PeakResultVoiceId(state));
            state.history.Last().peakGoalMet=false;Assert.AreEqual("peak_miss_02",OpsGame.PeakResultVoiceId(state));
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next4UI_山場の予測と達成未達と五段の評価を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartYear(14);
            game.State.month=2;game.OpenTab(0);while(game.PhasePresentationRunning)yield return null;yield return new WaitForSecondsRealtime(.3f);
            Assert.IsNotNull(Find<TextMeshProUGUI>("NextRankPoints"));CheckPointer("PeakGoal_2");Click("PeakGoal_2");yield return null;
            StringAssert.Contains("公開見積もり",Find<TextMeshProUGUI>("DialogBody").text);StringAssert.Contains("8万円",Find<TextMeshProUGUI>("DialogBody").text);
            CheckText();yield return new WaitForSecondsRealtime(.3f);Capture("140-peak-goal-prospect");Click("CloseDialog");yield return null;
            game.BeginIncident();while(game.PhasePresentationRunning)yield return null;yield return new WaitForSecondsRealtime(.3f);
            StringAssert.Contains("6月",Find<TextMeshProUGUI>("IncidentPeakGoalText").text);StringAssert.Contains("相談の泡",Find<TextMeshProUGUI>("KnowledgeHow").text);
            foreach(string id in new[]{"contain","scope","recover"})StringAssert.StartsWith("予測",Find<TextMeshProUGUI>("PeakForecastText_"+id).text);
            CheckText();Capture("141-peak-incident-goal");
            // テストでは真相を使って未達の固定ケースを選ぶ。プレイヤーの見込みには使用しない。
            int seed=game.State.seed;while(game.State.Preview("recover").loss<=OpsCatalog.JuneLossGoal&&game.State.Preview("recover").downtime<=OpsCatalog.JuneStopGoal)game.State.seed=++seed;
            game.Resolve("recover");yield return WaitForResolution(game);yield return new WaitForSecondsRealtime(.5f);
            Assert.IsFalse(game.State.Latest.peakGoalMet);StringAssert.Contains("未達",Find<TextMeshProUGUI>("PeakResultText").text);
            Assert.AreEqual(new Vector2(1250,-784),Find<RectTransform>("PeakResultBadge").anchoredPosition);
            Assert.AreEqual("四半期の報酬を選ぶ",Find<Button>("NextMonth").GetComponentInChildren<TextMeshProUGUI>().text);
            CheckText();Capture("142-peak-miss");
            game.StartYear(14);game.State.month=2;game.State.culture=100;game.State.fatigue=0;
            for(int i=0;i<game.State.levels.Length;i++)game.State.levels[i]=2;
            game.OpenTab(0);game.BeginIncident();game.Resolve("scope");Assert.IsTrue(game.State.Latest.peakGoalMet);
            bool captured=false;
            for(float t=0;t<12&&game.ResolutionActive;t+=.025f)
            {
                var caption=Object.FindObjectsByType<TextMeshProUGUI>().FirstOrDefault(l=>l.name=="CutinCaption"&&l.text=="山場の目標達成！");
                if(caption!=null&&!captured&&Mathf.Abs(((RectTransform)caption.transform.parent).anchoredPosition.x-380)<60){Capture("143-peak-shield-cutin");captured=true;}
                yield return new WaitForSecondsRealtime(.025f);
            }
            Assert.IsTrue(captured);yield return new WaitForSecondsRealtime(.5f);CheckText();Capture("144-peak-clear");
            game.State.phase=OpsPhase.Ended;
            game.State.completedMissions=Enumerable.Range(0,12).ToList();
            foreach(var pair in new[]{new{score=1000,rank="C"},new{score=OpsCatalog.AnnualB,rank="B"},new{score=OpsCatalog.AnnualA,rank="A"},new{score=OpsCatalog.AnnualS,rank="S"},new{score=OpsCatalog.AnnualSS,rank="SS"}})
            {
                // 得点だけを表示用に構成。年間のルールを変更しない。
                game.State.totalLoss=0;game.State.totalDowntime=0;game.State.budget=0;
                int baseScore=game.State.AnnualScore;
                game.State.totalLoss=Mathf.Max(0,(baseScore-pair.score+6)/7);
                game.State.budget=pair.score-(baseScore-game.State.totalLoss*7);
                game.OpenTab(0);yield return new WaitForSecondsRealtime(1.2f);
                Assert.AreEqual(pair.rank,game.State.RankCode);Assert.AreEqual(pair.rank,Find<TextMeshProUGUI>("CompanyRank").text);
                Assert.IsNotNull(Find<RectTransform>("PeakMedal0"));Assert.AreSame(game.PlanningArt.logoIcon,Find<Image>("PeakShield").sprite);
                Assert.Greater(Find<RectTransform>("PeakMedalHeading").GetSiblingIndex(),Find<RectTransform>("AnnualTimeline").GetSiblingIndex());
                CheckText();Capture("145-annual-"+pair.rank);
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [Test] public void Next4UI_吹き出しの改行後に全角空白を残さない()
        {
            Assert.AreEqual("春だね！\n桜もきれいだし、\n準備しよ。",OpsGame.SpeechLines("春だね！　桜もきれいだし、　準備しよ。"));
            Assert.AreEqual("一行目\n二行目\n三行目",OpsGame.SpeechLines("一行目\r\n　　二行目\n 三行目"));
        }
        [Test] public void Next4Rules_山場報酬は一度だけで旧年度を変更しない()
        {
            foreach (int month in OpsCatalog.PeakMonths)
            foreach (bool prepared in new[] { false, true })
            {
                var s = new OpsState(14, true); var old = new OpsState(14, true) { peakGoalRules = 0 };
                // 両年度を同じ操作で山場の月まで進める。
                for (int m=0; m<month; m++) { s.budget=old.budget=1000; s.stability=old.stability=100; s.BeginIncident(); s.Resolve("scope"); s.NextMonth(); old.BeginIncident(); old.Resolve("scope"); old.NextMonth(); }
                // 途中の報酬差を除外し、今回の確定結果の差だけを比較する。
                s.budget=old.budget=1000; s.stability=old.stability=100; s.trust=old.trust=45; s.fatigue=old.fatigue=0;
                if(prepared) { for(int i=0;i<s.levels.Length;i++) s.levels[i]=old.levels[i]=2; s.culture=old.culture=100; }
                s.BeginIncident(); old.BeginIncident(); Assert.IsTrue(s.Resolve("scope")); Assert.IsTrue(old.Resolve("scope"));
                var result=s.Latest; int peak=s.PeakIndex(month);
                Assert.IsTrue(result.peakGoalRecorded); Assert.IsFalse(old.Latest.peakGoalRecorded);
                Assert.AreEqual(result.loss<=OpsCatalog.PeakLossGoals[peak]&&result.downtime<=OpsCatalog.PeakStopGoals[peak],result.peakGoalMet);
                Assert.AreEqual(old.budget+result.peakBudgetBonus,s.budget);
                Assert.AreEqual(Mathf.Clamp(old.trust+result.peakTrustChange,0,100),s.trust);
                Assert.AreEqual(result.peakGoalMet?OpsCatalog.PeakScoreReward:0,result.peakScoreBonus);
                int budget=s.budget,score=s.AnnualScore; Assert.IsFalse(s.Resolve("scope")); Assert.AreEqual(budget,s.budget); Assert.AreEqual(score,s.AnnualScore);
                var loaded=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(s)); Assert.IsTrue(loaded.Valid()); Assert.AreEqual(s.PeakScore,loaded.PeakScore);
                loaded.Latest.peakScoreBonus++; Assert.IsFalse(loaded.Valid());
            }
            var legacy=JsonUtility.FromJson<OpsState>("{\"seed\":14,\"levels\":[0,0,0,0,0,0,0,0,0,0,0]}");
            Assert.IsTrue(legacy.Valid()); Assert.AreEqual(0,legacy.peakGoalRules); Assert.AreEqual(-1,legacy.PeakIndex(2));
        }
        [Test] public void Next4Rules_五段の境界と見込みは公開の幅だけを使う()
        {
            var s=new OpsState(14,true);
            foreach(int threshold in new[]{OpsCatalog.AnnualB,OpsCatalog.AnnualA,OpsCatalog.AnnualS,OpsCatalog.AnnualSS})
                Assert.AreNotEqual(s.RankAtScore(threshold-1),s.RankAtScore(threshold));
            CollectionAssert.AreEqual(new[]{"C","B","A","S","SS"},new[]{0,OpsCatalog.AnnualB,OpsCatalog.AnnualA,OpsCatalog.AnnualS,OpsCatalog.AnnualSS}.Select(s.RankAtScore));
            Assert.AreEqual("A",new OpsState(14){peakGoalRules=0}.RankAtScore(1350));
            foreach(int month in OpsCatalog.PeakMonths)
            {
                int i=s.PeakIndex(month),loss=OpsCatalog.PeakLossGoals[i],stop=OpsCatalog.PeakStopGoals[i];
                Assert.AreEqual(2,s.PeakProspect(month,new OpsEstimate{lossMax=loss,stopMax=stop}));
                Assert.AreEqual(1,s.PeakProspect(month,new OpsEstimate{lossMax=loss+1,stopMax=stop}));
                Assert.AreEqual(0,s.PeakProspect(month,new OpsEstimate{lossMin=loss+1,lossMax=loss+1}));
            }
            Assert.IsFalse(s.history.Any()); Assert.IsFalse(s.nextRankVoicePlayed);
        }
    }
}
