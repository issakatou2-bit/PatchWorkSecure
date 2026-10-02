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
            AttachMinigame(controller);
            CompanyOpsTypography.Configure(controller);
            controller.OfficeArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Office/office-topdown.png");
            controller.Navigator = AssetDatabase.LoadAssetAtPath<NavigatorPersona>("Assets/Personas/Persona_Hinata.asset");
            if (controller.Navigator != null)
            {
                var reactions=UpdateHinataVoice();
                controller.Navigator.Reactions = reactions;
                EditorUtility.SetDirty(controller.Navigator);
            }
            controller.PanelSprite = RoundedPanel();
            controller.PlanningArt = CompanyOpsPlanningAssets.LoadPalette();
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

        public static void AttachMinigame(OpsGame controller)
        {
            var host=controller.GetComponent<OpsMinigameHost>();
            if(host==null)host=controller.gameObject.AddComponent<OpsMinigameHost>();
            host.Owner=controller;
        }

        [MenuItem("PatchWorkSecure/新しい試作/ひなたの台本と試遊用音声を更新")]
        public static OpsReactionBank UpdateHinataVoice()
        {
            const string metadata="Assets/Personas/HinataReactions.asset";
            var bank=AssetDatabase.LoadAssetAtPath<OpsReactionBank>(metadata);
            if(bank==null){bank=ScriptableObject.CreateInstance<OpsReactionBank>();AssetDatabase.CreateAsset(bank,metadata);}
            // 配布するアセットには非公開MP3への参照を保存しない。
            bank.lines=ReadHinataScript();EditorUtility.SetDirty(bank);
            const string source="Assets/Audio/CompanyYear/VoiceTest/Hinata";
            if(Directory.Exists(source))
            {
                const string localPath=source+"/Resources/HinataVoiceTest.asset";
                Directory.CreateDirectory(source+"/Resources");AssetDatabase.Refresh();
                var local=AssetDatabase.LoadAssetAtPath<OpsReactionBank>(localPath);
                if(local==null){local=ScriptableObject.CreateInstance<OpsReactionBank>();AssetDatabase.CreateAsset(local,localPath);}
                local.lines=ReadHinataScript();
                foreach(var line in local.lines)line.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(source+"/"+line.id+".mp3")??AssetDatabase.LoadAssetAtPath<AudioClip>(source+"/"+line.id+".wav");
                EditorUtility.SetDirty(local);
            }
            var persona=AssetDatabase.LoadAssetAtPath<NavigatorPersona>("Assets/Personas/Persona_Hinata.asset");
            if(persona!=null){persona.Reactions=bank;EditorUtility.SetDirty(persona);}
            AssetDatabase.SaveAssets();return bank;
        }
        private static OpsReactionLine[] ReadHinataScript()
        {
            const string path="Docs/Voice/hinata-script-v2.csv";
            if(!File.Exists(path))return OpsReactionBank.ScriptV2();
            var result=new System.Collections.Generic.List<OpsReactionLine>();
            foreach(var row in File.ReadAllLines(path,System.Text.Encoding.UTF8).Skip(1))
            {
                if(string.IsNullOrWhiteSpace(row))continue;
                var cells=new System.Collections.Generic.List<string>();var cell=new System.Text.StringBuilder();bool quoted=false;
                for(int i=0;i<row.Length;i++)
                {
                    if(row[i]=='"'){if(quoted&&i+1<row.Length&&row[i+1]=='"'){cell.Append('"');i++;}else quoted=!quoted;}
                    else if(row[i]==','&&!quoted){cells.Add(cell.ToString());cell.Clear();}else cell.Append(row[i]);
                }
                cells.Add(cell.ToString());if(quoted||cells.Count!=8)throw new System.FormatException("ひなた台本のCSV形式が不正です。");
                System.Enum.TryParse(cells[0].Split('_')[0],true,out OpsReaction reaction);
                var diary=OpsDiaryCatalog.Entries.FirstOrDefault(e=>e.voiceId==cells[0]);
                result.Add(new OpsReactionLine{id=cells[0],caption=diary?.intro??cells[3],scene=cells[2],reaction=reaction,faceId=cells[4],poseId=cells[5],fullSpeech=cells[1]=="全文"||cells[1]=="日記",extra=cells[0].StartsWith("extra_")});
            }
            if(result.Count!=OpsCatalog.VoiceScriptLineCount||result.Select(l=>l.id).Distinct().Count()!=OpsCatalog.VoiceScriptLineCount)throw new System.FormatException("台本v2は重複のない"+OpsCatalog.VoiceScriptLineCount+"行が必要です。");
            return result.ToArray();
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
            const string musicRecord = "Docs/CompanyYear-Music-2026-09-28.md";
            if (File.Exists(musicRecord)) File.Copy(musicRecord, "Builds/CompanyYear/BGM制作記録.md", true);
            const string voiceNotices="Docs/Voice/Licenses";
            if(Directory.Exists(voiceNotices))
            {
                string target="Builds/CompanyYear/Licenses/CompanionVoices";Directory.CreateDirectory(target);
                foreach(string file in Directory.GetFiles(voiceNotices))File.Copy(file,Path.Combine(target,Path.GetFileName(file)),true);
            }
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
