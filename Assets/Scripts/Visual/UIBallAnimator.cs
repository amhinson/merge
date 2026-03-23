using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.Visual
{
    /// <summary>
    /// Animates a waveform ball rendered as a UI Image.
    /// Regenerates the sprite every frame with a scrolling phase offset.
    /// </summary>
    public class UIBallAnimator : MonoBehaviour
    {
        private Image image;
        private int tier;
        private float uiRadius;
        private float scrollOffset;
        private float scrollSpeed;

        public void Initialize(int ballTier, float radius)
        {
            image = GetComponent<Image>();
            tier = ballTier;
            uiRadius = radius;
            scrollOffset = Random.Range(0f, 1f);
            scrollSpeed = tier >= 0 && tier < NeonBallRenderer.ScrollSpeeds.Length
                ? NeonBallRenderer.ScrollSpeeds[tier]
                : 12f;
        }

        private void Update()
        {
            if (image == null) return;

            scrollOffset += Time.deltaTime / scrollSpeed;
            scrollOffset %= 1.0f;

            var png = NeonBallRenderer.GenerateBallPNG(tier, Color.white, uiRadius, scrollOffset);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.LoadImage(png);

            // Destroy old sprite texture to avoid memory leak
            if (image.sprite != null && image.sprite.texture != null)
                Destroy(image.sprite.texture);
            if (image.sprite != null)
                Destroy(image.sprite);

            image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
        }

        private void OnDestroy()
        {
            // Clean up final sprite
            if (image != null && image.sprite != null)
            {
                if (image.sprite.texture != null)
                    Destroy(image.sprite.texture);
                Destroy(image.sprite);
            }
        }
    }
}
