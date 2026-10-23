using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace VeinVanguard.Editor
{
    public static class DiagnosticAssetSetup
    {
        public static void Configure(BattleManager game)
        {
            if (!game || Application.isPlaying) throw new InvalidOperationException("Configure learning sprites outside Play Mode.");
            const string root = "Assets/Game/UI/Diagnostics";
            var paths = Directory.GetFiles(root, "*.png", SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')).OrderBy(p=>p).ToArray();
            if (paths.Length != 27) throw new InvalidOperationException("Expected 20 learning PNGs plus the plasma weapon and six formula icons.");
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var sprites = new Sprite[paths.Length];
            for (int i=0;i<paths.Length;i++)
            {
                string path=paths[i];
                var expectedFilter=Path.GetFileNameWithoutExtension(path).StartsWith("formula_",StringComparison.Ordinal)?FilterMode.Point:FilterMode.Bilinear;
                int expectedTextureSize=expectedFilter==FilterMode.Point?128:2048;
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(!importer)throw new InvalidOperationException("Missing learning texture importer: "+path);
                bool changed=importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Single||
                    !importer.alphaIsTransparency||importer.mipmapEnabled||importer.npotScale!=TextureImporterNPOTScale.None||
                    importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.maxTextureSize!=expectedTextureSize||
                    importer.filterMode!=expectedFilter||importer.wrapMode!=TextureWrapMode.Clamp||!Mathf.Approximately(importer.spritePixelsPerUnit,100);
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                changed|=settings.spriteMeshType!=SpriteMeshType.FullRect;
                if(changed)
                {
                    importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                    importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
                    importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=expectedTextureSize;
                    importer.filterMode=expectedFilter;importer.wrapMode=TextureWrapMode.Clamp;importer.spritePixelsPerUnit=100;
                    importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
                var provider=factory.GetSpriteEditorDataProviderFromObject(importer);
                if(provider==null)throw new InvalidOperationException("No sprite data provider: "+path);
                provider.InitSpriteEditorDataProvider();
                var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
                if(capability==null||!capability.GetEditCapability().HasCapability(EEditCapability.EditPivot))
                    throw new InvalidOperationException("Importer cannot safely edit pivot: "+path);
                var rects=provider.GetSpriteRects();
                if(rects.Length!=1)throw new InvalidOperationException("Expected one full-canvas sprite: "+path);
                if(rects[0].alignment!=SpriteAlignment.Center||rects[0].pivot!=new Vector2(.5f,.5f))
                {
                    rects[0].alignment=SpriteAlignment.Center;rects[0].pivot=new Vector2(.5f,.5f);
                    provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
                }
                sprites[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(!sprites[i])throw new InvalidOperationException("Sprite import failed: "+path);
            }
            if(game.learningSprites==null||!game.learningSprites.SequenceEqual(sprites))
            {
                Undo.RecordObject(game,"Install Diagnose / Synthesize assets");
                game.learningSprites=sprites;EditorUtility.SetDirty(game);
            }
        }
    }
}
