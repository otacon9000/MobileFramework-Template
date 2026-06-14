using UnityEngine;

namespace FlappyClone.Gameplay
{
    /// <summary>
    /// Generates simple solid-colour sprites at runtime so the template ships with
    /// ZERO binary art assets. A single 1x1 white texture is created once and reused;
    /// every renderer tints it via SpriteRenderer.color and sizes it via
    /// Transform.localScale.
    ///
    /// For a real game, a fork would replace UnitSquare()/CreateSprite() with calls
    /// that load actual sprites (from Resources, Addressables, or serialized fields)
    /// — the rest of the gameplay code would not need to change.
    /// </summary>
    public static class SpriteFactory
    {
        private static Sprite _unitSquare;

        /// <summary>A 1x1 world-unit white sprite, created once and cached.</summary>
        public static Sprite UnitSquare()
        {
            if (_unitSquare != null)
            {
                return _unitSquare;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "FlappyWhitePixel",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            // pixelsPerUnit = 1 means the single pixel maps to exactly one world unit,
            // so Transform.localScale becomes the object's world size directly.
            _unitSquare = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            _unitSquare.name = "FlappyUnitSquare";
            return _unitSquare;
        }

        /// <summary>Creates a tinted sprite GameObject parented under <paramref name="parent"/>.</summary>
        public static GameObject CreateSprite(string name, Color color, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = UnitSquare();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }
    }
}
