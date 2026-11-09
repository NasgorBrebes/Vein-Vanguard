using TMPro;
using UnityEngine;

namespace VeinVanguard
{
    public partial class BattleManager
    {
        static readonly Color OpeningCyan = new Color32(98, 225, 244, 255);
        static readonly Color OpeningAmber = new Color32(255, 190, 99, 255);
        static readonly Color OpeningInk = new Color32(10, 24, 40, 255);
        RectTransform openingSafe, openingEnvironment, openingCamera, openingThreatScan, openingMissionCards;
        UnityEngine.UI.Image openingBackdrop, openingCells, openingPlasma, openingShade, openingAlarm;
        UnityEngine.UI.Image openingUnit, openingPower, openingReactor, openingCorridorEnemy, openingRunner, openingRunnerShadow;
        UnityEngine.UI.Image openingDemoUnit, openingDemoTarget, openingDemoScan, openingDemoShield, openingDemoRestore;
        UnityEngine.UI.Image openingDemoProjectile, openingDemoImpact;
        RectTransform openingDemoMpFill;
        TextMeshProUGUI openingUnitStatus, openingThreatCategory, openingThreatWarning, openingAbilityTitle, openingAbilityDescription, openingDemoMpValue;
        UnityEngine.UI.Image[] openingThreats, openingEnergyParticles;
        RectTransform[] openingCards;
        GameObject[] openingSets;
        CanvasGroup openingStoryContent, openingEndTitle, openingGlitch, openingAlarmTitle, openingUnknownThreat, openingMpGroup;
        RectTransform openingEnergyRing, openingAlarmRing;
        UnityEngine.UI.Image openingUnknownImage;
        static readonly string[] OpeningCategories = { "PANGAN", "TEMBAKAU", "ALKOHOL" };
        static readonly string[] OpeningAbilities = { "DIAGNOSE", "SYNTHESIZE", "RESTORE", "HOMEOSTASIS" };
        static readonly string[] OpeningAbilityDescriptions = {
            "Pindai ancaman.", "Susun formula dan serang.", "Pulihkan energi melalui pengetahuan.", "Aktifkan perlindungan."
        };

        TextMeshProUGUI OpeningText(string name, string text, Transform parent, Vector2 min, Vector2 max, int size, Color tint,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var label = Label(text, parent, min, max, size, tint, alignment);
            label.name = name;
            label.fontSizeMin = size * .84f;
            return label;
        }

        UnityEngine.UI.Image OpeningImage(string name, Transform parent, Sprite sprite, Vector2 min, Vector2 max, Color tint)
        {
            var image = Box(name, parent, min, max, tint).GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            return image;
        }

        RectTransform OpeningRing(string name, Transform parent, Vector2 min, Vector2 max, Color tint)
        {
            var rect = Box(name, parent, min, max);
            var graphic = rect.gameObject.AddComponent<StageLoadingRing>();
            graphic.raycastTarget = false;
            graphic.color = tint;
            return rect;
        }

        void EnsureOpeningCinematic()
        {
            if (openingGroup) return;
            var root = Box("Opening Story", flowSafe.parent, Vector2.zero, Vector2.one, Color.clear);
            root.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            root.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            openingGroup = root.gameObject.AddComponent<CanvasGroup>();
            openingGroup.alpha = 0;
            openingGroup.blocksRaycasts = openingGroup.interactable = false;
            var content = Box("Opening Story Content", root, Vector2.zero, Vector2.one, OpeningInk);
            openingStoryContent = content.gameObject.AddComponent<CanvasGroup>();
            openingEnvironment = Box("Opening Environment", content, Vector2.zero, Vector2.one);
            openingBackdrop = OpeningImage("Vessel Artwork", openingEnvironment, background, Vector2.zero, Vector2.one, Color.white);
            if (arenaCellFrames == null) arenaCellFrames = ReadArenaFrames(Resources.Load<Texture2D>("BattleArena/Cells"), 8);
            if (arenaPlasmaFrames == null) arenaPlasmaFrames = ReadArenaFrames(Resources.Load<Texture2D>("BattleArena/Plasma"), 8);
            openingCells = OpeningImage("Flowing Blood Cells", openingEnvironment, null, Vector2.zero, Vector2.one, new Color(1, .4f, .5f, .35f));
            openingPlasma = OpeningImage("Foreground Plasma", openingEnvironment, null, Vector2.zero, Vector2.one, new Color(.3f, .85f, 1, .24f));
            openingShade = Box("Vessel Shade", content, Vector2.zero, Vector2.one, OpeningInk).GetComponent<UnityEngine.UI.Image>();
            openingAlarm = Box("Emergency Light", content, Vector2.zero, Vector2.one, Color.clear).GetComponent<UnityEngine.UI.Image>();
            openingSafe = Box("Opening Safe Area", content, Vector2.zero, Vector2.one);
            openingCamera = Box("Opening Camera", openingSafe, Vector2.zero, Vector2.one);

            openingSets = new GameObject[5];
            string[] names = { "Emergency Signal", "Threat Scan", "Unit Activation", "Mission Briefing", "Mission Launch" };
            for (int i = 0; i < openingSets.Length; i++)
                openingSets[i] = Box(names[i], openingCamera, Vector2.zero, Vector2.one).gameObject;
            BuildOpeningSignal(openingSets[0].transform);
            BuildOpeningThreatScan(openingSets[1].transform);
            BuildOpeningUnit(openingSets[2].transform);
            BuildOpeningMission(openingSets[3].transform);
            BuildOpeningLaunch(openingSets[4].transform);
            BuildOpeningMotion();

            var caption = Box("Opening Subtitle Panel", openingSafe, new Vector2(.05f, .045f), new Vector2(.95f, .215f), OpeningInk);
            Shape(caption.GetComponent<UnityEngine.UI.Image>(), new Color(.02f, .045f, .075f, .97f), 12, 1,
                new Color(.18f, .36f, .43f), false);
            openingSpeaker = OpeningText("Opening Speaker", "", caption, new Vector2(.025f, .70f), new Vector2(.975f, .96f), 21, OpeningAmber);
            openingSubtitle = OpeningText("Opening Subtitle", "", caption, new Vector2(.025f, .08f), new Vector2(.975f, .73f), 32, Color.white);
            openingSubtitle.fontSizeMin = 27;
            openingSubtitle.richText = false;
            openingChapter = OpeningText("Opening Chapter", "", openingSafe, new Vector2(.05f, .905f), new Vector2(.71f, .96f), 21, OpeningCyan);
            openingCurtain = Box("Opening Fade", root, Vector2.zero, Vector2.one, Color.black).GetComponent<UnityEngine.UI.Image>();
            openingSkip = Button("SKIP", root, new Vector2(.83f, .91f), new Vector2(.95f, .973f), CompleteOpeningCinematic, OpeningInk);
            MedicalButton(openingSkip);
            openingSkip.interactable = false;
            UpdateOpeningSafeArea();
        }

        void BuildOpeningSignal(Transform root)
        {
            var alarm = Box("Emergency Hologram", root, new Vector2(.11f, .39f), new Vector2(.89f, .76f));
            openingAlarmTitle = alarm.gameObject.AddComponent<CanvasGroup>();
            openingAlarmRing = OpeningRing("Emergency Reticle", alarm, new Vector2(.01f, .10f), new Vector2(.25f, .94f),
                new Color(1, .27f, .18f, .7f));
            OpeningText("Emergency Symbol", "!", alarm, new Vector2(.01f, .10f), new Vector2(.25f, .94f), 72,
                new Color(1, .42f, .3f), TextAlignmentOptions.Center);
            OpeningText("Emergency Protocol", "PROTOKOL DARURAT", alarm, new Vector2(.29f, .55f), new Vector2(1, .89f), 44, Color.white);
            OpeningText("Emergency Detected", "ANOMALI TERDETEKSI", alarm, new Vector2(.29f, .25f), new Vector2(1, .52f), 27, OpeningAmber);
            OpeningText("Simulation Context", "SIMULASI PEMBULUH DARAH", root, new Vector2(.12f, .30f), new Vector2(.8f, .37f), 20, OpeningCyan);
            var glitch = Box("Brief Signal Interference", root, Vector2.zero, Vector2.one);
            openingGlitch = glitch.gameObject.AddComponent<CanvasGroup>();
            for (int i = 0; i < 3; i++)
                Box("Interference Band " + i, glitch, new Vector2(.1f + i * .12f, .43f + i * .09f),
                    new Vector2(.85f, .435f + i * .09f), OpeningCyan);
        }

        void BuildOpeningThreatScan(Transform root)
        {
            openingThreatCategory = OpeningText("Threat Category", "", root, new Vector2(.075f, .67f), new Vector2(.39f, .78f), 40, Color.white);
            openingThreatWarning = OpeningText("Threat Detection", "", root, new Vector2(.08f, .50f), new Vector2(.36f, .65f), 23, OpeningAmber);
            var map = OpeningImage("Anomaly Map", root, worldMap, new Vector2(.79f, .36f), new Vector2(.95f, .72f), new Color(.2f, .75f, .85f, .22f));
            for (int i = 0; i < 6; i++)
            {
                float x = .24f + (i % 2) * .43f, y = .15f + i / 2 * .29f;
                OpeningImage("Anomaly " + i, map.transform, joystickDisc, new Vector2(x, y), new Vector2(x + .055f, y + .045f), OpeningAmber);
            }
            openingThreats = new UnityEngine.UI.Image[3];
            int[] ids = { 0, 2, 4 };
            for (int i = 0; i < 3; i++)
                openingThreats[i] = OpeningImage("Animated Threat " + ids[i], root, EncounterSprite(ids[i]),
                    new Vector2(.38f, .29f), new Vector2(.75f, .79f), Color.clear);
            openingThreatScan = Box("Threat Scan Line", root, new Vector2(.35f, .30f), new Vector2(.77f, .303f), OpeningCyan);
            var unknown = Box("Unknown Threat Crop", root, new Vector2(.48f, .33f), new Vector2(.83f, .80f));
            unknown.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            openingUnknownThreat = unknown.gameObject.AddComponent<CanvasGroup>();
            openingUnknownImage = OpeningImage("Partial Unknown Silhouette", unknown, EncounterSprite(5),
                new Vector2(-.65f, -1), new Vector2(2.55f, 1.15f), new Color(.23f, .13f, .30f, .9f));
        }

        void BuildOpeningUnit(Transform root)
        {
            OpeningText("Activation Heading", "UNIT / VEIN VANGUARD", root, new Vector2(.08f, .72f), new Vector2(.41f, .82f), 31, OpeningCyan);
            openingUnitStatus = OpeningText("Unit Status", "", root, new Vector2(.08f, .52f), new Vector2(.39f, .68f), 36, Color.white);
            openingPower = OpeningImage("Rising Activation Energy", root, joystickDisc, new Vector2(.42f, .31f), new Vector2(.69f, .82f),
                new Color(.08f, .8f, 1, .06f));
            openingPower.preserveAspect = false;
            openingEnergyRing = OpeningRing("Activation Energy Ring", root, new Vector2(.36f, .29f), new Vector2(.75f, .83f),
                new Color(.2f, .85f, 1, .36f));
            var platform = OpeningImage("Activation Plinth", root, joystickDisc, new Vector2(.42f, .29f), new Vector2(.69f, .32f),
                new Color(.2f, .8f, 1, .48f));
            platform.preserveAspect = false;
            openingUnit = OpeningImage("Activation Nanobot", root, playerFrames[47], new Vector2(.39f, .31f),
                new Vector2(.72f, .82f), Color.white);
            openingEnergyParticles = new UnityEngine.UI.Image[4];
            for (int i = 0; i < openingEnergyParticles.Length; i++)
                openingEnergyParticles[i] = OpeningImage("Orbiting Activation Energy " + i, root, joystickDisc,
                    Vector2.zero, Vector2.zero, OpeningCyan);
        }

        void BuildOpeningMission(Transform root)
        {
            openingAbilityTitle = OpeningText("Ability Demonstration", "", root, new Vector2(.075f, .75f), new Vector2(.8f, .84f), 39, OpeningCyan);
            openingAbilityDescription = OpeningText("Ability Description", "", root, new Vector2(.08f, .66f), new Vector2(.83f, .74f), 25, Color.white);
            openingDemoUnit = OpeningImage("Demonstrating Nanobot", root, playerFrames[0], new Vector2(.10f, .30f), new Vector2(.36f, .66f), Color.white);
            openingDemoTarget = OpeningImage("Demonstration Threat", root, EncounterSprite(0), new Vector2(.79f, .33f), new Vector2(.95f, .66f), new Color(.3f, .7f, .8f, .8f));
            openingDemoScan = OpeningImage("Diagnose Scan Demonstration", root, null, new Vector2(.76f, .30f), new Vector2(.97f, .70f), Color.white);
            openingDemoShield = OpeningImage("Homeostasis Shield Demonstration", root, null, new Vector2(.08f, .26f), new Vector2(.39f, .70f), Color.white);
            openingDemoRestore = OpeningImage("Restore Energy Demonstration", root, null, new Vector2(.08f, .26f), new Vector2(.39f, .70f), Color.white);
            openingMissionCards = Box("Hologram Assembly", root, new Vector2(.35f, .31f), new Vector2(.77f, .71f));
            OpeningImage("Mission Plasma Weapon", openingMissionCards, LearningSprite("plasma_weapon"), new Vector2(0, .04f), new Vector2(1, .85f), Color.white);
            openingCards = new RectTransform[2];
            for (int i = 0; i < 2; i++)
            {
                var port = Box("Demonstration Port " + i, openingMissionCards, new Vector2(.46f + i * .25f, .43f),
                    new Vector2(.62f + i * .25f, .65f), OpeningInk);
                Shape(port.GetComponent<UnityEngine.UI.Image>(), OpeningInk, 6, 1.5f, OpeningCyan, false);
                var card = Box("Information Card " + i, openingMissionCards, Vector2.zero, Vector2.one, OpeningInk);
                Shape(card.GetComponent<UnityEngine.UI.Image>(), new Color(.05f, .2f, .28f), 6, 2, OpeningCyan, false);
                OpeningText("Information Identity", i == 0 ? "A" : "B", card, Vector2.zero, Vector2.one, 26, OpeningCyan, TextAlignmentOptions.Center);
                openingCards[i] = card;
            }
            openingReactor = OpeningImage("Mission Reactor", openingMissionCards, joystickDisc, new Vector2(.25f, .33f), new Vector2(.39f, .53f), OpeningCyan);
            openingDemoProjectile = OpeningImage("Demonstration Plasma", root, null, Vector2.zero, Vector2.one, Color.white);
            openingDemoImpact = OpeningImage("Demonstration Impact", root, null, new Vector2(.76f, .29f), new Vector2(.98f, .72f), Color.white);
            var mp = Box("Restore MP Demonstration", root, new Vector2(.11f, .255f), new Vector2(.36f, .30f));
            openingMpGroup = mp.gameObject.AddComponent<CanvasGroup>();
            var track = Box("MP Track", mp, new Vector2(0, .1f), new Vector2(.68f, .4f), new Color(.03f, .16f, .2f));
            openingDemoMpFill = Box("MP Fill", track, Vector2.zero, Vector2.one, OpeningCyan);
            openingDemoMpValue = OpeningText("MP Demonstration Value", "20 / 60 MP", mp, new Vector2(.68f, 0), Vector2.one, 17, Color.white);
        }

        void BuildOpeningLaunch(Transform root)
        {
            openingCorridorEnemy = OpeningImage("Distant First Threat", root, EncounterSprite(0), new Vector2(1.22f, .40f),
                new Vector2(1.34f, .64f), new Color(.06f, .025f, .04f, .88f));
            openingRunnerShadow = OpeningImage("Runner Contact", root, joystickDisc, new Vector2(.20f, .29f),
                new Vector2(.36f, .32f), new Color(0, 0, 0, .5f));
            openingRunnerShadow.preserveAspect = false;
            openingRunner = OpeningImage("Advancing Nanobot", root, walkFrames[8], new Vector2(.17f, .30f),
                new Vector2(.40f, .68f), Color.white);
            var title = Box("Mission Title", root, new Vector2(.08f, .67f), new Vector2(.92f, .84f));
            openingEndTitle = title.gameObject.AddComponent<CanvasGroup>();
            OpeningText("Mission Game Title", "VEIN VANGUARD", title, new Vector2(0, .40f), Vector2.one, 53, Color.white, TextAlignmentOptions.Center);
            OpeningText("Mission Identifier", "MISSION 01 — ANALISIS ANCAMAN", title, Vector2.zero, new Vector2(1, .39f), 26, OpeningCyan, TextAlignmentOptions.Center);
        }

        void UpdateOpeningSafeArea()
        {
            if (!openingSafe) return;
            Rect area = Screen.safeArea;
            Place(openingSafe, new Vector2(area.xMin / Screen.width, area.yMin / Screen.height),
                new Vector2(area.xMax / Screen.width, area.yMax / Screen.height));
            float scale = openingGroup.GetComponentInParent<Canvas>().scaleFactor;
            float density = touchDensity > 0 ? touchDensity : 1;
            float height = Mathf.Max(58, 48 * density / scale);
            var parent = (RectTransform)openingSkip.transform.parent;
            Vector2 corner = parent.InverseTransformPoint(new Vector2(area.xMax, area.yMax));
            Fixed((RectTransform)openingSkip.transform, Vector2.one * .5f, Vector2.one, corner - new Vector2(28, 22),
                new Vector2(Mathf.Max(150, height * 2.2f), height));
        }

        Sprite OpeningEnemyFrame(int encounter, float time, int pose = 0)
        {
            var frames = encounter == 0 ? enemyOneFrames : EnemyAnimationFrames(encounter);
            return frames != null && frames.Length >= 16 ? frames[pose * 4 + (int)(time * 6) % 4] : EncounterSprite(encounter);
        }

        void OpeningEffect(UnityEngine.UI.Image image, int effect, float time, bool visible)
        {
            image.enabled = visible && vfxFrames != null && vfxFrames.Length > effect * 4 + 3;
            if (image.enabled) image.sprite = vfxFrames[effect * 4 + (openingReducedMotion ? 2 : (int)(Mathf.Max(0, time) * 7) % 4)];
        }

        void RenderOpeningEnvironment(int scene)
        {
            float time = openingTime, motion = openingReducedMotion ? 0 : time;
            float zoom = 1, pan = 0, lift = 0;
            if (!openingReducedMotion)
            {
                if (scene == 0) { zoom = Mathf.Lerp(1.02f, 1.16f, Mathf.SmoothStep(0, 1, time / 10)); pan = -time * 3; }
                else if (scene == 1)
                {
                    float entry = (time - 10) % 3.4f;
                    zoom = 1.02f + (time - 10) * .004f;
                    pan = -18 + Mathf.Sin(Mathf.Clamp01(entry / 1.6f) * Mathf.PI) * 18;
                }
                else if (scene == 2)
                {
                    float peak = time > 27.9f && time < 28.7f ? Mathf.Sin((time - 27.9f) / .8f * Mathf.PI) : 0;
                    zoom = 1 + .15f * Mathf.SmoothStep(0, 1, (time - 26) / 3) + peak * .025f;
                    pan = -18 * (zoom - 1);
                }
                else if (scene == 3) { zoom = 1.01f; }
                else OpeningJourneyCamera(out zoom, out pan, out lift);
            }
            Vector2 shake = Vector2.zero;
            if (!openingReducedMotion && time > 2.9f && time < 3.25f)
                shake = new Vector2(Mathf.Sin(time * 91), Mathf.Cos(time * 83)) * 4 * Mathf.Sin((time - 2.9f) / .35f * Mathf.PI);
            openingCamera.localScale = Vector3.one * zoom;
            openingCamera.anchoredPosition = new Vector2(pan, lift) + shake;
            openingEnvironment.localScale = Vector3.one * (1 + (zoom - 1) * .55f);
            openingEnvironment.anchoredPosition = new Vector2(pan * .25f, lift * .25f) + shake * .3f;
            var root = (RectTransform)openingGroup.transform;
            float aspect = background.rect.width / background.rect.height;
            float width = Mathf.Max(root.rect.width, root.rect.height * aspect) * 1.16f;
            Fixed(openingBackdrop.rectTransform, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(width, width / aspect));
            openingBackdrop.color = scene == 2 ? new Color(.32f, .49f, .65f) : Color.white;
            float dark = scene == 0 ? Mathf.Lerp(.98f, .86f, Mathf.Clamp01((time - 1) / 3)) :
                scene == 2 ? .94f : scene == 4 ? .84f : .90f;
            dark += openingReducedMotion ? 0 : Mathf.Sin(time * 1.2f) * .008f;
            openingShade.color = new Color(.008f, .02f, .04f, dark);
            openingCells.enabled = arenaCellFrames != null;
            if (openingCells.enabled) openingCells.sprite = arenaCellFrames[(int)(motion * 8) % arenaCellFrames.Length];
            openingPlasma.enabled = arenaPlasmaFrames != null;
            if (openingPlasma.enabled) openingPlasma.sprite = arenaPlasmaFrames[(int)(motion * 8) % arenaPlasmaFrames.Length];
            openingCells.rectTransform.anchoredPosition = new Vector2(pan * .4f + Mathf.Sin(motion * .25f) * 55, Mathf.Sin(motion * .5f) * 15);
            openingPlasma.rectTransform.anchoredPosition = new Vector2(pan * .65f + Mathf.Sin(motion * .32f) * 85, Mathf.Cos(motion * .3f) * 11);
            float red = scene == 0 && time > 2.5f ? .10f + (openingReducedMotion ? 0 : .09f * Mathf.Sin((time - 2.5f) * 5)) :
                scene == 4 ? Mathf.Lerp(.04f, .15f, (time - 47) / 12) : .015f;
            openingAlarm.color = new Color(.8f, .02f, .03f, red);
            openingPlasma.color = Color.Lerp(new Color(.3f, .85f, 1, .24f), new Color(1, .25f, .12f, .34f),
                scene == 0 ? Mathf.Clamp01(red * 4) : 0);
            RenderOpeningDepth(scene, pan, zoom, width);
        }

        void RenderOpeningActors(int scene)
        {
            if (scene != 4) openingEndTitle.alpha = 0;
            if (scene == 0) RenderOpeningSignal();
            else if (scene == 1) RenderOpeningThreats();
            else if (scene == 2) RenderOpeningActivation();
            else if (scene == 3) RenderOpeningDemonstration();
            else RenderOpeningDeparture();
        }

        void RenderOpeningSignal()
        {
            float reveal = Mathf.SmoothStep(0, 1, (openingTime - 2.3f) / .5f);
            openingAlarmTitle.alpha = reveal;
            openingAlarmTitle.transform.localScale = Vector3.one * (openingReducedMotion ? 1 : Mathf.Lerp(1.13f, 1, reveal));
            openingAlarmRing.localRotation = Quaternion.Euler(0, 0, openingReducedMotion ? 0 : -openingTime * 16);
            bool glitch = openingTime > 2.94f && openingTime < 3.02f || openingTime > 3.17f && openingTime < 3.24f;
            openingGlitch.alpha = !openingReducedMotion && glitch ? .3f : 0;
        }

        void RenderOpeningThreats()
        {
            float elapsed = openingTime - 10;
            int subject = Mathf.Min(2, (int)(elapsed / 3.4f));
            float local = elapsed - subject * 3.4f;
            bool unknown = elapsed > 10.1f;
            for (int i = 0; i < openingThreats.Length; i++)
            {
                var image = openingThreats[i];
                bool active = i == subject && !unknown;
                int id = i == 0 ? 0 : i == 1 ? 2 : 4;
                int pose = local >= 1.2f && local < 1.65f ? 2 : local >= 1.9f ? 1 : 0;
                image.sprite = OpeningEnemyFrame(id, openingReducedMotion ? 0 : Mathf.Max(0, local), pose);
                image.color = active ? Color.Lerp(new Color(.025f, .035f, .05f), new Color(.65f, .9f, 1),
                    Mathf.SmoothStep(0, 1, local / 1.5f)) : Color.clear;
                float arrival = 1 - Mathf.SmoothStep(0, 1, local / .8f);
                float recoil = local > 1.2f && local < 1.65f ? Mathf.Sin((local - 1.2f) / .45f * Mathf.PI) : 0;
                Vector2 offset = i == 0 ? Vector2.left * 100 * arrival :
                    i == 1 ? Vector2.down * 80 * arrival : Vector2.right * 85 * arrival;
                image.rectTransform.anchoredPosition = openingReducedMotion ? Vector2.zero : offset + new Vector2(recoil * 9, Mathf.Sin(local * 2) * 4);
                image.rectTransform.localRotation = Quaternion.Euler(0, 0, openingReducedMotion ? 0 : (i == 1 ? arrival * -5 : recoil * 2));
                image.rectTransform.localScale = Vector3.one * (openingReducedMotion ? 1 : 1 + (i == 2 ? arrival * .18f : 0) + recoil * .025f);
            }
            openingThreatCategory.text = unknown ? "SIGNAL TIDAK DIKENAL" : OpeningCategories[subject];
            openingThreatCategory.fontSizeMax = unknown ? 29 : 40;
            openingThreatCategory.fontSizeMin = unknown ? 24 : 33;
            openingThreatWarning.text = unknown ? "ANOMALI BESAR\nLOKASI BELUM TERPETAKAN" :
                local < 1.2f ? "PEMINDAIAN BERLANGSUNG" : "!  ANCAMAN TERIDENTIFIKASI";
            openingThreatWarning.color = local < 1.2f && !unknown ? OpeningCyan : new Color(1, .38f, .28f);
            openingThreatScan.gameObject.SetActive(!unknown);
            openingThreatScan.anchoredPosition = Vector2.up * (openingReducedMotion ? 150 : Mathf.PingPong(local * 230, 330));
            openingUnknownThreat.alpha = unknown ? Mathf.SmoothStep(0, 1, (elapsed - 10.1f) / .6f) : 0;
            openingUnknownImage.rectTransform.anchoredPosition = Vector2.left * (openingReducedMotion ? 0 : (elapsed - 10.1f) * 16);
            float ripple = Mathf.Clamp01((local - 1.2f) / .65f);
            openingThreatPulseGroup.alpha = !unknown && local >= 1.2f && local < 1.85f ? 1 - ripple : 0;
            openingThreatPulse.localScale = Vector3.one * (openingReducedMotion ? 1 : .85f + ripple * .4f);
        }

        void RenderOpeningActivation()
        {
            float elapsed = openingTime - 22;
            float rise = Mathf.SmoothStep(0, 1, (elapsed - 1) / 4.7f);
            int poseFrame = Mathf.Clamp(Mathf.FloorToInt((elapsed - 1) / 1.15f), 0, 3);
            if (elapsed < 6) openingUnit.sprite = playerFrames[47 - poseFrame];
            else if (elapsed < 8.3f) openingUnit.sprite = playerFrames[16 + Mathf.Clamp((int)((elapsed - 6) * 2), 0, 3)];
            else openingUnit.sprite = playerFrames[openingReducedMotion ? 0 : (int)((elapsed - 8.3f) * 7) % 4];
            openingUnit.color = Color.Lerp(new Color(.1f, .17f, .23f), Color.white, Mathf.Clamp01((elapsed - 2) / 4));
            openingUnit.rectTransform.anchoredPosition = openingReducedMotion ? Vector2.zero : new Vector2(0, Mathf.Lerp(-22, 8, rise));
            float settle = elapsed > 6 && elapsed < 7.3f ? Mathf.Sin((elapsed - 6) / 1.3f * Mathf.PI) : 0;
            openingUnit.rectTransform.localScale = openingReducedMotion ? Vector3.one : new Vector3(1 - settle * .04f, 1 + settle * .06f, 1);
            openingUnit.rectTransform.localRotation = Quaternion.Euler(0, 0, openingReducedMotion ? 0 : (1 - rise) * -5);
            Place(openingPower.rectTransform, new Vector2(.42f, .31f), new Vector2(.69f, .31f + .51f * rise));
            openingPower.color = new Color(.08f, .8f, 1, .02f + rise * .11f);
            openingEnergyRing.localRotation = Quaternion.Euler(0, 0, openingReducedMotion ? 0 : -elapsed * 38);
            for (int i = 0; i < openingEnergyParticles.Length; i++)
            {
                float angle = i * Mathf.PI * .5f + (openingReducedMotion ? 0 : elapsed * 2.2f);
                float radius = openingReducedMotion ? 135 : Mathf.Lerp(170, 105, rise);
                var point = openingEnergyParticles[i];
                Fixed(point.rectTransform, new Vector2(.55f, .56f), Vector2.one * .5f,
                    new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 1.2f), new Vector2(11, 11));
                point.color = new Color(.3f, .9f, 1, .25f + rise * .65f);
            }
            openingUnitStatus.text = elapsed < 1 ? "OFFLINE" : elapsed < 6 ? "MENGISI ENERGI" : "ONLINE";
            openingUnitStatus.color = elapsed < 6 ? OpeningAmber : OpeningCyan;
            RenderOpeningHeadRig(elapsed);
        }

        void RenderOpeningDemonstration()
        {
            float elapsed = openingTime - 35;
            int ability = Mathf.Clamp((int)(elapsed / 3), 0, 3);
            float local = elapsed - ability * 3;
            openingAbilityTitle.text = OpeningAbilities[ability];
            openingAbilityDescription.text = OpeningAbilityDescriptions[ability];
            int pose = ability == 0 ? 1 : ability == 1 ? 2 : ability == 2 ? 4 : 6;
            openingDemoUnit.sprite = playerFrames[pose * 4 + (openingReducedMotion ? 2 : Mathf.Min(3, (int)(local * 3)))];
            openingDemoTarget.sprite = OpeningEnemyFrame(0, openingReducedMotion ? 0 : local);
            OpeningEffect(openingDemoScan, 0, local, ability == 0);
            if (ability == 0) openingDemoScan.rectTransform.anchoredPosition = Vector2.up * (openingReducedMotion ? 0 : Mathf.Sin(local * 2) * 20);
            OpeningEffect(openingDemoRestore, 3, local, ability == 2);
            OpeningEffect(openingDemoShield, local < 1 ? 4 : 5, local, ability == 3);
            openingMissionCards.gameObject.SetActive(ability == 1);
            for (int i = 0; i < 2; i++)
            {
                float progress = Mathf.SmoothStep(0, 1, (local - .1f - i * .4f) / .65f);
                if (openingReducedMotion) progress = local < 1.2f ? 0 : 1;
                Vector2 min = Vector2.Lerp(new Vector2(.08f + i * .3f, .88f), new Vector2(.46f + i * .25f, .43f), progress);
                Place(openingCards[i], min, min + new Vector2(.16f, .22f));
            }
            float charged = Mathf.Clamp01((local - 1.2f) / .65f);
            openingReactor.color = new Color(.2f, .95f, 1, .2f + charged * .65f);
            float shot = Mathf.Clamp01((local - 2) / .6f);
            if (openingReducedMotion) shot = local < 2.6f ? 0 : 1;
            Vector2 launch = Vector2.Lerp(new Vector2(.71f, .45f), new Vector2(.84f, .45f), shot);
            Place(openingDemoProjectile.rectTransform, launch, launch + new Vector2(.10f, .12f));
            OpeningEffect(openingDemoProjectile, 1, local, ability == 1 && local >= 2 && local < 2.6f);
            OpeningEffect(openingDemoImpact, 2, local, ability == 1 && local >= 2.6f);
            openingMpGroup.alpha = ability == 2 ? 1 : 0;
            float recharge = Mathf.SmoothStep(0, 1, (local - .25f) / 1.8f);
            openingDemoMpFill.anchorMax = new Vector2(Mathf.Lerp(1f / 3, 1, recharge), 1);
            openingDemoMpValue.text = recharge < .99f ? "20 / 60 MP" : "60 / 60 MP";
        }

        void RenderOpeningDeparture()
        {
            float elapsed = openingTime - 47;
            float distance = OpeningJourneyDistance();
            int step = openingReducedMotion || elapsed >= 11.2f ? 0 : (int)(distance / (openingSafe.rect.height * .02f)) % 8;
            openingRunner.sprite = walkFrames[8 + step];
            openingRunner.rectTransform.anchoredPosition = Vector2.right * distance;
            openingRunnerShadow.rectTransform.anchoredPosition = openingRunner.rectTransform.anchoredPosition;
            float heroic = !openingReducedMotion && elapsed > 9.2f && elapsed < 10.4f ? Mathf.Sin((elapsed - 9.2f) / 1.2f * Mathf.PI) : 0;
            openingRunner.rectTransform.localRotation = Quaternion.Euler(0, 0, -heroic * 3);
            openingCorridorEnemy.sprite = OpeningEnemyFrame(0, openingReducedMotion ? 0 : elapsed, elapsed > 5 ? 1 : 0);
            float reaction = openingReducedMotion ? 0 : Mathf.SmoothStep(0, 1, (elapsed - 5.5f) / 1.5f);
            Place(openingCorridorEnemy.rectTransform, new Vector2(openingReducedMotion ? .88f : 1.22f, .40f),
                new Vector2(openingReducedMotion ? 1f : 1.34f, .64f));
            openingCorridorEnemy.rectTransform.anchoredPosition = new Vector2(reaction * 20, 0);
            openingCorridorEnemy.rectTransform.localScale = Vector3.one * (1 + reaction * .08f);
            openingEndTitle.alpha = Mathf.SmoothStep(0, 1, (elapsed - 7) / .8f);
            openingEndTitle.transform.localScale = Vector3.one * (openingReducedMotion ? 1 : Mathf.Lerp(1.06f, 1, openingEndTitle.alpha));
            RenderOpeningPortal();
        }
    }
}
