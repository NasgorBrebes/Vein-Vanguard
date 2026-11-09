using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VeinVanguard
{
    public partial class BattleManager
    {
        const float SpawnDuration = 3.2f, SpawnReducedDuration = .65f;
        public bool PortalSpawnPlaying => spawnPlaying;
        bool spawnPlaying, spawnReduced, spawnCloseSound;
        float spawnElapsed;
        double spawnLastTick;
        Color spawnColor;
        RectTransform spawnPortal, spawnEnergy, spawnRing, spawnCounterRing;
        CanvasGroup spawnInputGate, spawnPortalGroup;
        CanvasGroup[] spawnHudGroups;
        float[] spawnHudAlpha;
        Image spawnCore, spawnGlow, spawnBodyEnergy;
        Image[] spawnParticles;

        void EnsurePortalSpawn()
        {
            if (!spawnInputGate)
            {
                var gate = Box("Portal Spawn Input Gate", flowSafe, Vector2.zero, Vector2.one, Color.clear);
                gate.GetComponent<Image>().raycastTarget = true;
                spawnInputGate = gate.gameObject.AddComponent<CanvasGroup>();
                spawnInputGate.blocksRaycasts = spawnInputGate.interactable = false;
                gate.gameObject.SetActive(false);
                var groups = new System.Collections.Generic.List<CanvasGroup>();
                for (int i = 0; i < worldRoot.childCount; i++)
                {
                    var child = worldRoot.GetChild(i);
                    if (child == worldContent) continue;
                    var group = child.GetComponent<CanvasGroup>();
                    if (!group) group = child.gameObject.AddComponent<CanvasGroup>();
                    groups.Add(group);
                }
                spawnHudGroups = groups.ToArray();
                spawnHudAlpha = new float[spawnHudGroups.Length];
            }
            if (spawnPortal) return;
            spawnPortal = Box("Mission Spawn Portal", worldContent, Vector2.zero, Vector2.zero);
            spawnPortal.pivot = Vector2.one * .5f;
            spawnPortal.sizeDelta = new Vector2(230, 270);
            spawnPortal.SetSiblingIndex(worldPlayer.GetSiblingIndex());
            spawnPortalGroup = spawnPortal.gameObject.AddComponent<CanvasGroup>();
            spawnPortalGroup.blocksRaycasts = spawnPortalGroup.interactable = false;
            spawnEnergy = Box("Opening Energy Surface", spawnPortal, Vector2.zero, Vector2.one);
            spawnGlow = OpeningImage("Portal Halo", spawnEnergy, joystickDisc, new Vector2(-.12f, -.1f), new Vector2(1.12f, 1.1f), Color.clear);
            spawnCore = OpeningImage("Plasma Aperture", spawnEnergy, joystickDisc, new Vector2(.12f, .12f), new Vector2(.88f, .88f), Color.clear);
            spawnRing = OpeningRing("Energy Rim", spawnEnergy, Vector2.zero, Vector2.one, Color.white);
            spawnCounterRing = OpeningRing("Counter Rotating Energy", spawnEnergy, new Vector2(.1f, .1f), new Vector2(.9f, .9f), Color.cyan);
            spawnParticles = new Image[12];
            for (int i = 0; i < spawnParticles.Length; i++)
            {
                var spark = OpeningImage("Spawn Spark " + i, spawnPortal, joystickDisc, Vector2.one * .5f, Vector2.one * .5f, Color.white);
                spark.rectTransform.sizeDelta = Vector2.one * (i % 3 == 0 ? 8 : 5);
                spawnParticles[i] = spark;
            }
            if (!spawnBodyEnergy)
                spawnBodyEnergy = OpeningImage("Teleport Body Energy", worldPlayer, null, Vector2.zero, Vector2.one, Color.clear);
            foreach (var graphic in spawnPortal.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            spawnBodyEnergy.raycastTarget = false;
            spawnBodyEnergy.gameObject.SetActive(false);
            spawnPortal.gameObject.SetActive(false);
        }

        void BeginPortalSpawn()
        {
            if (spawnPlaying || View != GameView.World || !worldContent || !worldPlayer || !worldPlayerImage) return;
            try
            {
                EnsurePortalSpawn();
                spawnPlaying = true;
                spawnReduced = ReducedArenaMotion;
                spawnElapsed = 0;
                spawnLastTick = Time.realtimeSinceStartupAsDouble;
                spawnCloseSound = false;
                spawnColor = StageIndex == 0 ? new Color(.08f, .82f, 1) : new Color(.61f, .30f, 1);
                hasTarget = false; heldDirection = Vector2.zero; walking = false;
                if (joystick) joystick.ResetInput();
                if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                for (int i = 0; i < spawnHudGroups.Length; i++)
                    if (spawnHudGroups[i]) { spawnHudAlpha[i] = spawnHudGroups[i].alpha; spawnHudGroups[i].alpha = 0; }
                spawnInputGate.gameObject.SetActive(true);
                spawnInputGate.blocksRaycasts = true;
                spawnPortal.gameObject.SetActive(true);
                spawnBodyEnergy.gameObject.SetActive(true);
                RenderPortalSpawn();
            }
            catch (Exception error) { Debug.LogException(error); CancelPortalSpawn(); }
        }

        void TickPortalSpawn()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            float delta = Mathf.Max(0, (float)(now - spawnLastTick));
            spawnLastTick = now;
            if (!openingFocused || openingPaused) return;
            if (!spawnPortal || !spawnPortalGroup || !spawnBodyEnergy || !spawnInputGate || !worldPlayerImage || View != GameView.World)
            { CancelPortalSpawn(); return; }
            try
            {
                // Start SFX after the opening/loading relinquishes its audio and screen gate.
                if (spawnElapsed == 0 && effectSource && chargeSound) effectSource.PlayOneShot(chargeSound, .3f);
                spawnElapsed += delta;
                if (spawnElapsed >= (spawnReduced ? SpawnReducedDuration : SpawnDuration)) { CancelPortalSpawn(); return; }
                if (!spawnReduced && !spawnCloseSound && spawnElapsed >= 2.3f)
                {
                    spawnCloseSound = true;
                    if (effectSource && defendSound) effectSource.PlayOneShot(defendSound, .22f);
                }
                RenderPortalSpawn();
            }
            catch (Exception error) { Debug.LogException(error); CancelPortalSpawn(); }
        }

        void RenderPortalSpawn()
        {
            float t = spawnElapsed;
            Rect area = Screen.safeArea;
            flowSafe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            flowSafe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            Canvas.ForceUpdateCanvases();
            float reveal = Mathf.SmoothStep(0, 1, spawnReduced ? t / .45f : (t - .75f) / .85f);
            float close = spawnReduced ? 1 - Mathf.SmoothStep(0, 1, t / SpawnReducedDuration) :
                1 - Mathf.SmoothStep(0, 1, (t - 2.25f) / .85f);
            float open = spawnReduced ? 1 : Mathf.SmoothStep(0, 1, (t - .12f) / .6f);
            float orbit = spawnReduced ? 0 : t;
            spawnPortal.anchoredPosition = MapPoint(worldPosition) + new Vector2(-28, 106);
            spawnEnergy.localScale = spawnReduced ? Vector3.one :
                new Vector3(open * close * (1 + Mathf.Sin(orbit * 9) * .025f), open * close, 1);
            spawnPortalGroup.alpha = spawnReduced ? close * .4f : 1;
            spawnRing.localRotation = Quaternion.Euler(0, 0, orbit * 85);
            spawnCounterRing.localRotation = Quaternion.Euler(0, 0, -orbit * 115);
            spawnRing.GetComponent<StageLoadingRing>().color = spawnColor;
            spawnCounterRing.GetComponent<StageLoadingRing>().color = new Color(.2f, .92f, 1, .7f);
            float flash = spawnReduced ? 0 : Mathf.Sin(Mathf.Clamp01((t - .75f) / .7f) * Mathf.PI);
            spawnGlow.color = new Color(spawnColor.r, spawnColor.g, spawnColor.b, .12f + flash * .16f);
            spawnCore.color = new Color(spawnColor.r * .35f, spawnColor.g * .35f, spawnColor.b * .45f, .75f + flash * .2f);
            for (int i = 0; i < spawnParticles.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / spawnParticles.Length + orbit * (i % 2 == 0 ? 1.4f : -1.1f);
                float radius = spawnReduced ? 95 : Mathf.Lerp(180, 103, Mathf.SmoothStep(0, 1, t / .7f));
                var spark = spawnParticles[i];
                spark.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 1.13f);
                spark.color = new Color(.65f, .95f, 1, spawnReduced ? 0 : close * (.55f + .35f * Mathf.Sin(t * 5 + i)));
            }
            bool stepping = !spawnReduced && reveal > 0 && reveal < 1;
            worldPlayerImage.sprite = walkFrames[stepping ? 8 + Mathf.Min(7, (int)(reveal * 8)) : 64];
            var pose = worldPlayerImage.sprite;
            worldPlayer.pivot = stepping ? new Vector2(pose.pivot.x / pose.rect.width, 0) : new Vector2(.5f, 0);
            worldPlayer.sizeDelta = stepping ? new Vector2(110 * pose.rect.width / pose.rect.height, 110) : new Vector2(130, 150);
            worldPlayer.anchoredPosition = MapPoint(worldPosition) + (spawnReduced ? Vector2.zero : Vector2.Lerp(new Vector2(-38, 16), Vector2.zero, reveal));
            float landing = !spawnReduced && t > 1.5f && t < 1.85f ? Mathf.Sin((t - 1.5f) / .35f * Mathf.PI) * .06f : 0;
            worldPlayer.localScale = new Vector3(1 + landing, 1 - landing, 1);
            worldPlayerImage.color = new Color(1, 1, 1, reveal);
            spawnBodyEnergy.sprite = pose;
            spawnBodyEnergy.color = new Color(.28f, .9f, 1, flash * .7f);
            float zoom = spawnReduced ? 1 : Mathf.Lerp(2.2f, 1, Mathf.SmoothStep(0, 1, (t - 1.1f) / 1.9f));
            PositionWorldCamera(zoom, new Vector2(-28, 118) * Mathf.InverseLerp(1, 2.2f, zoom));
        }

        void CancelPortalSpawn()
        {
            bool active = spawnPlaying;
            spawnPlaying = false;
            if (spawnPortal) spawnPortal.gameObject.SetActive(false);
            if (spawnBodyEnergy) spawnBodyEnergy.gameObject.SetActive(false);
            if (spawnInputGate) { spawnInputGate.blocksRaycasts = false; spawnInputGate.gameObject.SetActive(false); }
            if (!active) return;
            if (effectSource) effectSource.Stop();
            for (int i = 0; i < spawnHudGroups.Length; i++)
                if (spawnHudGroups[i]) spawnHudGroups[i].alpha = spawnHudAlpha[i];
            if (worldPlayerImage) worldPlayerImage.color = Color.white;
            hasTarget = false; heldDirection = Vector2.zero; walking = false; walkTime = idleTime = 0;
            if (joystick) joystick.ResetInput();
            openingWaitForMovementRelease = OpeningMovementKeysHeld();
            if (worldPlayer && worldContent) PositionWorld();
        }
    }
}
