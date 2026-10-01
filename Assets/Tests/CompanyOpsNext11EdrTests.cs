#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static OpsState EdrFixture(bool benign,bool edr=true)
        {
            var s=Next11State(2,"ransom");s.levels[OpsCatalog.Index("monitor")]=1;s.levels[OpsCatalog.Index("edr")]=edr?1:0;
            for(int seed=0;seed<200;seed++)
            {s.seed=seed;if(s.Preview("scope").benign==benign){Assert.IsTrue(s.BeginIncident());return s;}}
            Assert.Fail("正常・感染のテスト入力が見つかりません");return null;
        }
        [Test] public void Next11Edr_最初の感染端末だけ一回隔離し時間と年度状態は変えない()
        {
            var state=EdrFixture(false);string before=JsonUtility.ToJson(state);var game=state.CreateContainment();
            Assert.AreEqual(-1,game.EdrIsolate());Assert.IsTrue(game.Start());Assert.IsTrue(game.CanEdrIsolate);
            int first=new System.Random(unchecked(state.seed^((state.month+1)*7919)^0x351ac)).Next(game.PCCount);
            float time=game.Elapsed,remaining=game.Remaining;int visible=game.VisibleCount;
            Assert.AreEqual(first,game.EdrIsolate());Assert.AreEqual(time,game.Elapsed);Assert.AreEqual(remaining,game.Remaining);
            Assert.AreEqual(visible-1,game.VisibleCount);Assert.AreEqual(1,game.StoppedCount);Assert.IsTrue(game.StoppedInfection(first));
            Assert.AreEqual(0,game.EdrIsolationsLeft);Assert.IsFalse(game.CanEdrIsolate);Assert.AreEqual(-1,game.EdrIsolate());
            Assert.AreEqual(1,game.StoppedCount);Assert.AreEqual(before,JsonUtility.ToJson(state));
            game.Tick(100);Assert.AreEqual(OpsMinigamePhase.Result,game.Phase);Assert.AreEqual(-1,game.EdrIsolate());
        }
        [Test] public void Next11Edr_未導入正常未検知と既に切り離した端末では隔離しない()
        {
            foreach(var state in new[]{EdrFixture(false,false),EdrFixture(true)})
            {
                var game=state.CreateContainment();game.Start();Assert.IsFalse(game.CanEdrIsolate);Assert.AreEqual(-1,game.EdrIsolate());
                Assert.AreEqual(0,game.StoppedCount);game.Tick(100);
                if(state.Preview("scope").benign){Assert.AreEqual(0,game.TotalInfected);Assert.AreEqual(0,game.NormalStopped);}
            }
            var hidden=EdrFixture(false);hidden.levels[OpsCatalog.Index("monitor")]=0;
            var pending=hidden.CreateContainment();pending.Start();Assert.IsFalse(pending.CanEdrIsolate);Assert.AreEqual(-1,pending.EdrIsolate());
            var already=EdrFixture(false);var cut=already.CreateContainment();cut.Start();
            foreach(int i in Enumerable.Range(0,cut.PCCount).Where(cut.Visible).ToArray())cut.Cut(i);
            Assert.IsFalse(cut.CanEdrIsolate);Assert.AreEqual(-1,cut.EdrIsolate());Assert.AreEqual(2,cut.StoppedCount);
        }
        [Test] public void Next11Edr_社員に任せる五十点は三方針の従来結果そのまま()
        {
            foreach(bool benign in new[]{false,true})foreach(string response in new[]{"contain","scope","recover"})
            {
                var state=EdrFixture(benign);var expected=state.Preview(response);var game=state.CreateContainment();
                Assert.IsTrue(game.Delegate());Assert.AreEqual(50,game.Score);Assert.AreEqual(-1,game.EdrIsolate());
                Assert.AreEqual(0,game.StoppedCount);Assert.IsTrue(state.Resolve(response,game.Score,true));
                Assert.AreEqual(expected.loss,state.Latest.loss);Assert.AreEqual(expected.downtime,state.Latest.downtime);Assert.AreEqual(expected.cost,state.Latest.cost);
            }
        }
        [UnityTest] public IEnumerator Next11EdrUI_本編の即時隔離ボタンと使用済みと五十点を撮影する()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);
            var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            var story=OpsStory.Begin(14,null);FinishStoryTestYear(story.state);story.AdvanceYear();FinishStoryTestYear(story.state);story.AdvanceYear();
            var state=story.state;for(int i=0;i<state.levels.Length;i++)state.levels[i]=2;
            state.culture=state.trust=100;state.fatigue=0;state.budget=400;
            while(state.month<11){DiaryTestMonth(state);if(state.QuarterRewardPending)state.ClaimQuarterReward("budget");Assert.IsTrue(state.NextMonth());}
            Assert.IsTrue(state.BeginIncident());Assert.IsTrue(state.Valid());
            Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=story}));game.ChooseResponse("scope");
            yield return new WaitForSecondsRealtime(.3f);Capture("next11-edr-instant-brief");
            game.StartMinigame();yield return null;
            var session=(OpsContainmentMinigame)game.Minigame;var button=Find<Button>("MinigameEdr");Assert.IsTrue(button.interactable);
            var label=button.GetComponentInChildren<TMPro.TextMeshProUGUI>();label.ForceMeshUpdate();Assert.LessOrEqual(label.preferredHeight,44);Assert.IsFalse(label.isTextOverflowing);
            CheckPointer("MinigameEdr");Capture("next11-edr-instant-ready");
            button.onClick.Invoke();Assert.AreEqual(1,session.StoppedCount);Assert.AreEqual(0,session.EdrIsolationsLeft);Assert.IsFalse(button.interactable);
            yield return null;Capture("next11-edr-instant-used");button.onClick.Invoke();Assert.AreEqual(1,session.StoppedCount);
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
