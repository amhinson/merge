using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.Visual
{
    /// <summary>
    /// Animates a waveform ball in UI space by periodically updating the sprite
    /// with a new phase. Smooth scrolling wave that matches the in-game look.
    /// </summary>
    public class UIBallAnimator : MonoBehaviour
    {
        private Image image;
        private int tier;
        private float radius;
        private float phase;
        private float speed;
        private float lastUpdateTime;

        private const float UpdateInterval = 0.1f; // regenerate sprite every 100ms
        private const float CycleSeconds = 30f;

        public void Initialize(int ballTier, float uiRadius)
        {
            image = GetComponent<Image>();
            if (image == null) return;

            tier = ballTier;
            // Generate at higher resolution for crisp UI rendering
            radius = Mathf.Max(uiRadius, 0.7f);
            phase = Random.Range(0f, 1f);
            speed = 1f / CycleSeconds;

            UpdateSprite();
        }

        private void Update()
        {
            if (image == null) return;

            phase += Time.deltaTime * speed;
            if (phase >= 1f) phase -= 1f;

            // Only regenerate sprite periodically (not every frame)
            if (Time.time - lastUpdateTime >= UpdateInterval)
            {
                lastUpdateTime = Time.time;
                UpdateSprite();
            }
        }

        private void UpdateSprite()
        {
            var color = BallRenderer.GetBallColor(tier);
            var pixels = BallRenderer.GenerateBallPixels(tier, color, radius, phase, out int texSize);

            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(pixels);
            tex.Apply();

            // Destroy old sprite texture to avoid memory leak
            if (image.sprite != null && image.sprite.texture != null)
            {
                var oldTex = image.sprite.texture;
                image.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                    new Vector2(0.5f, 0.5f), texSize);
                Destroy(oldTex);
            }
            else
            {
                image.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                    new Vector2(0.5f, 0.5f), texSize);
            }
        }

        private void OnDestroy()
        {
            // Clean up texture
            if (image != null && image.sprite != null && image.sprite.texture != null)
                Destroy(image.sprite.texture);
        }
    }
}
