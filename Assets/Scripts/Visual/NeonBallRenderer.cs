using UnityEngine;

namespace MergeGame.Visual
{
    /// <summary>
    /// Generates waveform ball sprites matching the Overtone design.
    /// Each ball has: radial gradient body, circle stroke, and one of three wave styles
    /// (Thread/Dash/Dot) that scrolls slowly.
    /// </summary>
    public static class NeonBallRenderer
    {
        private const int PixelsPerUnit = 48;
        private const int Padding = 4;

        // Ball colors per tier (0-indexed). Tier 0 = smallest ball, tier 10 = largest.
        // Level 11 (smallest) → tier 0, Level 1 (largest) → tier 10
        public static readonly Color[] BallColors =
        {
            HexColor("E8587A"), // tier 0 (Level 11, smallest): pink
            HexColor("F0B429"), // tier 1 (Level 10): amber
            HexColor("4DD9C0"), // tier 2 (Level 9): cyan
            HexColor("FB7185"), // tier 3 (Level 8): rose
            HexColor("FB923C"), // tier 4 (Level 7): orange
            HexColor("38BDF8"), // tier 5 (Level 6): sky
            HexColor("A3E635"), // tier 6 (Level 5): lime
            HexColor("A78BFA"), // tier 7 (Level 4): violet
            HexColor("F0B429"), // tier 8 (Level 3): amber
            HexColor("E8587A"), // tier 9 (Level 2): pink
            HexColor("4DD9C0"), // tier 10 (Level 1, largest): cyan
        };

        // Per-tier config (tier 0=smallest to tier 10=largest): freq, waveType, lineOpacity
        // All use thread style. waveType: 0=sine, 1=sawtooth, 2=triangle, 3=square
        private static readonly (int freq, int waveType, float lineOpacity)[] WaveConfig =
        {
            (3, 0, 0.75f), // tier 0 (L11): Sine
            (4, 1, 0.68f), // tier 1 (L10): Sawtooth
            (5, 0, 0.75f), // tier 2 (L9):  Sine
            (3, 2, 0.70f), // tier 3 (L8):  Triangle
            (4, 3, 0.68f), // tier 4 (L7):  Square
            (6, 0, 0.75f), // tier 5 (L6):  Sine
            (3, 1, 0.70f), // tier 6 (L5):  Sawtooth
            (5, 0, 0.75f), // tier 7 (L4):  Sine
            (2, 2, 0.70f), // tier 8 (L3):  Triangle
            (4, 0, 0.75f), // tier 9 (L2):  Sine
            (3, 0, 0.75f), // tier 10 (L1): Sine
        };

        // Per-tier scroll speeds in seconds (tier 0=smallest to tier 10=largest)
        public static readonly float[] ScrollSpeeds =
        {
            13f, 10f, 14f, 12f, 15f, 13f, 11f, 16f, 10f, 14f, 12f
        };

        public static Color GetBallColor(int tier)
        {
            return tier >= 0 && tier < BallColors.Length ? BallColors[tier] : Color.white;
        }

        // ===== Main generation =====

        /// <summary>Generate ball pixels directly — no PNG encode/decode overhead.</summary>
        public static Color[] GenerateBallPixels(int tier, Color ballColor, float radius, float phase, out int texSize)
        {
            int diameter = Mathf.Max(8, Mathf.RoundToInt(radius * 2f * PixelsPerUnit));
            texSize = diameter + Padding * 2;
            return GenerateBallPixelsInternal(tier, ballColor, radius, phase, texSize, diameter);
        }

        public static byte[] GenerateBallPNG(int tier, Color ignored, float radius, float phase = 0f)
        {
            Color ballColor = GetBallColor(tier);
            int diameter = Mathf.Max(8, Mathf.RoundToInt(radius * 2f * PixelsPerUnit));
            int size = diameter + Padding * 2;
            var pixels = GenerateBallPixelsInternal(tier, ballColor, radius, phase, size, diameter);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex.EncodeToPNG();
        }

        private static Color[] GenerateBallPixelsInternal(int tier, Color ballColor, float radius, float phase, int size, int diameter)
        {
            float r = diameter / 2f;
            float cx = size / 2f;
            float cy = size / 2f;

            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color baseFill = HexColor("161B24"); // opaque dark base

            // Gradient center (top-left biased: 42% cx, 36% cy from top)
            float gradCx = cx - r * 0.16f;
            float gradCy = cy + r * 0.28f; // upper area (in Unity tex coords, +y is up)

            // === 1. Ball body — opaque base + radial gradient ===
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float dx = px - cx;
                    float dy = py - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > r) continue;

                    // Opaque dark base
                    pixels[py * size + px] = baseFill;

                    // Radial gradient from top-left center
                    float gdx = px - gradCx;
                    float gdy = py - gradCy;
                    float gradDist = Mathf.Sqrt(gdx * gdx + gdy * gdy);
                    float gradT = Mathf.Clamp01(gradDist / (r * 1.2f));

                    float alpha = Mathf.Lerp(0.72f, 0.32f, gradT);
                    Color grad = new Color(ballColor.r, ballColor.g, ballColor.b, alpha);
                    pixels[py * size + px] = BlendOver(pixels[py * size + px], grad);
                }
            }

            // === 1b. Scanlines (subtle CRT texture) ===
            Color scanColor = new Color(0, 0, 0, 0.10f);
            float scanSpacing = 3f;
            for (float sy = 0; sy < r * 2f; sy += scanSpacing)
            {
                float localY = sy - r; // -r to +r
                float chordHalf = Mathf.Sqrt(Mathf.Max(0, r * r - localY * localY));
                int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - chordHalf));
                int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(cx + chordHalf));
                int py = Mathf.RoundToInt(cy + localY);
                if (py < 0 || py >= size) continue;

                for (int px = x0; px <= x1; px++)
                    pixels[py * size + px] = BlendOver(pixels[py * size + px], scanColor);
            }

            // === 2. Circle stroke ===
            float strokeWidth = Mathf.Max(1.0f, r * 0.028f);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float dx = px - cx;
                    float dy = py - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float edgeDist = Mathf.Abs(dist - r);
                    if (edgeDist < strokeWidth && dist <= r + strokeWidth)
                    {
                        float strokeAlpha = (1f - edgeDist / strokeWidth) * 0.55f;
                        Color stroke = new Color(ballColor.r, ballColor.g, ballColor.b, strokeAlpha);
                        pixels[py * size + px] = BlendOver(pixels[py * size + px], stroke);
                    }
                }
            }

            // === 3. Wave (thread style for all balls) ===
            if (tier >= 0 && tier < WaveConfig.Length)
            {
                var wc = WaveConfig[tier];
                float amp = r * 0.22f;
                float haloW = Mathf.Max(2.5f, r * 0.10f);
                float lineW = Mathf.Max(0.8f, r * 0.032f);

                DrawThreadWave(pixels, size, cx, cy, r, ballColor,
                    wc.freq, wc.waveType, amp, phase, haloW, lineW, wc.lineOpacity);
            }

            return pixels;
        }

        // ===== Thread wave (all balls use this) =====

        private static void DrawThreadWave(Color[] pixels, int size, float cx, float cy, float r,
            Color ballColor, int freq, int waveType, float amp, float phase,
            float haloWidth, float lineWidth, float lineOpacity)
        {
            float startX = cx - r;
            float endX = cx + r;
            int sampleCount = Mathf.Max(16, Mathf.RoundToInt((endX - startX) * 2f));

            // Halo pass (wide, low opacity)
            DrawPolylineWave(pixels, size, cx, cy, r, ballColor, freq, waveType, amp, phase,
                startX, endX, sampleCount, haloWidth, 0.12f);

            // Line pass (thin, per-tier opacity)
            DrawPolylineWave(pixels, size, cx, cy, r, ballColor, freq, waveType, amp, phase,
                startX, endX, sampleCount, lineWidth, lineOpacity);
        }

        private static void DrawPolylineWave(Color[] pixels, int size, float cx, float cy, float r,
            Color ballColor, int freq, int waveType, float amp, float phase,
            float startX, float endX, int sampleCount, float strokeW, float opacity)
        {
            float ballWidth = endX - startX;
            Color lineColor = new Color(ballColor.r, ballColor.g, ballColor.b, opacity);

            for (int i = 0; i < sampleCount - 1; i++)
            {
                float x0 = Mathf.Lerp(startX, endX, (float)i / (sampleCount - 1));
                float x1 = Mathf.Lerp(startX, endX, (float)(i + 1) / (sampleCount - 1));
                float t0 = (x0 - startX) / ballWidth + phase;
                float t1 = (x1 - startX) / ballWidth + phase;
                float y0 = cy + amp * GetWaveValue(waveType, freq, t0);
                float y1 = cy + amp * GetWaveValue(waveType, freq, t1);

                DrawLineSegment(pixels, size, cx, cy, r, x0, y0, x1, y1, strokeW, lineColor);
            }
        }

        private static void DrawLineSegment(Color[] pixels, int size, float circleCx, float circleCy, float circleR,
            float x0, float y0, float x1, float y1, float width, Color color)
        {
            float halfW = width / 2f;
            int minPx = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) - halfW));
            int maxPx = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) + halfW));
            int minPy = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) - halfW));
            int maxPy = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) + halfW));

            float dx = x1 - x0;
            float dy = y1 - y0;
            float segLen = Mathf.Sqrt(dx * dx + dy * dy);
            if (segLen < 0.001f) return;

            for (int py = minPy; py <= maxPy; py++)
            {
                for (int px = minPx; px <= maxPx; px++)
                {
                    // Circle clip
                    float cdx = px - circleCx;
                    float cdy = py - circleCy;
                    if (cdx * cdx + cdy * cdy > circleR * circleR) continue;

                    // Distance from point to line segment
                    float t = Mathf.Clamp01(((px - x0) * dx + (py - y0) * dy) / (segLen * segLen));
                    float closestX = x0 + t * dx;
                    float closestY = y0 + t * dy;
                    float dist = Mathf.Sqrt((px - closestX) * (px - closestX) + (py - closestY) * (py - closestY));

                    if (dist < halfW)
                    {
                        float falloff = 1f - dist / halfW;
                        Color c = new Color(color.r, color.g, color.b, color.a * falloff);
                        pixels[py * size + px] = BlendOver(pixels[py * size + px], c);
                    }
                }
            }
        }

        // ===== Wave functions =====

        private static float GetWaveValue(int waveType, int freq, float t)
        {
            switch (waveType)
            {
                case 0: return Mathf.Sin(freq * 2f * Mathf.PI * t);
                case 1: return 2f * ((freq * t) % 1f) - 1f;
                case 2: return 2f * Mathf.Abs(2f * ((freq * t + 0.25f) % 1f) - 1f) - 1f;
                case 3: return Mathf.Sin(freq * 2f * Mathf.PI * t) >= 0 ? 1f : -1f;
                default: return Mathf.Sin(freq * 2f * Mathf.PI * t);
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
            return (diameter + Padding * 2, PixelsPerUnit);
        }

        public static Sprite GenerateSprite(int tier, Color neonColor, float phase = 0f)
        {
            float[] defaultRadii = { 0.22f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1.10f, 1.20f, 1.40f };
            float radius = tier >= 0 && tier < defaultRadii.Length ? defaultRadii[tier] : 0.5f;
            return GenerateSpriteForRadius(tier, neonColor, radius, phase);
        }

        public static Sprite GenerateSpriteForRadius(int tier, Color neonColor, float radius, float phase = 0f)
        {
            Color ballColor = GetBallColor(tier);
            var pixels = GenerateBallPixels(tier, ballColor, radius, phase, out int size);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        public static float[] ComputeWaveform(int tier, int width, float phase)
        {
            if (width <= 0) return new float[] { 0 };
            var wave = new float[width];
            int waveType = 0, freq = 3;
            if (tier >= 0 && tier < WaveConfig.Length)
            {
                waveType = WaveConfig[tier].waveType;
                freq = WaveConfig[tier].freq;
            }
            float amp = width * 0.11f;
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
            float rv = (fg.r * fg.a + bg.r * bg.a * (1f - fg.a)) / a;
            float gv = (fg.g * fg.a + bg.g * bg.a * (1f - fg.a)) / a;
            float bv = (fg.b * fg.a + bg.b * bg.a * (1f - fg.a)) / a;
            return new Color(rv, gv, bv, a);
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out Color c);
            return c;
        }
    }
}
