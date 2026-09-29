using System;
using System.IO;
using System.Reflection;
using System.Linq;
using PatchWorkSecure.CompanyOps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PatchWorkSecure.EditorTools
{
    public static class CompanyOpsPlanningAssets
    {
        // インポート設定はEditorの公開APIを通す。素材そのものは再生成スクリプトに保存。
        public static OpsPlanningArt LoadPalette()
        {
            const string path="Assets/CompanyOps/PlanningArt.asset";
            var palette=AssetDatabase.LoadAssetAtPath<OpsPlanningArt>(path);
            if(palette==null) {palette=ScriptableObject.CreateInstance<OpsPlanningArt>();AssetDatabase.CreateAsset(palette,path);}
            foreach(var field in typeof(OpsPlanningArt).GetFields(BindingFlags.Public|BindingFlags.Instance))
            {
                if(field.Name=="titleKeyVisual")
                {
                    const string kvPath="Assets/Art/KeyVisual/title-kv.png";
                    AssetDatabase.ImportAsset(kvPath);
                    var importer=AssetImporter.GetAtPath(kvPath) as TextureImporter;
                    if(importer==null)throw new InvalidOperationException("承認済みタイトル画像がありません。");
                    importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                    importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.mipmapEnabled=false;
                    importer.SaveAndReimport();field.SetValue(palette,AssetDatabase.LoadAssetAtPath<Sprite>(kvPath));continue;
                }
                if(field.Name=="logoIcon"||field.Name=="logoWordmark")
                {
                    field.SetValue(palette,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Logo/"+(field.Name=="logoIcon"?"icon":"wordmark")+".png"));
                    continue;
                }
                string name=field.Name.StartsWith("round")?"round-"+field.Name.Substring(5):field.Name=="stageTop"?"stage-top":
                    field.Name=="shadow"?"soft-shadow":field.Name=="officeBlur"?"office-blur":field.Name=="gradient"?"planning-gradient":
                    field.Name=="stageShade"?"stage-shade":field.Name=="shine"?"button-shine":field.Name=="tail"?"speech-tail":
                    field.Name=="hinataShadow"?"hinata-shadow":field.Name=="starMuted"?"icon-star-muted":field.Name=="markerBubble"?"marker-bubble":
                    new[]{"audit","listen","map","rest","upgrade","menu","tool","star","morale"}.Contains(field.Name)?"icon-"+field.Name:field.Name;
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/"+name+".png");
                if(sprite==null)throw new InvalidOperationException("UI素材未インポート："+name);
                field.SetValue(palette,sprite);
            }
            EditorUtility.SetDirty(palette);return palette;
        }
        public static void AttachToCurrentScene()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("停止してから素材を反映してください。");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=CompanyOpsSceneBuilder.ScenePath||scene.isDirty)throw new InvalidOperationException("保存済みのCompanyYearを開いてください。");
            var game=UnityEngine.Object.FindAnyObjectByType<OpsGame>();game.PlanningArt=LoadPalette();
            EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
