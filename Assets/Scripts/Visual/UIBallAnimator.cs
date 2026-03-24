using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.Visual
{
    /// <summary>
    /// Assigns a static waveform ball sprite to a UI Image.
    /// Uses a single pre-cached sprite per tier (no animation) for maximum performance.
    /// </summary>
    public class UIBallAnimator : MonoBehaviour
    {
        private static Sprite[] uiSpriteCache;
        private static bool[] uiCacheBuilt;

        public void Initialize(int ballTier, float radius)
        {
            var image = GetComponent<Image>();
            if (image == null) return;

            int tier = ballTier;
            EnsureCache();

            if (!uiCacheBuilt[tier])
                BuildSprite(tier, radius);

            if (uiSpriteCache[tier] != null)
            {
                image.sprite = uiSpriteCache[tier];
                image.color = Color.white;
            }
        }

        private static void EnsureCache()
        {
            if (uiSpriteCache == null)
            {
                uiSpriteCache = new Sprite[11];
                uiCacheBuilt = new bool[11];
            }
        }

        private static void BuildSprite(int tier, float radius)
        {
            uiCacheBuilt[tier] = true;
            var color = NeonBallRenderer.GetBallColor(tier);
            float phase = tier * 0.09f;
            var pixels = NeonBallRenderer.GenerateBallPixels(tier, color, radius, phase, out int texSize);

            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(pixels);
            tex.Apply(false, true);

            // PPU = texSize so sprite renders as 1 unit, Image scales it
            uiSpriteCache[tier] = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f), texSize);
        }
    }
}
