using UnityEngine;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Displays a "x3 COMBO" indicator during chain merges.
    /// Pops in on chain start, pulses on each step, fades out when chain breaks.
    /// </summary>
    public class ComboUI : MonoBehaviour
    {
        private TextMeshProUGUI comboText;
        private CanvasGroup canvasGroup;
        private int displayedChain;
        private Coroutine fadeCoroutine;
        private Coroutine pulseCoroutine;

        private void Awake()
        {
            BuildUI();
            canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            if (MergeTracker.Instance != null)
            {
                MergeTracker.Instance.OnMerge += OnMerge;
                MergeTracker.Instance.OnChainComplete += OnChainComplete;
            }
        }

        private void OnDisable()
        {
            if (MergeTracker.Instance != null)
            {
                MergeTracker.Instance.OnMerge -= OnMerge;
                MergeTracker.Instance.OnChainComplete -= OnChainComplete;
            }
        }

        private void OnMerge(int tier, int chainLength, Vector3 worldPos)
        {
            if (chainLength < 2)
            {
                displayedChain = 0;
                return;
            }

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            displayedChain = chainLength;
            comboText.text = $"x{chainLength} COMBO";
            canvasGroup.alpha = 1f;

            // Pulse animation
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseCoroutine());

            // Auto-fade after timeout (chain might not formally "complete" if game ends)
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(AutoFadeCoroutine());
        }

        private void OnChainComplete(int finalLength)
        {
            if (displayedChain < 2) return;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutCoroutine());
        }

        private IEnumerator PulseCoroutine()
        {
            Vector3 baseScale = Vector3.one;
            transform.localScale = baseScale * 1.25f;

            float elapsed = 0f;
            const float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Ease out
                t = 1f - (1f - t) * (1f - t);
                transform.localScale = Vector3.Lerp(baseScale * 1.25f, baseScale, t);
                yield return null;
            }

            transform.localScale = baseScale;
        }

        private IEnumerator AutoFadeCoroutine()
        {
            // Wait for chain timeout + a little extra
            yield return new WaitForSeconds(1.3f);
            yield return FadeOutCoroutine();
        }

        private IEnumerator FadeOutCoroutine()
        {
            float start = canvasGroup.alpha;
            float elapsed = 0f;
            const float duration = 0.3f;
            Vector3 baseScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                canvasGroup.alpha = Mathf.Lerp(start, 0f, t);
                transform.localScale = Vector3.Lerp(baseScale, baseScale * 0.8f, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            transform.localScale = baseScale;
            displayedChain = 0;
        }

        public void ResetCombo()
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one;
            displayedChain = 0;
        }

        private void BuildUI()
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var dmMono = Resources.Load<TMP_FontAsset>("Fonts/DMMono-Medium SDF");

            comboText = gameObject.AddComponent<TextMeshProUGUI>();
            comboText.text = "";
            comboText.font = dmMono;
            comboText.fontSize = 14;
            comboText.color = OC.cyan;
            comboText.alignment = TextAlignmentOptions.Left;
            comboText.raycastTarget = false;
        }
    }
}
