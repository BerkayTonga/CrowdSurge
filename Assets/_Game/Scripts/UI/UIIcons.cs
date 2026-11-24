using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Procedurally drawn white icons (tinted by the Image colour), anti-aliased by 3x3 supersampling.</summary>
    public static class UIIcons
    {
        private static Sprite s_gear, s_bars, s_lock, s_help;

        public static Sprite Gear => s_gear != null ? s_gear : s_gear = Draw("Gear", (x, y) =>
        {
            float dx = x - 0.5f, dy = y - 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx);
            float tooth = Mathf.Cos(a * 8f) > 0.15f ? 0.11f : 0f;
            return r < 0.33f + tooth && r > 0.13f;
        });

        public static Sprite Bars => s_bars != null ? s_bars : s_bars = Draw("Bars", (x, y) =>
            (x > 0.14f && x < 0.34f && y > 0.12f && y < 0.50f) ||
            (x > 0.40f && x < 0.60f && y > 0.12f && y < 0.86f) ||
            (x > 0.66f && x < 0.86f && y > 0.12f && y < 0.66f));

        public static Sprite Lock => s_lock != null ? s_lock : s_lock = Draw("Lock", (x, y) =>
        {
            bool body = x > 0.2f && x < 0.8f && y > 0.1f && y < 0.52f;
            float dx = x - 0.5f, dy = y - 0.52f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            bool shackle = y >= 0.52f && r < 0.27f && r > 0.16f;
            return body || shackle;
        });

        /// <summary>Question mark drawn as an arc + stem + dot.</summary>
        public static Sprite Help => s_help != null ? s_help : s_help = Draw("Help", (x, y) =>
        {
            float dx = x - 0.5f, dy = y - 0.66f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            bool arc = r < 0.23f && r > 0.11f && !(dy < 0f && dx > 0f && dy > -0.16f);
            bool stem = x > 0.45f && x < 0.57f && y > 0.34f && y < 0.5f;
            float ddx = x - 0.51f, ddy = y - 0.2f;
            bool dot = ddx * ddx + ddy * ddy < 0.075f * 0.075f;
            return arc || stem || dot;
        });

        private static Sprite Draw(string name, System.Func<float, float, bool> inside)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = name };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < 3; sy++)
                for (int sx = 0; sx < 3; sx++)
                    if (inside((x + (sx + 0.5f) / 3f) / size, (y + (sy + 0.5f) / 3f) / size)) hits++;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 9));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
