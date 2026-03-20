using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MergeGame.Backend;

namespace MergeGame.UI
{
    /// <summary>
    /// Leaderboard display. Self-contained, repositionable component.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        [SerializeField] private Transform entriesContainer;
        [SerializeField] private GameObject entryPrefab; // Expects: RankText, NameText, ScoreText children
        [SerializeField] private TextMeshProUGUI titleText;

        public void Show(List<LeaderboardEntry> entries)
        {
            gameObject.SetActive(true);
            Populate(entries);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Populate(List<LeaderboardEntry> entries)
        {
            if (entriesContainer == null) return;

            // Clear existing
            foreach (Transform child in entriesContainer)
                Destroy(child.gameObject);

            if (entries == null || entryPrefab == null) return;

            for (int i = 0; i < entries.Count && i < 100; i++)
            {
                var entry = entries[i];
                var row = Instantiate(entryPrefab, entriesContainer);

                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{i + 1}";
                    texts[1].text = entry.display_name ?? "???";
                    texts[2].text = entry.score.ToString();
                }
            }
        }

        public void SetTitle(string title)
        {
            if (titleText != null)
                titleText.text = title;
        }
    }
}
