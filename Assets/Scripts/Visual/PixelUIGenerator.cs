using UnityEngine;

namespace MergeGame.Visual
{
    /// <summary>
    /// Generates pixel-art UI element textures: rounded rect buttons, panels, icons.
    /// All use point filtering for crisp pixels.
    /// </summary>
    public static class PixelUIGenerator
    {
        /// <summary>
        /// Create a rounded rectangle sprite with pixel-art border, highlight, and shadow.
        /// </summary>
        public static Sprite CreateRoundedRect(int width, int height, int cornerRadius,
            Color fill, Color border, Color highlight, Color shadow)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            // Clear
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
            tex.SetPixels(pixels);

            // Draw filled rounded rect
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsInsideRoundedRect(x, y, width, height, cornerRadius))
                        continue;

                    bool isBorder = !IsInsideRoundedRect(x, y, width, height, cornerRadius, 1);
                    bool isHighlight = !IsInsideRoundedRect(x, y, width, height, cornerRadius, 2)
                                       && (x <= 2 || y >= height - 3); // Top and left inner edge
                    bool isShadow = !IsInsideRoundedRect(x, y, width, height, cornerRadius, 2)
                                    && (x >= width - 3 || y <= 2); // Bottom and right inner edge

                    if (isBorder)
                        tex.SetPixel(x, y, border);
                    else if (isHighlight && y >= height - 3)
                        tex.SetPixel(x, y, highlight); // Top highlight
                    else if (isHighlight && x <= 2)
                        tex.SetPixel(x, y, highlight); // Left highlight
                    else if (isShadow && y <= 2)
                        tex.SetPixel(x, y, shadow); // Bottom shadow
                    else if (isShadow && x >= width - 3)
                        tex.SetPixel(x, y, shadow); // Right shadow
                    else
                        tex.SetPixel(x, y, fill);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), Mathf.Min(width, height));
        }

        /// <summary>
        /// Create a simple pixel icon (gear, trophy, home, X).
        /// Returns a small texture for use as a UI sprite.
        /// </summary>
        public static Sprite CreateGearIcon(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            // Simple 16x16 gear icon
            int s = size;
            ClearTexture(tex);

            // Draw a simple gear: circle with teeth
            float center = s / 2f;
            float outerR = s * 0.45f;
            float innerR = s * 0.25f;
            int teeth = 6;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // Teeth modulation
                    float toothWave = Mathf.Cos(angle * teeth);
                    float toothR = Mathf.Lerp(outerR * 0.75f, outerR, (toothWave + 1f) * 0.5f);

                    if (dist <= toothR && dist >= innerR)
                        tex.SetPixel(x, y, color);
                    else if (dist < innerR && dist > innerR * 0.5f)
                        tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        public static Sprite CreateTrophyIcon(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            int s = size;
            // Simple trophy shape: cup top, narrow stem, base
            int cupTop = s * 3 / 4;
            int cupBottom = s / 2;
            int stemTop = cupBottom;
            int stemBottom = s / 4;
            int baseY = stemBottom;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    bool draw = false;

                    // Cup
                    if (y >= cupBottom && y <= cupTop)
                    {
                        float progress = (float)(y - cupBottom) / (cupTop - cupBottom);
                        float halfWidth = Mathf.Lerp(s * 0.2f, s * 0.4f, progress);
                        if (Mathf.Abs(x - s / 2f) < halfWidth) draw = true;
                    }
                    // Stem
                    else if (y >= stemBottom && y < stemTop)
                    {
                        if (Mathf.Abs(x - s / 2f) < s * 0.08f) draw = true;
                    }
                    // Base
                    else if (y >= baseY - 2 && y < baseY)
                    {
                        if (Mathf.Abs(x - s / 2f) < s * 0.25f) draw = true;
                    }

                    if (draw) tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        public static Sprite CreateHomeIcon(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            int s = size;
            float center = s / 2f;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    bool draw = false;

                    // Roof (triangle top half)
                    if (y >= s / 2)
                    {
                        float roofProgress = (float)(y - s / 2) / (s / 2f);
                        float halfWidth = (1f - roofProgress) * s * 0.5f;
                        if (Mathf.Abs(x - center) < halfWidth && Mathf.Abs(x - center) > halfWidth - 2)
                            draw = true;
                    }
                    // House body (bottom half)
                    if (y < s / 2 && y > s / 8)
                    {
                        if (Mathf.Abs(x - center) < s * 0.35f &&
                            (Mathf.Abs(x - center) > s * 0.35f - 2 || y < s / 8 + 2))
                            draw = true;
                        // Door
                        if (Mathf.Abs(x - center) < s * 0.1f && y < s / 3)
                            draw = true;
                    }

                    if (draw) tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        public static Sprite CreateGameCenterIcon(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            int s = size;
            float center = s / 2f;

            // Draw 4 overlapping circles in a cluster pattern (Game Center logo style)
            float offset = s * 0.15f;
            float radius = s * 0.22f;

            Vector2[] centers = new Vector2[]
            {
                new Vector2(center - offset, center + offset), // top-left
                new Vector2(center + offset, center + offset), // top-right
                new Vector2(center - offset, center - offset), // bottom-left
                new Vector2(center + offset, center - offset), // bottom-right
            };

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    foreach (var c in centers)
                    {
                        float dx = x - c.x + 0.5f;
                        float dy = y - c.y + 0.5f;
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            tex.SetPixel(x, y, color);
                            break;
                        }
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        public static Sprite CreateBackIcon(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            ClearTexture(tex);

            int s = size;
            // Simple X
            for (int i = 0; i < s; i++)
            {
                for (int t = -1; t <= 1; t++)
                {
                    int y1 = i + t;
                    int y2 = s - 1 - i + t;
                    if (y1 >= 0 && y1 < s) tex.SetPixel(i, y1, color);
                    if (y2 >= 0 && y2 < s) tex.SetPixel(i, y2, color);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static bool IsInsideRoundedRect(int x, int y, int w, int h, int r, int inset = 0)
        {
            int ix = x;
            int iy = y;
            int iw = w - inset * 2;
            int ih = h - inset * 2;
            ix -= inset;
            iy -= inset;
            int ir = Mathf.Max(0, r - inset);

            if (ix < 0 || iy < 0 || ix >= iw || iy >= ih) return false;

            // Check corners
            if (ix < ir && iy < ir) // Bottom-left
                return (ix - ir) * (ix - ir) + (iy - ir) * (iy - ir) <= ir * ir;
            if (ix >= iw - ir && iy < ir) // Bottom-right
                return (ix - (iw - ir - 1)) * (ix - (iw - ir - 1)) + (iy - ir) * (iy - ir) <= ir * ir;
            if (ix < ir && iy >= ih - ir) // Top-left
                return (ix - ir) * (ix - ir) + (iy - (ih - ir - 1)) * (iy - (ih - ir - 1)) <= ir * ir;
            if (ix >= iw - ir && iy >= ih - ir) // Top-right
                return (ix - (iw - ir - 1)) * (ix - (iw - ir - 1)) + (iy - (ih - ir - 1)) * (iy - (ih - ir - 1)) <= ir * ir;

            return true;
        }

        /// <summary>
        /// Create the standard 9-slice RoundedRect sprite used by the Murge design system.
        /// 32x32 white, 8px corner radius. Tint via Image.color.
        /// Cached after first creation.
        /// </summary>
        public static Sprite GetRoundedRect9Slice()
        {
            if (cachedRoundedRect9Slice != null) return cachedRoundedRect9Slice;

            const int size = 32;
            const int radius = 8;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            ClearTexture(tex);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (IsInsideRoundedRect(x, y, size, size, radius))
                        tex.SetPixel(x, y, Color.white);
                }
            }

            tex.Apply();

            // 9-slice border = radius on all sides
            var border = new Vector4(radius, radius, radius, radius);
            cachedRoundedRect9Slice = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border
            );
            cachedRoundedRect9Slice.name = "RoundedRect9Slice";
            return cachedRoundedRect9Slice;
        }

        private static Sprite cachedRoundedRect9Slice;

        private static void ClearTexture(Texture2D tex)
        {
            Color[] clear = new Color[tex.width * tex.height];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
        }
    }
}
