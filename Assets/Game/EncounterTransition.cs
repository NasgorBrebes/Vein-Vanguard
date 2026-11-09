using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace VeinVanguard
{
    public partial class BattleManager
    {
        static readonly int MosaicGrid = Shader.PropertyToID("_Grid");
        Canvas encounterOverlay;
        RawImage encounterMosaic;
        Image encounterFlash;
        Camera encounterCamera;
        Canvas encounterCanvas;
        GraphicRaycaster encounterWorldInput, encounterBattleInput;
        bool encounterWorldInputEnabled, encounterBattleInputEnabled;
        RenderMode encounterOriginalMode;
        Camera encounterOriginalCamera;
        float encounterOriginalDistance;
        RenderTexture encounterFrame;
        Material encounterMaterial;
        Action encounterSwitch;
        float encounterElapsed, encounterCueEnd, encounterCut, encounterEnd, encounterPeak;
        double encounterLastTick;
        public bool EncounterTransitionPlaying { get; private set; }

        void StartEncounterTransition(int index)
        {
            if (!CanBeginEncounter(index)) return;
            int slot = StageIndex == 0 ? index : index - 4;
            BeginEncounterTransition(() => EnterEncounter(index), worldEnemies[slot]);
        }

        void StartReturnTransition()
        {
            if (View != GameView.Battle || (model.phase != BattlePhase.Victory && model.phase != BattlePhase.Complete)) return;
            BeginEncounterTransition(ReturnToWorld, null);
        }

        void BeginEncounterTransition(Action changeView, RectTransform target)
        {
            if (EncounterTransitionPlaying) return;
            if (!EnsureEncounterTransition())
            {
                changeView();
                return;
            }

            bool reduced = ReducedArenaMotion;
            bool entering = target != null;
            encounterElapsed = 0;
            encounterCueEnd = entering && !reduced ? .15f : 0;
            encounterCut = reduced ? .16f : entering ? .45f : .28f;
            encounterEnd = reduced ? .48f : entering ? 1f : .62f;
            encounterPeak = reduced ? 24 : 108;
            encounterSwitch = changeView;
            encounterLastTick = Time.realtimeSinceStartupAsDouble;
            EncounterTransitionPlaying = true;

            if (joystick) joystick.ResetInput();
            hasTarget = false;
            heldDirection = Vector2.zero;
            encounterWorldInput = flowSafe.GetComponentInParent<GraphicRaycaster>();
            encounterBattleInput = battleCanvas.GetComponent<GraphicRaycaster>();
            encounterWorldInputEnabled = encounterWorldInput && encounterWorldInput.enabled;
            encounterBattleInputEnabled = encounterBattleInput && encounterBattleInput.enabled;
            if (encounterWorldInput) encounterWorldInput.enabled = false;
            if (encounterBattleInput) encounterBattleInput.enabled = false;
            ClearEncounterFocus();
            encounterOverlay.gameObject.SetActive(true);
            if (!entering || reduced) return;

            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center));
            Vector2 anchor = new Vector2(point.x / Screen.width, point.y / Screen.height);
            Fixed(encounterFlash.rectTransform, anchor, Vector2.one * .5f, Vector2.zero, Vector2.one * 126);
            encounterFlash.rectTransform.localScale = Vector3.one;
            encounterFlash.color = new Color(.48f, .92f, 1, .28f);
            if (clickSound && effectSource) effectSource.PlayOneShot(clickSound, .55f);
        }

        bool EnsureEncounterTransition()
        {
            if (encounterOverlay) return encounterCamera && encounterMosaic;
            var shader = Resources.Load<Shader>("BattleArena/EncounterMosaic");
            if (!shader || !shader.isSupported) return false;

            var overlayObject = new GameObject("Encounter Transition", typeof(Canvas));
            overlayObject.transform.SetParent(transform, false);
            encounterOverlay = overlayObject.GetComponent<Canvas>();
            encounterOverlay.renderMode = RenderMode.ScreenSpaceOverlay;
            encounterOverlay.sortingOrder = short.MaxValue;
            encounterMosaic = Box("Pixel Mosaic", overlayObject.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
            encounterMosaic.raycastTarget = false;
            encounterMosaic.enabled = false;
            encounterMaterial = new Material(shader);
            encounterMosaic.material = encounterMaterial;
            encounterFlash = OpeningImage("Encounter Flash", overlayObject.transform, joystickDisc, Vector2.zero, Vector2.zero, Color.clear);
            encounterFlash.raycastTarget = false;
            overlayObject.SetActive(false);

            var cameraObject = new GameObject("Encounter Capture Camera", typeof(Camera), typeof(UniversalAdditionalCameraData));
            cameraObject.transform.SetParent(transform, false);
            // The distant camera sees its assigned Canvas without drawing any scene geometry.
            cameraObject.transform.position = new Vector3(0, 0, -10000);
            encounterCamera = cameraObject.GetComponent<Camera>();
            encounterCamera.enabled = false;
            encounterCamera.clearFlags = CameraClearFlags.SolidColor;
            encounterCamera.backgroundColor = OpeningInk;
            encounterCamera.orthographic = true;
            encounterCamera.farClipPlane = 20;
            encounterCamera.allowHDR = encounterCamera.allowMSAA = false;
            cameraObject.GetComponent<UniversalAdditionalCameraData>().renderShadows = false;
            return true;
        }

        void TickEncounterTransition()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            float delta = (float)(now - encounterLastTick);
            encounterLastTick = now;
            if (!encounterOverlay || !encounterCamera || !encounterMosaic)
            {
                EndEncounterTransition(true);
                return;
            }
            if (!openingFocused || openingPaused) return;

            encounterElapsed += Mathf.Clamp(delta, 0, .05f);
            if (encounterElapsed < encounterCueEnd)
            {
                float progress = encounterElapsed / encounterCueEnd;
                encounterFlash.color = new Color(.48f, .92f, 1, .28f * (1 - progress));
                encounterFlash.rectTransform.localScale = Vector3.one * (1 + .45f * progress);
                return;
            }

            encounterFlash.color = Color.clear;
            try
            {
                if (encounterElapsed >= encounterCut && encounterSwitch != null)
                {
                    RestoreEncounterCanvas();
                    var changeView = encounterSwitch;
                    encounterSwitch = null;
                    changeView();
                    ClearEncounterFocus();
                }
                if (encounterElapsed >= encounterEnd)
                {
                    EndEncounterTransition(false);
                    return;
                }

                if (!RenderEncounterView())
                {
                    EndEncounterTransition(true);
                    return;
                }
                float envelope = encounterElapsed < encounterCut
                    ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(encounterCueEnd, encounterCut, encounterElapsed))
                    : 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(encounterCut, encounterEnd, encounterElapsed));
                float block = Mathf.Pow(encounterPeak, envelope * envelope) * Screen.height / 900f;
                block = Mathf.Max(1, block);
                encounterMaterial.SetVector(MosaicGrid, new Vector4(Screen.width / block, Screen.height / block, 0, 0));
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EndEncounterTransition(true);
            }
        }

        bool RenderEncounterView()
        {
            if (!encounterFrame || !encounterFrame.IsCreated() || encounterFrame.width != Screen.width || encounterFrame.height != Screen.height)
            {
                ReleaseEncounterFrame();
                encounterFrame = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
                if (!encounterFrame || (!encounterFrame.IsCreated() && !encounterFrame.Create())) return false;
                encounterFrame.filterMode = FilterMode.Point;
                encounterFrame.wrapMode = TextureWrapMode.Clamp;
                encounterCamera.targetTexture = encounterFrame;
                encounterMosaic.texture = encounterFrame;
                encounterCamera.enabled = true;
                encounterMosaic.enabled = true;
            }
            if (!encounterCanvas)
            {
                var canvas = View == GameView.Battle ? battleCanvas.GetComponent<Canvas>() : worldRoot.GetComponentInParent<Canvas>();
                if (!canvas) return false;
                encounterCanvas = canvas.rootCanvas;
                encounterOriginalMode = encounterCanvas.renderMode;
                encounterOriginalCamera = encounterCanvas.worldCamera;
                encounterOriginalDistance = encounterCanvas.planeDistance;
                encounterCamera.cullingMask = 1 << encounterCanvas.gameObject.layer;
                encounterCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                encounterCanvas.worldCamera = encounterCamera;
                encounterCanvas.planeDistance = 5;
                Canvas.ForceUpdateCanvases();
            }
            return true;
        }

        void RestoreEncounterCanvas()
        {
            if (!encounterCanvas) return;
            encounterCanvas.renderMode = encounterOriginalMode;
            encounterCanvas.worldCamera = encounterOriginalCamera;
            encounterCanvas.planeDistance = encounterOriginalDistance;
            encounterCanvas = null;
        }

        void ReleaseEncounterFrame()
        {
            if (encounterMosaic) encounterMosaic.texture = null;
            if (encounterCamera) encounterCamera.targetTexture = null;
            if (encounterFrame) RenderTexture.ReleaseTemporary(encounterFrame);
            encounterFrame = null;
        }

        void EndEncounterTransition(bool finishPending)
        {
            if (!EncounterTransitionPlaying) return;
            EncounterTransitionPlaying = false;
            RestoreEncounterCanvas();
            if (encounterCamera) encounterCamera.enabled = false;
            if (encounterMosaic) encounterMosaic.enabled = false;
            if (encounterFlash) encounterFlash.color = Color.clear;
            if (encounterOverlay) encounterOverlay.gameObject.SetActive(false);
            ReleaseEncounterFrame();
            if (encounterWorldInput) encounterWorldInput.enabled = encounterWorldInputEnabled;
            if (encounterBattleInput) encounterBattleInput.enabled = encounterBattleInputEnabled;
            var changeView = encounterSwitch;
            encounterSwitch = null;
            if (finishPending) changeView?.Invoke();
            if (View == GameView.World) openingWaitForMovementRelease = OpeningMovementKeysHeld();
            ClearEncounterFocus();
        }

        static void ClearEncounterFocus()
        {
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
