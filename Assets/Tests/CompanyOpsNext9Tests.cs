#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator Next9Isolation_動的な作業フォントはアセットに書き戻さない()
        {
            string root=System.IO.Path.Combine(Application.dataPath,"Fonts/CompanyYear");var before=System.IO.File.ReadAllBytes(root+"/BodyDynamic.asset");var beforeHeading=System.IO.File.ReadAllBytes(root+"/HeadingDynamic.asset");
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();
            Assert.AreNotEqual(HideFlags.None,game.Font.hideFlags);Assert.AreNotEqual(HideFlags.None,game.Font.fallbackFontAssetTable[0].hideFlags);
            game.Font.fallbackFontAssetTable[0].TryAddCharacters("穏絆萌暫",out _);game.StartStory(9);yield return null;
            CollectionAssert.AreEqual(before,System.IO.File.ReadAllBytes(root+"/BodyDynamic.asset"));CollectionAssert.AreEqual(beforeHeading,System.IO.File.ReadAllBytes(root+"/HeadingDynamic.asset"));
            var prefs=new TestPreferenceScope();bool had=PlayerPrefs.HasKey("pws_ops_music");float volume=PlayerPrefs.GetFloat("pws_ops_music");prefs.Snapshot();
            try{PlayerPrefs.SetFloat("pws_ops_music",.123f);}finally{prefs.Restore();}
            Assert.AreEqual(had,PlayerPrefs.HasKey("pws_ops_music"));Assert.AreEqual(volume,PlayerPrefs.GetFloat("pws_ops_music"));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next9Endings_二つの結末と三年の実記録と因子への導線を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            foreach(bool special in new[]{true,false})
            {
                game.StartStory(14);for(int y=1;y<=3;y++){FinishStoryTestYear(game.State);if(y==3&&special){game.State.totalLoss=0;game.State.totalDowntime=0;}if(y==3&&!special)while(game.State.AnnualScore>=OpsCatalog.AnnualS)game.State.totalLoss++;if(y<3)Assert.IsTrue(game.NextStoryYear());}
                game.OpenTab(0);yield return new WaitForSecondsRealtime(3);Capture(special?"next9-ending-ss":"next9-ending-a");yield return null;
                Assert.IsTrue(game.Story.cleared&&game.Career.endlessUnlocked);CheckPointer("StoryRecord");CheckPointer("BackHome");
                if(special){Assert.IsNotNull(Find<Image>("EndingKeyVisual"));Assert.AreEqual(game.Story.records.Sum(r=>r.score).ToString("N0")+"点",Find<TextMeshProUGUI>("EndingTotalScore").text);}
                else {CheckPointer("StoryEndless");Click("StoryEndless");StringAssert.Contains("準備中",Find<TextMeshProUGUI>("DialogBody").text);Click("CloseDialog");}
                var prior=JsonUtility.ToJson(game.State);Click("StoryRecord");yield return null;StringAssert.Contains("3年目",Find<TextMeshProUGUI>("DialogBody").text);Click("StoryTitle");yield return new WaitForSecondsRealtime(1);
                Assert.IsNotNull(Find<Button>("ConfirmFactor"));Assert.AreEqual(prior,JsonUtility.ToJson(game.State));Click("ConfirmFactor");yield return null;Assert.IsTrue(game.Story.rewardClaimed);CheckText();
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next9FactorSelection_明示選択と入替と保存復帰を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.Career.factors.Add("backup");game.StartStory(14);
            FinishStoryTestYear(game.State);game.NextStoryYear();FinishStoryTestYear(game.State);while(OpsStory.RankValue(game.State.RankCode)>=OpsStory.RankValue(game.Story.Goal))game.State.totalLoss++;game.OpenTab(0);yield return new WaitForSecondsRealtime(2);
            Assert.IsFalse(game.Story.rewardClaimed);Assert.AreEqual(1,game.Career.factors.Count);var s=JsonUtility.ToJson(game.State);
            CheckPointer("StoryRecord");Click("StoryRecord");yield return new WaitForSecondsRealtime(2);CheckPointer("ChooseFactor0");CheckPointer("ChooseFactor1");CheckPointer("ChooseFactor2");CheckPointer("ConfirmFactor");Capture("next9-factor");
            Assert.AreEqual(s,JsonUtility.ToJson(game.State));Click("ChooseFactor0");yield return null;var chosen=game.Story.FactorCandidates(game.Career)[0].id;
            Click("ConfirmFactor");yield return new WaitForSecondsRealtime(2);Assert.AreEqual(1,game.Career.finishedAttempts);Assert.AreEqual(2,game.Career.factors.Count);Assert.AreEqual(1,game.State.Level(chosen));
            game.Career.factors.Clear();game.Career.factors.AddRange(new[]{"backup","mfa","inventory"});game.StartStory(9);game.State.stability=0;game.State.phase=OpsPhase.Ended;game.OpenTab(0);yield return new WaitForSecondsRealtime(2);
            Click("BackHome");yield return null;Assert.IsFalse(Find<Button>("ConfirmFactor").interactable);Click("ReplaceFactor1");yield return null;Assert.IsTrue(Find<Button>("ConfirmFactor").interactable);
            string replaced=game.Story.FactorCandidates(game.Career)[1].id;Click("ConfirmFactor");yield return null;Assert.AreEqual(3,game.Career.factors.Count);Assert.AreEqual(replaced,game.Career.factors[1]);Assert.AreEqual(2,game.Career.finishedAttempts);Assert.IsTrue(game.ExportProgress().Valid());CheckText();Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [Test] public void Next9Factors_推薦は未所持と前提不要で星は表示だけ()
        {
            var story=OpsStory.Begin(14,null);FinishStoryTestYear(story.state);Assert.IsTrue(story.AdvanceYear());FinishStoryTestYear(story.state);story.state.totalLoss=1000;story.RecordYear();
            var career=new OpsCareer{factors=new System.Collections.Generic.List<string>{"backup"}};var before=JsonUtility.ToJson(story);var candidates=story.FactorCandidates(career);
            Assert.AreEqual(3,candidates.Length);Assert.AreEqual(3,candidates.Select(c=>c.id).Distinct().Count());Assert.IsTrue(candidates.All(c=>!career.factors.Contains(c.id)&&string.IsNullOrEmpty(OpsCatalog.Projects[OpsCatalog.Index(c.id)].requires)));
            Assert.IsTrue(candidates.All(c=>c.stars==2));Assert.AreEqual(before,JsonUtility.ToJson(story));
            Assert.IsTrue(career.Claim(story,candidates[0].id));Assert.AreEqual(1,OpsStory.Begin(3,career.factors).state.Level(candidates[0].id));
        }
        [UnityTest] public IEnumerator Next9Fail_目標差と痛い月と候補と記録なしを撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.Career.factors.Add("backup");game.StartStory(14);
            FinishStoryTestYear(game.State);Assert.IsTrue(game.NextStoryYear());FinishStoryTestYear(game.State);while(OpsStory.RankValue(game.State.RankCode)>=OpsStory.RankValue(game.Story.Goal))game.State.totalLoss++;game.OpenTab(0);yield return new WaitForSecondsRealtime(3);Capture("next9-year-fail");
            Assert.IsTrue(game.Story.finished&&!game.Story.cleared);StringAssert.Contains("目標 A",Find<TextMeshProUGUI>("StoryGoalResult").text);Find<TextMeshProUGUI>("FailureScore").ForceMeshUpdate();Assert.AreEqual(1,Find<TextMeshProUGUI>("FailureScore").textInfo.lineCount);Assert.IsNotNull(Find<TextMeshProUGUI>("WorstMonthValue"));CheckPointer("StoryRecord");CheckPointer("BackHome");
            game.StartStory(9);game.State.stability=0;game.State.phase=OpsPhase.Ended;game.OpenTab(0);yield return new WaitForSecondsRealtime(2);
            Assert.AreEqual("事件の対応記録なし",Find<TextMeshProUGUI>("WorstMonthValue").text);Assert.IsNotNull(Find<TextMeshProUGUI>("GoodWorkValue"));Assert.IsTrue(game.ExportProgress().Valid());Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next9Renew_全額繰越と設備見直しと年度の道のりを撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartStory(14);
            FinishStoryTestYear(game.State);game.State.budget=38;game.State.culture=68;game.State.trust=71;
            foreach(var p in OpsCatalog.Projects)game.State.levels[OpsCatalog.Index(p.id)]=0;
            foreach(string id in new[]{"backup","drill","runbook"})game.State.levels[OpsCatalog.Index(id)]=2;
            foreach(string id in new[]{"inventory","mfa","automation","education"})game.State.levels[OpsCatalog.Index(id)]=1;
            game.State.capacity=0;Assert.IsTrue(game.State.Valid(),"撮影状態も有効な年度である");game.OpenTab(0);yield return new WaitForSecondsRealtime(3);Capture("next9-year-renew");
            Assert.AreEqual("114<size=15>万円</size>",Find<TextMeshProUGUI>("CarryValue0").text);Assert.AreEqual("58",Find<TextMeshProUGUI>("CarryValue3").text);
            Assert.AreEqual(3,Object.FindObjectsByType<TextMeshProUGUI>().Count(t=>t.name.StartsWith("EquipmentReviewLabel")&&t.text=="見直し"));
            var prior=JsonUtility.ToJson(game.State);CheckPointer("StoryAnnualReview");Click("StoryAnnualReview");yield return new WaitForSecondsRealtime(3);CheckPointer("BackHome");Click("BackHome");yield return new WaitForSecondsRealtime(2);
            Assert.AreEqual(prior,JsonUtility.ToJson(game.State));CheckPointer("NextStoryYear");Click("NextStoryYear");yield return new WaitForSecondsRealtime(2);
            Assert.AreEqual(2,game.Story.year);Assert.AreEqual(114,game.State.budget);Assert.AreEqual(12,game.State.yearPressure);Assert.IsTrue(game.ExportProgress().Valid());Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next9Title_承認済み三つのモードと因子札を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1.2f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            Assert.AreEqual(new Vector2(470,92),Find<Button>("NewYear").GetComponent<RectTransform>().sizeDelta);
            Assert.AreEqual(new Vector2(90,-292),Find<Button>("NewYear").GetComponent<RectTransform>().anchoredPosition);
            Assert.IsFalse(Find<Button>("EndlessYear").interactable);Assert.IsNotNull(Find<TextMeshProUGUI>("EndlessLockText"));
            Capture("next9-title-empty");
            yield return null;CheckPointer("NewYear");CheckPointer("SingleYear");CheckPointer("HomeGuide");CheckPointer("HomeSettings");
            game.Career.factors.Add("backup");game.Career.factors.Add("inventory");game.BuildPreview();
            yield return new WaitForSecondsRealtime(1.2f);Capture("next9-title-mode");
            var label=Find<Button>("NewYear").GetComponentInChildren<TextMeshProUGUI>();label.ForceMeshUpdate();Assert.IsTrue(label.textInfo.characterInfo.Any(c=>c.isVisible),"本編のボタン名が描画されている");
            Assert.AreEqual("因子 2/3",Find<TextMeshProUGUI>("StoryFactors").text);CheckText();
            game.Career.endlessUnlocked=true;game.BuildPreview();yield return null;
            CheckPointer("EndlessYear");Click("EndlessYear");StringAssert.Contains("準備中",Find<TextMeshProUGUI>("DialogBody").text);
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
