// Unity CLI eval_fileで実行する。プロジェクトの素材だけをEditor APIで取り込む。
UnityEditor.AssetDatabase.Refresh();
foreach (var path in System.IO.Directory.GetFiles("Assets/Art/UI", "*.png"))
{
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path.Replace('\\','/'));
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
    importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
    importer.filterMode = UnityEngine.FilterMode.Bilinear;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.maxTextureSize = 2048;
    importer.SaveAndReimport();
    var factories = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories(); factories.Init();
    var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
    var edit = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
    if (edit == null || !edit.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditBorder))
        throw new System.InvalidOperationException("9-slice境界を編集できないImporter：" + path);
    string name = System.IO.Path.GetFileNameWithoutExtension(path);
    float border = name.StartsWith("round-") ? int.Parse(name.Substring(6)) : name == "stage-top" ? 28 : name == "soft-shadow" ? 68 : 0;
    var rects = provider.GetSpriteRects();
    foreach (var rect in rects) rect.border = new UnityEngine.Vector4(border,border,border,border);
    provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
}
PatchWorkSecure.EditorTools.CompanyOpsPlanningAssets.AttachToCurrentScene();
UnityEngine.Debug.Log("[PlanningUI] UI素材とCompanyYearシーンへの割当を保存しました。");
