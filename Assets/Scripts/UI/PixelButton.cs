using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MergeGame.Core;

namespace MergeGame.UI
{
    /// <summary>
    /// Pixel-art styled button with press animation.
    /// Slight scale down on press, bounce back on release. Quick and snappy.
    /// Hit area extends beyond visual for comfortable mobile tapping (min 44pt).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PixelButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float pressScale = 0.92f;
        [SerializeField] private float pressDuration = 0.06f;
        [SerializeField] private float releaseDuration = 0.1f;

        private Vector3 originalScale;
        private Coroutine animCoroutine;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            var btn = GetComponent<Button>();
            if (btn != null && !btn.interactable) return;

            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(ScaleTo(originalScale * pressScale, pressDuration));

            if (HapticManager.Instance != null)
                HapticManager.Instance.PlayUITap();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            var btn = GetComponent<Button>();
            if (btn != null && !btn.interactable) return;

            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(BounceBack());
        }

        private System.Collections.IEnumerator ScaleTo(Vector3 target, float duration)
        {
            Vector3 start = transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            transform.localScale = target;
        }

        private System.Collections.IEnumerator BounceBack()
        {
            // Overshoot slightly then settle
            Vector3 start = transform.localScale;
            Vector3 overshoot = originalScale * 1.04f;

            float elapsed = 0f;
            float half = releaseDuration * 0.5f;

            // Bounce up
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(start, overshoot, elapsed / half);
                yield return null;
            }

            // Settle
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(overshoot, originalScale, elapsed / half);
                yield return null;
            }
            transform.localScale = originalScale;
        }
    }
}
