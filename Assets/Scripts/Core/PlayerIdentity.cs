using UnityEngine;
using System.Text.RegularExpressions;
using MergeGame.Backend;

namespace MergeGame.Core
{
    public class PlayerIdentity : MonoBehaviour
    {
        public static PlayerIdentity Instance { get; private set; }

        private const string UUIDKey = "player_uuid";
        private const string DisplayNameKey = "player_display_name";
        private const int MinNameLength = 3;
        private const int MaxNameLength = 24;

        public string DeviceUUID { get; private set; }
        public string DisplayName { get; private set; }

        public event System.Action<string> OnDisplayNameChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadOrCreateIdentity();
        }

        private void Start()
        {
            // Only register if already authenticated (returning user).
            // New users get registered after onboarding creates their anonymous session.
            if (Backend.AuthManager.Instance != null && Backend.AuthManager.Instance.IsAuthenticated)
                RegisterWithBackend();
        }

        private void RegisterWithBackend()
        {
            if (SupabaseClient.Instance == null) return;

            // Only send display_name if the player already has one (returning player).
            // New players get a name generated server-side.
            bool isNewPlayer = string.IsNullOrEmpty(DisplayName);
            var request = new RegisterRequest
            {
                user_id = DeviceUUID,
                device_uuid = DeviceUUID, // legacy fallback
                display_name = isNewPlayer ? "" : DisplayName
            };
            string json = JsonUtility.ToJson(request);

            SupabaseClient.Instance.CallFunction("register-player", json, (success, response) =>
            {
                if (success)
                {
                    Debug.Log("PlayerIdentity: Registered with backend");

                    // If we were a new player, adopt the name the backend generated
                    if (isNewPlayer)
                    {
                        try
                        {
                            var parsed = JsonUtility.FromJson<RegisterResponse>(response);
                            if (!string.IsNullOrEmpty(parsed.display_name))
                            {
                                DisplayName = parsed.display_name;
                                PlayerPrefs.SetString(DisplayNameKey, DisplayName);
                                PlayerPrefs.Save();
                                OnDisplayNameChanged?.Invoke(DisplayName);
                                Debug.Log($"PlayerIdentity: Assigned name '{DisplayName}'");
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"PlayerIdentity: Could not parse backend name — {e.Message}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"PlayerIdentity: Backend registration failed — {response}");
                    // Queue for retry when online
                    if (Backend.OfflineSyncQueue.Instance != null)
                        Backend.OfflineSyncQueue.Instance.Enqueue("register-player", json);
                }
            });
        }

        [System.Serializable]
        private class RegisterRequest
        {
            public string user_id;
            public string device_uuid; // legacy fallback — kept during transition
            public string display_name;
        }

        [System.Serializable]
        private class RegisterResponse
        {
            public bool success;
            public string display_name;
        }

        private void LoadOrCreateIdentity()
        {
            // Prefer AuthManager's user ID if available, fall back to legacy device UUID
            if (Backend.AuthManager.Instance != null && Backend.AuthManager.Instance.IsAuthenticated)
            {
                DeviceUUID = Backend.AuthManager.Instance.UserId;
            }
            else
            {
                // Legacy: use PlayerPrefs UUID (for backward compat during transition)
                DeviceUUID = PlayerPrefs.GetString(UUIDKey, "");
                if (string.IsNullOrEmpty(DeviceUUID))
                {
                    DeviceUUID = System.Guid.NewGuid().ToString();
                    PlayerPrefs.SetString(UUIDKey, DeviceUUID);
                }
            }

            // Display name — loaded from local cache; new players get their name
            // from the backend during registration.
            DisplayName = PlayerPrefs.GetString(DisplayNameKey, "");

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Register with backend after auth session is established.
        /// Called from onboarding after anonymous session or sign-in.
        /// </summary>
        public void RegisterAfterAuth()
        {
            RefreshFromAuth();
            RegisterWithBackend();
        }

        /// <summary>
        /// Re-sync identity after auth state changes (e.g., anonymous session created).
        /// </summary>
        public void RefreshFromAuth()
        {
            if (Backend.AuthManager.Instance != null && Backend.AuthManager.Instance.IsAuthenticated)
            {
                string oldUUID = DeviceUUID;
                DeviceUUID = Backend.AuthManager.Instance.UserId;
                GameSession.DeviceUUID = DeviceUUID;

                // If user_id changed, clear the cached display name — new one comes from registration
                if (oldUUID != DeviceUUID)
                {
                    DisplayName = "";
                    PlayerPrefs.SetString(DisplayNameKey, "");
                    PlayerPrefs.Save();
                    OnDisplayNameChanged?.Invoke(DisplayName);
                }

                Debug.Log($"PlayerIdentity: Updated to auth user {DeviceUUID}");
            }
        }

        /// <summary>Clear the display name (used on sign-out).</summary>
        public void ClearDisplayName()
        {
            DisplayName = "";
            PlayerPrefs.SetString(DisplayNameKey, "");
            PlayerPrefs.Save();
            OnDisplayNameChanged?.Invoke(DisplayName);
        }

        /// <summary>
        /// Attempt to change the display name. Returns true if valid and applied.
        /// </summary>
        public bool TrySetDisplayName(string newName)
        {
            string sanitized = SanitizeName(newName);
            if (!IsValidName(sanitized)) return false;

            DisplayName = sanitized;
            PlayerPrefs.SetString(DisplayNameKey, DisplayName);
            PlayerPrefs.Save();
            OnDisplayNameChanged?.Invoke(DisplayName);
            return true;
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            // Strip everything except alphanumeric, spaces, underscores, hyphens
            return Regex.Replace(name.Trim(), @"[^a-zA-Z0-9 _\-]", "");
        }

        private static bool IsValidName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name.Length < MinNameLength || name.Length > MaxNameLength) return false;
            return true;
        }
    }
}
