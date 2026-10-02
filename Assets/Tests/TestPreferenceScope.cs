#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PatchWorkSecure.Tests
{
    // 新旧両版を含むテスト全体。存在しなかったキーは削除し、無関係なキーは触らない。
    [SetUpFixture] public sealed class TestPreferenceScope
    {
        private readonly List<System.Action> restore=new List<System.Action>();
        [OneTimeSetUp] public void Snapshot()
        {
            foreach(var key in new[]{"pws_ops_mute","pws_ops_reduce_motion","pws_ops_voice_enabled","pws_ops_captions","pws_ops_shorten","pws_ops_speed","pws_ops_text_size","pws_ops_tutorial_seen","pws_audio_muted","pws_selected_persona_index","pws_ops_fullscreen"})
            {bool existed=PlayerPrefs.HasKey(key);int value=PlayerPrefs.GetInt(key);restore.Add(()=>{if(existed)PlayerPrefs.SetInt(key,value);else PlayerPrefs.DeleteKey(key);});}
            foreach(var key in new[]{"pws_ops_sfx","pws_ops_music","pws_ops_voice_volume"})
            {bool existed=PlayerPrefs.HasKey(key);float value=PlayerPrefs.GetFloat(key);restore.Add(()=>{if(existed)PlayerPrefs.SetFloat(key,value);else PlayerPrefs.DeleteKey(key);});}
            const string stats="pws_edu_stats";bool hadStats=PlayerPrefs.HasKey(stats);string oldStats=PlayerPrefs.GetString(stats);
            restore.Add(()=>{if(hadStats)PlayerPrefs.SetString(stats,oldStats);else PlayerPrefs.DeleteKey(stats);});
        }
        [OneTimeTearDown] public void Restore(){foreach(var action in restore)action();PlayerPrefs.Save();restore.Clear();}
    }
}
#endif
