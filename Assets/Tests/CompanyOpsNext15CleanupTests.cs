#if UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static Type Next15CleanupType=>Type.GetType("PatchWorkSecure.EditorTools.CompanyOpsTestSceneCleanup, PatchWorkSecure.Editor",true);
        [Test] public void Next15Cleanup_正式な起動用ファイルだけを候補にする()
        {
            var method=Next15CleanupType.GetMethod("IsBootstrapPath",BindingFlags.Public|BindingFlags.Static);
            Assert.IsTrue((bool)method.Invoke(null,new object[]{"Assets/InitTestScene7bd49a6b-9c0e-48c3-976e-1dcb643b501d.unity"}));
            foreach(var path in new[]{"Assets/Scenes/CompanyYear.unity","Assets/Scenes/SampleScene.unity","Assets/InitTestSceneUser.unity","Assets/Scenes/InitTestScene7bd49a6b-9c0e-48c3-976e-1dcb643b501d.unity","Assets/../InitTestScene7bd49a6b-9c0e-48c3-976e-1dcb643b501d.unity",null})Assert.IsFalse((bool)method.Invoke(null,new object[]{path}),path);
        }
        [Test] public void Next15Cleanup_再生テスト中は削除をしない()
        {
            Assert.IsTrue(Application.isPlaying);Assert.AreEqual(0,Next15CleanupType.GetMethod("CleanupOrphans").Invoke(null,null));
        }
    }
}
#endif
