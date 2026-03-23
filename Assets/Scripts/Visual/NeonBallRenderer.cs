using UnityEngine;

namespace MergeGame.Visual
{
    /// <summary>
    /// Generates waveform ball sprites matching the Overtone design spec.
    /// Each ball has: radial gradient background, circle stroke, pixel block waveform,
    /// specular highlight, and outer glow.
    /// </summary>
    public static class NeonBallRenderer
    {
        private const int PixelsPerUnit = 48;
        private const int Padding = 4; // extra space for glow

        // Ball colors per tier (0-indexed)
        public static readonly Color[] BallColors = new Color[]
        {
            HexColor("4DD9C0"), // 1: cyan
            HexColor("E8587A"), // 2: pink
            HexColor("F0B429"), // 3: amber
            HexColor("A78BFA"), // 4: violet
            HexColor("A3E635"), // 5: lime
            HexColor("38BDF8"), // 6: sky
            HexColor("FB923C"), // 7: orange
            HexColor("FB7185"), // 8: rose
            HexColor("4DD9C0"), // 9: cyan (same as 1)
            HexColor("F0B429"), // 10: amber (same as 3)
            HexColor("E8587A"), // 11: pink (same as 2)
        };

        // Per-tier waveform config: pixelW, pixelH, waveType, freq
        private static readonly (int pw, int ph, int waveType, int freq)[] WaveConfig = new[]
        {
            (6, 4, 0, 3),  // 1: Sine freq=3
            (5, 4, 0, 4),  // 2: Sine freq=4
            (4, 3, 2, 2),  // 3: Triangle freq=2
            (4, 3, 0, 5),  // 4: Sine freq=5
            (3, 3, 1, 3),  // 5: Sawtooth freq=3
            (3, 2, 0, 6),  // 6: Sine freq=6
            (3, 2, 3, 4),  // 7: Square freq=4
            (2, 2, 2, 3),  // 8: Triangle freq=3
            (2, 2, 0, 5),  // 9: Sine freq=5
            (2, 2, 1, 4),  // 10: Sawtooth freq=4
            (2, 2, 0, 3),  // 11: Sine freq=3
        };

        // Wave types: 0=Sine, 1=Sawtooth, 2=Triangle, 3=Square

        // Per-tier scroll speeds (seconds per full cycle)
        public static readonly float[] ScrollSpeeds = new[]
        {
            2.4f, 1.9f, 2.8f, 2.2f, 2.6f, 1.6f, 2.0f, 2.5f, 1.8f, 2.3f, 2.1f
        };

        public static Color GetBallColor(int tier)
        {
            return tier >= 0 && tier < BallColors.Length ? BallColors[tier] : Color.white;
        }

        // ===== Main generation =====

        public static byte[] GenerateBallPNG(int tier, Color neonColor, float radius, float phase = 0f)
        {
            Color ballColor = GetBallColor(tier);
            int diameter = Mathf.Max(8, Mathf.RoundToInt(radius * 2f * PixelsPerUnit));
            int size = diameter + Padding * 2;
            float r = diameter / 2f;
            float cx = size / 2f;
            float cy = size / 2f;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            // Clear
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            // === 1. Outer glow (slightly larger blurred circle behind the ball) ===
            float glowRadius = r + 3f;
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float dx = px - cx;
                    float dy = py - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > glowRadius + 3f) continue;
                    if (dist > r) // only glow OUTSIDE the ball body
                    {
                        float glowFade = 1f - Mathf.Clamp01((dist - r) / 5f);
                        float glowAlpha = glowFade * glowFade * 0.3f;
                        if (glowAlpha > 0.01f)
                            pixels[py * size + px] = new Color(ballColor.r, ballColor.g, ballColor.b, glowAlpha);
                    }
                }
            }

            // === 2. Ball body — opaque dark base + colored radial gradient ===
            Color baseFill = HexColor("161B24"); // matches grid background
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float dx = px - cx;
                    float dy = py - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > r) continue;

                    // Opaque dark base so grid doesn't show through
                    pixels[py * size + px] = baseFill;

                    // Colored radial gradient on top: brighter at top-left, darker at edges
                    float gradientT = Mathf.Clamp01(dist / r);
                    float topLeftBias = Mathf.Clamp01(1f - (dx + dy) / (r * 1.5f));
                    float alpha = Mathf.Lerp(0.45f, 0.10f, gradientT) * Mathf.Lerp(0.7f, 1f, topLeftBias);

                    Color bodyColor = new Color(ballColor.r, ballColor.g, ballColor.b, alpha);
                    pixels[py * size + px] = BlendOver(pixels[py * size + px], bodyColor);
                }
            }

            // === 3. Circle stroke ===
            float strokeWidth = Mathf.Max(1f, r * 0.035f);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float dx = px - cx;
                    float dy = py - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float edgeDist = Mathf.Abs(dist - r);
                    if (edgeDist < strokeWidth)
                    {
                        float strokeAlpha = (1f - edgeDist / strokeWidth) * 0.85f;
                        Color strokeColor = new Color(ballColor.r, ballColor.g, ballColor.b, strokeAlpha);
                        pixels[py * size + px] = BlendOver(pixels[py * size + px], strokeColor);
                    }
                }
            }

            // === 4. Pixel block waveform ===
            if (tier >= 0 && tier < WaveConfig.Length)
            {
                var wc = WaveConfig[tier];
                DrawPixelWaveform(pixels, size, cx, cy, r, ballColor, wc.pw, wc.ph, wc.waveType, wc.freq, phase);
            }

            // === 5. Specular highlight ===
            {
                float specCx = cx - r * 0.22f; // upper-left
                float specCy = cy + r * 0.30f;
                float specRx = r * 0.20f;
                float specRy = r * 0.10f;
                for (int py = 0; py < size; py++)
                {
                    for (int px = 0; px < size; px++)
                    {
                        float dx = px - cx;
                        float dy = py - cy;
                        if (dx * dx + dy * dy > r * r) continue; // clip to circle

                        float sx = (px - specCx) / specRx;
                        float sy = (py - specCy) / specRy;
                        float sd = sx * sx + sy * sy;
                        if (sd < 1f)
                        {
                            float specAlpha = (1f - sd) * 0.13f;
                            Color specColor = new Color(1f, 1f, 1f, specAlpha);
                            pixels[py * size + px] = BlendOver(pixels[py * size + px], specColor);
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex.EncodeToPNG();
        }

        // ===== Pixel block waveform =====

        private static void DrawPixelWaveform(Color[] pixels, int size,
            float cx, float cy, float r, Color ballColor,
            int pixelW, int pixelH, int waveType, int freq, float phase)
        {
            float diameter = r * 2f;
            int cols = Mathf.FloorToInt(diameter / pixelW);
            if (cols <= 0) return;

            float amp = r * 0.28f;
            float waveCenterY = cy - r * 0.22f; // wave sits in the lower portion (0.72 from top = 0.28 from center downward... cy - offset)

            // The spec says cy = radius * 0.72 from top. In pixel coords (y=0 is bottom in Unity tex):
            // topOfBall = cy + r, waveCenterFromTop = r * 0.72
            // waveCenterY = (cy + r) - r * 0.72 = cy + r * 0.28
            waveCenterY = cy + r * 0.28f;

            float startX = cx - (cols * pixelW) / 2f;

            for (int col = 0; col < cols; col++)
            {
                float t = (float)col / cols + phase;
                float waveY = waveCenterY + amp * GetWaveValue(waveType, freq, t);

                // Edge pixel (bright crest)
                DrawPixelRect(pixels, size, cx, cy, r, ballColor,
                    startX + col * pixelW + 0.5f,
                    waveY + 0.5f,
                    pixelW - 1, pixelH - 1,
                    0.95f);

                // Fill pixels below the edge, fading down
                int maxRows = Mathf.CeilToInt((cy + r - waveY) / pixelH);
                for (int row = 1; row < maxRows; row++)
                {
                    float fillY = waveY - row * pixelH; // going downward (lower y values)
                    if (fillY < cy - r) break;

                    float opacity = Mathf.Max(0.08f, 0.38f - row * 0.045f);
                    DrawPixelRect(pixels, size, cx, cy, r, ballColor,
                        startX + col * pixelW + 0.5f,
                        fillY + 0.5f,
                        pixelW - 1, pixelH - 1,
                        opacity);
                }
            }
        }

        private static void DrawPixelRect(Color[] pixels, int size,
            float circleCx, float circleCy, float circleR,
            Color ballColor, float rx, float ry, int rw, int rh, float opacity)
        {
            int x0 = Mathf.RoundToInt(rx);
            int y0 = Mathf.RoundToInt(ry);
            Color c = new Color(ballColor.r, ballColor.g, ballColor.b, opacity);

            for (int dy = 0; dy < rh; dy++)
            {
                for (int dx = 0; dx < rw; dx++)
                {
                    int px = x0 + dx;
                    int py = y0 - dy; // go downward
                    if (px < 0 || px >= size || py < 0 || py >= size) continue;

                    // Clip to circle
                    float ddx = px - circleCx;
                    float ddy = py - circleCy;
                    if (ddx * ddx + ddy * ddy > circleR * circleR) continue;

                    pixels[py * size + px] = BlendOver(pixels[py * size + px], c);
                }
            }
        }

        private static float GetWaveValue(int waveType, int freq, float t)
        {
            switch (waveType)
            {
                case 0: // Sine
                    return Mathf.Sin(freq * 2f * Mathf.PI * t);
                case 1: // Sawtooth
                    return 2f * ((freq * t) % 1f) - 1f;
                case 2: // Triangle
                    return 2f * Mathf.Abs(2f * ((freq * t + 0.25f) % 1f) - 1f) - 1f;
                case 3: // Square
                    return Mathf.Sin(freq * 2f * Mathf.PI * t) >= 0 ? 1f : -1f;
                default:
                    return Mathf.Sin(freq * 2f * Mathf.PI * t);
            }
        }

        // ===== Backward compat =====

        public static byte[] GenerateBallPNG(int tier, Color neonColor, float phase)
        {
            float[] defaultRadii = { 0.22f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1.10f, 1.20f, 1.40f };
            float radius = tier >= 0 && tier < defaultRadii.Length ? defaultRadii[tier] : 0.5f;
            return GenerateBallPNG(tier, neonColor, radius, phase);
        }

        public static (int spriteSize, int ppu) GetSpriteDimensions(float radius)
        {
            int diameter = Mathf.Max(8, Mathf.RoundToInt(radius * 2f * PixelsPerUnit));
            int size = diameter + Padding * 2;
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
            tex.filterMode = FilterMode.Bilinear;
            byte[] png = GenerateBallPNG(tier, neonColor, radius, phase);
            tex.LoadImage(png);
            tex.filterMode = FilterMode.Bilinear;

            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        // Waveform computation for external callers (WaveformAnimator)
        public static float[] ComputeWaveform(int tier, int width, float phase)
        {
            if (width <= 0) return new float[] { 0 };
            var wave = new float[width];
            int waveType = 0;
            int freq = 3;
            if (tier >= 0 && tier < WaveConfig.Length)
            {
                waveType = WaveConfig[tier].waveType;
                freq = WaveConfig[tier].freq;
            }
            float amp = width * 0.14f;
            for (int i = 0; i < width; i++)
            {
                float t = (float)i / width + phase;
                wave[i] = GetWaveValue(waveType, freq, t) * amp;
            }
            return wave;
        }

        // ===== Helpers =====

        private static Color BlendOver(Color bg, Color fg)
        {
            float a = fg.a + bg.a * (1f - fg.a);
            if (a < 0.001f) return Color.clear;
            float r = (fg.r * fg.a + bg.r * bg.a * (1f - fg.a)) / a;
            float g = (fg.g * fg.a + bg.g * bg.a * (1f - fg.a)) / a;
            float b = (fg.b * fg.a + bg.b * bg.a * (1f - fg.a)) / a;
            return new Color(r, g, b, a);
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out Color c);
            return c;
        }
    }
}
