using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VeinVanguard
{
    internal sealed class MapFoliageGraphic : RawImage
    {
        float middle, tip;
        static readonly float[] Rows = { 0, 169f / 519, 317f / 519, 1 };

        public void Sway(float mid, float extra)
        {
            middle = mid;
            tip = mid + extra;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var bounds = rectTransform.rect;
            var uv = uvRect;
            for (int i = 0; i < Rows.Length; i++)
            {
                float t = Rows[i];
                float x = i == 0 ? tip : i == 1 ? middle : 0;
                float y = Mathf.Lerp(bounds.yMax, bounds.yMin, t);
                float v = Mathf.Lerp(uv.yMax, uv.yMin, t);
                mesh.AddVert(new Vector3(bounds.xMin + x, y), color, new Vector2(uv.xMin, v));
                mesh.AddVert(new Vector3(bounds.xMax + x, y), color, new Vector2(uv.xMax, v));
                if (i == 0) continue;
                int j = i * 2;
                mesh.AddTriangle(j - 2, j - 1, j);
                mesh.AddTriangle(j, j - 1, j + 1);
            }
        }
    }

    // The atlas regions and their map coordinates come from the Remotion cutouts.
    internal sealed class WorldMapAnimation
    {
        const float Width = 1684, Height = 1191, Duration = 12;
        const string ResourcePath = "AnimatedMap01/";

        [Serializable]
        sealed class Layout
        {
            public Region[] layers;
            public Anchor[] anchors;
            public Vector3[] route;
        }

        [Serializable]
        sealed class Anchor
        {
            public string key;
            public float x, y, rx, phase;
            public bool hover;
        }

        [Serializable]
        sealed class Region
        {
            public string key;
            public int left, top, width, height, page, atlasX, atlasY;
        }

        sealed class Layer
        {
            public RawImage image;
            public RectTransform rect;
            public Vector2 home;
            public Vector2 size, topLeft;
            public Rect uv;

            public void Move(float x = 0, float y = 0, float angle = 0, float sx = 1, float sy = 1)
            {
                rect.anchoredPosition = home + new Vector2(x, -y);
                rect.localRotation = Quaternion.Euler(0, 0, -angle);
                rect.localScale = new Vector3(sx, sy, 1);
            }

            public void Blink(float amount)
            {
                image.enabled = amount > 0;
                if (amount <= 0) return;
                rect.sizeDelta = new Vector2(size.x, size.y * amount);
                image.uvRect = new Rect(uv.x, uv.yMax - uv.height * amount, uv.width, uv.height * amount);
            }

            public void Tint(Color color, float alpha)
            {
                color.a = alpha;
                image.color = color;
            }
        }

        static readonly Vector2[] DustPositions =
        {
            new Vector2(94, 344), new Vector2(497, 236), new Vector2(612, 353), new Vector2(1119, 224),
            new Vector2(1212, 317), new Vector2(1550, 303), new Vector2(336, 729), new Vector2(473, 855),
            new Vector2(914, 707), new Vector2(1047, 851), new Vector2(1157, 554), new Vector2(1515, 691)
        };
        static readonly Color DustGold = new Color(1, .83f, .54f);
        static readonly Color DustCream = new Color(1, .95f, .80f);

        readonly RectTransform root;
        readonly Dictionary<string, Region> regions = new Dictionary<string, Region>();
        readonly Dictionary<string, Anchor> anchors = new Dictionary<string, Anchor>();
        readonly Dictionary<string, Layer> actors = new Dictionary<string, Layer>();
        readonly Texture2D[] pages;
        readonly MapFoliageGraphic foliage;
        readonly Layer ember;
        readonly Vector3[] route;
        readonly Layer sky, roadLight, cyclops, iris, mouth, cyclopsLid, burger, fries, burgerLeftLid, burgerRightLid;
        readonly Layer cigarette, smoke, smokeLeftLid, smokeRightLid, plant;
        readonly Layer pinkEdge, pinkRight, rightAntennae, pinkLeft, leftAntennae, pinkFront, pinkLid;
        readonly Layer[] dust = new Layer[DustPositions.Length];
        float seconds;

        public static WorldMapAnimation TryCreate(RectTransform map)
        {
            var existing = map.Find("Animated Map 01");
            if (existing) UnityEngine.Object.Destroy(existing.gameObject);

            var text = Resources.Load<TextAsset>(ResourcePath + "Layout");
            var ground = Resources.Load<Texture2D>(ResourcePath + "Atlas0");
            var actors = Resources.Load<Texture2D>(ResourcePath + "Atlas1");
            if (!text || !ground || !actors) return null;
            var layout = JsonUtility.FromJson<Layout>(text.text);
            if (layout?.anchors == null || layout.layers == null || layout.route == null || layout.route.Length < 2) return null;
            return new WorldMapAnimation(map, layout, new[] { ground, actors });
        }

        WorldMapAnimation(RectTransform map, Layout layout, Texture2D[] pages)
        {
            this.pages = pages;
            foreach (var anchor in layout.anchors) anchors.Add(anchor.key, anchor);
            route = layout.route;
            foreach (var region in layout.layers) regions.Add(region.key, region);

            root = new GameObject("Animated Map 01", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            root.SetParent(map, false);
            root.SetAsFirstSibling();
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0, 1);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(Width, Height);
            Fit(map);

            sky = Add("sky");
            Add("ground");
            roadLight = Add("glow", pivotX: 48, pivotY: 48);
            cyclops = Add("cyclops");
            mouth = Add("cyclops-mouth", cyclops);
            Add("cyclops-eye-base", cyclops);
            iris = Add("cyclops-iris", cyclops);
            cyclopsLid = Add("cyclops-lid", cyclops);
            burger = Add("burger-body");
            fries = Add("burger-fries", burger, 343, 426);
            burgerLeftLid = Add("burger-eye-left-lid", burger);
            burgerRightLid = Add("burger-eye-right-lid", burger);
            cigarette = Add("smoke-body");
            smoke = Add("smoke", cigarette, 1415, 314);
            smokeLeftLid = Add("smoke-eye-left-lid", cigarette);
            smokeRightLid = Add("smoke-eye-right-lid", cigarette);
            plant = Add("plant");
            pinkEdge = Add("pink-edge");
            pinkRight = Add("pink-right-body");
            rightAntennae = Add("pink-right-antennae", pinkRight, 1569, 780);
            pinkLeft = Add("pink-left-body");
            leftAntennae = Add("pink-left-antennae", pinkLeft, 1341, 817);
            pinkFront = Add("pink-front");
            pinkLid = Add("pink-front-lid", pinkFront);

            for (int i = 0; i < dust.Length; i++)
            {
                dust[i] = Add("glow");
                dust[i].image.name = "Plasma dust " + (i + 1);
                dust[i].rect.pivot = Vector2.one * .5f;
                dust[i].rect.sizeDelta = Vector2.one * (8 + i % 3 * 2);
            }

            ember = Add("glow", cigarette);
            ember.rect.pivot = Vector2.one * .5f;
            ember.rect.sizeDelta = new Vector2(13, 4);
            ember.rect.anchoredPosition = new Vector2(1434, -312) - cigarette.topLeft;
            foliage = (MapFoliageGraphic)plant.image;
            Array.Sort(layout.anchors, (a, b) => a.y.CompareTo(b.y));
            foreach (var anchor in layout.anchors)
            {
                var shadow = Add("glow");
                shadow.image.name = anchor.key + " contact shadow";
                shadow.rect.pivot = Vector2.one * .5f;
                shadow.rect.sizeDelta = new Vector2(anchor.rx * 2, 12);
                shadow.rect.anchoredPosition = new Vector2(anchor.x, -anchor.y);
                shadow.Tint(new Color(.20f, .09f, .09f), .33f);
                actors[anchor.key].rect.SetAsLastSibling();
            }
            Render();
        }

        Layer Add(string key, Layer parent = null, float? pivotX = null, float? pivotY = null)
        {
            var region = regions[key];
            var node = new GameObject(key, typeof(RectTransform), key == "plant" ? typeof(MapFoliageGraphic) : typeof(RawImage));
            var rect = (RectTransform)node.transform;
            var image = node.GetComponent<RawImage>();
            rect.SetParent(parent?.rect ?? root, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            anchors.TryGetValue(key, out var anchor);
            float x = pivotX ?? anchor?.x ?? region.left;
            float y = pivotY ?? anchor?.y ?? region.top;
            rect.pivot = new Vector2((x - region.left) / region.width, 1 - (y - region.top) / region.height);
            rect.sizeDelta = new Vector2(region.width, region.height);
            rect.anchoredPosition = new Vector2(x, -y) - (parent?.topLeft ?? Vector2.zero);
            Texture2D page = pages[region.page];
            image.texture = page;
            image.raycastTarget = false;
            image.uvRect = new Rect((float)region.atlasX / page.width, 1f - (float)(region.atlasY + region.height) / page.height,
                (float)region.width / page.width, (float)region.height / page.height);
            var layer = new Layer
            {
                image = image,
                rect = rect,
                home = rect.anchoredPosition,
                size = rect.sizeDelta,
                topLeft = new Vector2(region.left, -region.top),
                uv = image.uvRect
            };
            if (anchor != null) actors.Add(key, layer);
            return layer;
        }

        public void SetVisible(bool visible)
        {
            if (root && root.gameObject.activeSelf != visible) root.gameObject.SetActive(visible);
        }

        public void Dispose()
        {
            if (root) UnityEngine.Object.Destroy(root.gameObject);
        }

        void Fit(RectTransform map)
        {
            root.localScale = new Vector3(map.rect.width / Width, map.rect.height / Height, 1);
        }

        public void Tick(float delta)
        {
            seconds = Mathf.Repeat(seconds + delta, Duration);
            Render();
        }

        float Wave(int cycles, float offset = 0) => Mathf.Sin(2 * Mathf.PI * (seconds / Duration * cycles + offset));

        float BlinkAt(float center) => 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.042f, .18f, Mathf.Abs(seconds - center)));

        void Render()
        {
            float breath = Wave(3);
            mouth.Move(y: .75f * Wave(3, .17f));
            iris.Move(14 * Wave(2, .08f), 2 * breath);
            cyclopsLid.Blink(Mathf.Max(BlinkAt(3), BlinkAt(8.2f)));

            float burgerBlink = Mathf.Max(BlinkAt(1.15f), Mathf.Max(BlinkAt(5.98f), BlinkAt(10.6f)));
            burgerLeftLid.Blink(burgerBlink);
            burgerRightLid.Blink(burgerBlink);

            smoke.Move(angle: 8 * Wave(4), sy: 1.12f + .12f * Wave(4, .22f));
            smoke.Tint(Color.white, .89f + .10f * Wave(4, .2f));
            float smokeBlink = Mathf.Max(BlinkAt(2), Mathf.Max(BlinkAt(7.2f), BlinkAt(9.9f)));
            smokeLeftLid.Blink(smokeBlink);
            smokeRightLid.Blink(smokeBlink);

            foliage.Sway(1.3f * Wave(2), 2.5f * Wave(3, .19f));
            ember.Tint(new Color(1, .68f, .32f), .36f + .22f * Wave(7));
            pinkEdge.Move(y: -1.8f * (1 + Wave(3, anchors["pink-edge"].phase)));
            pinkRight.Move(y: -1.8f * (1 + Wave(3, anchors["pink-right-body"].phase)));
            rightAntennae.Move(angle: 2 * Wave(4, .47f));
            pinkLeft.Move(y: -1.8f * (1 + Wave(3, anchors["pink-left-body"].phase)));
            leftAntennae.Move(angle: 2 * Wave(5, .11f));
            pinkFront.Move(y: -1.8f * (1 + Wave(3, anchors["pink-front"].phase)));
            pinkLid.Blink(Mathf.Max(BlinkAt(4.08f), BlinkAt(9.25f)));

            float phase = seconds / Duration;
            float sample = phase * (route.Length - 1);
            int left = Mathf.Min((int)sample, route.Length - 2);
            Vector3 point = Vector3.Lerp(route[left], route[left + 1], sample - left);
            roadLight.rect.anchoredPosition = new Vector2(point.x, -point.y);
            roadLight.rect.sizeDelta = Vector2.one * (point.z * 2);
            float fade = Mathf.Min(Mathf.InverseLerp(0, .07f, phase), Mathf.InverseLerp(1, .91f, phase));
            roadLight.Tint(Color.white, Mathf.SmoothStep(0, .57f + .13f * Mathf.Cos(phase * 6 * Mathf.PI), fade));

            for (int i = 0; i < dust.Length; i++)
            {
                var origin = DustPositions[i];
                float travel = Mathf.Repeat(phase + i / 13f, 1);
                dust[i].Move(origin.x + 95 * (travel - .5f), origin.y - 2.5f * Wave(2, i / 17f));
                dust[i].Tint(i % 2 == 0 ? DustCream : DustGold, .29f * Mathf.Pow(Mathf.Sin(Mathf.PI * travel), 2));
            }
        }
    }
}

