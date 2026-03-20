using UnityEngine;

namespace MergeGame.Data
{
    [CreateAssetMenu(fileName = "BallTierConfig", menuName = "MergeGame/Ball Tier Config")]
    public class BallTierConfig : ScriptableObject
    {
        [Tooltip("Ordered list of all ball tiers, index 0 = tier 1 (smallest)")]
        public BallData[] tiers;

        [Tooltip("Maximum tier index that can be spawned as a drop ball (exclusive upper bound)")]
        public int maxDropTier = 5;

        public int MaxTierIndex => tiers.Length - 1;

        public BallData GetTier(int index)
        {
            if (index < 0 || index >= tiers.Length) return null;
            return tiers[index];
        }

        public BallData GetNextTier(int currentIndex)
        {
            int next = currentIndex + 1;
            if (next >= tiers.Length) return null;
            return tiers[next];
        }

        public BallData GetRandomDropTier()
        {
            int index = Random.Range(0, Mathf.Min(maxDropTier, tiers.Length));
            return tiers[index];
        }
    }
}
