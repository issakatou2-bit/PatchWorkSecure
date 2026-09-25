using System;
using System.IO;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public static class OpsSaveStore
    {
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
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(state, true));
                if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
                else File.Move(path + ".tmp", path);
                return true;
            }
            catch (Exception) { warning = "保存できませんでした。このまま遊べますが終了すると進行を失います。"; return false; }
        }
    }
}
