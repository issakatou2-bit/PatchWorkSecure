#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
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
        private static string FinishDailyForTest(OpsMinigame game)
        {
            var trace=new StringBuilder();if(game.Phase==OpsMinigamePhase.Brief)Assert.IsTrue(game.Start());
            for(int step=0;game.Phase==OpsMinigamePhase.Playing&&step<4000;step++)
            {
                if(game is OpsMailMinigame mail&&mail.CanAnswer){trace.Append(mail.Current.Id+";");mail.Answer(mail.Current.Suspicious);}
                if(game is OpsMfaMinigame mfa&&mfa.CanAnswer){trace.Append(mfa.Current.Who+":"+mfa.Current.Number+":"+mfa.Current.Legitimate+";");mfa.Answer(mfa.Current.Legitimate);}
                if(game is OpsContainmentMinigame b)for(int pc=0;pc<b.PCCount;pc++)if(b.Visible(pc)){trace.Append(pc+";");b.Cut(pc);}
                if(game is OpsLogMinigame logs)foreach(var row in logs.Visible.Where(r=>r.Suspicious&&!r.Hit).ToArray()){trace.Append(row.Kind+":"+row.Ip+";");logs.Hit(row.Id);}
                if(game is OpsBlockMinigame blocks)foreach(var task in blocks.Tasks.Where(t=>t.Placed==null).ToArray()){trace.Append(task.Name+":"+string.Join("/",task.Solution.Select(c=>c.Row+","+c.Column))+";");blocks.Place(task.Id,task.Solution.Min(c=>c.Row),task.Solution.Min(c=>c.Column));}
                if(game is OpsRestoreMinigame restore){trace.Append(string.Join("/",restore.BestOrder));AdvanceBestRestore(restore);}
                game.Tick(.1f);
            }
            Assert.AreEqual(OpsMinigamePhase.Result,game.Phase);return trace+"="+game.Score+game.Grade;
        }
        [Test] public void Next13Practice_日付から六本を再現して旧状態に依存しない()
        {
            foreach(string id in OpsDailyPractice.Ids)foreach(int day in new[]{20261002,20261003,20261004,20261005})
            {
                var a=OpsDailyPractice.Create(id,day);var b=OpsDailyPractice.Create(id,day);
                Assert.AreEqual(FinishDailyForTest(a),FinishDailyForTest(b),id+":"+day);Assert.AreEqual(a.Grade,OpsDailyPractice.Grade(id,a.Score));Assert.AreEqual("S",a.Grade,id);
            }
            Assert.IsNull(OpsDailyPractice.Create("?",20261002));Assert.IsNull(OpsDailyPractice.Create("B",20260230));
        }
        [Test] public void Next13Practice_ベストと日付を保存し委任では称号を得ない()
        {
            var c=new OpsCareer();foreach(string id in OpsDailyPractice.Ids)
            {
                var skip=OpsDailyPractice.Create(id,20261002);skip.Delegate();Assert.IsFalse(c.RecordPractice(id,20261002,skip));Assert.IsNull(c.PracticeRecord(id));
                var played=OpsDailyPractice.Create(id,20261002);FinishDailyForTest(played);Assert.IsTrue(c.RecordPractice(id,20261002,played));
                Assert.AreEqual(played.Score,c.PracticeRecord(id).bestScore);Assert.IsTrue(c.Valid());
                var worse=OpsDailyPractice.Create(id,20261003);worse.Start();typeof(OpsMinigame).GetMethod("Complete",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(worse,new object[]{10});
                Assert.IsTrue(c.RecordPractice(id,20261003,worse));Assert.AreEqual(10,c.PracticeRecord(id).dailyScore);Assert.AreEqual(played.Score,c.PracticeRecord(id).bestScore);
            }
            Assert.IsTrue(c.HasTitle("minigames-s"));var restored=JsonUtility.FromJson<OpsCareer>(JsonUtility.ToJson(c));Assert.IsTrue(restored.Valid());Assert.AreEqual(6,restored.practiceRecords.Count);
            string dir=Path.Combine(Application.temporaryCachePath,"Next13-Practice-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try
            {
                string path=Path.Combine(dir,"save.json");var progress=new OpsProgress{careerOnly=true,career=c};Assert.IsTrue(OpsSaveStore.WriteProgress(path,progress,out _));
                var read=OpsSaveStore.ReadProgress(path,out string warning);Assert.IsEmpty(warning);Assert.IsTrue(read.careerOnly);Assert.IsNull(read.Current);Assert.AreEqual(6,read.career.practiceRecords.Count);
                var old=new OpsProgress{single=new OpsState(14,true),career=c};Assert.IsTrue(OpsSaveStore.WriteProgress(path,old,out _));byte[] bytes=File.ReadAllBytes(path);
                c.practiceRecords[0].dailyScore=9;Assert.IsTrue(OpsSaveStore.WriteProgress(path,old,out _));CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path+".bak"));
                var loaded=OpsSaveStore.ReadProgress(path,out warning);Assert.IsEmpty(warning);Assert.AreEqual(JsonUtility.ToJson(old.single),JsonUtility.ToJson(loaded.single));
            }
            finally{Directory.Delete(dir,true);}
            restored.practiceRecords.Add(restored.practiceRecords[0]);Assert.IsFalse(restored.Valid());
        }
        [UnityTest] public IEnumerator Next13PracticeUI_初回タイトルから六本を練習して記録と称号を残す()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            Click("HomeRecords");yield return new WaitForSecondsRealtime(.5f);Capture("next13-practice-records-empty");Assert.IsNull(game.State);
            foreach(string id in OpsDailyPractice.Ids)
            {
                CheckPointer("PracticePlay"+id);Click("PracticePlay"+id);yield return new WaitForSecondsRealtime(.25f);Assert.IsTrue(game.DailyPracticeActive);Capture("next13-practice-"+id+"-brief");
                Click("MinigameDelegate");yield return null;Assert.IsFalse(game.DailyPracticeActive);Assert.IsNull(game.Career.PracticeRecord(id));
                Assert.IsTrue(game.BeginDailyPractice(id,OpsDailyPractice.Day(DateTime.Today)));Click("MinigameStart");FinishDailyForTest(game.Minigame);game.TickMinigame(0);
                yield return new WaitForSecondsRealtime(2);Capture("next13-practice-"+id+"-result");Click("MinigameContinue");yield return null;
                Assert.IsNull(game.State);Assert.IsNotNull(game.Career.PracticeRecord(id));Assert.IsTrue(game.Career.Valid());
            }
            Assert.IsTrue(game.Career.HasTitle("minigames-s"));Capture("next13-practice-records-best");var scroll=Find<ScrollRect>("RecordsList");scroll.verticalNormalizedPosition=0;yield return null;Capture("next13-practice-records-titles");
            Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height);Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator Next13PracticeUI_途中の三モードを一切変更せず練習から戻る()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.8f);var game=Object.FindAnyObjectByType<OpsGame>();game.UseLocalTestVoices=false;
            foreach(int mode in new[]{0,1,2})
            {
                var career=new OpsCareer{endlessUnlocked=true};var progress=mode==0?new OpsProgress{single=new OpsState(14,true),career=career}:mode==1?new OpsProgress{story=AlliesStory(2),career=career}:new OpsProgress{endless=OpsEndless.Begin(14,null),career=career};
                Assert.IsTrue(game.RestoreProgress(progress));string before=JsonUtility.ToJson(game.State),story=JsonUtility.ToJson(game.Story),endless=JsonUtility.ToJson(game.Endless);game.OpenRecords();
                Assert.IsTrue(game.BeginDailyPractice("C",20261002));Click("MinigameStart");FinishDailyForTest(game.Minigame);game.TickMinigame(0);yield return new WaitForSecondsRealtime(2);Click("MinigameContinue");yield return null;
                Assert.AreEqual(before,JsonUtility.ToJson(game.State));Assert.AreEqual(story,JsonUtility.ToJson(game.Story));Assert.AreEqual(endless,JsonUtility.ToJson(game.Endless));Assert.IsTrue(game.ExportProgress().Valid());
            }
            Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
