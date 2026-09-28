using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public enum OpsReaction { Think, Purchase, Growth, Alert, Success, Recover, Month, Clear, Failure }

    [Serializable] public sealed class OpsReactionLine
    {
        public string id, caption;
        public OpsReaction reaction;
        public AudioClip clip;
    }

    // 音声は制作時に収録・生成して取り込む。プレイ中の外部通信・APIキーは不要。
    [CreateAssetMenu(menuName = "PatchWorkSecure/情シスの一年/キャラの短い反応")]
    public sealed class OpsReactionBank : ScriptableObject
    {
        public OpsReactionLine[] lines = Array.Empty<OpsReactionLine>();
        public bool HasAudio => lines != null && lines.Any(l => l != null && l.clip != null);
        public static OpsReactionLine[] Defaults()
        {
            string[][] captions = {
                new[] { "うーん。", "なるほど！", "そっか。", "考えてみよう。" },
                new[] { "準備できたね！", "頼りになりそう！", "よしっ！", "整ってきたね。" },
                new[] { "やったー！", "成長したね！", "いい感じ！", "頼もしいね！" },
                new[] { "あれっ？", "確認しよう。", "気になるね。", "落ち着いていこう。" },
                new[] { "よかった！", "うまくいったね！", "ほっとした。", "おつかれさま！" },
                new[] { "立て直そう。", "ここからだね。", "一緒に考えよう。", "次に備えよう。" },
                new[] { "今月もよろしく！", "さて、どうしよう？", "一緒に確認しよう。", "始めよう！" },
                new[] { "一年、おつかれさま！", "守りきったね！", "みんなで頑張ったね！", "やったね！" },
                new[] { "おつかれさま。", "振り返ってみよう。", "また挑戦しよう。", "次につなげよう。" }
            };
            return captions.SelectMany((set, cue) => set.Select((caption, i) => new OpsReactionLine {
                id = ((OpsReaction)cue).ToString().ToLowerInvariant() + "_" + (i + 1).ToString("00"), reaction = (OpsReaction)cue, caption = caption
            })).ToArray();
        }
    }

    // ゲームの乱数と分離。音声の有無や再抽選で出来事・結果を変えない。
    public sealed class OpsReactionDirector
    {
        private readonly System.Random random;
        private readonly Dictionary<OpsReaction, double> recent = new Dictionary<OpsReaction, double>();
        private double lastTime = double.NegativeInfinity;
        private string lastId = "", lastCaption = "";
        private AudioClip lastClip;
        private int lastPriority;
        public OpsReactionDirector(int seed) { random = new System.Random(seed); }
        public static int Priority(OpsReaction cue) => cue == OpsReaction.Clear || cue == OpsReaction.Failure ? 4 :
            cue == OpsReaction.Alert || cue == OpsReaction.Recover ? 3 : cue == OpsReaction.Success || cue == OpsReaction.Growth ? 2 : cue == OpsReaction.Think ? 0 : 1;
        public bool TryChoose(OpsReactionBank bank, OpsReaction cue, double time, out OpsReactionLine line)
        {
            line = null;
            if (bank == null || bank.lines == null || double.IsNaN(time) || double.IsInfinity(time)) return false;
            if (time - lastTime < .8 || time - lastTime < 4 && Priority(cue) <= lastPriority) return false;
            if (recent.TryGetValue(cue, out double previous) && time - previous < 8) return false;
            var pool = bank.lines.Where(l => l != null && l.reaction == cue && !string.IsNullOrWhiteSpace(l.caption)).ToArray();
            if (pool.Length == 0) return false;
            // 音源が一部だけ届いた段階でも、字幕と再生する台詞を一致させる。
            var recorded = pool.Where(l => l.clip != null).ToArray();
            if (recorded.Length > 0) pool = recorded;
            var alternatives = pool.Where(l => l.id != lastId && l.caption != lastCaption && (l.clip == null || l.clip != lastClip)).ToArray();
            if (alternatives.Length > 0) pool = alternatives;
            line = pool[random.Next(pool.Length)]; lastId = line.id; lastCaption = line.caption; lastClip = line.clip;
            lastTime = time; lastPriority = Priority(cue); recent[cue] = time; return true;
        }
    }
}
