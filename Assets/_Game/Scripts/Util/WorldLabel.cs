using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Camera-facing 3D text with a soft drop shadow. Cheap alternative to a world-space canvas.</summary>
    public sealed class WorldLabel : MonoBehaviour
    {
        private static Font s_font;

        private TextMesh _text;
        private TextMesh _shadow;
        private Transform _cam;
        private string _current;
        private Color _color;

        public static WorldLabel Create(Transform parent, string name, float size, Color color, FontStyle style = FontStyle.Bold)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<WorldLabel>();
            label.Init(size, color, style);
            return label;
        }

        public void SetText(string value)
        {
            if (value == _current) return;
            _current = value;
            _text.text = value;
            _shadow.text = value;
        }

        public void SetColor(Color color)
        {
            _color = color;
            _text.color = color;
        }

        /// <summary>Fades text and shadow together (the colour set by <see cref="SetColor"/> keeps its own alpha as the maximum).</summary>
        public void SetAlpha(float alpha)
        {
            var c = _color;
            c.a *= alpha;
            _text.color = c;
            _shadow.color = new Color(0f, 0f, 0f, 0.55f * alpha);
        }

        public Color CurrentColor => _color;

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
        }

        private void Init(float size, Color color, FontStyle style)
        {
            if (s_font == null) s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _cam = Camera.main != null ? Camera.main.transform : null;
            _color = color;

            _shadow = CreateText("Shadow", size, new Color(0f, 0f, 0f, 0.55f), style);
            _shadow.transform.localPosition = new Vector3(0.05f * size, -0.05f * size, 0.02f);
            _text = CreateText("Text", size, color, style);
        }

        private TextMesh CreateText(string childName, float size, Color color, FontStyle style)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = s_font;
            tm.fontStyle = style;
            tm.fontSize = 96;
            tm.characterSize = size * 0.1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = s_font.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return tm;
        }

        private void LateUpdate()
        {
            if (_cam == null)
            {
                if (Camera.main == null) return;
                _cam = Camera.main.transform;
            }
            transform.rotation = _cam.rotation;
        }
    }
}
