using UnityEngine;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Debug overlay that displays current physics values on screen.
    /// Toggle with backtick/tilde key or a hidden gesture (triple-tap top-left corner).
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        [SerializeField] private PhysicsConfig physicsConfig;

        private bool isVisible;
        private int cornerTapCount;
        private float cornerTapTimer;

        private GUIStyle labelStyle;

        private void Update()
        {
            // Keyboard toggle
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                isVisible = !isVisible;
            }

            // Hidden gesture: triple-tap top-left corner
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 pos = Input.mousePosition;
                if (pos.x < Screen.width * 0.15f && pos.y > Screen.height * 0.85f)
                {
                    cornerTapCount++;
                    cornerTapTimer = 1.5f;

                    if (cornerTapCount >= 3)
                    {
                        isVisible = !isVisible;
                        cornerTapCount = 0;
                    }
                }
            }

            if (cornerTapTimer > 0f)
            {
                cornerTapTimer -= Time.deltaTime;
                if (cornerTapTimer <= 0f)
                    cornerTapCount = 0;
            }
        }

        private void OnGUI()
        {
            if (!isVisible || physicsConfig == null) return;

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 14;
                labelStyle.normal.textColor = Color.green;
                labelStyle.fontStyle = FontStyle.Bold;
            }

            float x = 10;
            float y = 10;
            float lineHeight = 18;

            DrawLabel(ref y, x, lineHeight, "=== PHYSICS CONFIG ===");
            DrawLabel(ref y, x, lineHeight, $"Gravity Scale: {physicsConfig.gravityScale}");
            DrawLabel(ref y, x, lineHeight, $"Base Mass: {physicsConfig.baseMass} (+{physicsConfig.massPerTier}/tier)");
            DrawLabel(ref y, x, lineHeight, $"Base Bounciness: {physicsConfig.baseBounciness} ({physicsConfig.bouncinessPerTier:+0.##;-0.##}/tier)");
            DrawLabel(ref y, x, lineHeight, $"Base Friction: {physicsConfig.baseFriction} (+{physicsConfig.frictionPerTier}/tier)");
            DrawLabel(ref y, x, lineHeight, $"Linear Drag: {physicsConfig.linearDrag}");
            DrawLabel(ref y, x, lineHeight, $"Angular Drag: {physicsConfig.angularDrag}");
            DrawLabel(ref y, x, lineHeight, "--- Drop ---");
            DrawLabel(ref y, x, lineHeight, $"Drop Height: {physicsConfig.dropHeight}");
            DrawLabel(ref y, x, lineHeight, $"Cooldown: {physicsConfig.cooldownDuration}s");
            DrawLabel(ref y, x, lineHeight, "--- Merge ---");
            DrawLabel(ref y, x, lineHeight, $"Pop Force: {physicsConfig.postMergePopForce}");
            DrawLabel(ref y, x, lineHeight, $"Chain Delay: {physicsConfig.chainReactionDelay}s");
            DrawLabel(ref y, x, lineHeight, "--- Container ---");
            DrawLabel(ref y, x, lineHeight, $"Width: {physicsConfig.containerWidth} Height: {physicsConfig.containerHeight}");
            DrawLabel(ref y, x, lineHeight, $"Wall Bounce: {physicsConfig.wallBounciness} Friction: {physicsConfig.wallFriction}");
            DrawLabel(ref y, x, lineHeight, $"Death Line Y: {physicsConfig.deathLineY}");
            DrawLabel(ref y, x, lineHeight, $"Warning Time: {physicsConfig.deathLineWarningDuration}s");
        }

        private void DrawLabel(ref float y, float x, float lineHeight, string text)
        {
            GUI.Label(new Rect(x, y, 400, lineHeight), text, labelStyle);
            y += lineHeight;
        }
    }
}
