using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VeinVanguard.Editor
{
    public static class PrototypeBuilder
    {
        [MenuItem("Tools/Vein Vanguard/Build Playable Prototype")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            const string scenePath="Assets/Scenes/VeinVanguard.unity";
            if(System.IO.File.Exists(scenePath))
            {
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=scenePath)
                {
                    if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                    EditorSceneManager.OpenScene(scenePath);
                }
                var existing=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
                if(!existing)throw new InvalidOperationException("The prototype scene has no BattleManager.");
                FinishScene();return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            camera.tag="MainCamera"; camera.transform.position=new Vector3(0,0,-10);
            camera.GetComponent<Camera>().orthographic=true;
            camera.GetComponent<Camera>().backgroundColor=new Color(.03f,.09f,.13f);
            var game=new GameObject("Vein Vanguard").AddComponent<BattleManager>();
            ConfigureAssets(game);
            string[] poses={"idle","diagnose","synthesize","nutrient_attack","restore","trivia_wait","homeostasis_activate","homeostasis_hit","hurt","low_energy","victory","shutdown"};
            game.playerFrames=poses.SelectMany(p=>Enumerable.Range(0,4).Select(i=>SpriteAt($"Assets/Unity_MC/Frames/{p}_{i:00}.png"))).ToArray();
            string[] enemies={"idle","attack","hurt","defeat"};
            game.enemyOneFrames=enemies.SelectMany(p=>Enumerable.Range(0,4).Select(i=>SpriteAt($"Assets/Unity_Enemy1/frames/{p}_{i:00}.png"))).ToArray();
            game.enemyTwoFrames=enemies.SelectMany(p=>Enumerable.Range(0,4).Select(i=>SpriteAt($"Assets/Unity_Enemy2/frames/{p}_{i:00}.png"))).ToArray();
            FinishScene();
        }
        static void ConfigureAssets(BattleManager game)
        {
            game.background=SpriteAt("Assets/battle_background.png");
            game.titleScreen=SpriteAt("Assets/title_screen.png");
            game.worldMap=SpriteAt("Assets/map_1.png");
            string[] hudNames={"status_panel","button_normal","button_selected","button_pressed","button_disabled","bar_track","hp_fill","energy_fill","icon_diagnose","icon_synthesize","icon_restore","icon_homeostasis"};
            game.hudTextures=new Texture2D[hudNames.Length];game.hudCrops=new Rect[hudNames.Length];
            game.hudSprites=new Sprite[hudNames.Length];
            const string generatedFolder="Assets/Game/UI/HUD";
            if(!AssetDatabase.IsValidFolder("Assets/Game/UI"))AssetDatabase.CreateFolder("Assets/Game","UI");
            if(!AssetDatabase.IsValidFolder(generatedFolder))AssetDatabase.CreateFolder("Assets/Game/UI","HUD");
            for(int i=0;i<hudNames.Length;i++)
            {
                var path=$"Assets/Unity_HUD/Sprites/{hudNames[i]}.png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(!importer)throw new InvalidOperationException("Missing HUD sprite: "+path);
                bool changed=importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Single||!importer.alphaIsTransparency||importer.mipmapEnabled||importer.npotScale!=TextureImporterNPOTScale.None||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.wrapMode!=TextureWrapMode.Clamp;
                if(changed){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();}
                game.hudTextures[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                game.hudCrops[i]=VisibleBounds(game.hudTextures[i]);
                string spritePath=$"{generatedFolder}/{hudNames[i]}.asset";
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if(!sprite)
                {
                    Rect crop=game.hudCrops[i];Vector4 border=i<=5?new Vector4(crop.width*.18f,crop.height*.3f,crop.width*.18f,crop.height*.3f):Vector4.zero;
                    sprite=Sprite.Create(game.hudTextures[i],crop,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
                    sprite.name=hudNames[i];AssetDatabase.CreateAsset(sprite,spritePath);
                }
                game.hudSprites[i]=sprite;
            }
            game.mainMusic=AudioAt("Soundtrack main.mp3");game.mapMusic=AudioAt("Soundtrack map.mp3");game.battleMusic=AudioAt("Soundtrack batlle.mp3");
            game.clickSound=AudioAt("Click.mp3");game.attackSound=AudioAt("Attack Mc.mp3");game.enemyAttackSound=AudioAt("Attack Villain.mp3");
            game.chargeSound=AudioAt("Charging Mc.mp3");game.defendSound=AudioAt("Defend Mc.mp3");game.enemyDefendSound=AudioAt("Deffend villain.mp3");
            game.victorySound=AudioAt("Victory.mp3");game.loseSound=AudioAt("Lose.mp3");
            game.font=BuildUIFont();
        }
        static TMP_FontAsset BuildUIFont()
        {
            const string path="Assets/font/Rajdhani UI SDF.asset";
            var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if(existing)return existing;
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/font/Rajdhani-SemiBold.ttf");
            if(!source)throw new InvalidOperationException("Rajdhani source font missing.");
            var asset=TMP_FontAsset.CreateFontAsset(source,80,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic);
            asset.name="Rajdhani UI SDF";
            string chars=new string(Enumerable.Range(32,95).Select(i=>(char)i).ToArray())+"×•↑↓←→–—…";
            asset.TryAddCharacters(chars,out string missing);
            asset.atlasPopulationMode=AtlasPopulationMode.Static;
            var fallback=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            asset.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{fallback};
            AssetDatabase.CreateAsset(asset,path);
            foreach(var atlas in asset.atlasTextures)if(atlas)AssetDatabase.AddObjectToAsset(atlas,asset);
            AssetDatabase.AddObjectToAsset(asset.material,asset);EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
            return asset;
        }
        public static void FinishScene()
        {
            var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            if(!game) throw new InvalidOperationException("No prototype in active scene.");
            const string scenePath="Assets/Scenes/VeinVanguard.unity";
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(!game.font)game.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(!game.font)throw new InvalidOperationException("Prototype SDF font missing.");
            ConfigureAssets(game);
            game.BakeHierarchyForEditor();
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.runInBackground=true;
            EditorSceneManager.SaveScene(scene,scenePath);
            var existing=EditorBuildSettings.scenes.Where(s=>s.path!=scenePath).ToList();
            existing.Insert(0,new EditorBuildSettingsScene(scenePath,true)); EditorBuildSettings.scenes=existing.ToArray();
            AssetDatabase.SaveAssets(); Selection.activeGameObject=game.gameObject;
            Debug.Log("Vein Vanguard prototype ready. Press Play.");
        }
        static Rect VisibleBounds(Texture2D texture)
        {
            var target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;
            Texture2D readable=null;
            try
            {
                Graphics.Blit(texture,target);RenderTexture.active=target;
                readable=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
                readable.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);readable.Apply();
                var pixels=readable.GetPixels32();int w=texture.width,h=texture.height;
                int minX=w,minY=h,maxX=0,maxY=0;
                for(int y=0;y<h;y++){int count=0;for(int x=0;x<w;x++)if(pixels[y*w+x].a>128)count++;if(count>w*.04f){minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}}
                for(int x=0;x<w;x++){int count=0;for(int y=0;y<h;y++)if(pixels[y*w+x].a>128)count++;if(count>h*.04f){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);}}
                if(minX>maxX||minY>maxY)return new Rect(0,0,w,h);
                minX=Math.Max(0,minX-2);minY=Math.Max(0,minY-2);maxX=Math.Min(w-1,maxX+2);maxY=Math.Min(h-1,maxY+2);
                return new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(readable)UnityEngine.Object.DestroyImmediate(readable);}
        }
        [MenuItem("Tools/Vein Vanguard/Build Playable Prototype",true)]
        static bool ValidateBuild()=>!EditorApplication.isPlaying;
        static Sprite SpriteAt(string path)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer) throw new InvalidOperationException("Missing sprite: "+path);
            bool changed=importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Single||!importer.alphaIsTransparency||importer.mipmapEnabled||importer.maxTextureSize<2048;
            if(changed){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.SaveAndReimport();}
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static AudioClip AudioAt(string name)
        {
            string path="Assets/Sound/"+name;var importer=AssetImporter.GetAtPath(path) as AudioImporter;
            if(importer)
            {
                var current=importer.defaultSampleSettings;var settings=current;settings.loadType=name.Contains("Soundtrack")?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=name.Contains("main")?.65f:.8f;
                if(current.loadType!=settings.loadType||current.compressionFormat!=settings.compressionFormat||!Mathf.Approximately(current.quality,settings.quality)){importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        public static string VerifyRules()
        {
            var m=new BattleModel();m.Begin(0);
            if(m.Attack(Nutrient.Omega3)!=-1)throw new Exception("Diagnosis gate");
            m.Diagnose();if(m.Attack(Nutrient.Omega3)!=40||m.energy!=40)throw new Exception("Weakness");
            if(m.Attack(Nutrient.Omega3)!=-1)throw new Exception("Turn gate");
            m.EnemyAttack();if(m.hp!=86)throw new Exception("Enemy damage");
            m.Restore();m.Answer(true);if(m.energy!=60||m.shield!=10)throw new Exception("Recharge");
            if(m.Answer(true))throw new Exception("Duplicate answer");
            m.phase=BattlePhase.Enemy;m.EnemyAttack();if(m.hp!=73)throw new Exception("Shield");
            if(BattleModel.Multiplier(Nutrient.Water,Nutrient.Fiber)!=1||BattleModel.Multiplier(Nutrient.Omega3,Nutrient.Fiber)!=.1f)throw new Exception("Multiplier");
            m.Begin(1);if(m.diagnosed||m.enemyHp!=130)throw new Exception("Encounter reset");
            return "PASS: diagnosis, damage, turn lock, recharge, duplicate answers, mitigation, multipliers, encounter reset";
        }
    }
}
