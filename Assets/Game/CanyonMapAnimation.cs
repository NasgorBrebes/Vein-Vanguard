using UnityEngine;
using UnityEngine.UI;

namespace VeinVanguard
{
    internal sealed class CanyonMapAnimation
    {
        const string ResourcesPath = "AnimatedCanyon/";
        const float Duration = 12;
        const float Width = 2048, Height = 1448;
        static readonly int Sway0 = Shader.PropertyToID("_Sway0"), Sway1 = Shader.PropertyToID("_Sway1");
        static readonly int Tips0 = Shader.PropertyToID("_Tips0"), Tips1 = Shader.PropertyToID("_Tips1");
        static readonly int Lighting = Shader.PropertyToID("_Lighting"), Glow = Shader.PropertyToID("_Glow");
        static readonly int MistA = Shader.PropertyToID("_MistA"), MistB = Shader.PropertyToID("_MistB");
        static readonly int MistStrengthA = Shader.PropertyToID("_MistStrengthA"), MistStrengthB = Shader.PropertyToID("_MistStrengthB");
        static readonly int Flow = Shader.PropertyToID("_Flow");

        static readonly Vector2[] MoteOrigins =
        {
            new Vector2(935, 650), new Vector2(1031, 660), new Vector2(1104, 688),
            new Vector2(1160, 739), new Vector2(1305, 677), new Vector2(1370, 718),
            new Vector2(1460, 671), new Vector2(1096, 795), new Vector2(955, 817),
            new Vector2(891, 901), new Vector2(1153, 810), new Vector2(1023, 565)
        };

        readonly Material material;
        readonly Material originalMaterial;
        readonly Image map;
        readonly RectTransform dustRoot;
        readonly RawImage[] motes = new RawImage[MoteOrigins.Length];
        bool visible;
        float seconds;

        public static CanyonMapAnimation TryCreate(RectTransform parent)
        {
            var shader = Resources.Load<Shader>(ResourcesPath + "CanyonV2");
            var motion = Resources.Load<Texture2D>(ResourcesPath + "Motion");
            var regions = Resources.Load<Texture2D>(ResourcesPath + "Regions");
            var glow = Resources.Load<Texture2D>(ResourcesPath + "Mote");
            var map = parent.GetComponent<Image>();
            if (!shader || !shader.isSupported || !motion || !regions || !glow || !map) return null;
            return new CanyonMapAnimation(map, shader, motion, regions, glow);
        }

        CanyonMapAnimation(Image map, Shader shader, Texture2D motion, Texture2D regions, Texture2D glow)
        {
            this.map = map;
            originalMaterial = map.material;
            material = new Material(shader) { name = "Dark Fantasy Canyon V2 (Runtime)" };
            material.SetTexture("_Motion", motion);
            material.SetTexture("_Regions", regions);

            dustRoot = new GameObject("Animated Map 02", typeof(RectTransform)).GetComponent<RectTransform>();
            dustRoot.SetParent(map.rectTransform, false);
            dustRoot.SetAsFirstSibling();
            dustRoot.anchorMin = dustRoot.anchorMax = dustRoot.pivot = new Vector2(0, 1);
            dustRoot.anchoredPosition = Vector2.zero;
            dustRoot.sizeDelta = new Vector2(Width, Height);
            dustRoot.localScale = new Vector3(map.rectTransform.rect.width / Width, map.rectTransform.rect.height / Height, 1);

            for (int i = 0; i < motes.Length; i++)
            {
                var mote = new GameObject("Canyon mote " + (i + 1), typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                mote.transform.SetParent(dustRoot, false);
                mote.texture = glow;
                mote.raycastTarget = false;
                var rect = mote.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.sizeDelta = Vector2.one * (15 + i % 4 * 3);
                motes[i] = mote;
            }
            Tick(0);
            dustRoot.gameObject.SetActive(false);
        }

        public void SetVisible(bool show)
        {
            if (visible == show) return;
            visible = show;
            map.material = show ? material : originalMaterial;
            dustRoot.gameObject.SetActive(show);
        }

        public void Dispose()
        {
            if (map && map.material == material) map.material = originalMaterial;
            if (dustRoot) Object.Destroy(dustRoot.gameObject);
            Object.Destroy(material);
        }

        static float Wave(float phase, int cycles, float offset = 0) => Mathf.Sin(2 * Mathf.PI * (phase * cycles + offset));
        static float Wind(float phase, float offset) => .78f * Wave(phase, 1, offset) + .22f * Wave(phase, 3, offset + .16f);
        static float Fade(float phase)
        {
            float sine = Mathf.Sin(Mathf.PI * phase);
            return sine * sine;
        }

        void SetMist(int positionsId, int strengthId, float phase,
            float firstX, float firstTravel, float firstOpacity, float secondX, float secondTravel, float secondOpacity)
        {
            float a = Mathf.Repeat(phase, 1);
            float b = Mathf.Repeat(phase + .5f, 1);
            float c = Mathf.Repeat(phase * 2 + .32f, 1);
            float d = Mathf.Repeat(phase * 2 + .82f, 1);
            material.SetVector(positionsId, new Vector4(
                firstX + firstTravel * (a - .5f), firstX + firstTravel * (b - .5f),
                secondX + secondTravel * (c - .5f), secondX + secondTravel * (d - .5f)));
            material.SetVector(strengthId, new Vector4(
                Fade(a) * firstOpacity, Fade(b) * firstOpacity,
                Fade(c) * secondOpacity, Fade(d) * secondOpacity));
        }

        public void Tick(float delta)
        {
            seconds = Mathf.Repeat(seconds + delta, Duration);
            float phase = seconds / Duration;
            float pulse = .5f + .5f * Wave(phase, 2, .08f);
            material.SetVector(Lighting, new Vector4(phase, Wave(phase, 1), pulse, 0));
            material.SetVector(Sway0, new Vector4(Wave(phase, 2, .11f), Wave(phase, 2, .38f),
                Wind(phase, .04f), Wind(phase, .22f)));
            material.SetVector(Sway1, new Vector4(Wind(phase, .37f), Wind(phase, .52f), Wind(phase, .79f), 0));
            material.SetVector(Tips0, new Vector4(0, 0, Wave(phase, 3, .22f), Wave(phase, 4, .58f)));
            material.SetVector(Tips1, new Vector4(Wave(phase, 3, .72f), Wave(phase, 4, .10f), Wave(phase, 3, .43f), 0));
            material.SetVector(Glow, new Vector4(1180 + 143 * Wave(phase, 1, .25f), 676 + 44 * Wave(phase, 1),
                988 + 84 * Wave(phase, 1, .3f), 837 + 31 * Wave(phase, 2, .05f)));

            float firstFlow = Mathf.Repeat(phase * 2 + .21f, 1);
            float secondFlow = Mathf.Repeat(firstFlow + .5f, 1);
            material.SetVector(Flow, new Vector4(1090 + 244 * firstFlow, 1090 + 244 * secondFlow,
                .19f * Fade(firstFlow), .19f * Fade(secondFlow)));
            SetMist(MistA, MistStrengthA, phase + .57f, 1160, 302, .19f, 1348, 230, .21f);
            SetMist(MistB, MistStrengthB, phase + .27f, 1040, 191, .24f, 1450, 237, .14f);

            for (int i = 0; i < motes.Length; i++)
            {
                float age = Mathf.Repeat(phase * (1 + i % 2) + i * .38197f, 1);
                var origin = MoteOrigins[i];
                motes[i].rectTransform.anchoredPosition = new Vector2(
                    origin.x + (9 + i % 6) * Wave(phase, 2, i / 17f), -origin.y + (55 + i % 5 * 14) * age);
                motes[i].color = new Color(.93f, .74f, 1, .65f * Fade(age));
            }
        }
    }
}

