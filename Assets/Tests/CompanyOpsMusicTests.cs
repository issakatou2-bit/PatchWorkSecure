#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [UnityTest] public IEnumerator 二曲のBGMが場面で切り替わり消音と音量を守る()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(1);
            var game = Object.FindAnyObjectByType<OpsGame>();
            Assert.IsNotNull(game.Sounds);
            var calm = game.Sounds.planningMusic; var incident = game.Sounds.incidentMusic;
            Assert.IsNotNull(calm); Assert.IsNotNull(incident); Assert.AreNotSame(calm, incident);
            Assert.AreSame(calm, game.Sounds.titleMusic); Assert.AreSame(calm, game.Sounds.reviewMusic);
            foreach (var clip in new[] { calm, incident })
            {
                Assert.Greater(clip.length, 60); Assert.GreaterOrEqual(clip.frequency, 44100);
                Assert.AreEqual(2, clip.channels);
            }
            game.StartYear(14); yield return new WaitForSeconds(1);
            AssertMusic(game, calm);
            game.BeginIncident(); yield return new WaitForSeconds(1);
            AssertMusic(game, incident);
            game.Resolve("scope");yield return null;AssertMusic(game,incident);
            yield return WaitForResolution(game);yield return new WaitForSeconds(1);
            AssertMusic(game, calm);
            Click("Menu"); yield return null; Click("ToggleSound"); yield return null;
            Assert.IsTrue(game.GetComponents<AudioSource>().Where(s => s.loop).All(s => s.volume == 0));
            Click("ToggleSound"); yield return new WaitForSeconds(1);
            AssertMusic(game, calm);
            Click("MusicVolume"); yield return null;
            Assert.AreEqual(.27f, game.GetComponents<AudioSource>().Single(s => s.loop && s.isPlaying).volume, .002f);
            CheckText(); Assert.IsEmpty(glyphWarnings); LogAssert.NoUnexpectedReceived();
        }

        private static void AssertMusic(OpsGame game, AudioClip expected)
        {
            var active = game.GetComponents<AudioSource>().Where(s => s.loop && s.isPlaying).ToArray();
            Assert.AreEqual(1, active.Length); Assert.AreSame(expected, active[0].clip);
            Assert.Greater(active[0].timeSamples, 0); Assert.Greater(active[0].volume, 0);
            Assert.AreEqual(0, active[0].spatialBlend);
            Assert.IsTrue(active[0].mute, "自動検証では実際の音を鳴らさない");
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>().Length);
        }
    }
}
#endif
