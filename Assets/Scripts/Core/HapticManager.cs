using UnityEngine;
using System.Runtime.InteropServices;

namespace MergeGame.Core
{
    /// <summary>
    /// Haptic feedback. iOS uses UIImpactFeedbackGenerator, Android uses VibrationEffect.
    /// Light = barely there, Medium = noticeable, Heavy = strong.
    /// </summary>
    public class HapticManager : MonoBehaviour
    {
        public static HapticManager Instance { get; private set; }

        private const string HapticsEnabledKey = "haptics_enabled";

        public bool IsEnabled { get; private set; }

        private float lastHapticTime;
        private const float HapticCooldown = 0.12f;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void _HapticLight();
        [DllImport("__Internal")] private static extern void _HapticMedium();
        [DllImport("__Internal")] private static extern void _HapticHeavy();
        [DllImport("__Internal")] private static extern void _HapticSelection();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaClass _androidHaptic;
        private static AndroidJavaClass AndroidHaptic
        {
            get
            {
                if (_androidHaptic == null)
                    _androidHaptic = new AndroidJavaClass("com.murge.haptic.HapticPlugin");
                return _androidHaptic;
            }
        }
#endif

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

        /// <summary>No haptic on drop.</summary>
        public void PlayDrop() { }

        /// <summary>Soft tap on landing — light for small balls, medium for large.</summary>
        public void PlayLanding(int tier)
        {
            if (tier < 3) return; // Skip tiny balls
            if (tier >= 7)
                DoMedium();
            else
                DoLight();
        }

        /// <summary>Escalating haptic on merge — scales with chain combo.</summary>
        public void PlayMerge(int tier, int chainLength)
        {
            if (tier < 2 && chainLength < 2) return;

            if (chainLength >= 4)
                DoHeavy();
            else if (chainLength >= 2 || tier >= 8)
                DoMedium();
            else
                DoLight();
        }

        /// <summary>Medium haptic on game over.</summary>
        public void PlayGameOver()
        {
            DoMedium();
        }

        /// <summary>No haptic on UI taps.</summary>
        public void PlayUITap() { }

        private void DoLight()
        {
            if (!CanFire()) return;
#if UNITY_IOS && !UNITY_EDITOR
            _HapticLight();
#elif UNITY_ANDROID && !UNITY_EDITOR
            try { AndroidHaptic.CallStatic("hapticLight"); } catch { }
#endif
        }

        private void DoMedium()
        {
            if (!CanFire()) return;
#if UNITY_IOS && !UNITY_EDITOR
            _HapticMedium();
#elif UNITY_ANDROID && !UNITY_EDITOR
            try { AndroidHaptic.CallStatic("hapticMedium"); } catch { }
#endif
        }

        private void DoHeavy()
        {
            if (!CanFire()) return;
#if UNITY_IOS && !UNITY_EDITOR
            _HapticHeavy();
#elif UNITY_ANDROID && !UNITY_EDITOR
            try { AndroidHaptic.CallStatic("hapticHeavy"); } catch { }
#endif
        }

        private bool CanFire()
        {
            if (!IsEnabled) return false;
#if !(UNITY_IOS || UNITY_ANDROID)
            return false;
#endif
            if (Time.unscaledTime - lastHapticTime < HapticCooldown) return false;
            lastHapticTime = Time.unscaledTime;
            return true;
        }
    }
}
