using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Shows a toast banner at the top of the gameplay screen during practice games.
    /// Auto-creates itself at runtime.
    /// </summary>
    public class PracticeToast : MonoBehaviour
    {
        public static PracticeToast Instance { get; private set; }

        private GameObject toastGO;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("PracticeToast");
            go.AddComponent<PracticeToast>();
        }

        public void Show()
        {
            if (toastGO == null) BuildToast();
            toastGO.SetActive(true);
            canvasGroup.alpha = 1f;
            StopAllCoroutines();
            StartCoroutine(ShowThenFade());
        }

        public void Hide()
        {
            if (toastGO != null) toastGO.SetActive(false);
        }

        private IEnumerator ShowThenFade()
        {
            // Stay visible for 3 seconds
            yield return new WaitForSeconds(3f);

            // Fade out over 0.5s
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / 0.5f);
                yield return null;
            }

            toastGO.SetActive(false);
        }

        private void BuildToast()
        {
            // Find the main canvas
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            toastGO = new GameObject("PracticeToastBanner");
            toastGO.transform.SetParent(canvas.transform, false);

            var rt = toastGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -(OS.safeAreaTop + 58));
            rt.sizeDelta = new Vector2(240, 28);

            // Background pill
            var bgImg = toastGO.AddComponent<Image>();
            bgImg.sprite = MurgeUI.SmoothRoundedRect;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(OC.amber.r, OC.amber.g, OC.amber.b, 0.15f);
            bgImg.raycastTarget = false;

            // Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(toastGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "PRACTICE - score won't count";
            tmp.font = MurgeUI.PressStart2P;
            tmp.fontSize = 6;
            tmp.color = OC.amber;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.characterSpacing = 1;
            tmp.raycastTarget = false;

            canvasGroup = toastGO.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Ensure it renders on top
            var toastCanvas = toastGO.AddComponent<Canvas>();
            toastCanvas.overrideSorting = true;
            toastCanvas.sortingOrder = 50;
            toastGO.AddComponent<GraphicRaycaster>();

            toastGO.SetActive(false);
        }
    }
}
