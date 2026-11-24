using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Sunflower (phyllotaxis) packing: any unit count forms a compact, natural-looking blob.</summary>
    public static class FormationLayout
    {
        private const float GoldenAngle = 2.39996323f;

        /// <summary>Unit-spacing offsets (x,z) for slot <paramref name="index"/>.</summary>
        public static Vector2 Slot(int index)
        {
            float r = Mathf.Sqrt(index + 0.5f);
            float a = index * GoldenAngle;
            return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }

        /// <summary>Spacing that keeps the blob inside <paramref name="maxRadius"/> for the given visible unit count.</summary>
        public static float Spacing(int visibleCount, float preferred, float maxRadius)
            => Mathf.Min(preferred, maxRadius / Mathf.Sqrt(Mathf.Max(1, visibleCount)));
    }
}
