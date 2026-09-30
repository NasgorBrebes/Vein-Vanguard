using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Enemy2AnimationImporter
{
    [MenuItem("Tools/Vein Vanguard/Prepare Enemy 2 Animations")]
    public static void Prepare()
    {
        string root = null;
        foreach (string guid in AssetDatabase.FindAssets("Enemy2AnimationImporter t:MonoScript"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("/Editor/Enemy2AnimationImporter.cs", StringComparison.Ordinal))
                root = path.Substring(0, path.Length - "/Editor/Enemy2AnimationImporter.cs".Length);
        }
        if (root == null) throw new InvalidOperationException("Import the entire Unity_Enemy2 folder under Assets first.");
        string[] names = { "idle", "attack", "hurt", "defeat" };
        foreach (string name in names)
            for (int i = 0; i < 4; i++)
                if (!File.Exists(root + "/frames/" + name + "_" + i.ToString("D2") + ".png"))
                    throw new FileNotFoundException("Missing frame for " + name);
        if (!AssetDatabase.IsValidFolder(root + "/Animations")) AssetDatabase.CreateFolder(root, "Animations");
        foreach (string name in names)
        {
            float fps = name == "idle" ? 6f : name == "defeat" ? 7f : 10f;
            var keys = new ObjectReferenceKeyframe[5];
            for (int i = 0; i < 4; i++)
            {
                string path = root + "/frames/" + name + "_" + i.ToString("D2") + ".png";
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
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprite };
            }
            bool loop = name == "idle";
            keys[4] = new ObjectReferenceKeyframe { time = 4f / fps, value = loop ? keys[0].value : keys[3].value };
            string clipPath = root + "/Animations/" + name + ".anim";
            // Preserve clips already edited by the user.
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null) continue;
            var clip = new AnimationClip { name = name, frameRate = fps };
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            AssetDatabase.CreateAsset(clip, clipPath);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Enemy 2 ready: 16 sprites and 4 clips. Add the clips to an Animator on the same object as SpriteRenderer.");
    }
}
