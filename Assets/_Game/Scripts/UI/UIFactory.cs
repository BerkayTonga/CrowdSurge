using UnityEngine;
using UnityEngine.UI;

namespace CrowdSurge
{
    /// <summary>Helpers that build uGUI elements in code. Uses a generated rounded sprite, so no UI art is required.</summary>
    public static class UIFactory
    {
        private static Sprite s_rounded;
        private static Font s_font;

        public static Font Font
        {
            get
            {
                if (s_font == null) s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return s_font;
            }
        }

        /// <summary>Anti-aliased 9-sliceable rounded rectangle.</summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (s_rounded != null) return s_rounded;

                const int size = 64;
                const float radius = 22f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "RoundedRect"
                };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius);
                    float py = Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius);
                    float dist = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f)) - radius;
                    byte a = (byte)(Mathf.Clamp01(0.5f - dist) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(pixels);
                tex.Apply(false, true);
                s_rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                return s_rounded;
            }
        }

        private static Sprite s_circle, s_star;

        public static Sprite CircleSprite
        {
            get
            {
                if (s_circle != null) return s_circle;
                const int size = 64;
                var tex = NewTexture(size, "Circle");
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) - size * 0.5f + 1f;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - d) * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                s_circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return s_circle;
            }
        }

        /// <summary>Five-pointed star, anti-aliased by 3x3 supersampling.</summary>
        public static Sprite StarSprite
        {
            get
            {
                if (s_star != null) return s_star;
                const int size = 96;
                var poly = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                    float r = (i % 2 == 0 ? 0.48f : 0.2f) * size;
                    poly[i] = new Vector2(size * 0.5f + Mathf.Cos(a) * r, size * 0.52f + Mathf.Sin(a) * r);
                }
                var tex = NewTexture(size, "Star");
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < 3; sy++)
                    for (int sx = 0; sx < 3; sx++)
                        if (Inside(poly, new Vector2(x + (sx + 0.5f) / 3f, y + (sy + 0.5f) / 3f))) hits++;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 9));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                s_star = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return s_star;
            }
        }

        private static Texture2D NewTexture(int size, string name)
            => new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = name };

        private static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Fill(Transform parent, string name) =>
            Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        /// <summary>Rect anchored to one point with a fixed size.</summary>
        public static RectTransform Anchored(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rt = Rect(parent, name, anchor, anchor, Vector2.zero, Vector2.zero);
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
        {
            var rt = Fill(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded)
            {
                img.sprite = RoundedSprite;
                img.type = Image.Type.Sliced;
            }
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool outline = true)
        {
            var rt = Fill(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontStyle = FontStyle.Bold;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.04f, 0.07f, 0.24f, 0.9f);
                o.effectDistance = new Vector2(4f, -5f);
            }
            return t;
        }

        public static Button MakeButton(Transform parent, string name, string text, Color color, Vector2 size, Vector2 anchor, Vector2 position, int fontSize = 56)
        {
            var rt = Anchored(parent, name, anchor, new Vector2(0.5f, 0.5f), position, size);

            // Darker "shadow" slab gives the button a chunky, tappable look.
            var shadow = Panel(rt, "Shadow", Color.Lerp(color, Color.black, 0.45f));
            shadow.rectTransform.offsetMin = new Vector2(0f, -14f);
            shadow.rectTransform.offsetMax = new Vector2(0f, -14f);

            var face = Panel(rt, "Face", color);
            face.raycastTarget = true;

            // Glossy top half: the classic hyper-casual candy button.
            var shine = Panel(face.transform, "Shine", new Color(1f, 1f, 1f, 0.2f));
            shine.rectTransform.anchorMin = new Vector2(0f, 0.52f);
            shine.rectTransform.anchorMax = Vector2.one;
            shine.rectTransform.offsetMin = new Vector2(10f, 0f);
            shine.rectTransform.offsetMax = new Vector2(-10f, -9f);

            Label(face.transform, "Text", text, fontSize, Color.white);

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            var colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.highlightedColor = Color.white;
            button.colors = colors;
            return button;
        }
    }
}
