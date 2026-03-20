using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

namespace MergeGame.Backend
{
    /// <summary>
    /// Low-level HTTP client for Supabase Edge Functions.
    /// </summary>
    public class SupabaseClient : MonoBehaviour
    {
        public static SupabaseClient Instance { get; private set; }

        // ===== PLACEHOLDER: Replace with your Supabase project credentials =====
        [Header("Supabase Config — SET THESE")]
        [SerializeField] private string supabaseUrl = "https://YOUR_PROJECT_ID.supabase.co";
        [SerializeField] private string supabaseAnonKey = "YOUR_ANON_KEY_HERE";
        // ========================================================================

        private string FunctionsUrl => $"{supabaseUrl}/functions/v1";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void CallFunction(string functionName, string jsonBody, Action<bool, string> callback)
        {
            StartCoroutine(CallFunctionCoroutine(functionName, jsonBody, callback));
        }

        public void CallFunctionGet(string functionName, string queryParams, Action<bool, string> callback)
        {
            StartCoroutine(CallFunctionGetCoroutine(functionName, queryParams, callback));
        }

        private IEnumerator CallFunctionCoroutine(string functionName, string jsonBody, Action<bool, string> callback)
        {
            string url = $"{FunctionsUrl}/{functionName}";
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", supabaseAnonKey);
                request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");
                request.timeout = 10;

                yield return request.SendWebRequest();

                bool success = request.result == UnityWebRequest.Result.Success;
                string response = request.downloadHandler?.text ?? "";

                if (!success)
                {
                    Debug.LogWarning($"Supabase {functionName} failed: {request.error} — {response}");
                }

                callback?.Invoke(success, response);
            }
        }

        private IEnumerator CallFunctionGetCoroutine(string functionName, string queryParams, Action<bool, string> callback)
        {
            string url = $"{FunctionsUrl}/{functionName}";
            if (!string.IsNullOrEmpty(queryParams))
                url += $"?{queryParams}";

            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("apikey", supabaseAnonKey);
                request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");
                request.timeout = 10;

                yield return request.SendWebRequest();

                bool success = request.result == UnityWebRequest.Result.Success;
                string response = request.downloadHandler?.text ?? "";

                if (!success)
                {
                    Debug.LogWarning($"Supabase {functionName} failed: {request.error} — {response}");
                }

                callback?.Invoke(success, response);
            }
        }
    }
}
