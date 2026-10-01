using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using PatchWorkSecure.CompanyOps;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [Test] public void Next10Events_新しい八件は既存プロファイルと用語を使う()
        {
            Assert.AreEqual(40,OpsEventCatalog.Events.Length);Assert.AreEqual(8,OpsEventCatalog.StoryEvents.Length);
            var appeared=new System.Collections.Generic.HashSet<string>();
            foreach(var e in OpsEventCatalog.StoryEvents)
            {
                var p=OpsEventCatalog.Profile(e.profile);Assert.IsNotNull(p,e.id);Assert.IsNotNull(OpsCatalog.Term(p.lesson),e.id);
                Assert.IsNotEmpty(OpsEventCatalog.SourceUrl(e.source??p.source));
                foreach(string text in new[]{e.title,e.news,e.boss,e.staff,e.symptom,e.finding,e.hint})Assert.IsNotEmpty(text,e.id);
                Assert.AreEqual(e.id=="y3-passkey",e.operational);
            }
            for(int seed=0;seed<250;seed++)
            {
                var first=OpsEventCatalog.StorySchedule(seed,1,null);Assert.IsFalse(first.Any(id=>id.StartsWith("y")));
                var second=OpsEventCatalog.StorySchedule(seed+7919,2,first);var third=OpsEventCatalog.StorySchedule(seed+15838,3,first.Concat(second).ToArray());
                foreach(string id in second.Concat(third))if(id.StartsWith("y"))appeared.Add(id);
            }
            CollectionAssert.AreEquivalent(OpsEventCatalog.StoryEvents.Select(e=>e.id),appeared);
        }
        [Test] public void Next10Calendar_三年の抽選は重複せず固定月と運用月を守る()
        {
            for(int seed=0;seed<500;seed++)
            {
                var used=new string[0];
                for(int year=1;year<=3;year++)
                {
                    int yearSeed=unchecked(seed+year*OpsCatalog.StorySeedStride);
                    var ids=OpsEventCatalog.StorySchedule(yearSeed,year,used);
                    CollectionAssert.AreEqual(ids,OpsEventCatalog.StorySchedule(yearSeed,year,used));
                    Assert.AreEqual(12,ids.Distinct().Count());Assert.IsFalse(ids.Intersect(used).Any());
                    Assert.AreEqual(year==1?"intro-ransom":year==2?"y2-branch":"y3-audit-mail",ids[0]);
                    if(year==3)Assert.AreEqual("y3-final",ids[11]);
                    for(int m=0;m<12;m++)Assert.AreEqual(m==3||m==4||m==7||m==9,OpsEventCatalog.Event(ids[m]).operational);
                    used=used.Concat(ids).ToArray();
                }
                Assert.AreEqual(36,used.Distinct().Count());
            }
        }
        [Test] public void Next10Calendar_年別テーマの重みと初年度の完全一致()
        {
            foreach(var p in OpsEventCatalog.Profiles)
            {
                Assert.AreEqual(new[]{"supply","ai","remote","sharing","session"}.Contains(p.id)?2:1,OpsEventCatalog.StoryWeight(2,p.id));
                Assert.AreEqual(new[]{"ransom","targeted","bec","claim","ddos"}.Contains(p.id)?2:1,OpsEventCatalog.StoryWeight(3,p.id));
                Assert.AreEqual(1,OpsEventCatalog.StoryWeight(1,p.id));
            }
            var story=OpsStory.Begin(14,null);Assert.AreEqual(JsonUtility.ToJson(new OpsState(story.YearSeed,true)),JsonUtility.ToJson(story.state));
            FinishStoryTestYear(story.state);var y1=story.state;Assert.IsTrue(story.AdvanceYear());
            Assert.AreEqual("拠点が増える春",story.state.Current.season);Assert.AreEqual(OpsCatalog.Months[0].@base,story.state.Current.@base);
            CollectionAssert.AreEqual(y1.eventSchedule,story.state.previousStoryEvents);
            var copy=JsonUtility.FromJson<OpsStory>(JsonUtility.ToJson(story));Assert.IsTrue(copy.Valid());
            FinishStoryTestYear(story.state);FinishStoryTestYear(copy.state);Assert.IsTrue(story.AdvanceYear());Assert.IsTrue(copy.AdvanceYear());
            CollectionAssert.AreEqual(story.state.eventSchedule,copy.state.eventSchedule);Assert.AreEqual("3年目の総決算",story.state.MonthAt(11).season);Assert.IsTrue(story.Valid());
        }
    }
}
