using UnityEngine;
using MergeGame.Data;

namespace MergeGame.Visual
{
    /// <summary>
    /// Assigns a static waveform sprite to a ball's SpriteRenderer.
    /// Uses a single pre-cached sprite per tier (no animation) for maximum performance.
    /// Call PrewarmCache() during loading to avoid any runtime hitches.
    /// </summary>
    public class WaveformAnimator : MonoBehaviour
    {
        private static Sprite[] spriteCache; // [tier]

        // Default radii per tier (must match NeonBallRenderer / BallTierConfig)
        private static readonly float[] DefaultRadii =
            { 0.22f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1.10f, 1.20f, 1.40f };

        public void Initialize(BallData data, Color color)
        {
            int tier = data != null ? data.tierIndex : 0;
            float radius = data != null ? data.radius : 0.5f;
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            EnsureCache();

            if (spriteCache[tier] == null)
                BuildTierSprite(tier, radius);

            sr.sprite = spriteCache[tier];
        }

        /// <summary>
        /// Pre-generate sprites for all 11 tiers. Call during loading screen
        /// so gameplay has zero generation hitches.
        /// </summary>
        public static void PrewarmCache()
        {
            EnsureCache();
            for (int tier = 0; tier < 11; tier++)
            {
                if (spriteCache[tier] == null)
                {
                    float radius = tier < DefaultRadii.Length ? DefaultRadii[tier] : 0.5f;
                    BuildTierSprite(tier, radius);
                }
            }
        }

        private static void EnsureCache()
        {
            if (spriteCache == null)
                spriteCache = new Sprite[11];
        }

        private static void BuildTierSprite(int tier, float radius)
        {
            var color = NeonBallRenderer.GetBallColor(tier);
            float phase = tier * 0.09f; // slight offset per tier so they don't all look identical
            var pixels = NeonBallRenderer.GenerateBallPixels(tier, color, radius, phase, out int texSize);

            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(pixels);
            tex.Apply(false, true); // makeNoLongerReadable = true, saves memory

            spriteCache[tier] = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f), NeonBallRenderer.PixelsPerUnit);
        }

        public static void ClearCache()
        {
            spriteCache = null;
        }
    }
}
