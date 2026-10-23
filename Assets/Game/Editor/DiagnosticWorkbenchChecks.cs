using System;
using System.Linq;
using UnityEngine;

namespace VeinVanguard.Editor
{
    public static class DiagnosticWorkbenchChecks
    {
        public static string VerifyAssets()
        {
            var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            if(!game||game.learningSprites==null||game.learningSprites.Length!=27||game.learningSprites.Any(s=>!s))
                throw new Exception("The scene must reference all 27 learning PNGs as imported sprites.");
            string[] required={"plasma_weapon","formula_added","formula_water","formula_whole","formula_unsaturated","formula_steam","formula_avocado"};
            if(required.Any(name=>!game.learningSprites.Any(sprite=>sprite.name==name)))
                throw new Exception("The scene must reference the plasma weapon and all six universal formula icons.");
            foreach(var sprite in game.learningSprites)
            {
                if(Vector2.Distance(sprite.pivot,new Vector2(sprite.rect.width*.5f,sprite.rect.height*.5f))>.01f)
                    throw new Exception("Learning sprite pivot must remain at canvas center: "+sprite.name);
                var path=UnityEditor.AssetDatabase.GetAssetPath(sprite);
                var importer=UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if(importer==null||!importer.alphaIsTransparency||importer.mipmapEnabled||importer.npotScale!=UnityEditor.TextureImporterNPOTScale.None||importer.textureCompression!=UnityEditor.TextureImporterCompression.Uncompressed)
                    throw new Exception("Incorrect learning sprite import settings: "+sprite.name);
                importer.GetSourceTextureWidthAndHeight(out int width,out int height);
                bool formula=sprite.name.StartsWith("formula_",StringComparison.Ordinal);
                var filtering=formula?FilterMode.Point:FilterMode.Bilinear;
                if(importer.maxTextureSize!=(formula?128:2048)||sprite.texture.width!=(formula?128:width)||sprite.texture.height!=(formula?128:height)||
                    width<=0||height<=0||Mathf.Abs(sprite.texture.width/(float)sprite.texture.height-width/(float)height)>.001f)
                    throw new Exception("Formula icons must import at square 128 pixels; remaining art must retain source dimensions and every source aspect ratio: "+sprite.name);
                if(sprite.rect!=new Rect(0,0,sprite.texture.width,sprite.texture.height)||sprite.vertices.Length!=4||importer.filterMode!=filtering||sprite.texture.filterMode!=filtering||sprite.texture.wrapMode!=TextureWrapMode.Clamp)
                    throw new Exception("Learning sprites must retain full canvas, point-filtered formula icons, and bilinear remaining art: "+sprite.name);
            }
            return "PASS: 27 full-canvas sprites, six point-filtered 128px formula icons, source aspect ratios, bilinear remaining art at source size, and uncompressed alpha";
        }

        public static string VerifyEntry() => PlasmaWorkbenchChecks.VerifyEntry();
        public static string VerifyActionSeparation() => PlasmaWorkbenchChecks.VerifyActionSeparation();
        public static string VerifyInteractions() => PlasmaWorkbenchChecks.VerifyInteractions();
        public static string VerifyVisibleHitTargets() => PlasmaWorkbenchChecks.VerifyVisibleHitTargets();
    }
}
