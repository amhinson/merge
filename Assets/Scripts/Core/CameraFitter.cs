using UnityEngine;
using MergeGame.Data;

namespace MergeGame.Core
{
    /// <summary>
    /// Adjusts the camera so the container fills the maximum available screen space
    /// while reserving fixed zones at the top (header/score/next) and bottom (shake button)
    /// that the UI occupies. Works across all phone sizes.
    /// </summary>
    public class CameraFitter : MonoBehaviour
    {
        [SerializeField] private PhysicsConfig physicsConfig;

        [Tooltip("Horizontal padding on each side of the container, in world units")]
        [SerializeField] private float horizontalPadding = 0.3f;

        // UI zones as fractions of screen height (matches the 390x844 canvas layout)
        // Header: back button + score + shake button + next card = ~72pt out of 844
        // Bottom: grid flush to bottom, only safe area margin
        // Drop zone: space above container for the drop ball = ~1.5 world units minimum
        private const float HeaderScreenFraction = 0.14f;  // top 14% reserved for header + next card + drop ball
        private const float BottomScreenFraction = 0.02f;  // minimal bottom margin (safe area only, no shake button)
        private const float MinDropZone = 2.2f;            // minimum world units above container for drop ball

        private Camera cam;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Start()
        {
            cam = Camera.main;
            FitCamera();
        }

        private void Update()
        {
            // Only recalculate if screen size actually changed (orientation flip)
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
                FitCamera();
        }

        private void FitCamera()
        {
            if (cam == null || physicsConfig == null) return;

            float containerWidth = physicsConfig.containerWidth;
            float containerHeight = physicsConfig.containerHeight;
            float containerBottomY = physicsConfig.containerBottomY;
            float containerTopY = containerBottomY + containerHeight;
            float screenAspect = (float)Screen.width / Screen.height;

            // Step 1: Determine ortho size needed to fit container width
            float requiredWidth = containerWidth + horizontalPadding * 2f;
            float orthoSizeForWidth = requiredWidth / (2f * screenAspect);

            // Step 2: Determine how much world-space the UI zones consume
            // at a given ortho size. We use the width-based ortho as starting point.
            float ortho = orthoSizeForWidth;
            float totalWorldHeight = ortho * 2f;

            // World units consumed by UI at top and bottom of screen
            float topReserved = totalWorldHeight * HeaderScreenFraction;
            float bottomReserved = totalWorldHeight * BottomScreenFraction;

            // Ensure minimum drop zone above container
            float dropZone = Mathf.Max(MinDropZone, topReserved);

            // Step 3: Check if container fits vertically with these reservations
            float requiredTop = containerTopY + dropZone;
            float requiredBottom = containerBottomY - bottomReserved;
            float requiredWorldHeight = requiredTop - requiredBottom;
            float orthoSizeForHeight = requiredWorldHeight / 2f;

            // Use whichever is larger (ensures everything fits)
            ortho = Mathf.Max(orthoSizeForWidth, orthoSizeForHeight);

            // Step 4: Recalculate reservations at final ortho size
            totalWorldHeight = ortho * 2f;
            topReserved = totalWorldHeight * HeaderScreenFraction;
            bottomReserved = totalWorldHeight * BottomScreenFraction;
            dropZone = Mathf.Max(MinDropZone, topReserved);

            // Final required bounds
            requiredTop = containerTopY + dropZone;
            requiredBottom = containerBottomY - bottomReserved;

            cam.orthographicSize = ortho;

            // Center camera on the gameplay area (biased slightly toward the container)
            float centerY = (requiredTop + requiredBottom) / 2f;
            cam.transform.position = new Vector3(0f, centerY, cam.transform.position.z);

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
    }
}
