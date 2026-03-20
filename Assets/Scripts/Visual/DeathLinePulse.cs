using UnityEngine;

namespace MergeGame.Visual
{
    /// <summary>
    /// Pixel-art dashed line that pulses with a slow brightness oscillation
    /// when a ball is near it.
    /// </summary>
    public class DeathLinePulse : MonoBehaviour
    {
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float minAlpha = 0.15f;
        [SerializeField] private float maxAlpha = 0.6f;

        private LineRenderer lineRenderer;
        private bool ballNear;
        private float pulsePhase;

        private Color baseColor = new Color(1f, 0.2f, 0.2f, 0.3f);

        private void Start()
        {
            lineRenderer = GetComponentInChildren<LineRenderer>();
        }

        private void Update()
        {
            if (lineRenderer == null) return;

            if (ballNear)
            {
                pulsePhase += Time.deltaTime * pulseSpeed;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(pulsePhase) + 1f) * 0.5f);
                Color c = baseColor;
                c.a = alpha;
                lineRenderer.startColor = c;
                lineRenderer.endColor = c;
            }
            else
            {
                pulsePhase = 0f;
                Color c = baseColor;
                c.a = minAlpha;
                lineRenderer.startColor = c;
                lineRenderer.endColor = c;
            }
        }

        public void SetBallNear(bool near)
        {
            ballNear = near;
        }
    }
}
