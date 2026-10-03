using System.Linq;
using UnityEditor;

namespace PatchWorkSecure.EditorTools
{
    // 公開音源と私的音源は別のバンク。最新台本へ同じIDで自動接続する。
    public sealed class HinataVoiceImporter:AssetPostprocessor
    {
        private static bool queued;
        public static UnityEngine.AudioClip PublicClip(string id)=>AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>("Assets/Audio/CompanyYear/Voice/Hinata/"+id+".wav");
        private static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] oldPaths)
        {
            if(queued||!imported.Concat(deleted).Concat(moved).Concat(oldPaths).Any(p=>(p.StartsWith("Assets/Audio/CompanyYear/VoiceTest/Hinata/")||p.StartsWith("Assets/Audio/CompanyYear/Voice/Hinata/"))&&!p.Contains("/Resources/")&&(p.EndsWith(".mp3")||p.EndsWith(".wav"))))return;
            queued=true;EditorApplication.delayCall+=()=>{queued=false;CompanyOpsSceneBuilder.UpdateHinataVoice();};
        }
    }
}
