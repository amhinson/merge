using UnityEngine;
using MergeGame.Data;

namespace MergeGame.Core
{
    public class ContainerSetup : MonoBehaviour
    {
        [Header("Container Dimensions (overridden by PhysicsConfig if set)")]
        [SerializeField] private float width = 5f;
        [SerializeField] private float height = 8f;
        [SerializeField] private float wallThickness = 0.12f;
        [SerializeField] private float bottomThickness = 0.15f;
        [SerializeField] private float bottomY = -4.5f;

        [Header("Physics Config (optional)")]
        [SerializeField] private PhysicsConfig physicsConfig;

        [Header("Visual")]
        [SerializeField] private Color wallColor = new Color(0.22f, 0.20f, 0.25f, 1f);

        public float LeftX => -width / 2f;
        public float RightX => width / 2f;
        public float BottomY => bottomY;
        public float TopY => bottomY + height;

        private void Awake()
        {
            if (physicsConfig != null)
            {
                width = physicsConfig.containerWidth;
                height = physicsConfig.containerHeight;
                bottomY = physicsConfig.containerBottomY;
            }

            CreateWalls();
        }

        private void CreateWalls()
        {
            float halfWidth = width / 2f;
            float wallBounce = physicsConfig != null ? physicsConfig.wallBounciness : 0.1f;
            float wallFriction = physicsConfig != null ? physicsConfig.wallFriction : 0.5f;

            var wallMat = new PhysicsMaterial2D("WallMaterial");
            wallMat.bounciness = wallBounce;
            wallMat.friction = wallFriction;

            // Bottom wall
            CreateWall("BottomWall",
                new Vector3(0f, bottomY, 0f),
                new Vector2(width + wallThickness * 2f, bottomThickness),
                wallMat);

            // Left wall
            CreateWall("LeftWall",
                new Vector3(-halfWidth - wallThickness / 2f, bottomY + height / 2f, 0f),
                new Vector2(wallThickness, height),
                wallMat);

            // Right wall
            CreateWall("RightWall",
                new Vector3(halfWidth + wallThickness / 2f, bottomY + height / 2f, 0f),
                new Vector2(wallThickness, height),
                wallMat);
        }

        private void CreateWall(string wallName, Vector3 position, Vector2 size, PhysicsMaterial2D mat)
        {
            GameObject wall = new GameObject(wallName);
            wall.transform.parent = transform;
            wall.transform.localPosition = position;

            var boxCollider = wall.AddComponent<BoxCollider2D>();
            boxCollider.size = size;
            boxCollider.sharedMaterial = mat;

            var sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePixelRect();
            sr.color = wallColor;
            sr.sortingOrder = -1;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        private Sprite CreatePixelRect()
        {
            Texture2D tex = new Texture2D(4, 4);
            tex.filterMode = FilterMode.Point;
            Color[] colors = new Color[16];
            for (int i = 0; i < 16; i++) colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
