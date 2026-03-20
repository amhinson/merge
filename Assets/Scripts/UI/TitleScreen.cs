using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;
using MergeGame.Data;
using MergeGame.Visual;

namespace MergeGame.UI
{
    /// <summary>
    /// Title screen — shown on every app launch.
    /// Neon colored title, decorative ball cluster, animated play button waveform, streak dots.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI streakText;


        [Header("References")]
        [SerializeField] private BallTierConfig tierConfig;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI playButtonLabel;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button settingsButton;

        // Neon letter colors matching tier palette
        private static readonly Color[] LetterColors = new[]
        {
            HexColor("00FFE0"), // O
            HexColor("4D9FFF"), // V
            HexColor("8B5CF6"), // E
            HexColor("FF2D95"), // R
            HexColor("FF6BC2"), // T
            HexColor("FF4545"), // O
            HexColor("FF8A2D"), // N
            HexColor("FFBB33"), // E
        };

        public Button PlayButton => playButton;
        public Button LeaderboardButton => leaderboardButton;
        public Button SettingsButton => settingsButton;

        [Header("Decorative Ball")]
        [SerializeField] private RawImage decorativeBallImage;

        private RenderTexture ballRT;
        private Camera ballCamera;
        private GameObject ballPreviewObj;

        public void CleanupCluster()
        {
            if (ballPreviewObj != null) { Destroy(ballPreviewObj); ballPreviewObj = null; }
            if (ballCamera != null) { Destroy(ballCamera.gameObject); ballCamera = null; }
            if (ballRT != null) { ballRT.Release(); ballRT = null; }
        }

        public void Refresh()
        {
            // Neon colored title
            if (titleText != null)
            {
                titleText.richText = true;
                string source = "OVERTONE";
                string colored = "";
                int colorIdx = 0;
                foreach (char c in source)
                {
                    if (c == ' ')
                    {
                        colored += " ";
                        continue;
                    }
                    Color col = LetterColors[colorIdx % LetterColors.Length];
                    colored += $"<color=#{ColorUtility.ToHtmlStringRGB(col)}>{c}</color>";
                    colorIdx++;
                }
                titleText.text = colored;
            }

            // Day number
            if (dayText != null && DailySeedManager.Instance != null)
            {
                DailySeedManager.Instance.RefreshDay();
                dayText.text = $"Day #{DailySeedManager.Instance.DayNumber}";
            }

            RefreshStreak();

            // Decorative ball
            SpawnDecorativeBall();

            // Play button label
            if (playButtonLabel != null && DailySeedManager.Instance != null)
            {
                DailySeedManager.Instance.RefreshDay();
                bool alreadyScored = DailySeedManager.Instance.HasCompletedScoredAttempt();
                playButtonLabel.text = alreadyScored ? "Play Again" : "Play";
            }

            // Decorative ball cluster (only spawn once)
        }

        // Streak color thresholds — easy to adjust
        private static readonly (int minDays, string hex)[] StreakColorThresholds = new[]
        {
            (100, "FFD700"),  // Gold
            (60,  "FFBB33"),  // Warm amber
            (30,  "FF8A2D"),  // Neon orange
            (21,  "FF2D95"),  // Magenta
            (14,  "8B5CF6"),  // Violet
            (7,   "4D9FFF"),  // Blue
            (3,   "00FFE0"),  // Cyan
            (1,   "888888"),  // Muted gray
        };

        private void RefreshStreak()
        {
            if (StreakManager.Instance == null) return;
            int streak = StreakManager.Instance.CurrentStreak;

            if (streakText == null) return;

            if (streak <= 0)
            {
                streakText.text = "";
                streakText.gameObject.SetActive(false);
                return;
            }

            streakText.richText = true;
            streakText.gameObject.SetActive(true);

            string flameHex = "FF8A2D"; // Flame always warm amber
            string numberHex = "888888";

            // Find the color for this streak count
            foreach (var (minDays, hex) in StreakColorThresholds)
            {
                if (streak >= minDays)
                {
                    numberHex = hex;
                    break;
                }
            }

            string mutedHex = "666666";
            streakText.text = $"<color=#{flameHex}>*</color> <color=#{numberHex}>{streak}</color> <size=70%><color=#{mutedHex}>streak</color></size>";
        }


        private void SpawnDecorativeBall()
        {
            if (decorativeBallImage == null || tierConfig == null) return;
            if (ballPreviewObj != null) return; // Already spawned

            var data = tierConfig.GetTier(3); // Tier 4 (0-indexed)
            if (data == null) return;

            // Create a RenderTexture
            ballRT = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGB32);
            ballRT.filterMode = FilterMode.Point;
            ballRT.Create();

            // Create an offscreen camera to render just this ball
            var camObj = new GameObject("BallPreviewCamera");
            camObj.transform.position = new Vector3(100f, 100f, -10f); // Far offscreen
            ballCamera = camObj.AddComponent<Camera>();
            ballCamera.orthographic = true;
            ballCamera.orthographicSize = 0.8f;
            ballCamera.clearFlags = CameraClearFlags.SolidColor;
            ballCamera.backgroundColor = Color.clear;
            ballCamera.targetTexture = ballRT;
            ballCamera.cullingMask = 1 << 31; // Only render layer 31
            ballCamera.depth = -10;

            // Create the ball at the camera's position
            ballPreviewObj = new GameObject("DecorativeBall");
            ballPreviewObj.layer = 31;
            ballPreviewObj.transform.position = new Vector3(100f, 100f, 0f);

            float diameter = data.radius * 2f;
            ballPreviewObj.transform.localScale = Vector3.one * diameter;

            var sr = ballPreviewObj.AddComponent<SpriteRenderer>();
            sr.sprite = data.sprite;
            sr.color = Color.white;
            sr.sortingOrder = 0;
            sr.gameObject.layer = 31;

            // Add waveform animation
            var waveAnim = ballPreviewObj.AddComponent<WaveformAnimator>();
            waveAnim.Initialize(data, data.color);

            // Assign to the UI RawImage
            decorativeBallImage.texture = ballRT;
            decorativeBallImage.color = Color.white;
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out Color c);
            return c;
        }
    }
}
