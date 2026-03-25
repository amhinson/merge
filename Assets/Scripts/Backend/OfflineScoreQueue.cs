using UnityEngine;
using System.Collections;

namespace MergeGame.Backend
{
    /// <summary>
    /// Persists failed score submissions to PlayerPrefs and retries on next app launch.
    /// Only one pending score is stored at a time (one scored attempt per day).
    /// </summary>
    public class OfflineScoreQueue : MonoBehaviour
    {
        public static OfflineScoreQueue Instance { get; private set; }

        private const string QueueKey = "offline_score_queue";
        private bool isFlushing;

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
            // Try to flush any pending score from a previous session
            FlushQueue();
        }

        /// <summary>
        /// Save a score submission for later retry.
        /// Called when SubmitScore fails after all retries.
        /// </summary>
        public void Enqueue(string json)
        {
            PlayerPrefs.SetString(QueueKey, json);
            PlayerPrefs.Save();
            Debug.Log("[OfflineScoreQueue] Score queued for retry");
        }

        /// <summary>
        /// Check if there's a pending score and try to submit it.
        /// </summary>
        public void FlushQueue()
        {
            if (isFlushing) return;
            string json = PlayerPrefs.GetString(QueueKey, "");
            if (string.IsNullOrEmpty(json)) return;

            Debug.Log("[OfflineScoreQueue] Found pending score, attempting to submit");
            isFlushing = true;
            StartCoroutine(FlushCoroutine(json));
        }

        private IEnumerator FlushCoroutine(string json)
        {
            // Wait for SupabaseClient to be ready
            float waited = 0f;
            while (SupabaseClient.Instance == null && waited < 5f)
            {
                yield return new WaitForSeconds(0.5f);
                waited += 0.5f;
            }

            if (SupabaseClient.Instance == null)
            {
                Debug.LogWarning("[OfflineScoreQueue] SupabaseClient not available, will retry next launch");
                isFlushing = false;
                yield break;
            }

            bool done = false;
            bool success = false;

            SupabaseClient.Instance.CallFunction("submit-score", json, (s, response) =>
            {
                success = s;
                done = true;

                if (s)
                {
                    Debug.Log("[OfflineScoreQueue] Pending score submitted successfully");
                    PlayerPrefs.DeleteKey(QueueKey);
                    PlayerPrefs.Save();
                }
                else
                {
                    // 409 = already submitted (maybe it went through on a previous attempt)
                    if (response.Contains("already submitted"))
                    {
                        Debug.Log("[OfflineScoreQueue] Score already exists, clearing queue");
                        PlayerPrefs.DeleteKey(QueueKey);
                        PlayerPrefs.Save();
                    }
                    else
                    {
                        Debug.LogWarning($"[OfflineScoreQueue] Retry failed: {response}. Will try next launch.");
                    }
                }
            });

            // Wait for callback
            while (!done) yield return null;
            isFlushing = false;
        }

        public bool HasPendingScore()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString(QueueKey, ""));
        }
    }
}
