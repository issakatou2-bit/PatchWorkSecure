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
            // 隠した検証用ウィンドウでも進行する。通常プレイの設定は変更しない。
            Application.runInBackground = true;
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
            if(Environment.GetCommandLineArgs().Contains("-ops-sfx-smoke-test"))
            {
                foreach(OpsCue cue in Enum.GetValues(typeof(OpsCue)))
                {
                    var clip=game.Sounds==null?null:game.Sounds.Clip(cue);
                    if(clip==null||clip.samples<1000||clip.frequency!=48000)failed=true;
                }
                Debug.Log("[CompanyOps Player] B案SEの全15用途の読込確認 "+(failed?"FAILED":"PASSED"));
            }
            if (Environment.GetCommandLineArgs().Contains("-ops-audio-smoke-test"))
            {
                yield return new WaitForSecondsRealtime(1);
                CheckMusic(game, game.Sounds == null ? null : game.Sounds.planningMusic);
                game.BeginIncident(); yield return new WaitForSecondsRealtime(1);
                CheckMusic(game, game.Sounds == null ? null : game.Sounds.incidentMusic);
                if (Environment.GetCommandLineArgs().Contains("-ops-voice-smoke-test"))
                {
                    yield return new WaitForSecondsRealtime(.5f);
                    var line=game.ActiveVoiceBank?.Find("incident_start");
                    if(line?.clip==null||!game.PortraitVoicePlaying)failed=true;
                    Debug.Log("[CompanyOps Player] ひなたの事件音声の再生確認 "+(failed?"FAILED":"PASSED"));
                }
                game.Resolve("scope");
                float deadline=Time.realtimeSinceStartup+8;
                while(game.ResolutionActive&&Time.realtimeSinceStartup<deadline)yield return null;
                if(game.ResolutionActive)failed=true;
                yield return new WaitForSecondsRealtime(1);
                CheckMusic(game, game.Sounds == null ? null : game.Sounds.reviewMusic);
                Debug.Log("[CompanyOps Player] BGM二曲の再生確認 " + (failed ? "FAILED" : "PASSED"));
                game.StartYear(14); yield return null;
            }
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
                game.BeginIncident();
                // 機械的に限定対応を続けない。利用者に見える見積もりだけで方針を選ぶ。
                string response = new[] { "contain", "scope", "recover" }.OrderBy(id => {
                    var estimate = s.Estimate(id);
                    return estimate.cost + estimate.lossMax + estimate.stopMax * 2;
                }).First();
                game.Resolve(response); game.Next();
                return s.Valid();
            }
            catch (Exception e) { Debug.LogException(e); return false; }
        }
        private void CheckMusic(OpsGame game, AudioClip expected)
        {
            var active = game.GetComponents<AudioSource>().Where(s => s.loop && s.isPlaying).ToArray();
            if (expected == null || active.Length != 1 || active[0].clip != expected || active[0].timeSamples <= 0 ||
                active[0].volume <= 0 || active[0].spatialBlend != 0 || FindObjectsByType<AudioListener>().Length != 1)
                failed = true;
        }
    }
}
