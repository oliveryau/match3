using TMPro;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// Home scene character bar: player name + total stars collected.
    /// Visible only when a player name has been set (logged in from Landing).
    /// </summary>
    public class CharacterBarUI : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text starText;

        void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            string playerName = GameManager.Instance != null
                ? GameManager.Instance.PlayerName
                : string.Empty;

            bool loggedIn = !string.IsNullOrWhiteSpace(playerName);
            if (!loggedIn)
            {
                gameObject.SetActive(false);
                return;
            }

            if (nameText != null)
                nameText.text = playerName.Trim();

            if (starText != null)
                starText.text = PlayerProgress.GetTotalStars().ToString();
        }
    }
}
