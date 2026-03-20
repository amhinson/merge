using UnityEngine;

namespace MergeGame.Data
{
    [CreateAssetMenu(fileName = "BallData", menuName = "MergeGame/Ball Data")]
    public class BallData : ScriptableObject
    {
        [Header("Tier Settings")]
        public int tierIndex;
        public float radius = 0.25f;
        public Color color = Color.white;
        public Sprite sprite;
        public int pointValue = 1;

        [Header("Display")]
        public string displayName;
    }
}
