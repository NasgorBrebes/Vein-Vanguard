using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VeinVanguard
{
    public partial class BattleManager
    {
        const float LoadingFadeIn = .18f;
        const float LoadingPrepareAt = .58f;
        const float LoadingReadyHold = .10f;
        const float LoadingFadeOut = .22f;

        CanvasGroup stageLoading;
        RectTransform loadingSafe, loadingRing, loadingProgress;
        TextMeshProUGUI loadingStatus, loadingDestination;
        Action prepareStage;
        float loadingElapsed;
        bool loadingReducedMotion;
        bool IsStageLoading => stageLoading && stageLoading.blocksRaycasts;

        void StartGameFromUi()
        {
            if (OpeningPlaying || IsStageLoading || PortalSpawnPlaying) return;
            if (View == GameView.Title)
                BeginOpeningCinematic();
            else if (View == GameView.Battle && model.phase == BattlePhase.Defeat)
                ShowStageLoading("MAP 01", () => { StartGame(); BeginPortalSpawn(); });
        }

        void ShowStageLoading(string destination, Action prepare)
        {
            if (IsStageLoading) return;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            if (joystick) joystick.ResetInput();
            heldDirection = Vector2.zero;
            hasTarget = false;
            prepareStage = prepare;
            loadingElapsed = 0;
            loadingReducedMotion = ReducedArenaMotion;
            loadingDestination.text = destination;
            loadingStatus.text = "MEMUAT AREA";
            loadingProgress.anchorMax = new Vector2(.34f, 1);
            loadingRing.localRotation = Quaternion.identity;
            stageLoading.alpha = 0;
            UpdateLoadingSafeArea();
            stageLoading.blocksRaycasts = true;
        }

        void TickStageLoading()
        {
            UpdateLoadingSafeArea();
            loadingElapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (!loadingReducedMotion)
                loadingRing.localRotation = Quaternion.Euler(0, 0, -loadingElapsed * 135);

            if (prepareStage != null)
            {
                stageLoading.alpha = Mathf.Clamp01(loadingElapsed / LoadingFadeIn);
                if (loadingElapsed < LoadingPrepareAt) return;

                var prepare = prepareStage;
                prepareStage = null;
                try { prepare(); }
                catch { CancelStageLoading(); throw; }
                loadingElapsed = LoadingPrepareAt;
                loadingStatus.text = "AREA SIAP";
                loadingProgress.anchorMax = Vector2.one;
                return;
            }

            float fade = (loadingElapsed - LoadingPrepareAt - LoadingReadyHold) / LoadingFadeOut;
            stageLoading.alpha = 1 - Mathf.Clamp01(fade);
            if (fade >= 1) CancelStageLoading();
        }

        void UpdateLoadingSafeArea()
        {
            Rect area = Screen.safeArea;
            var screenSize = new Vector2(Screen.width, Screen.height);
            Place(loadingSafe, area.min / screenSize, area.max / screenSize);
        }

        void CancelStageLoading()
        {
            spawnLastTick = Time.realtimeSinceStartupAsDouble;
            prepareStage = null;
            if (!stageLoading) return;
            stageLoading.alpha = 0;
            stageLoading.blocksRaycasts = false;
        }

        void OnDisable()
        {
            bool interruptedOpening = OpeningPlaying;
            EndEncounterTransition(false);
            CancelPortalSpawn();
            CancelStageLoading();
            CancelOpeningCinematic();
            if (interruptedOpening && titleRoot) ShowMenu();
        }

        void BuildStageLoading()
        {
            var paper = new Color32(245, 249, 252, 255);
            var textColor = new Color32(30, 56, 75, 255);
            var accent = new Color32(40, 108, 139, 255);
            var line = new Color32(190, 209, 220, 255);
            var root = Box("Stage Loading", flowSafe.parent, Vector2.zero, Vector2.one, paper);
            root.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            stageLoading = root.gameObject.AddComponent<CanvasGroup>();
            stageLoading.interactable = false;
            stageLoading.blocksRaycasts = false;
            stageLoading.alpha = 0;

            var left = Box("Left Facet", root, new Vector2(.10f, -1), new Vector2(.38f, 2), new Color32(232, 239, 245, 255));
            left.localRotation = Quaternion.Euler(0, 0, -32);
            var right = Box("Right Facet", root, new Vector2(.62f, -1), new Vector2(.88f, 2), Color.white);
            right.localRotation = Quaternion.Euler(0, 0, -32);

            loadingSafe = Box("Loading Safe Area", root, Vector2.zero, Vector2.one);
            Label("VEIN VANGUARD  /  NAVIGASI", loadingSafe, new Vector2(.067f, .86f),
                new Vector2(.62f, .92f), 19, textColor);
            Box("Header Rule", loadingSafe, new Vector2(.072f, .85f), new Vector2(.928f, .852f), line);

            loadingRing = Box("Loading Ring", loadingSafe, Vector2.one * .5f, Vector2.one * .5f);
            Fixed(loadingRing, Vector2.one * .5f, Vector2.one * .5f, new Vector2(0, 35), new Vector2(320, 320));
            var ring = loadingRing.gameObject.AddComponent<StageLoadingRing>();
            ring.raycastTarget = false;
            ring.color = accent;

            loadingStatus = Label("", loadingSafe, new Vector2(.26f, .25f), new Vector2(.74f, .34f),
                31, textColor, TextAlignmentOptions.Center);
            loadingDestination = Label("", loadingSafe, new Vector2(.067f, .095f), new Vector2(.31f, .145f), 20, textColor);
            var progressTrack = Box("Loading Track", loadingSafe, new Vector2(.072f, .091f), new Vector2(.32f, .096f), line);
            loadingProgress = Box("Loading Progress", progressTrack, Vector2.zero, Vector2.one, accent);
        }
    }
}
