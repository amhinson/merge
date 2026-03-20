using UnityEngine;
using MergeGame.Data;

namespace MergeGame.Visual
{
    /// <summary>
    /// Animates the waveform inside a neon ball by regenerating the sprite
    /// with a shifting phase offset. Computationally cheap — just shifts phase.
    /// Each instance gets a random offset so adjacent balls aren't in sync.
    /// </summary>
    public class WaveformAnimator : MonoBehaviour
    {
        [SerializeField] private float cycleTime = 2.5f; // Full cycle in seconds

        private SpriteRenderer spriteRenderer;
        private BallData ballData;
        private float phase;
        private float phaseOffset;
        private float lastPhaseSnap; // Only regenerate every few frames
        private int tier;
        private float radius;
        private Color neonColor;

        // Pre-baked sprites per phase step (shared across all balls of same tier)
        private static readonly int PhaseSteps = 16;
        private static Sprite[][] spriteCache; // [tier][phaseStep]

        public void Initialize(BallData data, Color color)
        {
            ballData = data;
            tier = data != null ? data.tierIndex : 0;
            radius = data != null ? data.radius : 0.5f;
            neonColor = color;
            spriteRenderer = GetComponent<SpriteRenderer>();
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            phase = phaseOffset;

            // Initialize or reset cache
            if (spriteCache == null)
                spriteCache = new Sprite[11][];
            // Clear this tier's cached sprites so new visuals take effect
            if (spriteCache[tier] != null)
                spriteCache[tier] = null;

            // Generate initial sprite
            UpdateSprite();
        }

        private void Update()
        {
            if (spriteRenderer == null || ballData == null) return;

            phase += (Time.deltaTime / cycleTime) * Mathf.PI * 2f;

            // Only update sprite every few frames for performance
            float phaseStep = (phase % (Mathf.PI * 2f)) / (Mathf.PI * 2f) * PhaseSteps;
            int currentStep = (int)phaseStep % PhaseSteps;

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

            int step = (int)((phase % (Mathf.PI * 2f)) / (Mathf.PI * 2f) * PhaseSteps) % PhaseSteps;

            if (spriteCache[tier][step] == null)
            {
                float stepPhase = (step / (float)PhaseSteps) * Mathf.PI * 2f;
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
