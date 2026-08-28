using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    /// <summary>Match3 HUD back control — returns to Home at the stored street segment.</summary>
    public class Match3BackButton : MonoBehaviour
    {
        [SerializeField] Button backButton;

        void Awake()
        {
            if (backButton == null)
                backButton = GetComponent<Button>();
            if (backButton == null)
                return;
            backButton.onClick.RemoveListener(OnBackPressed);
            backButton.onClick.AddListener(OnBackPressed);
        }

        void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackPressed);
        }

        void OnBackPressed()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("Match3BackButton: GameManager missing.");
                return;
            }

            GameManager.Instance.ReturnHomeFromMatch3(earlyExit: true);
        }
    }
}
