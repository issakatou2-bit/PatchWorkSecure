// Unity CLI eval_file用。承認済み原本から取り込み、Editor APIだけで設定・明示割当する。
if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("再生を停止してください。");
string workspace=System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
string source=System.IO.Path.Combine(workspace,"ArtSource/Hinata/gen-20260929/final");
string target="Assets/Sprites/Hinata/v2";
System.IO.Directory.CreateDirectory(System.IO.Path.Combine(workspace,target));
var faces=new System.Collections.Generic.List<PatchWorkSecure.NavigatorPersona.NamedSprite>();
var poses=new System.Collections.Generic.List<PatchWorkSecure.NavigatorPersona.NamedSprite>();
foreach(string file in System.IO.Directory.GetFiles(source,"*.png"))
{
    string id=System.IO.Path.GetFileNameWithoutExtension(file);
    if(id.Contains("eyes_")||id.Contains("mouth_"))continue; // 口・目は次の工程で別途取り込む。
    string path=target+"/"+id+".png";
    System.IO.File.Copy(file,System.IO.Path.Combine(workspace,path),true);
    UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType=UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode=UnityEditor.SpriteImportMode.Single;
    importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=1024;
    importer.textureCompression=UnityEditor.TextureImporterCompression.CompressedHQ;importer.compressionQuality=100;
    var settings=new UnityEditor.TextureImporterSettings();importer.ReadTextureSettings(settings);
    settings.spriteMeshType=UnityEngine.SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
    var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factory.Init();
    var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
    var capability=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
    if(capability==null||!capability.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditPivot))
        throw new System.InvalidOperationException("基準点の変更に非対応："+path);
    var rects=provider.GetSpriteRects();
    foreach(var rect in rects){rect.alignment=UnityEngine.SpriteAlignment.Custom;rect.pivot=new UnityEngine.Vector2(.5f,id.StartsWith("pose_")?0:.5f);}
    provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
    var sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
    if(sprite==null)throw new System.InvalidOperationException("取り込めません："+id);
    var entry=new PatchWorkSecure.NavigatorPersona.NamedSprite{Id=id,Sprite=sprite};
    if(id.StartsWith("face_"))faces.Add(entry);else poses.Add(entry);
}
if(faces.Count!=18||poses.Count!=18)throw new System.InvalidOperationException("表情18・ポーズ18が必要です。");
var persona=UnityEditor.AssetDatabase.LoadAssetAtPath<PatchWorkSecure.NavigatorPersona>("Assets/Personas/Persona_Hinata.asset");
persona.Faces=faces.ToArray();persona.Poses=poses.ToArray();
persona.FaceNormal=persona.Face("face_normal");persona.FaceProud=persona.Face("face_proud");persona.FaceWorried=persona.Face("face_worried");
persona.FaceAlert=persona.Face("face_alert");persona.FaceRelieved=persona.Face("face_relieved");persona.FaceSad=persona.Face("face_sad");
foreach(string row in System.IO.File.ReadAllLines(System.IO.Path.Combine(workspace,"Docs/Voice/hinata-script-v1.csv")))
{
    var columns=row.Split(',');if(columns.Length<6)continue;
    if(persona.Reactions?.lines!=null)foreach(var line in persona.Reactions.lines)
        if(line!=null&&line.id==columns[0]){line.faceId=columns[4];line.poseId=columns[5];}
}
UnityEditor.EditorUtility.SetDirty(persona);if(persona.Reactions!=null)UnityEditor.EditorUtility.SetDirty(persona.Reactions);
foreach(string name in new[]{"icon","wordmark"})
{
    string path="Assets/Art/UI/Logo/"+name+".png";
    var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType=UnityEditor.TextureImporterType.Sprite;importer.spriteImportMode=UnityEditor.SpriteImportMode.Single;
    importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;
    importer.maxTextureSize=2048;importer.SaveAndReimport();
}
var iconTexture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/Art/UI/Logo/icon.png");
UnityEditor.PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown,new[]{iconTexture},UnityEditor.IconKind.Any);
PatchWorkSecure.EditorTools.CompanyOpsPlanningAssets.LoadPalette();UnityEditor.AssetDatabase.SaveAssets();
UnityEngine.Debug.Log("[HinataV2] 表情18・ポーズ18・ロゴの取り込み完了");
return new{faces=faces.Count,poses=poses.Count};
