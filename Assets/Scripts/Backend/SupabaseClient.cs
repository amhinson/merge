using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

namespace MergeGame.Backend
{
    /// <summary>
    /// Low-level HTTP client for Supabase Edge Functions.
    /// Automatically selects dev or prod credentials based on build type.
    /// Dev: used in Editor and Development builds.
    /// Prod: used in Release builds.
    /// </summary>
    public class SupabaseClient : MonoBehaviour
    {
        public static SupabaseClient Instance { get; private set; }

        // ===== DEV CREDENTIALS =====
        private const string DevUrl = "https://qbgrghcpvmsnoyzmglgv.supabase.co";
        private const string DevKey = "sb_publishable_MvO6gq-SZ4E5QmW_tq_ltg_TaR37Wty";

        // ===== PROD CREDENTIALS — SET THESE =====
        private const string ProdUrl = "https://negfbluxywxsadggnwwd.supabase.co";
        private const string ProdKey = "sb_publishable_7ACIeBkB8iBcNxxDkN7Vhg_MxMuO1fW";

        private string supabaseUrl;
        private string supabaseKey;
        private string FunctionsUrl => $"{supabaseUrl}/functions/v1";

        public bool IsProduction { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            supabaseUrl = DevUrl;
            supabaseKey = DevKey;
            IsProduction = false;
#else
            supabaseUrl = ProdUrl;
            supabaseKey = ProdKey;
            IsProduction = true;
#endif
            Debug.Log($"Supabase: {(IsProduction ? "PROD" : "DEV")} → {supabaseUrl}");
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
            Debug.Log($"Supabase POST {functionName}: {jsonBody}");
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", supabaseKey);
                request.timeout = 30;

                yield return request.SendWebRequest();

                bool success = request.result == UnityWebRequest.Result.Success;
                string response = request.downloadHandler?.text ?? "";

                Debug.Log($"Supabase {functionName} → {request.responseCode} {(success ? "OK" : request.error)} — {response}");

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
                request.SetRequestHeader("apikey", supabaseKey);
                request.timeout = 30;

                yield return request.SendWebRequest();

                bool success = request.result == UnityWebRequest.Result.Success;
                string response = request.downloadHandler?.text ?? "";

                Debug.Log($"Supabase {functionName} → {request.responseCode} {(success ? "OK" : request.error)} — {response}");

                callback?.Invoke(success, response);
            }
        }
    }
}
