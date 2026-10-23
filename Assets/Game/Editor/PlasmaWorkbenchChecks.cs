using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VeinVanguard.Editor
{
    public static class PlasmaWorkbenchChecks
    {
        public static string VerifyActionSeparation()
        {
            Check(Application.isPlaying && Time.unscaledDeltaTime > 0, "Run after one Play Mode frame.");
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            Check(game, "The scene needs a BattleManager.");
            game.StartGame(); game.BeginEncounter(0);
            var actors = new[] { Field<UnityEngine.UI.Image>(game, "playerArt").rectTransform,
                Field<UnityEngine.UI.Image>(game, "enemyArt").rectTransform };
            var originalLayout = actors.Select(actor => new[] { actor.anchorMin, actor.anchorMax, actor.offsetMin, actor.offsetMax }).ToArray();
            game.Diagnose();
            CompleteAnimation(game, "ScanAllLearningSamples");
            var root = Field<RectTransform>(game, "learningRoot");
            Check(!root || !root.gameObject.activeInHierarchy,
                "Diagnose must show only scan results; formula icons, sockets, charge, and Fire belong to Synthesize.");
            var diagnosis = Field<RectTransform>(game, "diagnosisRoot");
            Check(diagnosis && diagnosis.gameObject.activeInHierarchy &&
                diagnosis.GetComponentsInChildren<FormulaIconDrag>().Length == 0 &&
                diagnosis.GetComponentsInChildren<FormulaSocketDrop>().Length == 0 &&
                diagnosis.GetComponentsInChildren<UnityEngine.UI.Button>().All(b => b.name != "Fire Plasma"),
                "Diagnosis must contain scan results without active formula assembly or Fire controls.");
            Check(game.model.diagnosed && game.model.WasScanned(0) && game.model.WasScanned(1) &&
                game.model.energy == 60 && game.model.hp == 100 && game.model.enemyHp == game.model.MaxEnemyHp &&
                !Field<bool>(game, "formulaCharged") && Field<Coroutine>(game, "formulaChargeRoutine") == null,
                "Diagnosis must reveal both sources without charging, spending MP, or damaging the enemy.");
            CheckDockAndActors(diagnosis, actors);
            Invoke(game, "CloseLearningWorkbench");
            CheckActorLayout(actors, originalLayout);
            game.Synthesize();
            root = Field<RectTransform>(game, "learningRoot");
            Check(root && root.gameObject.activeInHierarchy, "Synthesize must open its own formula workbench.");
            Check(!diagnosis.gameObject.activeInHierarchy, "Opening synthesis must hide the diagnosis results dock.");
            CheckDockAndActors(root, actors);
            Check(root.GetComponentsInChildren<UnityEngine.UI.Button>().All(b => b.name != "Scan All Samples"),
                "The synthesis dock must not show a scan control.");
            game.InstallFormulaIcon(FormulaIcon.ReduceAdded, 0); game.InstallFormulaIcon(FormulaIcon.ReduceAdded, 1);
            CompleteAnimation(game, "ChargeFormula");
            Invoke(game, "CloseLearningWorkbench");
            CheckActorLayout(actors, originalLayout);
            game.Diagnose();
            Check(diagnosis.gameObject.activeInHierarchy && !root.gameObject.activeInHierarchy &&
                !Field<bool>(game, "formulaCharged") && Field<Coroutine>(game, "formulaChargeRoutine") == null,
                "Reopening diagnosis with a stored formula must show only results and must not charge it.");
            Invoke(game, "CloseLearningWorkbench");
            CheckActorLayout(actors, originalLayout); game.ShowMenu();
            return "PASS: separate scan/results and synthesis docks, unchanged diagnosis resources, visible actors, and restored actor layout";
        }

        public static string VerifyEntry()
        {
            Check(Application.isPlaying, "Run in Play Mode.");
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            Check(game, "The scene needs a BattleManager.");
            game.StartGame(); game.BeginEncounter(0); game.Diagnose();
            var root = Field<RectTransform>(game, "diagnosisRoot");
            var synthesis = Field<RectTransform>(game, "learningRoot");
            Check(root && root.gameObject.activeInHierarchy && (!synthesis || !synthesis.gameObject.activeInHierarchy),
                "Diagnose must open only the scan/results dock.");
            Check(!game.model.diagnosed && !game.model.WasScanned(0) && !game.model.WasScanned(1),
                "The source scan must finish before it unlocks synthesis.");
            return "PASS: Diagnose opens only scan/results and waits for both source records";
        }

        public static string VerifyInteractions()
        {
            Check(Application.isPlaying && Time.unscaledDeltaTime > 0, "Run after one Play Mode frame.");
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            foreach (int encounter in new[] { 0, 1 })
            {
                game.StartGame(); game.BeginEncounter(encounter); game.Diagnose();
                var diagnosis = Field<RectTransform>(game, "diagnosisRoot");
                var root = Field<RectTransform>(game, "learningRoot");
                Check(!game.model.diagnosed && diagnosis.gameObject.activeInHierarchy &&
                    (!root || !root.gameObject.activeInHierarchy), "Unscanned sources opened formula assembly.");
                CompleteAnimation(game, "ScanAllLearningSamples");
                Check(game.model.diagnosed && game.model.WasScanned(0) && game.model.WasScanned(1),
                    "A single scan must reveal both sources and complete diagnosis.");
                for (int source = 0; source < 2; source++)
                    Check(Field<TMPro.TextMeshProUGUI[]>(game, "formulaSourceLabels")[source].text.Contains(CombatSamples.Get(encounter, source).record),
                        "Scanned source labels must show their complete records before firing.");
                Invoke(game, "CloseLearningWorkbench"); game.Synthesize();
                root = Field<RectTransform>(game, "learningRoot");
                var fire = Button(root, "Fire Plasma");
                Check(root.gameObject.activeInHierarchy && !diagnosis.gameObject.activeInHierarchy && !fire.interactable,
                    "Synthesize must open its own uncharged formula dock after diagnosis closes.");
                var sources = root.GetComponentsInChildren<FormulaIconDrag>(true);
                var sockets = root.GetComponentsInChildren<FormulaSocketDrop>(true).OrderBy(s => s.socket).ToArray();
                Check(sources.Length == 6 && sources.Select(s => s.icon).Distinct().Count() == 6,
                    "The palette must expose all six formula icons.");
                Check(sockets.Length == 2 && sockets[0].socket == 0 && sockets[1].socket == 1 &&
                    sources.All(s => s.owner == game) && sockets.All(s => s.owner == game),
                    "Sources and the two sockets must belong to this workbench.");
                Check(Slots(game).All(s => s == FormulaIcon.None), "A new encounter retained a previous formula.");

                var reduce = sources.Single(s => s.icon == FormulaIcon.ReduceAdded);
                var water = sources.Single(s => s.icon == FormulaIcon.Water);
                reduce.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                sockets[0].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Check(Slots(game)[0] == FormulaIcon.ReduceAdded && Slots(game)[1] == FormulaIcon.None && !fire.interactable,
                    "Tap selection must install the selected icon only in the clicked socket.");

                var pointer = Pointer(reduce, sockets[1]);
                ExecuteEvents.Execute(reduce.gameObject, pointer, ExecuteEvents.beginDragHandler);
                var ghost = root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Formula Drag Ghost");
                Check(ghost && ghost.gameObject.activeInHierarchy &&
                    ghost.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g => !g.raycastTarget),
                    "Dragging needs a visible ghost that cannot block a socket drop.");
                ExecuteEvents.Execute(water.gameObject, Pointer(water, sockets[0]), ExecuteEvents.beginDragHandler);
                water.gameObject.SetActive(false);
                Check(ghost.gameObject.activeInHierarchy, "Disabling a source whose drag was rejected cancelled another source's drag.");
                water.gameObject.SetActive(true);
                ExecuteEvents.Execute(reduce.gameObject, pointer, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(sockets[1].gameObject, pointer, ExecuteEvents.dropHandler);
                ExecuteEvents.Execute(reduce.gameObject, pointer, ExecuteEvents.endDragHandler);
                Check(Slots(game)[0] == FormulaIcon.ReduceAdded && Slots(game)[1] == FormulaIcon.ReduceAdded,
                    "Dragging must allow the same icon in both sockets.");
                Check(!ghost.gameObject.activeInHierarchy && !fire.interactable && !Field<bool>(game, "formulaCharged"),
                    "Ending the drag must hide its ghost; two icons still need to charge.");
                fire.onClick.Invoke();
                Check(game.model.energy == 60 && game.model.enemyHp == game.model.MaxEnemyHp && root.gameObject.activeInHierarchy,
                    "Fire accepted an assembly before its charge finished.");

                CompleteAnimation(game, "ChargeFormula");
                Check(Field<bool>(game, "formulaCharged") && fire.interactable, "Every complete formula must charge and enable fire.");
                VerifyRejectedDrops(game, sockets[0], reduce, water);
                Drag(water, sockets[0]);
                Check(Slots(game)[0] == FormulaIcon.Water && Slots(game)[1] == FormulaIcon.ReduceAdded &&
                    !Field<bool>(game, "formulaCharged") && !fire.interactable,
                    "Replacing one socket must preserve the other and restart charging.");
                sockets[0].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Check(Slots(game)[0] == FormulaIcon.None && Field<string>(game, "playerPose") == "idle",
                    "Clearing a socket during charging must restore the player's idle pose.");
                water.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                sockets[0].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();

                Invoke(game, "CloseLearningWorkbench");
                Check(!root.gameObject.activeInHierarchy && !Field<bool>(game, "formulaCharged") &&
                    Field<Coroutine>(game, "formulaChargeRoutine") == null,
                    "Closing the workbench must cancel charging.");
                game.Synthesize();
                Check(root.gameObject.activeInHierarchy && !fire.interactable &&
                    Field<Coroutine>(game, "formulaChargeRoutine") != null,
                    "Reopening a filled formula must start a fresh charge.");
                CompleteAnimation(game, "ChargeFormula");
                fire.onClick.Invoke(); game.FireLearningSynthesis();
                Check(!root.gameObject.activeInHierarchy && game.model.energy == 60 && game.model.enemyHp == game.model.MaxEnemyHp,
                    "Fire must close the workbench and defer MP cost and damage until impact.");
                game.ShowMenu();
                Check(game.View == GameView.Title && !root.gameObject.activeInHierarchy &&
                    Field<Coroutine>(game, "formulaChargeRoutine") == null,
                    "Returning to the menu must stop workbench interactions and charging.");
            }
            game.StartGame(); game.BeginEncounter(0); game.Diagnose();
            Check(Slots(game).All(s => s == FormulaIcon.None) && !Field<bool>(game, "formulaCharged"),
                "Restart must clear all socket and charge state.");
            game.ShowMenu();
            return "PASS: both encounters, automatic scan, tap and drag/drop, duplicate icons, null/foreign input, charge replacement/close/reopen, fire timing, menu/restart";
        }

        static void VerifyRejectedDrops(BattleManager game, FormulaSocketDrop socket, FormulaIconDrag source, FormulaIconDrag idle)
        {
            var before = Slots(game).ToArray();
            socket.OnDrop(null);
            socket.OnDrop(new PointerEventData(EventSystem.current));
            socket.OnDrop(new PointerEventData(EventSystem.current) { pointerDrag = game.gameObject });
            var foreign = new GameObject("Foreign Formula Source");
            foreign.SetActive(false);
            try
            {
                var otherGame = foreign.AddComponent<BattleManager>();
                var drag = foreign.AddComponent<FormulaIconDrag>();
                drag.owner = otherGame; drag.icon = FormulaIcon.WholeFruit;
                socket.OnDrop(new PointerEventData(EventSystem.current) { pointerDrag = foreign });
                drag.owner = null;
                drag.OnBeginDrag(null); drag.OnDrag(null); drag.OnEndDrag(null);
                socket.OnDrop(new PointerEventData(EventSystem.current) { pointerDrag = foreign });
                Check(Slots(game).SequenceEqual(before) && Field<bool>(game, "formulaCharged"),
                    "Null or foreign drag input changed the formula or its charge.");
            }
            finally { UnityEngine.Object.DestroyImmediate(foreign); }

            var pointer = Pointer(source, socket);
            ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.beginDragHandler);
            idle.OnEndDrag(pointer);
            var ghost = Field<RectTransform>(game, "learningRoot").GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(t => t.name == "Formula Drag Ghost");
            Check(ghost && ghost.gameObject.activeInHierarchy, "Ending an idle source cancelled another source's drag.");
            source.gameObject.SetActive(false);
            source.gameObject.SetActive(true);
            Check(!ghost || !ghost.gameObject.activeInHierarchy, "Disabling a dragging source left its ghost active.");
            Check(Button(Field<RectTransform>(game, "learningRoot"), "Fire Plasma").interactable,
                "Cancelling a drag on a charged formula must restore the Fire button.");
        }

        public static string VerifyVisibleHitTargets()
        {
            Check(Application.isPlaying, "Run in Play Mode.");
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            var diagnosis = Field<RectTransform>(game, "diagnosisRoot");
            var root = diagnosis && diagnosis.gameObject.activeInHierarchy ? diagnosis : Field<RectTransform>(game, "learningRoot");
            Check(root && root.gameObject.activeInHierarchy, "Open either action dock and allow one rendered frame first.");
            UnityEngine.Canvas.ForceUpdateCanvases();
            var systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            Check(systems.Length == 1 && root.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>(),
                "Workbench controls need one EventSystem and an active canvas GraphicRaycaster.");
            foreach (var label in root.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
            {
                label.ForceMeshUpdate();
                Check(label.textBounds.size.x <= label.rectTransform.rect.width + .5f &&
                    label.textBounds.size.y <= label.rectTransform.rect.height + .5f,
                    "Action dock text extends outside its reading area: " + label.text);
            }
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                if (!button.interactable) continue;
                var rect = button.GetComponent<RectTransform>();
                var pointer = new PointerEventData(systems[0])
                { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
                var hits = new List<RaycastResult>();
                systems[0].RaycastAll(pointer, hits);
                Check(rect.rect.width > 0 && rect.rect.height > 0 && hits.Count > 0 &&
                    hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() == button,
                    "An interactive workbench control cannot receive a pointer hit: " + button.name);
            }
            CheckDockAndActors(root, new[] { Field<UnityEngine.UI.Image>(game, "playerArt").rectTransform,
                Field<UnityEngine.UI.Image>(game, "enemyArt").rectTransform });
            return "PASS: active action dock text, pointer targets, screen bounds, and unobscured battle actors";
        }

        static Rect ScreenBounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var bottom = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var top = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(bottom.x, bottom.y, top.x, top.y);
        }

        static void CheckDockAndActors(RectTransform root, RectTransform[] actors)
        {
            UnityEngine.Canvas.ForceUpdateCanvases();
            var dock = ScreenBounds(root);
            Check(dock.xMin >= -.5f && dock.xMax <= Screen.width + .5f && dock.yMin >= -.5f &&
                dock.yMax <= Screen.height * .4f && dock.height > 0 && dock.height < Screen.height * .36f,
                "The action dock must stay inside the bottom 40% of the screen and use less than 36% screen height.");
            foreach (var actor in actors)
            {
                var bounds = ScreenBounds(actor);
                Check(actor.gameObject.activeInHierarchy && bounds.yMin >= dock.yMax && bounds.yMax <= Screen.height + .5f,
                    "The action dock must leave the whole battle actor visible above it: " + actor.name);
            }
        }

        static void CheckActorLayout(RectTransform[] actors, Vector2[][] originalLayout)
        {
            for (int i = 0; i < actors.Length; i++)
                Check(actors[i].anchorMin == originalLayout[i][0] && actors[i].anchorMax == originalLayout[i][1] &&
                    actors[i].offsetMin == originalLayout[i][2] && actors[i].offsetMax == originalLayout[i][3],
                    "Closing either dock must restore the actor's original anchors and offsets: " + actors[i].name);
        }

        public static string VerifyFeedbackLayout()
        {
            Check(Application.isPlaying, "Run in Play Mode.");
            var game = UnityEngine.Object.FindFirstObjectByType<BattleManager>();
            foreach (int encounter in new[] { 0, 1 })
            {
                game.ShowMenu(); game.StartGame(); game.BeginEncounter(encounter);
                var first = encounter == 0 ? FormulaIcon.ReduceAdded : FormulaIcon.Water;
                var second = encounter == 0 ? FormulaIcon.UnsaturatedFat : FormulaIcon.WholeFruit;
                typeof(BattleManager).GetMethod("Dialog", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game,
                    new object[] { "HASIL SYNTHESIS", "<b>40 DAMAGE · -20 MP</b>\n" + UniversalFormula.Feedback(encounter, first, second),
                        new[] { "LANJUT · GILIRAN MUSUH" }, new UnityEngine.Events.UnityAction[] { () => { } } });
                UnityEngine.Canvas.ForceUpdateCanvases();
                var body = Field<TMPro.TextMeshProUGUI>(game, "modalBody");
                body.ForceMeshUpdate();
                Check(body.textBounds.size.x <= body.rectTransform.rect.width &&
                    body.textBounds.size.y <= body.rectTransform.rect.height,
                    "Synthesis feedback extends outside its reading area.");
            }
            game.ShowMenu();
            return "PASS: both source explanations fit the synthesis result panel";
        }

        static PointerEventData Pointer(FormulaIconDrag source, FormulaSocketDrop socket) =>
            new PointerEventData(EventSystem.current)
            {
                pointerDrag = source.gameObject,
                position = RectTransformUtility.WorldToScreenPoint(null, socket.transform.position)
            };
        static void Drag(FormulaIconDrag source, FormulaSocketDrop socket)
        {
            var pointer = Pointer(source, socket);
            ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(socket.gameObject, pointer, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(source.gameObject, pointer, ExecuteEvents.endDragHandler);
        }
        static UnityEngine.UI.Button Button(Transform root, string name) =>
            root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == name);
        static FormulaIcon[] Slots(BattleManager game) => Field<FormulaIcon[]>(game, "formulaSlots");
        static object Invoke(BattleManager game, string method) =>
            typeof(BattleManager).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
        static void CompleteAnimation(BattleManager game, string method)
        {
            game.StopAllCoroutines();
            var sequence = (IEnumerator)Invoke(game, method);
            int frames = 0;
            while (sequence.MoveNext())
                if (++frames > 10000) throw new Exception(method + " did not finish with a positive frame delta.");
        }

        static T Field<T>(BattleManager game, string name) =>
            (T)typeof(BattleManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}
