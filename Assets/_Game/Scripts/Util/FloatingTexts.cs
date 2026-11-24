using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Pooled billboard popups ("+8", "x2", "-15") that rise, pop and fade. Zero allocations while playing.</summary>
    public sealed class FloatingTexts : MonoBehaviour
    {
        private const int PoolSize = 14;
        private const float Duration = 1.0f;

        private sealed class Item
        {
            public WorldLabel label;
            public float age = Duration + 1f;
            public Vector3 start;
            public float size;
        }

        private readonly Item[] _items = new Item[PoolSize];
        private int _next;

        public static FloatingTexts Create(Transform parent)
        {
            var go = new GameObject("FloatingTexts");
            go.transform.SetParent(parent, false);
            var ft = go.AddComponent<FloatingTexts>();
            for (int i = 0; i < PoolSize; i++)
            {
                var label = WorldLabel.Create(go.transform, "Popup", 1f, Color.white);
                label.SetVisible(false);
                ft._items[i] = new Item { label = label };
            }
            return ft;
        }

        /// <param name="size">Relative size (1 = normal).</param>
        public void Spawn(Vector3 position, string text, Color color, float size = 1f)
        {
            var item = _items[_next];
            _next = (_next + 1) % PoolSize;
            item.age = 0f;
            item.start = position;
            item.size = size;
            item.label.SetText(text);
            item.label.SetColor(color);
            item.label.SetVisible(true);
            item.label.transform.position = position;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _items.Length; i++)
            {
                var it = _items[i];
                if (it.age > Duration) continue;

                it.age += dt;
                float k = Mathf.Clamp01(it.age / Duration);
                if (k >= 1f) { it.label.SetVisible(false); it.age = Duration + 1f; continue; }

                float pop = k < 0.18f ? Mathf.Lerp(0.3f, 1.25f, k / 0.18f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((k - 0.18f) / 0.15f));
                it.label.transform.localScale = Vector3.one * (pop * it.size * 1.6f);
                it.label.transform.position = it.start + Vector3.up * (k * 2.4f);
                it.label.SetAlpha(k > 0.6f ? 1f - (k - 0.6f) / 0.4f : 1f);
            }
        }
    }
}
