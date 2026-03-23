using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace MergeGame.Editor
{
    public static class IconGenerator
    {
        // Pixel art scale: each "pixel" is this many real pixels
        private const int PixelScale = 6;

        // Colors
        private static readonly Color BgColor = HexColor("121218");
        private static readonly Color BallColor = HexColor("FF2D95");
        private static readonly Color WaveColor = HexColor("2DFF97");
        private static readonly Color RimHighlight = Color.Lerp(HexColor("FF2D95"), Color.white, 0.35f);
        private static readonly Color RimBright = Color.Lerp(HexColor("FF2D95"), Color.white, 0.50f);

        [MenuItem("MergeGame/Generate App Icon", false, 40)]
        public static void GenerateIcons()
        {
            GenerateIcon(1024, "Assets/Icons/app_icon_1024.png");
            GenerateIcon(180, "Assets/Icons/app_icon_180.png");
            GenerateIcon(60, "Assets/Icons/app_icon_60.png");

            AssetDatabase.Refresh();
            Debug.Log("App icons generated in Assets/Icons/");

            // Configure the 1024 icon in Player Settings
            ConfigurePlayerIcon();
        }

        private static void GenerateIcon(int outputSize, string path)
        {
            var tex = new Texture2D(outputSize, outputSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            // Determine pixel scale for this output size
            int ps = Mathf.Max(1, outputSize * PixelScale / 1024);

            // Art grid size
            int gridSize = outputSize / ps;
            int center = gridSize / 2;

            // Ball fills ~85% of the icon
            int ballRadius = (int)(gridSize * 0.425f);

            // Build circle outline with midpoint algorithm
            var outlinePixels = new HashSet<long>();
            MidpointCircle(center, center, ballRadius, outlinePixels);

            // Inner rim
            var rimPixels = new HashSet<long>();
            if (ballRadius > 3)
                MidpointCircle(center, center, ballRadius - 1, rimPixels);

            // Fill
            var fillPixels = new HashSet<long>();
            FillCircle(center, center, ballRadius, outlinePixels, fillPixels, gridSize);

            // Waveform (tier 4 = neon pink, sawtooth-like)
            int waveMargin = ballRadius / 5;
            int waveLeft = center - ballRadius + waveMargin;
            int waveRight = center + ballRadius - waveMargin;
            int waveWidth = waveRight - waveLeft;
            float maxAmp = waveWidth * 0.25f;
            float[] waveY = new float[waveWidth];
            for (int i = 0; i < waveWidth; i++)
            {
                float t = (float)i / waveWidth;
                float x = t * Mathf.PI * 2f;
                // Tier 5 (index 4) = triangle wave
                float v = ((t * 2.5f) % 1f);
                float tri = v < 0.5f ? v * 4f - 1f : 3f - v * 4f;
                waveY[i] = tri * maxAmp * 0.6f;
            }

            // Waveform thickness (in art pixels)
            int waveThick = Mathf.Max(1, outputSize >= 512 ? 2 : 1);

            // Render to pixel art grid, then scale up
            // First fill background
            Color[] pixels = new Color[outputSize * outputSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = BgColor;

            // Draw each art pixel as a ps x ps block
            foreach (long key in fillPixels)
            {
                int gx = (int)(key >> 16);
                int gy = (int)(key & 0xFFFF);
                if (gx < 0 || gx >= gridSize || gy < 0 || gy >= gridSize) continue;

                Color pixel;

                if (outlinePixels.Contains(key))
                {
                    pixel = BallColor;
                }
                else if (rimPixels.Contains(key))
                {
                    int dx = gx - center;
                    int dy = gy - center;
                    bool isTopLeft = (-dx - dy) > 0;
                    pixel = isTopLeft ? RimBright : RimHighlight;
                }
                else
                {
                    pixel = BallColor;

                    // Waveform
                    if (gx >= waveLeft && gx < waveRight)
                    {
                        int wi = gx - waveLeft;
                        int wavePixelY = center + Mathf.RoundToInt(waveY[wi]);

                        if (Mathf.Abs(gy - wavePixelY) < waveThick)
                            pixel = WaveColor;
                        else if (Mathf.Abs(gy - wavePixelY) < waveThick + 1)
                            pixel = Color.Lerp(BallColor, WaveColor, 0.25f);
                    }
                }

                // Fill the ps x ps block
                for (int by = 0; by < ps; by++)
                {
                    for (int bx = 0; bx < ps; bx++)
                    {
                        int px = gx * ps + bx;
                        int py = gy * ps + by;
                        if (px < outputSize && py < outputSize)
                            pixels[py * outputSize + px] = pixel;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            Debug.Log($"Icon saved: {path} ({outputSize}x{outputSize})");
        }

        private static void ConfigurePlayerIcon()
        {
            AssetDatabase.Refresh();

            string iconPath = "Assets/Icons/app_icon_1024.png";
            TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }

            // Load the texture
            Texture2D iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (iconTex == null)
            {
                Debug.LogWarning("Could not load icon texture for Player Settings");
                return;
            }

#pragma warning disable CS0618
            // Set iOS icons
            var iosIcons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.iOS);
            if (iosIcons == null || iosIcons.Length == 0)
            {
                var sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.iOS);
                iosIcons = new Texture2D[sizes.Length];
            }
            for (int i = 0; i < iosIcons.Length; i++)
                iosIcons[i] = iconTex;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, iosIcons);

            // Set default icons
            var defaultIcons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
            if (defaultIcons == null || defaultIcons.Length == 0)
                defaultIcons = new Texture2D[1];
            for (int i = 0; i < defaultIcons.Length; i++)
                defaultIcons[i] = iconTex;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, defaultIcons);
#pragma warning restore CS0618

            Debug.Log("Player Settings icons configured.");
        }

        // ===== Circle drawing (same as NeonBallRenderer) =====

        private static void MidpointCircle(int cx, int cy, int r, HashSet<long> pixels)
        {
            if (r <= 0) return;
            int x = r, y = 0, err = 1 - r;
            while (x >= y)
            {
                SetPx(pixels, cx + x, cy + y); SetPx(pixels, cx - x, cy + y);
                SetPx(pixels, cx + x, cy - y); SetPx(pixels, cx - x, cy - y);
                SetPx(pixels, cx + y, cy + x); SetPx(pixels, cx - y, cy + x);
                SetPx(pixels, cx + y, cy - x); SetPx(pixels, cx - y, cy - x);
                y++;
                if (err < 0) err += 2 * y + 1;
                else { x--; err += 2 * (y - x) + 1; }
            }
        }

        private static void SetPx(HashSet<long> set, int x, int y)
        {
            set.Add(((long)x << 16) | (long)(y & 0xFFFF));
        }

        private static void FillCircle(int cx, int cy, int r, HashSet<long> outline, HashSet<long> fill, int gridSize)
        {
            for (int py = cy - r; py <= cy + r; py++)
            {
                if (py < 0 || py >= gridSize) continue;
                int left = gridSize, right = -1;
                for (int px = cx - r; px <= cx + r; px++)
                {
                    if (px < 0 || px >= gridSize) continue;
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

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out Color c);
            return c;
        }
    }
}
