using UnityEngine;
using UnityEngine.EventSystems;
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

        [Header("Next Ball Anchor (UI RectTransform to position the next ball under)")]
        [SerializeField] private RectTransform nextBallAnchor;
#pragma warning disable CS0414
        [SerializeField] private float previewScale = 0.6f;
#pragma warning restore CS0414

        [Header("Guide Line")]
        [SerializeField] private LineRenderer guideLine;

        public GameObject BallPrefab => ballPrefab;

        private GameObject previewBall;
        private GameObject nextBallObj;
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
                if (physicsConfig != null)
                {
                    dropY = physicsConfig.dropHeight;
                    cooldownDuration = physicsConfig.cooldownDuration;
                    leftWallX = -physicsConfig.containerWidth / 2f;
                    rightWallX = physicsConfig.containerWidth / 2f;
                }

                inCooldown = true;
                cooldownTimer = cooldownDuration;

                currentBallData = GetNextBallData();
                nextBallData = GetNextBallData();
                OnNextBallChanged?.Invoke(nextBallData);

                SpawnNextBallPreview();
            }
            else
            {
                DestroyPreview();
                DestroyNextPreview();
            }

            if (guideLine != null)
                guideLine.enabled = active;
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

#if UNITY_EDITOR
            // Debug: number keys 1-9 = tier 1-9, 0 = tier 10, minus = tier 11
            CheckDebugTierOverride();
#endif

            // Keep next ball positioned under the UI anchor
            UpdateNextBallPosition();

            if (inCooldown)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    inCooldown = false;
                    PromoteNextBall();
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

            if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                DropBall(clampedX);
            }
        }

        private void UpdateNextBallPosition()
        {
            if (nextBallObj == null || mainCamera == null) return;

            Vector3 worldPos;
            if (nextBallAnchor != null)
            {
                // Convert the UI anchor's center position to world space — no offset
                Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, nextBallAnchor.position);
                worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                worldPos.z = 0f;
            }
            else
            {
                worldPos = new Vector3(2f, 5f, 0f);
            }

            nextBallObj.transform.position = worldPos;
        }

        private void UpdateGuideLine(float x)
        {
            if (guideLine == null) return;
            guideLine.enabled = true;
            guideLine.positionCount = 2;
            guideLine.SetPosition(0, new Vector3(x, dropY - currentBallData.radius, 0f));
            guideLine.SetPosition(1, new Vector3(x, -5f, 0f));
        }

        private void PromoteNextBall()
        {
            if (nextBallObj != null)
            {
                previewBall = nextBallObj;
                nextBallObj = null;
            }
            else
            {
                previewBall = Instantiate(ballPrefab, new Vector3(0f, dropY, 0f), Quaternion.identity);
            }

            // Advance ball data
            currentBallData = nextBallData;
            nextBallData = GetNextBallData();
            OnNextBallChanged?.Invoke(nextBallData);

            // Initialize as the active drop ball
            var controller = previewBall.GetComponent<BallController>();
            if (controller != null)
                controller.Initialize(currentBallData, tierConfig, physicsConfig);

            previewBall.transform.position = new Vector3(0f, dropY, 0f);
            previewBall.transform.localScale = Vector3.one; // ensure full size

            // Re-enable renderer (was hidden during preview)
            var sr = previewBall.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            var rb = previewBall.GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

            var col = previewBall.GetComponent<CircleCollider2D>();
            if (col != null) col.enabled = false;

            // Spawn new next ball
            SpawnNextBallPreview();
        }

        private void SpawnNextBallPreview()
        {
            DestroyNextPreview();

            // Spawn at origin temporarily — UpdateNextBallPosition will place it
            nextBallObj = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);

            var controller = nextBallObj.GetComponent<BallController>();
            if (controller != null)
                controller.Initialize(nextBallData, tierConfig, physicsConfig);

            // Hide the world-space preview — UI Image in the NEXT card shows the ball instead
            nextBallObj.transform.localScale = Vector3.one; // keep at actual size for when it drops

            var rb = nextBallObj.GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;

            var col = nextBallObj.GetComponent<CircleCollider2D>();
            if (col != null) col.enabled = false;

            // Hide renderer — ball becomes visible when promoted to active dropper
            var sr = nextBallObj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            // Position off-screen
            nextBallObj.transform.position = new Vector3(100f, 100f, 0f);
        }

        private void DropBall(float x)
        {
            if (previewBall == null) return;

            var rb = previewBall.GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;

            var col = previewBall.GetComponent<CircleCollider2D>();
            if (col != null) col.enabled = true;

            previewBall.transform.position = new Vector3(x, dropY, 0f);

            if (AudioManager.Instance != null) AudioManager.Instance.PlayDrop();
            if (HapticManager.Instance != null) HapticManager.Instance.PlayDrop();
            if (MergeTracker.Instance != null) MergeTracker.Instance.RecordDrop();

            previewBall = null;
            inCooldown = true;
            cooldownTimer = cooldownDuration;

            if (guideLine != null) guideLine.enabled = false;
        }

        private void DestroyPreview()
        {
            if (previewBall != null) { Destroy(previewBall); previewBall = null; }
        }

        private void DestroyNextPreview()
        {
            if (nextBallObj != null) { Destroy(nextBallObj); nextBallObj = null; }
        }

#if UNITY_EDITOR
        private void CheckDebugTierOverride()
        {
            int tier = -1;
            if (Input.GetKeyDown(KeyCode.Alpha1)) tier = 0;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) tier = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) tier = 2;
            else if (Input.GetKeyDown(KeyCode.Alpha4)) tier = 3;
            else if (Input.GetKeyDown(KeyCode.Alpha5)) tier = 4;
            else if (Input.GetKeyDown(KeyCode.Alpha6)) tier = 5;
            else if (Input.GetKeyDown(KeyCode.Alpha7)) tier = 6;
            else if (Input.GetKeyDown(KeyCode.Alpha8)) tier = 7;
            else if (Input.GetKeyDown(KeyCode.Alpha9)) tier = 8;
            else if (Input.GetKeyDown(KeyCode.Alpha0)) tier = 9;
            else if (Input.GetKeyDown(KeyCode.Minus)) tier = 10;

            if (tier < 0 || tierConfig == null) return;

            var data = tierConfig.GetTier(tier);
            if (data == null) return;

            // Override the current drop ball
            currentBallData = data;
            if (previewBall != null)
            {
                var controller = previewBall.GetComponent<BallController>();
                if (controller != null)
                    controller.Initialize(data, tierConfig, physicsConfig);
            }

            Debug.Log($"Debug: next drop overridden to Tier {tier + 1}");
        }
#endif
    }
}

