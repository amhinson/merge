using UnityEngine;
using MergeGame.Data;

namespace MergeGame.Visual
{
    /// <summary>
    /// Animates the waveform inside a ball by regenerating the sprite with a shifting phase.
    /// Uses per-tier scroll speeds from the spec.
    /// </summary>
    public class WaveformAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private BallData ballData;
        private float scrollOffset;
        private float scrollSpeed;
        private float lastPhaseSnap;
        private int tier;
        private float radius;
        private Color neonColor;

        // Pre-baked sprites per phase step (shared across all balls of same tier)
        private static readonly int PhaseSteps = 16;
        private static Sprite[][] spriteCache;

        public void Initialize(BallData data, Color color)
        {
            ballData = data;
            tier = data != null ? data.tierIndex : 0;
            radius = data != null ? data.radius : 0.5f;
            neonColor = NeonBallRenderer.GetBallColor(tier);
            spriteRenderer = GetComponent<SpriteRenderer>();
            scrollOffset = Random.Range(0f, 1f); // random start phase

            // Per-tier scroll speed
            scrollSpeed = tier >= 0 && tier < NeonBallRenderer.ScrollSpeeds.Length
                ? NeonBallRenderer.ScrollSpeeds[tier]
                : 2.0f;

            // Initialize cache
            if (spriteCache == null)
                spriteCache = new Sprite[11][];
            if (spriteCache[tier] != null)
                spriteCache[tier] = null; // clear for new visuals

            UpdateSprite();
        }

        private void Update()
        {
            if (spriteRenderer == null || ballData == null) return;

            // Scroll the waveform
            scrollOffset += Time.deltaTime / scrollSpeed;
            scrollOffset %= 1.0f;

            // Snap to phase steps for performance (shared cache)
            int currentStep = Mathf.FloorToInt(scrollOffset * PhaseSteps) % PhaseSteps;

            if (currentStep != (int)lastPhaseSnap)
            {
                lastPhaseSnap = currentStep;
                UpdateSprite();
            }
        }

        private void UpdateSprite()
        {
            if (spriteCache[tier] == null)
                spriteCache[tier] = new Sprite[PhaseSteps];

            int step = Mathf.FloorToInt(scrollOffset * PhaseSteps) % PhaseSteps;

            if (spriteCache[tier][step] == null)
            {
                float stepPhase = (float)step / PhaseSteps;
                spriteCache[tier][step] = NeonBallRenderer.GenerateSpriteForRadius(tier, neonColor, radius, stepPhase);
            }

            spriteRenderer.sprite = spriteCache[tier][step];
        }

        /// <summary>Clear the cache (call when rebuilding scene).</summary>
        public static void ClearCache()
        {
            spriteCache = null;
        }
    }
}
