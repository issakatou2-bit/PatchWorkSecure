using System.Linq;
using UnityEditor;

namespace PatchWorkSecure.EditorTools
{
    // 未生成の音源も後で置くだけで私的試遊用バンクにつながる。音源そのものは追跡しない。
    public sealed class HinataVoiceImporter:AssetPostprocessor
    {
        private static bool queued;
        private static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] oldPaths)
        {
            if(queued||!imported.Concat(deleted).Concat(moved).Concat(oldPaths).Any(p=>p.StartsWith("Assets/Audio/CompanyYear/VoiceTest/Hinata/")&&!p.Contains("/Resources/")&&(p.EndsWith(".mp3")||p.EndsWith(".wav"))))return;
            queued=true;EditorApplication.delayCall+=()=>{queued=false;CompanyOpsSceneBuilder.UpdateHinataVoice();};
        }
    }
}
