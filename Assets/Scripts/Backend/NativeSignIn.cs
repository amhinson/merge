using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace MergeGame.Backend
{
    /// <summary>
    /// Unified interface for native Apple Sign In (iOS) and Google Sign In (Android).
    /// Calls platform-specific native code and returns ID tokens via callback.
    /// </summary>
    public class NativeSignIn : MonoBehaviour
    {
        public static NativeSignIn Instance { get; private set; }

        private Action<bool, string, string> pendingCallback; // success, idToken, error

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("NativeSignIn");
            go.AddComponent<NativeSignIn>();
            DontDestroyOnLoad(go);
        }

        /// <summary>
        /// Start the native sign-in flow for the given provider.
        /// Callback: (success, idToken, errorMessage)
        /// </summary>
        public void SignIn(string provider, Action<bool, string, string> callback)
        {
            pendingCallback = callback;

            if (provider == "apple")
                SignInWithApple();
            else if (provider == "google")
                SignInWithGoogle();
            else
                callback?.Invoke(false, null, $"Unknown provider: {provider}");
        }

        // ───── Apple Sign In (iOS) ─────

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void MurgeAppleSignIn_Start();
#endif

        private void SignInWithApple()
        {
#if UNITY_IOS && !UNITY_EDITOR
            MurgeAppleSignIn_Start();
#elif UNITY_WEBGL && !UNITY_EDITOR
            StartWebOAuth("apple");
#else
            Debug.LogWarning("[NativeSignIn] Apple Sign In not available on this platform");
            pendingCallback?.Invoke(false, null, "Apple Sign In not available");
#endif
        }

        /// <summary>Called from native iOS code on success.</summary>
        public void OnAppleSignInSuccess(string idToken)
        {
            Debug.Log("[NativeSignIn] Apple Sign In success");
            pendingCallback?.Invoke(true, idToken, null);
            pendingCallback = null;
        }

        /// <summary>Called from native iOS code on failure.</summary>
        public void OnAppleSignInFailure(string error)
        {
            Debug.LogWarning($"[NativeSignIn] Apple Sign In failed: {error}");
            pendingCallback?.Invoke(false, null, error);
            pendingCallback = null;
        }

        // ───── Google Sign In (iOS + Android + WebGL) ─────

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void MurgeGoogleSignIn_Start();
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WebAuth_StartOAuthSignIn(string supabaseUrl, string supabaseKey, string redirectUrl, string provider);

        [DllImport("__Internal")]
        private static extern bool WebAuth_IsAppleDevice();
#endif

        /// <summary>Whether Apple Sign In is available on this WebGL platform.</summary>
        public static bool IsAppleSignInAvailableOnWeb()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { return WebAuth_IsAppleDevice(); }
            catch { return false; }
#else
            return false;
#endif
        }

        private void SignInWithGoogle()
        {
#if UNITY_IOS && !UNITY_EDITOR
            MurgeGoogleSignIn_Start();
#elif UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var plugin = new AndroidJavaClass("com.murge.game.GoogleSignInPlugin"))
                {
                    plugin.CallStatic("signIn", activity);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NativeSignIn] Google Sign In error: {e.Message}");
                pendingCallback?.Invoke(false, null, e.Message);
            }
#elif UNITY_WEBGL && !UNITY_EDITOR
            StartWebOAuth("google");
#else
            Debug.LogWarning("[NativeSignIn] Google Sign In not available on this platform");
            pendingCallback?.Invoke(false, null, "Google Sign In not available");
#endif
        }

        // ───── WebGL OAuth (shared for Google + Apple) ─────

#if UNITY_WEBGL && !UNITY_EDITOR
        private void StartWebOAuth(string provider)
        {
            if (SupabaseClient.Instance != null)
            {
                string url = SupabaseClient.Instance.IsProduction
                    ? "https://negfbluxywxsadggnwwd.supabase.co"
                    : "https://qbgrghcpvmsnoyzmglgv.supabase.co";
                string key = SupabaseClient.Instance.IsProduction
                    ? "sb_publishable_7ACIeBkB8iBcNxxDkN7Vhg_MxMuO1fW"
                    : "sb_publishable_MvO6gq-SZ4E5QmW_tq_ltg_TaR37Wty";
                string redirect = Application.absoluteURL;
                if (string.IsNullOrEmpty(redirect))
                    redirect = "https://murgegame.com/play/";
                WebAuth_StartOAuthSignIn(url, key, redirect, provider);
            }
            else
            {
                pendingCallback?.Invoke(false, null, "SupabaseClient not available");
            }
        }
#endif

        /// <summary>Called from native Android code via UnitySendMessage on success.</summary>
        public void OnGoogleSignInSuccess(string idToken)
        {
            Debug.Log("[NativeSignIn] Google Sign In success");
            pendingCallback?.Invoke(true, idToken, null);
            pendingCallback = null;
        }

        /// <summary>Called from native Android code via UnitySendMessage on failure.</summary>
        public void OnGoogleSignInFailure(string error)
        {
            Debug.LogWarning($"[NativeSignIn] Google Sign In failed: {error}");
            pendingCallback?.Invoke(false, null, error);
            pendingCallback = null;
        }
    }
}
