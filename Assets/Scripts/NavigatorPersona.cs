using UnityEngine;

namespace PatchWorkSecure
{
    /// <summary>
    /// ナビゲーターキャラ1人分の名前・表情スプライトをまとめたデータ。
    /// 「3人から1人選べるアドバイスキャラ」を実現するための土台。
    /// 素材(立ち絵)が届く前はスプライトが全てnullでも、名前だけの選択UIとして機能する。
    /// </summary>
    [CreateAssetMenu(fileName = "NavigatorPersona", menuName = "PatchWorkSecure/Navigator Persona")]
    public class NavigatorPersona : ScriptableObject
    {
        public string DisplayName = "（未設定）";
        [TextArea] public string Description = "";

        [Header("表情スプライト（素材が届くまでは未割当でよい）")]
        public Sprite FaceNormal;
        public Sprite FaceWorried;
        public Sprite FaceAlert;
        public Sprite FaceRelieved;
        public Sprite FaceProud;
        public Sprite FaceSad;
    }
}
