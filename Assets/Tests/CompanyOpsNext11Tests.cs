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
        [UnityTest] public IEnumerator Next11Opening_五場面を年別データで撮影しクリックとスキップを確かめる()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartStory(14);
            string[] stages={"growth","unlock","rivals","allies","title"};
            for(int year=2;year<=3;year++)
            {
                FinishStoryTestYear(game.State);Assert.IsTrue(game.NextStoryYear());Assert.IsTrue(game.YearOpeningActive);Assert.AreEqual(0,game.YearOpeningStage);Assert.Contains(year,game.Career.seenOpeningYears);
                var original=JsonUtility.ToJson(game.State);
                for(int i=0;i<5;i++)
                {
                    game.PreviewYearOpening(i);yield return new WaitForSecondsRealtime(1.95f);Capture("next11-"+stages[i]+(year==3?"-y3":""));
                    Assert.AreEqual(original,JsonUtility.ToJson(game.State));CheckPointer("OpeningAdvance");CheckPointer("OpeningSkip");
                    if(i==0){Assert.AreEqual(OpsCatalog.CompanyYear(year).employees.ToString(),Find<TextMeshProUGUI>("OpeningCountAfter0").text);Assert.IsNotNull(Find<Image>("OpeningLocationArt").material.shader);}
                    if(i==1)Assert.AreEqual(OpsCatalog.CompanyYear(year).equipment[0].name,Find<TextMeshProUGUI>("OpeningEquipmentName0").text);
                    if(i==3)Assert.AreEqual(OpsGrowthCatalog.StaffNames[0]+" Lv"+game.State.StaffLevel(0),Find<TextMeshProUGUI>("OpeningStaff0Text").text);
                    foreach(var t in Object.FindObjectsByType<TextMeshProUGUI>().Where(t=>t.name.StartsWith("Opening"))){t.ForceMeshUpdate();Assert.IsFalse(t.isTextOverflowing,t.name+" / "+t.text);}
                }
                game.PreviewYearOpening(0);yield return null;Find<Button>("OpeningAdvance").onClick.Invoke();Assert.AreEqual(1,game.YearOpeningStage);
                Find<Button>("OpeningSkip").onClick.Invoke();Assert.AreEqual(4,game.YearOpeningStage);Assert.IsTrue(game.YearOpeningActive);
                yield return new WaitForSecondsRealtime(2.8f);Assert.IsFalse(game.YearOpeningActive);Assert.IsNotNull(Find<Button>("Goal0"));Assert.AreEqual(original.Replace("\"nextRankVoicePlayed\":true","\"nextRankVoicePlayed\":false"),JsonUtility.ToJson(game.State).Replace("\"nextRankVoicePlayed\":true","\"nextRankVoicePlayed\":false"),"計画に戻った後の声の既読旗だけは表示専用");
                Assert.IsNotNull(Find<Button>("Room_branch"));CheckPointer("Room_branch",new Vector2(24,-18));if(year==3){Assert.IsNotNull(Find<Button>("Room_partners"));CheckPointer("Room_partners",new Vector2(24,-18));}
                Capture("next11-planning-y"+year);
            }
            game.StartYear(14);yield return null;Assert.IsFalse(game.YearOpeningActive);Assert.AreEqual(11,game.State.levels.Length);Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next11OpeningAuto_初見の自動進行と再挑戦と演出軽減を確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;game.StartStory(14);FinishStoryTestYear(game.State);game.NextStoryYear();
            yield return new WaitForSecondsRealtime(12.4f);Assert.IsFalse(game.YearOpeningActive);
            game.StartStory(14);FinishStoryTestYear(game.State);game.NextStoryYear();yield return null;Assert.AreEqual(.32f,Find<Image>("OpeningSkip").color.a);
            typeof(OpsGame).GetProperty("ReducedMotion").SetValue(game,true);game.PreviewYearOpening(4);yield return new WaitForSecondsRealtime(2);Assert.AreEqual(Vector3.one,Find<TextMeshProUGUI>("OpeningYear").transform.localScale);Assert.AreEqual(0,Find<Image>("OpeningFlash").GetComponent<CanvasGroup>().alpha);game.AdvanceYearOpening();typeof(OpsGame).GetProperty("ReducedMotion").SetValue(game,false);
            Assert.IsTrue(game.ExportProgress().Valid());Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
