#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator Next17Visual_六本の選択表示を二解像度で撮影する()
        {
            using(var input=new PadFixture())
            {
                SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(1);var game=Object.FindAnyObjectByType<OpsGame>();
                string[] targets={"MinigamePC_0","MailSafe","MfaDeny","","BlockCell_0_0","RestoreNode_fs"};
                for(int i=0;i<OpsDailyPractice.Ids.Length;i++)
                {
                    string id=OpsDailyPractice.Ids[i];Assert.IsTrue(game.BeginDailyPractice(id,20261003));
                    yield return PadClick(game,input,"MinigameStart");
                    string target=targets[i];if(id=="E")target="LogRow_"+((OpsLogMinigame)game.Minigame).Visible[0].Id;
                    yield return PadTo(game,input,target);yield return null;
                    Assert.AreEqual(target,EventSystem.current.currentSelectedGameObject.name);
                    var focus=EventSystem.current.currentSelectedGameObject.GetComponent<OpsButtonFeedback>();Assert.IsNotNull(focus);
                    var flags=typeof(OpsButtonFeedback).GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    var outline=(UnityEngine.UI.Outline)flags.First(f=>f.Name=="focus").GetValue(focus);
                    Assert.IsTrue(outline.enabled,id+" 選択枠 / "+string.Join(" / ",flags.Where(f=>f.FieldType==typeof(bool)).Select(f=>f.Name+"="+f.GetValue(focus))));
                    // 撮影は採点試遊と別。撮影中だけホストを止め、解像度の変更で時間を使わない。
                    var host=Object.FindAnyObjectByType<OpsMinigameHost>();bool enabled=host.enabled;float elapsed=game.Minigame.Elapsed;
                    host.enabled=false;game.enabled=false;
                    try{Next17Shots(id+"-selected");Assert.AreEqual(elapsed,game.Minigame.Elapsed);}
                    finally{host.enabled=enabled;game.enabled=true;}
                    game.BuildPreview();yield return null;
                }
                Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
        }
    }
}
#endif
