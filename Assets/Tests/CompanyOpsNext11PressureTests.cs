#if UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [Test] public void Next11Pressure_新年度だけ調整して旧十一設備と調整前の途中保存を保つ()
        {
            CollectionAssert.AreEqual(new[]{0,12,24},OpsCatalog.StoryPressures);
            CollectionAssert.AreEqual(new[]{0,13,26},OpsCatalog.GrowthStoryPressures);
            foreach(int year in new[]{2,3})
            {
                var current=AlliesStory(year);Assert.IsTrue(current.Valid());
                Assert.AreEqual(OpsCatalog.StoryThreatVersion,current.state.yearThreatRules);
                Assert.AreEqual(OpsCatalog.GrowthStoryPressures[year-1],current.state.yearPressure);
                current.state.yearThreatRules=0;current.state.yearPressure=OpsCatalog.StoryPressures[year-1];
                Assert.IsTrue(current.Valid(),"調整前の追加設備を持つ途中保存も保持");
                var copy=JsonUtility.FromJson<OpsStory>(JsonUtility.ToJson(current));Assert.IsTrue(copy.Valid());
                Assert.AreEqual(OpsCatalog.StoryPressures[year-1],copy.state.yearPressure);
                current.state.yearGrowthRules=0;current.state.levels=current.state.levels.Take(OpsCatalog.BaseEquipmentCount).ToArray();
                current.state.staffExperience=current.state.staffExperience.Take(OpsCatalog.OriginalStaffCount).ToArray();
                current.state.eventSchedule=OpsEventCatalog.StorySchedule(current.YearSeed,year,current.state.previousStoryEvents);
                current.state.monthStartMetrics=current.state.ReportMetrics;Assert.IsTrue(current.Valid(),"旧十一設備の年度も調整しない");
                current.state.yearThreatRules=OpsCatalog.StoryThreatVersion;Assert.IsFalse(current.Valid(),"旧年度へ新脅威だけ混ぜない");
            }
            var one=new OpsState(14,true);Assert.AreEqual(0,one.yearThreatRules);Assert.AreEqual(0,one.yearPressure);Assert.IsTrue(one.Valid());
            var story=OpsStory.Begin(14,null);Assert.AreEqual(JsonUtility.ToJson(one=new OpsState(story.YearSeed,true)),JsonUtility.ToJson(story.state));
        }
    }
}
#endif
