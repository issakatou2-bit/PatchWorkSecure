using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace PatchWorkSecure.EditorTools
{
    // 日本語の既知文字を事前収録し、追加文字だけ同じ書体の動的アトラスへ送る。
    public static class CompanyOpsTypography
    {
        private const string Root = "Assets/Fonts/CompanyYear/";
        public static void Configure(PatchWorkSecure.CompanyOps.OpsGame game)
        {
            string corpus = string.Join("", Directory.GetFiles("Assets/Scripts/CompanyOps", "*.cs").Select(File.ReadAllText));
            corpus += "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz ￥＋−×％→～！？、。";
            corpus = new string(corpus.Where(c => !char.IsControl(c) && !char.IsSurrogate(c)).Distinct().OrderBy(c => c).ToArray());
            var bodyDynamic = Create("ZenKakuGothicNew-Medium", "BodyDynamic", null);
            var body = Create("ZenKakuGothicNew-Medium", "Body", corpus);
            body.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { bodyDynamic };
            var headingDynamic = Create("RoundedMplus1c-Bold", "HeadingDynamic", null);
            // Zen未収録の全角チルダなども、OSフォントに依存せず補完する。
            body.fallbackFontAssetTable.Add(headingDynamic);
            var heading = Create("RoundedMplus1c-Bold", "Heading", corpus);
            heading.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { headingDynamic, body };
            EditorUtility.SetDirty(body); EditorUtility.SetDirty(heading);
            game.Font = body; game.HeadingFont = heading;
            AssetDatabase.SaveAssets();
        }
        private static TMP_FontAsset Create(string sourceName, string assetName, string corpus)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(Root + sourceName + ".ttf");
            if (source == null) throw new System.IO.FileNotFoundException("日本語フォントがありません: " + sourceName);
            string path = Root + assetName + ".asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(source, 44, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                font.name = assetName; AssetDatabase.CreateAsset(font, path);
                font.material.name = assetName + " Material"; AssetDatabase.AddObjectToAsset(font.material, font);
            }
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            if (corpus != null)
            {
                string additions = new string(corpus.Where(c => !font.HasCharacter(c)).ToArray());
                if (additions.Length > 0) font.TryAddCharacters(additions, out _);
                // TMPは追加不要の場合も入力全体をmissingに返すため、収録結果を直接検査する。
                string missing = new string(corpus.Where(c => !font.HasCharacter(c)).ToArray());
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                Debug.Log("[CompanyOps Fonts] " + assetName + " / 収録 " + font.characterTable.Count + " / 補完対象 " + missing);
            }
            foreach (var atlas in font.atlasTextures)
                if (atlas != null && !AssetDatabase.Contains(atlas)) { atlas.name = assetName + " Atlas"; AssetDatabase.AddObjectToAsset(atlas, font); }
            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = corpus == null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var face = font.faceInfo; face.scale = 1; font.faceInfo = face;
            EditorUtility.SetDirty(font);
            return font;
        }
        public static void CopyLicenses(string outputDirectory)
        {
            string folder = Path.Combine(outputDirectory, "FontLicenses"); Directory.CreateDirectory(folder);
            foreach (string path in Directory.GetFiles(Root, "*-OFL.txt")) File.Copy(path, Path.Combine(folder, Path.GetFileName(path)), true);
        }
    }
}
