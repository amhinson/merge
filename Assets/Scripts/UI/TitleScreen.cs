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

        [Header("Streak Visual")]
        [SerializeField] private Transform streakDotsContainer;

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
            HexColor("00FFE0"), // D
            HexColor("4D9FFF"), // A
            HexColor("8B5CF6"), // I
            HexColor("FF2D95"), // L
            HexColor("FF6BC2"), // Y
            HexColor("FF4545"), // D
            HexColor("FF8A2D"), // R
            HexColor("FFBB33"), // O
            HexColor("39FF6B"), // P
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
                string source = "DAILY DROP";
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

            // Streak with dots
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

        private void RefreshStreak()
        {
            if (StreakManager.Instance == null) return;
            int streak = StreakManager.Instance.CurrentStreak;

            if (streakText != null)
            {
                if (streak > 0)
                {
                    streakText.richText = true;
                    string flameColor = ColorUtility.ToHtmlStringRGB(HexColor("FF8A2D"));
                    string mutedColor = ColorUtility.ToHtmlStringRGB(HexColor("888888"));
                    streakText.text = $"<color=#{flameColor}>*</color> {streak} <size=70%><color=#{mutedColor}>streak</color></size>";
                    streakText.gameObject.SetActive(true);
                }
                else
                {
                    streakText.text = "";
                    streakText.gameObject.SetActive(false);
                }
            }

            // Streak dots
            if (streakDotsContainer != null)
            {
                // Clear existing
                foreach (Transform child in streakDotsContainer)
                    Destroy(child.gameObject);

                if (streak <= 0) return;

                int dotsToShow = Mathf.Min(streak, 30);
                Color dotColor = HexColor("FF8A2D");

                // Create a small circle texture for dots
                var dotSprite = CreateCircleDot();

                for (int i = 0; i < dotsToShow; i++)
                {
                    var dot = new GameObject($"Dot{i}");
                    dot.transform.SetParent(streakDotsContainer, false);
                    var img = dot.AddComponent<Image>();
                    img.sprite = dotSprite;
                    img.color = dotColor;
                    var le = dot.AddComponent<LayoutElement>();
                    float dotSize = streak <= 15 ? 12 : 8;
                    le.preferredWidth = dotSize;
                    le.preferredHeight = dotSize;
                }

                if (streak > 30)
                {
                    var plus = new GameObject("Plus");
                    plus.transform.SetParent(streakDotsContainer, false);
                    var tmp = plus.AddComponent<TextMeshProUGUI>();
                    tmp.text = "+";
                    tmp.fontSize = 14;
                    tmp.color = dotColor;
                    tmp.alignment = TextAlignmentOptions.Center;
                    var le = plus.AddComponent<LayoutElement>();
                    le.preferredWidth = 16;
                    le.preferredHeight = 12;
                }
            }
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

        private static Sprite CreateCircleDot()
        {
            int s = 8;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            int center = s / 2;
            int r = s / 2 - 1;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    int dx = x - center;
                    int dy = y - center;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= r * r ? Color.white : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out Color c);
            return c;
        }
    }
}
