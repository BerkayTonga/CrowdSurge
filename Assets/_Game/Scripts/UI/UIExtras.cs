using System;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdSurge
{
    /// <summary>Extra generated UI pieces: slider and scroll lists.</summary>
    public static class UIExtras
    {
        /// <summary>Horizontal slider 0..1 built from generated sprites.</summary>
        public static Slider MakeSlider(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, float value, Action<float> onChanged, Color fillColor)
        {
            var root = UIFactory.Anchored(parent, name, anchor, new Vector2(0.5f, 0.5f), position, size);
            var slider = root.gameObject.AddComponent<Slider>();

            var bg = UIFactory.Rect(root, "Background", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -11f), new Vector2(0f, 11f));
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = UIFactory.RoundedSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);

            var fillArea = UIFactory.Rect(root, "Fill Area", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(14f, -11f), new Vector2(-14f, 11f));
            var fill = UIFactory.Rect(fillArea, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.sprite = UIFactory.RoundedSprite;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = fillColor;

            var handleArea = UIFactory.Rect(root, "Handle Slide Area", Vector2.zero, Vector2.one, new Vector2(30f, 0f), new Vector2(-30f, 0f));
            var handle = UIFactory.Anchored(handleArea, "Handle", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.sprite = UIFactory.CircleSprite;
            handleImg.color = Color.white;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => onChanged(v));
            return slider;
        }

        /// <summary>Scroll view with a masked viewport and a content rect that grows with its children.</summary>
        public static ScrollRect MakeScroll(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, bool horizontal, out RectTransform content)
        {
            var root = UIFactory.Anchored(parent, name, anchor, new Vector2(0.5f, 0.5f), position, size);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            var rootImg = root.gameObject.AddComponent<Image>();
            rootImg.color = new Color(0f, 0f, 0f, 0.001f); // catches drags in empty areas

            var viewport = UIFactory.Fill(root, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();

            content = UIFactory.Rect(viewport, "Content", horizontal ? new Vector2(0f, 0f) : new Vector2(0f, 1f), horizontal ? new Vector2(0f, 1f) : new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            content.pivot = horizontal ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 1f);

            if (horizontal)
            {
                var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 24f;
                layout.padding = new RectOffset(30, 30, 10, 10);
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 16f;
                layout.padding = new RectOffset(0, 0, 10, 10);
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = horizontal;
            scroll.vertical = !horizontal;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 40f;
            return scroll;
        }
    }
}
