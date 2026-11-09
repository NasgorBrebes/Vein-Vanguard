using UnityEngine;

namespace VeinVanguard
{
    public partial class BattleManager
    {
        UnityEngine.UI.Image openingBackdropNext, openingPlasmaWrap, openingOnlineGlow;
        UnityEngine.UI.Image openingHeadImage, openingBodyImage;
        UnityEngine.UI.Image[] openingRiseSparks, openingEyeGlints, openingFlyby;
        RectTransform openingUnitHead, openingUnitBodyMask, openingExitPortal, openingThreatPulse;
        CanvasGroup openingThreatPulseGroup;
        UnityEngine.UI.Image openingJourneyMap, openingExitGlow, openingExitWindow;
        OutlinedUiGraphic openingExitShape;

        void BuildOpeningMotion()
        {
            openingBackdropNext = OpeningImage("Continuation Of Vessel", openingEnvironment, background, Vector2.zero, Vector2.one, Color.white);
            openingBackdropNext.transform.SetSiblingIndex(1);
            openingBackdropNext.rectTransform.localScale = new Vector3(-1, 1, 1);
            openingPlasmaWrap = OpeningImage("Continuous Foreground Plasma", openingEnvironment, null, Vector2.zero, Vector2.one, Color.white);
            openingFlyby = new UnityEngine.UI.Image[2];
            for (int i = 0; i < openingFlyby.Length; i++)
                openingFlyby[i] = OpeningImage("Near Camera Cells " + i, openingSafe, null, Vector2.zero, Vector2.zero, Color.white);

            openingEndTitle.transform.SetParent(openingSafe, false);
            Place((RectTransform)openingEndTitle.transform, new Vector2(.08f, .76f), new Vector2(.92f, .90f));
            var launch = openingSets[4].transform;
            openingExitPortal = Box("Simulation Corridor Exit", launch, Vector2.zero, Vector2.zero);
            openingExitPortal.SetAsFirstSibling();
            openingExitGlow = OpeningImage("Exit Light", openingExitPortal, joystickDisc, new Vector2(-.12f, -.12f), new Vector2(1.12f, 1.12f), OpeningCyan);
            var window = OpeningImage("MAP 01 Aperture", openingExitPortal, joystickDisc, Vector2.zero, Vector2.one, Color.white);
            openingExitWindow = window;
            openingExitShape = Shape(window, Color.white, 225, 0, null, false);
            window.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            Rect source = worldMap.rect;
            float crop = Mathf.Min(source.width, source.height) * .24f;
            openingJourneyMap = OpeningImage("Connected MAP 01", window.transform, worldMap,
                new Vector2(.5f - source.width / (2 * crop), 0), new Vector2(.5f + source.width / (2 * crop), source.height / crop), Color.white);
            OpeningRing("Exit Rim", openingExitPortal, Vector2.zero, Vector2.one, new Color(.3f, .9f, 1, .6f));
            BuildOpeningHeadRig();

            openingRiseSparks = new UnityEngine.UI.Image[2];
            for (int i = 0; i < 2; i++)
                openingRiseSparks[i] = OpeningImage("Rising Body Energy " + i, openingSets[2].transform, joystickDisc,
                    Vector2.zero, Vector2.zero, OpeningCyan);
            openingOnlineGlow = OpeningImage("ONLINE Light Pulse", openingSets[2].transform, joystickDisc,
                new Vector2(.32f, .24f), new Vector2(.79f, .87f), Color.clear);
            openingOnlineGlow.transform.SetSiblingIndex(openingUnit.transform.GetSiblingIndex());
            openingThreatPulse = OpeningRing("Scan Confirmation Ripple", openingSets[1].transform,
                new Vector2(.36f, .28f), new Vector2(.77f, .79f), new Color(1, .38f, .2f, .65f));
            openingThreatPulse.SetAsFirstSibling();
            openingThreatPulseGroup = openingThreatPulse.gameObject.AddComponent<CanvasGroup>();
        }

        void BuildOpeningHeadRig()
        {
            openingUnitBodyMask = Box("Nanobot Body Crop", openingUnit.transform, Vector2.zero, new Vector2(1, .43f));
            openingUnitBodyMask.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            openingBodyImage = OpeningImage("Original Nanobot Body", openingUnitBodyMask, null, Vector2.zero, new Vector2(1, 1 / .43f), Color.white);
            openingUnitHead = Box("Nanobot Head Crop", openingUnit.transform, new Vector2(0, .43f), Vector2.one);
            openingUnitHead.pivot = new Vector2(.5f, 0);
            Place(openingUnitHead, new Vector2(0, .43f), Vector2.one);
            openingUnitHead.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            openingHeadImage = OpeningImage("Original Nanobot Head", openingUnitHead, null, new Vector2(0, -.43f / .57f), Vector2.one, Color.white);
            openingEyeGlints = new UnityEngine.UI.Image[2];
            for (int i = 0; i < 2; i++)
            {
                var eye = OpeningImage("ONLINE Eye Glint " + i, openingUnitHead, joystickDisc, Vector2.zero, Vector2.zero, Color.clear);
                Fixed(eye.rectTransform, new Vector2(i == 0 ? .52f : .60f, .25f), Vector2.one * .5f, Vector2.zero, new Vector2(5, 8));
                openingEyeGlints[i] = eye;
            }
        }

        float OpeningJourneyDistance()
        {
            return openingReducedMotion ? 0 : openingSafe.rect.width * .78f *
                Mathf.SmoothStep(0, 1, (openingTime - 47) / 11.2f);
        }

        void OpeningJourneyCamera(out float zoom, out float pan, out float lift)
        {
            float elapsed = openingTime - 47;
            float advance = Mathf.Clamp01(elapsed / 11.2f);
            float push = Mathf.SmoothStep(0, 1, (elapsed - 9.4f) / 2.7f);
            float heroic = elapsed > 9.2f && elapsed < 10.4f ? Mathf.Sin((elapsed - 9.2f) / 1.2f * Mathf.PI) : 0;
            zoom = 1 + advance * .08f + push * .18f + heroic * .025f;
            float tracking = -OpeningJourneyDistance() * .82f;
            float destination = -.60f * openingSafe.rect.width * zoom;
            pan = Mathf.Lerp(tracking, destination, push);
            lift = -push * openingSafe.rect.height * .03f;
        }

        void RenderOpeningDepth(int scene, float pan, float zoom, float width)
        {
            bool journey = scene == 4;
            openingBackdropNext.enabled = journey;
            openingPlasmaWrap.enabled = journey && arenaPlasmaFrames != null;
            if (journey)
            {
                openingEnvironment.localScale = Vector3.one * zoom;
                openingEnvironment.anchoredPosition = Vector2.zero;
                openingBackdrop.rectTransform.anchoredPosition = Vector2.right * (pan / zoom);
                Fixed(openingBackdropNext.rectTransform, Vector2.one * .5f, Vector2.one * .5f,
                    new Vector2(pan / zoom + width, 0), openingBackdrop.rectTransform.sizeDelta);
                openingBackdropNext.color = openingBackdrop.color;
                openingCells.rectTransform.anchoredPosition = new Vector2(pan * .4f / zoom, 0);
                float flow = openingReducedMotion ? 0 : (openingTime - 47) * 42;
                float span = openingSafe.rect.width;
                float offset = openingReducedMotion ? 0 : Mathf.Repeat(pan * 1.1f / zoom - flow, span) - span;
                openingPlasma.rectTransform.anchoredPosition = Vector2.right * offset;
                openingPlasmaWrap.rectTransform.anchoredPosition = Vector2.right * (offset + span);
                openingPlasmaWrap.sprite = openingPlasma.sprite;
                openingPlasmaWrap.color = openingPlasma.color;
            }
            for (int i = 0; i < openingFlyby.Length; i++)
            {
                var cell = openingFlyby[i];
                cell.enabled = !openingReducedMotion && arenaCellFrames != null;
                if (!cell.enabled) continue;
                float phase = Mathf.Repeat(openingTime * (journey ? .32f : .13f) + i * .5f, 1);
                float x = Mathf.Lerp(openingSafe.rect.width * .75f, -openingSafe.rect.width * .75f, phase);
                float y = openingSafe.rect.height * (.08f + i * .18f);
                Fixed(cell.rectTransform, Vector2.one * .5f, Vector2.one * .5f, new Vector2(x, y), new Vector2(480, 270));
                cell.sprite = arenaCellFrames[(int)(openingTime * 8) % arenaCellFrames.Length];
                cell.color = new Color(1, .5f, .6f, (journey ? .24f : .10f) * Mathf.Sin(phase * Mathf.PI));
            }
        }

        void RenderOpeningHeadRig(float elapsed)
        {
            bool standing = elapsed >= 5.9f;
            openingUnit.enabled = !standing;
            openingUnitBodyMask.gameObject.SetActive(standing);
            openingUnitHead.gameObject.SetActive(standing);
            openingHeadImage.sprite = openingBodyImage.sprite = openingUnit.sprite;
            openingHeadImage.color = openingBodyImage.color = openingUnit.color;
            float reaction = elapsed > 6 && elapsed < 8 ? Mathf.Sin((elapsed - 6) * Mathf.PI * 2) * Mathf.Exp(-(elapsed - 6)) : 0;
            openingUnitHead.localRotation = Quaternion.Euler(0, 0, openingReducedMotion ? 0 : reaction * 4);
            openingUnitHead.anchoredPosition = openingReducedMotion ? Vector2.zero : new Vector2(reaction * 1.5f, 0);
            float glow = elapsed > 6 && elapsed < 6.7f ? Mathf.Sin((elapsed - 6) / .7f * Mathf.PI) : 0;
            openingOnlineGlow.color = new Color(.1f, .8f, 1, glow * .25f);
            foreach (var eye in openingEyeGlints) eye.color = new Color(.3f, 1, 1, glow * .55f);
            for (int i = 0; i < openingRiseSparks.Length; i++)
            {
                float phase = openingReducedMotion ? .5f : Mathf.Repeat((elapsed - 1) * .5f + i * .5f, 1);
                var spark = openingRiseSparks[i];
                Fixed(spark.rectTransform, new Vector2(.55f, .56f), Vector2.one * .5f,
                    new Vector2(i == 0 ? -25 : 25, Mathf.Lerp(-155, 155, phase)), new Vector2(7, 15));
                spark.color = new Color(.3f, 1, 1, elapsed > 1 && elapsed < 6.7f ? Mathf.Sin(phase * Mathf.PI) * .7f : 0);
            }
        }

        void RenderOpeningPortal()
        {
            float elapsed = openingTime - 47;
            float size = openingSafe.rect.height * .43f;
            Fixed(openingExitPortal, new Vector2(openingReducedMotion ? .76f : 1.10f, .49f), Vector2.one * .5f, Vector2.zero, new Vector2(size, size));
            if (openingExitShape.radius != size * .5f)
            {
                openingExitShape.radius = size * .5f;
                openingExitWindow.SetVerticesDirty();
            }
            float approach = Mathf.SmoothStep(0, 1, (elapsed - 5) / 6);
            openingJourneyMap.color = new Color(.8f, 1, 1, .2f + approach * .8f);
            float heroic = elapsed > 9.2f && elapsed < 10.4f ? Mathf.Sin((elapsed - 9.2f) / 1.2f * Mathf.PI) : 0;
            openingExitGlow.color = new Color(.3f, .9f, 1, .10f + approach * .16f + heroic * .12f);
        }
    }
}
