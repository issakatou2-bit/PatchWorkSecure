using System;
using System.IO;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public static class OpsSaveStore
    {
        [Serializable] private sealed class Header { public int format=0; public bool storyMode=false; }
        public static OpsProgress ReadProgress(string path,out string warning)
        {
            warning="";
            try
            {
                if(!File.Exists(path))return null;
                if(new FileInfo(path).Length>OpsCatalog.ProgressSaveBytes)throw new InvalidDataException();
                string json=File.ReadAllText(path);var header=JsonUtility.FromJson<Header>(json);
                OpsProgress progress;
                if(header!=null&&header.format==0)
                {
                    var state=JsonUtility.FromJson<OpsState>(json);if(state==null||state.yearPressure!=0)throw new InvalidDataException();
                    progress=new OpsProgress{single=state};
                }
                else
                {
                    progress=JsonUtility.FromJson<OpsProgress>(json);
                    // Unityのインライン直列化はnullのクラスを空の実体にするため、明示のモードで復元する。
                    if(progress!=null){if(header.storyMode)progress.single=null;else progress.story=null;}
                }
                if(progress==null||!progress.Valid())throw new InvalidDataException();return progress;
            }
            catch(Exception){warning="保存データを読み込めません。元ファイルは保持しています。";return null;}
        }
        public static bool WriteProgress(string path,OpsProgress progress,out string warning)
        {
            warning="";
            try
            {
                if(progress==null||!progress.Valid())throw new InvalidDataException();
                progress.storyMode=progress.story!=null;
                return WriteJson(path,JsonUtility.ToJson(progress,true),out warning);
            }
            catch(Exception){warning="保存できませんでした。このまま遊べますが終了すると進行を失います。";return false;}
        }
        public static OpsState Read(string path, out string warning)
        {
            warning = "";
            try
            {
                if (!File.Exists(path)) return null;
                if (new FileInfo(path).Length > 300000) throw new InvalidDataException();
                var state = JsonUtility.FromJson<OpsState>(File.ReadAllText(path));
                if (state == null || !state.Valid()) throw new InvalidDataException();
                return state;
            }
            catch (Exception) { warning = "保存データを読み込めません。元ファイルは保持しています。"; return null; }
        }
        public static bool Write(string path, OpsState state, out string warning)
        {
            warning = "";
            try
            {
                if (state == null || !state.Valid()) throw new InvalidDataException();
                return WriteJson(path,JsonUtility.ToJson(state,true),out warning);
            }
            catch (Exception) { warning = "保存できませんでした。このまま遊べますが終了すると進行を失います。"; return false; }
        }
        private static bool WriteJson(string path,string json,out string warning)
        {
            warning="";
            try
            {
                if(System.Text.Encoding.UTF8.GetByteCount(json)>OpsCatalog.ProgressSaveBytes)throw new InvalidDataException();
                string directory=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(directory))Directory.CreateDirectory(directory);
                File.WriteAllText(path+".tmp",json);
                if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);return true;
            }
            catch(Exception){warning="保存できませんでした。このまま遊べますが終了すると進行を失います。";return false;}
        }
    }
}
