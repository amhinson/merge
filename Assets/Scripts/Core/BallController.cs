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

        private static int nextSortingOrder = 10; // Incrementing counter for unique z-order

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
        public bool IsMerging => isMerging;
        public float TimeAboveDeathLine => timeAboveDeathLine;

        /// <summary>Restore state for save/resume.</summary>
        public void RestoreState(bool landed, float deathLineTime)
        {
            hasLanded = landed;
            timeAboveDeathLine = deathLineTime;
        }

        public void Initialize(BallData data, BallTierConfig config, PhysicsConfig physics = null, bool skipSpawnAnimation = false)
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
            if (!skipSpawnAnimation) PlaySpawnAnimation();

            // Add waveform animator
            var waveAnim = GetComponent<WaveformAnimator>();
            if (waveAnim == null)
                waveAnim = gameObject.AddComponent<WaveformAnimator>();
            waveAnim.Initialize(data, data.color);
        }

        private void ApplyVisuals()
        {
            if (spriteRenderer != null && ballData != null)
            {
                spriteRenderer.color = Color.white;
                if (ballData.sprite != null)
                    spriteRenderer.sprite = ballData.sprite;

                // Unique sorting order — newer balls render on top
                spriteRenderer.sortingOrder = nextSortingOrder++;
            }

            if (tierLabel != null)
            {
                tierLabel.gameObject.SetActive(false);
            }
        }

        private void ApplySize()
        {
            if (ballData == null) return;

            // Sprite is generated at native display resolution — no scaling needed
            transform.localScale = Vector3.one;

            var collider = GetComponent<CircleCollider2D>();
            if (collider != null)
            {
                // Collider radius in local space = ball radius in world units
                // (since localScale is 1, local = world)
                collider.radius = ballData.radius;
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

                // Landing squash removed for cleaner look

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

            // Run the animated merge on a host that won't be destroyed mid-coroutine
            var host = GameManager.Instance;
            if (host != null)
                host.StartCoroutine(MergeCoroutine(other));
            else
                MergeImmediate(other); // fallback if no host
        }

        private void MergeImmediate(BallController other)
        {
            Vector3 spawnPos = transform.position.y <= other.transform.position.y
                ? transform.position : other.transform.position;
            int currentTier = TierIndex;
            bool isMaxTier = currentTier >= tierConfig.MaxTierIndex;

            if (isMaxTier)
            {
                RecordMerge(currentTier, spawnPos);
                int chainLen = MergeTracker.Instance != null ? MergeTracker.Instance.CurrentChainLength : 1;
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScoreWithCombo(ballData.pointValue * 2, chainLen, spawnPos);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayMerge(currentTier);
                SpawnMergeParticles(spawnPos, ballData.color, currentTier);
                Destroy(other.gameObject);
                Destroy(gameObject);
                return;
            }

            BallData nextTier = tierConfig.GetNextTier(currentTier);
            if (nextTier == null) { Destroy(other.gameObject); Destroy(gameObject); return; }

            RecordMerge(currentTier, spawnPos);
            int chain = MergeTracker.Instance != null ? MergeTracker.Instance.CurrentChainLength : 1;
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddScoreWithCombo(nextTier.pointValue, chain, spawnPos);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMerge(nextTier.tierIndex);
            SpawnMergeParticles(spawnPos, nextTier.color, nextTier.tierIndex);
            SpawnMergedBall(nextTier, spawnPos);
            Destroy(other.gameObject);
            Destroy(gameObject);
        }

        private System.Collections.IEnumerator MergeCoroutine(BallController other)
        {
            // --- Phase 1: Absorb — both balls shrink toward merge point ---
            Vector3 spawnPos = transform.position.y <= other.transform.position.y
                ? transform.position : other.transform.position;
            int currentTier = TierIndex;
            float oldRadius = ballData.radius;

            Vector3 startPosA = transform.position;
            Vector3 startPosB = other.transform.position;
            Vector3 startScaleA = transform.localScale;
            Vector3 startScaleB = other.transform.localScale;

            // Freeze physics on both balls during absorb
            var rbA = GetComponent<Rigidbody2D>();
            var rbB = other.GetComponent<Rigidbody2D>();
            if (rbA != null) { rbA.linearVelocity = Vector2.zero; rbA.bodyType = RigidbodyType2D.Kinematic; }
            if (rbB != null) { rbB.linearVelocity = Vector2.zero; rbB.bodyType = RigidbodyType2D.Kinematic; }

            float absorbDuration = physicsConfig != null ? physicsConfig.mergeAbsorbDuration : 0.12f;
            float elapsed = 0f;

            while (elapsed < absorbDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / absorbDuration;
                // Ease in (accelerate into the merge point)
                float eased = t * t;

                // Move toward merge point and shrink
                if (this != null && gameObject != null)
                {
                    transform.position = Vector3.Lerp(startPosA, spawnPos, eased);
                    transform.localScale = Vector3.Lerp(startScaleA, startScaleA * 0.3f, eased);
                }
                if (other != null && other.gameObject != null)
                {
                    other.transform.position = Vector3.Lerp(startPosB, spawnPos, eased);
                    other.transform.localScale = Vector3.Lerp(startScaleB, startScaleB * 0.3f, eased);
                }

                yield return null;
            }

            // --- Phase 2: Destroy originals, spawn merged ball ---
            bool isMaxTier = currentTier >= tierConfig.MaxTierIndex;

            if (isMaxTier)
            {
                RecordMerge(currentTier, spawnPos);
                int chainLen = MergeTracker.Instance != null ? MergeTracker.Instance.CurrentChainLength : 1;
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScoreWithCombo(ballData.pointValue * 2, chainLen, spawnPos);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayMerge(currentTier);
                SpawnMergeParticles(spawnPos, ballData.color, currentTier);
                if (other != null) Destroy(other.gameObject);
                if (this != null) Destroy(gameObject);
                yield break;
            }

            BallData nextTier = tierConfig.GetNextTier(currentTier);
            if (nextTier == null)
            {
                if (other != null) Destroy(other.gameObject);
                if (this != null) Destroy(gameObject);
                yield break;
            }

            RecordMerge(currentTier, spawnPos);
            int chain = MergeTracker.Instance != null ? MergeTracker.Instance.CurrentChainLength : 1;
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddScoreWithCombo(nextTier.pointValue, chain, spawnPos);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMerge(nextTier.tierIndex);
            SpawnMergeParticles(spawnPos, nextTier.color, nextTier.tierIndex);

            if (other != null) Destroy(other.gameObject);
            if (this != null) Destroy(gameObject);

            // Spawn merged ball with scale animation from old size → new size
            SpawnMergedBall(nextTier, spawnPos, oldRadius);
        }

        private void RecordMerge(int resultTier, Vector3 mergePosition = default)
        {
            // Merge tracker
            if (MergeTracker.Instance != null)
                MergeTracker.Instance.RecordMerge(resultTier, mergePosition);

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

        private void SpawnMergedBall(BallData nextTier, Vector3 position, float oldRadius = 0f)
        {
            GameObject newBall = Instantiate(
                DropController.Instance.BallPrefab,
                position,
                Quaternion.identity
            );

            bool hasMergeScale = oldRadius > 0f;
            var controller = newBall.GetComponent<BallController>();
            if (controller != null)
            {
                controller.Initialize(nextTier, tierConfig, physicsConfig, skipSpawnAnimation: hasMergeScale);
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

            // Gentle radial impulse on nearby balls — simulates the larger ball squeezing in
            // Same-tier balls are exempt so chain merges still happen naturally
            ApplyRadialImpulse(position, nextTier.radius, physicsConfig, nextTier.tierIndex);

            // Scale animation: old ball size → new ball size
            if (oldRadius > 0f && controller != null)
            {
                float scaleRatio = oldRadius / nextTier.radius;
                float scaleDuration = physicsConfig != null ? physicsConfig.mergeScaleDuration : 0.18f;
                var host = GameManager.Instance;
                if (host != null)
                    host.StartCoroutine(MergeScaleCoroutine(newBall.transform, scaleRatio, scaleDuration));
            }
        }

        private static void ApplyRadialImpulse(Vector3 center, float mergedRadius, PhysicsConfig config, int mergedTier = -1)
        {
            float rangeMul = config != null ? config.mergeRadialRange : 3f;
            float impulseRadius = mergedRadius * rangeMul;
            float impulseStrength = config != null ? config.mergeRadialImpulse : 1.5f;

            var hits = Physics2D.OverlapCircleAll(center, impulseRadius);
            foreach (var hit in hits)
            {
                var otherRb = hit.attachedRigidbody;
                if (otherRb == null || otherRb.bodyType != RigidbodyType2D.Dynamic) continue;

                // Skip same-tier balls so they can still chain-merge
                var otherBall = hit.GetComponent<BallController>();
                if (otherBall != null && mergedTier >= 0 && otherBall.TierIndex == mergedTier) continue;

                // Skip the merged ball itself
                Vector2 diff = (Vector2)hit.transform.position - (Vector2)center;
                float dist = diff.magnitude;
                if (dist < 0.01f) continue;

                // Inverse falloff — closer balls get pushed more
                float falloff = 1f - Mathf.Clamp01(dist / impulseRadius);
                Vector2 force = diff.normalized * impulseStrength * falloff;
                otherRb.AddForce(force, ForceMode2D.Impulse);
            }
        }

        private static System.Collections.IEnumerator MergeScaleCoroutine(Transform ball, float startRatio, float duration)
        {
            if (ball == null) yield break;

            Vector3 targetScale = ball.localScale;
            ball.localScale = targetScale * startRatio;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (ball == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease-out with slight overshoot (back ease): overshoots ~10% then settles
                float s = 1.4f;
                float eased = 1f + (t - 1f) * (t - 1f) * ((s + 1f) * (t - 1f) + s);
                ball.localScale = Vector3.LerpUnclamped(targetScale * startRatio, targetScale, eased);
                yield return null;
            }

            if (ball != null)
                ball.localScale = targetScale;
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

        private int frameSkipCounter;

        private void Update()
        {
            if (!hasLanded || isMerging || gameOverTriggered)
            {
                // Restore color if not in danger
                if (timeAboveDeathLine > 0f)
                {
                    if (spriteRenderer != null && ballData != null)
                        spriteRenderer.color = Color.white;
                    timeAboveDeathLine = 0f;
                    flashTimer = 0f;
                }
                return;
            }

            // Throttle death line checks: only check every 3rd frame if not already in danger
            if (timeAboveDeathLine <= 0f)
            {
                frameSkipCounter++;
                if (frameSkipCounter % 3 != 0) return;
            }

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
                    spriteRenderer.color = Color.Lerp(Color.white, Color.red, flash);
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
                    spriteRenderer.color = Color.white;
            }
        }
    }
}
