using UnityEngine;

namespace MergeGame.Core
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        private const string HighScoreKey = "HighScore";
        private const float MaxMultiplier = 4.0f;

        public int CurrentScore { get; private set; }
        public int HighScore { get; private set; }

        public event System.Action<int> OnScoreChanged;
        public event System.Action<int> OnHighScoreChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        public void ResetScore()
        {
            CurrentScore = 0;
            OnScoreChanged?.Invoke(CurrentScore);
        }

        /// <summary>Restore score for save/resume.</summary>
        public void SetScore(int score)
        {
            CurrentScore = score;
            OnScoreChanged?.Invoke(CurrentScore);
            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
                OnHighScoreChanged?.Invoke(HighScore);
            }
        }

        public void AddScore(int points)
        {
            CurrentScore += points;
            OnScoreChanged?.Invoke(CurrentScore);

            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
                OnHighScoreChanged?.Invoke(HighScore);
            }
        }

        /// <summary>
        /// Add score with chain combo multiplier.
        /// multiplier = 1.0 + (chainLength - 1) * 0.5, capped at 4x.
        /// </summary>
        public int AddScoreWithCombo(int basePoints, int chainLength, Vector3 worldPosition)
        {
            float multiplier = 1f + (chainLength - 1) * 0.5f;
            multiplier = Mathf.Min(multiplier, MaxMultiplier);

            int finalPoints = Mathf.RoundToInt(basePoints * multiplier);
            AddScore(finalPoints);

            return finalPoints;
        }

        public void SaveHighScore()
        {
            if (CurrentScore > PlayerPrefs.GetInt(HighScoreKey, 0))
            {
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
                PlayerPrefs.Save();
            }
        }
    }
}
