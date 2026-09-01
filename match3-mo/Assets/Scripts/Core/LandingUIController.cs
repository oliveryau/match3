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

        // Common simple simplified characters (surnames + everyday given-name chars).
        static readonly char[] SimpleNameChars =
        {
            '王', '李', '张', '刘', '陈', '杨', '黄', '赵', '周', '吴', '徐', '孙', '马', '朱', '胡',
            '林', '郭', '何', '高', '罗', '郑', '梁', '谢', '宋', '唐', '许', '韩', '冯', '邓', '曹',
            '彭', '曾', '田', '叶', '程', '苏', '魏', '丁', '任', '沈', '卢', '姜', '崔', '陆', '汪',
            '范', '石', '廖', '贾', '夏', '韦', '方', '白', '邹', '孟', '熊', '秦', '江', '尹', '薛',
            '段', '雷', '侯', '龙', '史', '陶', '黎', '贺', '顾', '毛', '郝', '龚', '邵', '万', '钱',
            '严', '武', '戴', '莫', '孔', '向', '汤', '云', '天', '明', '月', '星', '山', '水', '火',
            '木', '田', '心', '文', '玉', '兰', '竹', '梅', '芳', '芬', '香', '花', '草', '春', '夏',
            '秋', '冬', '东', '南', '西', '北', '安', '宁', '乐', '和', '平', '美', '丽', '欣', '怡',
            '悦', '欢', '歌', '诗', '书', '画', '音', '金', '凤', '鹤', '鹿', '兔', '虎', '牛', '羊',
            '鸡', '犬', '猫', '江', '海', '河', '湖', '林', '森', '城', '家', '村', '友', '爱', '梦',
            '希', '福', '禄', '寿', '喜', '财', '昌', '勇', '健', '康', '泰', '祥', '吉', '庆', '华',
            '强', '伟', '杰', '俊', '秀', '雅', '洁', '婷', '娜', '玲', '艳', '红', '青', '蓝', '银',
            '珠', '宝', '涵', '浩', '轩', '宇', '诺', '航', '雨', '泽', '博', '昊', '辉', '佳', '语',
            '桐', '妍', '梓', '一', '子', '小', '大', '人', '儿', '女', '男', '民', '生', '光', '亮'
        };

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
                GameManager.Instance.LoadHomeFromLanding();
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
                sb.Append(SimpleNameChars[Random.Range(0, SimpleNameChars.Length)]);

            nameField.text = sb.ToString();
        }
    }
}
