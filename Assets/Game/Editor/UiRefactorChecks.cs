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
        static UiRefactorChecks()
        {
            EditorApplication.playModeStateChanged+=state=>{
                if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("VV.UI.QA",false))
                {
                    SessionState.SetBool("VV.UI.QA",false);
                    steps.Clear();steps.Push(RunPlayChecks());lastFrame=-1;
                    EditorApplication.update+=Tick;
                }
            };
        }
        static T Field<T>(BattleManager game, string name) => (T)typeof(BattleManager).GetField(name, Private).GetValue(game);
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
            foreach (var path in new[] { "Header/Nanobot Status", "Header/Pathogen Status", "Commands/Message Panel" })
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
                EditorApplication.update-=Tick;Record("ACCEPTANCE COMPLETE");
            }
            catch(Exception error)
            {
                EditorApplication.update-=Tick;steps.Clear();
                var game=UnityEngine.Object.FindFirstObjectByType<BattleManager>();
                if(game){game.SetMusicVolume(SessionState.GetFloat("VV.UI.Music",.45f));game.SetSfxVolume(SessionState.GetFloat("VV.UI.Sfx",.75f));PlayerPrefs.Save();}
                Record("FAIL: "+error);Debug.LogException(error);
            }
        }
        static IEnumerator Wait(float seconds=.15f)
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
                Check(((RectTransform)Field<RectTransform>(game,"formulaChargeFill").parent).rect.height>=16,"Reactor bar must be at least 16 canvas units.");
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
            game.StartGame();game.BeginEncounter(1);yield return Wait();Click(Field<Button>(game,"diagnose"));yield return Wait(.5f);
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
                Click(Field<RectTransform>(game,"options"),"LANJUT");yield return Wait(1.2f);
            }
            Click(Field<Button>(game,"homeostasis"));yield return Wait(1.8f);
            Check(game.model.shieldHits==1&&game.model.lastReflectedDamage>0,"Homeostasis button must parry and reflect the first hit.");
            yield return Capture("homeostasis");
            Record("PASS: map settings/menu, Diagnose close, Restore correct/incorrect feedback, Continue, and Homeostasis via live pointer targets");

            game.StartGame();
            for(int encounter=0;encounter<2;encounter++)
            {
                game.BeginEncounter(encounter);yield return Wait();Click(Field<Button>(game,"diagnose"));yield return Wait(.5f);
                Click(Field<RectTransform>(game,"diagnosisRoot"),"TUTUP");yield return Wait();Click(Field<Button>(game,"synthesize"));yield return Wait();
                game.InstallFormulaIcon(encounter==0?FormulaIcon.ReduceAdded:FormulaIcon.Water,0);
                game.InstallFormulaIcon(encounter==0?FormulaIcon.UnsaturatedFat:FormulaIcon.WholeFruit,1);yield return Wait(.8f);
                game.model.enemyHp=1;Invoke(game,"Refresh");Click(Field<Button>(game,"learningFire"));yield return WaitForDialog(game);
                Check(game.Phase==(encounter==0?BattlePhase.Victory:BattlePhase.Complete),"Lethal Fire must show victory/completion.");
                TextFits(Field<RectTransform>(game,"modal"));yield return Capture(encounter==0?"victory":"mission-complete");
                Click(Field<RectTransform>(game,"options"),"KEMBALI KE PETA");yield return Wait();
                Check(game.View==GameView.World&&game.defeated[encounter],"Victory return must preserve defeated marker.");
            }
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
            var message=Field<RectTransform>(game,"commands").Find("Message Panel");bool narrativeHidden=!message.gameObject.activeSelf;
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
