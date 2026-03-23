using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Game screen HUD overlay per the spec: back button, score block, next ball card.
    /// Also handles the practice mode banner with auto-fade.
    /// Attach to the game screen's CanvasGroup panel.
    /// </summary>
    public class GameScreenHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BallTierConfig tierConfig;

        // Built UI
        private TextMeshProUGUI scoreLabel;
        private TextMeshProUGUI scoreValue;
        private Image nextBallImage;
        private TextMeshProUGUI nextLabel;
        private GameObject practiceBanner;
        private CanvasGroup practiceBannerCG;

        private bool isBuilt;
        private int displayedScore;
        private int targetScore;
        private Coroutine scoreTweenCoroutine;

        private void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }

            // Show practice banner if in practice mode
            if (practiceBanner != null)
            {
                bool show = GameSession.IsPractice;
                practiceBanner.SetActive(show);
                if (show) StartCoroutine(FadeBanner());
            }

            // Subscribe to score changes
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
            if (DropController.Instance != null)
                DropController.Instance.OnNextBallChanged += OnNextBallChanged;
        }

        private void OnDisable()
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
            if (DropController.Instance != null)
                DropController.Instance.OnNextBallChanged -= OnNextBallChanged;
        }

        private void BuildUI()
        {
            // Header bar (anchored top, full width)
            var header = OvertoneUI.CreateUIObject("HeaderBar", transform);
            var headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1);
            headerRT.anchoredPosition = Vector2.zero;
            headerRT.sizeDelta = new Vector2(0, OS.safeAreaTop + 60);

            var hlg = header.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.padding = new RectOffset(16, 16, (int)(OS.safeAreaTop + 16), 8);

            // Back button
            var (backGO, backBtn) = OvertoneUI.CreateBackButton(header.transform, "BackButton");
            backBtn.onClick.AddListener(OnBackClicked);

            // Score block (flex=1)
            var scoreBlock = OvertoneUI.CreateUIObject("ScoreBlock", header.transform);
            var scoreVLG = scoreBlock.AddComponent<VerticalLayoutGroup>();
            scoreVLG.childAlignment = TextAnchor.UpperLeft;
            scoreVLG.spacing = 2;
            scoreVLG.childControlWidth = true;
            scoreVLG.childControlHeight = false;
            scoreVLG.childForceExpandWidth = true;
            scoreBlock.AddComponent<LayoutElement>().flexibleWidth = 1;

            scoreLabel = OvertoneUI.CreateLabel(scoreBlock.transform, "SCORE",
                OvertoneUI.PressStart2P, OFont.labelXs, OC.muted, "ScoreLabel");
            scoreLabel.characterSpacing = 2;
            scoreLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 12;

            scoreValue = OvertoneUI.CreateLabel(scoreBlock.transform, "0",
                OvertoneUI.DMMono, OFont.scoreGame, OC.cyan, "ScoreValue");
            scoreValue.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;

            // Next ball card
            var nextCard = OvertoneUI.CreateCard(header.transform, "NextBallCard");
            var nextCardVLG = nextCard.AddComponent<VerticalLayoutGroup>();
            nextCardVLG.childAlignment = TextAnchor.MiddleCenter;
            nextCardVLG.spacing = 3;
            nextCardVLG.childControlWidth = true;
            nextCardVLG.childControlHeight = false;
            nextCardVLG.childForceExpandWidth = true;
            nextCardVLG.padding = new RectOffset(8, 8, 5, 5);
            var nextCardLE = nextCard.GetComponent<LayoutElement>();
            if (nextCardLE == null) nextCardLE = nextCard.AddComponent<LayoutElement>();
            nextCardLE.preferredWidth = 52;
            nextCardLE.flexibleWidth = 0;

            nextLabel = OvertoneUI.CreateLabel(nextCard.transform, "NEXT",
                OvertoneUI.PressStart2P, OFont.labelXxs, OC.dim, "NextLabel");
            nextLabel.characterSpacing = 1;
            nextLabel.alignment = TextAlignmentOptions.Center;
            nextLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 10;

            var nextBallGO = OvertoneUI.CreateUIObject("NextBallDisplay", nextCard.transform);
            nextBallImage = nextBallGO.AddComponent<Image>();
            nextBallImage.preserveAspect = true;
            nextBallGO.AddComponent<LayoutElement>().preferredHeight = 28;

            // Practice banner (centered in arena area)
            BuildPracticeBanner();
        }

        private void BuildPracticeBanner()
        {
            practiceBanner = OvertoneUI.CreateUIObject("PracticeBanner", transform);
            var bannerRT = practiceBanner.GetComponent<RectTransform>();
            bannerRT.anchorMin = new Vector2(0.5f, 1);
            bannerRT.anchorMax = new Vector2(0.5f, 1);
            bannerRT.pivot = new Vector2(0.5f, 1);
            bannerRT.anchoredPosition = new Vector2(0, -(OS.safeAreaTop + 70));
            bannerRT.sizeDelta = new Vector2(300, 30);

            var img = practiceBanner.AddComponent<Image>();
            img.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            img.type = Image.Type.Sliced;
            img.color = OC.A(OC.amber, 0.18f);
            img.raycastTarget = false;

            var tmp = OvertoneUI.CreateLabel(practiceBanner.transform,
                "only first score of the day is counted",
                OvertoneUI.PressStart2P, OFont.labelXs, OC.amber, "BannerText");
            tmp.characterSpacing = 1;
            tmp.alignment = TextAlignmentOptions.Center;
            OvertoneUI.StretchFill(tmp.GetComponent<RectTransform>());

            practiceBannerCG = practiceBanner.AddComponent<CanvasGroup>();
            practiceBanner.SetActive(false);
        }

        private IEnumerator FadeBanner()
        {
            if (practiceBannerCG == null) yield break;
            practiceBannerCG.alpha = 1f;

            // Hold visible
            yield return new WaitForSeconds(2.2f);

            // Fade out
            float elapsed = 0f;
            float duration = 1.8f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                practiceBannerCG.alpha = 1f - t * t; // ease in quad
                yield return null;
            }

            practiceBanner.SetActive(false);
        }

        // ───── Event handlers ─────

        private void OnScoreChanged(int score)
        {
            targetScore = score;
            if (scoreTweenCoroutine != null) StopCoroutine(scoreTweenCoroutine);
            scoreTweenCoroutine = StartCoroutine(TweenScore());
        }

        private IEnumerator TweenScore()
        {
            int start = displayedScore;
            int end = targetScore;
            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out quad
                float eased = 1f - (1f - t) * (1f - t);
                displayedScore = Mathf.RoundToInt(Mathf.Lerp(start, end, eased));
                if (scoreValue != null)
                    scoreValue.text = displayedScore.ToString("N0");
                yield return null;
            }

            displayedScore = end;
            if (scoreValue != null)
                scoreValue.text = displayedScore.ToString("N0");
            scoreTweenCoroutine = null;
        }

        private void OnNextBallChanged(BallData data)
        {
            if (nextBallImage == null || data == null) return;
            if (data.sprite != null)
            {
                nextBallImage.sprite = data.sprite;
                nextBallImage.color = Color.white;
            }
            else
            {
                nextBallImage.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
                nextBallImage.color = data.color;
            }
        }

        private void OnBackClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Menu);
        }
    }
}
