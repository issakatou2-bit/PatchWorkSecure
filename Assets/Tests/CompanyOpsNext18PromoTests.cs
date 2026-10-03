#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        [Serializable] private sealed class PromoSound
        {
            public int frame,source;public string operation,kind,id,file,clip,restriction;
            public float volume,pitch,offsetSeconds;public bool loop;
        }
        [Serializable] private sealed class PromoSoundList
        {public int fps=30,frames,width=1920,height=1080;public string shot;public List<PromoSound> sounds=new List<PromoSound>();}
        private sealed class PromoRecorder:IDisposable
        {
            private readonly Camera camera;private readonly GameObject cameraObject;private readonly RenderTexture target;private readonly Texture2D texture;
            private readonly Dictionary<AudioSource,PromoSound> playing=new Dictionary<AudioSource,PromoSound>();
            private readonly Dictionary<AudioSource,int> sourceKeys=new Dictionary<AudioSource,int>();
            private readonly MethodInfo assetPath;
            private Canvas canvas;private RenderMode oldMode;private Camera oldCamera;private float oldDistance;
            private PromoSoundList list;private string folder;public int Frame;
            private readonly string outputRoot;
            public PromoRecorder(string outputRoot="Artifacts/Promo/shots")
            {
                this.outputRoot=outputRoot;
                cameraObject=new GameObject("素材撮影カメラ",typeof(Camera));camera=cameraObject.GetComponent<Camera>();camera.enabled=false;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;target=new RenderTexture(1920,1080,24);camera.targetTexture=target;
                texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.AssetDatabase")).First(t=>t!=null);
                assetPath=type.GetMethod("GetAssetPath",new[]{typeof(Object)});
                OpsGame.PromoAudioPlayed+=Played;
            }
            public void Begin(string shot,int frames,OpsGame game)
            {
                Frame=1;folder=Path.Combine(Application.dataPath,"..",outputRoot,shot);Directory.CreateDirectory(folder);
                // 再撮影時は前の素材を消さず退避し、短くなった連番を混ぜない。
                var previous=Directory.GetFiles(folder,"frame_*.png").Concat(Directory.GetFiles(folder,"sounds.json")).ToArray();
                if(previous.Length>0){string backup=Path.Combine(folder,"previous-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(backup);foreach(string path in previous)File.Move(path,Path.Combine(backup,Path.GetFileName(path)));}
                list=new PromoSoundList{shot=shot,frames=frames};playing.Clear();sourceKeys.Clear();
                foreach(var source in game.GetComponents<AudioSource>())if(source.isPlaying&&source.clip!=null)
                {string kind=source.loop?"bgm":source.clip.name.StartsWith("hinata_")?"hinata-voice":"ongoing";Record(source,source.clip,kind,source.clip.name,"ongoing");}
            }
            private void Played(AudioSource source,AudioClip clip,string kind,string id)
            {if(list!=null)Record(source,clip,kind,id,"play");}
            private void Record(AudioSource source,AudioClip clip,string kind,string id,string operation)
            {
                string path=(string)assetPath.Invoke(null,new object[]{clip});
                if(string.IsNullOrEmpty(path))path=ExportGenerated(clip);
                if(!sourceKeys.TryGetValue(source,out int sourceKey)){sourceKey=sourceKeys.Count+1;sourceKeys[source]=sourceKey;}
                var row=new PromoSound{frame=Frame,source=sourceKey,operation=operation,kind=kind,id=id,file=path.Replace('\\','/'),clip=clip.name,
                    volume=source.volume,pitch=source.pitch,loop=source.loop,offsetSeconds=operation=="ongoing"?source.time:0,
                    restriction=kind=="hinata-voice"||path.Contains("VoiceTest")?"private-only: 動画に使用禁止":kind=="bgm"?"pending: 利用条件確認まで動画に使用禁止":"出典の条件に従う"};
                list.sounds.Add(row);playing[source]=row;
            }
            private static string ExportGenerated(AudioClip clip)
            {
                string name=Regex.Replace(clip.name,"[^A-Za-z0-9_-]","_");string relative="Artifacts/Promo/audio/generated/"+name+".wav";
                string path=Path.GetFullPath(Path.Combine(Application.dataPath,"..",relative));if(File.Exists(path))return relative;
                Directory.CreateDirectory(Path.GetDirectoryName(path));var samples=new float[clip.samples*clip.channels];Assert.IsTrue(clip.GetData(samples,0));
                using(var writer=new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16);writer.Write((short)1);writer.Write((short)clip.channels);writer.Write(clip.frequency);writer.Write(clip.frequency*clip.channels*2);writer.Write((short)(clip.channels*2));writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);foreach(float sample in samples)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample,-1,1)*32767));
                }return relative;
            }
            public void Snapshot()
            {
                var next=Find<Canvas>("CompanyOpsCanvas");if(canvas!=next){RestoreCanvas();canvas=next;oldMode=canvas.renderMode;oldCamera=canvas.worldCamera;oldDistance=canvas.planeDistance;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
                Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;
                try{RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,"frame_"+Frame.ToString("D5")+".png"),texture.EncodeToPNG());}
                finally{RenderTexture.active=previous;}
                foreach(var source in Object.FindObjectsByType<AudioSource>())if(playing.TryGetValue(source,out var prior)&&source.isPlaying&&Mathf.Abs(prior.volume-source.volume)>.002f)
                {var gain=new PromoSound{frame=Frame,source=prior.source,operation="gain",kind=prior.kind,id=prior.id,file=prior.file,clip=prior.clip,volume=source.volume,pitch=source.pitch,loop=source.loop,restriction=prior.restriction};list.sounds.Add(gain);playing[source]=gain;}
            }
            public void End()
            {
                Assert.AreEqual(list.frames,Directory.GetFiles(folder,"frame_*.png").Length);
                File.WriteAllText(Path.Combine(folder,"sounds.json"),JsonUtility.ToJson(list,true));list=null;RestoreCanvas();
            }
            private void RestoreCanvas(){if(canvas!=null){canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;}canvas=null;}
            public void Dispose(){OpsGame.PromoAudioPlayed-=Played;RestoreCanvas();camera.targetTexture=null;target.Release();Object.DestroyImmediate(texture);Object.DestroyImmediate(target);Object.DestroyImmediate(cameraObject);}
        }
        private static IEnumerator PromoWait(int frames,Action step=null)
        {for(int i=0;i<frames;i++){OpsPromoClock.Advance();step?.Invoke();yield return null;}}
        private static void PromoCheckText()
        {
            foreach(var label in Object.FindObjectsByType<TextMeshProUGUI>())
            {
                Assert.IsFalse(Regex.IsMatch(label.name,"Debug|FPS",RegexOptions.IgnoreCase),label.name);
                foreach(Match ip in Regex.Matches(label.text,@"\b\d{1,3}(?:\.\d{1,3}){3}\b"))
                    Assert.IsTrue(ip.Value.StartsWith("10.")||ip.Value.StartsWith("192.0.2.")||ip.Value.StartsWith("198.51.100.")||ip.Value.StartsWith("203.0.113."),"実在の公開IPを撮らない: "+ip.Value);
                Assert.IsFalse(Regex.IsMatch(label.text,@"https?://(?![^/\s]*\.(?:example|invalid|test)(?:[/:\s]|$))",RegexOptions.IgnoreCase),"実在のURLを撮らない: "+label.text);
            }
        }
        private static IEnumerator PromoShot(PromoRecorder recorder,OpsGame game,string name,int seconds,Action<int> action=null)
        {
            if(string.CompareOrdinal(name,OpsPromoClock.ResumeFrom)<0)
            {for(int n=1;n<=seconds*30;n++){OpsPromoClock.Advance();action?.Invoke(n);yield return null;}yield break;}
            recorder.Begin(name,seconds*30,game);
            for(int n=1;n<=seconds*30;n++)
            {recorder.Frame=n;OpsPromoClock.Advance();action?.Invoke(n);yield return null;PromoCheckText();recorder.Snapshot();}
            recorder.End();
        }
        private static void PromoAuto(OpsGame game)
        {
            if(game.Minigame is OpsMailMinigame mail&&mail.CanAnswer)Click(mail.Current.Suspicious?"MailReport":"MailSafe");
            if(game.Minigame is OpsLogMinigame logs){var row=logs.Visible.FirstOrDefault(r=>r.Suspicious&&!r.Hit);if(row!=null)Click("LogRow_"+row.Id);}
        }
        [UnityTest,Explicit("宣伝素材撮影は名前指定時だけ実行。全件の件数と結果を変えない")]
        [Category("Capture")]
        public IEnumerator Next18Promo_九場面の30Hz連番と実再生の音を保存する()
        {
            int captureBefore=UnityEngine.Time.captureFramerate;OpsPromoClock.Begin();
            try
            {
                SceneManager.LoadScene("CompanyYear");yield return PromoWait(60);var game=Object.FindAnyObjectByType<OpsGame>();Assert.IsNotNull(game);
                using(var recorder=new PromoRecorder())
                {
                    game.StartYear(14);game.SkipTutorial();yield return PromoWait(110);game.SpeakSceneLine("month_01",0);yield return PromoShot(recorder,game,"01-office",5);
                    yield return PromoShot(recorder,game,"02-planning",6,n=>{if(n==15)Click("Action_listen");});
                    yield return PromoShot(recorder,game,"03-incident",6,n=>{if(n==8)game.BeginIncident();});
                    foreach(string id in new[]{"B","C","E"})
                    {
                        Assert.IsTrue(game.BeginDailyPractice(id,20261003));game.StartMinigame();yield return PromoWait(6);
                        if(id=="C")while(((OpsMailMinigame)game.Minigame).Number<9){yield return PromoWait(1,()=>PromoAuto(game));}
                        if(id=="E")while(game.Minigame.Remaining>.6f){yield return PromoWait(1,()=>PromoAuto(game));}
                        string shot=id=="B"?"04-mini-b":id=="C"?"05-mini-c":"06-mini-e";
                        yield return PromoShot(recorder,game,shot,4,n=>
                        {
                            if(id=="B"&&n%9==0&&game.Minigame.Phase==OpsMinigamePhase.Playing)
                            {var b=(OpsContainmentMinigame)game.Minigame;int pc=Enumerable.Range(0,b.PCCount).Where(i=>b.Visible(i)).DefaultIfEmpty(-1).First();if(pc>=0)Click("MinigamePC_"+pc);}
                            else PromoAuto(game);
                        });
                        Assert.AreEqual(OpsMinigamePhase.Result,game.Minigame.Phase);Assert.AreEqual("S",game.Minigame.Grade);Assert.IsTrue(Find<Transform>("MinigameRankStamp").gameObject.activeInHierarchy);game.ConfirmMinigame();yield return PromoWait(6);
                    }
                    Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(2,0)}));game.PreviewYearOpening(2);yield return PromoWait(1);
                    yield return PromoShot(recorder,game,"07-rivals",5);Assert.AreEqual(3,Object.FindObjectsByType<Transform>().Count(t=>t.name.StartsWith("OpeningRival")&&t.name.Length==13));game.AdvanceYearOpening();game.AdvanceYearOpening();game.AdvanceYearOpening();
                    game.Career.diary.Clear();foreach(int page in new[]{2,6})game.Career.diary.Add(new OpsDiaryRecord{key=page,content=page,mood=0,recap="今月の備えを確認した",thought="みんなで守れた",rank="A",season="秋",minigame=""});
                    yield return PromoShot(recorder,game,"08-pairs",6,n=>
                    {
                        if(n==1||n==61){game.OpenDiaryBook();Click("DiaryPage_"+(n==1?2:6));}
                        if(n==121){Assert.IsTrue(game.RestoreProgress(new OpsProgress{story=StoreStory(3,1)}));Assert.IsTrue(game.SpeakSceneLine("kanon_meeting",0));}
                        if(n==40||n==100||n==165)Assert.IsNotNull(Find<Image>(n<121?"DiaryPairPhoto":"MeetingPairPhoto").sprite);
                    });
                    game.StopVoice();Assert.IsTrue(game.RestoreProgress(new OpsProgress{careerOnly=true,career=game.Career}));yield return PromoWait(60);yield return PromoShot(recorder,game,"09-title",6);
                }
                Assert.IsEmpty(glyphWarnings);LogAssert.NoUnexpectedReceived();
            }
            finally{OpsPromoClock.End();OpsPromoClock.ResumeFrom="";}
            Assert.AreEqual(captureBefore,UnityEngine.Time.captureFramerate);Assert.IsFalse(OpsPromoClock.Active);
        }
    }
}
#endif
