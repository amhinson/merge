using UnityEngine;
using UnityEngine.UI;
using MergeGame.Data;
using System.Collections;

namespace MergeGame.UI
{
    /// <summary>
    /// Next ball preview with idle float animation and drop transition.
    /// </summary>
    public class NextBallPreviewUI : MonoBehaviour
    {
        [SerializeField] private Image ballImage;
        [SerializeField] private float idleFloatSpeed = 1.5f;
        [SerializeField] private float idleFloatAmount = 3f; // pixels
#pragma warning disable CS0414
        [SerializeField] private float previewScale = 0.65f;
#pragma warning restore CS0414

        private Vector2 basePosition;
        private float floatPhase;
        private bool isAnimating;

        private void Start()
        {
            if (ballImage != null)
            {
                basePosition = ballImage.rectTransform.anchoredPosition;
                floatPhase = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private void Update()
        {
            if (ballImage == null || isAnimating) return;

            // Gentle idle float
            floatPhase += Time.deltaTime * idleFloatSpeed;
            float offset = Mathf.Sin(floatPhase) * idleFloatAmount;
            ballImage.rectTransform.anchoredPosition = basePosition + new Vector2(0, offset);
        }

        public void UpdateBall(BallData data)
        {
            if (ballImage == null || data == null) return;

            if (data.sprite != null)
                ballImage.sprite = data.sprite;
            ballImage.color = data.color;

            // Match approximate in-game visual size
            float size = 35f * (data.radius / 0.6f);
            size = Mathf.Clamp(size, 12f, 45f);
            ballImage.rectTransform.sizeDelta = new Vector2(size, size);
        }

        /// <summary>
        /// Animate the current preview out (to drop position) and fade in the new ball.
        /// </summary>
        public void TransitionToNext(BallData nextData)
        {
            StartCoroutine(TransitionCoroutine(nextData));
        }

        private IEnumerator TransitionCoroutine(BallData nextData)
        {
            isAnimating = true;

            // Quick fade out
            float elapsed = 0f;
            float duration = 0.12f;
            Color startColor = ballImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Color c = ballImage.color;
                c.a = 1f - t;
                ballImage.color = c;
                yield return null;
            }

            // Update to new ball
            UpdateBall(nextData);

            // Fade in
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Color c = ballImage.color;
                c.a = t;
                ballImage.color = c;
                yield return null;
            }

            Color final_ = ballImage.color;
            final_.a = 1f;
            ballImage.color = final_;

            isAnimating = false;
        }
    }
}
