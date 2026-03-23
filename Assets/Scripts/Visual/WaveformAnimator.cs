using UnityEngine;
using MergeGame.Data;

namespace MergeGame.Visual
{
    /// <summary>
    /// Animates the waveform inside a ball with perfectly smooth continuous scrolling.
    /// Regenerates the sprite every frame — no stepping or caching.
    /// </summary>
    public class WaveformAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private BallData ballData;
        private float scrollOffset;
        private float scrollSpeed;
        private int tier;
        private float radius;

        public void Initialize(BallData data, Color color)
        {
            ballData = data;
            tier = data != null ? data.tierIndex : 0;
            radius = data != null ? data.radius : 0.5f;
            spriteRenderer = GetComponent<SpriteRenderer>();
            scrollOffset = Random.Range(0f, 1f); // random start phase

            scrollSpeed = tier >= 0 && tier < NeonBallRenderer.ScrollSpeeds.Length
                ? NeonBallRenderer.ScrollSpeeds[tier]
                : 12.0f;

            // Generate initial sprite
            RegenerateSprite();
        }

        private void Update()
        {
            if (spriteRenderer == null || ballData == null) return;

            // Smooth continuous scroll — increment every frame
            scrollOffset += Time.deltaTime / scrollSpeed;
            scrollOffset %= 1.0f;

            // Regenerate every frame for perfectly smooth animation
            RegenerateSprite();
        }

        private void RegenerateSprite()
        {
            var color = NeonBallRenderer.GetBallColor(tier);
            spriteRenderer.sprite = NeonBallRenderer.GenerateSpriteForRadius(tier, color, radius, scrollOffset);
        }

        /// <summary>Clear any cached data (no-op now, kept for API compat).</summary>
        public static void ClearCache() { }
    }
}
