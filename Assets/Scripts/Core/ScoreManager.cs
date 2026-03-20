using UnityEngine;

namespace MergeGame.Core
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        private const string HighScoreKey = "HighScore";

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
