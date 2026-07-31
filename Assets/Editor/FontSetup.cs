using UnityEngine;
using UnityEditor;
using TMPro;

namespace PatchWorkSecure.EditorTools
{
    /// <summary>
    /// フォントの「文字が抜ける」問題を根本から解消するためのエディタ拡張。
    ///
    /// 【なぜ抜けるのか】
    /// `meiryo SDF`は生成時にAtlasPopulationMode=Static（静的）で作られていた。
    /// 静的アトラスは「生成時に指定した文字だけ」を焼き込んだ画像なので、
    /// そこに含まれない文字（珍しい漢字・記号・絵文字など）は描画できず空白になり、
    /// Consoleに "The character with Unicode value ... was not found" が大量に出る。
    /// さらに元フォントへの参照(m_SourceFontFile)も切れていたため、後から足すこともできなかった。
    ///
    /// 【対処】
    /// AtlasPopulationModeをDynamic（動的）に変え、元フォント(meiryo.ttc)への参照を復元する。
    /// これで未収録の文字は実行時にその場でアトラスへ追加されるようになり、原理的に抜けなくなる。
    /// 既存の焼き込み済みグリフはそのまま残す（消すと起動時の生成量が増えるため）。
    /// アトラスが1枚で足りなくなる場合に備えてマルチアトラスも有効にする。
    /// </summary>
    public static class FontSetup
    {
        private const string FontAssetPath = "Assets/Fonts/meiryo SDF.asset";
        private const string SourceFontPath = "Assets/Fonts/meiryo.ttc";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("PatchWorkSecure/フォントの文字抜けを解消する")]
        public static void FixFontGlyphCoverage()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);

            if (fontAsset == null)
            {
                Debug.LogError($"[FontSetup] フォントアセットが見つかりません: {FontAssetPath}");
                return;
            }
            if (sourceFont == null)
            {
                Debug.LogError($"[FontSetup] 元フォントが見つかりません: {SourceFontPath}");
                return;
            }

            var so = new SerializedObject(fontAsset);

            // 元フォントへの参照を復元する（動的生成にはこれが必須）
            SetObjectRef(so, "m_SourceFontFile", sourceFont);
            SetString(so, "m_SourceFontFileGUID", AssetDatabase.AssetPathToGUID(SourceFontPath));

            // 静的(0) → 動的(1)
            SetEnum(so, "m_AtlasPopulationMode", (int)AtlasPopulationMode.Dynamic);

            // アトラス1枚に収まらなくなったら自動で増やす
            SetBool(so, "m_IsMultiAtlasTexturesEnabled", true);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(fontAsset);

            ApplyAsProjectDefaultFont(fontAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FontSetup] フォントを動的生成モードに切り替えました。未収録の文字も実行時に生成されます。");
        }

        /// <summary>
        /// TMPの既定フォントとフォールバックにも設定する。
        /// スクリプトから動的に作ったTextMeshProUGUIがフォント未指定でも日本語を表示できるようにするため。
        /// </summary>
        private static void ApplyAsProjectDefaultFont(TMP_FontAsset fontAsset)
        {
            var settings = AssetDatabase.LoadAssetAtPath<Object>(TmpSettingsPath);
            if (settings == null)
            {
                Debug.LogWarning($"[FontSetup] TMP Settingsが見つかりませんでした: {TmpSettingsPath}");
                return;
            }

            var so = new SerializedObject(settings);
            SetObjectRef(so, "m_defaultFontAsset", fontAsset);

            // フォールバック一覧の先頭に入れておくと、他フォント使用時も日本語が欠けなくなる
            var fallbacks = so.FindProperty("m_fallbackFontAssets");
            if (fallbacks != null && fallbacks.isArray)
            {
                bool alreadyListed = false;
                for (int i = 0; i < fallbacks.arraySize; i++)
                {
                    if (fallbacks.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset)
                    {
                        alreadyListed = true;
                        break;
                    }
                }
                if (!alreadyListed)
                {
                    fallbacks.InsertArrayElementAtIndex(fallbacks.arraySize);
                    fallbacks.GetArrayElementAtIndex(fallbacks.arraySize - 1).objectReferenceValue = fontAsset;
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }

        // ---- SerializedProperty操作の小道具（プロパティ名が版によって無い場合は警告だけ出す） ----

        private static void SetObjectRef(SerializedObject so, string name, Object value)
        {
            var p = so.FindProperty(name);
            if (p == null) { Warn(name); return; }
            p.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject so, string name, string value)
        {
            var p = so.FindProperty(name);
            if (p == null) { Warn(name); return; }
            p.stringValue = value;
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            var p = so.FindProperty(name);
            if (p == null) { Warn(name); return; }
            p.boolValue = value;
        }

        private static void SetEnum(SerializedObject so, string name, int value)
        {
            var p = so.FindProperty(name);
            if (p == null) { Warn(name); return; }
            if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = value;
            else p.intValue = value;
        }

        private static void Warn(string name)
        {
            Debug.LogWarning($"[FontSetup] プロパティ '{name}' が見つかりませんでした（TMPのバージョン差の可能性）。");
        }
    }
}
