using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MergeGame.Backend;
using MergeGame.Core;

namespace MergeGame.UI
{
    /// <summary>
    /// Leaderboard display. Creates rows dynamically — no prefab needed.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        [SerializeField] private Transform entriesContainer;
        [SerializeField] private TextMeshProUGUI titleText;

        private static readonly Color RowColor = new Color(0.14f, 0.14f, 0.18f);
        private static readonly Color PlayerRowColor = new Color(0.05f, 0.35f, 0.32f);
        private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.95f);
        private static readonly Color TextMuted = new Color(0.55f, 0.55f, 0.60f);

        public void Populate(List<LeaderboardEntry> entries)
        {
            if (entriesContainer == null)
            {
                Debug.LogWarning("LeaderboardUI: entriesContainer is null");
                return;
            }

            Debug.Log($"LeaderboardUI.Populate: {entries?.Count ?? 0} entries, container={entriesContainer.name}, childCount={entriesContainer.childCount}");

            // Clear existing rows
            foreach (Transform child in entriesContainer)
                Destroy(child.gameObject);

            if (entries == null || entries.Count == 0)
            {
                CreateMessageRow("No scores yet today");
                return;
            }

            string playerUUID = PlayerIdentity.Instance != null ? PlayerIdentity.Instance.DeviceUUID : "";
            bool playerFound = false;

            for (int i = 0; i < entries.Count && i < 100; i++)
            {
                var entry = entries[i];
                bool isPlayer = entry.device_uuid == playerUUID;
                if (isPlayer) playerFound = true;

                CreateEntryRow(i + 1, entry.display_name ?? "???", entry.score, isPlayer);
            }

            if (!playerFound && !string.IsNullOrEmpty(playerUUID))
            {
                // Player not in top 100 — show separator and their row
                CreateSeparatorRow();
                string name = PlayerIdentity.Instance != null ? PlayerIdentity.Instance.DisplayName : "You";
                CreateEntryRow(-1, name, 0, true);
            }
        }

        private void CreateEntryRow(int rank, string displayName, int score, bool isPlayer)
        {
            var row = new GameObject($"Row_{rank}");
            row.transform.SetParent(entriesContainer, false);
            row.AddComponent<RectTransform>();

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 55;
            le.minHeight = 55;

            var bg = row.AddComponent<Image>();
            bg.color = isPlayer ? PlayerRowColor : RowColor;

            // Text in child (can't have Image + TMP on same object)
            var textObj = new GameObject("Label");
            textObj.transform.SetParent(row.transform, false);
            var trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            string rankStr = rank > 0 ? $"#{rank}" : "--";
            string name = displayName.Length > 12 ? displayName.Substring(0, 12) : displayName;
            tmp.text = $"  {rankStr}  {name}  {score}";
            tmp.fontSize = 20;
            tmp.color = isPlayer ? new Color(1f, 0.9f, 0.4f) : TextWhite;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void CreateSeparatorRow()
        {
            var sep = new GameObject("Separator");
            sep.transform.SetParent(entriesContainer, false);
            var rt = sep.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 10);

            var img = sep.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.35f, 0.5f);

            var le = sep.AddComponent<LayoutElement>();
            le.preferredHeight = 3;
        }

        private void CreateMessageRow(string message)
        {
            var row = new GameObject("Message");
            row.transform.SetParent(entriesContainer, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 80);

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            le.minHeight = 80;

            var text = row.AddComponent<TextMeshProUGUI>();
            text.text = message;
            text.fontSize = 20;
            text.color = TextMuted;
            text.alignment = TextAlignmentOptions.Center;
        }

        private TextMeshProUGUI CreateRowText(Transform parent, string name,
            string text, float fontSize, Color color, float preferredWidth)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            if (preferredWidth > 0)
            {
                var le = obj.AddComponent<LayoutElement>();
                le.preferredWidth = preferredWidth;
            }

            return tmp;
        }

        public void SetTitle(string title)
        {
            if (titleText != null)
                titleText.text = title;
        }
    }
}
