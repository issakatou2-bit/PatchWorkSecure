using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    // 明示的な検証引数がある時だけ起動。利用者のセーブは読み書きしない。
    public sealed class OpsPlayerSmoke : MonoBehaviour
    {
        private bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Application.isEditor || !Environment.GetCommandLineArgs().Contains("-ops-smoke-test")) return;
            OpsGame.TestMode = true;
            var go = new GameObject("試遊版の自動検証"); DontDestroyOnLoad(go); go.AddComponent<OpsPlayerSmoke>();
        }
        private void Observe(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert || message.Contains("Unicode value")) failed = true;
        }
        private IEnumerator Start()
        {
            Application.logMessageReceived += Observe;
            yield return null; yield return null;
            var game = FindAnyObjectByType<OpsGame>();
            if (game == null) { Debug.LogError("[CompanyOps Player] 試作が見つかりません"); Application.Quit(1); yield break; }
            game.StartYear(14); yield return null;
            for (int month = 0; month < 12 && game.State.phase != OpsPhase.Ended; month++)
            {
                if (!Advance(game)) { failed = true; break; }
                yield return null;
            }
            yield return null;
            if (!game.State.IsClear || game.State.history.Count != 12) failed = true;
            Debug.Log("[CompanyOps Player] 年間通し検証 " + (failed ? "FAILED" : "PASSED") + " / " + game.State.history.Count + "か月");
            Application.logMessageReceived -= Observe;
            Application.Quit(failed ? 1 : 0);
        }
        private bool Advance(OpsGame game)
        {
            try
            {
                var s = game.State;
                if (s.fatigue > 40) s.Act("rest"); s.Act("listen");
                foreach (string key in new[] { "automation", "backup", "inventory", "drill", "education", "runbook", "mfa", "patch", "redundancy", "monitor", "segment" })
                {
                    int index = OpsCatalog.Index(key); var p = OpsCatalog.Projects[index];
                    if (s.levels[index] != 0 || s.UpgradeBlock(index) != "") continue;
                    if (s.capacity >= s.WorkCost(index) + 1 && !s.proposed) s.Act("proposal", p.group);
                    if (s.budget >= s.Cost(index) + 12) s.Upgrade(index);
                }
                s.Act("audit"); s.Act("rest"); s.Act("map");
                game.BeginIncident(); game.Resolve(s.Current.kind == "outage" ? "recover" : "scope"); game.Next();
                return s.Valid();
            }
            catch (Exception e) { Debug.LogException(e); return false; }
        }
    }
}
