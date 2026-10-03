#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest, Explicit("変更前の撮影は実装前に一度だけ行う")]
        [Category("Capture")]
        public IEnumerator Next17Before_既存画面とマウスの選択を保存()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(2);
            var game=Object.FindAnyObjectByType<OpsGame>();
            foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1920,1080)})Next15Shot("title-before",size.x,size.y,"Next17");
            CheckPointer("SingleYear");yield return new WaitForSecondsRealtime(.2f);
            foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1920,1080)})Next15Shot("title-hover",size.x,size.y,"Next17");
            game.StartYear(14);game.SkipTutorial();while(game.PhasePresentationRunning)yield return null;yield return new WaitForSecondsRealtime(1);
            foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1920,1080)})Next15Shot("planning-before",size.x,size.y,"Next17");
        }
    }
}
#endif
