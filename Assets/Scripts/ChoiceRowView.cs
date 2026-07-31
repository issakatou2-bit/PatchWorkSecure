using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PatchWorkSecure
{
    /// <summary>
    /// 攻撃への対応選択肢1つ分の見た目をまとめた部品。
    /// 参考UIに合わせて「番号バッジ + 見出し + 補足（コスト）」の構成にしてある。
    /// 番号があると「1を押す」と口に出せるようになり、選択肢としての手触りが良くなる。
    /// </summary>
    public class ChoiceRowView : MonoBehaviour
    {
        public Image Background;
        public Image NumberBadge;
        public TextMeshProUGUI NumberText;
        public TextMeshProUGUI LabelText;
        public TextMeshProUGUI DetailText;
    }
}
