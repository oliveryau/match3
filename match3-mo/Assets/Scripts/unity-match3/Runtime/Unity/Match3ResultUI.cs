using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    public class Match3ResultUI : MonoBehaviour
    {
        public static Match3ResultUI Instance { get; private set; }

        [SerializeField] GameObject resultRoot;
        [SerializeField] TMP_Text resultText;
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

            if (resultText != null)
                resultText.text = Match3StarVisuals.ResultText(earnedStars);

            Match3StarVisuals.Apply(star1, star2, star3, earnedStars);

            if (resultRoot != null)
                resultRoot.SetActive(true);
        }

        public void Hide()
        {
            _shown = false;
            if (resultRoot != null)
                resultRoot.SetActive(false);
        }

        void OnExitPressed()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("Match3ResultUI: GameManager missing.");
                return;
            }

            GameManager.Instance.ClearPendingMatch3Level();
            GameManager.Instance.LoadHome();
        }
    }
}
