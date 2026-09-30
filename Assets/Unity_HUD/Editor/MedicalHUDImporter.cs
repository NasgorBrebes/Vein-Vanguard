using UnityEditor;
using UnityEngine;

public static class MedicalHUDImporter
{
    [MenuItem("Tools/Vein Vanguard/Import Medical HUD Sprites")]
    public static void Import()
    {
        string root = null;
        foreach (var id in AssetDatabase.FindAssets("MedicalHUDImporter t:MonoScript"))
        {
            var path = AssetDatabase.GUIDToAssetPath(id);
            const string suffix = "/Editor/MedicalHUDImporter.cs";
            if (path.EndsWith(suffix)) root = path.Substring(0, path.Length - suffix.Length);
        }
        if (root == null) { Debug.LogError("Copy Unity_HUD into Assets first."); return; }
        foreach (var id in AssetDatabase.FindAssets("t:Texture2D", new[] { root + "/Sprites" }))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(id));
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
        }
        Debug.Log("Medical HUD sprites imported. Assemble as Canvas UI Images with separate text and gameplay values.");
    }
}
