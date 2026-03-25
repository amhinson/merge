using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MergeGame.Backend
{
    /// <summary>
    /// Persists failed network calls to PlayerPrefs and retries when connectivity returns.
    /// Handles any Supabase function call (score submission, registration, etc.).
    /// </summary>
    public class OfflineSyncQueue : MonoBehaviour
    {
        public static OfflineSyncQueue Instance { get; private set; }

        private const string QueueKey = "offline_sync_queue";
        private bool isFlushing;

        [System.Serializable]
        private class QueueEntry
        {
            public string functionName;
            public string jsonBody;
        }

        [System.Serializable]
        private class QueueData
        {
            public List<QueueEntry> entries = new List<QueueEntry>();
        }

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
            MigrateOldQueue();
            FlushAll();
        }

        /// <summary>Migrate from old OfflineScoreQueue format if present.</summary>
        private void MigrateOldQueue()
        {
            string oldJson = PlayerPrefs.GetString("offline_score_queue", "");
            if (!string.IsNullOrEmpty(oldJson))
            {
                Enqueue("submit-score", oldJson);
                PlayerPrefs.DeleteKey("offline_score_queue");
                PlayerPrefs.Save();
                Debug.Log("[OfflineSyncQueue] Migrated old score queue entry");
            }
        }

        private void OnEnable()
        {
            Core.NetworkMonitor.OnConnectivityChanged += OnConnectivityChanged;
        }

        private void OnDisable()
        {
            Core.NetworkMonitor.OnConnectivityChanged -= OnConnectivityChanged;
        }

        private void OnConnectivityChanged(bool online)
        {
            if (online) FlushAll();
        }

        /// <summary>Queue a failed call for later retry.</summary>
        public void Enqueue(string functionName, string jsonBody)
        {
            var data = LoadQueue();
            data.entries.Add(new QueueEntry { functionName = functionName, jsonBody = jsonBody });
            SaveQueue(data);
            Debug.Log($"[OfflineSyncQueue] Queued {functionName} ({data.entries.Count} total)");
        }

        /// <summary>Try to send all queued calls.</summary>
        public void FlushAll()
        {
            if (isFlushing) return;
            var data = LoadQueue();
            if (data.entries.Count == 0) return;
            if (!Core.NetworkMonitor.QuickCheck()) return;

            Debug.Log($"[OfflineSyncQueue] Flushing {data.entries.Count} queued calls");
            isFlushing = true;
            StartCoroutine(FlushCoroutine());
        }

        private IEnumerator FlushCoroutine()
        {
            // Wait for SupabaseClient
            float waited = 0f;
            while (SupabaseClient.Instance == null && waited < 5f)
            {
                yield return new WaitForSeconds(0.5f);
                waited += 0.5f;
            }

            if (SupabaseClient.Instance == null)
            {
                isFlushing = false;
                yield break;
            }

            var data = LoadQueue();
            var remaining = new List<QueueEntry>();

            foreach (var entry in data.entries)
            {
                bool done = false;
                bool success = false;

                SupabaseClient.Instance.CallFunction(entry.functionName, entry.jsonBody, (s, response) =>
                {
                    success = s;
                    // Also clear on 409 (already submitted / duplicate)
                    if (!s && response != null && response.Contains("already"))
                        success = true;
                    done = true;
                });

                while (!done) yield return null;

                if (!success)
                {
                    remaining.Add(entry);
                    Debug.LogWarning($"[OfflineSyncQueue] {entry.functionName} failed, keeping in queue");
                }
                else
                {
                    Debug.Log($"[OfflineSyncQueue] {entry.functionName} synced successfully");
                }
            }

            // Save whatever's left
            data.entries = remaining;
            SaveQueue(data);

            isFlushing = false;
        }

        public bool HasPendingItems()
        {
            return LoadQueue().entries.Count > 0;
        }

        private QueueData LoadQueue()
        {
            string json = PlayerPrefs.GetString(QueueKey, "");
            if (string.IsNullOrEmpty(json)) return new QueueData();
            try { return JsonUtility.FromJson<QueueData>(json) ?? new QueueData(); }
            catch { return new QueueData(); }
        }

        private void SaveQueue(QueueData data)
        {
            if (data.entries.Count == 0)
                PlayerPrefs.DeleteKey(QueueKey);
            else
                PlayerPrefs.SetString(QueueKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
