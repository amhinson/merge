using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.Visual
{
    /// <summary>
    /// Animates a waveform ball rendered as a UI Image.
    /// Uses pre-baked frames shared per tier for performance.
    /// </summary>
    public class UIBallAnimator : MonoBehaviour
    {
        private Image image;
        private int tier;
        private float scrollOffset;
        private float scrollSpeed;
        private int lastFrame = -1;

        private const int FrameCount = 48;
        private static Sprite[][] uiFrameCache;
        private static bool[] uiCacheBuilt;
        private float uiRadius;

        public void Initialize(int ballTier, float radius)
        {
            image = GetComponent<Image>();
            tier = ballTier;
            uiRadius = radius;
            scrollOffset = Random.Range(0f, 1f);
            scrollSpeed = tier >= 0 && tier < NeonBallRenderer.ScrollSpeeds.Length
                ? NeonBallRenderer.ScrollSpeeds[tier]
                : 12f;

            EnsureCache();
            UpdateFrame();
        }

        private void Update()
        {
            if (image == null) return;

            scrollOffset += Time.deltaTime / scrollSpeed;
            scrollOffset %= 1.0f;

            UpdateFrame();
        }

        private void UpdateFrame()
        {
            int frame = Mathf.FloorToInt(scrollOffset * FrameCount) % FrameCount;
            if (frame == lastFrame) return;
            lastFrame = frame;

            EnsureCache();

            if (uiFrameCache[tier] == null)
                BuildCache(tier, uiRadius);

            if (uiFrameCache[tier] != null && uiFrameCache[tier][frame] != null)
            {
                image.sprite = uiFrameCache[tier][frame];
                image.color = Color.white;
            }
        }

        private static void EnsureCache()
        {
            if (uiFrameCache == null)
            {
                uiFrameCache = new Sprite[11][];
                uiCacheBuilt = new bool[11];
            }
        }

        private static void BuildCache(int tier, float radius)
        {
            if (uiCacheBuilt[tier]) return;
            uiCacheBuilt[tier] = true;
            uiFrameCache[tier] = new Sprite[FrameCount];

            var color = NeonBallRenderer.GetBallColor(tier);

            for (int i = 0; i < FrameCount; i++)
            {
                float phase = (float)i / FrameCount;
                var pixels = NeonBallRenderer.GenerateBallPixels(tier, color, radius, phase, out int texSize);
                var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.SetPixels(pixels);
                tex.Apply(false, true); // makeNoLongerReadable saves memory

                // PPU = texSize so sprite renders as 1 unit, Image scales it
                uiFrameCache[tier][i] = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                    new Vector2(0.5f, 0.5f), texSize);
            }
        }
    }
}
