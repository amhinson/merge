using UnityEngine;
using System.Collections.Generic;

namespace MergeGame.Visual
{
    /// <summary>
    /// Generates pixel-art styled circle sprites at a consistent pixel scale.
    /// All sprites use point (nearest-neighbor) filtering to keep pixels crisp.
    /// </summary>
    public static class PixelBallRenderer
    {
        // Base pixel grid size for the smallest ball. Larger balls scale up.
        private const int BasePixelSize = 32;
        private const int PixelsPerUnit = 32;

        private static Dictionary<int, Sprite> spriteCache = new Dictionary<int, Sprite>();

        /// <summary>
        /// Generate a pixel-art circle sprite for a given tier.
        /// 3-shade shading: highlight (top-left), base (middle), shadow (bottom-right).
        /// </summary>
        public static Sprite GenerateBallSprite(int tier, Color baseColor)
        {
            if (spriteCache.TryGetValue(tier, out Sprite cached))
                return cached;

            int size = BasePixelSize;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point; // Nearest-neighbor — pixels stay crisp
            tex.wrapMode = TextureWrapMode.Clamp;

            Color highlight = LightenColor(baseColor, 0.35f);
            Color shadow = DarkenColor(baseColor, 0.35f);

            float center = size / 2f;
            float radius = size / 2f - 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Determine shade based on position relative to center
                    // Light comes from top-left
                    float lightAngle = (dx - dy) / (radius * 2f); // -1 to 1, top-left = positive
                    float distFromEdge = 1f - (dist / radius);

                    Color pixelColor;
                    if (lightAngle > 0.2f && distFromEdge > 0.15f)
                    {
                        // Highlight zone (top-left inner area)
                        pixelColor = highlight;
                    }
                    else if (lightAngle < -0.2f || distFromEdge < 0.15f)
                    {
                        // Shadow zone (bottom-right or near edge)
                        pixelColor = shadow;
                    }
                    else
                    {
                        // Base color
                        pixelColor = baseColor;
                    }

                    tex.SetPixel(x, y, pixelColor);
                }
            }

            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            spriteCache[tier] = sprite;
            return sprite;
        }

        /// <summary>
        /// Generate sprite for editor use and save to asset path.
        /// Returns the texture bytes as PNG.
        /// </summary>
        public static byte[] GenerateBallPNG(int tier, Color baseColor, int size = 32)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color highlight = LightenColor(baseColor, 0.35f);
            Color shadow = DarkenColor(baseColor, 0.35f);

            float center = size / 2f;
            float radius = size / 2f - 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float lightAngle = (dx - dy) / (radius * 2f);
                    float distFromEdge = 1f - (dist / radius);

                    Color pixelColor;
                    if (lightAngle > 0.2f && distFromEdge > 0.15f)
                        pixelColor = highlight;
                    else if (lightAngle < -0.2f || distFromEdge < 0.15f)
                        pixelColor = shadow;
                    else
                        pixelColor = baseColor;

                    tex.SetPixel(x, y, pixelColor);
                }
            }

            tex.Apply();
            return tex.EncodeToPNG();
        }

        public static void ClearCache()
        {
            spriteCache.Clear();
        }

        private static Color LightenColor(Color c, float amount)
        {
            return new Color(
                Mathf.Min(1f, c.r + amount),
                Mathf.Min(1f, c.g + amount),
                Mathf.Min(1f, c.b + amount),
                c.a
            );
        }

        private static Color DarkenColor(Color c, float amount)
        {
            return new Color(
                Mathf.Max(0f, c.r - amount),
                Mathf.Max(0f, c.g - amount),
                Mathf.Max(0f, c.b - amount),
                c.a
            );
        }
    }
}
