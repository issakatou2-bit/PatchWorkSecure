using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace PatchWorkSecure.EditorTools
{
    /// <summary>
    /// キャラの立ち絵をファイル名から自動で`NavigatorPersona`に割り当てるエディタ拡張。
    ///
    /// 置き場所:  Assets/Sprites/&lt;キャラ名&gt;/  （例: Assets/Sprites/Hinata/）
    /// ファイル名: 表情がファイル名に含まれていれば拾う（大文字小文字は問わない）。
    ///             例) hinata_normal.png / normal.png / ひなた_通常.png すべて可
    ///
    /// 対応する表情キーワードは ExpressionSlots を参照。
    /// 画像のインポート設定（Sprite化・透過・ミップマップ無効など）もここで自動的に整える。
    /// </summary>
    public static class PersonaSpriteImporter
    {
        private const string SpriteRoot = "Assets/Sprites";
        private const string PersonaDir = "Assets/Personas";

        /// <summary>NavigatorPersonaのフィールド名と、それに割り当てるファイル名キーワード。</summary>
        private static readonly (string field, string[] keywords)[] ExpressionSlots =
        {
            ("FaceNormal",   new[] { "normal", "default", "smile", "通常", "普通", "笑顔" }),
            ("FaceWorried",  new[] { "worried", "thinking", "困", "心配", "考" }),
            ("FaceAlert",    new[] { "alert", "shocked", "surprised", "驚", "警戒" }),
            ("FaceRelieved", new[] { "relieved", "embarrassed", "shy", "安心", "照" }),
            ("FaceProud",    new[] { "proud", "confident", "得意", "自信", "ドヤ" }),
            ("FaceSad",      new[] { "sad", "crying", "cry", "泣", "悲", "落ち込" }),
        };

        [MenuItem("PatchWorkSecure/キャラ立ち絵を取り込む")]
        public static void ImportAll()
        {
            int assigned = ImportAllInternal(verbose: true);
            EditorUtility.DisplayDialog(
                "立ち絵の取り込み",
                assigned > 0
                    ? $"{assigned}枚の立ち絵を割り当てました。"
                    : $"割り当てられる画像が見つかりませんでした。\n\n{SpriteRoot}/<キャラ名>/ に、\n" +
                      "表情名を含むファイル名(normal / worried / alert / relieved / proud / sad)で置いてください。",
                "OK");
        }

        /// <summary>
        /// 実際の割り当て処理。シーン構築からも呼ぶので、画像が無くてもエラーにはしない。
        /// </summary>
        public static int ImportAllInternal(bool verbose)
        {
            if (!Directory.Exists(SpriteRoot)) return 0;

            int assignedCount = 0;
            foreach (string personaPath in Directory.GetFiles(PersonaDir, "*.asset"))
            {
                var persona = AssetDatabase.LoadAssetAtPath<NavigatorPersona>(personaPath.Replace('\\', '/'));
                if (persona == null) continue;

                // Persona_Hinata.asset → Assets/Sprites/Hinata/
                string folderName = Path.GetFileNameWithoutExtension(personaPath).Replace("Persona_", "");
                string spriteDir = $"{SpriteRoot}/{folderName}";
                if (!Directory.Exists(spriteDir)) continue;

                assignedCount += AssignForPersona(persona, spriteDir, verbose);
            }

            if (assignedCount > 0) AssetDatabase.SaveAssets();
            return assignedCount;
        }

        private static int AssignForPersona(NavigatorPersona persona, string spriteDir, bool verbose)
        {
            var imagePaths = Directory
                .GetFiles(spriteDir)
                .Select(p => p.Replace('\\', '/'))
                .Where(p => p.EndsWith(".png") || p.EndsWith(".jpg") || p.EndsWith(".psd"))
                .ToList();
            if (imagePaths.Count == 0) return 0;

            foreach (string path in imagePaths) EnsureSpriteImportSettings(path);

            var so = new SerializedObject(persona);
            int assigned = 0;

            foreach (var (field, keywords) in ExpressionSlots)
            {
                string match = FindBestMatch(imagePaths, keywords);
                if (match == null) continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(match);
                if (sprite == null) continue;

                var prop = so.FindProperty(field);
                if (prop == null) continue;

                prop.objectReferenceValue = sprite;
                assigned++;
                if (verbose) Debug.Log($"[立ち絵] {persona.DisplayName} の {field} ← {Path.GetFileName(match)}");
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(persona);
            return assigned;
        }

        /// <summary>ファイル名にキーワードを含む画像を探す。前方のキーワードほど優先度が高い。</summary>
        private static string FindBestMatch(List<string> imagePaths, string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                foreach (string path in imagePaths)
                {
                    string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                    if (name.Contains(keyword.ToLowerInvariant())) return path;
                }
            }
            return null;
        }

        /// <summary>UI立ち絵として使えるインポート設定に揃える。</summary>
        private static void EnsureSpriteImportSettings(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
            // 立ち絵は縁が命なので、圧縮ノイズを避けて非圧縮で読み込む
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
