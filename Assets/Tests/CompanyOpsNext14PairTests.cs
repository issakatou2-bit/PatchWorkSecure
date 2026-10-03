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
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static void CheckPairPhoto(string name,string sprite,string caption)
        {
            var photo=Find<Image>(name+"Photo");Assert.AreEqual(sprite,photo.sprite.name);Assert.IsTrue(photo.preserveAspect);
            Assert.IsFalse(photo.raycastTarget);Assert.IsNull(photo.GetComponent<Mask>());Assert.IsNull(photo.GetComponent<RectMask2D>());
            Assert.AreEqual(296,photo.rectTransform.rect.width,.01f);Assert.AreEqual(photo.sprite.rect.width/photo.sprite.rect.height,photo.rectTransform.rect.width/photo.rectTransform.rect.height,.001f);
            Assert.AreEqual(new Vector2(320,220),Find<RectTransform>(name+"Polaroid").sizeDelta);
            Assert.AreEqual(caption,Find<TextMeshProUGUI>(name+"Caption").text);
            Assert.AreEqual(new Vector2(12,-12),photo.rectTransform.anchoredPosition);
        }
        [Category("Capture")]
        [UnityTest] public IEnumerator Next14PairsUI_指定の日記二頁と会議だけに横長写真を表示する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            game.Career.diary.Clear();foreach(int page in new[]{2,6,26})game.Career.diary.Add(new OpsDiaryRecord{key=page,content=page,mood=0,recap="今月の備えを確認した",thought="みんなで守れた",rank="A",minigame="",season="秋"});
            foreach(int page in new[]{2,6})
            {
                game.OpenDiaryBook();Click("DiaryPage_"+page);yield return new WaitForSecondsRealtime(3);
                CheckPairPhoto("DiaryPair",page==2?"hinata-kanon-1":"hinata-engineer-3",page==2?"予算のひみつ":"はじめての夜");
                Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t=>t.name=="DiaryPhoto"));
                var memo=Find<TextMeshProUGUI>("DiaryMemoText");memo.ForceMeshUpdate();Assert.IsFalse(memo.isTextOverflowing);
                Capture("next14-pair-diary-"+page);CheckPointer("DiaryClose");
            }
            game.OpenDiaryBook();Click("DiaryPage_26");yield return new WaitForSecondsRealtime(3);Assert.IsNotNull(Find<Image>("DiaryPhoto"));
            Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t=>t.name=="DiaryPairPhoto"));
            var story=AlliesStory(3);DiaryTestMonth(story.state);story.state.NextMonth();Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));
            yield return new WaitForSecondsRealtime(2);Assert.IsFalse(game.PhasePresentationRunning);
            string snapshot=JsonUtility.ToJson(game.State);
            foreach(string id in new[]{"kanon_meeting","eng_forgot"})
            {
                Assert.IsTrue(game.SpeakSceneLine(id,0));yield return new WaitForSecondsRealtime(.5f);
                CheckPairPhoto("MeetingPair","kanon-engineer-2","5分前");
                Assert.Less(Find<Transform>("MeetingPairPolaroid").GetSiblingIndex(),Find<Transform>("Navigator").GetSiblingIndex());
                Assert.AreEqual(snapshot,JsonUtility.ToJson(game.State));Capture("next14-pair-"+id);CheckPointer("Action_rest");
            }
            game.SpeakSceneLine("kanon_tease",0);yield return null;Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t=>t.name=="MeetingPairPhoto"));
            game.SpeakSceneLine("eng_forgot",0);yield return null;game.StopVoice();yield return null;
            Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t=>t.name=="MeetingPairPhoto"));
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
