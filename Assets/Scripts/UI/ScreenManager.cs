using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MergeGame.UI
{
    public enum Screen
    {
        None,
        Title,
        Gameplay,
        Results,
        Leaderboard,
        Settings
    }

    /// <summary>
    /// Manages screen transitions with consistent fade + subtle scale.
    /// All transitions use the same animation — uniform and predictable.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [Header("Screens")]
        [SerializeField] private CanvasGroup titleScreen;
        [SerializeField] private CanvasGroup gameplayScreen;
        [SerializeField] private CanvasGroup resultsScreen;
        [SerializeField] private CanvasGroup leaderboardScreen;
        [SerializeField] private CanvasGroup settingsScreen;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 0.25f;
        [SerializeField] private float scaleFrom = 0.97f;

        public Screen CurrentScreen { get; private set; } = Screen.None;

        private Coroutine activeTransition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);

                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Ensure title screen is shown on startup, hiding all others
            ShowImmediate(Screen.Title);
        }

        public void TransitionTo(Screen screen)
        {
            if (screen == CurrentScreen) return;
            if (activeTransition != null) StopCoroutine(activeTransition);
            activeTransition = StartCoroutine(TransitionCoroutine(screen));
        }

        private IEnumerator TransitionCoroutine(Screen target)
        {
            CanvasGroup current = GetScreenGroup(CurrentScreen);
            CanvasGroup next = GetScreenGroup(target);

            // Activate next screen and ensure it starts invisible
            if (next != null)
            {
                next.gameObject.SetActive(true);
                next.alpha = 0f;
                // Put next on top so it fades in over current
                next.transform.SetAsLastSibling();
            }

            // Keep current at full alpha while next fades in on top
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float eased = 1f - (1f - t) * (1f - t);

                // Current stays fully opaque — no gap
                if (next != null)
                {
                    next.alpha = eased;
                    RectTransform rt = next.GetComponent<RectTransform>();
                    if (rt != null)
                        rt.localScale = Vector3.Lerp(Vector3.one * scaleFrom, Vector3.one, eased);
                }
                yield return null;
            }

            // Next is now fully visible — hide old screen
            if (current != null)
            {
                current.alpha = 0f;
                current.gameObject.SetActive(false);
            }
            if (next != null)
            {
                next.alpha = 1f;
                RectTransform rt = next.GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one;
            }

            CurrentScreen = target;
            activeTransition = null;
        }

        // Keep FadeOut for potential future use but it's no longer called in transitions
        private IEnumerator FadeOut(CanvasGroup group)
        {
            float elapsed = 0f;
            float half = transitionDuration * 0.5f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / half;
                group.alpha = 1f - t;
                yield return null;
            }
            group.alpha = 0f;
            group.gameObject.SetActive(false);
        }

        private IEnumerator FadeIn(CanvasGroup group)
        {
            group.gameObject.SetActive(true);
            group.alpha = 0f;

            RectTransform rt = group.GetComponent<RectTransform>();
            Vector3 targetScale = Vector3.one;

            if (rt != null) rt.localScale = Vector3.one * scaleFrom;

            float elapsed = 0f;
            float half = transitionDuration * 0.5f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / half;
                float eased = 1f - (1f - t) * (1f - t); // ease out
                group.alpha = eased;
                if (rt != null)
                    rt.localScale = Vector3.Lerp(Vector3.one * scaleFrom, targetScale, eased);
                yield return null;
            }
            group.alpha = 1f;
            if (rt != null) rt.localScale = targetScale;
        }

        private CanvasGroup GetScreenGroup(Screen screen)
        {
            switch (screen)
            {
                case Screen.Title: return titleScreen;
                case Screen.Gameplay: return gameplayScreen;
                case Screen.Results: return resultsScreen;
                case Screen.Leaderboard: return leaderboardScreen;
                case Screen.Settings: return settingsScreen;
                default: return null;
            }
        }

        /// <summary>Show a screen immediately without transition (for initial setup).</summary>
        public void ShowImmediate(Screen screen)
        {
            // Hide all
            SetGroupActive(titleScreen, false);
            SetGroupActive(gameplayScreen, false);
            SetGroupActive(resultsScreen, false);
            SetGroupActive(leaderboardScreen, false);
            SetGroupActive(settingsScreen, false);

            CurrentScreen = screen;
            var group = GetScreenGroup(screen);
            SetGroupActive(group, true);
        }

        private void SetGroupActive(CanvasGroup group, bool active)
        {
            if (group == null) return;
            group.gameObject.SetActive(active);
            group.alpha = active ? 1f : 0f;
        }
    }
}
