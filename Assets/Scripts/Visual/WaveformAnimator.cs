using UnityEngine;
using MergeGame.Data;

namespace MergeGame.Visual
{
    /// <summary>
    /// Animates the waveform inside a ball using pre-baked sprite frames.
    /// Caches frames per tier so all balls of the same tier share textures.
    /// Smooth scrolling via high frame count (48 frames per cycle).
    /// </summary>
    public class WaveformAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private BallData ballData;
        private float scrollOffset;
        private float scrollSpeed;
        private int tier;
        private float radius;
        private int lastFrame = -1;

        private const int FrameCount = 48; // frames per full scroll cycle
        private static Sprite[][] frameCache; // [tier][frame]
        private static bool[] cacheBuilt;

        public void Initialize(BallData data, Color color)
        {
            ballData = data;
            tier = data != null ? data.tierIndex : 0;
            radius = data != null ? data.radius : 0.5f;
            spriteRenderer = GetComponent<SpriteRenderer>();
            scrollOffset = Random.Range(0f, 1f);

            scrollSpeed = tier >= 0 && tier < NeonBallRenderer.ScrollSpeeds.Length
                ? NeonBallRenderer.ScrollSpeeds[tier]
                : 12.0f;

            EnsureCache();
            UpdateSprite();
        }

        private void Update()
        {
            if (spriteRenderer == null || ballData == null) return;

            scrollOffset += Time.deltaTime / scrollSpeed;
            scrollOffset %= 1.0f;

            UpdateSprite();
        }

        private void UpdateSprite()
        {
            int frame = Mathf.FloorToInt(scrollOffset * FrameCount) % FrameCount;
            if (frame == lastFrame) return; // no change, skip
            lastFrame = frame;

            EnsureCache();

            if (frameCache[tier] == null)
                BuildTierCache(tier, radius);

            if (frameCache[tier][frame] != null)
                spriteRenderer.sprite = frameCache[tier][frame];
        }

        private static void EnsureCache()
        {
            if (frameCache == null)
            {
                frameCache = new Sprite[11][];
                cacheBuilt = new bool[11];
            }
        }

        private static void BuildTierCache(int tier, float radius)
        {
            if (cacheBuilt[tier]) return;
            cacheBuilt[tier] = true;
            frameCache[tier] = new Sprite[FrameCount];

            var color = NeonBallRenderer.GetBallColor(tier);
            int diameter = Mathf.Max(8, Mathf.RoundToInt(radius * 2f * NeonBallRenderer.PixelsPerUnit));
            int size = diameter + 12; // padding

            for (int i = 0; i < FrameCount; i++)
            {
                float phase = (float)i / FrameCount;
                // Generate directly to texture (skip PNG encode/decode)
                var pixels = NeonBallRenderer.GenerateBallPixels(tier, color, radius, phase, out int texSize);
                var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.SetPixels(pixels);
                tex.Apply(false, true); // makeNoLongerReadable = true, saves memory

                frameCache[tier][i] = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                    new Vector2(0.5f, 0.5f), NeonBallRenderer.PixelsPerUnit);
            }
        }

        public static void ClearCache()
        {
            frameCache = null;
            cacheBuilt = null;
        }
    }
}
