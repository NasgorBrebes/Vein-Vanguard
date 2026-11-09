using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace VeinVanguard.Editor
{
    [InitializeOnLoad]
    public static class PortalSpawnChecks
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const string Results = "Temp/PortalSpawnResults.txt";
        const string Evidence = "Temp/PortalSpawn";
        static readonly Stack<IEnumerator> steps = new Stack<IEnumerator>();
        static int lastFrame;

        static PortalSpawnChecks()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("VV.Portal.QA", false)) return;
                SessionState.SetBool("VV.Portal.QA", false);
                steps.Clear(); steps.Push(Verify()); lastFrame = -1;
                EditorApplication.update += Tick;
            };
        }

        [MenuItem("Tools/Vein Vanguard/Portal Spawn/Run Acceptance")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Results, "Portal spawn acceptance / " + DateTime.Now.ToString("s") + "\n");
            SessionState.SetBool("VV.Portal.QA", true);
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; steps.Clear(); return; }
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                while (steps.Count > 0)
                {
                    var current = steps.Peek();
                    if (!current.MoveNext()) { steps.Pop(); continue; }
                    if (current.Current is IEnumerator nested) { steps.Push(nested); continue; }
                    return;
                }
                Record("PORTAL ACCEPTANCE COMPLETE");
                EditorApplication.update -= Tick;
                EditorApplication.isPlaying = false;
            }
            catch (Exception error)
            {
                Record("FAIL: " + error); Debug.LogException(error);
                steps.Clear(); EditorApplication.update -= Tick;
                EditorApplication.isPlaying = false;
            }
        }

        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Record(string message) => File.AppendAllText(Results, message + "\n");
        static T Field<T>(BattleManager game, string name) => (T)typeof(BattleManager).GetField(name, Hidden).GetValue(game);
        static void Set(BattleManager game, string name, object value) => typeof(BattleManager).GetField(name, Hidden).SetValue(game, value);
        static void Invoke(BattleManager game, string name, params object[] values) => typeof(BattleManager).GetMethod(name, Hidden).Invoke(game, values);
        static bool Playing(BattleManager game) => (bool)typeof(BattleManager).GetProperty("PortalSpawnPlaying").GetValue(game);
        static IEnumerator Wait(float seconds)
        {
            double end = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < end) yield return null;
        }
        static void Click(Button button)
        {
            Check(button && button.IsActive() && button.IsInteractable(), "Unavailable control: " + button);
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "Blocked pointer: " + button.name);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
        static IEnumerator Start(BattleManager game, bool skip)
        {
            game.ShowMenu(); yield return Wait(.1f);
            Click(Field<RectTransform>(game, "titleRoot").GetComponentsInChildren<Button>().Single(b => b.name == "START"));
            Check(game.OpeningPlaying, "START did not begin the opening.");
            if (skip) Click(Field<Button>(game, "openingSkip"));
            else
            {
                Set(game, "openingTime", 59.4f); Invoke(game, "RenderOpeningCinematic");
                Check(Playing(game), "Portal must be prepared behind the opening reveal.");
                Check(Field<RectTransform>(game, "worldContent").localScale.x > 2, "Reveal exposed the normal camera before close-up.");
                yield return Wait(.8f);
            }
            Check(!game.OpeningPlaying && Playing(game), "Opening must hand off to portal, including SKIP.");
        }
        static IEnumerator Finish(BattleManager game)
        {
            double end = Time.realtimeSinceStartupAsDouble + 5;
            while (Playing(game) && Time.realtimeSinceStartupAsDouble < end) yield return null;
            Check(!Playing(game), "Portal exceeded fallback deadline.");
            Check(Field<RectTransform>(game, "worldContent").localScale == Vector3.one, "Camera did not restore zoom 1.");
            Check(Field<Image>(game, "worldPlayerImage").color == Color.white, "Nanobot remained faded/tinted.");
            Check(!Field<RectTransform>(game, "spawnPortal").gameObject.activeSelf && !Field<CanvasGroup>(game, "spawnInputGate").blocksRaycasts,
                "Active portal or input blocker remained after handoff.");
            Check(Field<CanvasGroup[]>(game, "spawnHudGroups").All(g => !g || g.alpha >= 0), "HUD restoration produced an invalid alpha.");
        }
        static void CameraBounds(BattleManager game)
        {
            Canvas.ForceUpdateCanvases();
            var map = Field<RectTransform>(game, "worldContent");
            var view = Field<RectTransform>(game, "worldRoot");
            float zoom = map.localScale.x;
            Vector2 offset = map.anchoredPosition;
            Check(zoom >= .9999f && zoom <= 2.201f, "Camera zoom escaped bounds.");
            Check(offset.x <= .1f && offset.y <= .1f && offset.x + 2400 * zoom >= view.rect.width - .1f && offset.y + 1697 * zoom >= view.rect.height - .1f,
                "Camera exposed space outside map.");
        }
        static IEnumerator Verify()
        {
            yield return Wait(.25f);
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            Check(typeof(BattleManager).GetProperty("PortalSpawnPlaying") != null, "Portal spawn lifecycle is missing.");
            bool hadReduced = PlayerPrefs.HasKey("VV.Motion.Reduced"); int savedReduced = PlayerPrefs.GetInt("VV.Motion.Reduced");
            try
            {
                PlayerPrefs.SetInt("VV.Motion.Reduced", 0);
                yield return Start(game, false);
                Vector2 position = game.WorldPosition;
                var model = game.model; int hp = model.hp, mp = model.energy;
                Check(position == new Vector2(.50f, .04f), "MAP 01 spawn changed.");
                Check(Field<Image>(game, "worldPlayerImage").color.a < .01f, "Nanobot must start hidden.");
                Check(Field<CanvasGroup[]>(game, "spawnHudGroups").All(g => g.alpha == 0), "HUD should wait for spawn completion.");
                game.MoveWorld(Vector2.right * 100); game.BeginEncounter(0); game.OpenSettings(); game.TryAdvanceStage();
                Invoke(game, "HandleMapClick", new Vector2(Screen.width * .8f, Screen.height * .5f));
                Check(game.WorldPosition == position && game.View == GameView.World && !game.SettingsOpen, "Movement, encounter or settings leaked into spawn.");
                Check(!game.CanBeginEncounter(0) && !Field<bool>(game, "hasTarget"), "Encounter or tap target leaked into spawn.");
                var joystick = Field<ExplorationJoystick>(game, "joystick");
                joystick.changed(Vector2.right);
                Check(Field<Vector2>(game, "heldDirection") == Vector2.zero, "Analog leaked into spawn.");
                double started = Time.realtimeSinceStartupAsDouble;
                float initialElapsed = Field<float>(game, "spawnElapsed");
                while (Playing(game)) { CameraBounds(game); Check(game.WorldPosition == position, "Spawn changed logical position."); yield return null; }
                Check(Time.realtimeSinceStartupAsDouble - started + initialElapsed >= 3.18, "Normal portal ended prematurely.");
                yield return Finish(game);
                Check(game.model == model && model.hp == hp && model.energy == mp && game.defeated.All(x => !x), "Spawn changed session resources or progress.");
                var map = Field<RectTransform>(game, "worldContent"); Vector2 finalPan = map.anchoredPosition;
                Invoke(game, "PositionWorld"); Check(map.anchoredPosition == finalPan, "Gameplay camera snapped after handoff.");
                Check(Field<CanvasGroup[]>(game, "spawnHudGroups").All(g => g.alpha > .99f), "HUD not restored after normal spawn.");
                Record("PASS: START opening reveal -> cyan portal -> MAP 01; hidden actor/HUD, natural duration, input gates, resources, exact spawn and camera restore");

                yield return Start(game, true);
                var keyboard = Keyboard.current; Check(keyboard != null, "Keyboard not found.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); InputSystem.Update();
                position = game.WorldPosition;
                yield return Finish(game); yield return Wait(.15f);
                Check(game.WorldPosition == position, "Held keyboard moved Nanobot after portal.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                yield return Wait(.2f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); InputSystem.Update();
                yield return Wait(.1f);
                Check(game.WorldPosition.x > position.x, "Keyboard movement did not recover after release.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                game.BeginEncounter(0); Check(game.View == GameView.Battle, "Battle remains blocked after spawn.");
                Invoke(game, "ReturnToWorld"); yield return Wait(.1f);
                Check(!Playing(game), "Battle return repeated portal.");
                game.OpenSettings(); game.CloseOverlay(); Invoke(game, "PositionWorld");
                Check(!Playing(game), "Menu or refresh repeated portal.");
                Record("PASS: SKIP retains portal; held keyboard release, battle entry/return and menu/refresh do not repeat spawn");

                Array.Fill(game.defeated, true, 0, 4);
                game.model.hp = 73; game.model.energy = 27;
                Invoke(game, "PositionWorld");
                Click(Field<Button>(game, "stageExitButton"));
                double deadline = Time.realtimeSinceStartupAsDouble + 3;
                while (game.StageIndex != 1 && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                Check(game.StageIndex == 1 && Playing(game), "MAP 02 loading did not prepare portal.");
                Check(game.WorldPosition == new Vector2(.08f, .48f), "MAP 02 spawn changed.");
                Check(Field<Color>(game, "spawnColor").b > Field<Color>(game, "spawnColor").g, "MAP 02 must use violet energy.");
                while ((bool)typeof(BattleManager).GetProperty("IsStageLoading", Hidden).GetValue(game)) yield return null;
                yield return Finish(game);
                Check(game.model.hp == 73 && game.model.energy == 27 && game.defeated.Take(4).All(x => x) && !game.defeated[4], "MAP 02 portal reset progress/resources.");
                Check(game.CanBeginEncounter(4) && !game.CanBeginEncounter(5), "MAP 02 encounter gates changed.");
                Record("PASS: completed-four fixture -> Map ke dua pointer -> loading -> violet portal -> MAP 02; original spawn, progress, HP/MP and boss gate");

                yield return Start(game, true);
                Invoke(game, "OnApplicationFocus", false); float elapsed = Field<float>(game, "spawnElapsed");
                yield return Wait(.2f); Check(Field<float>(game, "spawnElapsed") == elapsed, "Unfocused portal advanced.");
                Invoke(game, "OnApplicationFocus", true); Invoke(game, "OnApplicationPause", true);
                yield return Wait(.2f); Check(Field<float>(game, "spawnElapsed") == elapsed, "Paused portal advanced.");
                Invoke(game, "OnApplicationPause", false); yield return Wait(.1f);
                Check(Field<float>(game, "spawnElapsed") > elapsed && Field<float>(game, "spawnElapsed") - elapsed < .3f, "Portal resume jumped.");
                game.enabled = false; yield return null; game.enabled = true;
                yield return Finish(game); Check(game.CanBeginEncounter(0), "Disable stranded gameplay.");
                yield return Start(game, true);
                UnityEngine.Object.Destroy(Field<RectTransform>(game, "spawnPortal").gameObject);
                yield return Wait(.15f); Check(!Playing(game) && game.CanBeginEncounter(0), "Lost portal reference stranded gameplay.");
                Invoke(game, "EnsurePortalSpawn");
                Record("PASS: focus/pause/resume, disable and lost-effect fallback restore camera, visibility and gameplay");

                var select = typeof(UiRefactorChecks).GetMethod("SelectResolution", BindingFlags.NonPublic | BindingFlags.Static);
                foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1600,900), new Vector2Int(1920,1080), new Vector2Int(2000,900) })
                {
                    select.Invoke(null, new object[] { size.x, size.y, false }); yield return Wait(.15f);
                    foreach (int stage in new[] { 0, 1 })
                    {
                        game.StartGame();
                        if (stage == 1) { Array.Fill(game.defeated, true, 0, 4); Check(game.TryAdvanceStage(), "Stage fixture failed."); }
                        Invoke(game, "BeginPortalSpawn");
                        float previousRadius = float.MaxValue;
                        foreach (float sample in new[] { .4f, 1.3f, 2.6f })
                        {
                            Set(game, "spawnElapsed", sample); Set(game, "spawnLastTick", Time.realtimeSinceStartupAsDouble); Invoke(game, "RenderPortalSpawn");
                            CameraBounds(game);
                            var corners = new Vector3[4];
                            Field<RectTransform>(game, "spawnEnergy").GetWorldCorners(corners);
                            Check(Screen.safeArea.Contains(corners[0]) && Screen.safeArea.Contains(corners[2]), "Portal close-up is cropped outside the safe area.");
                            float radius = Field<Image[]>(game, "spawnParticles")[0].rectTransform.anchoredPosition.magnitude;
                            if (sample <= 1.3f) Check(radius < previousRadius, "Particles must converge while the energy surface opens.");
                            previousRadius = radius;
                            ScreenCapture.CaptureScreenshot(Evidence + "/map-" + (stage + 1) + "-" + size.x + "x" + size.y + "-" + sample.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + ".png");
                            yield return null;
                        }
                        yield return Finish(game);
                    }
                    Record("PASS: bounded close-up, emergence and zoom-out at " + size + " on both maps; screenshots captured");
                }
                select.Invoke(null, new object[] { 1600, 900, false });
                PlayerPrefs.SetInt("VV.Motion.Reduced", 1);
                yield return Start(game, true);
                Check(map.localScale == Vector3.one, "Reduced motion zoomed the map.");
                Vector2 particle = Field<Image[]>(game, "spawnParticles")[0].rectTransform.anchoredPosition;
                Vector3 energyScale = Field<RectTransform>(game, "spawnEnergy").localScale;
                yield return Wait(.2f);
                Check(map.localScale == Vector3.one && Field<Image[]>(game, "spawnParticles")[0].rectTransform.anchoredPosition == particle && Field<RectTransform>(game, "spawnEnergy").localScale == energyScale, "Reduced motion moved particles, portal scale or camera.");
                yield return Finish(game);
                Check(Field<float>(game, "spawnElapsed") < 1, "Reduced-motion fallback is too long.");
                Record("PASS: reduced motion uses short fade without zoom or orbit");

                game.BeginEncounter(0); game.model.hp = 0; game.model.phase = BattlePhase.Defeat; Invoke(game, "Refresh");
                Invoke(game, "StartGameFromUi");
                double retryEnd = Time.realtimeSinceStartupAsDouble + 3;
                while (!Playing(game) && Time.realtimeSinceStartupAsDouble < retryEnd) yield return null;
                Check(Playing(game) && game.StageIndex == 0 && game.model.hp == 100 && game.model.energy == 60 && game.defeated.All(x => !x), "Retry failed to reset session and replay portal.");
                yield return Finish(game);
                int count = game.GetComponentsInChildren<Transform>(true).Length;
                Invoke(game, "EnsurePortalSpawn"); Invoke(game, "EnsurePortalSpawn");
                Check(count == game.GetComponentsInChildren<Transform>(true).Length, "Portal setup duplicated nodes.");
                Check(Field<RectTransform>(game, "spawnPortal").GetComponentsInChildren<Collider>(true).Length == 0 && Field<RectTransform>(game, "spawnPortal").GetComponentsInChildren<Collider2D>(true).Length == 0, "Portal should have no collider.");
                Record("PASS: mission failure/retry reset session and portal; idempotent setup, no collider or active effect debris");
                game.ShowMenu();
            }
            finally
            {
                if (Keyboard.current != null) { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); InputSystem.Update(); }
                if (hadReduced) PlayerPrefs.SetInt("VV.Motion.Reduced", savedReduced); else PlayerPrefs.DeleteKey("VV.Motion.Reduced");
            }
        }
    }
}
