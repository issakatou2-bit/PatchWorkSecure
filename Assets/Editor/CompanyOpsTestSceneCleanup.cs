using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PatchWorkSecure.EditorTools
{
    // Test Frameworkが通常は削除する起動用シーンの、取り残しだけを扱う。
    [InitializeOnLoad] public static class CompanyOpsTestSceneCleanup
    {
        private const string ActiveKey="pws_tests_active";
        private static double after;
        static CompanyOpsTestSceneCleanup()
        {
            TestRunnerApi.RegisterTestCallback(new Callbacks());
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode)Schedule();};
            Schedule();
        }
        private static void Schedule()
        {
            after=EditorApplication.timeSinceStartup+2;
            EditorApplication.update-=Delayed;EditorApplication.update+=Delayed;
        }
        private static void Delayed()
        {
            if(EditorApplication.timeSinceStartup<after||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||SessionState.GetBool(ActiveKey,false))return;
            EditorApplication.update-=Delayed;CleanupOrphans();
        }
        public static bool IsBootstrapPath(string path)
        {
            const string prefix="Assets/InitTestScene",suffix=".unity";
            return path!=null&&path.StartsWith(prefix,StringComparison.Ordinal)&&path.EndsWith(suffix,StringComparison.Ordinal)&&
                path.Length==prefix.Length+36+suffix.Length&&Guid.TryParseExact(path.Substring(prefix.Length,36),"D",out _);
        }
        public static int CleanupOrphans()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||SessionState.GetBool(ActiveKey,false))return 0;
            string controller=AssetDatabase.AssetPathToGUID("Packages/com.unity.test-framework/UnityEngine.TestRunner/TestRunner/PlaymodeTestsController.cs");
            if(string.IsNullOrEmpty(controller))return 0;
            var protectedPaths=EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path).ToList();
            for(int i=0;i<SceneManager.sceneCount;i++)protectedPaths.Add(SceneManager.GetSceneAt(i).path);
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));int count=0;
            foreach(string full in Directory.GetFiles(Application.dataPath,"InitTestScene*.unity",SearchOption.TopDirectoryOnly))
            {
                string path="Assets/"+Path.GetFileName(full);
                if(!IsBootstrapPath(path)||protectedPaths.Contains(path)||!File.ReadAllText(full).Contains("guid: "+controller))continue;
                // 根拠を確認した個別ファイルだけ。素材・利用者のシーンを名前だけで消さない。
                if(!Path.GetFullPath(full).StartsWith(Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))continue;
                string backup=Path.Combine(root,"Artifacts/TestCleanup",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(backup);
                File.Copy(full,Path.Combine(backup,Path.GetFileName(full)),false);
                if(File.Exists(full+".meta"))File.Copy(full+".meta",Path.Combine(backup,Path.GetFileName(full)+".meta"),false);
                if(AssetDatabase.DeleteAsset(path))
                {
                    count++;File.AppendAllText(Path.Combine(root,"Artifacts/TestCleanup/cleanup.tsv"),path+"\t"+backup+Environment.NewLine);
                    Debug.Log("[CompanyOps Tests] 一時シーンを退避して削除: "+path+" / "+backup);
                }
            }
            return count;
        }
        private sealed class Callbacks:ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun){SessionState.SetBool(ActiveKey,true);}
            public void RunFinished(ITestResultAdaptor result){SessionState.SetBool(ActiveKey,false);Schedule();}
            public void TestStarted(ITestAdaptor test){}
            public void TestFinished(ITestResultAdaptor result){}
        }
    }
}
