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
        private const int MaxNameLength = 16;

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
            // Register player with backend on startup (best-effort)
            RegisterWithBackend();
        }

        private void RegisterWithBackend()
        {
            if (SupabaseClient.Instance == null) return;

            string json = $"{{\"device_uuid\":\"{DeviceUUID}\",\"display_name\":\"{DisplayName}\"}}";
            SupabaseClient.Instance.CallFunction("register-player", json, (success, response) =>
            {
                if (success)
                    Debug.Log("PlayerIdentity: Registered with backend");
                else
                    Debug.LogWarning($"PlayerIdentity: Backend registration failed — {response}");
            });
        }

        private void LoadOrCreateIdentity()
        {
            // UUID
            DeviceUUID = PlayerPrefs.GetString(UUIDKey, "");
            if (string.IsNullOrEmpty(DeviceUUID))
            {
                DeviceUUID = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString(UUIDKey, DeviceUUID);
            }

            // Display name
            DisplayName = PlayerPrefs.GetString(DisplayNameKey, "");
            if (string.IsNullOrEmpty(DisplayName))
            {
                DisplayName = GenerateDefaultName();
                PlayerPrefs.SetString(DisplayNameKey, DisplayName);
            }

            PlayerPrefs.Save();
        }

        private static string GenerateDefaultName()
        {
            int digits = Random.Range(100000, 999999);
            return $"Player{digits}";
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
