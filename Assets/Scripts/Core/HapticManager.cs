using UnityEngine;

namespace MergeGame.Core
{
    public class HapticManager : MonoBehaviour
    {
        public static HapticManager Instance { get; private set; }

        private const string HapticsEnabledKey = "haptics_enabled";

        public bool IsEnabled { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            IsEnabled = PlayerPrefs.GetInt(HapticsEnabledKey, 1) == 1;
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            PlayerPrefs.SetInt(HapticsEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void PlayLight()
        {
            if (!IsEnabled || !IsMobilePlatform()) return;
#if UNITY_IOS || UNITY_ANDROID
            Vibrate(10);
#endif
        }

        public void PlayMedium()
        {
            if (!IsEnabled || !IsMobilePlatform()) return;
#if UNITY_IOS || UNITY_ANDROID
            Vibrate(25);
#endif
        }

        public void PlayHeavy()
        {
            if (!IsEnabled || !IsMobilePlatform()) return;
#if UNITY_IOS || UNITY_ANDROID
            Vibrate(50);
#endif
        }

        public void PlayDrop()
        {
            PlayLight();
        }

        public void PlayLanding(int tier)
        {
            if (!IsEnabled || !IsMobilePlatform()) return;
            // Scale with ball size
            int duration = Mathf.Clamp(8 + tier * 3, 8, 40);
#if UNITY_IOS || UNITY_ANDROID
            Vibrate(duration);
#endif
        }

        public void PlayMerge(int tier, int chainIndex)
        {
            if (!IsEnabled || !IsMobilePlatform()) return;

            if (tier >= 10)
            {
                // Tier 11 creation — strongest haptic
                PlayHeavy();
                return;
            }

            if (tier >= 7)
            {
                // High-tier merge (8+)
                PlayHeavy();
                return;
            }

            // Scale haptic intensity with chain
#if UNITY_IOS || UNITY_ANDROID
            int duration = 15 + Mathf.Min(chainIndex * 5, 25);
            Vibrate(duration);
#endif
        }

        public void PlayGameOver()
        {
            if (!IsEnabled || !IsMobilePlatform()) return;
            // Slow descending buzz
#if UNITY_IOS || UNITY_ANDROID
            Vibrate(100);
#endif
        }

        public void PlayUITap()
        {
            PlayLight();
        }

        private static bool IsMobilePlatform()
        {
#if UNITY_WEBGL
            return false;
#elif UNITY_IOS || UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }

        private static void Vibrate(int milliseconds)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null)
                    {
                        vibrator.Call("vibrate", (long)milliseconds);
                    }
                }
            }
            catch (System.Exception) { }
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS: Use Handheld.Vibrate as baseline. For finer control, a native plugin
            // would call UIImpactFeedbackGenerator with style based on the duration parameter.
            // TODO: Replace with Core Haptics native plugin for production.
            Handheld.Vibrate();
#endif
        }
    }
}
