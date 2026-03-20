using UnityEngine;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.Visual
{
    /// <summary>
    /// Dashed death line that is invisible by default.
    /// Fades in when any ball is within proximity threshold below the line.
    /// Pulses faster as balls get closer.
    /// </summary>
    public class DeathLinePulse : MonoBehaviour
    {
        [SerializeField] private float basePulseSpeed = 2f;
        [SerializeField] private float maxPulseSpeed = 8f;
        [SerializeField] private float maxAlpha = 0.5f;
        [SerializeField] private float fadeSpeed = 3f;

        private LineRenderer[] dashRenderers;
        private float currentAlpha;
        private float pulsePhase;
        private Color baseColor = new Color(1f, 0.6f, 0.3f);

        private float proximityThreshold = 1.5f;
        private float deathLineY;

        private void Start()
        {
            // Find all dash LineRenderers (children of DeathLineVisual)
            dashRenderers = GetComponentsInChildren<LineRenderer>();

            // Initialize all invisible
            foreach (var lr in dashRenderers)
            {
                Color c = baseColor;
                c.a = 0f;
                lr.startColor = c;
                lr.endColor = c;
            }

            var configs = Resources.FindObjectsOfTypeAll<PhysicsConfig>();
            if (configs.Length > 0)
            {
                proximityThreshold = configs[0].deathLineProximityThreshold;
                deathLineY = configs[0].deathLineY;
            }
            else
            {
                deathLineY = transform.position.y;
            }
        }

        private void Update()
        {
            if (dashRenderers == null || dashRenderers.Length == 0) return;

            float closestDistance = float.MaxValue;
            var balls = FindObjectsByType<BallController>(FindObjectsSortMode.None);

            foreach (var ball in balls)
            {
                if (!ball.HasLanded) continue;
                float ballTop = ball.transform.position.y;
                if (ball.BallData != null)
                    ballTop += ball.BallData.radius;

                float dist = deathLineY - ballTop;
                if (dist >= 0f && dist < closestDistance)
                    closestDistance = dist;
                else if (dist < 0f)
                    closestDistance = 0f;
            }

            float targetAlpha = 0f;
            if (closestDistance <= proximityThreshold)
            {
                float proximity = 1f - (closestDistance / proximityThreshold);
                targetAlpha = maxAlpha * proximity;
            }

            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

            Color c = baseColor;
            if (currentAlpha > 0.01f)
            {
                float proximity01 = currentAlpha / maxAlpha;
                float pulseSpeed = Mathf.Lerp(basePulseSpeed, maxPulseSpeed, proximity01);
                pulsePhase += Time.deltaTime * pulseSpeed;
                float pulse = 0.7f + Mathf.Sin(pulsePhase) * 0.3f;
                c.a = currentAlpha * pulse;
            }
            else
            {
                c.a = 0f;
            }

            foreach (var lr in dashRenderers)
            {
                lr.startColor = c;
                lr.endColor = c;
            }
        }
    }
}
