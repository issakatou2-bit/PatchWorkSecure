using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        // テスト中の追加字形はメモリ内のフォントへ。配布アセットのキャッシュは触らない。
        private readonly Dictionary<TMP_FontAsset,TMP_FontAsset> testFonts=new Dictionary<TMP_FontAsset,TMP_FontAsset>();
        private void Awake()
        {
            if(!TestMode)return;
            Font=TestFont(Font);HeadingFont=TestFont(HeadingFont);
            if(Surface!=null)foreach(var text in Surface.GetComponentsInChildren<TMP_Text>(true))
                if(text.font!=null)text.font=TestFont(text.font);
        }
        private TMP_FontAsset TestFont(TMP_FontAsset source)
        {
            if(source==null)return null;if(testFonts.TryGetValue(source,out var cached))return cached;
            bool dynamic=source.atlasPopulationMode==AtlasPopulationMode.Dynamic;
            var font=dynamic?TMP_FontAsset.CreateFontAsset(source.sourceFontFile,(int)source.faceInfo.pointSize,source.atlasPadding,source.atlasRenderMode,source.atlasWidth,source.atlasHeight,AtlasPopulationMode.Dynamic,source.isMultiAtlasTexturesEnabled):Instantiate(source);
            font.name=source.name+"_Test_"+font.GetEntityId();font.hideFlags=HideFlags.DontSave;testFonts.Add(source,font);
            var face=source.faceInfo;font.faceInfo=face;
            if(!dynamic)
            {
                // TMPは静的フォントも破棄時に画像とMaterialを片付ける。共有アセットを渡さない。
                var textures=new Texture2D[source.atlasTextures.Length];
                for(int i=0;i<textures.Length;i++)if(source.atlasTextures[i]!=null){textures[i]=Instantiate(source.atlasTextures[i]);textures[i].hideFlags=HideFlags.DontSave;}
                font.atlasTextures=textures;font.material=Instantiate(source.material);font.material.mainTexture=textures[0];
            }
            font.material.hideFlags=HideFlags.DontSave;foreach(var atlas in font.atlasTextures)if(atlas!=null)atlas.hideFlags=HideFlags.DontSave;
            font.fallbackFontAssetTable=new List<TMP_FontAsset>();
            if(source.fallbackFontAssetTable!=null)foreach(var fallback in source.fallbackFontAssetTable)font.fallbackFontAssetTable.Add(TestFont(fallback));
            font.ReadFontAssetDefinition();return font;
        }
        private void ReleaseTestFonts()
        {
            foreach(var font in testFonts.Values)if(font!=null)Destroy(font);testFonts.Clear();
        }
    }
}
