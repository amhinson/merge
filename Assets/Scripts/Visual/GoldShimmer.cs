using UnityEngine;

namespace MergeGame.Visual
{
    /// <summary>
    /// Tier 11 gold ball shimmer effect.
    /// Cycles a few highlight pixels through brightness on a slow loop.
    /// Attach to Tier 11 balls at runtime.
    /// </summary>
    public class GoldShimmer : MonoBehaviour
    {
        [SerializeField] private float shimmerSpeed = 2f;
        [SerializeField] private float shimmerIntensity = 0.3f;

        private SpriteRenderer spriteRenderer;
        private Color baseColor;
        private float phase;

        private void Start()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                baseColor = spriteRenderer.color;
            phase = Random.Range(0f, Mathf.PI * 2f); // Randomize start phase
        }

        private void Update()
        {
            if (spriteRenderer == null) return;

            phase += Time.deltaTime * shimmerSpeed;
            float shimmer = Mathf.Sin(phase) * shimmerIntensity;

            spriteRenderer.color = new Color(
                Mathf.Clamp01(baseColor.r + shimmer),
                Mathf.Clamp01(baseColor.g + shimmer * 0.8f),
                Mathf.Clamp01(baseColor.b + shimmer * 0.3f),
                baseColor.a
            );
        }
    }
}
