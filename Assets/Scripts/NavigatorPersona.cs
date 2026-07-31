using UnityEngine;

namespace PatchWorkSecure
{
    /// <summary>
    /// ナビゲーターキャラ1人分のデータ。名前・イメージカラー・表情スプライト・セリフをまとめて持つ。
    /// 「3人から1人選べる」を実現するための単位で、キャラごとの口調の違いもここで表現する。
    ///
    /// 立ち絵が未着手でも、名前・イメージカラー・セリフだけで成立するように設計してある
    /// （スプライトがnullの場合はイメージカラーで塗った枠がプレースホルダーとして表示される）。
    /// セリフ欄が空文字の場合は、GameManager側の汎用セリフに自動でフォールバックする。
    /// </summary>
    [CreateAssetMenu(fileName = "NavigatorPersona", menuName = "PatchWorkSecure/Navigator Persona")]
    public class NavigatorPersona : ScriptableObject
    {
        [Header("基本情報")]
        public string DisplayName = "（未設定）";
        [TextArea] public string Description = "";

        /// <summary>名前チップ・吹き出しの縁・立ち絵プレースホルダーに使うイメージカラー。</summary>
        public Color ThemeColor = new Color(0.55f, 0.60f, 0.85f);

        [Header("表情スプライト（素材が届くまでは未割当でよい）")]
        public Sprite FaceNormal;
        public Sprite FaceWorried;
        public Sprite FaceAlert;
        public Sprite FaceRelieved;
        public Sprite FaceProud;
        public Sprite FaceSad;

        [Header("日常フェーズのセリフ")]
        [TextArea] public string LineNormal;      // 特に問題がないとき
        [TextArea] public string LineNoDefense;   // 対策を1つも導入していない
        [TextArea] public string LineLowBudget;   // 予算が心もとない
        [TextArea] public string LineLowTrust;    // 人望が低い
        [TextArea] public string LineHighStress;  // 社員のストレスが高い
        [TextArea] public string LineEndgame;     // 年度末が近い
        [TextArea] public string LineGood;        // 順調なとき

        [Header("攻撃・パリィのセリフ")]
        [TextArea] public string LineAttackSevere; // S級の攻撃
        [TextArea] public string LineAttackNew;    // 新興・新人（見慣れない手口）
        [TextArea] public string LineAttackNormal; // それ以外
        [TextArea] public string LineParry;        // パリィ直前

        [Header("結果・エンディングのセリフ")]
        [TextArea] public string LineWin;        // 危なげなく防いだ
        [TextArea] public string LineWinNarrow;  // 紙一重で防いだ
        [TextArea] public string LineLose;       // 突破された
        [TextArea] public string LineGameOver;   // ゲームオーバー
        [TextArea] public string LineClear;      // クリア
    }
}
