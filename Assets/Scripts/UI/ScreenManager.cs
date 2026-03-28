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
        Paused,         // overlay — paused menu over gameplay
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
        [SerializeField] private CanvasGroup pausedOverlay;

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

            // If a curtain transition is needed (Game involved), let the coroutine
            // handle overlay dismissal behind the opaque curtain to avoid flashes
            bool willUseCurtain = (screen == Screen.Game || BaseScreen == Screen.Game);
            if (!willUseCurtain)
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
            return screen == Screen.ResultOverlay || screen == Screen.ShareSheet || screen == Screen.Paused;
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
            SetGroupActive(pausedOverlay, false);

            // Legacy
            if (resultsScreen != null && resultsScreen != resultOverlay)
                SetGroupActive(resultsScreen, false);
        }

        // ───── Base screen transitions ─────

        private IEnumerator TransitionCoroutine(Screen target)
        {
            CanvasGroup next = GetScreenGroup(target);
            bool needsCurtain = (target == Screen.Game || BaseScreen == Screen.Game);

            if (needsCurtain)
            {
                // Fade to black, switch, fade from black — covers world-space content
                yield return FadeCurtain(0f, 1f, transitionDuration * 0.6f);
                // Everything hidden behind opaque curtain — safe to switch
                DismissAllOverlays();
                HideAllBaseScreens();
                SetGroupActive(next, true);
                CurrentScreen = target;
                BaseScreen = target;
                yield return FadeCurtain(1f, 0f, transitionDuration * 0.6f);
            }
            else
            {
                // Standard crossfade for UI-only screens
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

                HideAllBaseScreens();
                SetGroupActive(next, true);
                CurrentScreen = target;
                BaseScreen = target;
            }

            activeTransition = null;
        }

        // Full-screen black curtain for transitions involving world-space content
        private GameObject curtainGO;
        private Image curtainImage;

        private IEnumerator FadeCurtain(float fromAlpha, float toAlpha, float duration)
        {
            if (curtainGO == null)
            {
                curtainGO = new GameObject("TransitionCurtain");
                curtainGO.transform.SetParent(transform, false);
                var cRT = curtainGO.AddComponent<RectTransform>();
                cRT.anchorMin = Vector2.zero;
                cRT.anchorMax = Vector2.one;
                cRT.offsetMin = Vector2.zero;
                cRT.offsetMax = Vector2.zero;
                curtainImage = curtainGO.AddComponent<Image>();
                curtainImage.color = new Color(0.059f, 0.067f, 0.090f, 0f); // OC.bg
                curtainImage.raycastTarget = true; // block input during transition
            }

            curtainGO.SetActive(true);
            // Must be last sibling EVERY time — overlays may have been added on top since last use
            curtainGO.transform.SetAsLastSibling();

            // Ensure curtain starts at the correct alpha immediately to avoid flash
            curtainImage.color = new Color(
                curtainImage.color.r, curtainImage.color.g, curtainImage.color.b, fromAlpha);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t; // ease in
                curtainImage.color = new Color(
                    curtainImage.color.r, curtainImage.color.g, curtainImage.color.b,
                    Mathf.Lerp(fromAlpha, toAlpha, eased));
                yield return null;
            }

            curtainImage.color = new Color(
                curtainImage.color.r, curtainImage.color.g, curtainImage.color.b, toAlpha);

            if (toAlpha <= 0f)
                curtainGO.SetActive(false);
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
                case Screen.Paused:        return pausedOverlay;
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
