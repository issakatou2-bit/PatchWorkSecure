#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static Type VoiceEditorType(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
        private static AudioClip ImportVoiceFixture(string path)
        {
            var db=VoiceEditorType("UnityEditor.AssetDatabase");var options=VoiceEditorType("UnityEditor.ImportAssetOptions");
            db.GetMethod("ImportAsset",new[]{typeof(string),options}).Invoke(null,new[]{(object)path,Enum.Parse(options,"ForceSynchronousImport")});
            return (AudioClip)db.GetMethod("LoadAssetAtPath",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{path,typeof(AudioClip)});
        }
        private static void SilentVoiceFixture(string path)
        {
            Assert.IsFalse(File.Exists(path),"実音源は上書きしない");Directory.CreateDirectory(Path.GetDirectoryName(path));
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(96036);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(24000);writer.Write(48000);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(96000);writer.Write(new byte[96000]);
            }
        }
        [Test] public void Next21Voice_公開優先の行別フォールバックは最新字幕と欠番を保つ()
        {
            var published=AudioClip.Create("公開",24000,1,24000,false);var trial=AudioClip.Create("私的",24000,1,24000,false);
            try
            {
                var current=new OpsReactionLine{id="maxim_report_v2",caption="最新の字幕",clip=published};var previous=new OpsReactionLine{id=current.id,caption="旧字幕",clip=trial};
                var selected=OpsReactionBank.WithFallback(current,previous);Assert.AreSame(published,selected.clip);Assert.AreEqual(current.caption,selected.caption);
                current.clip=null;selected=OpsReactionBank.WithFallback(current,previous);Assert.AreSame(trial,selected.clip);Assert.AreEqual(current.caption,selected.caption);
                Assert.IsNull(OpsReactionBank.WithFallback(current,null).clip);Assert.AreSame(trial,previous.clip);
            }
            finally{Object.DestroyImmediate(published);Object.DestroyImmediate(trial);}
        }
        [Test] public void Next21Voice_公開ビルドは私的Resourcesと直接参照を拒否する()
        {
            var guard=VoiceEditorType("PatchWorkSecure.EditorTools.CompanyOpsVoiceBuildGuard").GetMethod("RejectPrivateInputs");
            guard.Invoke(null,new object[]{new[]{"Assets/Audio/CompanyYear/Voice/Hinata/combo_1.wav"}});
            foreach(string path in new[]{"Assets/Audio/CompanyYear/VoiceTest/Hinata/Resources/HinataVoiceTest.asset","Assets/Audio/CompanyYear/VoiceTest/Hinata/think_01.mp3"})
                Assert.Throws<TargetInvocationException>(()=>guard.Invoke(null,new object[]{new[]{path}}));
            var db=VoiceEditorType("UnityEditor.AssetDatabase");var dependencies=(string[])db.GetMethod("GetDependencies",new[]{typeof(string[]),typeof(bool)}).Invoke(null,new object[]{new[]{"Assets/Scenes/CompanyYear.unity","Assets/Personas/HinataReactions.asset"},true});
            Assert.IsFalse(dependencies.Any(p=>p.Contains("/VoiceTest/")),"公開側の参照から私的音源へ到達しない");
        }
        [UnityTest] public IEnumerator Next21Voice_仮wavの公開優先と削除後の私的復帰と音量を確認し素材を片付ける()
        {
            SceneManager.LoadScene("CompanyYear");yield return new WaitForSecondsRealtime(.7f);var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(9);game.SkipTutorial();
            string id=new[]{"combo_1","combo_2","combo_3","combo_4","combo_5","combo_6","combo_7","maxim_report_v2","diary_y3_07_v2"}.FirstOrDefault(candidate=>
                !File.Exists("Assets/Audio/CompanyYear/Voice/Hinata/"+candidate+".wav")&&!File.Exists("Assets/Audio/CompanyYear/VoiceTest/Hinata/"+candidate+".wav")&&!File.Exists("Assets/Audio/CompanyYear/VoiceTest/Hinata/"+candidate+".mp3"))??"next21_test_"+Guid.NewGuid().ToString("N");
            string published="Assets/Audio/CompanyYear/Voice/Hinata/"+id+".wav",trial="Assets/Audio/CompanyYear/VoiceTest/Hinata/"+id+".wav";
            var original=game.Navigator;var persona=Object.Instantiate(original);var publicBank=ScriptableObject.CreateInstance<OpsReactionBank>();publicBank.name="HinataReactions";
            var privateBank=ScriptableObject.CreateInstance<OpsReactionBank>();var db=VoiceEditorType("UnityEditor.AssetDatabase");
            try
            {
                SilentVoiceFixture(published);SilentVoiceFixture(trial);var publicClip=ImportVoiceFixture(published);var privateClip=ImportVoiceFixture(trial);Assert.IsNotNull(publicClip);Assert.IsNotNull(privateClip);
                yield return null;yield return null;
                if(!id.StartsWith("next21_test_"))Assert.AreSame(publicClip,original.Reactions.Find(id).clip,"置いた同じIDを自動で台本へ登録");
                publicBank.lines=new[]{new OpsReactionLine{id=id,caption="最新字幕",clip=publicClip}};privateBank.lines=new[]{new OpsReactionLine{id=id,caption="旧字幕",clip=privateClip}};
                persona.Reactions=publicBank;game.Navigator=persona;game.UseLocalTestVoices=true;
                typeof(OpsGame).GetField("localVoiceBank",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,privateBank);
                typeof(OpsGame).GetField("localVoiceChecked",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,true);
                typeof(OpsGame).GetField("voiceVolume",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,.23f);
                Assert.AreSame(publicClip,game.ActiveVoiceBank.Find(id).clip);game.SpeakSceneLine(id,0);yield return null;yield return null;
                var source=game.GetComponents<AudioSource>().Single(s=>s.clip==publicClip);Assert.AreEqual(.23f,source.volume,.001f);Assert.AreEqual("最新字幕",game.LastReactionCaption);game.StopVoice();
                db.GetMethod("DeleteAsset",new[]{typeof(string)}).Invoke(null,new object[]{published});Assert.IsFalse(File.Exists(published));
                var importer=VoiceEditorType("PatchWorkSecure.EditorTools.HinataVoiceImporter");Assert.IsNull(importer.GetMethod("PublicClip").Invoke(null,new object[]{id}));
                publicBank.lines=new[]{new OpsReactionLine{id=id,caption="最新字幕"}};Assert.AreSame(privateClip,game.ActiveVoiceBank.Find(id).clip);
                game.SpeakSceneLine(id,0);yield return null;yield return null;Assert.AreEqual(.23f,source.volume,.001f);Assert.AreSame(privateClip,source.clip);
                game.UseLocalTestVoices=false;Assert.IsNull(game.ActiveVoiceBank.Find(id).clip);game.SpeakSceneLine(id,0);yield return null;Assert.IsFalse(game.VoicePlaying);Assert.AreEqual("最新字幕",game.LastReactionCaption);
            }
            finally
            {
                game.StopVoice();game.Navigator=original;game.UseLocalTestVoices=false;
                db.GetMethod("DeleteAsset",new[]{typeof(string)}).Invoke(null,new object[]{published});db.GetMethod("DeleteAsset",new[]{typeof(string)}).Invoke(null,new object[]{trial});
                Object.Destroy(persona);Object.Destroy(publicBank);Object.Destroy(privateBank);
            }
            Assert.IsFalse(File.Exists(published));Assert.IsFalse(File.Exists(trial));Assert.IsFalse(File.Exists(published+".meta"));Assert.IsFalse(File.Exists(trial+".meta"));
        }
    }
}
#endif
