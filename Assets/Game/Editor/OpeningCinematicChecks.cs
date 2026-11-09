using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace VeinVanguard.Editor
{
    [InitializeOnLoad]
    public static class OpeningCinematicChecks
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const string Results = "Temp/OpeningCinematicV3Results.txt";
        const string Evidence = "Temp/OpeningCinematicV3";
        static readonly Stack<IEnumerator> steps = new Stack<IEnumerator>();
        static int lastFrame;

        static OpeningCinematicChecks()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("VV.Opening.QA", false)) return;
                SessionState.SetBool("VV.Opening.QA", false);
                steps.Clear();
                steps.Push(VerifyOpening());
                lastFrame = -1;
                EditorApplication.update += Tick;
            };
        }

        [MenuItem("Tools/Vein Vanguard/Opening/Run Acceptance")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Results, "Opening acceptance / " + DateTime.Now.ToString("s") + "\n");
            SessionState.SetBool("VV.Opening.QA", true);
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
                Record("OPENING ACCEPTANCE COMPLETE");
                EditorApplication.update -= Tick;
                EditorApplication.isPlaying = false;
            }
            catch (Exception error)
            {
                Record("FAIL: " + error);
                Debug.LogException(error);
                steps.Clear();
                EditorApplication.update -= Tick;
                EditorApplication.isPlaying = false;
            }
        }

        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        static void Record(string message) => File.AppendAllText(Results, message + "\n");
        static T Field<T>(BattleManager game, string name) => (T)typeof(BattleManager).GetField(name, Hidden).GetValue(game);
        static void Invoke(BattleManager game, string name, params object[] args) => typeof(BattleManager).GetMethod(name, Hidden).Invoke(game, args);
        static bool Playing(BattleManager game) => (bool)typeof(BattleManager).GetProperty("OpeningPlaying").GetValue(game);
        static float Elapsed(BattleManager game) => Field<float>(game, "openingTime");

        static IEnumerator Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }
        static void Click(Button button)
        {
            Check(button && button.IsActive() && button.IsInteractable(), "Button unavailable.");
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "Pointer cannot reach " + button.name);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
        static IEnumerator Start(BattleManager game)
        {
            game.ShowMenu();
            yield return Wait(.15f);
            var title = Field<RectTransform>(game, "titleRoot");
            Click(title.GetComponentsInChildren<Button>().Single(b => b.name == "START"));
            Check(Playing(game), "START must open the story cinematic.");
            Check(game.View == GameView.Title && !Field<RectTransform>(game, "worldRoot").gameObject.activeSelf,
                "The map must wait for the cinematic.");
        }
        static IEnumerator MapReady(BattleManager game)
        {
            // The opening hands off to the new portal before exploration input is released.
            double deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (game.PortalSpawnPlaying && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Check(!game.PortalSpawnPlaying, "Portal did not release exploration after opening.");
            Check(!Playing(game) && game.View == GameView.World && game.StageIndex == 0,
                "Expected MAP 01 after opening. time=" + Elapsed(game) + " view=" + game.View + " playing=" + Playing(game));
            Check(game.model.hp == 100 && game.model.energy == 60 && game.defeated.All(x => !x), "New session must retain initial resources.");
            Check(!Field<CanvasGroup>(game, "stageLoading").blocksRaycasts, "Opening must release the loading gate.");
            Check(!Field<CanvasGroup>(game, "openingGroup").blocksRaycasts, "Opening must release pointer input.");
            Check(Field<RectTransform>(game, "worldRoot").gameObject.activeSelf && !game.SettingsOpen, "Map and HUD must be active.");
        }
        static void Seek(BattleManager game, float seconds)
        {
            typeof(BattleManager).GetField("openingTime", Hidden).SetValue(game, seconds);
            Invoke(game, "RenderOpeningCinematic");
        }
        static void TextFits(BattleManager game)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var label in Field<CanvasGroup>(game, "openingGroup").GetComponentsInChildren<TextMeshProUGUI>())
            {
                label.ForceMeshUpdate();
                Check(!label.isTextOverflowing, "Opening text overflows: " + label.name + " / " + label.text);
            }
            var skip = (RectTransform)Field<Button>(game, "openingSkip").transform;
            var corners = new Vector3[4];
            skip.GetWorldCorners(corners);
            Check(Screen.safeArea.Contains(corners[0]) && Screen.safeArea.Contains(corners[2]), "SKIP must remain in safe area.");
            float density = Field<float>(game, "touchDensity");
            Check((corners[2].y - corners[0].y) / Mathf.Max(1, density) >= 48 - .1f, "SKIP target must be at least 48 dp.");
        }
        static IEnumerator VerifyOpening()
        {
            yield return Wait(.3f);
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            Check(typeof(BattleManager).GetProperty("OpeningPlaying") != null, "Opening lifecycle has not been implemented.");
            yield return VerifyV3Journey(game);
            yield return VerifyV2Presentation(game);
            yield return Start(game);
            yield return Wait(.2f);
            float before = Elapsed(game);
            Field<RectTransform>(game, "titleRoot").GetComponentsInChildren<Button>(true).Single(b => b.name == "START").onClick.Invoke();
            Check(Elapsed(game) >= before, "Duplicate START restarted the opening.");
            var position = game.WorldPosition;
            game.MoveWorld(Vector2.right * 100);
            game.BeginEncounter(0);
            game.OpenSettings();
            Check(game.WorldPosition == position && game.View == GameView.Title && !game.SettingsOpen, "Gameplay input leaked into opening.");
            Click(Field<Button>(game, "openingSkip"));
            yield return MapReady(game);
            var session = game.model;
            Field<Button>(game, "openingSkip").onClick.Invoke();
            Check(game.model == session, "Repeated SKIP reset the session twice.");
            yield return Wait(.2f);
            position = game.WorldPosition;
            game.MoveWorld(Vector2.right * 10);
            Check(game.WorldPosition != position, "Map movement did not resume after SKIP.");
            Record("PASS: START, duplicate input, pointer SKIP, initial resources, map HUD, and movement");

            yield return Start(game);
            Invoke(game, "OnApplicationFocus", false);
            float paused = Elapsed(game);
            yield return Wait(.25f);
            Check(Mathf.Approximately(Elapsed(game), paused), "Cinematic advanced without focus.");
            Invoke(game, "OnApplicationFocus", true);
            Invoke(game, "OnApplicationPause", true);
            yield return Wait(.2f);
            Check(Mathf.Approximately(Elapsed(game), paused), "Cinematic advanced during application pause.");
            Invoke(game, "OnApplicationPause", false);
            yield return Wait(.15f);
            Check(Elapsed(game) > paused && Elapsed(game) - paused < .4f, "Resume jumped or failed.");
            game.ShowMenu();
            Check(!Playing(game) && game.View == GameView.Title, "Menu did not cancel the cinematic.");
            Record("PASS: focus, pause, resume, and cancellation restore lifecycle");

            yield return Start(game);
            game.enabled = false;
            yield return null;
            game.enabled = true;
            Check(!Playing(game) && game.View == GameView.Title && Field<RectTransform>(game, "titleRoot").gameObject.activeSelf,
                "Disable and re-enable must recover the title.");
            yield return Start(game);
            var keyboard = Keyboard.current;
            Check(keyboard != null, "Expected the Editor keyboard.");
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                Click(Field<Button>(game, "openingSkip"));
                var landed = game.WorldPosition;
                yield return MapReady(game);
                yield return Wait(.15f);
                Check(game.WorldPosition == landed, "Held movement escaped into MAP 01.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                float releaseDeadline = Time.realtimeSinceStartup + 2;
                while (Field<bool>(game, "openingWaitForMovementRelease") && Time.realtimeSinceStartup < releaseDeadline) yield return null;
                Check(!Field<bool>(game, "openingWaitForMovementRelease"), "Movement release gate did not observe key-up.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                float movementDeadline = Time.realtimeSinceStartup + 2;
                while (game.WorldPosition.x <= landed.x && Time.realtimeSinceStartup < movementDeadline) yield return null;
                Check(game.WorldPosition.x > landed.x, "Movement did not resume after key release.");
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
            }
            Record("PASS: disable recovery and held-key release across map handoff");

            yield return Start(game);
            Invoke(game, "OnApplicationFocus", true);
            float started = Time.realtimeSinceStartup;
            float[] samples = { 4.3f, 15, 29, 39.6f, 56.8f };
            for (int i = 0; i < samples.Length; i++)
            {
                while (Playing(game) && Elapsed(game) < samples[i] && Time.realtimeSinceStartup - started < 95) yield return null;
                Check(Playing(game) && Elapsed(game) >= samples[i], "Opening failed to reach sample " + samples[i] + " time=" + Elapsed(game));
                TextFits(game);
                ScreenCapture.CaptureScreenshot(Evidence + "/scene-" + (i + 1) + ".png");
                yield return null;
            }
            while (Playing(game) && Time.realtimeSinceStartup - started < 95) yield return null;
            float storySeconds = Time.realtimeSinceStartup - started;
            yield return MapReady(game);
            Check(Time.realtimeSinceStartup - started >= 59, "The full opening ended prematurely.");
            ScreenCapture.CaptureScreenshot(Evidence + "/map-after-opening.png");
            Record("PASS: complete 60-second five-scene sequence, subtitles, and normal map handoff; wall seconds=" +
                storySeconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));

            var resolution = typeof(UiRefactorChecks).GetMethod("SelectResolution", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080), new Vector2Int(2000, 900) })
            {
                resolution.Invoke(null, new object[] { size.x, size.y, false });
                yield return Wait(.2f);
                yield return Start(game);
                foreach (float time in samples)
                {
                    Seek(game, time);
                    yield return null;
                    TextFits(game);
                }
                Seek(game, 59.5f);
                Click(Field<Button>(game, "openingSkip"));
                yield return MapReady(game);
                Record("PASS: five compositions, text bounds, safe area and late SKIP " + size);
            }
            resolution.Invoke(null, new object[] { 1600, 900, false });
            bool had = PlayerPrefs.HasKey("VV.Motion.Reduced");
            int saved = PlayerPrefs.GetInt("VV.Motion.Reduced");
            try
            {
                PlayerPrefs.SetInt("VV.Motion.Reduced", 1);
                yield return Start(game);
                Seek(game, 4);
                var background = Field<RectTransform>(game, "openingEnvironment");
                var oldPosition = background.anchoredPosition;
                var oldScale = background.localScale;
                Seek(game, 7);
                Check(background.anchoredPosition == oldPosition && background.localScale == oldScale, "Reduced motion still moves the background.");
                Seek(game, 23.5f);
                var orbit = Field<UnityEngine.UI.Image[]>(game, "openingEnergyParticles")[0].rectTransform;
                Vector2 orbitPosition = orbit.anchoredPosition;
                Vector2 unitPosition = Field<UnityEngine.UI.Image>(game, "openingUnit").rectTransform.anchoredPosition;
                Seek(game, 27.5f);
                Check(orbit.anchoredPosition == orbitPosition && Field<UnityEngine.UI.Image>(game, "openingUnit").rectTransform.anchoredPosition == unitPosition,
                    "Reduced motion still moves activation particles or the body.");
                Seek(game, 28.1f);
                var head = Field<RectTransform>(game, "openingUnitHead");
                var headRotation = head.localRotation;
                Seek(game, 28.6f);
                Check(head.localRotation == headRotation, "Reduced motion still animates the head rig.");
                Seek(game, 53);
                Vector2 runnerPosition = Field<UnityEngine.UI.Image>(game, "openingRunner").rectTransform.anchoredPosition;
                Seek(game, 57);
                Check(Field<UnityEngine.UI.Image>(game, "openingRunner").rectTransform.anchoredPosition == runnerPosition,
                    "Reduced motion still moves the journey.");
                var portal = Field<RectTransform>(game, "openingExitPortal");
                Vector2 portalCenter = RectTransformUtility.WorldToScreenPoint(null, portal.TransformPoint(portal.rect.center));
                Check(Screen.safeArea.Contains(portalCenter), "Reduced-motion destination must remain visible.");
                Click(Field<Button>(game, "openingSkip"));
                yield return MapReady(game);
            }
            finally
            {
                if (had) PlayerPrefs.SetInt("VV.Motion.Reduced", saved);
                else PlayerPrefs.DeleteKey("VV.Motion.Reduced");
            }
            Record("PASS: reduced motion and preference cleanup");
            int count = game.GetComponentsInChildren<Transform>(true).Length;
            Invoke(game, "EnsureOpeningCinematic");
            Invoke(game, "EnsureOpeningCinematic");
            Check(count == game.GetComponentsInChildren<Transform>(true).Length, "Repeated setup duplicates opening nodes.");
            Record("PASS: idempotent opening construction");
            game.ShowMenu();
        }

        static IEnumerator VerifyV2Presentation(BattleManager game)
        {
            yield return Start(game);
            var root = Field<CanvasGroup>(game, "openingGroup").transform;
            Check(!root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Signal Monitor"),
                "V2 must replace the static cyan bar dashboard with the emergency event.");
            Seek(game, .4f);
            Check(Field<UnityEngine.UI.Image>(game, "openingCurtain").color.a > .98f, "Opening must hold a dark first frame.");
            var model = game.model;
            int hp = model.hp, energy = model.energy, enemyHp = model.enemyHp;
            Seek(game, 11.08f);
            var threat = Field<UnityEngine.UI.Image[]>(game, "openingThreats")[0];
            var first = threat.sprite;
            var scan = Field<RectTransform>(game, "openingThreatScan");
            float scanY = scan.anchoredPosition.y;
            Seek(game, 11.27f);
            Check(threat.sprite != first, "Threat must play its original animated frames.");
            Check(scan.anchoredPosition.y != scanY, "Scanner must travel across the enemy.");
            Check(!root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Threat PANGAN" || t.name == "Four Abilities"),
                "Threat cards and the four-ability list must be replaced by staged actions.");

            Seek(game, 22.5f);
            var unit = Field<UnityEngine.UI.Image>(game, "openingUnit");
            var sleeping = unit.sprite;
            var sleepingPosition = unit.rectTransform.anchoredPosition;
            Seek(game, 28.7f);
            Check(unit.sprite != sleeping && unit.rectTransform.anchoredPosition != sleepingPosition,
                "Activation must animate both the original pose sequence and the body.");
            Seek(game, 35.8f);
            Check(Field<UnityEngine.UI.Image>(game, "openingDemoScan").enabled, "Diagnose must demonstrate a scan.");
            Seek(game, 38.5f);
            var card = Field<RectTransform[]>(game, "openingCards")[0];
            Vector2 inFlight = card.anchorMin;
            Seek(game, 40.2f);
            Check(card.anchorMin != inFlight, "Synthesize cards must travel into ports.");
            Seek(game, 41.3f);
            var mp = Field<RectTransform>(game, "openingDemoMpFill");
            float charge = mp.anchorMax.x;
            Seek(game, 43.2f);
            Check(mp.anchorMax.x > charge, "Restore must visibly fill MP.");
            Seek(game, 45.2f);
            Check(Field<UnityEngine.UI.Image>(game, "openingDemoShield").enabled, "Homeostasis must visibly surround the unit.");
            Check(game.model == model && model.hp == hp && model.energy == energy && model.enemyHp == enemyHp,
                "Demonstrations must not alter combat resources.");
            foreach (float time in new[] { 19.99f, 29.99f, 39.99f, 49.99f })
            {
                Seek(game, time);
                Vector3 before = Field<RectTransform>(game, "openingCamera").localScale;
                Seek(game, time + .02f);
                Check(Vector3.Distance(before, Field<RectTransform>(game, "openingCamera").localScale) < .01f,
                    "Camera zoom snapped at an internal ten-second boundary.");
            }
            foreach (float time in new[] { 35.8f, 39.6f, 42.5f, 45.2f })
            {
                Seek(game, time);
                TextFits(game);
                ScreenCapture.CaptureScreenshot(Evidence + "/ability-" + time.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + ".png");
                yield return null;
            }
            Seek(game, 59.55f);
            Check(Playing(game) && game.View == GameView.World && Field<bool>(game, "openingMapPrepared"),
                "Map must be prepared behind the final reveal.");
            var prepared = game.model;
            Click(Field<Button>(game, "openingSkip"));
            yield return MapReady(game);
            Check(game.model == prepared, "SKIP during map reveal must not reset the prepared session twice.");
            Record("PASS: V2 emergency staging, animated threats/scanner, activation poses, four action demos, continuous camera, and map reveal");

            foreach (float time in new[] { 3.5f, 15f, 28f, 40f, 55f })
            {
                yield return Start(game);
                Seek(game, time);
                Click(Field<Button>(game, "openingSkip"));
                yield return MapReady(game);
                var session = game.model;
                yield return Wait(.15f);
                Check(game.model == session && !Playing(game), "An opening event survived SKIP.");
                Check(!Field<AudioSource>(game, "effectSource").isPlaying, "A cinematic SFX survived SKIP.");
            }
            Record("PASS: SKIP cancels every V2 scene and releases a single MAP 01 session");
        }

        static IEnumerator VerifyV3Journey(BattleManager game)
        {
            yield return Start(game);
            Seek(game, 49);
            var runner = Field<UnityEngine.UI.Image>(game, "openingRunner");
            var camera = Field<RectTransform>(game, "openingCamera");
            var safe = Field<RectTransform>(game, "openingSafe");
            float firstPosition = runner.rectTransform.anchoredPosition.x;
            float firstCamera = camera.anchoredPosition.x;
            Seek(game, 56);
            float travelled = runner.rectTransform.anchoredPosition.x - firstPosition;
            Check(travelled > safe.rect.width * .35f, "V3 ending must travel through the corridor rather than a short walking loop.");
            Check(camera.anchoredPosition.x < firstCamera - travelled * .5f, "Camera must track the actual journey.");
            var map = Field<UnityEngine.UI.Image>(game, "openingJourneyMap");
            Check(map.sprite == game.worldMap, "The destination must show the existing MAP 01 artwork.");
            Check(Field<UnityEngine.UI.Image>(game, "openingBackdropNext").enabled, "Tracking must have a second background span.");
            var title = Field<CanvasGroup>(game, "openingEndTitle");
            Check(title.transform.parent == safe, "Mission title must stay readable while the world camera tracks.");
            Seek(game, 58.25f);
            var lastStep = runner.sprite;
            Seek(game, 58.4f);
            Check(runner.sprite == lastStep, "The unit must stop stepping when its world movement ends.");

            Seek(game, 24.1f);
            var sparks = Field<UnityEngine.UI.Image[]>(game, "openingRiseSparks");
            Vector2 sparkPosition = sparks[0].rectTransform.anchoredPosition;
            Seek(game, 24.4f);
            Check(sparks[0].rectTransform.anchoredPosition.y > sparkPosition.y, "Activation energy must rise from feet toward the head.");
            Seek(game, 28.15f);
            var head = Field<RectTransform>(game, "openingUnitHead");
            var before = head.localRotation;
            Seek(game, 28.5f);
            Check(Quaternion.Angle(before, head.localRotation) > .1f, "ONLINE must include a head reaction.");
            Check(Field<UnityEngine.UI.Image>(game, "openingOnlineGlow").color.a > 0, "ONLINE must show a light pulse.");

            Seek(game, 10.2f);
            Vector2 foodEntry = Field<UnityEngine.UI.Image[]>(game, "openingThreats")[0].rectTransform.anchoredPosition;
            Seek(game, 13.6f);
            Vector2 tobaccoEntry = Field<UnityEngine.UI.Image[]>(game, "openingThreats")[1].rectTransform.anchoredPosition;
            Check(Vector2.Distance(foodEntry, tobaccoEntry) > 30, "Threat categories need distinct entry motion.");
            Seek(game, 11.4f);
            var reaction = Field<UnityEngine.UI.Image[]>(game, "openingThreats")[0].sprite;
            int poseIndex = Array.IndexOf(game.enemyOneFrames, reaction);
            Check(poseIndex >= 8 && poseIndex < 12, "The enemy must react to scanner confirmation using its original pose.");
            foreach (float time in new[] { 24.5f, 28.35f, 50, 54, 57.5f, 58.4f })
            {
                Seek(game, time);
                TextFits(game);
                ScreenCapture.CaptureScreenshot(Evidence + "/v3-" + time.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + ".png");
                yield return null;
            }
            Click(Field<Button>(game, "openingSkip"));
            yield return MapReady(game);
            Record("PASS: V3 measured journey, tracking, connected MAP 01 destination, distance-driven steps, head rig, energy rise, and threat reactions");
        }
    }
}
