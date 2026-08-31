using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    /// <summary>
    /// Landing screen: name entry (max 14), Chinese name randomiser, enter to Home.
    /// </summary>
    public class LandingUIController : MonoBehaviour
    {
        const int MaxNameLength = 14;
        const int CjkStart = 0x4E00;
        const int CjkEndExclusive = 0x9FA6;

        [SerializeField] TMP_InputField nameField;
        [SerializeField] Button enterButton;
        [SerializeField] Button randomiserButton;

        void Awake()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayNamedBgm(AudioManager.Bgm1Name);

            if (nameField != null)
            {
                nameField.characterLimit = MaxNameLength;
                nameField.lineType = TMP_InputField.LineType.SingleLine;
                if (string.IsNullOrWhiteSpace(nameField.text) || nameField.text == "NewTTexTTTTTTT")
                    nameField.text = string.Empty;

                if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.PlayerName))
                    nameField.text = GameManager.Instance.PlayerName;
            }

            if (enterButton != null)
            {
                enterButton.onClick.RemoveListener(OnEnterPressed);
                enterButton.onClick.AddListener(OnEnterPressed);
            }

            if (randomiserButton != null)
            {
                randomiserButton.onClick.RemoveListener(OnRandomisePressed);
                randomiserButton.onClick.AddListener(OnRandomisePressed);
            }
        }

        void OnDestroy()
        {
            if (enterButton != null)
                enterButton.onClick.RemoveListener(OnEnterPressed);
            if (randomiserButton != null)
                randomiserButton.onClick.RemoveListener(OnRandomisePressed);
        }

        public void OnEnterPressed()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUiClick();

            string playerName = nameField != null ? nameField.text.Trim() : string.Empty;
            if (playerName.Length > MaxNameLength)
                playerName = playerName.Substring(0, MaxNameLength);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerName(playerName);
                GameManager.Instance.LoadHome();
            }
        }

        public void OnRandomisePressed()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUiClick();

            if (nameField == null)
                return;

            int length = Random.Range(2, 4);
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append((char)Random.Range(CjkStart, CjkEndExclusive));

            nameField.text = sb.ToString();
        }
    }
}
