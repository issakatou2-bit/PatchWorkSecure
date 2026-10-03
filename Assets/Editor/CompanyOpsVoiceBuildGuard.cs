using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace PatchWorkSecure.EditorTools
{
    // 公開ビルドは私的音源が1つでも混入し得る状態なら停止する。試遊だけ明示的に許可。
    public sealed class CompanyOpsVoiceBuildGuard:IPreprocessBuildWithReport
    {
        private static bool privateTrial;
        public int callbackOrder=>-100;
        public static IDisposable PrivateTrial()=>new TrialScope();
        private sealed class TrialScope:IDisposable
        {
            private readonly bool previous=privateTrial;
            public TrialScope(){privateTrial=true;}
            public void Dispose(){privateTrial=previous;}
        }
        public static void RejectPrivateInputs(string[] paths)
        {
            string found=paths.FirstOrDefault(p=>p.Replace('\\','/').IndexOf("/VoiceTest/",StringComparison.OrdinalIgnoreCase)>=0);
            if(found!=null)throw new BuildFailedException("公開版に私的試遊音声を含められません: "+found);
        }
        public static void ValidatePublicBuild()
        {
            var resources=AssetDatabase.GetAllAssetPaths().Where(p=>p.IndexOf("/Resources/",StringComparison.OrdinalIgnoreCase)>=0&&!AssetDatabase.IsValidFolder(p)).ToArray();
            RejectPrivateInputs(resources);
            var roots=resources.Concat(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path)).ToArray();
            RejectPrivateInputs(AssetDatabase.GetDependencies(roots,true));
        }
        public void OnPreprocessBuild(BuildReport report){if(!privateTrial)ValidatePublicBuild();}
    }
}
