using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PatchWorkSecure
{
    [Serializable]
    public class QuizQuestion
    {
        public string Id;
        public string Question;
        public string[] Options;
        public int AnswerIndex;
    }

    /// <summary>1セッション分の教育効果データ。</summary>
    [Serializable]
    public class EduSession
    {
        public int PreCorrect;
        public int PreTotal;
        public int PostCorrect;
        public int PostTotal;
        public string PlayedAt;
    }

    /// <summary>全セッションの累計データ。JSONにしてPlayerPrefsへ保存する。</summary>
    [Serializable]
    public class EduStats
    {
        public int PreCorrect;
        public int PreTotal;
        public int PostCorrect;
        public int PostTotal;
        public int Sessions;
        public List<EduSession> History = new List<EduSession>();

        public float PreRate => PreTotal > 0 ? (float)PreCorrect / PreTotal : 0f;
        public float PostRate => PostTotal > 0 ? (float)PostCorrect / PostTotal : 0f;
        /// <summary>プレイ前後での正答率の伸び（教育効果の指標）。</summary>
        public float Improvement => PostRate - PreRate;
    }

    /// <summary>
    /// 教育効果測定を担当するクラス。
    /// プレイ前後で同じ難度のクイズを出し、正答率の変化を記録する。
    /// 卒研で求められる「教育効果の定量評価」のデータはここから取得できる。
    /// </summary>
    public static class EducationTracker
    {
        private const string StorageKey = "pws_edu_stats";

        public static readonly List<QuizQuestion> QuizPool = new List<QuizQuestion>
        {
            new QuizQuestion {
                Id = "q1", Question = "フィッシング攻撃への対策として最も基本的なものは？",
                Options = new[] { "MFA導入", "DDoS対策", "バックアップのみ" }, AnswerIndex = 0
            },
            new QuizQuestion {
                Id = "q2", Question = "ランサムウェア被害からの復旧に最も重要な備えは？",
                Options = new[] { "ファイアウォール", "定期バックアップ", "社員研修のみ" }, AnswerIndex = 1
            },
            new QuizQuestion {
                Id = "q3", Question = "ゼロトラストの考え方に近いものは？",
                Options = new[] { "一度認証したら信頼し続ける", "内外問わず都度検証する", "社内なら無条件に信頼する" }, AnswerIndex = 1
            },
            new QuizQuestion {
                Id = "q4", Question = "サプライチェーン攻撃の説明として正しいものは？",
                Options = new[] { "自社システムの脆弱性を直接突く攻撃", "取引先・委託先を経由して侵入する攻撃", "大量アクセスでサービスを停止させる攻撃" }, AnswerIndex = 1
            },
            new QuizQuestion {
                Id = "q5", Question = "ゼロデイ脆弱性とは？",
                Options = new[] { "修正パッチが公開される前の脆弱性", "発見から1年以上放置された脆弱性", "パッチ適用後に残る脆弱性" }, AnswerIndex = 0
            },
            new QuizQuestion {
                Id = "q6", Question = "多層防御の考え方として正しいものは？",
                Options = new[] { "最強の対策を1つ導入すれば十分", "複数の対策を重ね、1つ破られても致命傷にしない", "対策は多いほど社員の負担が減る" }, AnswerIndex = 1
            },
            new QuizQuestion {
                Id = "q7", Question = "内部不正のリスクを高める要因として適切なものは？",
                Options = new[] { "従業員の不満やストレスの蓄積", "ファイアウォールの設定漏れ", "サーバーの物理的な老朽化" }, AnswerIndex = 0
            },
            new QuizQuestion {
                Id = "q8", Question = "情報セキュリティのCIAに含まれないものは？",
                Options = new[] { "機密性(Confidentiality)", "可用性(Availability)", "互換性(Compatibility)" }, AnswerIndex = 2
            },
        };

        /// <summary>プールからランダムにn問抽出する。</summary>
        public static List<QuizQuestion> SampleQuestions(int n)
        {
            var rng = new System.Random();
            return QuizPool.OrderBy(_ => rng.Next()).Take(Mathf.Min(n, QuizPool.Count)).ToList();
        }

        public static EduStats Load()
        {
            string json = PlayerPrefs.GetString(StorageKey, "");
            if (string.IsNullOrEmpty(json)) return new EduStats();
            try { return JsonUtility.FromJson<EduStats>(json) ?? new EduStats(); }
            catch { return new EduStats(); }
        }

        /// <summary>1セッション分の結果を累計に加算して保存する。</summary>
        public static EduStats RecordSession(int preCorrect, int preTotal, int postCorrect, int postTotal)
        {
            var stats = Load();
            stats.PreCorrect += preCorrect;
            stats.PreTotal += preTotal;
            stats.PostCorrect += postCorrect;
            stats.PostTotal += postTotal;
            stats.Sessions++;
            stats.History.Add(new EduSession
            {
                PreCorrect = preCorrect,
                PreTotal = preTotal,
                PostCorrect = postCorrect,
                PostTotal = postTotal,
                PlayedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            });

            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(stats));
            PlayerPrefs.Save();
            return stats;
        }

        /// <summary>
        /// 研究データとしてCSV出力する。アンケート分析や発表資料の作成に使う。
        /// </summary>
        public static string ExportCsv()
        {
            var stats = Load();
            var lines = new List<string> { "session,played_at,pre_correct,pre_total,post_correct,post_total" };
            for (int i = 0; i < stats.History.Count; i++)
            {
                var h = stats.History[i];
                lines.Add($"{i + 1},{h.PlayedAt},{h.PreCorrect},{h.PreTotal},{h.PostCorrect},{h.PostTotal}");
            }
            return string.Join("\n", lines);
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(StorageKey);
            PlayerPrefs.Save();
        }
    }
}
