using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using MergeGame.Data;
using MergeGame.Backend;
using MergeGame.Visual;

namespace MergeGame.UI
{
    /// <summary>
    /// Modal overlay for signing in with Apple or Google.
    /// Self-managing singleton — call SignInSheet.Show() from anywhere.
    /// </summary>
    public class SignInSheet : MonoBehaviour
    {
        public static SignInSheet Instance { get; private set; }

        private GameObject overlayGO;
        private CanvasGroup canvasGroup;
        private bool isBuilt;

        /// <summary>Fired after successful sign-in with the provider name.</summary>
        public event Action<string> OnSignInComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("SignInSheet");
            go.AddComponent<SignInSheet>();
        }

        public void Show(Action<string> onComplete = null)
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            OnSignInComplete = onComplete;
            canvasGroup.interactable = true;
            overlayGO.SetActive(true);
            canvasGroup.alpha = 0f;
            StartCoroutine(FadeIn());
        }

        public void Hide()
        {
            if (overlayGO != null)
                StartCoroutine(FadeOut());
        }

        private System.Collections.IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / 0.2f);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private System.Collections.IEnumerator FadeOut()
        {
            float elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.15f);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            overlayGO.SetActive(false);
        }

        private void BuildUI()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            overlayGO = new GameObject("SignInOverlay");
            overlayGO.transform.SetParent(canvas.transform, false);

            var rt = overlayGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            canvasGroup = overlayGO.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;

            // Ensure on top
            var overlayCanvas = overlayGO.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 100;
            overlayGO.AddComponent<GraphicRaycaster>();

            // Scrim
            var scrimImg = overlayGO.AddComponent<Image>();
            scrimImg.color = new Color(0.031f, 0.031f, 0.055f, 0.92f);

            // Scrim tap to dismiss
            var scrimBtn = overlayGO.AddComponent<Button>();
            scrimBtn.onClick.AddListener(Hide);
            scrimBtn.targetGraphic = scrimImg;

            // Card
            var card = MurgeUI.CreateUIObject("Card", overlayGO.transform);
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(300, 240);

            // Card bg
            var cardBg = card.AddComponent<Image>();
            cardBg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            cardBg.type = Image.Type.Sliced;
            cardBg.color = OC.surface;
            cardBg.raycastTarget = true; // block scrim tap

            // Card border
            var border = MurgeUI.CreateUIObject("Border", card.transform);
            MurgeUI.StretchFill(border.GetComponent<RectTransform>());
            var bImg = border.AddComponent<Image>();
            bImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            bImg.type = Image.Type.Sliced;
            bImg.color = OC.border;
            bImg.raycastTarget = false;

            // Content VLG
            var content = MurgeUI.CreateUIObject("Content", card.transform);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
            cRT.offsetMin = new Vector2(20, 20); cRT.offsetMax = new Vector2(-20, -20);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            // Title
            var title = MurgeUI.CreateLabel(content.transform, "Connect Account",
                MurgeUI.PressStart2P, 11, Color.white, "Title");
            title.alignment = TextAlignmentOptions.Center;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;

            // Apple button (iOS) / Google button (Android) / both in editor
            // Platform-appropriate sign-in options
#if UNITY_IOS && !UNITY_EDITOR
            BuildProviderButton(content.transform, "Continue with Apple", "apple");
            BuildProviderButton(content.transform, "Continue with Google", "google");
#elif UNITY_ANDROID && !UNITY_EDITOR
            BuildProviderButton(content.transform, "Continue with Google", "google");
#else
            // Editor: show both for testing
            BuildProviderButton(content.transform, "Continue with Apple", "apple");
            BuildProviderButton(content.transform, "Continue with Google", "google");
#endif

            // Cancel
            var cancelGO = MurgeUI.CreateUIObject("Cancel", content.transform);
            var cancelLE = cancelGO.AddComponent<LayoutElement>();
            cancelLE.preferredHeight = 30;
            var cancelTMP = cancelGO.AddComponent<TextMeshProUGUI>();
            cancelTMP.text = "Cancel";
            cancelTMP.font = MurgeUI.DMMono;
            cancelTMP.fontSize = 12;
            cancelTMP.color = OC.muted;
            cancelTMP.alignment = TextAlignmentOptions.Center;
            var cancelBtn = cancelGO.AddComponent<Button>();
            cancelBtn.targetGraphic = cancelTMP;
            cancelBtn.onClick.AddListener(Hide);

            overlayGO.SetActive(false);
        }

        private void BuildProviderButton(Transform parent, string text, string provider)
        {
            bool isApple = provider == "apple";
            var btnGO = MurgeUI.CreateUIObject($"{provider}Btn", parent);
            var btnLE = btnGO.AddComponent<LayoutElement>();
            btnLE.preferredHeight = 44;

            // Apple: black bg, white text. Google: white bg, dark text.
            Color bgColor = isApple ? Color.black : Color.white;
            Color textColor = isApple ? Color.white : new Color(0.26f, 0.26f, 0.26f, 1f);

            // Rounded background
            var bgImg = btnGO.AddComponent<Image>();
            bgImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = bgColor;

            // Logo + text label
            var labelGO = MurgeUI.CreateUIObject("Label", btnGO.transform);
            MurgeUI.StretchFill(labelGO.GetComponent<RectTransform>());
            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            // Apple logo: use  (Apple logo in SF Pro/system font won't render in DMMono)
            // Use text-only for now — compliant per Apple guidelines if logo isn't available
            labelTMP.text = text;
            labelTMP.font = MurgeUI.DMMono;
            labelTMP.fontSize = 12;
            labelTMP.color = textColor;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.raycastTarget = false;

            // Border for Google button (Apple is borderless per spec)
            if (!isApple)
            {
                var borderGO = MurgeUI.CreateUIObject("Border", btnGO.transform);
                MurgeUI.StretchFill(borderGO.GetComponent<RectTransform>());
                var borderImg = borderGO.AddComponent<Image>();
                borderImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
                borderImg.type = Image.Type.Sliced;
                borderImg.color = new Color(0.46f, 0.46f, 0.46f, 0.5f);
                borderImg.raycastTarget = false;
                borderGO.transform.SetAsFirstSibling(); // behind the text
            }

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            btn.onClick.AddListener(() => OnProviderClicked(provider));
        }

        private void OnProviderClicked(string provider)
        {
            Debug.Log($"[SignInSheet] {provider} sign-in tapped");
            StartNativeSignIn(provider);
        }

        // ───── Native sign-in flow ─────

        private void StartNativeSignIn(string provider)
        {
            if (NativeSignIn.Instance == null)
            {
                Debug.LogWarning("[SignInSheet] NativeSignIn not available");
                return;
            }

            canvasGroup.interactable = false;

            NativeSignIn.Instance.SignIn(provider, (success, idToken, error) =>
            {
                if (!success)
                {
                    canvasGroup.interactable = true;
                    Debug.LogWarning($"[SignInSheet] Native {provider} sign-in failed: {error}");
                    return;
                }

                // Hide the sign-in sheet and show full-screen loading
                Hide();
                if (AuthLoadingOverlay.Instance != null)
                    AuthLoadingOverlay.Instance.Show("Connecting...");

                if (AuthManager.Instance != null)
                {
                    AuthManager.Instance.SignInWithIdToken(provider, idToken, null, (authSuccess, authError) =>
                    {
                        if (authSuccess)
                        {
                            if (MergeGame.Core.PlayerIdentity.Instance != null)
                                MergeGame.Core.PlayerIdentity.Instance.RefreshFromAuth();
                            OnSignInComplete?.Invoke(provider);
                            // Loading overlay dismissed by the caller (settings/onboarding)
                        }
                        else
                        {
                            Debug.LogWarning($"[SignInSheet] Supabase auth failed: {authError}");
                            if (AuthLoadingOverlay.Instance != null)
                                AuthLoadingOverlay.Instance.Hide();
                        }
                    });
                }
            });
        }
    }
}
