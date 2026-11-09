using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VeinVanguard.Editor
{
    public static partial class UiRefactorChecks
    {
        const string EncounterEvidence = "Temp/EncounterTransition";
        const string EncounterResults = EncounterEvidence + "/Results.txt";

        [MenuItem("Tools/Vein Vanguard/Encounter Transition/Run Acceptance")]
        public static void RunEncounterAcceptance()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Exit Play Mode before acceptance.");
            Directory.CreateDirectory(EncounterEvidence);
            File.WriteAllText(EncounterResults, "Encounter acceptance / " + DateTime.Now.ToString("s") + "\n");
            SelectResolution(1280, 720);
            SessionState.SetBool("VV.Encounter.QA", true);
            EditorApplication.isPlaying = true;
        }

        static void TickEncounterChecks()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= TickEncounterChecks; steps.Clear(); return; }
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            try
            {
                while (steps.Count != 0)
                {
                    var step = steps.Peek();
                    if (!step.MoveNext()) { (steps.Pop() as IDisposable)?.Dispose(); continue; }
                    if (step.Current is IEnumerator nested) { steps.Push(nested); continue; }
                    return;
                }
                EncounterRecord("ENCOUNTER ACCEPTANCE COMPLETE");
            }
            catch (Exception error)
            {
                EncounterRecord("FAIL: " + error);
                Debug.LogException(error);
            }
            while (steps.Count != 0) (steps.Pop() as IDisposable)?.Dispose();
            EditorApplication.update -= TickEncounterChecks;
            EditorApplication.isPlaying = false;
        }

        static void EncounterRecord(string result) => File.AppendAllText(EncounterResults, result + "\n");

        static IEnumerator WaitForEncounterTransition(BattleManager game)
        {
            double end = Time.realtimeSinceStartupAsDouble + 3;
            while (game.EncounterTransitionPlaying && Time.realtimeSinceStartupAsDouble < end) yield return null;
            Check(!game.EncounterTransitionPlaying, "Encounter transition did not restore controls.");
            yield return null;
        }

        static void ReachEncounter(BattleManager game, int id)
        {
            int slot = game.StageIndex == 0 ? id : id - 4;
            typeof(BattleManager).GetField("worldPosition", Private).SetValue(game, Field<Vector2[]>(game, "enemyPositions")[slot]);
            Invoke(game, "UpdateFlow");
            Check(game.EncounterTransitionPlaying && game.View == GameView.World, "Proximity must hold map before the mosaic cut.");
        }

        static void EncounterCapture(string name) => ScreenCapture.CaptureScreenshot(EncounterEvidence + "/" + name + ".png");

        static IEnumerator VerifyEncounterTransitions()
        {
            yield return Wait(.2f);
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            const string motion = "VV.Motion.Reduced";
            bool savedKey = PlayerPrefs.HasKey(motion);
            int saved = PlayerPrefs.GetInt(motion);
            try
            {
                PlayerPrefs.SetInt(motion, 0);
                for (int id = 0; id < 6; id++)
                {
                    if (id == 4) { SelectResolution(2000, 900); yield return Wait(.2f); }
                    game.StartGame();
                    if (id >= 4)
                    {
                        Array.Fill(game.defeated, true, 0, 4);
                        Check(game.TryAdvanceStage(), "Stage 2 fixture failed.");
                        if (id == 5) game.defeated[4] = true;
                    }
                    game.model.hp = 73; game.model.energy = 27;
                    ReachEncounter(game, id);
                    double start = Time.realtimeSinceStartupAsDouble;
                    var position = game.WorldPosition;
                    var map = Field<RectTransform>(game, "worldContent");
                    Vector2 pan = map.anchoredPosition;
                    game.MoveWorld(Vector2.right * 100); game.BeginEncounter((id + 1) % 6); game.OpenSettings();
                    Check(game.WorldPosition == position && !game.SettingsOpen && !game.CanBeginEncounter(id), "Transition input or duplicate encounter leaked.");
                    Canvas.ForceUpdateCanvases();
                    var pointer = new PointerEventData(EventSystem.current) { position = new Vector2(Screen.width, Screen.height) * .5f };
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointer, hits);
                    Check(hits.Count == 0, "Map pointer targets must be unavailable during transition.");

                    bool inFrame = false, outFrame = false;
                    while (game.EncounterTransitionPlaying && Time.realtimeSinceStartupAsDouble - start < 3)
                    {
                        float t = Field<float>(game, "encounterElapsed");
                        if (game.View == GameView.World)
                        {
                            Check(game.WorldPosition == position && Vector2.Distance(map.anchoredPosition, pan) < .01f, "Map framing drifted during transition.");
                            if (!inFrame && t >= .36f) { EncounterCapture("encounter-" + id + "-map-mosaic"); inFrame = true; }
                        }
                        else
                        {
                            Check(game.model.encounter == id, "Battle view received wrong encounter.");
                            game.Diagnose();
                            Check(!game.model.diagnosed && !Field<bool>(game, "actionAnimating"), "Battle action started before reveal finished.");
                            if (!outFrame)
                            {
                                hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
                                Check(hits.Count == 0, "Battle pointer targets must stay unavailable until reveal.");
                                EncounterCapture("encounter-" + id + "-arena-mosaic"); outFrame = true;
                            }
                        }
                        yield return null;
                    }
                    Check(inFrame && outFrame && !game.EncounterTransitionPlaying && game.View == GameView.Battle,
                        $"Both mosaic halves must complete. Map={inFrame} Battle={outFrame} t={Field<float>(game, "encounterElapsed"):F3} View={game.View}");
                    Check(Field<float>(game, "encounterElapsed") >= 1 && Field<float>(game, "encounterElapsed") < 1.06f, "Entry timeline outside 1 second.");
                    Check(game.model.hp == 73 && game.model.energy == 27, "Transition altered resources.");
                    Check(!Field<RenderTexture>(game, "encounterFrame") && !Field<Camera>(game, "encounterCamera").enabled, "Render resources remained active.");
                    Check(Field<GameObject>(game, "battleCanvas").GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay, "Battle Canvas mode was not restored.");
                    Check(Field<CanvasGroup>(game, "radialGroup").blocksRaycasts, "Radial menu must be ready when the reveal ends.");
                    EncounterCapture("encounter-" + id + "-arena-sharp");
                    yield return null;
                    Click(Field<Button>(game, "diagnose")); Check(Field<bool>(game, "actionAnimating"), "Battle controls did not unlock.");
                    yield return Wait(.65f);

                    game.model.enemyHp = 0; game.model.phase = BattlePhase.Victory;
                    Invoke(game, "Victory");
                    yield return Wait();
                    Check(game.View == GameView.Battle && !game.EncounterTransitionPlaying, "Victory must remain readable until Continue.");
                    Click(Field<RectTransform>(game, "options"), "KEMBALI KE PETA");
                    Check(game.EncounterTransitionPlaying, "Victory Continue did not start return mosaic.");
                    double returnDeadline = Time.realtimeSinceStartupAsDouble + 3;
                    while (game.View == GameView.Battle && Time.realtimeSinceStartupAsDouble < returnDeadline) yield return null;
                    Check(game.View == GameView.World && game.EncounterTransitionPlaying, "Map must appear before return mosaic ends.");
                    EncounterCapture("encounter-" + id + "-return-mosaic");
                    yield return WaitForEncounterTransition(game);
                    Check(game.defeated[id] && !Field<RectTransform[]>(game, "worldEnemies")[id < 4 ? id : id - 4].gameObject.activeSelf, "Defeated enemy reappeared after return.");
                    Check(game.WorldPosition == position && !game.PortalSpawnPlaying && !Field<RenderTexture>(game, "encounterFrame"), "Return must restore position and release texture without replaying portal.");
                    EncounterCapture("encounter-" + id + "-return-sharp");
                    yield return null;
                    EncounterRecord("PASS: proximity, mosaic entry, input lock, victory/return, exact position and defeated marker for " + id);
                }

                PlayerPrefs.SetInt(motion, 1);
                game.StartGame(); ReachEncounter(game, 0);
                Check(Field<float>(game, "encounterCueEnd") == 0 && Field<float>(game, "encounterEnd") < .5f, "Reduced motion must skip flash and shorten transition.");
                yield return WaitForEncounterTransition(game);
                Check(game.View == GameView.Battle && Field<CanvasGroup>(game, "radialGroup").blocksRaycasts, "Reduced motion failed to unlock arena.");
                PlayerPrefs.SetInt(motion, 0);
                game.StartGame(); ReachEncounter(game, 0);
                yield return Wait(.23f);
                game.ShowMenu();
                Check(!game.EncounterTransitionPlaying && game.View == GameView.Title && !Field<RenderTexture>(game, "encounterFrame"), "Menu cancellation leaked renderer state.");
                game.StartGame(); ReachEncounter(game, 0);
                UnityEngine.Object.Destroy(Field<Camera>(game, "encounterCamera"));
                yield return WaitForEncounterTransition(game);
                Check(game.View == GameView.Battle && !Field<RenderTexture>(game, "encounterFrame"), "Missing camera stranded encounter.");
                EncounterRecord("PASS: reduced motion, menu cancellation, missing-camera fallback, and resource cleanup");
            }
            finally
            {
                if (savedKey) PlayerPrefs.SetInt(motion, saved); else PlayerPrefs.DeleteKey(motion);
            }
        }
    }
}
