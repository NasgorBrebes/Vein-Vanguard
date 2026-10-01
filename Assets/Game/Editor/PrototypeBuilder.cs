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
            game.joystickDisc=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            foreach(var name in new[]{"MC_Walk_Master","IdleFront"})
            {
                var importer=AssetImporter.GetAtPath($"Assets/Resources/VeinVanguard/{name}.png") as TextureImporter;
                if(!importer)throw new InvalidOperationException("Missing exploration sheet: "+name);
                bool changed=importer.textureType!=TextureImporterType.Default||importer.npotScale!=TextureImporterNPOTScale.None||importer.mipmapEnabled||!importer.alphaIsTransparency||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.maxTextureSize!=2048||importer.wrapMode!=TextureWrapMode.Clamp;
                if(changed){importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();}
            }
            string[] effects={"diagnose","nutrient_projectile","impact","restore","shield_activate","shield_hit"};
            game.vfxFrames=effects.SelectMany(p=>Enumerable.Range(0,4).Select(i=>SpriteAt($"Assets/Game/VFX/frames/{p}_{i:00}.png"))).ToArray();
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
            if(path.StartsWith("Assets/Game/VFX/"))
            {
                bool vfxChanged=importer.npotScale!=TextureImporterNPOTScale.None||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.wrapMode!=TextureWrapMode.Clamp||importer.spritePivot!=new Vector2(.5f,.5f);
                if(vfxChanged){importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spritePivot=new Vector2(.5f,.5f);importer.SetTextureSettings(settings);importer.SaveAndReimport();}
            }
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
            m.Restore();m.Answer(true);if(m.energy!=60||m.shield!=0)throw new Exception("Recharge without passive shield");
            if(m.Answer(true))throw new Exception("Duplicate answer");
            m.phase=BattlePhase.Enemy;m.EnemyAttack();if(m.hp!=72)throw new Exception("Unprotected damage");
            if(!m.Homeostasis()||m.energy!=50||m.shieldHits!=2||m.phase!=BattlePhase.Enemy)throw new Exception("Active homeostasis");
            if(m.Homeostasis())throw new Exception("Duplicate homeostasis");
            if(m.EnemyAttack()!=7||m.shieldHits!=1||m.shield!=50)throw new Exception("First shield hit");
            if(m.Homeostasis())throw new Exception("Shield stacking");
            m.Attack(Nutrient.Water);if(m.EnemyAttack()!=7||m.shieldHits!=0||m.shield!=0)throw new Exception("Shield expiration");
            m.energy=9;if(m.Homeostasis())throw new Exception("Homeostasis energy gate");
            m.energy=60;if(!m.Homeostasis())throw new Exception("Shield reactivation");
            if(BattleModel.Multiplier(Nutrient.Water,Nutrient.Fiber)!=1||BattleModel.Multiplier(Nutrient.Omega3,Nutrient.Fiber)!=.1f)throw new Exception("Multiplier");
            m.Begin(1);if(m.diagnosed||m.enemyHp!=130||m.shield!=0||m.shieldHits!=0||m.Homeostasis())throw new Exception("Encounter reset and diagnosis gate");
            return "PASS: diagnosis, damage, turn lock, recharge, duplicate answers, mitigation, multipliers, encounter reset";
        }
        public static string VerifyExploration()
        {
            if(!Application.isPlaying)throw new Exception("Run this check in Play Mode.");
            var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            Vector2[] directions={Vector2.down,Vector2.right,Vector2.left,Vector2.up,new Vector2(1,-1),new Vector2(-1,-1),Vector2.one,new Vector2(-1,1)};
            for(int row=0;row<directions.Length;row++)
            {
                game.StartGame();game.MoveWorld(directions[row]*10);
                typeof(BattleManager).GetMethod("PositionWorld",flags).Invoke(game,null);
                if((int)typeof(BattleManager).GetField("walkRow",flags).GetValue(game)!=row)throw new Exception("Wrong walking direction: "+row);
                var image=(UnityEngine.UI.Image)typeof(BattleManager).GetField("worldPlayerImage",flags).GetValue(game);
                bool mirrored=row==2||row==5||row==7;
                if(image.rectTransform.localScale.x!=(mirrored?-1:1))throw new Exception("Left directions must mirror the right master poses");
                if(image.sprite.texture!=Resources.Load<Texture2D>("VeinVanguard/MC_Walk_Master"))throw new Exception("Walking must use the measured master atlas");
                if(Mathf.Abs(image.rectTransform.sizeDelta.y-110)>.001f||Mathf.Abs(image.rectTransform.pivot.x-image.sprite.pivot.x/image.sprite.rect.width)>.001f)throw new Exception("Master poses must use fixed height and registered pivot");
            }
            game.StartGame();
            var frames=(Sprite[])typeof(BattleManager).GetField("walkFrames",flags).GetValue(game);
            var art=(UnityEngine.UI.Image)typeof(BattleManager).GetField("worldPlayerImage",flags).GetValue(game);
            if(art.rectTransform.localScale!=Vector3.one)throw new Exception("Idle must reset horizontal flip");
            if(frames==null||frames.Length!=72||Array.IndexOf(frames,art.sprite)<64)throw new Exception("Expected eight walk frames per direction and eight front idle frames");
            if(frames[0].texture.width!=1586||frames[0].texture.height!=992||frames[0].rect!=new Rect(76,580,140,192)||frames[8].rect!=new Rect(92,778,118,199))throw new Exception("Master crops must match the measured PNG, without import resizing");
            if(art.rectTransform.sizeDelta!=new Vector2(130,150)||art.rectTransform.pivot!=new Vector2(.5f,0))throw new Exception("Idle must restore its original size and pivot");
            game.MoveWorld(new Vector2(-1,-1)*10);
            for(int frame=0;frame<8;frame++)
            {
                typeof(BattleManager).GetField("walkTime",flags).SetValue(game,(frame+.1f)/12f);
                typeof(BattleManager).GetMethod("PositionWorld",flags).Invoke(game,null);
                if(Array.IndexOf(frames,art.sprite)!=40+frame)throw new Exception("Southwest must play all eight frames in order");
            }
            game.StartGame();
            var type=typeof(BattleManager).Assembly.GetType("VeinVanguard.ExplorationJoystick");
            if(type==null)throw new Exception("Analog joystick missing");
            var joystick=game.GetComponentInChildren(type,true) as MonoBehaviour;
            var rect=joystick.GetComponent<RectTransform>();
            var knob=(RectTransform)type.GetField("knob").GetValue(joystick);
            var baseImage=rect.GetComponent<UnityEngine.UI.Image>();
            if(baseImage.sprite!=game.hudSprites[1]||baseImage.type!=UnityEngine.UI.Image.Type.Sliced||baseImage.color!=Color.white||!baseImage.raycastTarget)throw new Exception("Analog base must match the white/cyan HUD frame and remain interactive");
            if(rect.sizeDelta!=new Vector2(180,180)||rect.anchoredPosition!=new Vector2(40,40)||knob.sizeDelta!=new Vector2(66,66))throw new Exception("Analog reskin must preserve touch geometry");
            if(knob.GetComponent<UnityEngine.UI.Image>().sprite!=game.hudSprites[1]||!knob.Find("Cross Horizontal")||!knob.Find("Cross Vertical"))throw new Exception("Analog knob must use the HUD frame and retro cyan cross");
            foreach(var decoration in rect.GetComponentsInChildren<UnityEngine.UI.Image>(true))if(decoration!=baseImage&&decoration.raycastTarget)throw new Exception("Analog decoration must not intercept touches");
            Canvas.ForceUpdateCanvases();
            var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=17};
            data.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center+Vector2.one*rect.rect.width));
            ((UnityEngine.EventSystems.IPointerDownHandler)joystick).OnPointerDown(data);
            var value=(Vector2)type.GetProperty("Value").GetValue(joystick);
            if(value.x<=0||value.y<=0||Mathf.Abs(value.magnitude-1)>.001f)throw new Exception("Joystick diagonal clamp");
            var before=game.WorldPosition;
            typeof(BattleManager).GetMethod("UpdateFlow",flags).Invoke(game,null);
            if(game.WorldPosition.x<=before.x||game.WorldPosition.y<=before.y||Array.IndexOf(frames,art.sprite)<48||Array.IndexOf(frames,art.sprite)>55)throw new Exception("Analog movement must render northeast walk");
            ((UnityEngine.EventSystems.IPointerUpHandler)joystick).OnPointerUp(data);
            if((Vector2)type.GetProperty("Value").GetValue(joystick)!=Vector2.zero)throw new Exception("Joystick release must stop movement");
            before=game.WorldPosition;
            typeof(BattleManager).GetMethod("UpdateFlow",flags).Invoke(game,null);
            if(game.WorldPosition!=before||Array.IndexOf(frames,art.sprite)<64)throw new Exception("Release must show stationary front idle");
            data.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            ((UnityEngine.EventSystems.IPointerDownHandler)joystick).OnPointerDown(data);
            if((Vector2)type.GetProperty("Value").GetValue(joystick)!=Vector2.zero)throw new Exception("Joystick center deadzone");
            ((UnityEngine.EventSystems.IPointerUpHandler)joystick).OnPointerUp(data);
            data.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center+Vector2.right*28));
            ((UnityEngine.EventSystems.IPointerDownHandler)joystick).OnPointerDown(data);
            value=(Vector2)type.GetProperty("Value").GetValue(joystick);
            if(value.x<=0||value.x>=.9f||Mathf.Abs(value.y)>.001f)throw new Exception("Joystick must preserve partial speed");
            var second=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=18,position=data.position};
            ((UnityEngine.EventSystems.IPointerUpHandler)joystick).OnPointerUp(second);
            if((Vector2)type.GetProperty("Value").GetValue(joystick)!=value)throw new Exception("Unrelated touch must not release joystick");
            ((UnityEngine.EventSystems.IPointerUpHandler)joystick).OnPointerUp(data);
            return "PASS: eight directions, 72 frames, front idle, analog diagonal/clamp/release/deadzone";
        }
    }
}
