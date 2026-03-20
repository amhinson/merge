using UnityEngine;
using MergeGame.Data;
using MergeGame.Audio;

namespace MergeGame.Core
{
    public class DropController : MonoBehaviour
    {
        public static DropController Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private BallTierConfig tierConfig;
        [SerializeField] private PhysicsConfig physicsConfig;
        [SerializeField] private GameObject ballPrefab;

        [Header("Drop Settings")]
        [SerializeField] private float dropY = 4.5f;
        [SerializeField] private float leftWallX = -2.5f;
        [SerializeField] private float rightWallX = 2.5f;
        [SerializeField] private float cooldownDuration = 0.5f;

        [Header("Guide Line")]
        [SerializeField] private LineRenderer guideLine;

        public GameObject BallPrefab => ballPrefab;

        private GameObject previewBall;
        private BallData currentBallData;
        private BallData nextBallData;
        private bool isActive;
        private float cooldownTimer;
        private bool inCooldown;
        private Camera mainCamera;

        public event System.Action<BallData> OnNextBallChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            mainCamera = Camera.main;
        }

        public void SetActive(bool active)
        {
            isActive = active;
            if (active)
            {
                // Read from PhysicsConfig if available
                if (physicsConfig != null)
                {
                    dropY = physicsConfig.dropHeight;
                    cooldownDuration = physicsConfig.cooldownDuration;
                    leftWallX = -physicsConfig.containerWidth / 2f;
                    rightWallX = physicsConfig.containerWidth / 2f;
                }

                inCooldown = true;
                cooldownTimer = cooldownDuration;

                // Get first ball from daily seed or random
                nextBallData = GetNextBallData();
            }
            else
            {
                DestroyPreview();
            }

            if (guideLine != null)
            {
                guideLine.enabled = active;
            }
        }

        private BallData GetNextBallData()
        {
            if (DailySeedManager.Instance != null)
                return DailySeedManager.Instance.GetNextBall();
            return tierConfig.GetRandomDropTier();
        }

        private void Update()
        {
            if (!isActive) return;

            if (inCooldown)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    inCooldown = false;
                    SpawnPreview();
                }
                return;
            }

            if (previewBall == null || mainCamera == null || currentBallData == null) return;

            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            float clampedX = Mathf.Clamp(
                mouseWorld.x,
                leftWallX + currentBallData.radius,
                rightWallX - currentBallData.radius
            );

            previewBall.transform.position = new Vector3(clampedX, dropY, 0f);

            UpdateGuideLine(clampedX);

            if (Input.GetMouseButtonUp(0))
            {
                DropBall(clampedX);
            }
        }

        private void UpdateGuideLine(float x)
        {
            if (guideLine == null) return;
            guideLine.enabled = true;
            guideLine.positionCount = 2;
            guideLine.SetPosition(0, new Vector3(x, dropY - currentBallData.radius, 0f));
            guideLine.SetPosition(1, new Vector3(x, -5f, 0f));
        }

        private void SpawnPreview()
        {
            currentBallData = nextBallData;
            nextBallData = GetNextBallData();
            OnNextBallChanged?.Invoke(nextBallData);

            previewBall = Instantiate(ballPrefab, new Vector3(0f, dropY, 0f), Quaternion.identity);

            var controller = previewBall.GetComponent<BallController>();
            if (controller != null)
            {
                controller.Initialize(currentBallData, tierConfig, physicsConfig);
            }

            var rb = previewBall.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            var col = previewBall.GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.enabled = false;
            }
        }

        private void DropBall(float x)
        {
            if (previewBall == null) return;

            var rb = previewBall.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }

            var col = previewBall.GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.enabled = true;
            }

            previewBall.transform.position = new Vector3(x, dropY, 0f);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayDrop();

            if (HapticManager.Instance != null)
                HapticManager.Instance.PlayDrop();

            // Track drop for idle time
            if (MergeTracker.Instance != null)
                MergeTracker.Instance.RecordDrop();

            previewBall = null;

            inCooldown = true;
            cooldownTimer = cooldownDuration;

            if (guideLine != null)
            {
                guideLine.enabled = false;
            }
        }

        private void DestroyPreview()
        {
            if (previewBall != null)
            {
                Destroy(previewBall);
                previewBall = null;
            }
        }
    }
}
