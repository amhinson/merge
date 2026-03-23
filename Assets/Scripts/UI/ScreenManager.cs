using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MergeGame.UI
{
    public enum Screen
    {
        None,
        Onboarding,
        HomeFresh,
        HomePlayed,
        Game,
        ResultOverlay,  // overlay — keeps Game visible underneath
        ShareSheet,     // overlay — keeps underlying screen visible
        Settings,
        Leaderboard,

        // Legacy aliases (map to new screens internally)
        Title = HomeFresh,
        Gameplay = Game,
        Results = ResultOverlay
    }

    /// <summary>
    /// Manages screen transitions with fade + subtle scale.
    /// Supports overlays (ResultOverlay, ShareSheet) that layer on top of base screens.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [Header("Base Screens")]
        [SerializeField] private CanvasGroup onboardingScreen;
        [SerializeField] private CanvasGroup homeFreshScreen;
        [SerializeField] private CanvasGroup homePlayedScreen;
        [SerializeField] private CanvasGroup gameScreen;
        [SerializeField] private CanvasGroup settingsScreen;
        [SerializeField] private CanvasGroup leaderboardScreen;

        [Header("Overlays")]
        [SerializeField] private CanvasGroup resultOverlay;
        [SerializeField] private CanvasGroup shareSheet;

        [Header("Legacy (backward compat — optional)")]
        [SerializeField] private CanvasGroup titleScreen;
        [SerializeField] private CanvasGroup gameplayScreen;
        [SerializeField] private CanvasGroup resultsScreen;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 0.25f;
        [SerializeField] private float scaleFrom = 0.97f;

        public Screen CurrentScreen { get; private set; } = Screen.None;

        /// <summary>The base screen underneath any active overlay.</summary>
        public Screen BaseScreen { get; private set; } = Screen.None;

        private Coroutine activeTransition;

        // ───── Lifecycle ─────

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
            // Don't auto-show a screen — let GameManager / startup flow decide
        }

        // ───── Navigation ─────

        public void NavigateTo(Screen screen)
        {
            if (screen == CurrentScreen) return;

            if (IsOverlay(screen))
            {
                ShowOverlay(screen);
                return;
            }

            // Transitioning to a base screen — dismiss any overlays first
            DismissAllOverlays();

            if (activeTransition != null) StopCoroutine(activeTransition);
            activeTransition = StartCoroutine(TransitionCoroutine(screen));
        }

        /// <summary>Backward-compatible alias.</summary>
        public void TransitionTo(Screen screen) => NavigateTo(screen);

        /// <summary>Dismiss the topmost overlay, returning to the screen beneath.</summary>
        public void DismissOverlay()
        {
            if (IsOverlay(CurrentScreen))
            {
                var group = GetScreenGroup(CurrentScreen);
                if (group != null)
                    StartCoroutine(FadeOutOverlay(group, CurrentScreen));
            }
        }

        /// <summary>Show a screen immediately without animation.</summary>
        public void ShowImmediate(Screen screen)
        {
            DismissAllOverlays();
            HideAllBaseScreens();

            CurrentScreen = screen;
            BaseScreen = IsOverlay(screen) ? BaseScreen : screen;

            var group = GetScreenGroup(screen);
            SetGroupActive(group, true);
        }

        // ───── Overlay logic ─────

        private static bool IsOverlay(Screen screen)
        {
            return screen == Screen.ResultOverlay || screen == Screen.ShareSheet;
        }

        private void ShowOverlay(Screen screen)
        {
            // Keep current base screen visible
            if (!IsOverlay(CurrentScreen))
                BaseScreen = CurrentScreen;

            var group = GetScreenGroup(screen);
            if (group == null) return;

            CurrentScreen = screen;
            group.gameObject.SetActive(true);
            group.transform.SetAsLastSibling();

            if (activeTransition != null) StopCoroutine(activeTransition);
            activeTransition = StartCoroutine(FadeInOverlay(group));
        }

        private IEnumerator FadeInOverlay(CanvasGroup group)
        {
            group.alpha = 0f;
            group.interactable = true;
            group.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float eased = 1f - (1f - t) * (1f - t);
                group.alpha = eased;

                RectTransform rt = group.GetComponent<RectTransform>();
                if (rt != null)
                    rt.localScale = Vector3.Lerp(Vector3.one * 0.96f, Vector3.one, eased);

                yield return null;
            }

            group.alpha = 1f;
            var finalRT = group.GetComponent<RectTransform>();
            if (finalRT != null) finalRT.localScale = Vector3.one;
            activeTransition = null;
        }

        private IEnumerator FadeOutOverlay(CanvasGroup group, Screen overlayScreen)
        {
            float elapsed = 0f;
            float duration = transitionDuration * 0.8f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = 1f - t;
                yield return null;
            }

            group.alpha = 0f;
            group.gameObject.SetActive(false);

            // Return to base screen
            CurrentScreen = BaseScreen;
        }

        private void DismissAllOverlays()
        {
            SetGroupActive(resultOverlay, false);
            SetGroupActive(shareSheet, false);

            // Legacy
            if (resultsScreen != null && resultsScreen != resultOverlay)
                SetGroupActive(resultsScreen, false);
        }

        // ───── Base screen transitions ─────

        private IEnumerator TransitionCoroutine(Screen target)
        {
            CanvasGroup current = GetScreenGroup(CurrentScreen);
            CanvasGroup next = GetScreenGroup(target);

            if (next != null)
            {
                next.gameObject.SetActive(true);
                next.alpha = 0f;
                next.transform.SetAsLastSibling();
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float eased = 1f - (1f - t) * (1f - t);

                if (next != null)
                {
                    next.alpha = eased;
                    RectTransform rt = next.GetComponent<RectTransform>();
                    if (rt != null)
                        rt.localScale = Vector3.Lerp(Vector3.one * scaleFrom, Vector3.one, eased);
                }
                yield return null;
            }

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
            BaseScreen = target;
            activeTransition = null;
        }

        // ───── Screen lookup ─────

        private CanvasGroup GetScreenGroup(Screen screen)
        {
            switch (screen)
            {
                case Screen.Onboarding:    return onboardingScreen;
                case Screen.HomeFresh:     return homeFreshScreen ?? titleScreen;
                case Screen.HomePlayed:    return homePlayedScreen ?? homeFreshScreen ?? titleScreen;
                case Screen.Game:          return gameScreen ?? gameplayScreen;
                case Screen.ResultOverlay: return resultOverlay ?? resultsScreen;
                case Screen.ShareSheet:    return shareSheet;
                case Screen.Settings:      return settingsScreen;
                case Screen.Leaderboard:   return leaderboardScreen;
                default: return null;
            }
        }

        private void HideAllBaseScreens()
        {
            SetGroupActive(onboardingScreen, false);
            SetGroupActive(homeFreshScreen, false);
            SetGroupActive(homePlayedScreen, false);
            SetGroupActive(gameScreen, false);
            SetGroupActive(settingsScreen, false);
            SetGroupActive(leaderboardScreen, false);

            // Legacy
            SetGroupActive(titleScreen, false);
            SetGroupActive(gameplayScreen, false);
        }

        private void SetGroupActive(CanvasGroup group, bool active)
        {
            if (group == null) return;
            group.gameObject.SetActive(active);
            group.alpha = active ? 1f : 0f;
            group.interactable = active;
            group.blocksRaycasts = active;
        }
    }
}
