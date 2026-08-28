using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match3
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] HomeVideoCatalog homeVideoCatalog;
        [SerializeField] Sprite starSprite;
        [SerializeField] Sprite starEmptySprite;

        public string PlayerName { get; private set; } = string.Empty;
        public HomeVideoId ActiveStreetVideoId { get; private set; }
        public StreetMatch3Slot ActiveStreetSlot { get; private set; }
        public bool HasPendingMatch3Level { get; private set; }
        public bool HasPendingHomeResume { get; private set; }
        public HomeVideoId PendingHomeVideoId { get; private set; }

        public Match3LevelConfig ActiveMatch3Level
        {
            get
            {
                if (!HasPendingMatch3Level || homeVideoCatalog == null)
                    return null;
                return homeVideoCatalog.GetLevel(ActiveStreetVideoId, ActiveStreetSlot);
            }
        }

        public string ActiveMatch3LevelKey =>
            HasPendingMatch3Level
                ? PlayerProgress.LevelKey(ActiveStreetVideoId, ActiveStreetSlot)
                : string.Empty;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Match3StarVisuals.SetSprites(starSprite, starEmptySprite);
            PlayerName = PlayerPrefs.GetString(PlayerProgress.PlayerNamePrefsKey, string.Empty);
            PlayerProgress.LoadForPlayer(PlayerName);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            if (ShouldLoadLanding())
                LoadLanding();
        }

        public void SetPlayerName(string playerName)
        {
            PlayerName = playerName ?? string.Empty;
            PlayerPrefs.SetString(PlayerProgress.PlayerNamePrefsKey, PlayerName);
            PlayerPrefs.Save();
            PlayerProgress.LoadForPlayer(PlayerName);
        }

        public void LoadLanding() => LoadScene(SceneIds.Landing);

        public void LoadHome() => LoadScene(SceneIds.Home);

        public void LoadMatch3() => LoadScene(SceneIds.Match3);

        public void LoadMatch3FromStreet(HomeVideoId videoId, StreetMatch3Slot slot)
        {
            ActiveStreetVideoId = videoId;
            ActiveStreetSlot = slot;
            HasPendingMatch3Level = true;
            LoadMatch3();
        }

        /// <summary>
        /// Exit Match3 → Home. Street levels resume that street;
        /// Micro3 Left → Micro1, Micro3 Right → Micro4; others start Normal Day.
        /// </summary>
        public void ReturnHomeFromMatch3()
        {
            if (HasPendingMatch3Level &&
                (ActiveStreetVideoId == HomeVideoId.NormalStreet ||
                 ActiveStreetVideoId == HomeVideoId.VacationStreet))
            {
                PendingHomeVideoId = ActiveStreetVideoId;
                HasPendingHomeResume = true;
            }
            else if (HasPendingMatch3Level && ActiveStreetVideoId == HomeVideoId.Micro3)
            {
                PendingHomeVideoId = ActiveStreetSlot == StreetMatch3Slot.Right
                    ? HomeVideoId.Micro4
                    : HomeVideoId.Micro1;
                HasPendingHomeResume = true;
            }
            else
            {
                HasPendingHomeResume = false;
            }

            ClearPendingMatch3Level();
            LoadHome();
        }

        public bool TryConsumeHomeResume(out HomeVideoId videoId)
        {
            if (!HasPendingHomeResume)
            {
                videoId = HomeVideoId.NormalDay;
                return false;
            }

            videoId = PendingHomeVideoId;
            HasPendingHomeResume = false;
            return true;
        }

        public void ClearPendingMatch3Level()
        {
            HasPendingMatch3Level = false;
        }

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        private static bool ShouldLoadLanding()
        {
            if (SceneManager.sceneCount != 1)
                return false;

            return SceneManager.GetActiveScene().name == SceneIds.Boot;
        }
    }
}
