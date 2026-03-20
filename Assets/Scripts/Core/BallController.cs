using UnityEngine;
using TMPro;
using MergeGame.Data;
using MergeGame.Audio;
using MergeGame.Visual;

namespace MergeGame.Core
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TextMeshPro tierLabel;

        private BallData ballData;
        private BallTierConfig tierConfig;
        private PhysicsConfig physicsConfig;
        private bool isMerging;
        private bool hasLanded;

        // Death line tracking
        private float timeAboveDeathLine;
        private bool gameOverTriggered;
        private float flashTimer;

        public BallData BallData => ballData;
        public int TierIndex => ballData != null ? ballData.tierIndex : -1;
        public bool HasLanded => hasLanded;

        public void Initialize(BallData data, BallTierConfig config, PhysicsConfig physics = null)
        {
            ballData = data;
            tierConfig = config;
            physicsConfig = physics;
            isMerging = false;
            hasLanded = false;
            timeAboveDeathLine = 0f;
            gameOverTriggered = false;
            flashTimer = 0f;

            ApplyVisuals();
            ApplySize();
            ApplyPhysicsConfig();
            PlaySpawnAnimation();

            // Add gold shimmer for tier 11
            if (data != null && data.tierIndex >= 10)
            {
                if (GetComponent<GoldShimmer>() == null)
                    gameObject.AddComponent<GoldShimmer>();
            }
        }

        private void ApplyVisuals()
        {
            if (spriteRenderer != null && ballData != null)
            {
                spriteRenderer.color = ballData.color;
                if (ballData.sprite != null)
                {
                    spriteRenderer.sprite = ballData.sprite;
                }
            }

            // No tier labels in visual overhaul — tiers distinguished by color and size only
            if (tierLabel != null)
            {
                tierLabel.gameObject.SetActive(false);
            }
        }

        private void ApplySize()
        {
            if (ballData == null) return;

            float diameter = ballData.radius * 2f;
            transform.localScale = Vector3.one * diameter;

            var collider = GetComponent<CircleCollider2D>();
            if (collider != null)
            {
                // Match collider to visible pixel art (32px sprite, ~1.5px transparent border)
                collider.radius = 0.45f;
            }
        }

        private void ApplyPhysicsConfig()
        {
            if (physicsConfig == null || ballData == null) return;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.mass = physicsConfig.GetMassForTier(ballData.tierIndex);
                rb.linearDamping = physicsConfig.linearDrag;
                rb.angularDamping = physicsConfig.angularDrag;
                rb.gravityScale = physicsConfig.gravityScale;
            }

            var col = GetComponent<CircleCollider2D>();
            if (col != null && col.sharedMaterial != null)
            {
                // Create instance to avoid modifying shared asset
                var mat = new PhysicsMaterial2D();
                mat.bounciness = physicsConfig.GetBouncinessForTier(ballData.tierIndex);
                mat.friction = physicsConfig.GetFrictionForTier(ballData.tierIndex);
                col.sharedMaterial = mat;
            }
        }

        private void PlaySpawnAnimation()
        {
            StartCoroutine(ScaleUpCoroutine());
        }

        private System.Collections.IEnumerator ScaleUpCoroutine()
        {
            Vector3 targetScale = transform.localScale;
            transform.localScale = targetScale * 0.5f;

            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = 1f - (1f - t) * (1f - t);
                transform.localScale = Vector3.Lerp(targetScale * 0.5f, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!hasLanded)
            {
                hasLanded = true;

                // Haptic feedback for landing
                if (HapticManager.Instance != null && ballData != null)
                    HapticManager.Instance.PlayLanding(ballData.tierIndex);

                // Squash animation on landing
                StartCoroutine(SquashCoroutine());

                // Track first landing for merge-before-floor achievement
                if (MergeTracker.Instance != null)
                    MergeTracker.Instance.RecordFirstLanding();
            }

            if (isMerging) return;

            var otherBall = collision.gameObject.GetComponent<BallController>();
            if (otherBall == null) return;
            if (otherBall.isMerging) return;
            if (otherBall.TierIndex != TierIndex) return;

            if (GetInstanceID() > otherBall.GetInstanceID()) return;

            Merge(otherBall);
        }

        private System.Collections.IEnumerator SquashCoroutine()
        {
            Vector3 baseScale = transform.localScale;
            Vector3 squashed = new Vector3(baseScale.x * 1.15f, baseScale.y * 0.85f, baseScale.z);

            float elapsed = 0f;
            float duration = 0.1f;

            // Squash
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(baseScale, squashed, elapsed / duration);
                yield return null;
            }

            // Recover
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(squashed, baseScale, elapsed / duration);
                yield return null;
            }

            transform.localScale = baseScale;
        }

        private void Merge(BallController other)
        {
            isMerging = true;
            other.isMerging = true;

            Vector3 midpoint = (transform.position + other.transform.position) / 2f;
            int currentTier = TierIndex;

            bool isMaxTier = currentTier >= tierConfig.MaxTierIndex;

            if (isMaxTier)
            {
                int points = ballData.pointValue * 2;
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.AddScore(points);

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayMerge(currentTier);

                // Track merge
                RecordMerge(currentTier);

                // Particles + shake on every merge
                SpawnMergeParticles(midpoint, ballData.color, currentTier);
                TriggerMergeShake();

                Destroy(other.gameObject);
                Destroy(gameObject);
                return;
            }

            BallData nextTier = tierConfig.GetNextTier(currentTier);
            if (nextTier == null)
            {
                Destroy(other.gameObject);
                Destroy(gameObject);
                return;
            }

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(nextTier.pointValue);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayMerge(nextTier.tierIndex);

            // Track merge
            RecordMerge(nextTier.tierIndex);

            // Particles
            SpawnMergeParticles(midpoint, nextTier.color, nextTier.tierIndex);

            // Screen shake on every merge — same intensity
            TriggerMergeShake();

            SpawnMergedBall(nextTier, midpoint);

            Destroy(other.gameObject);
            Destroy(gameObject);
        }

        private void RecordMerge(int resultTier)
        {
            // Merge tracker
            if (MergeTracker.Instance != null)
                MergeTracker.Instance.RecordMerge(resultTier);

            // Haptic feedback
            if (HapticManager.Instance != null)
            {
                int chainLength = MergeTracker.Instance != null ? MergeTracker.Instance.LongestChain : 0;
                HapticManager.Instance.PlayMerge(resultTier, chainLength);
            }

            // Live rank update
            if (ScoreManager.Instance != null && GameManager.Instance != null)
            {
                int rank = GameManager.Instance.GetLiveRank();
                bool isReplay = DailySeedManager.Instance != null &&
                                DailySeedManager.Instance.CurrentAttemptType == AttemptType.Replay;

                // The LiveRankUI listens for ScoreManager.OnScoreChanged and updates itself
            }
        }

        private void SpawnMergeParticles(Vector3 position, Color color, int tier)
        {
            if (MergeParticles.Instance != null)
                MergeParticles.Instance.SpawnBurst(position, color, tier);
        }

        private void TriggerMergeShake()
        {
            // Use a static coroutine host so it survives this object being destroyed
            var cam = Camera.main;
            if (cam == null) return;
            if (physicsConfig == null) return;

            var host = GameManager.Instance;
            if (host != null)
                host.StartCoroutine(MergeShakeCoroutine(cam));
        }

        private System.Collections.IEnumerator MergeShakeCoroutine(Camera cam)
        {
            if (physicsConfig == null) yield break;

            Vector3 originalPos = cam.transform.position;
            Quaternion originalRot = cam.transform.rotation;
            float elapsed = 0f;
            float duration = physicsConfig.shakeDuration;
            float intensity = physicsConfig.shakeIntensity;
            float rotAmount = physicsConfig.shakeRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - Mathf.Pow(elapsed / duration, physicsConfig.shakeDecaySpeed);

                Vector2 offset = Random.insideUnitCircle * intensity * decay;
                cam.transform.position = originalPos + new Vector3(offset.x, offset.y, 0f);

                float rotOffset = Random.Range(-rotAmount, rotAmount) * decay;
                cam.transform.rotation = Quaternion.Euler(0, 0, rotOffset);

                yield return null;
            }

            cam.transform.position = originalPos;
            cam.transform.rotation = originalRot;
        }

        private void SpawnMergedBall(BallData nextTier, Vector3 position)
        {
            GameObject newBall = Instantiate(
                DropController.Instance.BallPrefab,
                position,
                Quaternion.identity
            );

            var controller = newBall.GetComponent<BallController>();
            if (controller != null)
            {
                controller.Initialize(nextTier, tierConfig, physicsConfig);
                controller.hasLanded = true;
            }

            var rb = newBall.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;

                // Post-merge pop force
                if (physicsConfig != null && physicsConfig.postMergePopForce > 0f)
                {
                    rb.AddForce(Vector2.up * physicsConfig.postMergePopForce, ForceMode2D.Impulse);
                }
            }
        }

        public void SetAboveDeathLine(bool above)
        {
            // Only used as a hint — actual position check happens in Update
        }

        private float DeathLineY
        {
            get { return physicsConfig != null ? physicsConfig.deathLineY : 3.5f; }
        }

        private float WarningDuration
        {
            get { return physicsConfig != null ? physicsConfig.deathLineWarningDuration : 5f; }
        }

        private bool IsActuallyAboveDeathLine()
        {
            if (ballData == null) return false;
            float ballTop = transform.position.y + ballData.radius;
            return ballTop >= DeathLineY;
        }

        private void Update()
        {
            if (!hasLanded || isMerging || gameOverTriggered)
            {
                // Restore color if not in danger
                if (spriteRenderer != null && ballData != null && timeAboveDeathLine > 0f)
                {
                    spriteRenderer.color = ballData.color;
                    timeAboveDeathLine = 0f;
                    flashTimer = 0f;
                }
                return;
            }

            // Check actual position every frame — don't rely on trigger state
            bool aboveLine = IsActuallyAboveDeathLine();

            if (aboveLine)
            {
                timeAboveDeathLine += Time.deltaTime;

                // Flash red warning
                if (spriteRenderer != null && ballData != null)
                {
                    flashTimer += Time.deltaTime;
                    float flashSpeed = Mathf.Lerp(4f, 12f, timeAboveDeathLine / WarningDuration);
                    float flash = Mathf.Sin(flashTimer * flashSpeed) * 0.5f + 0.5f;
                    spriteRenderer.color = Color.Lerp(ballData.color, Color.red, flash);
                }

                if (timeAboveDeathLine >= WarningDuration)
                {
                    gameOverTriggered = true;
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.TriggerGameOver();
                    }
                }
            }
            else
            {
                // Ball dropped below the line — reset timer and restore color
                if (timeAboveDeathLine > 0f)
                {
                    if (MergeTracker.Instance != null)
                        MergeTracker.Instance.RecordDeathLineSurvival(timeAboveDeathLine);
                }
                timeAboveDeathLine = 0f;
                flashTimer = 0f;
                if (spriteRenderer != null && ballData != null)
                    spriteRenderer.color = ballData.color;
            }
        }
    }
}
