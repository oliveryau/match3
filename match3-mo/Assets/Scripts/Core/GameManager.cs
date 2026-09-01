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
        /// <summary>When true, Home seeks to <see cref="PendingHomeResumeTime"/> and shows street pause UI.</summary>
        public bool PendingHomeResumeAtSegment { get; private set; }
        public float PendingHomeResumeTime { get; private set; }
        public int PendingHomePauseIndex { get; private set; }

        /// <summary>When true with segment resume, skip pause UI and keep playing past the segment.</summary>
        public bool PendingHomeAutoContinue { get; private set; }

        bool _pendingStreetAreaClear;
        HomeVideoId _pendingStreetAreaClearVideoId;
        bool _pendingHomeIntroFromLanding;

        bool _match3ResumeAtSegment;
        float _match3ResumeTime;
        int _match3PauseIndex;

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

        public void LoadHomeFromLanding()
        {
            _pendingHomeIntroFromLanding = true;
            LoadHome();
        }

        public bool TryConsumeHomeIntroFromLanding()
        {
            if (!_pendingHomeIntroFromLanding)
                return false;

            _pendingHomeIntroFromLanding = false;
            return true;
        }

        public void LoadMatch3() => LoadScene(SceneIds.Match3);

        public void LoadMatch3FromStreet(
            HomeVideoId videoId,
            StreetMatch3Slot slot,
            int pauseIndex = 0,
            float resumeTime = 0f,
            bool resumeAtSegment = false)
        {
            ActiveStreetVideoId = videoId;
            ActiveStreetSlot = slot;
            HasPendingMatch3Level = true;
            _match3PauseIndex = pauseIndex;
            _match3ResumeTime = resumeTime;
            _match3ResumeAtSegment = resumeAtSegment;
            LoadMatch3();
        }

        /// <summary>
        /// Exit Match3 → Home. Street levels resume that street at the segment pause;
        /// Micro3 result exit → Micro1/Micro4; Micro3 back button → Normal Day.
        /// </summary>
        /// <param name="earlyExit">True when leaving via Back (not result Exit).</param>
        /// <param name="match3EarnedStars">Stars earned on this Match3 run (0 if unknown / back).</param>
        public void ReturnHomeFromMatch3(bool earlyExit = false, int match3EarnedStars = 0)
        {
            if (HasPendingMatch3Level &&
                (ActiveStreetVideoId == HomeVideoId.NormalStreet ||
                 ActiveStreetVideoId == HomeVideoId.VacationStreet))
            {
                PendingHomeVideoId = ActiveStreetVideoId;
                HasPendingHomeResume = true;
                PendingHomeResumeAtSegment = _match3ResumeAtSegment;
                PendingHomeResumeTime = _match3ResumeTime;
                PendingHomePauseIndex = _match3PauseIndex;
                // One-shot: auto-advance only when exiting after a 3★ clear this run.
                PendingHomeAutoContinue = !earlyExit && match3EarnedStars >= 3;
            }
            else if (HasPendingMatch3Level && ActiveStreetVideoId == HomeVideoId.Micro3)
            {
                PendingHomeAutoContinue = false;
                // Back: skip aftermath micros and return home. Result Exit: Micro1 / Micro4.
                if (earlyExit)
                {
                    HasPendingHomeResume = false;
                    PendingHomeResumeAtSegment = false;
                    PendingHomeResumeTime = 0f;
                    PendingHomePauseIndex = 0;
                }
                else
                {
                    PendingHomeVideoId = ActiveStreetSlot == StreetMatch3Slot.Right
                        ? HomeVideoId.Micro4
                        : HomeVideoId.Micro1;
                    HasPendingHomeResume = true;
                    PendingHomeResumeAtSegment = false;
                    PendingHomeResumeTime = 0f;
                    PendingHomePauseIndex = 0;
                }
            }
            else
            {
                HasPendingHomeResume = false;
                PendingHomeResumeAtSegment = false;
                PendingHomeAutoContinue = false;
            }

            ClearPendingMatch3Level();
            LoadHome();
        }

        public bool TryConsumeHomeResume(
            out HomeVideoId videoId,
            out float resumeTime,
            out int pauseIndex,
            out bool resumeAtSegment,
            out bool autoContinue)
        {
            if (!HasPendingHomeResume)
            {
                videoId = HomeVideoId.NormalDay;
                resumeTime = 0f;
                pauseIndex = 0;
                resumeAtSegment = false;
                autoContinue = false;
                return false;
            }

            videoId = PendingHomeVideoId;
            resumeTime = PendingHomeResumeTime;
            pauseIndex = PendingHomePauseIndex;
            resumeAtSegment = PendingHomeResumeAtSegment;
            autoContinue = PendingHomeAutoContinue;
            HasPendingHomeResume = false;
            PendingHomeResumeAtSegment = false;
            PendingHomeAutoContinue = false;
            return true;
        }

        public void ClearPendingMatch3Level()
        {
            HasPendingMatch3Level = false;
        }

        public void QueueStreetAreaClear(HomeVideoId videoId)
        {
            if (videoId != HomeVideoId.NormalStreet && videoId != HomeVideoId.VacationStreet)
                return;

            _pendingStreetAreaClear = true;
            _pendingStreetAreaClearVideoId = videoId;
        }

        public bool TryConsumeStreetAreaClear(out HomeVideoId videoId)
        {
            if (!_pendingStreetAreaClear)
            {
                videoId = HomeVideoId.NormalDay;
                return false;
            }

            videoId = _pendingStreetAreaClearVideoId;
            _pendingStreetAreaClear = false;
            return true;
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
