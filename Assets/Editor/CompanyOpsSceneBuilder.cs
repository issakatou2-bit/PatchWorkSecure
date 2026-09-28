using System.IO;
using System.Linq;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PatchWorkSecure.EditorTools
{
    public static class CompanyOpsSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/CompanyYear.unity";
        [MenuItem("PatchWorkSecure/新しい試作/情シスの一年を構築")]
        public static void BuildScene()
        {
            // 既存シーンの未保存編集を破棄しない。バッチでは保存済みの別シーンだけを作成する。
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera"; camera.GetComponent<Camera>().backgroundColor = new Color(.071f, .086f, .114f);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvasObject = new GameObject("CompanyOpsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var surface = new GameObject("GameSurface", typeof(RectTransform)).GetComponent<RectTransform>();
            surface.SetParent(canvasObject.transform, false); surface.anchorMin = surface.anchorMax = new Vector2(.5f, .5f);
            surface.pivot = new Vector2(.5f, .5f); surface.sizeDelta = new Vector2(1600, 900);
            var controller = new GameObject("CompanyOpsGame").AddComponent<OpsGame>();
            controller.Surface = surface;
            CompanyOpsTypography.Configure(controller);
            controller.OfficeArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Office/office-topdown.png");
            controller.Navigator = AssetDatabase.LoadAssetAtPath<NavigatorPersona>("Assets/Personas/Persona_Hinata.asset");
            if (controller.Navigator != null)
            {
                const string reactionPath = "Assets/Personas/HinataReactions.asset";
                var reactions = AssetDatabase.LoadAssetAtPath<OpsReactionBank>(reactionPath);
                if (reactions == null)
                {
                    reactions = ScriptableObject.CreateInstance<OpsReactionBank>(); reactions.lines = OpsReactionBank.Defaults();
                    AssetDatabase.CreateAsset(reactions, reactionPath);
                }
                controller.Navigator.Reactions = reactions;
                EditorUtility.SetDirty(controller.Navigator);
            }
            controller.PanelSprite = RoundedPanel();
            if (controller.PanelSprite == null) throw new System.InvalidOperationException("角丸UIスプライトがありません。");
            Directory.CreateDirectory("Assets/CompanyOps");
            controller.Sounds = AssetDatabase.LoadAssetAtPath<OpsSoundPalette>("Assets/CompanyOps/YearSounds.asset");
            if (controller.Sounds == null)
            {
                controller.Sounds = ScriptableObject.CreateInstance<OpsSoundPalette>();
                AssetDatabase.CreateAsset(controller.Sounds, "Assets/CompanyOps/YearSounds.asset");
            }
            var prefab = new GameObject("OpsChoice", typeof(RectTransform), typeof(Image), typeof(Button));
            prefab.GetComponent<Image>().sprite = controller.PanelSprite; prefab.GetComponent<Image>().type = Image.Type.Sliced;
            var button = prefab.GetComponent<Button>(); button.targetGraphic = prefab.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f); colors.pressedColor = new Color(.78f, .78f, .78f);
            colors.disabledColor = new Color(.55f, .55f, .55f); button.colors = colors;
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(prefab.transform, false);
            label.GetComponent<TextMeshProUGUI>().font = controller.Font; label.GetComponent<TextMeshProUGUI>().raycastTarget = false;
            controller.ChoicePrefab = PrefabUtility.SaveAsPrefabAsset(prefab, "Assets/CompanyOps/OpsChoice.prefab").GetComponent<Button>();
            Object.DestroyImmediate(prefab);
            controller.BuildPreview();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new System.IO.IOException("試作シーンを保存できませんでした。");
            // 旧版の起動順は変えず、新しい試作だけ追加する。
            if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[CompanyOps] 保存完了: " + ScenePath);
        }

        [MenuItem("PatchWorkSecure/新しい試作/情シスの一年を開く")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("PatchWorkSecure/新しい試作/Windows試遊版をビルド")]
        public static void BuildPlayer()
        {
            Directory.CreateDirectory("Builds/CompanyYear");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/CompanyYear/PatchWorkSecure-Year.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("試遊版のビルドに失敗: " + report.summary.result);
            CompanyOpsTypography.CopyLicenses("Builds/CompanyYear");
            Debug.Log("[CompanyOps] Windows試遊版のビルド成功");
        }
        public static void BuildRelease() { BuildScene(); BuildPlayer(); }

        private static Sprite RoundedPanel()
        {
            const string path = "Assets/CompanyOps/RoundedPanel.asset";
            var panel = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (panel != null) return panel;
            // 標準の円のテクスチャを共有する。画像生成や元のインポーターの編集は行わない。
            var circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (circle == null) throw new System.InvalidOperationException("標準の円形UI素材がありません。");
            var rect = circle.rect;
            var border = new Vector4(rect.width / 2 - 1, rect.height / 2 - 1, rect.width / 2 - 1, rect.height / 2 - 1);
            panel = Sprite.Create(circle.texture, rect, new Vector2(.5f, .5f), circle.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            panel.name = "CompanyYearRoundedPanel";
            AssetDatabase.CreateAsset(panel, path);
            return panel;
        }
    }
}
