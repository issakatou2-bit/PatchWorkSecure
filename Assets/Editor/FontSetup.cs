using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.LowLevel;

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
    /// 動的生成に加えてアトラスの読み書きを許可する。元フォントに無い絵文字は対象外。
    /// 読み取り不可の古いアトラスだけはTMPの公開APIで作り直す（アセット参照は維持する）。
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

            // バッチ起動直後は描画が始まっていないため、ネイティブの文字描画機構を先に初期化する。
            FontEngine.InitializeFontEngine();
            var faceError = FontEngine.LoadFontFace(sourceFont, (int)fontAsset.faceInfo.pointSize);
            if (faceError != FontEngineError.Success)
                throw new System.InvalidOperationException($"元の日本語フォントを読み込めません: {faceError}");

            var so = new SerializedObject(fontAsset);

            // 元フォントへの参照を復元する（動的生成にはこれが必須）
            SetObjectRef(so, "m_SourceFontFile", sourceFont);
            SetString(so, "m_SourceFontFileGUID", AssetDatabase.AssetPathToGUID(SourceFontPath));

            // 静的(0) → 動的(1)
            SetEnum(so, "m_AtlasPopulationMode", (int)AtlasPopulationMode.Dynamic);

            // アトラス1枚に収まらなくなったら自動で増やす
            SetBool(so, "m_IsMultiAtlasTexturesEnabled", true);

            so.ApplyModifiedProperties();
            // 静的生成時のアトラスは読み取り不可。Dynamicへの変更だけでは追加に失敗する。
            bool rebuildAtlas = false;
            foreach (var atlas in fontAsset.atlasTextures)
                if (atlas != null && !atlas.isReadable) rebuildAtlas = true;
            // TMP自身のエディタ処理が読み取り属性・パッキング情報をまとめて初期化する。
            if (rebuildAtlas) fontAsset.ClearFontAssetData();
            foreach (var atlas in fontAsset.atlasTextures)
            {
                if (atlas == null) continue;
                if (!atlas.isReadable) throw new System.InvalidOperationException("アトラスの読み書き設定を復元できませんでした。");
                EditorUtility.SetDirty(atlas);
            }
            const string probe = "ひなた 予算 人望 ストレス 緒常選済測和誠丈夫費黄思弾返学習記録低音声：　→ ¥0123456789%";
            // TMPは「すべて収録済みで追加ゼロ」の場合もfalseを返すため、追加の戻り値では判定しない。
            fontAsset.TryAddCharacters(probe, out _);
            if (!fontAsset.HasCharacters(probe, out System.Collections.Generic.List<char> missing))
                throw new System.InvalidOperationException($"日本語フォントの描画準備に失敗: {new string(missing.ToArray())}");
            EditorUtility.SetDirty(fontAsset);

            ApplyAsProjectDefaultFont(fontAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FontSetup] 読み書き可能な動的アトラスを確認し、日本語の追加生成テストに成功しました。");
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
