using System;
using System.IO;
using PatchWorkSecure.CompanyOps;
using UnityEditor;
using UnityEngine;

namespace PatchWorkSecure.EditorTools
{
    // 加藤さんが試遊用に選んだB案だけを登録する。A案と旧版には触れない。
    public static class CompanyOpsSfxImporter
    {
        [MenuItem("PatchWorkSecure/情シスの一年/B案の効果音を取り込む")]
        public static void ImportApprovedB()
        {
            string root=Directory.GetParent(Application.dataPath).FullName;
            const string target="Assets/Audio/CompanyYear/SFX/B-bright";
            string[] names={"01-click","02-confirm","03-purchase","04-growth","05-count","06-alert","07-shield","08-damage","09-stamp","10-clear","11-transition"};
            Directory.CreateDirectory(Path.Combine(root,target));
            var clips=new AudioClip[names.Length];
            for(int i=0;i<names.Length;i++)
            {
                string path=target+"/"+names[i]+".wav";
                File.Copy(Path.Combine(root,"ArtSource/SfxCandidates/B-bright",names[i]+".wav"),Path.Combine(root,path),true);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);
                var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;
                importer.forceToMono=false;importer.loadInBackground=false;
                importer.SaveAndReimport();
                clips[i]=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if(clips[i]==null)throw new InvalidOperationException("効果音を取り込めません："+path);
            }
            var palette=AssetDatabase.LoadAssetAtPath<OpsSoundPalette>("Assets/CompanyOps/YearSounds.asset");
            if(palette==null)throw new InvalidOperationException("YearSoundsが見つかりません");
            palette.click=clips[0];palette.action=palette.success=palette.staffHelp=clips[1];
            palette.purchase=clips[2];palette.growth=clips[3];palette.count=clips[4];palette.alert=clips[5];
            palette.prepared=clips[6];palette.damage=palette.failure=clips[7];palette.stamp=clips[8];
            palette.clear=clips[9];palette.month=palette.transition=clips[10];
            EditorUtility.SetDirty(palette);AssetDatabase.SaveAssets();
            Debug.Log("[CompanyOps] B案の効果音11種を登録しました。");
        }
    }
}
