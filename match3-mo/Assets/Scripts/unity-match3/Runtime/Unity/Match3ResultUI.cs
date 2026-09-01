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
        int _earnedStars;
        bool _streetAreaClearPending;

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
            _earnedStars = earnedStars;
            _streetAreaClearPending = false;

            if (GameManager.Instance != null && GameManager.Instance.HasPendingMatch3Level)
            {
                var key = GameManager.Instance.ActiveMatch3LevelKey;
                var streetId = GameManager.Instance.ActiveStreetVideoId;
                bool wasStreetComplete = IsStreetVideo(streetId) && IsStreetFullyStarred(streetId);

                PlayerProgress.RecordStars(key, earnedStars);
                PlayerProgress.MarkLevelPlayed(key);

                if (IsStreetVideo(streetId) && earnedStars >= 3)
                    _streetAreaClearPending = !wasStreetComplete && IsStreetFullyStarred(streetId);
            }

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
            _earnedStars = 0;
            _streetAreaClearPending = false;
            if (resultRoot != null)
                resultRoot.SetActive(false);
        }

        static bool IsStreetVideo(HomeVideoId id) =>
            id == HomeVideoId.NormalStreet || id == HomeVideoId.VacationStreet;

        static bool IsStreetFullyStarred(HomeVideoId id) =>
            PlayerProgress.GetStars(id, StreetMatch3Slot.Left) >= 3
            && PlayerProgress.GetStars(id, StreetMatch3Slot.Right) >= 3;

        void OnExitPressed()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUiClick();

            if (GameManager.Instance == null)
            {
                Debug.LogWarning("Match3ResultUI: GameManager missing.");
                return;
            }

            if (_streetAreaClearPending)
            {
                GameManager.Instance.QueueStreetAreaClear(GameManager.Instance.ActiveStreetVideoId);
                _streetAreaClearPending = false;
            }

            GameManager.Instance.ReturnHomeFromMatch3(match3EarnedStars: _earnedStars);
        }
    }
}
