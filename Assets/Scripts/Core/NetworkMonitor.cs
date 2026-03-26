using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

namespace MergeGame.Core
{
    /// <summary>
    /// Central connectivity tracker. Uses Application.internetReachability for
    /// instant offline detection, then confirms with a HEAD request to avoid
    /// false positives (captive portals, DNS issues).
    /// Polls periodically while offline to detect reconnection.
    /// </summary>
    public class NetworkMonitor : MonoBehaviour
    {
        public static NetworkMonitor Instance { get; private set; }

        public static bool IsOnline { get; private set; } = true;
        public static event Action<bool> OnConnectivityChanged;

        private const float PollInterval = 30f;
        private const float PingTimeout = 3f;
        private Coroutine pollCoroutine;
        private bool isChecking;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Fast check: if system says not reachable, we're definitely offline
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                SetOnline(false);
                StartPolling();
            }
            else
            {
                // System says reachable, but confirm with actual request
                StartCoroutine(ConfirmConnectivity());
            }
        }

        /// <summary>
        /// Synchronous quick check — call before deciding whether to attempt network calls.
        /// Uses cached state, never blocks.
        /// </summary>
        public static bool QuickCheck()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return false;
            return IsOnline;
        }

        /// <summary>
        /// Force a connectivity recheck (e.g., after app resume).
        /// </summary>
        public void Recheck()
        {
            if (!isChecking)
                StartCoroutine(ConfirmConnectivity());
        }

        private IEnumerator ConfirmConnectivity()
        {
            if (isChecking) yield break;
            isChecking = true;

            // Use Supabase health endpoint
            string url = Backend.SupabaseClient.Instance != null
                ? Backend.SupabaseClient.Instance.HealthUrl
                : null;

            if (string.IsNullOrEmpty(url))
            {
                // No URL available yet — assume system reachability is correct
                SetOnline(Application.internetReachability != NetworkReachability.NotReachable);
                isChecking = false;
                yield break;
            }

            using (var request = UnityWebRequest.Head(url))
            {
                request.timeout = (int)PingTimeout;
                yield return request.SendWebRequest();

                // Any HTTP response (even 401/404) means the server is reachable.
                // Only ConnectionError or timeout means truly offline.
                bool reachable = request.result != UnityWebRequest.Result.ConnectionError;
                SetOnline(reachable);
            }

            isChecking = false;
        }

        private void SetOnline(bool online)
        {
            bool wasOnline = IsOnline;
            IsOnline = online;
            GameSession.IsOffline = !online;

            if (online && !wasOnline)
            {
                Debug.Log("[NetworkMonitor] Back online — flushing queues");
                OnConnectivityChanged?.Invoke(true);
                StopPolling();
            }
            else if (!online && wasOnline)
            {
                Debug.Log("[NetworkMonitor] Went offline");
                OnConnectivityChanged?.Invoke(false);
                StartPolling();
            }
        }

        private void StartPolling()
        {
            if (pollCoroutine != null) return;
            pollCoroutine = StartCoroutine(PollCoroutine());
        }

        private void StopPolling()
        {
            if (pollCoroutine != null)
            {
                StopCoroutine(pollCoroutine);
                pollCoroutine = null;
            }
        }

        private IEnumerator PollCoroutine()
        {
            while (!IsOnline)
            {
                yield return new WaitForSeconds(PollInterval);

                // Quick system check first
                if (Application.internetReachability == NetworkReachability.NotReachable)
                    continue;

                // System says reachable — confirm
                yield return ConfirmConnectivity();
            }
            pollCoroutine = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
                Recheck();
        }
    }
}
