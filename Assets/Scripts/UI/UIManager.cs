using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject playingPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Menu")]
        [SerializeField] private Button playButton;

        [Header("Playing UI")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private Image nextBallPreview;
        [SerializeField] private TextMeshProUGUI nextBallLabel;
        [SerializeField] private float previewBaseSize = 120f;

        [Header("Shake UI")]
        [SerializeField] private Button shakeButton;
        [SerializeField] private TextMeshProUGUI shakeCountText;

        [Header("Game Over UI")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI finalHighScoreText;
        [SerializeField] private Button restartButton;

        [Header("References")]
        [SerializeField] private BallTierConfig tierConfig;

        private void Start()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
            if (shakeButton != null)
                shakeButton.onClick.AddListener(OnShakeClicked);

            // Subscribe to shake changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShakesChanged += UpdateShakeCount;
            }

            // Subscribe to score changes
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += UpdateScore;
                ScoreManager.Instance.OnHighScoreChanged += UpdateHighScore;
            }

            // Subscribe to next ball changes
            if (DropController.Instance != null)
            {
                DropController.Instance.OnNextBallChanged += UpdateNextBallPreview;
            }
        }

        private void OnDestroy()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= UpdateScore;
                ScoreManager.Instance.OnHighScoreChanged -= UpdateHighScore;
            }

            if (DropController.Instance != null)
            {
                DropController.Instance.OnNextBallChanged -= UpdateNextBallPreview;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShakesChanged -= UpdateShakeCount;
            }
        }

        public void ShowMenu()
        {
            SetPanelActive(menuPanel, true);
            SetPanelActive(playingPanel, false);
            SetPanelActive(gameOverPanel, false);
        }

        public void ShowPlaying()
        {
            SetPanelActive(menuPanel, false);
            SetPanelActive(playingPanel, true);
            SetPanelActive(gameOverPanel, false);

            UpdateScore(0);
            if (ScoreManager.Instance != null)
            {
                UpdateHighScore(ScoreManager.Instance.HighScore);
            }
        }

        public void ShowGameOver(int finalScore, int highScore)
        {
            SetPanelActive(menuPanel, false);
            SetPanelActive(playingPanel, true); // Keep score visible
            SetPanelActive(gameOverPanel, true);

            if (finalScoreText != null)
                finalScoreText.text = $"Score: {finalScore}";
            if (finalHighScoreText != null)
                finalHighScoreText.text = $"Best: {highScore}";
        }

        private void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = score.ToString();
        }

        private void UpdateHighScore(int highScore)
        {
            if (highScoreText != null)
                highScoreText.text = $"Best: {highScore}";
        }

        private void UpdateNextBallPreview(BallData nextBall)
        {
            if (nextBallPreview != null && nextBall != null)
            {
                nextBallPreview.color = nextBall.color;
                if (nextBall.sprite != null)
                    nextBallPreview.sprite = nextBall.sprite;

                // Scale to match ball radius (tier 1 smallest ~ 0.35, tier 5 ~ 0.85)
                float scale = nextBall.radius / 0.85f;
                float size = previewBaseSize * scale;
                nextBallPreview.rectTransform.sizeDelta = new Vector2(size, size);
            }

            if (nextBallLabel != null && nextBall != null)
            {
                nextBallLabel.text = (nextBall.tierIndex + 1).ToString();
            }
        }

        private void UpdateShakeCount(int remaining)
        {
            if (shakeCountText != null)
                shakeCountText.text = $"Shake x{remaining}";
            if (shakeButton != null)
                shakeButton.interactable = remaining > 0;
        }

        private void OnShakeClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerShake();
        }

        private void OnPlayClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayButtonPressed();
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRestartButtonPressed();
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }
    }
}
