using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MCAnimationImporter
{
    [MenuItem("Tools/Vein Vanguard/Prepare MC Animations")]
    public static void Prepare()
    {
        string root = null;
        foreach (string guid in AssetDatabase.FindAssets("MCAnimationImporter t:MonoScript"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("/Editor/MCAnimationImporter.cs", StringComparison.Ordinal))
                root = path.Substring(0, path.Length - "/Editor/MCAnimationImporter.cs".Length);
        }
        if (root == null) throw new InvalidOperationException("Import the entire Unity_MC folder under Assets first.");
        string[] names = { "idle", "diagnose", "synthesize", "nutrient_attack", "trivia_wait", "restore", "homeostasis_activate", "homeostasis_hit", "hurt", "low_energy", "shutdown", "victory" };
        foreach (string name in names)
            for (int i = 0; i < 4; i++)
                if (!File.Exists(root + "/Frames/" + name + "_" + i.ToString("D2") + ".png"))
                    throw new FileNotFoundException("Missing frame for " + name);
        if (!AssetDatabase.IsValidFolder(root + "/Animations")) AssetDatabase.CreateFolder(root, "Animations");
        foreach (string name in names)
        {
            var keys = new ObjectReferenceKeyframe[5];
            for (int i = 0; i < 4; i++)
            {
                string path = root + "/Frames/" + name + "_" + i.ToString("D2") + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 512;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 1f / 12f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) throw new InvalidOperationException("Sprite import failed: " + path);
                keys[i] = new ObjectReferenceKeyframe { time = i / 8f, value = sprite };
            }
            bool loop = name == "idle" || name == "trivia_wait" || name == "low_energy";
            keys[4] = new ObjectReferenceKeyframe { time = 4f / 8f, value = loop ? keys[0].value : keys[3].value };
            string clipPath = root + "/Animations/" + name + ".anim";
            // Preserve clips already edited by the user.
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null) continue;
            var clip = new AnimationClip { name = name, frameRate = 8 };
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            AssetDatabase.CreateAsset(clip, clipPath);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("MC ready: 48 sprites and 12 clips. Add the clips to an Animator on the same object as SpriteRenderer.");
    }
}
