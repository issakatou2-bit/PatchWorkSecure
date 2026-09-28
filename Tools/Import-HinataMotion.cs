// Unity CLI eval_file用。基本ポーズの原本4枚をそのまま取り込み、切り替える範囲を設定する。
if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("再生を停止してください。");
string workspace=System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
foreach(string name in new[]{"pose_fists","pose_fists_eyes_half","pose_fists_eyes_closed","pose_fists_mouth_closed"})
{
    string path="Assets/Sprites/Hinata/v2/"+name+".png";
    System.IO.File.Copy(System.IO.Path.Combine(workspace,"ArtSource/Hinata/gen-20260929/final/"+name+".png"),System.IO.Path.Combine(workspace,path),true);
    UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType=UnityEditor.TextureImporterType.Sprite;importer.spriteImportMode=UnityEditor.SpriteImportMode.Single;
    importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=1024;importer.textureCompression=UnityEditor.TextureImporterCompression.CompressedHQ;importer.compressionQuality=100;
    var settings=new UnityEditor.TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=UnityEngine.SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
    var factories=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factories.Init();
    var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
    var cap=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
    if(cap==null||!cap.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditPivot))throw new System.InvalidOperationException("基準点に非対応");
    var rects=provider.GetSpriteRects();foreach(var rect in rects){rect.alignment=UnityEngine.SpriteAlignment.Custom;rect.pivot=new UnityEngine.Vector2(.5f,0);}
    provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
}
var persona=UnityEditor.AssetDatabase.LoadAssetAtPath<PatchWorkSecure.NavigatorPersona>("Assets/Personas/Persona_Hinata.asset");
var frame=new PatchWorkSecure.NavigatorPersona.FaceAnimationFrames{PoseId="pose_fists",Expression="normal"};
frame.MouthOpen=persona.Pose("pose_fists");
frame.EyesHalf=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Sprites/Hinata/v2/pose_fists_eyes_half.png");
frame.EyesClosed=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Sprites/Hinata/v2/pose_fists_eyes_closed.png");
frame.MouthClosed=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Sprites/Hinata/v2/pose_fists_mouth_closed.png");
// 顔だけを重ね、口は最後に別の範囲を上書き。閉眼と閉口が同時に成立する。
frame.EyeRegion=new UnityEngine.Rect(108f/496,337f/615,285f/496,184f/615);
frame.MouthRegion=new UnityEngine.Rect(229f/496,329f/615,66f/496,45f/615);
persona.AnimationFrames=new[]{frame};
var icons=new System.Collections.Generic.List<UnityEngine.Sprite>();
foreach(string id in new[]{"alert","sweat","joy","anger","thinking","dust"})
{
    string path="Assets/Art/UI/HinataEmotions/"+id+".png";UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);importer.textureType=UnityEditor.TextureImporterType.Sprite;importer.spriteImportMode=UnityEditor.SpriteImportMode.Single;
    importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
    icons.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path));
}
persona.EmotionMarks=icons.ToArray();UnityEditor.EditorUtility.SetDirty(persona);UnityEditor.AssetDatabase.SaveAssets();
UnityEngine.Debug.Log("[HinataMotion] 基本ポーズ4枚と感情マーク6種を登録");return new{frames=persona.AnimationFrames.Length,marks=icons.Count};
