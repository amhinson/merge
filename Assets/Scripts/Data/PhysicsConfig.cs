using UnityEngine;

namespace MergeGame.Data
{
    [CreateAssetMenu(fileName = "PhysicsConfig", menuName = "MergeGame/Physics Config")]
    public class PhysicsConfig : ScriptableObject
    {
        [Header("Global")]
        public float gravityScale = 1f;

        [Header("Ball Physics - Defaults (overridden per-tier if curves are set)")]
        public float baseMass = 1f;
        public float massPerTier = 0.3f;
        public float baseBounciness = 0.2f;
        [Tooltip("Bounciness change per tier (can be negative for heavier = less bouncy)")]
        public float bouncinessPerTier = -0.01f;
        public float baseFriction = 0.4f;
        public float frictionPerTier = 0.02f;
        public float linearDrag = 0.1f;
        public float angularDrag = 0.05f;

        [Header("Drop Behavior")]
        public float dropHeight = 4.5f;
        public float cooldownDuration = 0.5f;

        [Header("Merge Behavior")]
        public float mergeAbsorbDuration = 0.12f;
        public float mergeScaleDuration = 0.18f;
        public float mergeRadialImpulse = 1.5f;
        [Tooltip("How far (in multiples of merged ball radius) the radial impulse reaches")]
        public float mergeRadialRange = 3f;
        public float postMergePopForce = 1.5f;
        [Tooltip("Minimum time (seconds) between successive merges for visual clarity")]
        public float chainReactionDelay = 0.05f;
        [Tooltip("Screen shake intensity multiplier by tier (applied to tiers 8+)")]
        public AnimationCurve shakeIntensityByTier = AnimationCurve.Linear(0, 0, 10, 1f);

        [Header("Container")]
        public float containerWidth = 4.5f;
        public float containerHeight = 8f;
        public float wallBounciness = 0.1f;
        public float wallFriction = 0.5f;
        public float containerBottomY = -4.5f;

        [Header("Death Line")]
        public float deathLineY = 3.5f;
        public float deathLineWarningDuration = 5f;
        [Tooltip("Distance below death line at which it becomes visible")]
        public float deathLineProximityThreshold = 1.5f;

        [Header("Screen Shake (triggers on every merge)")]
        public float shakeIntensity = 0.06f;
        public float shakeDuration = 0.12f;
        public float shakeDecaySpeed = 8f;
        [Tooltip("Small random rotation component in degrees")]
        public float shakeRotation = 0.5f;

        public float GetMassForTier(int tier)
        {
            return baseMass + massPerTier * tier;
        }

        public float GetBouncinessForTier(int tier)
        {
            return Mathf.Clamp01(baseBounciness + bouncinessPerTier * tier);
        }

        public float GetFrictionForTier(int tier)
        {
            return Mathf.Max(0f, baseFriction + frictionPerTier * tier);
        }

        public float GetShakeIntensity(int tier)
        {
            if (tier < 8) return 0f;
            return shakeIntensityByTier.Evaluate(tier);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        public void LoadFromJson(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }
    }
}
