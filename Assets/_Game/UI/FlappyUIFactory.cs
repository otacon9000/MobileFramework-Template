using UnityEngine;
using UnityEngine.UI;

namespace FlappyClone.UI
{
    /// <summary>
    /// Builds uGUI elements from code. The template intentionally avoids
    /// prefab/scene-authored UI so each panel is fully self-contained and readable in
    /// a fork — you can see exactly what every panel draws. A single screen-space
    /// overlay Canvas hosts all panels.
    ///
    /// A real project would more likely author panels as prefabs and load them; the
    /// Core does not care either way, as long as the panels are registered with the
    /// UIManager (see FlappyInstaller).
    /// </summary>
    public static class FlappyUIFactory
    {
        public static Canvas CreateOverlayCanvas(string name)
        {
            var go = new GameObject(name);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Creates a stretched Text element anchored within its parent rect.</summary>
        public static Text CreateText(Transform parent, string content, int fontSize, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = DefaultFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Stretch(text.rectTransform, anchorMin, anchorMax);
            return text;
        }

        /// <summary>Creates a full-rect coloured background image.</summary>
        public static Image CreatePanelBackground(Transform parent, Color color)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = color;
            Stretch(image.rectTransform, Vector2.zero, Vector2.one);
            return image;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font DefaultFont()
        {
            // Unity 6 ships the legacy dynamic font under this name; older versions used
            // "Arial.ttf". Fall back so the template renders text on either.
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
