using UnityEngine;
using UnityEditor;
using System.IO;

namespace MergeGame.Editor
{
    public static class IconGenerator
    {
        private static readonly Color BgColor = HexColor("0F1117");

        // Use tier 10 (cyan, the largest ball) for the icon
        private const int IconTier = 10;
        private const float GameRadius = 1.4f; // BallData tier 10

        [MenuItem("MergeGame/Generate App Icon", false, 40)]
        public static void GenerateIcons()
        {
            Directory.CreateDirectory("Assets/Icons");

            // Render a ball using the actual shader to a RenderTexture
            var ballTex = RenderBallWithShader(512);

            GenerateIcon(1024, "Assets/Icons/app_icon_1024.png", ballTex, 0.35f, false);
            GenerateIcon(180, "Assets/Icons/app_icon_180.png", ballTex, 0.35f, false);
            GenerateIcon(60, "Assets/Icons/app_icon_60.png", ballTex, 0.35f, false);
            GenerateIcon(432, "Assets/Icons/android_foreground.png", ballTex, 0.28f, true);
            GenerateBackground(432, "Assets/Icons/android_background.png");

            Object.DestroyImmediate(ballTex);

            AssetDatabase.Refresh();
            Debug.Log("App icons generated in Assets/Icons/");
            ConfigurePlayerIcon();
        }

        /// <summary>
        /// Renders a ball using the actual WaveformBall shader to capture
        /// the exact in-game look (scanlines, halo glow, wave thickness).
        /// </summary>
        private static Texture2D RenderBallWithShader(int size)
        {
            // Create the ball body texture (static, no wave — shader draws the wave)
            var ballColor = Visual.BallRenderer.GetBallColor(IconTier);
            var bodyPixels = Visual.BallRenderer.GenerateStaticBallPixels(
                IconTier, ballColor, GameRadius, out int texSize);

            var bodyTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            bodyTex.filterMode = FilterMode.Bilinear;
            bodyTex.SetPixels(bodyPixels);
            bodyTex.Apply();

            var bodySprite = Sprite.Create(bodyTex, new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f), Visual.BallRenderer.PixelsPerUnit);

            // Set up offscreen rendering
            var go = new GameObject("IconBall");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = bodySprite;

            // Apply the waveform shader
            var shader = Shader.Find("Murge/WaveformBall");
            if (shader != null)
            {
                var mat = new Material(shader);
                sr.material = mat;

                var wc = Visual.BallRenderer.WaveConfig[IconTier];
                float ballRadiusUV = Visual.BallRenderer.GetBallRadiusUV(GameRadius);
                float lineWidthNorm = 2.5f / ((GameRadius * 2f * Visual.BallRenderer.PixelsPerUnit / 2f) - 2f);
                float haloWidthNorm = 6.0f / ((GameRadius * 2f * Visual.BallRenderer.PixelsPerUnit / 2f) - 2f);

                mat.SetColor("_WaveColor", new Color(ballColor.r, ballColor.g, ballColor.b, wc.Item3));
                mat.SetFloat("_Freq", wc.Item1);
                mat.SetFloat("_WaveType", wc.Item2);
                mat.SetFloat("_Amp", 0.22f);
                mat.SetFloat("_Phase", 0.15f);
                mat.SetFloat("_LineWidth", lineWidthNorm);
                mat.SetFloat("_HaloWidth", haloWidthNorm);
                mat.SetFloat("_BallRadiusUV", ballRadiusUV);
            }

            // Render camera
            var camGO = new GameObject("IconCamera");
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = GameRadius * 1.15f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.cullingMask = ~0;
            camGO.transform.position = new Vector3(0, 0, -10);

            var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            // Read pixels
            RenderTexture.active = rt;
            var result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            result.Apply();
            RenderTexture.active = null;

            // Cleanup
            Object.DestroyImmediate(camGO);
            Object.DestroyImmediate(go);
            rt.Release();

            return result;
        }

        private static void GenerateIcon(int size, string path, Texture2D ballTex, float fraction, bool transparent)
        {
            int targetBallSize = Mathf.RoundToInt(size * fraction * 2f);
            var scaledBall = ScaleTexture(ballTex, targetBallSize, targetBallSize);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] output = new Color[size * size];
            Color bg = transparent ? Color.clear : BgColor;
            for (int i = 0; i < output.Length; i++)
                output[i] = bg;

            Color[] ballPixels = scaledBall.GetPixels();
            int offsetX = (size - targetBallSize) / 2;
            int offsetY = (size - targetBallSize) / 2;

            for (int y = 0; y < targetBallSize; y++)
            {
                for (int x = 0; x < targetBallSize; x++)
                {
                    int outX = x + offsetX;
                    int outY = y + offsetY;
                    if (outX < 0 || outX >= size || outY < 0 || outY >= size) continue;

                    Color ballPx = ballPixels[y * targetBallSize + x];
                    if (ballPx.a < 0.01f) continue;

                    Color dst = output[outY * size + outX];
                    float a = ballPx.a;
                    output[outY * size + outX] = new Color(
                        ballPx.r * a + dst.r * (1f - a),
                        ballPx.g * a + dst.g * (1f - a),
                        ballPx.b * a + dst.b * (1f - a),
                        Mathf.Max(dst.a, a)
                    );
                }
            }

            tex.SetPixels(output);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"Icon saved: {path} ({size}x{size})");
        }

        private static void GenerateBackground(int size, string path)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = BgColor;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"Android background saved: {path}");
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

            // Set Android icons
            var androidIcons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Android);
            if (androidIcons == null || androidIcons.Length == 0)
            {
                var sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
                androidIcons = new Texture2D[sizes.Length];
            }
            for (int i = 0; i < androidIcons.Length; i++)
                androidIcons[i] = iconTex;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, androidIcons);
#pragma warning restore CS0618

            // Android adaptive icon layers
            var fgTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/android_foreground.png");
            var bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/android_background.png");
            if (fgTex != null && bgTex != null)
            {
                // Ensure textures are importable
                foreach (var texPath in new[] { "Assets/Icons/android_foreground.png", "Assets/Icons/android_background.png" })
                {
                    var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
                    if (imp != null)
                    {
                        imp.textureType = TextureImporterType.Default;
                        imp.npotScale = TextureImporterNPOTScale.None;
                        imp.textureCompression = TextureImporterCompression.Uncompressed;
                        imp.mipmapEnabled = false;
                        imp.isReadable = true;
                        imp.SaveAndReimport();
                    }
                }

                fgTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/android_foreground.png");
                bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/android_background.png");

                var platform = UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
                var icons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, platform);
                foreach (var icon in icons)
                {
                    icon.SetTextures(bgTex, fgTex);
                }
                PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, platform, icons);
                Debug.Log("Android adaptive icon layers configured.");
            }

            Debug.Log("Player Settings icons configured.");
        }

        private static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            var rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            var result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out Color c);
            return c;
        }
    }
}
