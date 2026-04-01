using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;

namespace MergeGame.Backend
{
    /// <summary>
    /// Manages Supabase Auth state: anonymous sessions, token refresh, and account linking.
    /// Uses the GoTrue REST API directly (no SDK dependency).
    /// </summary>
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        // Auth state
        public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);
        public bool IsAnonymous { get; private set; } = true;
        public string UserId { get; private set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public string AuthProvider { get; private set; } = "anonymous";
        public string AuthEmail { get; private set; }

        // Token expiry (absolute Unix timestamp in seconds)
        private long tokenExpiresAtUnix;
        private const long RefreshBufferSeconds = 300; // refresh 5 min before expiry

        // Persistence keys
        private const string AccessTokenKey = "auth_access_token";
        private const string RefreshTokenKey = "auth_refresh_token";
        private const string UserIdKey = "auth_user_id";
        private const string IsAnonymousKey = "auth_is_anonymous";
        private const string ProviderKey = "auth_provider";
        private const string EmailKey = "auth_email";
        private const string TokenExpiryKey = "auth_token_expiry";

        public event Action OnAuthStateChanged;

        /// <summary>Fired when WebGL OAuth redirect tokens are detected on startup.</summary>
        public event Action OnWebGLSignInComplete;

        /// <summary>True while processing WebGL OAuth redirect tokens.</summary>
        public bool IsProcessingWebGLTokens { get; private set; }

        private string AuthUrl => SupabaseClient.Instance != null
            ? $"{GetSupabaseUrl()}/auth/v1"
            : "";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string WebAuth_GetTokensFromUrl();
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadPersistedSession();
        }

        private void Start()
        {
            // On WebGL, check if we're returning from an OAuth redirect
            CheckWebGLTokens();

            // If we have a session, check if token needs refresh
            if (IsAuthenticated)
                StartCoroutine(RefreshTokenIfNeeded());
        }

        /// <summary>
        /// On WebGL, check URL fragment for Supabase auth tokens from OAuth redirect.
        /// If found, establishes a session and fires OnWebGLSignInComplete.
        /// </summary>
        private void CheckWebGLTokens()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string json = WebAuth_GetTokensFromUrl();
                if (string.IsNullOrEmpty(json)) return;

                var tokens = JsonUtility.FromJson<WebGLTokenResponse>(json);
                if (string.IsNullOrEmpty(tokens.access_token)) return;

                Debug.Log("[Auth] WebGL OAuth tokens detected — establishing session");
                IsProcessingWebGLTokens = true;

                // Store old user ID for potential migration
                string oldUserId = UserId;
                bool wasAnonymous = IsAnonymous && IsAuthenticated;

                // Set session from the OAuth tokens — we need to fetch the user info
                AccessToken = tokens.access_token;
                RefreshToken = tokens.refresh_token;
                tokenExpiresAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + tokens.expires_in;

                // Fetch user details to get user_id and email
                StartCoroutine(FetchWebGLUser(oldUserId, wasAnonymous));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Auth] WebGL token check failed: {e.Message}");
            }
#endif
        }

        private IEnumerator FetchWebGLUser(string oldUserId, bool wasAnonymous)
        {
            string url = $"{AuthUrl}/user";

            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("apikey", GetSupabaseKey());
                request.SetRequestHeader("Authorization", $"Bearer {AccessToken}");
                request.timeout = 15;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var user = JsonUtility.FromJson<AuthUser>(request.downloadHandler.text);
                    UserId = user.id;
                    AuthEmail = user.email ?? "";
                    IsAnonymous = false;
                    AuthProvider = user.app_metadata?.provider ?? "google";
                    PersistSession();
                    OnAuthStateChanged?.Invoke();

                    Debug.Log($"[Auth] WebGL session established: user={UserId}, email={AuthEmail}");

                    // Mark onboarding complete since user authenticated via provider
                    Core.GameSession.MarkOnboardingComplete();

                    // Sync PlayerIdentity so leaderboard highlights work
                    if (Core.PlayerIdentity.Instance != null)
                        Core.PlayerIdentity.Instance.RegisterAfterAuth();

                    // Migrate data if we had an anonymous session
                    if (wasAnonymous && !string.IsNullOrEmpty(oldUserId) && oldUserId != UserId)
                    {
                        Debug.Log($"[Auth] Migrating anonymous data: {oldUserId} -> {UserId}");
                        yield return MigratePlayerData(oldUserId, UserId);
                    }

                    IsProcessingWebGLTokens = false;
                    OnWebGLSignInComplete?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"[Auth] WebGL user fetch failed: {request.responseCode} {request.downloadHandler?.text}");
                    // Clear the tokens since we can't establish a full session
                    AccessToken = "";
                    RefreshToken = "";
                    IsProcessingWebGLTokens = false;
                }
            }
        }

        private void Update()
        {
            // Periodic token refresh check
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (IsAuthenticated && now > tokenExpiresAtUnix - RefreshBufferSeconds)
            {
                tokenExpiresAtUnix = long.MaxValue; // prevent re-entry
                StartCoroutine(DoRefreshToken());
            }
        }

        // ───── Session Persistence ─────

        private void LoadPersistedSession()
        {
            AccessToken = PlayerPrefs.GetString(AccessTokenKey, "");
            RefreshToken = PlayerPrefs.GetString(RefreshTokenKey, "");
            UserId = PlayerPrefs.GetString(UserIdKey, "");
            IsAnonymous = PlayerPrefs.GetInt(IsAnonymousKey, 1) == 1;
            AuthProvider = PlayerPrefs.GetString(ProviderKey, "anonymous");
            AuthEmail = PlayerPrefs.GetString(EmailKey, "");
            long.TryParse(PlayerPrefs.GetString(TokenExpiryKey, "0"), out tokenExpiresAtUnix);

            if (!string.IsNullOrEmpty(UserId))
                Debug.Log($"[Auth] Restored session: user={UserId}, anonymous={IsAnonymous}, provider={AuthProvider}");
        }

        private void PersistSession()
        {
            PlayerPrefs.SetString(AccessTokenKey, AccessToken ?? "");
            PlayerPrefs.SetString(RefreshTokenKey, RefreshToken ?? "");
            PlayerPrefs.SetString(UserIdKey, UserId ?? "");
            PlayerPrefs.SetInt(IsAnonymousKey, IsAnonymous ? 1 : 0);
            PlayerPrefs.SetString(ProviderKey, AuthProvider ?? "anonymous");
            PlayerPrefs.SetString(EmailKey, AuthEmail ?? "");
            PlayerPrefs.SetString(TokenExpiryKey, tokenExpiresAtUnix.ToString());
            PlayerPrefs.Save();
        }

        private void ClearSession()
        {
            AccessToken = "";
            RefreshToken = "";
            UserId = "";
            IsAnonymous = true;
            AuthProvider = "anonymous";
            AuthEmail = "";
            tokenExpiresAtUnix = 0;
            PersistSession();
        }

        // ───── Anonymous Auth ─────

        /// <summary>
        /// Create an anonymous session. Called when user taps "LET'S PLAY" in onboarding
        /// (or as a defensive fallback before any API call).
        /// </summary>
        public void CreateAnonymousSession(Action<bool> callback = null)
        {
            if (IsAuthenticated)
            {
                callback?.Invoke(true);
                return;
            }
            StartCoroutine(DoAnonymousSignUp(callback));
        }

        /// <summary>
        /// Ensure we have a valid auth session. Creates anonymous if needed.
        /// Call before any API request.
        /// </summary>
        public void EnsureAuthenticated(Action<bool> callback)
        {
            if (IsAuthenticated)
            {
                callback?.Invoke(true);
                return;
            }
            CreateAnonymousSession(callback);
        }

        private IEnumerator DoAnonymousSignUp(Action<bool> callback)
        {
            string url = $"{AuthUrl}/signup";
            string json = "{\"data\":{}}"; // anonymous signup — no email/password

            Debug.Log("[Auth] Creating anonymous session...");

            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", GetSupabaseKey());
                request.timeout = 15;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    SetSession(response, isAnonymous: true);
                    Debug.Log($"[Auth] Anonymous session created: user={UserId}");
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning($"[Auth] Anonymous signup failed: {request.responseCode} {request.downloadHandler?.text}");
                    callback?.Invoke(false);
                }
            }
        }

        // ───── Token Refresh ─────

        private IEnumerator RefreshTokenIfNeeded()
        {
            if (string.IsNullOrEmpty(RefreshToken)) yield break;

            // Check if token is expired or about to expire
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now < tokenExpiresAtUnix - RefreshBufferSeconds)
                yield break;

            yield return DoRefreshToken();
        }

        private IEnumerator DoRefreshToken()
        {
            if (string.IsNullOrEmpty(RefreshToken)) yield break;

            string url = $"{AuthUrl}/token?grant_type=refresh_token";
            string json = $"{{\"refresh_token\":\"{RefreshToken}\"}}";

            Debug.Log("[Auth] Refreshing token...");

            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", GetSupabaseKey());
                request.timeout = 15;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    SetSession(response, isAnonymous: IsAnonymous,
                        provider: AuthProvider, email: AuthEmail);
                    Debug.Log("[Auth] Token refreshed");
                }
                else
                {
                    Debug.LogWarning($"[Auth] Token refresh failed: {request.responseCode}");
                    if (request.responseCode == 401)
                    {
                        // Retry once before giving up
                        yield return new WaitForSeconds(2f);
                        yield return DoRefreshTokenFinal();
                    }
                    else
                    {
                        // Network error — try again later
                        tokenExpiresAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 30;
                    }
                }
            }
        }

        private IEnumerator DoRefreshTokenFinal()
        {
            if (string.IsNullOrEmpty(RefreshToken)) { ClearSession(); yield break; }

            string url = $"{AuthUrl}/token?grant_type=refresh_token";
            string json = $"{{\"refresh_token\":\"{RefreshToken}\"}}";

            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", GetSupabaseKey());
                request.timeout = 15;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    SetSession(response, isAnonymous: IsAnonymous,
                        provider: AuthProvider, email: AuthEmail);
                    Debug.Log("[Auth] Token refreshed on retry");
                }
                else
                {
                    Debug.LogWarning($"[Auth] Token refresh retry failed: {request.responseCode} — clearing session");
                    ClearSession();
                }
            }
        }

        // ───── Sign In with Provider (Apple/Google) ─────

        /// <summary>
        /// Sign in or link with an OAuth provider using an ID token from native sign-in.
        /// If currently anonymous, this links the identity (preserves user ID).
        /// If not authenticated, this creates a new session.
        /// </summary>
        public void SignInWithIdToken(string provider, string idToken, string accessToken = null,
            Action<bool, string> callback = null)
        {
            StartCoroutine(DoSignInWithIdToken(provider, idToken, accessToken, callback));
        }

        private IEnumerator DoSignInWithIdToken(string provider, string idToken,
            string accessToken, Action<bool, string> callback)
        {
            // If currently anonymous, link the identity to preserve user_id
            if (IsAuthenticated && IsAnonymous)
            {
                yield return DoLinkIdentity(provider, idToken, accessToken, callback);
                yield break;
            }

            // Not authenticated — sign in directly (creates or finds account)
            string url = $"{AuthUrl}/token?grant_type=id_token";
            var requestBody = new IdTokenRequest
            {
                provider = provider,
                id_token = idToken,
                access_token = accessToken ?? ""
            };
            string json = JsonUtility.ToJson(requestBody);

            Debug.Log($"[Auth] Signing in with {provider}...");

            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", GetSupabaseKey());
                request.timeout = 15;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    string email = response.user?.email ?? "";
                    SetSession(response, isAnonymous: false, provider: provider, email: email);
                    Debug.Log($"[Auth] Signed in with {provider}: user={UserId}");
                    callback?.Invoke(true, null);
                }
                else
                {
                    string error = request.downloadHandler?.text ?? request.error;
                    Debug.LogWarning($"[Auth] {provider} sign-in failed: {request.responseCode} {error}");
                    callback?.Invoke(false, error);
                }
            }
        }

        /// <summary>
        /// Link a provider to the current anonymous user.
        /// Signs in with the provider, then migrates player data if the user_id changed.
        /// </summary>
        private IEnumerator DoLinkIdentity(string provider, string idToken,
            string accessToken, Action<bool, string> callback)
        {
            string oldUserId = UserId;
            string anonToken = AccessToken;
            Debug.Log($"[Auth] Linking {provider} to anonymous user {oldUserId} (has token: {!string.IsNullOrEmpty(anonToken)})...");

            string url = $"{AuthUrl}/token?grant_type=id_token";
            string json = JsonUtility.ToJson(new IdTokenRequest
            {
                provider = provider,
                id_token = idToken,
                access_token = accessToken ?? ""
            });

            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", GetSupabaseKey());
                // Include anonymous session JWT — Supabase should link the identity
                request.SetRequestHeader("Authorization", $"Bearer {anonToken}");
                request.timeout = 15;

                yield return request.SendWebRequest();

                string responseText = request.downloadHandler?.text ?? "";
                Debug.Log($"[Auth] Link response {request.responseCode}: {responseText.Substring(0, Mathf.Min(responseText.Length, 500))}");

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Auth] {provider} link failed: {request.responseCode}");
                    callback?.Invoke(false, responseText);
                    yield break;
                }

                var response = JsonUtility.FromJson<AuthResponse>(responseText);
                string email = response.user?.email ?? "";
                string newUserId = response.user?.id ?? "";

                SetSession(response, isAnonymous: false, provider: provider, email: email);

                if (newUserId == oldUserId)
                {
                    // Same user_id — identity linked successfully, no migration needed
                    Debug.Log($"[Auth] SUCCESS: Linked {provider} to user {UserId} — user_id preserved!");
                }
                else
                {
                    // Different user_id — linking didn't work, fall back to migration
                    Debug.Log($"[Auth] user_id changed: {oldUserId} -> {newUserId}. Migrating data...");
                    yield return MigratePlayerData(oldUserId, newUserId);
                }

                callback?.Invoke(true, null);
            }
        }

        /// <summary>
        /// Migrate player data (display_name, streaks) from old user_id to new user_id.
        /// Called when an anonymous user connects a provider and gets a different user_id.
        /// </summary>
        private IEnumerator MigratePlayerData(string fromUserId, string toUserId)
        {
            if (SupabaseClient.Instance == null) yield break;

            Debug.Log($"[Auth] Migrating player data from {fromUserId} to {toUserId}");

            string json = $"{{\"from_user_id\":\"{fromUserId}\",\"to_user_id\":\"{toUserId}\"}}";
            bool done = false;
            SupabaseClient.Instance.CallFunction("migrate-player", json, (success, response) =>
            {
                if (success)
                    Debug.Log($"[Auth] Player data migrated: {response}");
                else
                    Debug.LogWarning($"[Auth] Player migration failed: {response}");
                done = true;
            });

            while (!done) yield return null;
        }

        // ───── Sign Out ─────

        /// <summary>
        /// Signs out and creates a fresh anonymous session.
        /// Old scores remain under the old user ID.
        /// </summary>
        public void SignOut(Action<bool> callback = null)
        {
            StartCoroutine(DoSignOut(callback));
        }

        private IEnumerator DoSignOut(Action<bool> callback)
        {
            // Tell server to invalidate the session
            if (IsAuthenticated)
            {
                string url = $"{AuthUrl}/logout";
                using (var request = new UnityWebRequest(url, "POST"))
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Authorization", $"Bearer {AccessToken}");
                    request.SetRequestHeader("apikey", GetSupabaseKey());
                    request.timeout = 10;
                    yield return request.SendWebRequest();
                    // Ignore result — clear local session regardless
                }
            }

            ClearSession();
            OnAuthStateChanged?.Invoke();
            Debug.Log("[Auth] Signed out");
            callback?.Invoke(true);
        }

        // ───── Session Management ─────

        private void SetSession(AuthResponse response, bool isAnonymous,
            string provider = "anonymous", string email = "")
        {
            AccessToken = response.access_token;
            RefreshToken = response.refresh_token;
            IsAnonymous = isAnonymous;
            AuthProvider = provider;
            AuthEmail = email;

            if (response.user != null && !string.IsNullOrEmpty(response.user.id))
                UserId = response.user.id;

            // Token expires in N seconds — store as absolute Unix timestamp
            tokenExpiresAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + response.expires_in;

            PersistSession();
            OnAuthStateChanged?.Invoke();
        }

        // ───── WebGL Popup Auth (iframe) ─────

        /// <summary>
        /// Called from JavaScript via SendMessage when OAuth tokens arrive
        /// from the popup callback page (used when running in an iframe).
        /// </summary>
        public void OnWebAuthTokensReceived(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var tokens = JsonUtility.FromJson<WebGLTokenResponse>(json);
                if (string.IsNullOrEmpty(tokens.access_token)) return;

                Debug.Log("[Auth] WebGL popup auth tokens received");

                string oldUserId = UserId;
                bool wasAnonymous = IsAnonymous && IsAuthenticated;

                AccessToken = tokens.access_token;
                RefreshToken = tokens.refresh_token;
                tokenExpiresAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + tokens.expires_in;

                StartCoroutine(FetchWebGLUser(oldUserId, wasAnonymous));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Auth] WebGL popup token parse failed: {e.Message}");
            }
        }

        // ───── Helpers ─────

        private string GetSupabaseUrl()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return "https://qbgrghcpvmsnoyzmglgv.supabase.co";
#else
            return "https://negfbluxywxsadggnwwd.supabase.co";
#endif
        }

        private string GetSupabaseKey()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return "sb_publishable_MvO6gq-SZ4E5QmW_tq_ltg_TaR37Wty";
#else
            return "sb_publishable_7ACIeBkB8iBcNxxDkN7Vhg_MxMuO1fW";
#endif
        }

        // ───── JSON Types ─────

        [Serializable]
        private class AuthResponse
        {
            public string access_token;
            public string refresh_token;
            public int expires_in;
            public AuthUser user;
        }

        [Serializable]
        private class AuthUser
        {
            public string id;
            public string email;
            public bool is_anonymous;
            public AuthAppMetadata app_metadata;
        }

        [Serializable]
        private class AuthAppMetadata
        {
            public string provider;
        }

        [Serializable]
        private class IdTokenRequest
        {
            public string provider;
            public string id_token;
            public string access_token;
        }

        [Serializable]
        private class WebGLTokenResponse
        {
            public string access_token;
            public string refresh_token;
            public int expires_in;
            public string token_type;
        }
    }
}
