using UnityEngine;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Spawns floating "x3" text at merge world positions during combos (chain >= 2).
    /// </summary>
    public class FloatingComboText : MonoBehaviour
    {
        public static FloatingComboText Instance { get; private set; }

        private static TMP_FontAsset cachedFont;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("FloatingComboText");
            go.AddComponent<FloatingComboText>();
        }

        private bool subscribed;

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void Subscribe()
        {
            if (subscribed) return;
            if (MergeTracker.Instance == null) return;
            MergeTracker.Instance.OnMerge += OnMerge;
            subscribed = true;
        }

        private void OnDisable()
        {
            if (MergeTracker.Instance != null)
                MergeTracker.Instance.OnMerge -= OnMerge;
            subscribed = false;
        }

        private void OnMerge(int tier, int chainLength, Vector3 worldPos)
        {
            if (chainLength < 2) return;
            SpawnFloater(chainLength, worldPos);
        }

        private void SpawnFloater(int chainLength, Vector3 worldPos)
        {
            if (cachedFont == null)
                cachedFont = Resources.Load<TMP_FontAsset>("Fonts/DMMono-Medium SDF");

            var go = new GameObject("FloatingCombo");
            go.transform.position = worldPos + Vector3.up * 0.3f;
            go.transform.localScale = Vector3.one * 0.04f;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = $"x{chainLength}";
            tmp.font = cachedFont;
            tmp.fontSize = 48;
            tmp.color = OC.cyan;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 100;

            StartCoroutine(FloatCoroutine(go, tmp));
        }

        private static IEnumerator FloatCoroutine(GameObject go, TextMeshPro tmp)
        {
            Vector3 startPos = go.transform.position;
            Vector3 startScale = go.transform.localScale;
            Color startColor = tmp.color;

            const float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (go == null) yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                go.transform.position = startPos + Vector3.up * (t * 0.8f);

                float scaleT = Mathf.Min(t * 4f, 1f);
                float eased = 1f - (1f - scaleT) * (1f - scaleT);
                go.transform.localScale = startScale * eased;

                float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
                tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

                yield return null;
            }

            if (go != null) Destroy(go);
        }
    }
}
