using UnityEngine;
using MergeGame.Data;

namespace MergeGame.Core
{
    /// <summary>
    /// Adjusts the camera's orthographic size so the container always fills
    /// the screen width with consistent padding. The container world-space
    /// dimensions are identical on every device — only the camera zoom changes.
    /// </summary>
    public class CameraFitter : MonoBehaviour
    {
        [SerializeField] private PhysicsConfig physicsConfig;

        [Tooltip("Horizontal padding on each side of the container, in world units")]
        [SerializeField] private float horizontalPadding = 0.3f;

        [Tooltip("Vertical padding above the container for HUD/drop zone")]
        [SerializeField] private float topPadding = 2.5f;

        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
            FitCamera();
        }

        private void Update()
        {
            // Re-fit if aspect ratio changes (e.g. rotation, though we lock portrait)
            FitCamera();
        }

        private void FitCamera()
        {
            if (cam == null || physicsConfig == null) return;

            float containerWidth = physicsConfig.containerWidth;
            float containerHeight = physicsConfig.containerHeight;
            float containerBottomY = physicsConfig.containerBottomY;

            // Total world width we need to show
            float requiredWidth = containerWidth + horizontalPadding * 2f;

            // Camera ortho size needed to fit this width
            float screenAspect = (float)Screen.width / Screen.height;
            float orthoSizeForWidth = requiredWidth / (2f * screenAspect);

            // Also check vertical: we need to show container + top padding for HUD/drop zone
            float containerTop = containerBottomY + containerHeight;
            float requiredTop = containerTop + topPadding;
            float requiredBottom = containerBottomY - 1.2f; // Room for shake button below grid
            float requiredHeight = requiredTop - requiredBottom;
            float orthoSizeForHeight = requiredHeight / 2f;

            // Use whichever is larger (ensures everything fits)
            float orthoSize = Mathf.Max(orthoSizeForWidth, orthoSizeForHeight);
            cam.orthographicSize = orthoSize;

            // Center the camera vertically on the gameplay area
            float centerY = (requiredTop + requiredBottom) / 2f;
            cam.transform.position = new Vector3(0f, centerY, cam.transform.position.z);
        }
    }
}
