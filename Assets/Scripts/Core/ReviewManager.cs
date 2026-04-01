using UnityEngine;
using System.Runtime.InteropServices;

namespace MergeGame.Core
{
    /// <summary>
    /// In-app review prompt using native iOS (SKStoreReviewController)
    /// and Android (Google Play In-App Review API).
    ///
    /// Call ReviewManager.Instance.RequestReviewIfEligible() at natural
    /// moments (e.g. after a good game, streak milestone). The OS controls
    /// whether the prompt actually shows — calling this is always safe.
    /// </summary>
    public class ReviewManager : MonoBehaviour
    {
        public static ReviewManager Instance { get; private set; }

        private const string GamesCompletedKey = "review_games_completed";
        private const string HasReviewedKey = "review_has_reviewed";
        private const string LastPromptDateKey = "review_last_prompt_date";

        private const int MinGamesBeforePrompt = 3;
        private const int MinDaysBetweenPrompts = 30;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void _RequestAppReview();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _reviewManager;
        private static AndroidJavaObject _reviewInfo;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Call after each completed game to track engagement.
        /// </summary>
        public void RecordGameCompleted()
        {
            int count = PlayerPrefs.GetInt(GamesCompletedKey, 0) + 1;
            PlayerPrefs.SetInt(GamesCompletedKey, count);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Request an in-app review if the player is eligible.
        /// Safe to call anytime — no-ops if conditions aren't met or
        /// if the OS decides not to show the prompt.
        /// </summary>
        public void RequestReviewIfEligible()
        {
            if (!IsEligible()) return;

            PlayerPrefs.SetString(LastPromptDateKey, System.DateTime.Now.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();

            RequestNativeReview();
        }

        /// <summary>
        /// Force-request a review prompt, bypassing eligibility checks.
        /// Use sparingly — e.g. from a settings "Rate this app" button.
        /// </summary>
        public void RequestReview()
        {
            RequestNativeReview();
        }

        /// <summary>
        /// Mark that the player has been through the review flow.
        /// Call this after RequestReviewIfEligible to avoid repeated prompts.
        /// (The OS may still suppress the dialog even if we request it.)
        /// </summary>
        public void MarkReviewed()
        {
            PlayerPrefs.SetInt(HasReviewedKey, 1);
            PlayerPrefs.Save();
        }

        public bool IsEligible()
        {
            if (PlayerPrefs.GetInt(HasReviewedKey, 0) == 1) return false;

            int gamesCompleted = PlayerPrefs.GetInt(GamesCompletedKey, 0);
            if (gamesCompleted < MinGamesBeforePrompt) return false;

            string lastPrompt = PlayerPrefs.GetString(LastPromptDateKey, "");
            if (!string.IsNullOrEmpty(lastPrompt))
            {
                if (System.DateTime.TryParse(lastPrompt, out var lastDate))
                {
                    int daysSince = (System.DateTime.Now - lastDate).Days;
                    if (daysSince < MinDaysBetweenPrompts) return false;
                }
            }

            return true;
        }

        private void RequestNativeReview()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _RequestAppReview();
#elif UNITY_ANDROID && !UNITY_EDITOR
            RequestAndroidReview();
#else
            Debug.Log("ReviewManager: Review requested (editor/unsupported platform)");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void RequestAndroidReview()
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var reviewManagerFactory = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory"))
                {
                    _reviewManager = reviewManagerFactory.CallStatic<AndroidJavaObject>("create", activity);
                    var requestTask = _reviewManager.Call<AndroidJavaObject>("requestReviewFlow");
                    requestTask.Call<AndroidJavaObject>("addOnCompleteListener", new ReviewFlowListener(activity));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"ReviewManager: Android review failed — {e.Message}");
            }
        }

        private class ReviewFlowListener : AndroidJavaProxy
        {
            private readonly AndroidJavaObject _activity;

            public ReviewFlowListener(AndroidJavaObject activity)
                : base("com.google.android.gms.tasks.OnCompleteListener")
            {
                _activity = activity;
            }

            public void onComplete(AndroidJavaObject task)
            {
                if (!task.Call<bool>("isSuccessful")) return;
                _reviewInfo = task.Call<AndroidJavaObject>("getResult");
                if (_reviewManager != null && _reviewInfo != null)
                {
                    _reviewManager.Call<AndroidJavaObject>("launchReviewFlow", _activity, _reviewInfo);
                }
            }
        }
#endif
    }
}
