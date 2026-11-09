using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace VeinVanguard.Editor
{
    [InitializeOnLoad]
    public static class UiRefactorChecks
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        const string Evidence="docs/superpowers/verification/ui-ux-refactor";
        const string Results="Temp/UiRefactorResults.txt";
        static readonly Stack<IEnumerator> steps=new Stack<IEnumerator>();
        static int lastFrame;
        static float originalMusic,originalSfx;
        static int dynamicTask;
        const string DynamicResults="Temp/DynamicBattleResults.txt";
        static UiRefactorChecks()
        {
            EditorApplication.playModeStateChanged+=state=>{
                if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("VV.Dynamic.QA",false))
                {
                    SessionState.SetBool("VV.Dynamic.QA",false);
                    dynamicTask=SessionState.GetInt("VV.Dynamic.Task",2);
                    steps.Clear();steps.Push(VerifyRadialRuntime());lastFrame=-1;
                    EditorApplication.update+=DynamicTick;
                }
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("VV.Arena.QA", false))
                {
                    SessionState.SetBool("VV.Arena.QA", false);
                    EditorApplication.delayCall += () =>
                    {
                        const string path = "Temp/AnimatedArenaResults.txt";
                        try { File.WriteAllText(path, VerifyAnimatedArena()); }
                        catch (Exception error) { File.WriteAllText(path, error.ToString()); }
                        EditorApplication.ExitPlaymode();
                    };
                }
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("VV.FinalLearning.QA", false))
                {
                    SessionState.SetBool("VV.FinalLearning.QA", false);
                    EditorApplication.delayCall += () =>
                    {
                        string result;
                        try
                        {
                            int through = SessionState.GetInt("VV.FinalLearning.ThroughTask", 4);
                            result = UniversalFormulaRuleChecks.VerifyExpansionProgression() + "\n";
                            if (through >= 4) result += VerifyFinalLearningPresentation() + "\n";
                            result += "STAGE EXPANSION ACCEPTANCE COMPLETE task=" + through + "\n";
                        }
                        catch (Exception error) { result = "FAIL: " + error + "\n"; }
                        File.AppendAllText("Temp/StageExpansionResults.txt", result);
                        File.AppendAllText("docs/superpowers/verification/two-stage-existing-gameplay/StageExpansionResults.txt", result);
                        EditorApplication.ExitPlaymode();
                    };
                }
                if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("VV.UI.QA",false))
                {
                    SessionState.SetBool("VV.UI.QA",false);
                    steps.Clear();steps.Push(RunPlayChecks());lastFrame=-1;
                    EditorApplication.update+=Tick;
                }
            };
        }
        static T Field<T>(BattleManager game, string name) => (T)typeof(BattleManager).GetField(name, Private).GetValue(game);

        public static void RunDynamicAcceptance() => RunDynamicAcceptance(4);
        public static void RunDynamicAcceptance(int throughTask=4)
        {
            const string path="Temp/DynamicBattleResults.txt";
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
            const string scenePath = "Assets/Scenes/VeinVanguard.unity";
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath);
            }
            Directory.CreateDirectory("Temp");
            File.WriteAllText(path,"Dynamic acceptance task="+throughTask+"\n");
            try
            {
                Check(throughTask>=1&&throughTask<=8,"Invalid task number");
                File.AppendAllText(path,VerifyDynamicConfiguration()+"\n");
                if(throughTask>=2)File.AppendAllText(path,VerifyRadialAndFocus()+"\n");
                if(throughTask>=3)File.AppendAllText(path,VerifyStageFraming()+"\n");
                if(throughTask>=4)Check(typeof(BattleManager).GetMethod("LayoutSynthesisDrawer",Private)!=null,"Adaptive synthesis layout missing");
                Check(throughTask<=4,"Requested task has not been implemented yet.");
                if(throughTask>=2)
                {
                    SessionState.SetInt("VV.Dynamic.Task",throughTask);
                    SessionState.SetBool("VV.Dynamic.QA",true);
                    SelectResolution(1600,900,true);
                    EditorApplication.isPlaying=true;return;
                }
                File.AppendAllText(path,"DYNAMIC ACCEPTANCE COMPLETE task="+throughTask+"\n");
            }
            catch(Exception error){File.AppendAllText(path,"FAIL: "+error+"\n");}
        }

        [MenuItem("Tools/Vein Vanguard/Stage Expansion/Run Stage Expansion Acceptance")]
        public static void RunStageExpansionAcceptance() => RunStageExpansionAcceptance(5);

        public static void RunStageExpansionAcceptance(int throughTask = 5)
        {
            const string path = "Temp/StageExpansionResults.txt";
            const string persistentDir = "docs/superpowers/verification/two-stage-existing-gameplay";
            const string persistentPath = persistentDir + "/StageExpansionResults.txt";
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            const string scenePath = "Assets/Scenes/VeinVanguard.unity";
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath);
            }
            Directory.CreateDirectory("Temp");
            Directory.CreateDirectory(persistentDir);
            string header = "Stage expansion acceptance task=" + throughTask + "\n";
            File.WriteAllText(path, header);
            File.WriteAllText(persistentPath, header);
            Debug.Log(header.TrimEnd());
            try
            {
                Check(throughTask >= 1 && throughTask <= 5, "Invalid task number");
                string cat = UniversalFormulaRuleChecks.VerifyExpansionCatalog() + "\n";
                File.AppendAllText(path, cat); File.AppendAllText(persistentPath, cat);
                Debug.Log(cat.TrimEnd());
                string learn = UniversalFormulaRuleChecks.VerifyReusedLearning() + "\n";
                File.AppendAllText(path, learn); File.AppendAllText(persistentPath, learn);
                Debug.Log(learn.TrimEnd());

                if (throughTask >= 2)
                {
                    string art = VerifyExpansionArtwork() + "\n";
                    File.AppendAllText(path, art); File.AppendAllText(persistentPath, art);
                    Debug.Log(art.TrimEnd());
                }

                if (throughTask >= 3)
                {
                    string trav = UniversalFormulaRuleChecks.VerifyExpansionTraversal() + "\n";
                    File.AppendAllText(path, trav); File.AppendAllText(persistentPath, trav);
                    Debug.Log(trav.TrimEnd());
                }

                if (throughTask >= 5)
                {
                    string fullSession = UniversalFormulaRuleChecks.VerifyExpansionFullSession() + "\n";
                    File.AppendAllText(path, fullSession); File.AppendAllText(persistentPath, fullSession);
                    Debug.Log(fullSession.TrimEnd());
                }

                if (throughTask >= 3)
                {
                    SessionState.SetInt("VV.FinalLearning.ThroughTask", throughTask);
                    SessionState.SetBool("VV.FinalLearning.QA", true);
                    EditorApplication.EnterPlaymode();
                    return;
                }

                string complete = "STAGE EXPANSION ACCEPTANCE COMPLETE task=" + throughTask + "\n";
                File.AppendAllText(path, complete); File.AppendAllText(persistentPath, complete);
                Debug.Log(complete.TrimEnd());
            }
            catch (Exception error)
            {
                string fail = "FAIL: " + error + "\n";
                File.AppendAllText(path, fail); File.AppendAllText(persistentPath, fail);
                Debug.LogError(fail.TrimEnd());
                Debug.LogException(error);
                throw;
            }
        }

        public static string VerifyFinalLearningPresentation()
        {
            Check(Application.isPlaying, "Run final learning presentation in Play Mode.");
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            for (int encounter = 0; encounter < EncounterCatalog.Count; encounter++)
            {
                game.StartGame();
                if (encounter >= 4)
                {
                    Array.Fill(game.defeated, true, 0, 4);
                    Check(game.TryAdvanceStage(), "Could not enter Stage 2.");
                    if (encounter == 5) game.defeated[4] = true;
                }
                game.BeginEncounter(encounter);
                game.Diagnose();
                game.StopAllCoroutines();
                typeof(BattleManager).GetField("actionAnimating", Private).SetValue(game, false);
                game.model.DiagnoseAll();
                Invoke(game, "RefreshLearningWorkbench");
                var sourceText = Field<TMPro.TextMeshProUGUI[]>(game, "formulaSourceLabels");
                for (int target = 0; target < 2; target++)
                    Check(sourceText[target].text == CombatSamples.Get(encounter, target).record, "Wrong Diagnose record.");
                Invoke(game, "CloseLearningWorkbench");
                game.Synthesize();
                var palette = UniversalFormula.Palette(encounter);
                var buttons = Field<UnityEngine.UI.Button[]>(game, "formulaPalette");
                var slots = Field<FormulaIcon[]>(game, "formulaSlots");
                for (int i = 0; i < 6; i++)
                {
                    var icon = palette[i];
                    Check(buttons[i].GetComponent<FormulaIconDrag>().icon == icon, "Palette drag uses a grid index as card ID.");
                    Check(buttons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>().text == UniversalFormula.Title(icon), "Card title mismatch.");
                    buttons[i].onClick.Invoke();
                    Check(Field<FormulaIcon>(game, "selectedFormulaIcon") == icon, "Card click selected a stale card.");
                    Check(Field<TMPro.TextMeshProUGUI>(game, "formulaInfoBody").text == game.FormulaInfoText(icon), "Card fact mismatch.");
                    game.InstallFormulaIcon(icon, 0);
                    game.InstallFormulaIcon(icon, 1);
                    Check(slots[0] == icon && slots[1] == icon, "Duplicate cards could not fill both ports.");
                    if (icon >= FormulaIcon.Nicotine)
                    {
                        var images = Field<UnityEngine.UI.Image[]>(game, "formulaSocketIcons");
                        foreach (var image in images)
                            Check(image.transform.Find("Data Badge").GetComponent<TMPro.TextMeshProUGUI>().text == UniversalFormula.Badge(icon),
                                "Port is missing its data badge.");
                    }
                    Check(game.BeginFormulaDrag(icon, new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)),
                        "Active palette card could not begin a drag.");
                    game.EndFormulaDrag(null);
                }
                var foreign = encounter == 2 ? FormulaIcon.Ethanol : encounter == 5 ? FormulaIcon.Benzene : FormulaIcon.Nicotine;
                var previous = slots[0];
                game.InstallFormulaIcon(foreign, 0);
                Check(slots[0] == previous, "Foreign card entered a port.");
                var oldNotice = Field<RectTransform>(game, "safe").Find("Header/Temporary Learning Notice");
                Check(!oldNotice || !oldNotice.gameObject.activeInHierarchy, "Temporary banner is still visible.");
                Check(game.model.energy == 60, "Assembly charged MP before impact.");
            }
            game.ShowMenu();
            return "PASS: six final encounters, palette IDs, card facts, duplicate ports, drag, foreign-card rejection and banner removal.";
        }

        public static string VerifyExpansionArtwork()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            Check(game != null, "BattleManager missing in active scene");

            var addTexField = typeof(BattleManager).GetField("additionalEnemyTextures");
            Check(addTexField != null, "additionalEnemyTextures field missing");
            var addTex = (Texture2D[])addTexField.GetValue(game);
            Check(addTex != null && addTex.Length == 4, "additionalEnemyTextures length must be 4");
            Check(addTex[0] != null && addTex[0].name.Contains("enemy_3"), "enemy_3 binding missing");
            Check(addTex[1] != null && addTex[1].name.Contains("enemy_4"), "enemy_4 binding missing");
            Check(addTex[2] != null && addTex[2].name.Contains("enemy_5"), "enemy_5 binding missing");
            Check(addTex[3] != null && addTex[3].name.Contains("boss"), "boss binding missing");

            var secondMapField = typeof(BattleManager).GetField("secondWorldMap");
            Check(secondMapField != null, "secondWorldMap field missing");
            var secondMap = (Sprite)secondMapField.GetValue(game);
            Check(secondMap != null && (Mathf.RoundToInt(secondMap.rect.width) == 4093 || Mathf.RoundToInt(secondMap.rect.width) == 2048) && secondMap.name.StartsWith("map_3"), "secondWorldMap fullrect sprite missing");

            var secondBgField = typeof(BattleManager).GetField("secondBattleBackground");
            Check(secondBgField != null, "secondBattleBackground field missing");
            var secondBg = (Sprite)secondBgField.GetValue(game);
            Check(secondBg != null && (Mathf.RoundToInt(secondBg.rect.width) == 4093 || Mathf.RoundToInt(secondBg.rect.width) == 2048) && secondBg.name.StartsWith("battle_background_2"), "secondBattleBackground fullrect sprite missing");

            var encounterSpriteMethod = typeof(BattleManager).GetMethod("EncounterSprite", Private);
            Check(encounterSpriteMethod != null, "EncounterSprite method missing");
            var bottleSprite = (Sprite)encounterSpriteMethod.Invoke(game, new object[] { 4 });
            Check(bottleSprite != null, "Bottle sprite missing");
            Check(Mathf.RoundToInt(bottleSprite.rect.width) == addTex[2].width && Mathf.RoundToInt(bottleSprite.rect.height) == addTex[2].height, "Bottle sprite must cover full texture");

            var worldEnemiesField = typeof(BattleManager).GetField("worldEnemies", Private);
            Check(worldEnemiesField != null, "worldEnemies missing");
            var worldEnemies = (RectTransform[])worldEnemiesField.GetValue(game);
            Check(worldEnemies != null && worldEnemies.Length >= 4, "worldEnemies must have at least 4 nodes");
            for (int i = 0; i < 4; i++)
            {
                Check(worldEnemies[i] != null, $"worldEnemies[{i}] missing");
                Check(worldEnemies[i].name == "Enemy " + (i + 1), $"worldEnemies[{i}] name mismatch: {worldEnemies[i].name}");
            }

            var exitField = typeof(BattleManager).GetField("stageExit", Private) ?? typeof(BattleManager).GetField("stageExit");
            Check(exitField != null, "stageExit field missing");
            var exitObj = exitField.GetValue(game);
            Check(exitObj != null, "stageExit instance missing");

            int initialCount = game.GetComponentsInChildren<Transform>(true).Length;
            var ensureNodesMethod = typeof(BattleManager).GetMethod("EnsureStageMapNodes", Private);
            Check(ensureNodesMethod != null, "EnsureStageMapNodes method missing");
            ensureNodesMethod.Invoke(game, null);
            ensureNodesMethod.Invoke(game, null);
            Check(game.GetComponentsInChildren<Transform>(true).Length == initialCount, "Repeated EnsureStageMapNodes must be idempotent");

            return "PASS: expansion artwork bindings and map nodes";
        }
        public static string VerifyDynamicConfiguration()
        {
            var field=typeof(BattleManager).GetField("presentation");
            Check(field!=null,"BattleManager.presentation is missing");
            var type=field.FieldType;
            var config=Activator.CreateInstance(type);
            Check(Math.Abs(Convert.ToDouble(type.GetField("targetMs").GetValue(config))-800)<.00001, "Configuration targetMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("normalPerfectMs").GetValue(config))-70)<.00001, "Configuration normalPerfectMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("normalGoodMs").GetValue(config))-160)<.00001, "Configuration normalGoodMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("widePerfectMs").GetValue(config))-120)<.00001, "Configuration widePerfectMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("wideGoodMs").GetValue(config))-240)<.00001, "Configuration wideGoodMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("initialRadius").GetValue(config))-120)<.00001, "Configuration initialRadius changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("targetRadius").GetValue(config))-42)<.00001, "Configuration targetRadius changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("minimumRadius").GetValue(config))-12)<.00001, "Configuration minimumRadius changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("releaseWaitMs").GetValue(config))-500)<.00001, "Configuration releaseWaitMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("timingLabelMs").GetValue(config))-180)<.00001, "Configuration timingLabelMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("anticipationMs").GetValue(config))-150)<.00001, "Configuration anticipationMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("flightMs").GetValue(config))-500)<.00001, "Configuration flightMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("impactHoldMs").GetValue(config))-80)<.00001, "Configuration impactHoldMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("revealMs").GetValue(config))-350)<.00001, "Configuration revealMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("idleTransitionMs").GetValue(config))-280)<.00001, "Configuration idleTransitionMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("drawerTransitionMs").GetValue(config))-220)<.00001, "Configuration drawerTransitionMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("attackTransitionMs").GetValue(config))-180)<.00001, "Configuration attackTransitionMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("projectileTransitionMs").GetValue(config))-240)<.00001, "Configuration projectileTransitionMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("parryTransitionMs").GetValue(config))-220)<.00001, "Configuration parryTransitionMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("resultTransitionMs").GetValue(config))-260)<.00001, "Configuration resultTransitionMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("radialX").GetValue(config))-170)<.00001, "Configuration radialX changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("radialY").GetValue(config))-110)<.00001, "Configuration radialY changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("radialWidth").GetValue(config))-190)<.00001, "Configuration radialWidth changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("radialHeight").GetValue(config))-120)<.00001, "Configuration radialHeight changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("popMs").GetValue(config))-180)<.00001, "Configuration popMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("staggerMs").GetValue(config))-35)<.00001, "Configuration staggerMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("fadeMs").GetValue(config))-100)<.00001, "Configuration fadeMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("focusScale").GetValue(config))-1.05)<.00001, "Configuration focusScale changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("idleZoom").GetValue(config))-1.06)<.00001, "Configuration idleZoom changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("drawerZoom").GetValue(config))-1)<.00001, "Configuration drawerZoom changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("hitZoom").GetValue(config))-1.16)<.00001, "Configuration hitZoom changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("parryZoom").GetValue(config))-1.06)<.00001, "Configuration parryZoom changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("panX").GetValue(config))-0.08)<.00001, "Configuration panX changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("panY").GetValue(config))-0.025)<.00001, "Configuration panY changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("farFactor").GetValue(config))-0.2)<.00001, "Configuration farFactor changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("midFactor").GetValue(config))-0.4)<.00001, "Configuration midFactor changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("nearFactor").GetValue(config))-0.6)<.00001, "Configuration nearFactor changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("overscan").GetValue(config))-0.18)<.00001, "Configuration overscan changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("midAlpha").GetValue(config))-0.1)<.00001, "Configuration midAlpha changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("nearAlpha").GetValue(config))-0.06)<.00001, "Configuration nearAlpha changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("idleAmplitude").GetValue(config))-0.015)<.00001, "Configuration idleAmplitude changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("idlePeriod").GetValue(config))-1.6)<.00001, "Configuration idlePeriod changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("shakePixels").GetValue(config))-3)<.00001, "Configuration shakePixels changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("shakeMs").GetValue(config))-120)<.00001, "Configuration shakeMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("popupCount").GetValue(config))-8)<.00001, "Configuration popupCount changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("popupMs").GetValue(config))-700)<.00001, "Configuration popupMs changed");
            Check(Math.Abs(Convert.ToDouble(type.GetField("popupRise").GetValue(config))-42)<.00001, "Configuration popupRise changed");
            var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            Check(game,"Scene requires BattleManager");
            var old=field.GetValue(game);
            try
            {
                field.SetValue(game,config);
                var factsField=typeof(BattleManager).GetField("formulaInfoTexts");
                Check(factsField!=null,"Six owner-approved formula text slots are missing");
                var original=(string[])factsField.GetValue(game);
                string[] expected={
                "Gula, lemak, dan bahan lain yang ditambahkan saat mengolah atau menyajikan makanan ikut menambah energi pada sajian. Daftar komposisi dan label gizi menunjukkan apa saja yang ditambahkan. WHO menganjurkan gula bebas kurang dari 10% dari asupan energi harian.",
                "Air putih tidak mengandung energi, gula, maupun lemak. Sebagai minuman, air putih tidak menambah gula bebas ke dalam asupan harian.",
                "Buah utuh masih membawa seratnya. Gula yang menyatu dalam struktur buah utuh tidak dihitung sebagai gula bebas menurut definisi WHO.",
                "Lemak tak jenuh banyak terdapat pada ikan, kacang-kacangan, alpukat, serta minyak seperti minyak zaitun, kanola, dan bunga matahari. WHO menganjurkan lemak jenuh kurang dari 10% dari asupan energi harian, dengan pergeseran konsumsi lemak ke arah lemak tak jenuh.",
                "Mengukus dan merebus memasak makanan dengan uap atau air panas, tanpa minyak masak. Cara memasak menentukan apakah minyak ikut ditambahkan ke dalam sajian.",
                "Variasi olesan berarti mengganti jenis bahan yang dioleskan pada roti. Alpukat, misalnya, mengandung serat, dan lemaknya sebagian besar tak jenuh."
                };
                Check(original.SequenceEqual(expected),"Owner formula text changed");
                var info=typeof(BattleManager).GetMethod("FormulaInfoText",Private|BindingFlags.Public);
                Check(info!=null,"FormulaInfoText missing");
                try
                {
                    factsField.SetValue(game,new[]{""," ",null,"\t","\r\n",""});
                    for(int i=0;i<6;i++)
                        Check((string)info.Invoke(game,new object[]{(FormulaIcon)i})=="Fakta formula belum diisi.","Empty/whitespace fallback changed");
                }
                finally {factsField.SetValue(game,original);}
                for(int i=0;i<6;i++)
                    Check((string)info.Invoke(game,new object[]{(FormulaIcon)i})==expected[i],"Formula info text mismatch");
                type.GetField("flightMs").SetValue(config,-1f);
                type.GetField("normalGoodMs").SetValue(config,1f);
                Invoke(game,"ValidatePresentationSettings");
                Check((float)type.GetField("flightMs").GetValue(config)>0,"Negative duration accepted");
                Check((float)type.GetField("normalGoodMs").GetValue(config)>=(float)type.GetField("normalPerfectMs").GetValue(config),"Inverted timing window accepted");
            }
            finally {field.SetValue(game,old);}
            return "PASS: D01 configuration, six exact owner texts, empty/whitespace fallback and invalid settings";
        }

        public static string VerifyRadialAndFocus()
        {
            Check(Mathf.Approximately(BattleManager.MinimumTouchCanvasUnits(2.5f,1),120),"Density conversion incorrect");
            Check(Mathf.Approximately(BattleManager.MinimumTouchCanvasUnits(0,1),120),"Invalid density fallback incorrect");
            var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            Check(typeof(BattleManager).GetMethod("EnsureRadialActions",Private)!=null,"Radial setup is missing");
            game.BakeHierarchyForEditor();
            var buttons=new[]{Field<Button>(game,"diagnose"),Field<Button>(game,"synthesize"),Field<Button>(game,"restore"),Field<Button>(game,"homeostasis")};
            Check(buttons.All(b=>b.transform.parent.name=="Radial Actions"),"Actions still use static bar");
            Check(Field<TMPro.TextMeshProUGUI>(game,"log").transform.parent.parent==Field<RectTransform>(game,"safe"),"Narration must be independent from radial");
            Canvas.ForceUpdateCanvases();
            for(int i=0;i<buttons.Length;i++)
            {
                var rect=(RectTransform)buttons[i].transform;
                Check(rect.rect.width>=189.9f&&rect.rect.height>=119.9f,"Radial touch target shrank");
                Check(buttons[i].navigation.mode==Navigation.Mode.Explicit,"Radial direction focus must be explicit");
            }
            return "PASS: D02 radial hierarchy, independent narration, touch bounds and explicit navigation";
        }

        static void VerifyTouchBounds(BattleManager game,float density)
        {
            var buttons=new[]{Field<Button>(game,"diagnose"),Field<Button>(game,"synthesize"),Field<Button>(game,"restore"),Field<Button>(game,"homeostasis")};
            var bounds=new Rect[4];
            for(int i=0;i<4;i++)
            {
                var rect=(RectTransform)buttons[i].transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);
                bounds[i]=Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
                Check(bounds[i].width/density>=47.9f&&bounds[i].height/density>=47.9f,"Target less than48dp");
                for(int j=0;j<i;j++)Check(!bounds[j].Overlaps(bounds[i]),"Radial hit targets overlap");
                Check(bounds[i].xMin>=0&&bounds[i].xMax<=Screen.width&&bounds[i].yMin>=0&&bounds[i].yMax<=Screen.height,"Radial escaped screen");
                if(buttons[i].interactable)
                {
                    var hits=new List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=bounds[i].center},hits);
                    Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==buttons[i],"Radial target obstructed");
                }
            }
        }

        public static string VerifyStageFraming()
        {
            var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            Check(typeof(BattleManager).GetMethod("EnsureBattleStage",Private)!=null,"Battle stage setup missing");
            Invoke(game,"EnsureBattleStage");
            var stage=Field<RectTransform>(game,"stageRoot");
            Check(stage&&Field<Image>(game,"playerArt").transform.IsChildOf(stage),"Actor not in stage");
            Check(!Field<RectTransform>(game,"safe").IsChildOf(stage),"HUD moves with stage");
            Check(stage.parent.GetComponent<RectMask2D>(),"Stage viewport must mask edges");
            int count=game.GetComponentsInChildren<Transform>(true).Length;Invoke(game,"EnsureBattleStage");
            Check(count==game.GetComponentsInChildren<Transform>(true).Length,"Stage setup duplicated nodes");
            return "PASS: D03 stage hierarchy, fixed HUD, viewport mask and idempotent setup";
        }

        static void DynamicTick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=DynamicTick;steps.Clear();return;}
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            try
            {
                while(steps.Count>0)
                {
                    var current=steps.Peek();
                    if(!current.MoveNext()){steps.Pop();continue;}
                    if(current.Current is IEnumerator nested){steps.Push(nested);continue;}
                    return;
                }
                File.AppendAllText(DynamicResults,"DYNAMIC ACCEPTANCE COMPLETE task="+dynamicTask+"\n");
                FinishDynamicRun();
            }
            catch(Exception error){File.AppendAllText(DynamicResults,"FAIL: "+error+"\n");FinishDynamicRun();}
        }
        static void FinishDynamicRun()
        {
            var viewType=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var view=EditorWindow.GetWindow(viewType);
            viewType.GetProperty("selectedSizeIndex",Private|BindingFlags.Public).SetValue(view,SessionState.GetInt("VV.UI.PreviousSize",0));
            EditorApplication.update-=DynamicTick;steps.Clear();EditorApplication.isPlaying=false;
        }
        static IEnumerator VerifyRadialRuntime()
        {
            yield return Wait(.5f);
            var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            var sizes=new[]{new Vector2Int(1600,900),new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(2000,900)};
            var densities=new[]{2.5f,3f,2f,2.5f};
            Directory.CreateDirectory("docs/superpowers/verification/dynamic-battle");
            for(int n=0;n<sizes.Length;n++)
            {
                SelectResolution(sizes[n].x,sizes[n].y);yield return Wait(.3f);
                game.StartGame();game.BeginEncounter(0);yield return Wait(.5f);
                typeof(BattleManager).GetField("touchDensity",Private).SetValue(game,densities[n]);Invoke(game,"RefreshRadialActions");
                var buttons=new[]{Field<Button>(game,"diagnose"),Field<Button>(game,"synthesize"),Field<Button>(game,"restore"),Field<Button>(game,"homeostasis")};
                Check(buttons[0].interactable&&!buttons[1].interactable&&!buttons[2].interactable&&!buttons[3].interactable,"Initial action gates changed");
                var canvas=buttons[0].GetComponentInParent<Canvas>();
                VerifyTouchBounds(game,densities[n]);
                foreach(var b in buttons)
                {
                    var rect=(RectTransform)b.transform;
                    Check(rect.rect.width*canvas.scaleFactor/densities[n]>=47.9f&&rect.rect.height*canvas.scaleFactor/densities[n]>=47.9f,"Target smaller than48dp");
                    var label=b.GetComponentInChildren<TMPro.TextMeshProUGUI>();label.ForceMeshUpdate();
                    string name=label.text.Split('\n')[0];
                    Check(label.textInfo.lineCount>0&&label.textInfo.lineInfo[0].lastCharacterIndex>=name.Length-1,"Radial action name broken: "+name);
                }
                ScreenCapture.CaptureScreenshot("docs/superpowers/verification/dynamic-battle/radial-before-scan-"+sizes[n].x+"x"+sizes[n].y+".png");yield return Wait(.2f);
                Click(buttons[0]);yield return Wait(.5f);
                Check(game.model.diagnosed,"Radial Diagnose pointer failed");
                Invoke(game,"CloseLearningWorkbench");yield return Wait(.5f);
                game.model.energy=19;Invoke(game,"Refresh");Check(!buttons[1].interactable&&buttons[2].interactable&&buttons[3].interactable,"LowMP availability changed");
                game.model.shieldHits=1;Invoke(game,"Refresh");Check(!buttons[3].interactable,"Shield stacking unlocked");
                game.model.shieldHits=0;game.model.energy=60;Invoke(game,"Refresh");
                Check(buttons[1].transform.Find("Scan Complete").gameObject.activeSelf,"Scan availability badge missing");
                ScreenCapture.CaptureScreenshot("docs/superpowers/verification/dynamic-battle/radial-after-scan-"+sizes[n].x+"x"+sizes[n].y+".png");yield return Wait(.2f);
                File.AppendAllText(DynamicResults,"PASS: D02 live radial pointer/gates/48dp "+sizes[n]+" density="+densities[n]+"\n");
                if(dynamicTask>=3)yield return VerifyStageRuntime(game,sizes[n]);
                if(dynamicTask>=4)yield return VerifyAssemblyPresentation(game,sizes[n]);
            }
            if(dynamicTask>=4)
            {
                SelectResolution(960,540);yield return Wait(.3f);
                game.StartGame();game.BeginEncounter(0);game.model.DiagnoseAll();Invoke(game,"Refresh");
                typeof(BattleManager).GetField("touchDensity",Private).SetValue(game,2f);
                game.Synthesize();yield return Wait(.4f);
                Check(Field<ScrollRect>(game,"assemblyScroll").horizontal&&Field<ScrollRect>(game,"paletteScroll").vertical,"Narrow fallback did not scroll");
                File.AppendAllText(DynamicResults,VerifyCompactAssembly()+" stress480x270dp\n");
                game.SelectFormulaIcon(FormulaIcon.Water);
                var fire=(RectTransform)Field<Button>(game,"learningFire").transform;
                game.RevealAssemblyControl(fire);
                Check(RectTransformUtility.RectangleContainsScreenPoint(Field<RectTransform>(game,"assemblyViewport"),RectTransformUtility.WorldToScreenPoint(null,fire.TransformPoint(fire.rect.center))),"Keyboard focus did not reveal Fire");
                Check(Field<FormulaIcon>(game,"selectedFormulaIcon")==FormulaIcon.Water,"Scrolling lost selected formula");
                ScreenCapture.CaptureScreenshot("docs/superpowers/verification/dynamic-battle/synthesis-narrow-480x270dp.png");yield return Wait(.1f);
                Invoke(game,"CloseLearningWorkbench");
                SelectResolution(1600,900);yield return Wait(.3f);
                typeof(BattleManager).GetField("touchDensity",Private).SetValue(game,2.5f);
                File.AppendAllText(DynamicResults,PlasmaWorkbenchChecks.VerifyInteractions()+"\n");
            }
            VerifyKeyboardPipeline(game);
            yield return VerifyFreshHierarchy(game);
            File.AppendAllText(DynamicResults,"PASS: D02 keyboard pipeline and fresh/baked parity\n");
            if(dynamicTask>=3)File.AppendAllText(DynamicResults,PrototypeBuilder.VerifyAttackImpact()+"\n");
        }

        static void AssertArenaCameraCoupling(BattleManager game)
        {
            var stage = Field<RectTransform>(game, "stageRoot");
            var viewport = Field<RectTransform>(game, "stageViewport");
            var background = Field<Image>(game, "farBackground");
            float aspect = background.sprite.rect.width / background.sprite.rect.height;
            float framingAspect = Mathf.Max(aspect, 16f / 9f);
            float baseWidth = Mathf.Max(viewport.rect.width, viewport.rect.height * framingAspect) * (1 + game.presentation.overscan);
            var expectedPosition = stage.anchoredPosition;
            expectedPosition.y += (Field<Image>(game, "playerArt").rectTransform.anchorMin.y - .24f) * viewport.rect.height * stage.localScale.y;
            var artwork = background.rectTransform.rect;
            Check(Mathf.Abs(artwork.width / artwork.height - aspect) < .001f, "Arena artwork was stretched.");
            Check(artwork.width + .1f >= baseWidth * stage.localScale.x, "Arena floor must follow the actors' stage zoom.");
            if (game.StageIndex == 1) expectedPosition.y += (artwork.height - artwork.width / framingAspect) * .18f;
            Check(Vector2.Distance(background.rectTransform.anchoredPosition, expectedPosition) < .1f,
                "Arena floor pan must follow the actors' stage pan.");
            foreach (string field in new[] { "playerContactShadow", "enemyContactShadow" })
            {
                var shadow = Field<Image>(game, field);
                Check(shadow && shadow.sprite && !shadow.raycastTarget && shadow.rectTransform.rect.height > 0 &&
                    shadow.rectTransform.pivot == Vector2.one * .5f,
                    "Actors require visible, centered, nonblocking contact shadows.");
            }
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, background.rectTransform);
            Check(bounds.min.x <= viewport.rect.xMin && bounds.max.x >= viewport.rect.xMax &&
                bounds.min.y <= viewport.rect.yMin && bounds.max.y >= viewport.rect.yMax,
                "Arena background exposed an edge.");
        }

        public static string VerifyArenaCameraCoupling()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run camera checks in Play Mode.");
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            game.StartGame(); game.BeginEncounter(0);
            var stage = Field<RectTransform>(game, "stageRoot");
            var background = Field<Image>(game, "farBackground");
            var oldSprite = background.sprite;
            var oldPosition = stage.anchoredPosition;
            var oldScale = stage.localScale;
            try
            {
                foreach (var sprite in new[] { game.background, game.secondBattleBackground })
                {
                    background.sprite = sprite;
                    foreach (float zoom in new[] { 1f, 1.06f, 1.16f })
                        foreach (float direction in new[] { -1f, 0f, 1f })
                        {
                            var size = Field<RectTransform>(game, "stageViewport").rect.size;
                            stage.localScale = Vector3.one * zoom;
                            stage.anchoredPosition = new Vector2(direction * .08f * size.x, direction * .025f * size.y);
                            Invoke(game, "UpdateStageLayers");
                            AssertArenaCameraCoupling(game);
                        }
                }
            }
            finally
            {
                background.sprite = oldSprite; stage.localScale = oldScale; stage.anchoredPosition = oldPosition;
                Invoke(game, "UpdateStageLayers");
            }
            return "PASS: both arena floors follow stage zoom/pan and cover the viewport at all configured limits.";
        }

        public static string VerifyAnimatedArena()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run arena animation checks in Play Mode.");
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            const string preference = "VV.Motion.Reduced";
            bool savedPreference = PlayerPrefs.HasKey(preference);
            int previous = PlayerPrefs.GetInt(preference);
            try
            {
                PlayerPrefs.SetInt(preference, 0);
                game.StartGame();
                game.BeginEncounter(0);
                var viewport = Field<RectTransform>(game, "stageViewport");
                var art = Field<Image>(game, "farBackground");
                Check(art.sprite == game.background, "Stage 1 must render the original sprite at its imported resolution.");
                var material = art.material;
                Check(material && material.HasProperty("_ArenaMotion"), "Arena needs the original-art motion material.");
                var cells = viewport.Find("Mid Background")?.GetComponent<Image>();
                var plasma = viewport.Find("Near Plasma")?.GetComponent<Image>();
                Check(cells && plasma && !art.raycastTarget && !cells.raycastTarget && !plasma.raycastTarget,
                    "Arena layers must leave pointer input to the HUD.");
                Check(viewport.Cast<Transform>().All(node => !node.gameObject.activeSelf ||
                    (!node.name.StartsWith("Stage Silhouette ") && node.name != "Far Background" && node.name != "Arena Shade")),
                    "Legacy arena overlays are visible.");
                var first = material.GetVector("_ArenaMotion");
                float now = Time.unscaledTime;
                Invoke(game, "TickArenaBackground", now + .26f);
                Check(art.sprite == game.background && material.GetVector("_ArenaMotion") != first,
                    "The arena must animate while retaining its original texture.");
                float normalBrightness = plasma.color.a;
                typeof(BattleManager).GetField("arenaShotActive", Private).SetValue(game, true);
                Invoke(game, "TickArenaBackground", now + .51f);
                Check(plasma.color.a > normalBrightness, "Plasma did not brighten during a shot.");
                typeof(BattleManager).GetField("arenaShotActive", Private).SetValue(game, false);
                Invoke(game, "TickArenaBackground", now + 1.01f);
                Check(Mathf.Abs(plasma.color.a - normalBrightness) < .01f, "Plasma did not return to its idle tint.");
                PlayerPrefs.SetInt(preference, 1);
                Invoke(game, "TickArenaBackground", now + 1.25f);
                var stage = Field<RectTransform>(game, "stageRoot");
                var plasmaPosition = plasma.rectTransform.anchoredPosition;
                Invoke(game, "TickArenaBackground", now + 1.75f);
                Check(art.sprite == game.background && material.GetVector("_ArenaMotion") == first &&
                    plasma.rectTransform.anchoredPosition == plasmaPosition &&
                    stage.localScale == Vector3.one && stage.anchoredPosition == Vector2.zero,
                    "Reduced motion must freeze the artwork, particles and camera.");
                game.Diagnose();
                AssertArenaCameraCoupling(game);
                game.StartGame();
                Array.Fill(game.defeated, true, 0, 4);
                Check(game.TryAdvanceStage(), "The fixture could not enter Stage 2.");
                game.BeginEncounter(4);
                Check(art.sprite == game.secondBattleBackground, "Stage 2 must render the original sprite at its imported resolution.");
                Check(Field<RectTransform>(game, "playerMotion").anchoredPosition.y <= -viewport.rect.height * .05f &&
                    Field<RectTransform>(game, "enemyMotion").anchoredPosition.y <= -viewport.rect.height * .05f,
                    "Stage 2 actors must stand on the foreground ground plane.");
                game.Diagnose();
                Invoke(game, "UpdateStageLayers");
                Check(Mathf.Abs(Field<RectTransform>(game, "playerMotion").anchoredPosition.y + viewport.rect.height * .015f) < .1f,
                    "Stage 2 drawer must keep the actors on its lifted ground plane.");
                var panelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, Field<RectTransform>(game, "modal"));
                foreach (string contact in new[] { "playerContactShadow", "enemyContactShadow" })
                {
                    var shadowBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, Field<Image>(game, contact).rectTransform);
                    Check(shadowBounds.min.y > panelBounds.max.y, "Stage 2 feet must remain above the drawer.");
                }


                AssertArenaCameraCoupling(game);
                stage.localScale = Vector3.one * game.presentation.hitZoom;
                stage.anchoredPosition = new Vector2(-game.presentation.panX * viewport.rect.width,
                    game.presentation.panY * viewport.rect.height);
                Invoke(game, "UpdateStageLayers");
                AssertArenaCameraCoupling(game);
            }
            finally
            {
                if (savedPreference) PlayerPrefs.SetInt(preference, previous);
                else PlayerPrefs.DeleteKey(preference);
                game.ShowMenu();
            }
            return "PASS: original artwork, aspect and floor coverage, projectile tint, and reduced motion.";
        }

        static IEnumerator VerifyStageRuntime(BattleManager game,Vector2Int size)
        {
            game.model.energy=20;game.Restore();yield return Wait(.3f);
            Check(Field<RectTransform>(game,"modal").parent==Field<RectTransform>(game,"safe"),"Quiz dialog is hidden by radial container");
            game.StartGame();game.BeginEncounter(0);game.model.DiagnoseAll();Invoke(game,"Refresh");yield return Wait(.4f);
            var stage=Field<RectTransform>(game,"stageRoot");var viewport=Field<RectTransform>(game,"stageViewport");
            var background=Field<Image>(game,"farBackground").rectTransform;
            var header=Field<RectTransform>(game,"safe").Find("Header");var headerPosition=header.position;
            var framing=typeof(BattleManager).GetNestedType("BattleFraming",BindingFlags.NonPublic);
            foreach(string name in new[]{"Idle","Drawer","Projectile","ParryQte","Result"})
            {
                var routine=(IEnumerator)typeof(BattleManager).GetMethod("TransitionBattleFraming",Private).Invoke(game,new[]{Enum.Parse(framing,name)});
                int frames=0;
                while(routine.MoveNext())
                {
                    Check(++frames<600,"Framing did not finish");
                    AssertArenaCameraCoupling(game);
                    Check(stage.localScale.x<=1.1601f&&stage.localScale.x>=.9999f,"Zoom escaped clamp");
                    Check(Vector3.Distance(header.position,headerPosition)<.01f,"HUD moved with stage");
                    Check(background.rect.width>=viewport.rect.width&&background.rect.height>=viewport.rect.height,"Background does not cover viewport");
                    Check(Mathf.Abs(background.anchoredPosition.x)<=Mathf.Max(0,(background.rect.width-viewport.rect.width)*.5f)+.1f,"Background horizontal edge exposed");
                    Check(Mathf.Abs(background.anchoredPosition.y)<=Mathf.Max(0,(background.rect.height-viewport.rect.height)*.5f)+.1f,"Background vertical edge exposed");
                    yield return null;
                }
                ScreenCapture.CaptureScreenshot("docs/superpowers/verification/dynamic-battle/camera-"+name+"-"+size.x+"x"+size.y+".png");yield return Wait(.1f);
            }
            bool had=PlayerPrefs.HasKey("VV.Motion.Reduced");int old=PlayerPrefs.GetInt("VV.Motion.Reduced");
            try
            {
                PlayerPrefs.SetInt("VV.Motion.Reduced",1);
                yield return (IEnumerator)typeof(BattleManager).GetMethod("TransitionBattleFraming",Private).Invoke(game,new[]{Enum.Parse(framing,"Projectile")});
                Check(stage.localScale==Vector3.one&&stage.anchoredPosition==Vector2.zero,"Reduced motion moved stage");
                ScreenCapture.CaptureScreenshot("docs/superpowers/verification/dynamic-battle/camera-reduced-"+size.x+"x"+size.y+".png");yield return Wait(.1f);
            }
            finally {if(had)PlayerPrefs.SetInt("VV.Motion.Reduced",old);else PlayerPrefs.DeleteKey("VV.Motion.Reduced");}
            File.AppendAllText(DynamicResults,"PASS: D03 all framing presets, cover/clamps, stable HUD and reduced motion "+size+"\n");
        }

        public static string VerifyCompactAssembly()
        {
            var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            var root=Field<RectTransform>(game,"learningRoot");Check(root.gameObject.activeInHierarchy,"Open synthesis first");
            var body=Field<TMPro.TextMeshProUGUI>(game,"formulaInfoBody");
            var original=game.formulaInfoTexts;
            try
            {
                game.formulaInfoTexts=new[]{""," ",null,"\t","\r\n",""};
                for(int i=0;i<6;i++){game.SelectFormulaIcon((FormulaIcon)i);Check(body.text=="Fakta formula belum diisi.","Compact info fallback changed");}
            }
            finally {game.formulaInfoTexts=original;}
            game.SelectFormulaIcon(FormulaIcon.VariedSpread);
            var sockets=Field<Button[]>(game,"formulaSockets");
            float density=Field<float>(game,"touchDensity"),scale=root.GetComponentInParent<Canvas>().scaleFactor;
            foreach(var button in sockets.Concat(Field<Button[]>(game,"formulaPalette")).Concat(new[]{Field<Button>(game,"learningClose"),Field<Button>(game,"learningFire")}))
            {
                var rect=(RectTransform)button.transform;
                Check(rect.rect.width*scale/density>=47.9f&&rect.rect.height*scale/density>=47.9f,"Assembly target smaller than48dp: "+button.name);
            }
            var corners=new Vector3[4];sockets[0].GetComponent<RectTransform>().GetWorldCorners(corners);float right=corners[2].x;
            sockets[1].GetComponent<RectTransform>().GetWorldCorners(corners);Check(right<=corners[0].x,"Socket hit targets overlap");
            Check(root.rect.height*scale<Screen.height*.36f,"Drawer too tall: canvasHeight="+root.rect.height+" scale="+scale+" screen="+Screen.height+" safe="+Screen.safeArea+" anchors="+Field<RectTransform>(game,"safe").anchorMax);
            if(Field<ScrollRect>(game,"assemblyScroll").horizontal)
            {
                var pointer=new PointerEventData(EventSystem.current){position=new Vector2(100,0),pressPosition=Vector2.zero};
                var scrolling=game.BeginAssemblyScroll(pointer,root);
                try {Check(scrolling&&game.AssemblyScrolling&&!game.BeginFormulaDrag(FormulaIcon.Water,pointer),"Second input stole active assembly scroll");}
                finally {game.EndAssemblyScroll();}
            }
            return "PASS: D04 compact assembly fallback, socket separation, all targets48dp and drawer<36%";
        }
        static IEnumerator VerifyAssemblyPresentation(BattleManager game,Vector2Int size)
        {
            game.Synthesize();yield return Wait(.4f);
            var body=Field<TMPro.TextMeshProUGUI>(game,"formulaInfoBody");
            for(int i=0;i<6;i++)
            {
                game.SelectFormulaIcon((FormulaIcon)i);
                Check(body.text==game.FormulaInfoText((FormulaIcon)i),"Owner fact missing from info panel");
            }
            File.AppendAllText(DynamicResults,VerifyCompactAssembly()+"\n");
            game.SelectFormulaIcon(FormulaIcon.UnsaturatedFat);yield return Wait(.1f);
            var info=Field<ScrollRect>(game,"infoScroll");
            var ownerText=game.formulaInfoTexts[(int)FormulaIcon.UnsaturatedFat];
            try
            {
                game.formulaInfoTexts[(int)FormulaIcon.UnsaturatedFat]=string.Concat(Enumerable.Repeat("Contoh teks untuk uji tata letak. ",30));
                game.SelectFormulaIcon(FormulaIcon.UnsaturatedFat);yield return Wait(.1f);
                Check(info.content.rect.height>info.viewport.rect.height,"Long fixture cannot scroll");
                var keyboard=Keyboard.current;
                if(keyboard!=null)
                {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.PageDown));InputSystem.Update();Invoke(game,"UpdateUiNavigation");
                    Check(info.verticalNormalizedPosition<.99f,"PageDown did not scroll formula facts");
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                }
                info.verticalNormalizedPosition=0;
            }
            finally {game.formulaInfoTexts[(int)FormulaIcon.UnsaturatedFat]=ownerText;game.SelectFormulaIcon(FormulaIcon.UnsaturatedFat);}
            ScreenCapture.CaptureScreenshot("docs/superpowers/verification/dynamic-battle/synthesis-info-long-"+size.x+"x"+size.y+".png");yield return Wait(.1f);
            game.InstallFormulaIcon(FormulaIcon.Water,1);game.InstallFormulaIcon(FormulaIcon.Water,0);yield return Wait(.7f);
            Check(Field<Button>(game,"learningFire").interactable,"Wrong/repeated pair did not charge");
            Check(Field<TMPro.TextMeshProUGUI[]>(game,"formulaSocketLabels").All(label=>!label.gameObject.activeSelf),"Filled socket still shows centered A/B");
            ScreenCapture.CaptureScreenshot("docs/superpowers/verification/dynamic-battle/synthesis-filled-"+size.x+"x"+size.y+".png");yield return Wait(.1f);
            Invoke(game,"CloseLearningWorkbench");game.Synthesize();yield return Wait(.1f);
            Check(Field<FormulaIcon[]>(game,"formulaSlots").All(icon=>icon==FormulaIcon.Water),"Reopen lost sockets");
            Check(!Field<Button>(game,"learningFire").interactable,"Reopen bypassed charge");yield return Wait(.7f);
            Invoke(game,"CloseLearningWorkbench");
        }
        static void Invoke(BattleManager game,string name,params object[] args)=>typeof(BattleManager).GetMethod(name,Private).Invoke(game,args);
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Record(string result){File.AppendAllText(Results,result+"\n");Debug.Log(result);}

        [MenuItem("Tools/Vein Vanguard/UI Refactor/Check Skin %#u")]
        public static void CheckSkinMenu()
        {
            try { File.WriteAllText("Temp/UiRefactorEdit.txt", VerifySkin()); }
            catch (Exception error) { File.WriteAllText("Temp/UiRefactorEdit.txt", "FAIL: " + error); Debug.LogException(error); }
        }

        public static string VerifySkin(BattleManager game=null)
        {
            if(!game)game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            Check(game, "Prototype scene must be open.");
            game.BakeHierarchyForEditor();
            var safe = Field<RectTransform>(game, "safe");
            foreach (var path in new[] { "Header/Nanobot Status", "Header/Pathogen Status", "Message Panel" })
            {
                var panel = safe.Find(path).GetComponent<Image>();
                Check(panel.color.r < .2f && panel.color.a >= .88f && panel.color.a <= .92f,
                    "Information panel must use translucent dark navy: " + path);
            }
            foreach (var name in new[] { "diagnose", "synthesize", "restore", "homeostasis" })
            {
                var button = Field<Button>(game, name);
                Check(button.image.color.r < .2f, "Battle action must use a dark surface: " + name);
                var label = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                Check(label.color.r > .95f && label.color.g > .95f, "Both lines must use bright text: " + name);
                var feedback=button.GetComponent<UiControlFeedback>();
                foreach(var surface in new[]{feedback.surface,Color.Lerp(feedback.surface,Color.white,.07f),new Color(.15f,.20f,.27f,.98f)})
                    Check(Contrast(label.color,surface)>=7,"Both action text lines need at least 7:1 contrast in every state: "+name);
            }
            foreach(var button in Field<RectTransform>(game,"titleRoot").GetComponentsInChildren<Button>(true))
                Check(((RectTransform)button.transform).sizeDelta.x<=280,"Title button too wide: "+button.name);
            return "PASS: information panels dark; title widths <=280; both action lines >=7:1 normal/hover/disabled";
        }
        static float Contrast(Color first,Color second)
        {
            float Light(Color color)
            {
                float Linear(float value)=>value<=.04045f?value/12.92f:Mathf.Pow((value+.055f)/1.055f,2.4f);
                return .2126f*Linear(color.r)+.7152f*Linear(color.g)+.0722f*Linear(color.b);
            }
            float a=Light(first),b=Light(second);return (Mathf.Max(a,b)+.05f)/(Mathf.Min(a,b)+.05f);
        }

        [MenuItem("Tools/Vein Vanguard/UI Refactor/Run Acceptance and Capture %#y")]
        public static void RunAcceptance()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before acceptance run.");
            const string scenePath = "Assets/Scenes/VeinVanguard.unity";
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath);
            }
            Directory.CreateDirectory("Temp");
            Directory.CreateDirectory(Evidence);File.WriteAllText(Results,"UI acceptance / "+DateTime.Now.ToString("s")+"\n");
            try
            {
                Record(PrototypeBuilder.VerifyRules());Record(VerifySkin());
                var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
                originalMusic=Field<AudioSource>(game,"musicSource").volume;originalSfx=Field<AudioSource>(game,"effectSource").volume;
                SessionState.SetFloat("VV.UI.Music",originalMusic);SessionState.SetFloat("VV.UI.Sfx",originalSfx);
                int count=game.GetComponentsInChildren<Transform>(true).Length;
                game.BakeHierarchyForEditor();
                Check(count==game.GetComponentsInChildren<Transform>(true).Length,"Bake twice must not duplicate UI nodes.");
                Record("PASS: repeated bake is idempotent");
                EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
                EditorSceneManager.SaveScene(game.gameObject.scene);
                SelectResolution(1600,900,true);
                SessionState.SetBool("VV.UI.QA",true);EditorApplication.isPlaying=true;
            }
            catch(Exception error){Record("FAIL: "+error);Debug.LogException(error);}
        }

        static void SelectResolution(int width,int height,bool remember=false)
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var viewType=assembly.GetType("UnityEditor.GameView");
            var view=EditorWindow.GetWindow(viewType);var selected=viewType.GetProperty("selectedSizeIndex",Private|BindingFlags.Public);
            if(remember)SessionState.SetInt("VV.UI.PreviousSize",(int)selected.GetValue(view));
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes=singleton.GetProperty("instance",BindingFlags.Static|BindingFlags.Public|BindingFlags.FlattenHierarchy).GetValue(null);
            var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});
            var groupClass=group.GetType();string label="UI QA "+width+"x"+height;
            var names=(string[])groupClass.GetMethod("GetDisplayTexts").Invoke(group,null);
            int index=Array.FindIndex(names,name=>name.Contains(label));
            if(index<0)
            {
                var sizeType=assembly.GetType("UnityEditor.GameViewSize");var modeType=assembly.GetType("UnityEditor.GameViewSizeType");
                var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,
                    new object[]{Enum.Parse(modeType,"FixedResolution"),width,height,label},null);
                groupClass.GetMethod("AddCustomSize").Invoke(group,new[]{size});
                index=(int)groupClass.GetMethod("GetBuiltinCount").Invoke(group,null)+(int)groupClass.GetMethod("GetCustomCount").Invoke(group,null)-1;
            }
            selected.SetValue(view,index);view.Focus();
        }

        static void Tick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=Tick;steps.Clear();return;}
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            try
            {
                while(steps.Count>0)
                {
                    var current=steps.Peek();
                    if(!current.MoveNext()){steps.Pop();continue;}
                    if(current.Current is IEnumerator nested){steps.Push(nested);continue;}
                    return;
                }
                EditorApplication.update-=Tick;Record("ACCEPTANCE COMPLETE");EditorApplication.isPlaying=false;
            }
            catch(Exception error)
            {
                EditorApplication.update-=Tick;steps.Clear();
                var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
                if(game){game.SetMusicVolume(SessionState.GetFloat("VV.UI.Music",.45f));game.SetSfxVolume(SessionState.GetFloat("VV.UI.Sfx",.75f));PlayerPrefs.Save();}
                Record("FAIL: "+error);Debug.LogException(error);EditorApplication.isPlaying=false;
            }
        }
        // Radial pop-in takes 285 ms before its CanvasGroup accepts input.
        static IEnumerator Wait(float seconds=.35f)
        {for(float elapsed=0;elapsed<seconds;elapsed+=Time.unscaledDeltaTime)yield return null;}
        static IEnumerator Capture(string name)
        {
            yield return Wait();
            var path=Evidence+"/"+name+".png";
            ScreenCapture.CaptureScreenshot(path);yield return Wait(.3f);
            Check(File.Exists(path),"Capture missing: "+path);Record("CAPTURE: "+path+" ("+Screen.width+"x"+Screen.height+")");
        }
        static void TextFits(Transform root)
        {
            Canvas.ForceUpdateCanvases();
            foreach(var label in root.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
            {
                label.ForceMeshUpdate();
                Check(!label.isTextOverflowing && label.textBounds.size.x<=label.rectTransform.rect.width+.5f && label.textBounds.size.y<=label.rectTransform.rect.height+.5f,
                    "Text must fit: "+label.name+" / "+label.text+" / "+label.rectTransform.rect.size+" vs "+label.textBounds.size);
            }
        }
        static void Click(Transform root,string name)
        {
            var button=root.GetComponentsInChildren<Button>().First(b=>b.name==name);
            Click(button);
        }
        static void Click(Button button)
        {
            Check(button.IsInteractable(),"Button is disabled: "+button.name);
            var rt=(RectTransform)button.transform;
            var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Pointer cannot reach "+button.name);
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        static IEnumerator RunPlayChecks()
        {
            yield return Wait();
            var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            Record(DiagnosticWorkbenchChecks.VerifyAssets());Record(PrototypeBuilder.VerifyClickAudio());
            yield return VerifyFreshHierarchy(game);
            yield return VerifyFeedbackRegression(game);
            VerifyKeyboardPipeline(game);
            var title=Field<RectTransform>(game,"titleRoot");TextFits(title);yield return Capture("title-1600x900");
            Click(title,"SETTINGS");yield return Wait();
            var panel=Field<RectTransform>(game,"overlayContent");
            Check(panel.rect.width<=460.1f,"Settings width must not exceed 460 canvas units.");TextFits(panel);
            var sliders=panel.GetComponentsInChildren<Slider>();float music=game.MusicVolume,sfx=game.SfxVolume;
            sliders[0].value=.31f;sliders[1].value=.52f;
            Check(Mathf.Abs(game.MusicVolume-.31f)<.001f&&Mathf.Abs(game.SfxVolume-.52f)<.001f,"Volume callbacks must change audio sources.");
            yield return Capture("settings-1600x900");Click(panel,"KEMBALI");
            game.OpenSettings();yield return Wait();panel=Field<RectTransform>(game,"overlayContent");sliders=panel.GetComponentsInChildren<Slider>();
            Check(Mathf.Abs(sliders[0].value-.31f)<.001f&&Mathf.Abs(sliders[1].value-.52f)<.001f,"Settings must reopen saved values.");
            sliders[0].value=music;sliders[1].value=sfx;Click(panel,"KEMBALI");
            Click(title,"CREDITS");yield return Wait();panel=Field<RectTransform>(game,"overlayContent");TextFits(panel);yield return Capture("credits-1600x900");Click(panel,"KEMBALI");
            Record("PASS: title settings/credits close; both sliders update and persist; original volume restored");
            Click(title,"START");yield return Wait();TextFits(Field<RectTransform>(game,"worldRoot"));yield return Capture("map-1600x900");
            Record(PrototypeBuilder.VerifyExploration());yield return Wait();
            var hint=Field<CanvasGroup>(game,"movementHint");game.StartGame();yield return Wait();Check(hint.alpha>.99f,"New session must show movement hint.");
            game.MoveWorld(Vector2.right*10);Invoke(game,"UpdateMovementHint");yield return Wait(.8f);
            Check(hint.alpha<.1f,"Successful movement must dismiss hint.");
            game.BeginEncounter(0);Invoke(game,"ReturnToWorld");yield return Wait();Check(hint.alpha<.1f,"Battle return must not reset hint.");
            game.StartGame();yield return Wait();Check(hint.alpha>.99f,"Session reset must restore hint.");
            Record("PASS: movement hint fades after movement, stays dismissed on battle return, resets for new session");
            game.BeginEncounter(0);yield return Wait();TextFits(Field<RectTransform>(game,"safe"));yield return Capture("battle-1600x900");
            Record(PlasmaWorkbenchChecks.VerifyEntry());yield return Wait(.5f);TextFits(Field<RectTransform>(game,"diagnosisRoot"));yield return Capture("diagnosis-1600x900");
            Record(PlasmaWorkbenchChecks.VerifyVisibleHitTargets());Record(PlasmaWorkbenchChecks.VerifyActionSeparation());
            yield return Wait();Record(PlasmaWorkbenchChecks.VerifyInteractions());yield return Wait();
            Record(PrototypeBuilder.VerifyAttackImpact());yield return Wait();Record(PrototypeBuilder.VerifyHomeostasisIcon());
            Record(PlasmaWorkbenchChecks.VerifyFeedbackLayout());yield return Wait();
            foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(2000,900)})
            {
                SelectResolution(size.x,size.y);yield return Wait(.3f);
                string suffix=size.x+"x"+size.y;
                game.ShowMenu();yield return Wait();TextFits(title);
                game.StartGame();yield return Wait();TextFits(Field<RectTransform>(game,"worldRoot"));
                game.BeginEncounter(0);yield return Wait();TextFits(Field<RectTransform>(game,"safe"));
                foreach(var name in new[]{"hpBar","mpBar","enemyBar"})
                    Check(((RectTransform)Field<Image>(game,name).transform.parent).rect.height>=16,"Battle bar must be at least 16 canvas units: "+name);
                game.Diagnose();yield return Wait(.5f);TextFits(Field<RectTransform>(game,"diagnosisRoot"));
                Record(PlasmaWorkbenchChecks.VerifyVisibleHitTargets());
                Invoke(game,"CloseLearningWorkbench");game.Synthesize();yield return Wait();
                TextFits(Field<RectTransform>(game,"learningRoot"));Record(PlasmaWorkbenchChecks.VerifyVisibleHitTargets());yield return Capture("synthesis-empty-"+suffix);
                // The approved compact module drawer uses an 8-unit reactor track.
                Check(((RectTransform)Field<RectTransform>(game,"formulaChargeFill").parent).rect.height>=8,"Compact reactor bar must be at least 8 canvas units.");
                game.InstallFormulaIcon(FormulaIcon.ReduceAdded,0);game.InstallFormulaIcon(FormulaIcon.UnsaturatedFat,1);yield return Wait(.8f);
                Check(Field<Button>(game,"learningFire").interactable,"Charged valid assembly must enable Fire.");
                TextFits(Field<RectTransform>(game,"learningRoot"));Record(PlasmaWorkbenchChecks.VerifyVisibleHitTargets());yield return Capture("synthesis-ready-"+suffix);
                Click(Field<RectTransform>(game,"learningRoot"),"Fire Plasma");yield return WaitForDialog(game);
                TextFits(Field<RectTransform>(game,"modal"));yield return Capture("result-"+suffix);
                Record("PASS: landscape text bounds and dock hit targets "+suffix);
            }
            SelectResolution(1600,900);game.ShowMenu();yield return Wait();
            yield return VerifyRemainingControls(game);
            game.ShowMenu();yield return Wait();
            Record("PASS: acceptance checks finished; screenshots recorded; APK/physical Android not tested");
        }
        static IEnumerator WaitForDialog(BattleManager game)
        {
            float deadline=Time.unscaledTime+5;
            while(!Field<RectTransform>(game,"modal").gameObject.activeInHierarchy&&Time.unscaledTime<deadline)yield return null;
            Check(Field<RectTransform>(game,"modal").gameObject.activeInHierarchy,"Action must display feedback within five seconds.");
            yield return Wait();
        }
        static IEnumerator VerifyFreshHierarchy(BattleManager original)
        {
            original.gameObject.SetActive(false);
            var root=new GameObject("Fresh UI QA");root.SetActive(false);
            try
            {
                var fresh=root.AddComponent<BattleManager>();
                foreach(var field in typeof(BattleManager).GetFields(BindingFlags.Instance|BindingFlags.Public))
                    if(field.Name!="model"&&field.Name!="defeated"&&field.Name!="hierarchyBaked")field.SetValue(fresh,field.GetValue(original));
                fresh.hierarchyBaked=false;root.SetActive(true);yield return Wait();
                Record(VerifySkin(fresh));
                int count=root.GetComponentsInChildren<Transform>(true).Length;
                Invoke(fresh,"ApplyPresentation");Invoke(fresh,"ApplyPresentation");
                Check(root.GetComponentsInChildren<Transform>(true).Length==count,"Fresh styling must not duplicate UI.");
                Click(Field<RectTransform>(fresh,"titleRoot"),"START");Check(fresh.View==GameView.World,"Fresh START must open map.");
                fresh.BeginEncounter(1);fresh.Diagnose();yield return Wait(.5f);
                Check(fresh.model.diagnosed,"Fresh diagnosis wiring must finish scan.");
                Invoke(fresh,"CloseLearningWorkbench");fresh.Synthesize();yield return Wait();
                Check(Field<RectTransform>(fresh,"learningRoot").gameObject.activeInHierarchy,"Fresh synthesis must open its own dock.");
                Record(PlasmaWorkbenchChecks.VerifyVisibleHitTargets());
            }
            finally
            {
                root.SetActive(false);UnityEngine.Object.Destroy(root);original.gameObject.SetActive(true);
            }
            yield return Wait();
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Fresh QA must restore a single EventSystem.");
            Record("PASS: fresh Awake build, repeated styling, START, scan, synthesis, hit targets, and cleanup agree with baked path");
        }
        static IEnumerator VerifyRemainingControls(BattleManager game)
        {
            game.StartGame();yield return Wait();var world=Field<RectTransform>(game,"worldRoot");
            Click(world,"SETTINGS");yield return Wait();Check(game.SettingsOpen,"Map settings must open.");
            Click(Field<RectTransform>(game,"overlayContent"),"KEMBALI");yield return Wait();
            Click(world,"MENU");Check(game.View==GameView.Title,"Map MENU must return to title.");
            game.StartGame();game.BeginEncounter(1);yield return Wait(.35f);Click(Field<Button>(game,"diagnose"));yield return Wait(.5f);
            Click(Field<RectTransform>(game,"diagnosisRoot"),"TUTUP");yield return Wait();
            for(int attempt=0;attempt<2;attempt++)
            {
                game.model.energy=20;Invoke(game,"Refresh");yield return Wait();Click(Field<Button>(game,"restore"));yield return Wait();
                Check(game.Phase==BattlePhase.Trivia,"Restore must show trivia.");TextFits(Field<RectTransform>(game,"modal"));
                var question=Field<TriviaQuestion>(game,"currentQuestion");
                Click(Field<RectTransform>(game,"options"),question.options[attempt==0?question.answer:(question.answer+1)%3]);
                yield return WaitForDialog(game);TextFits(Field<RectTransform>(game,"modal"));
                Check(game.model.energy==(attempt==0?60:30),"Correct/incorrect answers must preserve +40/+10 MP.");
                yield return Capture(attempt==0?"restore-correct":"restore-incorrect");
                Click(Field<RectTransform>(game,"options"),"LANJUT");yield return Wait(1.5f);
            }
            Click(Field<Button>(game,"homeostasis"));yield return Wait(1.8f);
            Check(game.model.shieldHits==1&&game.model.lastReflectedDamage>0,"Homeostasis button must parry and reflect the first hit.");
            yield return Capture("homeostasis");
            Record("PASS: map settings/menu, Diagnose close, Restore correct/incorrect feedback, Continue, and Homeostasis via live pointer targets");

            game.StartGame();
            for(int encounter=0;encounter<2;encounter++)
            {
                game.BeginEncounter(encounter);yield return Wait(.35f);Click(Field<Button>(game,"diagnose"));yield return Wait(.5f);
                Click(Field<RectTransform>(game,"diagnosisRoot"),"TUTUP");yield return Wait();Click(Field<Button>(game,"synthesize"));yield return Wait();
                game.InstallFormulaIcon(encounter==0?FormulaIcon.ReduceAdded:FormulaIcon.Water,0);
                game.InstallFormulaIcon(encounter==0?FormulaIcon.UnsaturatedFat:FormulaIcon.WholeFruit,1);yield return Wait(.8f);
                game.model.enemyHp=1;Invoke(game,"Refresh");Click(Field<Button>(game,"learningFire"));yield return WaitForDialog(game);
                Check(game.Phase==BattlePhase.Victory && !game.defeated[encounter], "Lethal result must precede victory rewards.");
                Click(Field<RectTransform>(game,"options"),"LANJUT · KEMENANGAN");yield return WaitForDialog(game);
                TextFits(Field<RectTransform>(game,"modal"));yield return Capture("victory");
                Click(Field<RectTransform>(game,"options"),"KEMBALI KE PETA");yield return Wait();
                Check(game.View==GameView.World&&game.defeated[encounter],"Victory return must preserve defeated marker.");
            }
            game.defeated[2]=game.defeated[3]=true;
            game.TryAdvanceStage();
            game.defeated[4]=true;
            game.BeginEncounter(5);yield return Wait(.35f);Click(Field<Button>(game,"diagnose"));yield return Wait(.5f);
            Click(Field<RectTransform>(game,"diagnosisRoot"),"TUTUP");yield return Wait();Click(Field<Button>(game,"synthesize"));yield return Wait();
            game.InstallFormulaIcon(FormulaIcon.CarbonMonoxide,0);game.InstallFormulaIcon(FormulaIcon.Acetaldehyde,1);yield return Wait(.8f);
            game.model.enemyHp=1;Invoke(game,"Refresh");Click(Field<Button>(game,"learningFire"));yield return WaitForDialog(game);
            Check(game.Phase==BattlePhase.Victory && !game.defeated[5], "Boss result must precede the completion reward.");
            Click(Field<RectTransform>(game,"options"),"LANJUT · KEMENANGAN");yield return WaitForDialog(game);
            Check(game.Phase==BattlePhase.Complete,"Final boss defeat must show mission completion.");
            TextFits(Field<RectTransform>(game,"modal"));yield return Capture("mission-complete");
            Click(Field<RectTransform>(game,"options"),"KEMBALI KE PETA");yield return Wait();
            Check(game.View==GameView.World&&game.defeated[5],"Victory return must preserve boss defeated marker.");
            TextFits(world);yield return Capture("map-complete");
            game.StartGame();game.BeginEncounter(0);yield return Wait();game.Diagnose();yield return Wait(.5f);
            Click(Field<RectTransform>(game,"diagnosisRoot"),"TUTUP");game.model.hp=1;game.model.energy=20;Invoke(game,"Refresh");yield return Wait();
            Click(Field<Button>(game,"restore"));yield return Wait();var last=Field<TriviaQuestion>(game,"currentQuestion");
            Click(Field<RectTransform>(game,"options"),last.options[last.answer]);yield return WaitForDialog(game);
            Click(Field<RectTransform>(game,"options"),"LANJUT");yield return WaitForDialog(game);
            Check(game.Phase==BattlePhase.Defeat,"Enemy hit on 1 HP must show defeat.");TextFits(Field<RectTransform>(game,"modal"));yield return Capture("defeat");
            Click(Field<RectTransform>(game,"options"),"COBA LAGI");yield return Wait();
            Check(game.View==GameView.World&&game.model.hp==100&&game.model.energy==60,"Retry must reset session.");
            Record("PASS: lethal Fire, both victory stages, map completion, defeat, and Retry via live pointer targets (1 HP fixtures)");
        }
        [MenuItem("Tools/Vein Vanguard/UI Refactor/Check Feedback Regression")]
        public static void CheckFeedbackRegression()
        {
            Check(Application.isPlaying,"Run feedback regression in Play Mode.");
            steps.Clear();steps.Push(VerifyFeedbackRegression(UnityEngine.Object.FindFirstObjectByType<BattleManager>()));lastFrame=-1;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        static IEnumerator VerifyFeedbackRegression(BattleManager game)
        {
            game.ShowMenu();yield return Wait();
            var settings=Field<RectTransform>(game,"titleRoot").GetComponentsInChildren<Button>().First(button=>button.name=="SETTINGS");
            ExecuteEvents.Execute(settings.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);yield return null;
            bool keyboardPressed=settings.GetComponent<OutlinedUiGraphic>().visualOffset.y<0;
            Record((keyboardPressed?"PASS":"FAIL")+": keyboard submit displays pressed bevel without invoking action twice");
            Canvas.ForceUpdateCanvases();
            foreach(var slider in Field<RectTransform>(game,"overlayContent").GetComponentsInChildren<Slider>())
                Check(Mathf.Abs(slider.handleRect.rect.width-slider.handleRect.rect.height)<.1f,"Slider thumb must render as a circle, not stretch with its touch container: "+slider.handleRect.rect.size);
            yield return Wait(.2f);game.CloseOverlay();game.StartGame();game.BeginEncounter(0);game.Diagnose();yield return Wait(.5f);
            var message=Field<RectTransform>(game,"safe").Find("Message Panel");bool narrativeHidden=!message.gameObject.activeSelf;
            Record((narrativeHidden?"PASS":"FAIL")+": drawer hides underlying narration");
            Invoke(game,"CloseLearningWorkbench");bool narrativeRestored=message.gameObject.activeSelf;
            Invoke(game,"Dialog","HASIL SYNTHESIS",UniversalFormula.Feedback(0,FormulaIcon.ReduceAdded,FormulaIcon.UnsaturatedFat),
                new[]{"LANJUT"},new UnityEngine.Events.UnityAction[]{game.ShowMenu});yield return Wait();Canvas.ForceUpdateCanvases();
            var single=Field<RectTransform>(game,"options").GetComponentInChildren<Button>();
            bool compact=((RectTransform)single.transform).rect.width<=330.1f;
            Record((compact?"PASS":"FAIL")+": single dialog action is compact");game.ShowMenu();
            Check(keyboardPressed&&narrativeHidden&&narrativeRestored&&compact,"Feedback regression must preserve keyboard pressed state, narration visibility, and compact single choice.");
        }
        static void VerifyKeyboardPipeline(BattleManager game)
        {
            var keyboard=Keyboard.current;Check(keyboard!=null,"Keyboard device must be available.");
            void Press(Key key)
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();
                Invoke(game,"UpdateFlow");
            }
            try
            {
                game.ShowMenu();EventSystem.current.SetSelectedGameObject(null);
                Press(Key.Tab);Check(EventSystem.current.currentSelectedGameObject.name=="START","Tab must focus START.");
                Press(Key.Space);Check(game.View==GameView.World,"Space must submit START through keyboard navigation.");
                var before=game.WorldPosition;Press(Key.D);Check(game.WorldPosition.x>before.x,"D must move through the real keyboard path.");
                Press(Key.Escape);Check(game.SettingsOpen,"Escape must open world settings.");
                Check(EventSystem.current.currentSelectedGameObject.GetComponent<Slider>(),"Settings must focus a slider.");
                Press(Key.Tab);Check(EventSystem.current.currentSelectedGameObject.name=="Efek suara Volume","Tab must stay in settings and focus SFX.");
                Press(Key.Escape);Check(!game.SettingsOpen,"Escape must close settings.");
                Record("PASS: InputSystem keyboard events -> Tab focus, Space START, D movement, Escape settings, modal Tab focus, Escape close");
            }
            finally {InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();game.ShowMenu();}
        }
    }
}
