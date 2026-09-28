using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public enum OpsCue { Click, Action, Purchase, Growth, Alert, Success, Damage, Month, Clear, Failure, Prepared, StaffHelp, Count, Stamp, Transition }

    // 差し替え素材はここへ集約。未設定の効果音は試作用の合成音で動く。
    [CreateAssetMenu(menuName = "PatchWorkSecure/情シスの一年/サウンド素材")]
    public sealed class OpsSoundPalette : ScriptableObject
    {
        public AudioClip click, action, purchase, growth, alert, success, damage, month, clear, failure;
        public AudioClip prepared, staffHelp;
        public AudioClip count, stamp, transition;
        public AudioClip titleMusic, planningMusic, incidentMusic, reviewMusic;
        public AudioClip Clip(OpsCue cue)
        {
            switch (cue)
            {
                case OpsCue.Click: return click;
                case OpsCue.Action: return action;
                case OpsCue.Purchase: return purchase;
                case OpsCue.Growth: return growth;
                case OpsCue.Alert: return alert;
                case OpsCue.Success: return success;
                case OpsCue.Damage: return damage;
                case OpsCue.Month: return month;
                case OpsCue.Clear: return clear;
                case OpsCue.Prepared: return prepared;
                case OpsCue.StaffHelp: return staffHelp;
                case OpsCue.Count: return count;
                case OpsCue.Stamp: return stamp;
                case OpsCue.Transition: return transition;
                default: return failure;
            }
        }
    }

    // 音程だけを変えた一種類の音ではなく、用途別の音列・減衰・打音を作る。
    // 外部音源・生成サービスは使っていない。最終ミックス前の試作用。
    public static class OpsSoundDesign
    {
        public const int SampleRate = 44100;
        public static float[] Samples(OpsCue cue)
        {
            float length = cue == OpsCue.Click ? .075f : cue == OpsCue.Clear ? .95f :
                cue == OpsCue.Growth ? .62f : cue == OpsCue.Alert ? .48f : cue == OpsCue.Failure ? .65f : .36f;
            var samples = new float[(int)(SampleRate * length)];
            switch (cue)
            {
                case OpsCue.Click: Note(samples, 1100, 0, .055f, .12f, true); break;
                case OpsCue.Action: Note(samples, 660, 0, .16f, .20f); Note(samples, 880, .065f, .20f, .14f); break;
                case OpsCue.Purchase: Note(samples, 784, 0, .18f, .22f, true); Note(samples, 1175, .09f, .23f, .17f); break;
                case OpsCue.Prepared: Note(samples, 392, 0, .10f, .20f, true); Note(samples, 784, .06f, .23f, .19f); Note(samples, 1175, .12f, .22f, .16f); break;
                case OpsCue.StaffHelp: Note(samples, 880, 0, .14f, .18f); Note(samples, 1320, .08f, .25f, .16f); break;
                case OpsCue.Growth:
                    Note(samples, 523, 0, .26f, .18f); Note(samples, 659, .10f, .26f, .18f);
                    Note(samples, 784, .20f, .35f, .19f); Note(samples, 1047, .29f, .30f, .13f); break;
                case OpsCue.Alert: Note(samples, 440, 0, .14f, .19f, true); Note(samples, 440, .23f, .17f, .19f, true); break;
                case OpsCue.Success: Note(samples, 659, 0, .24f, .18f); Note(samples, 988, .09f, .25f, .19f); break;
                case OpsCue.Damage: Note(samples, 164, 0, .30f, .25f, true); Note(samples, 123, .07f, .27f, .18f, true); break;
                case OpsCue.Month: Note(samples, 392, 0, .25f, .17f); Note(samples, 587, .08f, .26f, .16f); break;
                case OpsCue.Count: Note(samples, 740, 0, .08f, .10f, true); Note(samples, 932, .11f, .08f, .10f, true); Note(samples, 1175, .22f, .08f, .10f, true); break;
                case OpsCue.Stamp: Note(samples, 220, 0, .12f, .24f, true); Note(samples, 1047, .04f, .25f, .12f); break;
                case OpsCue.Transition: Note(samples, 330, 0, .20f, .12f); Note(samples, 494, .07f, .26f, .12f); break;
                case OpsCue.Clear:
                    Note(samples, 523, 0, .34f, .18f); Note(samples, 659, .14f, .34f, .18f);
                    Note(samples, 784, .28f, .60f, .18f); Note(samples, 1047, .40f, .52f, .15f); break;
                case OpsCue.Failure: Note(samples, 311, 0, .35f, .17f); Note(samples, 233, .20f, .42f, .20f); break;
            }
            return samples;
        }
        private static void Note(float[] samples, float hz, float start, float duration, float gain, bool percussive = false)
        {
            int first = (int)(start * SampleRate), count = (int)(duration * SampleRate);
            for (int i = 0; i < count && first + i < samples.Length; i++)
            {
                double t = i / (double)SampleRate, phase = 2 * System.Math.PI * hz * t;
                double envelope = System.Math.Min(1, t / .004) * System.Math.Pow(1 - i / (double)count, 2);
                double tone = System.Math.Sin(phase) * .8 + System.Math.Sin(phase * (percussive ? 2.71 : 2)) * .2;
                samples[first + i] += (float)(tone * gain * envelope);
            }
        }
    }
}
