using UnityEngine;

namespace MergeGame.Visual
{
    /// <summary>
    /// Generates neon ball sprites with clean horizontal waveform patterns.
    /// Ball body only — glow is handled as a separate sprite by BallController.
    /// Pixel-art: point filtering, no anti-aliasing, no smooth gradients.
    /// </summary>
    public static class NeonBallRenderer
    {
        private const int BaseSize = 48;
        private const int PixelsPerUnit = 48;

        public static byte[] GenerateBallPNG(int tier, Color neonColor, float phase = 0f)
        {
            int size = BaseSize;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            int center = size / 2;
            int bodyRadius = size / 2 - 3;

            // Body: the full neon color, barely darkened
            Color bodyColor = neonColor;
            // Edge: very subtle darkening — just the outer 2px ring
            Color edgeColor = DarkenColor(neonColor, 0.08f);
            // Waveform: near-white, the brightest part
            Color waveColor = Color.Lerp(neonColor, Color.white, 0.65f);

            int waveLeft = center - bodyRadius + 3;
            int waveRight = center + bodyRadius - 3;
            int waveWidth = waveRight - waveLeft;
            float[] waveY = ComputeWaveform(tier, waveWidth, phase);

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    int dx = px - center;
                    int dy = py - center;
                    int distSq = dx * dx + dy * dy;

                    // Outside ball
                    if (distSq > bodyRadius * bodyRadius)
                    {
                        tex.SetPixel(px, py, Color.clear);
                        continue;
                    }

                    // Ball body
                    float dist = Mathf.Sqrt(distSq);
                    Color pixel = dist > bodyRadius - 2 ? edgeColor : bodyColor;

                    // Waveform — horizontal through center
                    if (px >= waveLeft && px < waveRight)
                    {
                        int wi = px - waveLeft;
                        int wavePixelY = center + Mathf.RoundToInt(waveY[wi]);

                        if (py == wavePixelY)
                        {
                            pixel = waveColor;
                        }
                        else if (Mathf.Abs(py - wavePixelY) == 1)
                        {
                            pixel = Color.Lerp(pixel, waveColor, 0.25f);
                        }
                    }

                    tex.SetPixel(px, py, pixel);
                }
            }

            tex.Apply();
            return tex.EncodeToPNG();
        }

        public static Sprite GenerateSprite(int tier, Color neonColor, float phase = 0f)
        {
            var tex = new Texture2D(BaseSize, BaseSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            byte[] png = GenerateBallPNG(tier, neonColor, phase);
            tex.LoadImage(png);
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, BaseSize, BaseSize),
                new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        // ===== WAVEFORM =====

        public static float[] ComputeWaveform(int tier, int width, float phase)
        {
            if (width <= 0) return new float[] { 0 };
            var wave = new float[width];
            float maxAmp = Mathf.Min(5f, width * 0.15f);

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
                case 3:
                {
                    float saw = ((t * 3f + phase) % 1f);
                    return (saw * 2f - 1f) * amp * 0.6f;
                }
                case 4:
                {
                    float v = ((t * 2.5f + phase) % 1f);
                    float tri = v < 0.5f ? v * 4f - 1f : 3f - v * 4f;
                    return tri * amp * 0.6f;
                }
                case 5: return (Mathf.Sin(x * 2f + p) + Mathf.Sin(x * 2.5f + p * 1.3f)) * 0.5f * amp * 0.7f;
                case 6:
                {
                    float mod = Mathf.Sin(x * 1f + p * 0.7f) * 1.5f;
                    return Mathf.Sin(x * 3f + p + mod) * amp * 0.65f;
                }
                case 7:
                    return (Mathf.Sin(x * 3f + p) * 0.4f
                          + Mathf.Sin(x * 7f + p * 1.5f) * 0.3f
                          + Mathf.Sin(x * 13f + p * 2f) * 0.2f) * amp;
                case 8:
                    return (Mathf.Sin(x * 1f + p) * 0.5f
                          + Mathf.Sin(x * 2f + p) * 0.25f
                          + Mathf.Sin(x * 3f + p) * 0.15f
                          + Mathf.Sin(x * 4f + p) * 0.1f) * amp;
                case 9:
                    return (Mathf.Sin(x * 1f + p) * 0.3f
                          + Mathf.Sin(x * 2.5f + p) * 0.25f
                          + Mathf.Sin(x * 4f + p * 1.2f) * 0.2f
                          + Mathf.Sin(x * 6f + p * 1.5f) * 0.12f
                          + Mathf.Sin(x * 9f + p * 2f) * 0.08f) * amp;
                case 10:
                {
                    float envelope = Mathf.Sin(t * Mathf.PI);
                    return (Mathf.Sin(x * 3f + p) * 0.7f
                          + Mathf.Sin(x * 5f + p * 1.5f) * 0.3f) * envelope * amp * 0.7f;
                }
                default: return Mathf.Sin(x + p) * amp * 0.5f;
            }
        }

        private static Color DarkenColor(Color c, float amount)
        {
            return new Color(c.r * (1f - amount), c.g * (1f - amount), c.b * (1f - amount), c.a);
        }
    }
}
