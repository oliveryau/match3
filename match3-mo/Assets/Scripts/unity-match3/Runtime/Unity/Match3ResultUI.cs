using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    public class Match3ResultUI : MonoBehaviour
    {
        public static Match3ResultUI Instance { get; private set; }

        [SerializeField] GameObject resultRoot;
        [SerializeField] Image resultImage;
        [SerializeField] Sprite oneStarTextSprite;
        [SerializeField] Sprite twoStarTextSprite;
        [SerializeField] Sprite threeStarTextSprite;
        [SerializeField] Image star1;
        [SerializeField] Image star2;
        [SerializeField] Image star3;
        [SerializeField] Button exitButton;

        bool _shown;

        public bool IsShowing => _shown;

        void Awake()
        {
            Instance = this;

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitPressed);
                exitButton.onClick.AddListener(OnExitPressed);
            }

            Hide();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Show(int earnedStars)
        {
            if (_shown)
                return;

            _shown = true;
            earnedStars = Mathf.Clamp(earnedStars, 0, 3);

            if (GameManager.Instance != null && GameManager.Instance.HasPendingMatch3Level)
                PlayerProgress.RecordStars(GameManager.Instance.ActiveMatch3LevelKey, earnedStars);

            ApplyResultImage(earnedStars);
            Match3StarVisuals.Apply(star1, star2, star3, earnedStars);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayResultStars(earnedStars);

            if (resultRoot != null)
                resultRoot.SetActive(true);
        }

        void ApplyResultImage(int earnedStars)
        {
            if (resultImage == null)
                return;

            Sprite sprite = null;
            switch (earnedStars)
            {
                case 1: sprite = oneStarTextSprite; break;
                case 2: sprite = twoStarTextSprite; break;
                case 3: sprite = threeStarTextSprite; break;
            }

            resultImage.sprite = sprite;
            resultImage.enabled = sprite != null;
        }

        public void Hide()
        {
            _shown = false;
            if (resultRoot != null)
                resultRoot.SetActive(false);
        }

        void OnExitPressed()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUiClick();

            if (GameManager.Instance == null)
            {
                Debug.LogWarning("Match3ResultUI: GameManager missing.");
                return;
            }

            GameManager.Instance.ReturnHomeFromMatch3();
        }
    }
}
