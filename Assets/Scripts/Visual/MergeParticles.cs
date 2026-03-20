using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MergeGame.Visual
{
    /// <summary>
    /// Spawns pixel-style particle bursts on merge.
    /// Small colored pixel squares scatter outward and fade.
    /// </summary>
    public class MergeParticles : MonoBehaviour
    {
        public static MergeParticles Instance { get; private set; }

        [SerializeField] private int particleCount = 8;
        [SerializeField] private float particleSpeed = 4f;
        [SerializeField] private float particleLifetime = 0.4f;
        [SerializeField] private float particleSize = 0.08f;

        private static Sprite pixelSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (pixelSprite == null)
                pixelSprite = CreatePixelSprite();
        }

        public void SpawnBurst(Vector3 position, Color color, int tier)
        {
            int count = particleCount + tier; // More particles for higher tiers
            StartCoroutine(BurstCoroutine(position, color, count));
        }

        private IEnumerator BurstCoroutine(Vector3 position, Color color, int count)
        {
            var particles = new List<(GameObject go, Vector2 velocity, SpriteRenderer sr)>();

            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i + Random.Range(-15f, 15f);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                float speed = particleSpeed * Random.Range(0.6f, 1.2f);

                GameObject p = new GameObject("Particle");
                p.transform.position = position;
                p.transform.localScale = Vector3.one * particleSize;

                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = pixelSprite;
                sr.color = color;
                sr.sortingOrder = 10;

                particles.Add((p, dir * speed, sr));
            }

            float elapsed = 0f;
            while (elapsed < particleLifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / particleLifetime;

                foreach (var (go, velocity, sr) in particles)
                {
                    if (go == null) continue;
                    go.transform.position += (Vector3)(velocity * Time.deltaTime);
                    Color c = sr.color;
                    c.a = 1f - t;
                    sr.color = c;
                }

                yield return null;
            }

            foreach (var (go, _, _) in particles)
            {
                if (go != null) Destroy(go);
            }
        }

        private static Sprite CreatePixelSprite()
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] colors = new Color[16];
            for (int i = 0; i < 16; i++) colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
