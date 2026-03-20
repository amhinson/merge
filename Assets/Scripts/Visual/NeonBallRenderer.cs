using UnityEngine;
using System.Collections.Generic;

namespace MergeGame.Visual
{
    /// <summary>
    /// Generates neon ball sprites at native display resolution per tier.
    /// Each tier gets its own sprite sized to its actual pixel count — no scaling.
    /// Uses midpoint circle algorithm, inner rim highlight, horizontal waveform.
    /// </summary>
    public static class NeonBallRenderer
    {
        // Base pixel scale — pixels per world unit. Must match the game's visual scale.
        // With ortho size 6 and 1920 reference height, this gives crisp pixels.
        private const int PixelsPerUnit = 48;

        // Padding pixels around the ball in the sprite (for rim highlight at edge)
        private const int Padding = 2;

        /// <summary>
        /// Generate a ball PNG at the correct native pixel size for a given radius.
        /// The sprite PPU is set so the ball renders at exactly `radius * 2` world units
        /// with localScale = 1 (no scaling).
        /// </summary>
        public static byte[] GenerateBallPNG(int tier, Color neonColor, float radius, float phase = 0f)
        {
            // Sprite size in pixels = diameter in world units * PPU + padding
            int diameter = Mathf.Max(8, Mathf.RoundToInt(radius * 2f * PixelsPerUnit));
            int size = diameter + Padding * 2;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            int center = size / 2;
            int bodyRadius = diameter / 2;

            Color bodyColor = neonColor;
            Color rimHighlight = Color.Lerp(neonColor, Color.white, 0.35f);
            Color rimHighlightBright = Color.Lerp(neonColor, Color.white, 0.50f);
            Color waveColor = GetComplementaryWaveColor(tier);

            // Clear
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Outer circle outline
            var outlinePixels = new HashSet<long>();
            MidpointCircle(center, center, bodyRadius, outlinePixels);

            // Inner rim (1px inside)
            var innerRimPixels = new HashSet<long>();
            if (bodyRadius > 3)
                MidpointCircle(center, center, bodyRadius - 1, innerRimPixels);

            // Fill interior
            var fillPixels = new HashSet<long>();
            FillCircleInterior(center, center, bodyRadius, outlinePixels, fillPixels, size);

            // Waveform
            int waveMargin = Mathf.Max(2, bodyRadius / 5);
            int waveLeft = center - bodyRadius + waveMargin;
            int waveRight = center + bodyRadius - waveMargin;
            int waveWidth = waveRight - waveLeft;
            float[] waveY = waveWidth > 0 ? ComputeWaveform(tier, waveWidth, phase) : new float[0];


            // Draw
            foreach (long key in fillPixels)
            {
                int px = (int)(key >> 16);
                int py = (int)(key & 0xFFFF);
                if (px < 0 || px >= size || py < 0 || py >= size) continue;

                Color pixel;

                if (outlinePixels.Contains(key))
                {
                    // Outer edge — body color (no dark outline)
                    pixel = bodyColor;
                }
                else if (innerRimPixels.Contains(key))
                {
                    // Inner rim highlight — top-left brighter
                    int dx = px - center;
                    int dy = py - center;
                    bool isTopLeft = (-dx - dy) > 0;
                    pixel = isTopLeft ? rimHighlightBright : rimHighlight;
                }
                else
                {
                    pixel = bodyColor;

                    // Waveform
                    if (px >= waveLeft && px < waveRight && waveWidth > 0)
                    {
                        int wi = px - waveLeft;
                        if (wi >= 0 && wi < waveY.Length)
                        {
                            int wavePixelY = center + Mathf.RoundToInt(waveY[wi]);
                            if (py == wavePixelY)
                                pixel = waveColor;
                            else if (Mathf.Abs(py - wavePixelY) == 1)
                                pixel = Color.Lerp(pixel, waveColor, 0.25f);
                        }
                    }
                }

                tex.SetPixel(px, py, pixel);
            }

            tex.Apply();
            return tex.EncodeToPNG();
        }

        /// <summary>
        /// Generate a ball PNG using the old signature (no radius) — uses a default size.
        /// Kept for backward compatibility with editor sprite generation.
        /// </summary>
        public static byte[] GenerateBallPNG(int tier, Color neonColor, float phase)
        {
            // Default radius lookup for editor generation
            float[] defaultRadii = { 0.22f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1.10f, 1.20f, 1.40f };
            float radius = tier >= 0 && tier < defaultRadii.Length ? defaultRadii[tier] : 0.5f;
            return GenerateBallPNG(tier, neonColor, radius, phase);
        }

        /// <summary>
        /// Get the sprite size and PPU for a given radius so the ball renders at localScale=1.
        /// </summary>
        public static (int spriteSize, int ppu) GetSpriteDimensions(float radius)
        {
            int diameter = Mathf.Max(8, Mathf.RoundToInt(radius * 2f * PixelsPerUnit));
            int size = diameter + Padding * 2;
            // PPU = sprite pixels / world units. We want the sprite to cover radius*2 world units.
            // So PPU = size / (radius * 2) ... but we need to account for padding.
            // Simpler: PPU = PixelsPerUnit, and the sprite naturally covers the right area.
            return (size, PixelsPerUnit);
        }

        public static Sprite GenerateSprite(int tier, Color neonColor, float phase = 0f)
        {
            float[] defaultRadii = { 0.22f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1.10f, 1.20f, 1.40f };
            float radius = tier >= 0 && tier < defaultRadii.Length ? defaultRadii[tier] : 0.5f;
            return GenerateSpriteForRadius(tier, neonColor, radius, phase);
        }

        public static Sprite GenerateSpriteForRadius(int tier, Color neonColor, float radius, float phase = 0f)
        {
            int diameter = Mathf.Max(8, Mathf.RoundToInt(radius * 2f * PixelsPerUnit));
            int size = diameter + Padding * 2;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            byte[] png = GenerateBallPNG(tier, neonColor, radius, phase);
            tex.LoadImage(png);
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        // ===== CIRCLE DRAWING =====

        private static void MidpointCircle(int cx, int cy, int r, HashSet<long> pixels)
        {
            if (r <= 0) return;
            int x = r, y = 0, err = 1 - r;
            while (x >= y)
            {
                DrawOctants(cx, cy, x, y, pixels);
                y++;
                if (err < 0)
                    err += 2 * y + 1;
                else
                {
                    x--;
                    err += 2 * (y - x) + 1;
                }
            }
        }

        private static void DrawOctants(int cx, int cy, int x, int y, HashSet<long> pixels)
        {
            SetPx(pixels, cx + x, cy + y); SetPx(pixels, cx - x, cy + y);
            SetPx(pixels, cx + x, cy - y); SetPx(pixels, cx - x, cy - y);
            SetPx(pixels, cx + y, cy + x); SetPx(pixels, cx - y, cy + x);
            SetPx(pixels, cx + y, cy - x); SetPx(pixels, cx - y, cy - x);
        }

        private static void SetPx(HashSet<long> set, int x, int y)
        {
            set.Add(((long)x << 16) | (long)(y & 0xFFFF));
        }

        private static void FillCircleInterior(int cx, int cy, int r, HashSet<long> outline, HashSet<long> fill, int size)
        {
            for (int py = cy - r; py <= cy + r; py++)
            {
                if (py < 0 || py >= size) continue;
                int left = size, right = -1;
                for (int px = cx - r; px <= cx + r; px++)
                {
                    if (px < 0 || px >= size) continue;
                    if (outline.Contains(((long)px << 16) | (long)(py & 0xFFFF)))
                    {
                        if (px < left) left = px;
                        if (px > right) right = px;
                    }
                }
                if (left <= right)
                    for (int px = left; px <= right; px++)
                        fill.Add(((long)px << 16) | (long)(py & 0xFFFF));
            }
        }

        // ===== WAVEFORM =====

        public static float[] ComputeWaveform(int tier, int width, float phase)
        {
            if (width <= 0) return new float[] { 0 };
            var wave = new float[width];
            float maxAmp = width * 0.25f; // Scale with ball size — no cap
            for (int i = 0; i < width; i++)
            {
                float t = (float)i / width;
                wave[i] = GetWaveValue(tier, t, phase, maxAmp);
            }
            return wave;
        }

        private static float GetWaveValue(int tier, float t, float phase, float amp)
        {
            float x = t * Mathf.PI * 2f;
            float p = phase * Mathf.PI * 2f;
            switch (tier)
            {
                case 0: return Mathf.Sin(x * 1f + p) * amp * 0.6f;
                case 1: return Mathf.Sin(x * 2f + p) * amp * 0.7f;
                case 2: return (Mathf.Sin(x * 2f + p) >= 0 ? 1f : -1f) * amp * 0.5f;
                case 3: return (((t * 3f + phase) % 1f) * 2f - 1f) * amp * 0.6f;
                case 4:
                {
                    float v = ((t * 2.5f + phase) % 1f);
                    return (v < 0.5f ? v * 4f - 1f : 3f - v * 4f) * amp * 0.6f;
                }
                case 5: return (Mathf.Sin(x * 2f + p) + Mathf.Sin(x * 2.5f + p * 1.3f)) * 0.5f * amp * 0.7f;
                case 6: return Mathf.Sin(x * 3f + p + Mathf.Sin(x * 1f + p * 0.7f) * 1.5f) * amp * 0.65f;
                case 7: return (Mathf.Sin(x * 3f + p) * 0.4f + Mathf.Sin(x * 7f + p * 1.5f) * 0.3f + Mathf.Sin(x * 13f + p * 2f) * 0.2f) * amp;
                case 8: // Tier 9 — Ring modulation: two frequencies multiplied, metallic/bell-like
                {
                    float carrier = Mathf.Sin(x * 3f + p);
                    float modulator = Mathf.Sin(x * 4.7f + p * 1.3f);
                    return carrier * modulator * amp * 0.85f;
                }

                case 9: // Pulse wave — rhythmic bursts with silence between, like a heartbeat
                {
                    float pulse = Mathf.Pow(Mathf.Abs(Mathf.Sin(x * 1.5f + p)), 8f); // Sharp peaks
                    float detail = Mathf.Sin(x * 7f + p * 2f) * 0.3f * pulse; // High freq only during pulse
                    return (pulse * 0.7f + detail) * amp;
                }

                case 10: // Tier 11 — Crown wave: three big sharp peaks like a crown, fills the space
                {
                    // Three tall spikes spread across the ball, with ripples between
                    float spike1 = Mathf.Exp(-Mathf.Pow((t - 0.2f) * 8f, 2f));
                    float spike2 = Mathf.Exp(-Mathf.Pow((t - 0.5f) * 8f, 2f)) * 1.3f;
                    float spike3 = Mathf.Exp(-Mathf.Pow((t - 0.8f) * 8f, 2f));
                    float spikes = (spike1 + spike2 + spike3);
                    // Animate: the spikes breathe up and down
                    float breath = Mathf.Sin(p * 0.5f) * 0.3f + 0.7f;
                    // Small ripple between the peaks
                    float ripple = Mathf.Sin(x * 6f + p) * 0.15f;
                    return (spikes * breath + ripple) * amp * 0.9f;
                }
                default: return Mathf.Sin(x + p) * amp * 0.5f;
            }
        }

        // Complementary waveform colors — opposite on color wheel from each tier's body
        private static readonly string[] WaveformHexColors = new[]
        {
            "FF4040", // Tier 1:  Cyan body → Warm red waveform
            "FF9F4D", // Tier 2:  Blue body → Warm orange
            "C0F65C", // Tier 3:  Violet body → Yellow-green
            "2DFF97", // Tier 4:  Magenta body → Green
            "6BFFB8", // Tier 5:  Pink body → Teal-green
            "45FFFF", // Tier 6:  Red body → Cyan
            "2D8AFF", // Tier 7:  Orange body → Blue
            "3345FF", // Tier 8:  Amber body → Indigo
            "FF39CD", // Tier 9:  Green body → Magenta
            "4D9FFF", // Tier 10: White body → Soft blue
            "7B00FF", // Tier 11: Gold body → Deep violet
        };

        private static Color GetComplementaryWaveColor(int tier)
        {
            if (tier >= 0 && tier < WaveformHexColors.Length)
            {
                ColorUtility.TryParseHtmlString($"#{WaveformHexColors[tier]}", out Color c);
                return c;
            }
            return Color.white;
        }

        private static Color DarkenColor(Color c, float amount)
        {
            return new Color(c.r * (1f - amount), c.g * (1f - amount), c.b * (1f - amount), c.a);
        }
    }
}
