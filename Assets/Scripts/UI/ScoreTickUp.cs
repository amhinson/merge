using UnityEngine;
using TMPro;
using System.Collections;

namespace MergeGame.UI
{
    /// <summary>
    /// Animates a score number ticking up digit by digit.
    /// </summary>
    public class ScoreTickUp : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private float tickDuration = 0.8f;

        private int displayedValue;
        private int targetValue;
        private Coroutine tickCoroutine;

        public void SetImmediate(int value)
        {
            if (tickCoroutine != null) { StopCoroutine(tickCoroutine); tickCoroutine = null; }
            displayedValue = value;
            targetValue = value;
            if (scoreText != null) scoreText.text = value.ToString();
        }

        public void AnimateTo(int value)
        {
            targetValue = value;
            if (!gameObject.activeInHierarchy)
            {
                // Can't start coroutine on inactive object — just set immediately
                SetImmediate(value);
                return;
            }
            if (tickCoroutine != null) StopCoroutine(tickCoroutine);
            tickCoroutine = StartCoroutine(TickCoroutine());
        }

        private IEnumerator TickCoroutine()
        {
            int start = displayedValue;
            int diff = targetValue - start;
            if (diff == 0) yield break;

            float elapsed = 0f;
            while (elapsed < tickDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tickDuration);
                // Ease out
                t = 1f - (1f - t) * (1f - t);
                displayedValue = start + Mathf.RoundToInt(diff * t);
                if (scoreText != null) scoreText.text = displayedValue.ToString();
                yield return null;
            }

            displayedValue = targetValue;
            if (scoreText != null) scoreText.text = targetValue.ToString();
        }
    }
}
