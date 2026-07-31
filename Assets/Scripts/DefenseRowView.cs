using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PatchWorkSecure
{
    /// <summary>
    /// 対策リスト1行分の見た目をまとめた部品。
    /// 「アイコン + 対策名 + レベル + 右寄せの金額」という並びを、
    /// 文字列の組み立てではなく個別のUI要素として持つことで、
    /// レベルだけ緑にする・金額だけ右端に置く、といった表現ができるようにしている。
    /// </summary>
    public class DefenseRowView : MonoBehaviour
    {
        public Image Background;
        public Image IconFrame;      // アイコンの角丸背景（対策ごとの色分けに使う）
        public Image IconGlyph;      // アイコン内側の図形
        public Image SelectedEdge;   // 左端の縦線（導入済みの目印）
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI LevelText;
        public TextMeshProUGUI CostText;
    }
}
