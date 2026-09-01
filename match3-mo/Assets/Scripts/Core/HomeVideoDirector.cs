using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Match3
{
    /// <summary>
    /// Home video playback + main-button sequencing.
    /// Normal Day → Street / Travel(Micro2→Vacation Day). Streets show Home only.
    /// </summary>
    public class HomeVideoDirector : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] HomeVideoCatalog catalog;
        [SerializeField] VideoPlayer videoPlayer;
        [SerializeField] RawImage rawImage;
        [Tooltip("Scene RenderTexture shared by VideoPlayer + RawImage. Keep ≤2048 on mobile.")]
        [SerializeField] RenderTexture displayTexture;
        [SerializeField] RectTransform videoRect;
        [SerializeField] GameObject streetUi;
        [SerializeField] GameObject areaClearUi;
        [SerializeField] GameObject continueButton;
        [SerializeField] Image leftButtonImage;
        [SerializeField] Image rightButtonImage;
        [SerializeField] GameObject leftPlayLevelButton;
        [SerializeField] GameObject rightPlayLevelButton;
        [SerializeField] Image leftStar1;
        [SerializeField] Image leftStar2;
        [SerializeField] Image leftStar3;
        [SerializeField] Image rightStar1;
        [SerializeField] Image rightStar2;
        [SerializeField] Image rightStar3;
        const int StarsRequiredForTravel = 3;
        const float PhoneNotificationPulseSpeed = 8f;
        const float PhoneNotificationPulseMin = 0.9f;
        const float PhoneNotificationPulseMax = 1.1f;
        const float MapTravelPulseSpeed = 4f;
        const float MapTravelPulseMin = 0.95f;
        const float MapTravelPulseMax = 1.05f;
        const float ContinueButtonPulseSpeed = 4f;
        const float ContinueButtonPulseMin = 0.95f;
        const float ContinueButtonPulseMax = 1.05f;
        const float PhoneBubbleAgreePulseSpeed = 4f;
        const float PhoneBubbleAgreePulseMin = 0.95f;
        const float PhoneBubbleAgreePulseMax = 1.05f;
        const float AchievementPulseSpeed = 6f;
        const float AchievementPulseMin = 1f;
        const float AchievementPulseMax = 1.1f;
        const float StreetStarPulseSpeed = 6f;
        const float StreetStarPulseMin = 0.9f;
        const float StreetStarPulseMax = 1.1f;
        const float StreetStarPulseDuration = 1.5f;
        const float AreaClearHoldSeconds = 2.5f;
        const float AreaClearFadeSeconds = 0.8f;

        [Header("Main Buttons")]
        [SerializeField] GameObject phoneButton;
        [SerializeField] GameObject streetButton;
        [SerializeField] GameObject travelButton;
        [SerializeField] GameObject travelLockedUi;
        [SerializeField] GameObject homeButton;
        [SerializeField] GameObject roomButton;
        [Header("Phone Photos")]
        [SerializeField] GameObject phonePhotosUi;
        [SerializeField] GameObject phonePhotosCloseButton;
        [Tooltip("Food (1): Normal Street Left — shown after 3★")]
        [SerializeField] GameObject phoneFood1;
        [Tooltip("Food (2): Normal Street Right — shown after 3★")]
        [SerializeField] GameObject phoneFood2;
        [Tooltip("Food (3): Vacation Street Left — shown after 3★")]
        [SerializeField] GameObject phoneFood3;
        [Tooltip("Food (4): Vacation Street Right — shown after 3★")]
        [SerializeField] GameObject phoneFood4;
        [Tooltip("Food (5): Micro3 Left or Right — shown after either is 3★")]
        [SerializeField] GameObject phoneFood5;
        [Tooltip("Normal Street 6★ + hotpot 3★ → Micro5")]
        [SerializeField] GameObject achievement1Button;
        [SerializeField] GameObject achievement1Notification;
        [Tooltip("Vacation Street 6★ → Micro6")]
        [SerializeField] GameObject achievement2Button;
        [SerializeField] GameObject achievement2Notification;
        [Header("Phone Notification (3+ stars on Normal Day)")]
        [SerializeField] GameObject phoneNotificationButton;
        [SerializeField] GameObject phoneNotification2Button;
        [FormerlySerializedAs("phoneBubbleUi")]
        [SerializeField] GameObject phoneWechatUi;
        [SerializeField] GameObject phoneBubbleAgreeButton;
        [SerializeField] GameObject phoneBubbleCloseButton;
        [Header("Map UI (Travel confirm)")]
        [SerializeField] GameObject mapUi;
        [SerializeField] GameObject mapCloseButton;
        [SerializeField] GameObject mapTravelButton;
        [Header("Hotpot UI (Micro3 end)")]
        [SerializeField] GameObject hotpotUi;
        [SerializeField] Image hotpotLeftButtonImage;
        [SerializeField] Image hotpotRightButtonImage;
        [Header("Photo Frames (Normal Day)")]
        [SerializeField] RectTransform photoFramesRoot;
        [SerializeField] GameObject suzhouFanUi;
        [SerializeField] GameObject friendsPhotoUi;
        [SerializeField] GameObject moneyPlantUi;
        [SerializeField] GameObject suzhouFanGlowUi;
        [SerializeField] GameObject friendsPhotoGlowUi;
        [SerializeField] GameObject moneyPlantGlowUi;
        [SerializeField] GameObject suzhouFanLockUi;
        [SerializeField] GameObject friendsPhotoLockUi;
        [SerializeField] GameObject moneyPlantLockUi;
        [Header("Photo Frame Popup")]
        [SerializeField] GameObject photoFramePopup;
        [SerializeField] TMP_Text photoFrameHeaderText;
        [SerializeField] TMP_Text photoFrameDescriptionText;
        [SerializeField] Image photoFrameCollectionImage;
        [SerializeField] GameObject photoFrameOkButton;
        [SerializeField] Sprite suzhouFanPopupSprite;
        [SerializeField] Sprite friendsPhotoPopupSprite;
        [Header("Ornaments Preview")]
        [SerializeField] GameObject ornamentsPreviewUi;
        [SerializeField] GameObject ornamentsPreviewPopup;
        [SerializeField] TMP_Text ornamentsPreviewHeaderText;
        [SerializeField] TMP_Text ornamentsPreviewDescriptionText;
        [SerializeField] Image ornamentsPreviewCollectionImage;
        [SerializeField] GameObject ornamentsPreviewLockIcon;
        [Header("Drag Indicators (Normal / Vacation Day)")]
        [SerializeField] GameObject dragIndicatorsUi;
        [Header("Location")]
        [SerializeField] TMP_Text locationText;
        [Header("Street / Travel / Home / Room Finger Hint")]
        [SerializeField] GameObject streetFingerUi;
        [SerializeField] GameObject travelFingerUi;
        [SerializeField] GameObject homeFingerUi;
        [SerializeField] GameObject roomFingerUi;
        [SerializeField] HomeVideoId videoId = HomeVideoId.NormalDay;

        const string SuzhouFanPopupHeader = "获得苏州纪念品";
        const string SuzhouFanPopupDescription = "尝了苏州美味";
        const string FriendsPhotoPopupHeader = "获得与朋友\n的火锅回忆";
        const string FriendsPhotoPopupDescription = "跟朋友吃火锅";
        const string MoneyPlantPopupHeader = "获得金钱树";
        const string MoneyPlantPopupDescription = "获得9颗星以上";
        const string OrnamentLockedHeader = "???";
        static readonly Color OrnamentLockedTint = new Color(1f, 1f, 1f, 0.45f);
        static readonly Color OrnamentUnlockedTint = Color.white;
        static readonly Color OrnamentLockedDescriptionColor = new Color(0xF3 / 255f, 0x91 / 255f, 0x39 / 255f, 1f);
        static readonly Color OrnamentUnlockedDescriptionColor = new Color(0x34 / 255f, 0xC7 / 255f, 0x59 / 255f, 1f);
        static readonly Vector2 OrnamentPreviewCollectionSize = new Vector2(700f, 600f);
        static readonly Vector2 MoneyPlantPreviewCollectionSize = new Vector2(600f, 750f);
        static readonly Vector2 PhotoFramePopupCollectionSize = new Vector2(588.0621f, 518f);
        static readonly Vector2 MoneyPlantPhotoFramePopupCollectionSize = new Vector2(588.0621f, 600f);
        const float MainButtonFingerIdleSeconds = 2f;
        const float PhotoFrameGlowSeconds = 3f;
        const float OrnamentInspectDuration = 0.35f;
        const float OrnamentInspectDimAlpha = 0.78431374f;

        HomeVideoEntry _entry;
        readonly List<float> _pauseTimes = new List<float>();
        int _pauseIndex;
        bool _watchingPause;
        bool _needWrapBeforePause;
        double _lastTime;
        float _armRealtime;
        bool _dragEnabled;
        float _mainButtonFingerIdleElapsed;
        int _mainButtonFingerMode; // 0 none, 1 travel, 2 street, 3 home, 4 room
        bool _pendingResumeAtSegment;
        float _pendingResumeTime;
        int _pendingResumePauseIndex;
        bool _pendingAutoContinue;
        int _pauseAfterLoopIndex = -1;
        Coroutine _loadRoutine;
        Coroutine _phoneNotificationPulse;
        Coroutine _phoneNotification2Pulse;
        Coroutine _mapTravelPulse;
        Coroutine _continueButtonPulse;
        Coroutine _phoneBubbleAgreePulse;
        Coroutine _achievement1Pulse;
        Coroutine _achievement2Pulse;
        Coroutine _streetStarPulse;
        Coroutine _areaClearRoutine;
        bool _pendingAreaClear;
        HomeVideoId _pendingAreaClearStreet;
        bool _areaClearShowing;
        bool _streetPauseUiVisible;
        CanvasGroup _areaClearCanvasGroup;
        Coroutine _suzhouFanGlowRoutine;
        Coroutine _friendsPhotoGlowRoutine;
        Coroutine _moneyPlantGlowRoutine;
        Coroutine _ornamentInspectRoutine;
        GameObject _ornamentInspectOverlay;
        Image _ornamentInspectDim;
        RectTransform _inspectingOrnament;
        Transform _ornamentRestParent;
        int _ornamentRestSiblingIndex;
        Vector2 _ornamentRestAnchoredPosition;
        Vector3 _ornamentRestScale;
        Vector3 _ornamentRestWorldPosition;
        Vector3 _ornamentInspectZoomScale;
        CanvasGroup _ornamentsPreviewCanvasGroup;
        bool _ornamentInspected;
        bool _ornamentPreviewShown;
        bool _prepareFailed;
        string _lastError;
        /// <summary>False = normal home, true = vacation home.</summary>
        bool _atVacation;
        Color _travelImageColor = Color.white;
        enum PhotoFramePopupKind { None, SuzhouFan, FriendsPhoto, MoneyPlant }
        PhotoFramePopupKind _activePhotoFramePopup;

        void Awake()
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
            if (rawImage == null)
                rawImage = GetComponent<RawImage>();
            if (videoRect == null)
                videoRect = transform as RectTransform;

            if (displayTexture == null && videoPlayer != null)
                displayTexture = videoPlayer.targetTexture;

            EnsureDisplayTextureReady();

            if (continueButton != null)
            {
                var button = continueButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveListener(AudioManager.PlayUiClickStatic);
                    button.onClick.RemoveListener(OnContinuePressed);
                    button.onClick.AddListener(AudioManager.PlayUiClickStatic);
                    button.onClick.AddListener(OnContinuePressed);
                }
            }

            WireStreetButton(hotpotLeftButtonImage, OnHotpotLeftPressed);
            WireStreetButton(hotpotRightButtonImage, OnHotpotRightPressed);
            WireMainButton(leftPlayLevelButton, OnLeftLevelPressed);
            WireMainButton(rightPlayLevelButton, OnRightLevelPressed);
            WireMainButton(streetButton, OnStreetPressed);
            WireMainButton(travelButton, OnTravelPressed);
            WireMainButton(homeButton, OnHomePressed);
            WireMainButton(roomButton, OnRoomPressed);
            WireMainButton(phoneButton, OnPhonePressed);
            WireMainButton(phonePhotosCloseButton, OnPhonePhotosClosePressed);
            WireMainButton(achievement1Button, OnAchievement1Pressed);
            WireMainButton(achievement2Button, OnAchievement2Pressed);
            WireMainButton(mapCloseButton, OnMapClosePressed);
            WireMainButton(mapTravelButton, OnMapTravelPressed);
            WireMainButton(phoneNotificationButton, OnPhoneNotificationPressed);
            WireMainButton(phoneNotification2Button, OnPhoneNotification2Pressed);
            WireMainButton(phoneBubbleAgreeButton, OnPhoneBubbleAgreePressed);
            WireMainButton(phoneBubbleCloseButton, OnPhoneBubbleClosePressed);
            WireMainButton(photoFrameOkButton, OnPhotoFrameOkPressed);
            EnableGraphicRaycast(suzhouFanUi);
            EnableGraphicRaycast(friendsPhotoUi);
            EnableGraphicRaycast(moneyPlantUi);
            WireMainButton(suzhouFanUi, OnSuzhouFanOrnamentPressed);
            WireMainButton(friendsPhotoUi, OnFriendsPhotoOrnamentPressed);
            WireMainButton(moneyPlantUi, OnMoneyPlantOrnamentPressed);
            EnsureOrnamentInspectOverlay();

            if (travelButton != null)
            {
                var travelImage = travelButton.GetComponent<Image>();
                if (travelImage != null)
                    _travelImageColor = travelImage.color;
            }

            if (phonePhotosUi != null)
                phonePhotosUi.SetActive(false);
            RefreshPhoneFoods();
            if (mapUi != null)
                mapUi.SetActive(false);
            if (hotpotUi != null)
                hotpotUi.SetActive(false);
            if (phoneWechatUi != null)
                phoneWechatUi.SetActive(false);
            if (phoneNotificationButton != null)
                phoneNotificationButton.SetActive(false);
            if (phoneNotification2Button != null)
                phoneNotification2Button.SetActive(false);
            if (travelLockedUi != null)
                travelLockedUi.SetActive(false);
            HidePhotoFramePopup();
            SnapOrnamentInspectClosed();
            SetPhotoFramesVisible(false);
            HidePhotoFrameGlows();
            HideDragIndicators();
            HideMainButtonFingers();
            RefreshLocationText();
        }

        static void WireStreetButton(Image image, UnityEngine.Events.UnityAction handler)
        {
            if (image == null)
                return;
            var button = image.GetComponent<Button>();
            if (button == null)
                return;
            button.onClick.RemoveListener(AudioManager.PlayUiClickStatic);
            button.onClick.RemoveListener(handler);
            button.onClick.AddListener(AudioManager.PlayUiClickStatic);
            button.onClick.AddListener(handler);
        }

        static void WireMainButton(GameObject go, UnityEngine.Events.UnityAction handler)
        {
            if (go == null)
                return;
            var button = go.GetComponent<Button>();
            if (button == null)
                return;
            button.onClick.RemoveListener(AudioManager.PlayUiClickStatic);
            button.onClick.RemoveListener(handler);
            button.onClick.AddListener(AudioManager.PlayUiClickStatic);
            button.onClick.AddListener(handler);
        }

        void OnEnable()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached += OnLoopPointReached;
                videoPlayer.errorReceived += OnVideoError;
                videoPlayer.prepareCompleted += OnPrepareCompleted;
            }

            _atVacation = false;
            _pendingResumeAtSegment = false;
            _pendingResumeTime = 0f;
            _pendingResumePauseIndex = 0;
            _pendingAutoContinue = false;
            _pauseAfterLoopIndex = -1;
            _pendingAreaClear = false;
            var startId = HomeVideoId.NormalDay;
            bool fromLanding = GameManager.Instance != null
                && GameManager.Instance.TryConsumeHomeIntroFromLanding();
            if (fromLanding && !PlayerProgress.HasWatchedHomeIntro())
                startId = HomeVideoId.Micro7;
            else if (GameManager.Instance != null &&
                GameManager.Instance.TryConsumeHomeResume(
                    out var resumeId, out var resumeTime, out var pauseIndex, out var atSegment, out var autoContinue))
            {
                startId = resumeId;
                _pendingResumeAtSegment = atSegment;
                _pendingResumeTime = resumeTime;
                _pendingResumePauseIndex = pauseIndex;
                _pendingAutoContinue = autoContinue;
            }

            if (GameManager.Instance != null &&
                GameManager.Instance.TryConsumeStreetAreaClear(out var areaClearStreet))
            {
                _pendingAreaClear = true;
                _pendingAreaClearStreet = areaClearStreet;
            }

            HideAreaClearUi();
            Play(startId);
        }

        void OnDisable()
        {
            StopLoad();
            _watchingPause = false;
            _pauseAfterLoopIndex = -1;
            _dragEnabled = false;
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnLoopPointReached;
                videoPlayer.errorReceived -= OnVideoError;
                videoPlayer.prepareCompleted -= OnPrepareCompleted;
                if (videoPlayer.isPlaying)
                    videoPlayer.Stop();
            }
            HidePauseUi();
            SetStreetUiVisible(false);
            HideHotpotUi();
            HideMapUi();
            HidePhoneWechat();
            HidePhoneNotification();
            HidePhoneNotification2();
            HidePhotoFramePopup();
            SnapOrnamentInspectClosed();
            SetPhotoFramesVisible(false);
            HidePhotoFrameGlows();
            HideDragIndicators();
            HideMainButtonFingers();
            HideAreaClearUi();
        }

        void OnVideoError(VideoPlayer source, string message)
        {
            _prepareFailed = true;
            _lastError = message;
            Debug.LogError($"HomeVideoDirector: VideoPlayer error — {message}");
        }

        void OnPrepareCompleted(VideoPlayer source)
        {
            if (rawImage != null && source != null && source.targetTexture != null)
                rawImage.texture = source.targetTexture;
        }

        bool EnsureDisplayTextureReady()
        {
            if (displayTexture == null)
                return false;

            if (!displayTexture.IsCreated())
            {
                if (!displayTexture.Create())
                {
                    Debug.LogError(
                        $"HomeVideoDirector: RenderTexture Create() failed " +
                        $"({displayTexture.width}x{displayTexture.height}). Use ≤2048 on mobile.");
                    return false;
                }
            }

            if (videoPlayer != null)
            {
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = displayTexture;
            }

            if (rawImage != null)
                rawImage.texture = displayTexture;

            return true;
        }

        void Update()
        {
#if UNITY_EDITOR
            if (TryEditorSwitchVideo())
                return;
#endif
            UpdateMainButtonFingerHint();

            if (!_watchingPause || videoPlayer == null || !videoPlayer.isPrepared)
                return;
            if (Time.realtimeSinceStartup < _armRealtime)
                return;

            double time = videoPlayer.time;

            if (_pauseAfterLoopIndex >= 0)
            {
                _lastTime = time;
                return;
            }

            if (_needWrapBeforePause)
            {
                if (time + 0.2 < _lastTime)
                    _needWrapBeforePause = false;
                _lastTime = time;
                return;
            }

            float pauseAt = _pauseTimes[_pauseIndex];
            if (_lastTime < pauseAt && time >= pauseAt)
            {
                PauseForContinue();
                return;
            }

            _lastTime = time;
        }

#if UNITY_EDITOR
        bool TryEditorSwitchVideo()
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1) && !Input.GetKeyDown(KeyCode.Keypad1))
                return false;
            if (catalog == null || catalog.VideoCount == 0)
                return false;

            int index = catalog.IndexOf(videoId);
            if (index < 0)
                index = 0;
            int next = (index + 1) % catalog.VideoCount;
            Play(catalog.GetIdAt(next));
            return true;
        }
#endif

        public void Play(HomeVideoId id)
        {
            videoId = id;
            if (id == HomeVideoId.VacationDay || id == HomeVideoId.VacationStreet)
                _atVacation = true;
            else if (id == HomeVideoId.NormalDay || id == HomeVideoId.NormalStreet)
                _atVacation = false;

            if (AudioManager.Instance != null)
                AudioManager.Instance.ApplyForHomeVideo(id);

            StopLoad();
            _loadRoutine = StartCoroutine(PlayWhenReady(id));
        }

        IEnumerator PlayWhenReady(HomeVideoId id)
        {
            videoId = id;
            yield return null;
            yield return LoadAndStart();
        }

        public void OnPhonePressed()
        {
            // While notification 1 is up, Phone opens WeChat instead of photos.
            if (phoneNotificationButton != null && phoneNotificationButton.activeSelf)
            {
                ShowPhoneWechat();
                return;
            }

            if (phonePhotosUi != null)
                phonePhotosUi.SetActive(true);
            RefreshPhoneFoods();
        }

        public void OnPhonePhotosClosePressed()
        {
            HidePhonePhotos();
        }

        void RefreshPhoneFoods()
        {
            SetPhoneFoodActive(phoneFood1, HomeVideoId.NormalStreet, StreetMatch3Slot.Left);
            SetPhoneFoodActive(phoneFood2, HomeVideoId.NormalStreet, StreetMatch3Slot.Right);
            SetPhoneFoodActive(phoneFood3, HomeVideoId.VacationStreet, StreetMatch3Slot.Left);
            SetPhoneFoodActive(phoneFood4, HomeVideoId.VacationStreet, StreetMatch3Slot.Right);
            // Hotpot: either Micro3 level at 3★ unlocks Food (5).
            SetPhoneFoodActive(phoneFood5, HasAnyMicro3ThreeStarClear());
            RefreshAchievementButtons();
        }

        void RefreshAchievementButtons()
        {
            bool unlocked1 = ShouldShowAchievement1();
            bool unlocked2 = ShouldShowAchievement2();
            bool showNotif1 = unlocked1 && !PlayerProgress.HasWatchedAchievement1();
            bool showNotif2 = unlocked2 && !PlayerProgress.HasWatchedAchievement2();

            SetGoActive(achievement1Button, unlocked1);
            SetGoActive(achievement1Notification, showNotif1);
            SetGoActive(achievement2Button, unlocked2);
            SetGoActive(achievement2Notification, showNotif2);

            bool photosOpen = phonePhotosUi != null && phonePhotosUi.activeInHierarchy;
            if (showNotif1 && photosOpen)
                StartAchievement1Pulse();
            else
                StopAchievement1Pulse();

            if (showNotif2 && photosOpen)
                StartAchievement2Pulse();
            else
                StopAchievement2Pulse();
        }

        static bool ShouldShowAchievement1() =>
            HasCompletedNormalStreet() && HasAnyMicro3ThreeStarClear();

        static bool ShouldShowAchievement2() =>
            HasCompletedVacationStreet();

        static bool HasUnwatchedAchievement() =>
            (ShouldShowAchievement1() && !PlayerProgress.HasWatchedAchievement1())
            || (ShouldShowAchievement2() && !PlayerProgress.HasWatchedAchievement2());

        void MarkAchievementWatchedIfNeeded(HomeVideoId id)
        {
            if (id == HomeVideoId.Micro5)
                PlayerProgress.MarkAchievement1Watched();
            else if (id == HomeVideoId.Micro6)
                PlayerProgress.MarkAchievement2Watched();
        }

        static void SetPhoneFoodActive(GameObject foodUi, HomeVideoId levelVideoId, StreetMatch3Slot slot)
        {
            SetPhoneFoodActive(foodUi, PlayerProgress.GetStars(levelVideoId, slot) >= 3);
        }

        static void SetPhoneFoodActive(GameObject foodUi, bool unlocked)
        {
            if (foodUi == null)
                return;
            if (foodUi.activeSelf != unlocked)
                foodUi.SetActive(unlocked);
        }

        public void OnStreetPressed()
        {
            if (IsMicro(videoId) || IsStreet(videoId))
                return;

            HideMainButtonFingers();
            ResetMainButtonFingerIdle();

            HidePhonePhotos();
            HideMapUi();
            HidePhoneWechat();
            Play(_atVacation || videoId == HomeVideoId.VacationDay
                ? HomeVideoId.VacationStreet
                : HomeVideoId.NormalStreet);
        }

        public void OnTravelPressed()
        {
            // Normal Day + unlocked: open map first; Micro2 starts from map Travel.
            if (videoId != HomeVideoId.NormalDay || !HasTravelUnlocked())
                return;

            HideMainButtonFingers();
            ResetMainButtonFingerIdle();

            HidePhonePhotos();
            HidePhoneWechat();
            if (mapUi != null)
            {
                mapUi.SetActive(true);
                StartMapTravelPulse();
            }
        }

        public void OnMapClosePressed()
        {
            HideMapUi();
        }

        public void OnMapTravelPressed()
        {
            HideMapUi();
            HidePhonePhotos();
            HidePhoneWechat();
            Play(HomeVideoId.Micro2);
        }

        void HideMapUi()
        {
            StopMapTravelPulse();
            if (mapUi != null)
                mapUi.SetActive(false);
        }

        public void OnHomePressed()
        {
            HidePhonePhotos();
            HideMapUi();
            HidePhoneWechat();
            HideMainButtonFingers();
            ResetMainButtonFingerIdle();

            // Vacation Day → Normal Day
            if (videoId == HomeVideoId.VacationDay)
            {
                Play(HomeVideoId.NormalDay);
                return;
            }

            // Normal Street → Normal Day (Vacation Street uses Room)
            if (videoId != HomeVideoId.NormalStreet)
                return;

            Play(HomeVideoId.NormalDay);
        }

        public void OnRoomPressed()
        {
            if (videoId != HomeVideoId.VacationStreet)
                return;

            HidePhonePhotos();
            HideMapUi();
            HidePhoneWechat();
            HideMainButtonFingers();
            ResetMainButtonFingerIdle();
            Play(HomeVideoId.VacationDay);
        }

        public void OnPhoneNotificationPressed()
        {
            if (videoId != HomeVideoId.NormalDay || !HasTravelUnlocked())
                return;
            ShowPhoneWechat();
        }

        public void OnPhoneNotification2Pressed()
        {
            if (phonePhotosUi != null)
                phonePhotosUi.SetActive(true);
            RefreshPhoneFoods();
        }

        public void OnAchievement1Pressed()
        {
            if (!ShouldShowAchievement1())
                return;
            HidePhonePhotos();
            HideMapUi();
            HidePhoneWechat();
            HidePhoneNotification2();
            Play(HomeVideoId.Micro5);
        }

        public void OnAchievement2Pressed()
        {
            if (!ShouldShowAchievement2())
                return;
            HidePhonePhotos();
            HideMapUi();
            HidePhoneWechat();
            HidePhoneNotification2();
            Play(HomeVideoId.Micro6);
        }

        public void OnPhoneBubbleAgreePressed()
        {
            HidePhoneWechat();
            HidePhoneNotification();
            HidePhonePhotos();
            HideMapUi();
            Play(HomeVideoId.Micro3);
        }

        public void OnPhoneBubbleClosePressed()
        {
            HidePhoneWechat();
        }

        void HidePhonePhotos()
        {
            StopAchievement1Pulse();
            StopAchievement2Pulse();
            if (phonePhotosUi != null)
                phonePhotosUi.SetActive(false);
        }

        void ShowPhoneWechat()
        {
            if (phoneWechatUi != null)
                phoneWechatUi.SetActive(true);
            StartPhoneBubbleAgreePulse();
        }

        void HidePhoneWechat()
        {
            StopPhoneBubbleAgreePulse();
            if (phoneWechatUi != null)
                phoneWechatUi.SetActive(false);
        }

        void HidePhoneNotification()
        {
            StopPhoneNotificationPulse();
            if (phoneNotificationButton != null)
                phoneNotificationButton.SetActive(false);
        }

        void HidePhoneNotification2()
        {
            StopPhoneNotification2Pulse();
            if (phoneNotification2Button != null)
                phoneNotification2Button.SetActive(false);
        }

        public void OnContinuePressed()
        {
            if (_entry == null || _entry.mode != HomeVideoPlaybackMode.Segmented)
                return;
            if (_watchingPause || _pauseTimes.Count == 0)
                return;

            _pauseIndex = (_pauseIndex + 1) % _pauseTimes.Count;
            HidePauseUi();
            ResumeTowardCurrentPause();
        }

        public void OnLeftLevelPressed() => EnterMatch3(StreetMatch3Slot.Left);

        public void OnRightLevelPressed() => EnterMatch3(StreetMatch3Slot.Right);

        public void OnHotpotLeftPressed() => EnterMatch3FromHotpot(StreetMatch3Slot.Left);

        public void OnHotpotRightPressed() => EnterMatch3FromHotpot(StreetMatch3Slot.Right);

        void EnterMatch3(StreetMatch3Slot slot)
        {
            if (_entry == null || _entry.mode != HomeVideoPlaybackMode.Segmented)
                return;
            if (!IsStreet(videoId))
                return;
            LoadMatch3(slot);
        }

        void EnterMatch3FromHotpot(StreetMatch3Slot slot)
        {
            if (videoId != HomeVideoId.Micro3 || _entry == null)
                return;
            LoadMatch3(slot);
        }

        static int PauseIndexForSlot(StreetMatch3Slot slot) =>
            slot == StreetMatch3Slot.Left ? 0 : 1;

        void LoadMatch3(StreetMatch3Slot slot)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("HomeVideoDirector: GameManager missing; cannot open Match3.");
                return;
            }

            bool resumeAtSegment = IsStreet(videoId) && _pauseTimes.Count > 0;
            int pauseIndex = resumeAtSegment
                ? Mathf.Clamp(PauseIndexForSlot(slot), 0, _pauseTimes.Count - 1)
                : 0;
            float resumeTime = resumeAtSegment ? _pauseTimes[pauseIndex] : 0f;
            GameManager.Instance.LoadMatch3FromStreet(videoId, slot, pauseIndex, resumeTime, resumeAtSegment);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_ornamentInspected)
                return;
            NotifyPlayerDragged();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ornamentInspected || !_dragEnabled || videoRect == null || eventData == null)
                return;

            NotifyPlayerDragged();

            var parent = videoRect.parent as RectTransform;
            if (parent == null)
                return;

            float extra = Mathf.Max(0f, videoRect.rect.width - parent.rect.width);
            float minX = -extra * 0.5f;
            float maxX = extra * 0.5f;
            var pos = videoRect.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x + eventData.delta.x, minX, maxX);
            pos.y = 0f;
            videoRect.anchoredPosition = pos;
        }

        void NotifyPlayerDragged()
        {
            if (!_dragEnabled || !IsDayHomeVideo(videoId))
                return;

            if (!PlayerProgress.HasCompletedHomeDragIntro())
            {
                PlayerProgress.MarkHomeDragIntroCompleted();
                HideDragIndicators();
                RefreshMainButtons();
            }
        }

        void HideDragIndicators() => SetGoActive(dragIndicatorsUi, false);

        void RefreshDragIntroUi()
        {
            // Only on the very first home visit — never from idle time.
            bool showIntro = !PlayerProgress.HasCompletedHomeDragIntro()
                && IsDayHomeVideo(videoId)
                && _dragEnabled;
            SetGoActive(dragIndicatorsUi, showIntro);
        }

        bool ShouldHoldMainButtonsForDragIntro() =>
            !PlayerProgress.HasCompletedHomeDragIntro() && IsDayHomeVideo(videoId);

        void UpdateMainButtonFingerHint()
        {
            bool travelHint = IsTravelFingerEligible();
            bool streetHint = !travelHint && IsStreetFingerEligible();
            bool homeHint = !travelHint && !streetHint && IsHomeFingerEligible();
            bool roomHint = !travelHint && !streetHint && !homeHint && IsRoomFingerEligible();
            int mode = travelHint ? 1 : streetHint ? 2 : homeHint ? 3 : roomHint ? 4 : 0;

            if (mode == 0)
            {
                ResetMainButtonFingerIdle();
                HideMainButtonFingers();
                return;
            }

            // Home / Room on completed streets: show immediately.
            if (mode == 3 || mode == 4)
            {
                _mainButtonFingerMode = mode;
                SetGoActive(streetFingerUi, false);
                SetGoActive(travelFingerUi, false);
                SetGoActive(homeFingerUi, mode == 3);
                SetGoActive(roomFingerUi, mode == 4);
                return;
            }

            if (mode != _mainButtonFingerMode)
            {
                _mainButtonFingerMode = mode;
                _mainButtonFingerIdleElapsed = 0f;
                HideMainButtonFingers();
            }

            _mainButtonFingerIdleElapsed += Time.unscaledDeltaTime;
            if (_mainButtonFingerIdleElapsed < MainButtonFingerIdleSeconds)
                return;

            SetGoActive(streetFingerUi, mode == 2);
            SetGoActive(travelFingerUi, mode == 1);
            SetGoActive(homeFingerUi, false);
            SetGoActive(roomFingerUi, false);
        }

        bool IsTravelFingerEligible()
        {
            if (travelButton == null || !travelButton.activeSelf)
                return false;
            if (videoId != HomeVideoId.NormalDay || !HasTravelUnlocked())
                return false;
            if (HasCompletedVacationStreet())
                return false;
            var button = travelButton.GetComponent<Button>();
            return button == null || button.interactable;
        }

        bool IsStreetFingerEligible()
        {
            if (streetButton == null || !streetButton.activeSelf)
                return false;
            if (videoId == HomeVideoId.NormalDay)
                return !HasCompletedNormalStreet();
            if (videoId == HomeVideoId.VacationDay)
                return !HasCompletedVacationStreet();
            return false;
        }

        bool IsHomeFingerEligible()
        {
            if (homeButton == null || !homeButton.activeSelf)
                return false;
            return videoId == HomeVideoId.NormalStreet && HasCompletedNormalStreet();
        }

        bool IsRoomFingerEligible()
        {
            if (roomButton == null || !roomButton.activeSelf)
                return false;
            return videoId == HomeVideoId.VacationStreet && HasCompletedVacationStreet();
        }

        void ResetMainButtonFingerIdle()
        {
            _mainButtonFingerIdleElapsed = 0f;
            _mainButtonFingerMode = 0;
        }

        void HideMainButtonFingers()
        {
            SetGoActive(streetFingerUi, false);
            SetGoActive(travelFingerUi, false);
            SetGoActive(homeFingerUi, false);
            SetGoActive(roomFingerUi, false);
        }

        void RefreshLocationText()
        {
            if (locationText == null)
                return;

            locationText.text = LocationLabelFor(videoId);
        }

        static string LocationLabelFor(HomeVideoId id)
        {
            switch (id)
            {
                case HomeVideoId.NormalDay: return "我的家";
                case HomeVideoId.NormalStreet: return "美食街";
                case HomeVideoId.VacationDay: return "苏州酒店";
                case HomeVideoId.VacationStreet: return "苏州美食街";
                case HomeVideoId.Micro2: return "苏州";
                case HomeVideoId.Micro1:
                case HomeVideoId.Micro3:
                case HomeVideoId.Micro4: return "火锅店";
                default: return string.Empty;
            }
        }

        static bool IsDayHomeVideo(HomeVideoId id) =>
            id == HomeVideoId.NormalDay || id == HomeVideoId.VacationDay;

        IEnumerator LoadAndStart()
        {
            _watchingPause = false;
            _dragEnabled = false;
            ResetMainButtonFingerIdle();
            _prepareFailed = false;
            _lastError = null;
            HidePauseUi();
            SetStreetUiVisible(false);
            HideHotpotUi();
            HideMapUi();
            HideDragIndicators();
            HideMainButtonFingers();
            SnapOrnamentInspectClosed();
            ResetPan();
            RefreshMainButtons();
            RefreshLocationText();
            RefreshPhotoFramesForCurrentVideo();

            if (catalog == null || videoPlayer == null)
            {
                Debug.LogError("HomeVideoDirector: catalog or VideoPlayer is missing.");
                yield break;
            }

            _entry = catalog.GetEntry(videoId);
            if (_entry == null || _entry.clip == null)
            {
                if (videoId == HomeVideoId.Micro5 || videoId == HomeVideoId.Micro6 || videoId == HomeVideoId.Micro7)
                {
                    Debug.LogWarning($"HomeVideoDirector: {videoId} has no clip yet; returning home.");
                    MarkAchievementWatchedIfNeeded(videoId);
                    Play(CurrentDayHomeVideo());
                    yield break;
                }

                Debug.LogError($"HomeVideoDirector: no clip for {videoId}.");
                yield break;
            }

            if (!IsStreet(videoId))
            {
                _pendingResumeAtSegment = false;
                _pendingAutoContinue = false;
            }

            if (!EnsureDisplayTextureReady())
                yield break;

            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;
            videoPlayer.aspectRatio = UsesNativeAspect(videoId)
                ? VideoAspectRatio.FitVertically
                : VideoAspectRatio.Stretch;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = displayTexture;
            if (rawImage != null)
            {
                rawImage.texture = displayTexture;
                rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            }

            if (_entry.mute)
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            else
                videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            if (videoPlayer.isPlaying)
                videoPlayer.Stop();

            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = _entry.clip;
            videoPlayer.isLooping = _entry.loop;
            videoPlayer.Prepare();

            float prepareDeadline = Time.realtimeSinceStartup + 15f;
            while (videoPlayer != null && !videoPlayer.isPrepared && !_prepareFailed)
            {
                if (Time.realtimeSinceStartup > prepareDeadline)
                {
                    Debug.LogError($"HomeVideoDirector: Prepare timed out for '{_entry.clip.name}'.");
                    yield break;
                }
                yield return null;
            }

            if (!isActiveAndEnabled || videoPlayer == null)
                yield break;

            if (_prepareFailed)
            {
                Debug.LogError($"HomeVideoDirector: prepare failed for '{_entry.clip.name}': {_lastError}");
                yield break;
            }

            if (rawImage != null)
                rawImage.texture = displayTexture;

            StartPlaybackForMode();
            RefreshMainButtons();
            RefreshDragIntroUi();
            TryStartPendingAreaClear();

            float frameDeadline = Time.realtimeSinceStartup + 3f;
            while (videoPlayer != null && videoPlayer.frame < 1 && Time.realtimeSinceStartup < frameDeadline)
                yield return null;
        }

        void StartPlaybackForMode()
        {
            switch (_entry.mode)
            {
                case HomeVideoPlaybackMode.HorizontalDrag:
                    _dragEnabled = true;
                    videoPlayer.Play();
                    break;

                case HomeVideoPlaybackMode.Normal:
                    _dragEnabled = false;
                    videoPlayer.Play();
                    break;

                case HomeVideoPlaybackMode.Segmented:
                    _dragEnabled = false;
                    ApplyStreetButtonSprites();
                    BuildPauseTimes();
                    if (_pendingResumeAtSegment && _pauseTimes.Count > 0)
                    {
                        _pauseIndex = Mathf.Clamp(_pendingResumePauseIndex, 0, _pauseTimes.Count - 1);
                        bool autoContinue = _pendingAutoContinue;
                        _pendingResumeAtSegment = false;
                        _pendingAutoContinue = false;
                        if (autoContinue)
                        {
                            // 3★ Match3 exit: resume at this segment's pause, play forward to the
                            // next segment pause, then show street UI again (one-shot).
                            int clearedIndex = _pauseIndex;
                            float resumeTime = _pendingResumeTime > 0.01f
                                ? _pendingResumeTime
                                : _pauseTimes[clearedIndex];
                            PlayForwardToNextSegmentAfterClear(clearedIndex, resumeTime);
                        }
                        else
                        {
                            float resumeTime = _pendingResumeTime > 0.01f
                                ? _pendingResumeTime
                                : _pauseTimes[_pauseIndex];
                            videoPlayer.time = resumeTime;
                            videoPlayer.Play();
                            videoPlayer.Pause();
                            ShowPauseUi(_pauseIndex);
                        }
                        break;
                    }

                    _pendingAutoContinue = false;
                    videoPlayer.Play();
                    if (_pauseTimes.Count > 0)
                    {
                        _pauseIndex = 0;
                        videoPlayer.time = 0;
                        BeginWatchingCurrentPause(needWrap: false);
                    }
                    break;
            }
        }

        void RefreshMainButtons()
        {
            bool micro = IsMicro(videoId);
            bool vacationStreet = videoId == HomeVideoId.VacationStreet;
            bool normalStreet = videoId == HomeVideoId.NormalStreet;
            bool vacationDay = videoId == HomeVideoId.VacationDay;
            bool normalDay = videoId == HomeVideoId.NormalDay;

            // First-time home only: hide main buttons until the player drags once.
            if (ShouldHoldMainButtonsForDragIntro())
            {
                SetGoActive(phoneButton, false);
                SetGoActive(streetButton, false);
                SetGoActive(travelButton, false);
                SetGoActive(homeButton, false);
                SetGoActive(roomButton, false);
                RefreshTravelLockState(travelVisible: false);
                RefreshPhoneNotifications(phoneVisible: false);
                return;
            }

            if (micro)
            {
                SetGoActive(phoneButton, false);
                SetGoActive(streetButton, false);
                SetGoActive(travelButton, false);
                SetGoActive(homeButton, false);
                SetGoActive(roomButton, false);
                RefreshTravelLockState(travelVisible: false);
                RefreshPhoneNotifications(phoneVisible: false);
                return;
            }

            if (vacationStreet)
            {
                // Vacation Street: room only
                SetGoActive(phoneButton, false);
                SetGoActive(streetButton, false);
                SetGoActive(travelButton, false);
                SetGoActive(homeButton, false);
                SetGoActive(roomButton, true);
                RefreshTravelLockState(travelVisible: false);
                RefreshPhoneNotifications(phoneVisible: false);
                return;
            }

            if (normalStreet)
            {
                // Normal Street: home only
                SetGoActive(phoneButton, false);
                SetGoActive(streetButton, false);
                SetGoActive(travelButton, false);
                SetGoActive(homeButton, true);
                SetGoActive(roomButton, false);
                RefreshTravelLockState(travelVisible: false);
                RefreshPhoneNotifications(phoneVisible: false);
                return;
            }

            if (vacationDay)
            {
                // Vacation Day: home + phone + street; travel/room hidden
                SetGoActive(phoneButton, true);
                SetGoActive(streetButton, true);
                SetGoActive(travelButton, false);
                SetGoActive(homeButton, true);
                SetGoActive(roomButton, false);
                RefreshTravelLockState(travelVisible: false);
                RefreshPhoneNotifications(phoneVisible: true);
                return;
            }

            // Normal Day / other: phone + street + travel; home/room hidden
            SetGoActive(phoneButton, true);
            SetGoActive(streetButton, true);
            SetGoActive(travelButton, true);
            SetGoActive(homeButton, false);
            SetGoActive(roomButton, false);
            RefreshTravelLockState(travelVisible: true);
            RefreshPhoneNotifications(phoneVisible: true);
        }

        void RefreshTravelLockState(bool travelVisible)
        {
            bool unlocked = HasTravelUnlocked();
            bool showLocked = travelVisible && !unlocked;

            SetGoActive(travelLockedUi, showLocked);

            if (travelButton == null)
                return;

            var button = travelButton.GetComponent<Button>();
            if (button != null)
                button.interactable = travelVisible && unlocked;

            var image = travelButton.GetComponent<Image>();
            if (image != null)
                image.color = showLocked ? new Color(0.78f, 0.78f, 0.78f, _travelImageColor.a) : _travelImageColor;
        }

        /// <summary>
        /// ≥3 total stars, and no Micro3 Match3 level cleared at 3★ yet
        /// (so notification returns after a crash mid-Micro3).
        /// </summary>
        static bool ShouldShowPhoneNotification() =>
            HasTravelUnlocked() && !HasAnyMicro3ThreeStarClear();

        static bool HasTravelUnlocked() =>
            PlayerProgress.GetTotalStars() >= StarsRequiredForTravel;

        static bool HasAnyMicro3ThreeStarClear() =>
            PlayerProgress.GetStars(HomeVideoId.Micro3, StreetMatch3Slot.Left) >= 3
            || PlayerProgress.GetStars(HomeVideoId.Micro3, StreetMatch3Slot.Right) >= 3;

        static bool HasCompletedVacationStreet() =>
            PlayerProgress.GetStars(HomeVideoId.VacationStreet, StreetMatch3Slot.Left) >= 3
            && PlayerProgress.GetStars(HomeVideoId.VacationStreet, StreetMatch3Slot.Right) >= 3;

        static bool HasCompletedNormalStreet() =>
            PlayerProgress.GetStars(HomeVideoId.NormalStreet, StreetMatch3Slot.Left) >= 3
            && PlayerProgress.GetStars(HomeVideoId.NormalStreet, StreetMatch3Slot.Right) >= 3;

        /// <summary>
        /// Pulse Continue when the level at this pause is already 3★ and the other
        /// street level is not — nudge onward without pulsing on an uncleared level.
        /// </summary>
        bool ShouldPulseContinueButton()
        {
            if (!IsStreet(videoId) || continueButton == null || !continueButton.activeSelf)
                return false;

            var currentSlot = (_pauseIndex % 2) == 0 ? StreetMatch3Slot.Left : StreetMatch3Slot.Right;
            var otherSlot = currentSlot == StreetMatch3Slot.Left
                ? StreetMatch3Slot.Right
                : StreetMatch3Slot.Left;

            if (PlayerProgress.GetStars(videoId, currentSlot) < 3)
                return false;
            if (PlayerProgress.GetStars(videoId, otherSlot) >= 3)
                return false;
            return true;
        }

        void RefreshContinueButtonPulse()
        {
            if (ShouldPulseContinueButton())
                StartContinueButtonPulse();
            else
                StopContinueButtonPulse();
        }

        void StartContinueButtonPulse()
        {
            if (continueButton == null)
                return;
            if (_continueButtonPulse != null)
                return;
            _continueButtonPulse = StartCoroutine(PulseContinueButton());
        }

        void StopContinueButtonPulse()
        {
            if (_continueButtonPulse != null)
            {
                StopCoroutine(_continueButtonPulse);
                _continueButtonPulse = null;
            }

            if (continueButton != null)
                continueButton.transform.localScale = Vector3.one;
        }

        IEnumerator PulseContinueButton()
        {
            var t = continueButton != null ? continueButton.transform : null;
            float elapsed = 0f;
            while (t != null
                   && continueButton != null
                   && continueButton.activeInHierarchy
                   && ShouldPulseContinueButton())
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = (Mathf.Sin(elapsed * ContinueButtonPulseSpeed) + 1f) * 0.5f;
                float scale = Mathf.Lerp(ContinueButtonPulseMin, ContinueButtonPulseMax, wave);
                t.localScale = Vector3.one * scale;
                yield return null;
            }

            if (t != null)
                t.localScale = Vector3.one;
            _continueButtonPulse = null;
        }

        HomeVideoId CurrentDayHomeVideo() =>
            _atVacation ? HomeVideoId.VacationDay : HomeVideoId.NormalDay;

        void RefreshPhoneNotifications(bool phoneVisible)
        {
            bool normalDay = videoId == HomeVideoId.NormalDay;
            bool vacationDay = videoId == HomeVideoId.VacationDay;
            RefreshPhoneNotification(show: phoneVisible && normalDay && ShouldShowPhoneNotification());
            RefreshPhoneNotification2(
                show: phoneVisible
                    && (normalDay || vacationDay)
                    && HasUnwatchedAchievement());
        }

        void RefreshPhoneNotification(bool show)
        {
            if (!show)
            {
                HidePhoneNotification();
                HidePhoneWechat();
                return;
            }

            if (phoneNotificationButton == null)
                return;

            if (!phoneNotificationButton.activeSelf)
            {
                phoneNotificationButton.SetActive(true);
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayNotif();
            }
            StartPhoneNotificationPulse();
        }

        void RefreshPhoneNotification2(bool show)
        {
            if (!show)
            {
                HidePhoneNotification2();
                return;
            }

            if (phoneNotification2Button == null)
                return;

            if (!phoneNotification2Button.activeSelf)
            {
                phoneNotification2Button.SetActive(true);
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayNotif();
            }
            StartPhoneNotification2Pulse();
        }

        void StartPhoneNotificationPulse()
        {
            if (phoneNotificationButton == null)
                return;
            if (_phoneNotificationPulse != null)
                return;
            _phoneNotificationPulse = StartCoroutine(PulsePhoneNotification());
        }

        void StopPhoneNotificationPulse()
        {
            if (_phoneNotificationPulse != null)
            {
                StopCoroutine(_phoneNotificationPulse);
                _phoneNotificationPulse = null;
            }

            if (phoneNotificationButton != null)
                phoneNotificationButton.transform.localScale = Vector3.one;
        }

        IEnumerator PulsePhoneNotification()
        {
            var t = phoneNotificationButton != null ? phoneNotificationButton.transform : null;
            float elapsed = 0f;
            while (t != null && phoneNotificationButton.activeInHierarchy)
            {
                elapsed += Time.deltaTime;
                float wave = (Mathf.Sin(elapsed * PhoneNotificationPulseSpeed) + 1f) * 0.5f;
                float scale = Mathf.Lerp(PhoneNotificationPulseMin, PhoneNotificationPulseMax, wave);
                t.localScale = Vector3.one * scale;
                yield return null;
            }

            if (t != null)
                t.localScale = Vector3.one;
            _phoneNotificationPulse = null;
        }

        void StartPhoneNotification2Pulse()
        {
            if (phoneNotification2Button == null)
                return;
            if (_phoneNotification2Pulse != null)
                return;
            _phoneNotification2Pulse = StartCoroutine(PulsePhoneNotification2());
        }

        void StopPhoneNotification2Pulse()
        {
            if (_phoneNotification2Pulse != null)
            {
                StopCoroutine(_phoneNotification2Pulse);
                _phoneNotification2Pulse = null;
            }

            if (phoneNotification2Button != null)
                phoneNotification2Button.transform.localScale = Vector3.one;
        }

        IEnumerator PulsePhoneNotification2()
        {
            var t = phoneNotification2Button != null ? phoneNotification2Button.transform : null;
            float elapsed = 0f;
            while (t != null && phoneNotification2Button.activeInHierarchy)
            {
                elapsed += Time.deltaTime;
                float wave = (Mathf.Sin(elapsed * PhoneNotificationPulseSpeed) + 1f) * 0.5f;
                float scale = Mathf.Lerp(PhoneNotificationPulseMin, PhoneNotificationPulseMax, wave);
                t.localScale = Vector3.one * scale;
                yield return null;
            }

            if (t != null)
                t.localScale = Vector3.one;
            _phoneNotification2Pulse = null;
        }

        void StartMapTravelPulse()
        {
            if (mapTravelButton == null)
                return;
            if (_mapTravelPulse != null)
                return;
            _mapTravelPulse = StartCoroutine(PulseMapTravel());
        }

        void StopMapTravelPulse()
        {
            if (_mapTravelPulse != null)
            {
                StopCoroutine(_mapTravelPulse);
                _mapTravelPulse = null;
            }

            if (mapTravelButton != null)
                mapTravelButton.transform.localScale = Vector3.one;
        }

        IEnumerator PulseMapTravel()
        {
            var t = mapTravelButton != null ? mapTravelButton.transform : null;
            float elapsed = 0f;
            while (t != null && mapUi != null && mapUi.activeInHierarchy)
            {
                elapsed += Time.deltaTime;
                float wave = (Mathf.Sin(elapsed * MapTravelPulseSpeed) + 1f) * 0.5f;
                float scale = Mathf.Lerp(MapTravelPulseMin, MapTravelPulseMax, wave);
                t.localScale = Vector3.one * scale;
                yield return null;
            }

            if (t != null)
                t.localScale = Vector3.one;
            _mapTravelPulse = null;
        }

        void StartPhoneBubbleAgreePulse()
        {
            if (phoneBubbleAgreeButton == null)
                return;
            if (_phoneBubbleAgreePulse != null)
                return;
            _phoneBubbleAgreePulse = StartCoroutine(PulsePhoneBubbleAgree());
        }

        void StopPhoneBubbleAgreePulse()
        {
            if (_phoneBubbleAgreePulse != null)
            {
                StopCoroutine(_phoneBubbleAgreePulse);
                _phoneBubbleAgreePulse = null;
            }

            if (phoneBubbleAgreeButton != null)
                phoneBubbleAgreeButton.transform.localScale = Vector3.one;
        }

        IEnumerator PulsePhoneBubbleAgree()
        {
            var t = phoneBubbleAgreeButton != null ? phoneBubbleAgreeButton.transform : null;
            float elapsed = 0f;
            while (t != null && phoneWechatUi != null && phoneWechatUi.activeInHierarchy)
            {
                elapsed += Time.deltaTime;
                float wave = (Mathf.Sin(elapsed * PhoneBubbleAgreePulseSpeed) + 1f) * 0.5f;
                float scale = Mathf.Lerp(PhoneBubbleAgreePulseMin, PhoneBubbleAgreePulseMax, wave);
                t.localScale = Vector3.one * scale;
                yield return null;
            }

            if (t != null)
                t.localScale = Vector3.one;
            _phoneBubbleAgreePulse = null;
        }

        void StartAchievement1Pulse()
        {
            if (achievement1Button == null)
                return;
            if (_achievement1Pulse != null)
                return;
            _achievement1Pulse = StartCoroutine(PulseAchievement(
                achievement1Button, () => _achievement1Pulse = null));
        }

        void StopAchievement1Pulse()
        {
            if (_achievement1Pulse != null)
            {
                StopCoroutine(_achievement1Pulse);
                _achievement1Pulse = null;
            }

            if (achievement1Button != null)
                achievement1Button.transform.localScale = Vector3.one;
        }

        void StartAchievement2Pulse()
        {
            if (achievement2Button == null)
                return;
            if (_achievement2Pulse != null)
                return;
            _achievement2Pulse = StartCoroutine(PulseAchievement(
                achievement2Button, () => _achievement2Pulse = null));
        }

        void StopAchievement2Pulse()
        {
            if (_achievement2Pulse != null)
            {
                StopCoroutine(_achievement2Pulse);
                _achievement2Pulse = null;
            }

            if (achievement2Button != null)
                achievement2Button.transform.localScale = Vector3.one;
        }

        IEnumerator PulseAchievement(GameObject button, System.Action onEnded)
        {
            var t = button != null ? button.transform : null;
            float elapsed = 0f;
            while (t != null
                && button.activeInHierarchy
                && phonePhotosUi != null
                && phonePhotosUi.activeInHierarchy)
            {
                elapsed += Time.deltaTime;
                float wave = (Mathf.Sin(elapsed * AchievementPulseSpeed) + 1f) * 0.5f;
                float scale = Mathf.Lerp(AchievementPulseMin, AchievementPulseMax, wave);
                t.localScale = Vector3.one * scale;
                yield return null;
            }

            if (t != null)
                t.localScale = Vector3.one;
            onEnded?.Invoke();
        }

        static void SetGoActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }

        static bool IsStreet(HomeVideoId id) =>
            id == HomeVideoId.NormalStreet || id == HomeVideoId.VacationStreet;

        static bool IsMicro(HomeVideoId id) =>
            id == HomeVideoId.Micro1 || id == HomeVideoId.Micro2
            || id == HomeVideoId.Micro3 || id == HomeVideoId.Micro4
            || id == HomeVideoId.Micro5 || id == HomeVideoId.Micro6 || id == HomeVideoId.Micro7;

        static bool UsesNativeAspect(HomeVideoId id)
        {
            return IsStreet(id) || IsMicro(id);
        }

        void OnLoopPointReached(VideoPlayer source)
        {
            if (_entry == null)
                return;

            // After clearing the last street segment, loop once then pause at segment 0.
            if (_pauseAfterLoopIndex >= 0)
            {
                _pauseIndex = _pauseAfterLoopIndex;
                _pauseAfterLoopIndex = -1;
                BeginWatchingCurrentPause(needWrap: false);
                return;
            }

            // Travel sequence: Micro2 → Vacation Day
            if (videoId == HomeVideoId.Micro2)
            {
                Play(HomeVideoId.VacationDay);
                return;
            }

            // Landing intro → Normal Day
            if (videoId == HomeVideoId.Micro7)
            {
                PlayerProgress.MarkHomeIntroWatched();
                Play(HomeVideoId.NormalDay);
                return;
            }

            // Achievement micros → return to current day home (BGM resumes via ApplyForHomeVideo)
            if (videoId == HomeVideoId.Micro5 || videoId == HomeVideoId.Micro6)
            {
                MarkAchievementWatchedIfNeeded(videoId);
                // Hide immediately; day Play will re-evaluate HasUnwatchedAchievement.
                if (!HasUnwatchedAchievement())
                    HidePhoneNotification2();
                Play(CurrentDayHomeVideo());
                return;
            }

            // After Hotpot Match3: Micro1 / Micro4 → Normal Day
            if (videoId == HomeVideoId.Micro1 || videoId == HomeVideoId.Micro4)
            {
                Play(HomeVideoId.NormalDay);
                return;
            }

            // Micro3 end → Hotpot choice UI (not Street UI)
            if (videoId == HomeVideoId.Micro3)
            {
                if (source != null && source.isPlaying)
                    source.Pause();
                ShowHotpotUi();
                return;
            }

            if (_entry.loop)
                return;
            if (source != null && source.isPlaying)
                source.Pause();
        }

        void ShowHotpotUi()
        {
            SetStreetUiVisible(false);
            if (_entry != null)
            {
                if (hotpotLeftButtonImage != null)
                    hotpotLeftButtonImage.sprite = _entry.leftButtonSprite;
                if (hotpotRightButtonImage != null)
                    hotpotRightButtonImage.sprite = _entry.rightButtonSprite;
            }

            if (hotpotLeftButtonImage != null)
                hotpotLeftButtonImage.gameObject.SetActive(_entry != null && _entry.leftButtonSprite != null);
            if (hotpotRightButtonImage != null)
                hotpotRightButtonImage.gameObject.SetActive(_entry != null && _entry.rightButtonSprite != null);

            if (hotpotUi != null)
                hotpotUi.SetActive(true);
        }

        void HideHotpotUi()
        {
            if (hotpotUi != null)
                hotpotUi.SetActive(false);
        }

        void BuildPauseTimes()
        {
            _pauseTimes.Clear();
            if (_entry.segments == null)
                return;

            float length = ClipLength();
            for (int i = 0; i < _entry.segments.Length; i++)
            {
                float pause = _entry.segments[i].endSeconds;
                if (pause <= 0f)
                    continue;
                _pauseTimes.Add(Mathf.Min(pause, Mathf.Max(0.01f, length - 0.05f)));
            }
        }

        void PlayForwardToNextSegmentAfterClear(int clearedPauseIndex, float resumeTime)
        {
            if (videoPlayer == null || _pauseTimes.Count == 0)
                return;

            HidePauseUi();
            _pauseAfterLoopIndex = -1;

            float length = ClipLength();
            float start = Mathf.Clamp(resumeTime + 0.02f, 0f, Mathf.Max(0f, length - 0.05f));
            videoPlayer.time = start;

            if (clearedPauseIndex + 1 < _pauseTimes.Count)
            {
                _pauseIndex = clearedPauseIndex + 1;
                if (!videoPlayer.isPlaying)
                    videoPlayer.Play();
                ResumeTowardCurrentPause();
                return;
            }

            // Last segment (right level): play through the loop, then pause at segment 0.
            _pauseIndex = 0;
            _pauseAfterLoopIndex = 0;
            _needWrapBeforePause = false;
            _watchingPause = true;
            _lastTime = start;
            _armRealtime = Time.realtimeSinceStartup + 0.12f;
            if (!videoPlayer.isPlaying)
                videoPlayer.Play();
        }

        void ResumeTowardCurrentPause()
        {
            if (videoPlayer == null || _pauseTimes.Count == 0)
                return;

            float pauseAt = _pauseTimes[_pauseIndex];
            double time = videoPlayer.time;
            bool needWrap = pauseAt <= time + 0.05;

            if (!videoPlayer.isPlaying)
                videoPlayer.Play();

            BeginWatchingCurrentPause(needWrap);
        }

        void BeginWatchingCurrentPause(bool needWrap)
        {
            _needWrapBeforePause = needWrap;
            _lastTime = videoPlayer != null ? videoPlayer.time : 0;
            _armRealtime = Time.realtimeSinceStartup + 0.12f;
            _watchingPause = true;
        }

        void PauseForContinue()
        {
            _watchingPause = false;
            if (videoPlayer != null && videoPlayer.isPlaying)
                videoPlayer.Pause();
            ShowPauseUi(_pauseIndex);
        }

        float ClipLength()
        {
            if (videoPlayer.clip != null)
                return (float)videoPlayer.clip.length;
            return (float)videoPlayer.length;
        }

        void ApplyStreetButtonSprites()
        {
            if (leftButtonImage != null)
                leftButtonImage.sprite = _entry.leftButtonSprite;

            if (rightButtonImage != null)
                rightButtonImage.sprite = _entry.rightButtonSprite;
        }

        void ShowPauseUi(int pauseIndex)
        {
            _streetPauseUiVisible = true;
            SetStreetUiVisible(true);

            bool showLeft = (pauseIndex % 2) == 0;
            SetButtonVisible(leftButtonImage, showLeft && _entry != null && _entry.leftButtonSprite != null);
            SetButtonVisible(rightButtonImage, !showLeft && _entry != null && _entry.rightButtonSprite != null);

            if (showLeft)
            {
                RefreshButtonStars(StreetMatch3Slot.Left, leftStar1, leftStar2, leftStar3);
                SetPlayLevelButtonLabel(
                    leftPlayLevelButton,
                    PlayerProgress.HasPlayedLevel(videoId, StreetMatch3Slot.Left));
            }
            else
            {
                RefreshButtonStars(StreetMatch3Slot.Right, rightStar1, rightStar2, rightStar3);
                SetPlayLevelButtonLabel(
                    rightPlayLevelButton,
                    PlayerProgress.HasPlayedLevel(videoId, StreetMatch3Slot.Right));
            }

            if (continueButton != null)
                continueButton.SetActive(true);

            RefreshContinueButtonPulse();
        }

        void TryStartPendingAreaClear()
        {
            if (!_pendingAreaClear || !IsStreetVideo(videoId) || videoId != _pendingAreaClearStreet)
                return;
            if (!IsStreetFullyStarred(videoId))
                return;

            _pendingAreaClear = false;
            StartAreaClearPresentation();
        }

        void StartAreaClearPresentation()
        {
            if (areaClearUi == null)
                return;

            if (_areaClearRoutine != null)
            {
                StopCoroutine(_areaClearRoutine);
                _areaClearRoutine = null;
            }

            _areaClearShowing = true;
            SetStreetUiVisible(true);
            HideStreetPauseControls();
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayAreaClear();
            _areaClearRoutine = StartCoroutine(AreaClearRoutine());
        }

        IEnumerator AreaClearRoutine()
        {
            areaClearUi.SetActive(true);
            var cg = EnsureAreaClearCanvasGroup();
            cg.alpha = 1f;
            cg.blocksRaycasts = false;

            yield return new WaitForSeconds(AreaClearHoldSeconds);

            float t = 0f;
            while (t < AreaClearFadeSeconds)
            {
                t += Time.deltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / AreaClearFadeSeconds);
                yield return null;
            }

            _areaClearShowing = false;
            HideAreaClearUi();
            if (!_streetPauseUiVisible)
                SetStreetUiVisible(false);
            _areaClearRoutine = null;
        }

        CanvasGroup EnsureAreaClearCanvasGroup()
        {
            if (_areaClearCanvasGroup == null && areaClearUi != null)
            {
                _areaClearCanvasGroup = areaClearUi.GetComponent<CanvasGroup>();
                if (_areaClearCanvasGroup == null)
                    _areaClearCanvasGroup = areaClearUi.AddComponent<CanvasGroup>();
            }

            return _areaClearCanvasGroup;
        }

        void HideAreaClearUi()
        {
            if (_areaClearRoutine != null)
            {
                StopCoroutine(_areaClearRoutine);
                _areaClearRoutine = null;
            }

            _areaClearShowing = false;

            if (areaClearUi == null)
                return;

            var cg = EnsureAreaClearCanvasGroup();
            if (cg != null)
                cg.alpha = 0f;
            areaClearUi.SetActive(false);
        }

        static bool IsStreetVideo(HomeVideoId id) =>
            id == HomeVideoId.NormalStreet || id == HomeVideoId.VacationStreet;

        static bool IsStreetFullyStarred(HomeVideoId id) =>
            PlayerProgress.GetStars(id, StreetMatch3Slot.Left) >= 3
            && PlayerProgress.GetStars(id, StreetMatch3Slot.Right) >= 3;

        void RefreshPlayLevelButtonLabels()
        {
            SetPlayLevelButtonLabel(
                leftPlayLevelButton,
                PlayerProgress.HasPlayedLevel(videoId, StreetMatch3Slot.Left));
            SetPlayLevelButtonLabel(
                rightPlayLevelButton,
                PlayerProgress.HasPlayedLevel(videoId, StreetMatch3Slot.Right));
        }

        const string PlayLevelFirstLabel = "吃吧";
        const string PlayLevelReplayLabel = "再吃";

        static void SetPlayLevelButtonLabel(GameObject button, bool hasPlayed)
        {
            if (button == null)
                return;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;
            label.text = hasPlayed ? PlayLevelReplayLabel : PlayLevelFirstLabel;
        }

        void RefreshButtonStars(StreetMatch3Slot slot, Image s1, Image s2, Image s3)
        {
            int stars = PlayerProgress.GetStars(videoId, slot);
            Match3StarVisuals.Apply(s1, s2, s3, stars);
            StartStreetStarPulse(stars, s1, s2, s3);
        }

        void HidePauseUi()
        {
            _streetPauseUiVisible = false;
            StopStreetStarPulse();
            StopContinueButtonPulse();
            _pauseAfterLoopIndex = -1;
            HideStreetPauseControls();
            if (!_areaClearShowing)
                SetStreetUiVisible(false);
        }

        void HideStreetPauseControls()
        {
            SetButtonVisible(leftButtonImage, false);
            SetButtonVisible(rightButtonImage, false);
            if (continueButton != null)
                continueButton.SetActive(false);
        }

        static void SetButtonVisible(Image image, bool visible)
        {
            if (image == null)
                return;
            image.gameObject.SetActive(visible);
        }

        void StartStreetStarPulse(int earnedStars, Image s1, Image s2, Image s3)
        {
            StopStreetStarPulse();
            ResetStreetStarScales();
            if (earnedStars <= 0)
                return;
            _streetStarPulse = StartCoroutine(PulseEarnedStreetStars(earnedStars, s1, s2, s3));
        }

        void StopStreetStarPulse()
        {
            if (_streetStarPulse != null)
            {
                StopCoroutine(_streetStarPulse);
                _streetStarPulse = null;
            }

            ResetStreetStarScales();
        }

        void ResetStreetStarScales()
        {
            ResetStarScale(leftStar1);
            ResetStarScale(leftStar2);
            ResetStarScale(leftStar3);
            ResetStarScale(rightStar1);
            ResetStarScale(rightStar2);
            ResetStarScale(rightStar3);
        }

        static void ResetStarScale(Image star)
        {
            if (star != null)
                star.transform.localScale = Vector3.one;
        }

        IEnumerator PulseEarnedStreetStars(int earnedStars, Image s1, Image s2, Image s3)
        {
            var stars = new[] { s1, s2, s3 };
            int earned = Mathf.Clamp(earnedStars, 0, 3);
            float elapsed = 0f;
            while (elapsed < StreetStarPulseDuration)
            {
                elapsed += Time.deltaTime;
                float wave = (Mathf.Sin(elapsed * StreetStarPulseSpeed) + 1f) * 0.5f;
                float scale = Mathf.Lerp(StreetStarPulseMin, StreetStarPulseMax, wave);
                for (int i = 0; i < earned; i++)
                {
                    if (stars[i] != null)
                        stars[i].transform.localScale = Vector3.one * scale;
                }
                yield return null;
            }

            for (int i = 0; i < earned; i++)
                ResetStarScale(stars[i]);
            _streetStarPulse = null;
        }

        void SetStreetUiVisible(bool visible)
        {
            if (streetUi != null)
                streetUi.SetActive(visible);
        }

        void ResetPan()
        {
            if (videoRect != null)
                videoRect.anchoredPosition = new Vector2(0f, videoRect.anchoredPosition.y);
        }

        static bool HasSuzhouFanUnlocked() => PlayerProgress.IsSuzhouFanUnlocked();

        static bool HasFriendsPhotoUnlocked() => PlayerProgress.IsFriendsPhotoUnlocked();

        static bool HasMoneyPlantUnlocked() => PlayerProgress.IsMoneyPlantUnlocked();

        void RefreshPhotoFramesForCurrentVideo()
        {
            PlayerProgress.SyncPhotoCollectibleUnlocks();

            // Ornaments are Normal Day only — unlock only affects tint, never visibility.
            bool showOrnaments = videoId == HomeVideoId.NormalDay;
            SetPhotoFramesVisible(showOrnaments);

            if (!showOrnaments)
            {
                HidePhotoFramePopup();
                HideOrnamentPreviewDisplay();
                return;
            }

            TryShowPendingPhotoFramePopup();
        }

        void SetPhotoFramesVisible(bool visible)
        {
            if (!visible && _inspectingOrnament != null)
                SnapOrnamentInspectClosed();

            if (photoFramesRoot != null)
                photoFramesRoot.gameObject.SetActive(visible);

            // Always show all three on Normal Day; hide all otherwise. Unlock does not gate this.
            if (suzhouFanUi != null)
                suzhouFanUi.SetActive(visible);
            if (friendsPhotoUi != null)
                friendsPhotoUi.SetActive(visible);
            if (moneyPlantUi != null)
                moneyPlantUi.SetActive(visible);

            if (!visible)
                HidePhotoFrameGlows();
            else
                ApplyOrnamentUnlockVisuals();
        }

        void ApplyOrnamentUnlockVisuals()
        {
            SetOrnamentCollectedLook(suzhouFanUi, HasSuzhouFanUnlocked());
            SetOrnamentCollectedLook(friendsPhotoUi, HasFriendsPhotoUnlocked());
            SetOrnamentCollectedLook(moneyPlantUi, HasMoneyPlantUnlocked());
            SetGoActive(suzhouFanLockUi, !HasSuzhouFanUnlocked());
            SetGoActive(friendsPhotoLockUi, !HasFriendsPhotoUnlocked());
            SetGoActive(moneyPlantLockUi, !HasMoneyPlantUnlocked());
        }

        static void SetOrnamentCollectedLook(GameObject ornament, bool collected)
        {
            if (ornament == null)
                return;
            var image = ornament.GetComponent<Image>();
            if (image != null)
                image.color = collected ? OrnamentUnlockedTint : OrnamentLockedTint;
        }

        void TryShowPendingPhotoFramePopup()
        {
            if (_activePhotoFramePopup != PhotoFramePopupKind.None)
                return;

            if (HasSuzhouFanUnlocked() && !PlayerProgress.HasSeenSuzhouFanPopup())
            {
                ShowPhotoFramePopup(PhotoFramePopupKind.SuzhouFan);
                return;
            }

            if (HasFriendsPhotoUnlocked() && !PlayerProgress.HasSeenFriendsPhotoPopup())
            {
                ShowPhotoFramePopup(PhotoFramePopupKind.FriendsPhoto);
                return;
            }

            if (HasMoneyPlantUnlocked() && !PlayerProgress.HasSeenMoneyPlantPopup())
                ShowPhotoFramePopup(PhotoFramePopupKind.MoneyPlant);
        }

        void ShowPhotoFramePopup(PhotoFramePopupKind kind)
        {
            SnapOrnamentInspectClosed();
            _activePhotoFramePopup = kind;
            if (photoFramePopup != null)
                photoFramePopup.SetActive(true);

            GetOrnamentPopupCopy(OrnamentUiFor(kind), out var header, out var description, out var sprite, out _);
            if (photoFrameHeaderText != null)
                photoFrameHeaderText.text = header;
            if (photoFrameDescriptionText != null)
                photoFrameDescriptionText.text = description;
            if (photoFrameCollectionImage != null && sprite != null)
                photoFrameCollectionImage.sprite = sprite;
            ApplyPhotoFramePopupCollectionSize(kind);
            ApplyOrnamentPreviewCollectedLook(true);
        }

        void ApplyPhotoFramePopupCollectionSize(PhotoFramePopupKind kind)
        {
            if (photoFrameCollectionImage == null)
                return;
            photoFrameCollectionImage.rectTransform.sizeDelta =
                kind == PhotoFramePopupKind.MoneyPlant
                    ? MoneyPlantPhotoFramePopupCollectionSize
                    : PhotoFramePopupCollectionSize;
        }

        public void OnPhotoFrameOkPressed()
        {
            var dismissed = _activePhotoFramePopup;
            if (dismissed == PhotoFramePopupKind.SuzhouFan)
                PlayerProgress.MarkSuzhouFanPopupSeen();
            else if (dismissed == PhotoFramePopupKind.FriendsPhoto)
                PlayerProgress.MarkFriendsPhotoPopupSeen();
            else if (dismissed == PhotoFramePopupKind.MoneyPlant)
                PlayerProgress.MarkMoneyPlantPopupSeen();

            HidePhotoFramePopup();
            PlayPhotoFrameGlow(dismissed);
            ApplyOrnamentUnlockVisuals();
            TryShowPendingPhotoFramePopup();
        }

        void PlayPhotoFrameGlow(PhotoFramePopupKind kind)
        {
            if (kind == PhotoFramePopupKind.SuzhouFan)
            {
                if (_suzhouFanGlowRoutine != null)
                    StopCoroutine(_suzhouFanGlowRoutine);
                _suzhouFanGlowRoutine = StartCoroutine(ShowGlowTemporarily(suzhouFanGlowUi, kind));
            }
            else if (kind == PhotoFramePopupKind.FriendsPhoto)
            {
                if (_friendsPhotoGlowRoutine != null)
                    StopCoroutine(_friendsPhotoGlowRoutine);
                _friendsPhotoGlowRoutine = StartCoroutine(ShowGlowTemporarily(friendsPhotoGlowUi, kind));
            }
            else if (kind == PhotoFramePopupKind.MoneyPlant)
            {
                if (_moneyPlantGlowRoutine != null)
                    StopCoroutine(_moneyPlantGlowRoutine);
                _moneyPlantGlowRoutine = StartCoroutine(ShowGlowTemporarily(moneyPlantGlowUi, kind));
            }
        }

        IEnumerator ShowGlowTemporarily(GameObject glow, PhotoFramePopupKind kind)
        {
            SetGoActive(glow, true);
            yield return new WaitForSecondsRealtime(PhotoFrameGlowSeconds);
            SetGoActive(glow, false);
            if (kind == PhotoFramePopupKind.SuzhouFan)
                _suzhouFanGlowRoutine = null;
            else if (kind == PhotoFramePopupKind.FriendsPhoto)
                _friendsPhotoGlowRoutine = null;
            else if (kind == PhotoFramePopupKind.MoneyPlant)
                _moneyPlantGlowRoutine = null;
        }

        void HidePhotoFrameGlows()
        {
            if (_suzhouFanGlowRoutine != null)
            {
                StopCoroutine(_suzhouFanGlowRoutine);
                _suzhouFanGlowRoutine = null;
            }
            if (_friendsPhotoGlowRoutine != null)
            {
                StopCoroutine(_friendsPhotoGlowRoutine);
                _friendsPhotoGlowRoutine = null;
            }
            if (_moneyPlantGlowRoutine != null)
            {
                StopCoroutine(_moneyPlantGlowRoutine);
                _moneyPlantGlowRoutine = null;
            }
            SetGoActive(suzhouFanGlowUi, false);
            SetGoActive(friendsPhotoGlowUi, false);
            SetGoActive(moneyPlantGlowUi, false);
        }

        void HidePhotoFramePopup()
        {
            _activePhotoFramePopup = PhotoFramePopupKind.None;
            if (photoFramePopup != null)
                photoFramePopup.SetActive(false);
        }

        static void EnableGraphicRaycast(GameObject go)
        {
            if (go == null)
                return;
            var graphic = go.GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = true;
        }

        public void OnSuzhouFanOrnamentPressed()
        {
            if (videoId != HomeVideoId.NormalDay)
                return;
            ToggleOrnamentInspect(suzhouFanUi);
        }

        public void OnFriendsPhotoOrnamentPressed()
        {
            if (videoId != HomeVideoId.NormalDay)
                return;
            ToggleOrnamentInspect(friendsPhotoUi);
        }

        public void OnMoneyPlantOrnamentPressed()
        {
            if (videoId != HomeVideoId.NormalDay)
                return;
            ToggleOrnamentInspect(moneyPlantUi);
        }

        void OnOrnamentInspectOverlayPressed()
        {
            if (_ornamentInspected)
                CloseOrnamentInspect();
        }

        void ToggleOrnamentInspect(GameObject ornament)
        {
            if (ornament == null || !ornament.activeInHierarchy)
                return;
            if (_activePhotoFramePopup != PhotoFramePopupKind.None)
                return;

            var rt = ornament.transform as RectTransform;
            if (rt == null)
                return;

            if (_inspectingOrnament == rt && (_ornamentInspected || _ornamentInspectRoutine != null))
            {
                CloseOrnamentInspect();
                return;
            }

            OpenOrnamentInspect(rt);
        }

        void EnsureOrnamentInspectOverlay()
        {
            if (ornamentsPreviewUi != null)
                _ornamentInspectOverlay = ornamentsPreviewUi;

            if (_ornamentInspectOverlay == null)
                return;

            _ornamentInspectDim = _ornamentInspectOverlay.GetComponent<Image>();

            var button = _ornamentInspectOverlay.GetComponent<Button>();
            if (button == null)
                button = _ornamentInspectOverlay.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            if (_ornamentInspectDim != null)
                button.targetGraphic = _ornamentInspectDim;
            button.onClick.RemoveListener(OnOrnamentInspectOverlayPressed);
            button.onClick.AddListener(OnOrnamentInspectOverlayPressed);

            if (ornamentsPreviewPopup != null)
            {
                _ornamentsPreviewCanvasGroup = ornamentsPreviewPopup.GetComponent<CanvasGroup>();
                if (_ornamentsPreviewCanvasGroup == null)
                    _ornamentsPreviewCanvasGroup = ornamentsPreviewPopup.AddComponent<CanvasGroup>();
            }

            HideOrnamentPreviewDisplay();
            if (_ornamentInspectDim != null)
            {
                var dimColor = _ornamentInspectDim.color;
                dimColor.a = 0f;
                _ornamentInspectDim.color = dimColor;
            }
            _ornamentInspectOverlay.SetActive(false);
        }

        void ApplyOrnamentPreviewContent(GameObject ornament)
        {
            GetOrnamentPopupCopy(ornament, out var header, out var description, out var sprite, out var collected);
            if (ornamentsPreviewHeaderText != null)
                ornamentsPreviewHeaderText.text = collected ? header : OrnamentLockedHeader;
            if (ornamentsPreviewDescriptionText != null)
                ornamentsPreviewDescriptionText.text = description;
            if (ornamentsPreviewCollectionImage != null && sprite != null)
                ornamentsPreviewCollectionImage.sprite = sprite;
            ApplyOrnamentPreviewCollectionSize(ornament);
            ApplyOrnamentPreviewCollectedLook(collected);
        }

        void ApplyOrnamentPreviewCollectionSize(GameObject ornament)
        {
            if (ornamentsPreviewCollectionImage == null)
                return;
            ornamentsPreviewCollectionImage.rectTransform.sizeDelta =
                ornament == moneyPlantUi
                    ? MoneyPlantPreviewCollectionSize
                    : OrnamentPreviewCollectionSize;
        }

        GameObject OrnamentUiFor(PhotoFramePopupKind kind)
        {
            if (kind == PhotoFramePopupKind.SuzhouFan)
                return suzhouFanUi;
            if (kind == PhotoFramePopupKind.FriendsPhoto)
                return friendsPhotoUi;
            if (kind == PhotoFramePopupKind.MoneyPlant)
                return moneyPlantUi;
            return null;
        }

        void GetOrnamentPopupCopy(
            GameObject ornament,
            out string header,
            out string description,
            out Sprite sprite,
            out bool collected)
        {
            header = string.Empty;
            description = string.Empty;
            sprite = null;
            collected = false;

            if (ornament == suzhouFanUi)
            {
                header = SuzhouFanPopupHeader;
                description = SuzhouFanPopupDescription;
                sprite = suzhouFanPopupSprite;
                collected = HasSuzhouFanUnlocked();
            }
            else if (ornament == friendsPhotoUi)
            {
                header = FriendsPhotoPopupHeader;
                description = FriendsPhotoPopupDescription;
                sprite = friendsPhotoPopupSprite;
                collected = HasFriendsPhotoUnlocked();
            }
            else if (ornament == moneyPlantUi)
            {
                header = MoneyPlantPopupHeader;
                description = MoneyPlantPopupDescription;
                var image = moneyPlantUi != null ? moneyPlantUi.GetComponent<Image>() : null;
                sprite = image != null ? image.sprite : null;
                collected = HasMoneyPlantUnlocked();
            }
        }

        void ApplyOrnamentPreviewCollectedLook(bool collected)
        {
            SetGoActive(ornamentsPreviewLockIcon, !collected);

            if (ornamentsPreviewCollectionImage != null)
                ornamentsPreviewCollectionImage.color = collected ? OrnamentUnlockedTint : OrnamentLockedTint;
            if (ornamentsPreviewDescriptionText != null)
                ornamentsPreviewDescriptionText.color = collected
                    ? OrnamentUnlockedDescriptionColor
                    : OrnamentLockedDescriptionColor;
            if (photoFrameCollectionImage != null)
                photoFrameCollectionImage.color = collected ? OrnamentUnlockedTint : OrnamentLockedTint;
            if (photoFrameDescriptionText != null)
                photoFrameDescriptionText.color = collected
                    ? OrnamentUnlockedDescriptionColor
                    : OrnamentLockedDescriptionColor;
        }

        void SetOrnamentPreviewDisplayVisible(bool visible)
        {
            _ornamentPreviewShown = visible;
            if (_ornamentsPreviewCanvasGroup == null)
                return;
            _ornamentsPreviewCanvasGroup.alpha = visible ? 1f : 0f;
            _ornamentsPreviewCanvasGroup.blocksRaycasts = visible;
            _ornamentsPreviewCanvasGroup.interactable = visible;
        }

        void HideOrnamentPreviewDisplay() => SetOrnamentPreviewDisplayVisible(false);

        void SetOrnamentShown(RectTransform rt, bool shown)
        {
            if (rt == null)
                return;
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = shown ? 1f : 0f;
            cg.blocksRaycasts = shown;
            cg.interactable = shown;
        }

        bool TryGetCollectionTarget(RectTransform ornamentRt, out Vector2 localPos, out Vector3 scale)
        {
            localPos = Vector2.zero;
            scale = _ornamentRestScale;
            var overlayRt = _ornamentInspectOverlay != null
                ? _ornamentInspectOverlay.transform as RectTransform
                : null;
            var collectionRt = ornamentsPreviewCollectionImage != null
                ? ornamentsPreviewCollectionImage.rectTransform
                : null;
            if (overlayRt == null || collectionRt == null || ornamentRt == null)
                return false;

            Canvas.ForceUpdateCanvases();
            Camera cam = OverlayCamera();
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, collectionRt.position);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRt, screen, cam, out localPos))
                localPos = Vector2.zero;

            float ornamentW = Mathf.Max(1f, ornamentRt.rect.width);
            float collectionW = Mathf.Max(1f, collectionRt.rect.width);
            scale = _ornamentRestScale * (collectionW / ornamentW);
            return true;
        }

        void OpenOrnamentInspect(RectTransform rt)
        {
            EnsureOrnamentInspectOverlay();
            if (_ornamentInspectOverlay == null)
                return;

            if (_inspectingOrnament != null && _inspectingOrnament != rt)
                SnapOrnamentInspectClosed();

            StopOrnamentInspectRoutine();

            _inspectingOrnament = rt;
            _ornamentRestParent = rt.parent;
            _ornamentRestSiblingIndex = rt.GetSiblingIndex();
            _ornamentRestAnchoredPosition = rt.anchoredPosition;
            _ornamentRestScale = rt.localScale;
            _ornamentRestWorldPosition = rt.position;
            _ornamentInspected = true;
            _ornamentPreviewShown = false;

            ApplyOrnamentPreviewContent(rt.gameObject);
            HideOrnamentPreviewDisplay();
            SetOrnamentShown(rt, true);

            _ornamentInspectOverlay.SetActive(true);
            _ornamentInspectOverlay.transform.SetAsLastSibling();
            if (_ornamentInspectDim != null)
            {
                var dimColor = _ornamentInspectDim.color;
                dimColor.a = 0f;
                _ornamentInspectDim.color = dimColor;
            }
            rt.SetParent(_ornamentInspectOverlay.transform, true);

            if (!TryGetCollectionTarget(rt, out var targetPos, out var targetScale))
            {
                targetPos = Vector2.zero;
                targetScale = _ornamentRestScale * 3f;
            }
            _ornamentInspectZoomScale = targetScale;

            _ornamentInspectRoutine = StartCoroutine(AnimateOrnamentInspect(
                rt, targetPos, targetScale, OrnamentInspectDimAlpha, ShowOrnamentPreviewAfterFlyIn));
        }

        void ShowOrnamentPreviewAfterFlyIn()
        {
            if (_inspectingOrnament == null)
                return;

            SetOrnamentShown(_inspectingOrnament, false);
            RestoreInspectedOrnamentToWall(keepOverlay: true);
            SetOrnamentPreviewDisplayVisible(true);
        }

        void CloseOrnamentInspect()
        {
            if (_inspectingOrnament == null)
                return;

            StopOrnamentInspectRoutine();
            bool fromPreview = _ornamentPreviewShown;
            _ornamentInspected = false;

            var rt = _inspectingOrnament;
            HideOrnamentPreviewDisplay();

            if (fromPreview)
            {
                SetOrnamentShown(rt, false);
                if (_ornamentInspectOverlay != null && rt.parent != _ornamentInspectOverlay.transform)
                    rt.SetParent(_ornamentInspectOverlay.transform, false);

                if (!TryGetCollectionTarget(rt, out var fromPos, out var fromScale))
                {
                    fromPos = Vector2.zero;
                    fromScale = _ornamentInspectZoomScale.sqrMagnitude > 0f
                        ? _ornamentInspectZoomScale
                        : _ornamentRestScale;
                }
                rt.anchoredPosition = fromPos;
                rt.localScale = fromScale;
            }

            SetOrnamentShown(rt, true);

            var overlayRt = _ornamentInspectOverlay != null
                ? _ornamentInspectOverlay.transform as RectTransform
                : null;
            Vector2 targetPos = rt.anchoredPosition;
            if (overlayRt != null)
            {
                Camera cam = OverlayCamera();
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, _ornamentRestWorldPosition);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRt, screen, cam, out targetPos))
                    targetPos = rt.anchoredPosition;
            }

            _ornamentInspectRoutine = StartCoroutine(AnimateOrnamentInspect(
                rt, targetPos, _ornamentRestScale, 0f, RestoreInspectedOrnament));
        }

        void SnapOrnamentInspectClosed()
        {
            StopOrnamentInspectRoutine();
            RestoreInspectedOrnament();
        }

        void RestoreInspectedOrnamentToWall(bool keepOverlay)
        {
            var rt = _inspectingOrnament;
            if (rt != null && _ornamentRestParent != null)
            {
                rt.SetParent(_ornamentRestParent, false);
                rt.SetSiblingIndex(_ornamentRestSiblingIndex);
                rt.anchoredPosition = _ornamentRestAnchoredPosition;
                rt.localScale = _ornamentRestScale;
            }

            if (!keepOverlay)
                SetOrnamentShown(rt, true);
        }

        void RestoreInspectedOrnament()
        {
            RestoreInspectedOrnamentToWall(keepOverlay: false);
            HideOrnamentPreviewDisplay();

            _inspectingOrnament = null;
            _ornamentRestParent = null;
            _ornamentInspected = false;
            _ornamentPreviewShown = false;

            if (_ornamentInspectDim != null)
            {
                var c = _ornamentInspectDim.color;
                c.a = 0f;
                _ornamentInspectDim.color = c;
            }

            if (_ornamentInspectOverlay != null)
                _ornamentInspectOverlay.SetActive(false);
        }

        IEnumerator AnimateOrnamentInspect(
            RectTransform rt, Vector2 toPos, Vector3 toScale, float toDimAlpha, System.Action onDone)
        {
            Vector2 fromPos = rt.anchoredPosition;
            Vector3 fromScale = rt.localScale;
            float fromDim = _ornamentInspectDim != null ? _ornamentInspectDim.color.a : 0f;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / OrnamentInspectDuration;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                rt.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, u);
                rt.localScale = Vector3.LerpUnclamped(fromScale, toScale, u);
                if (_ornamentInspectDim != null)
                {
                    var c = _ornamentInspectDim.color;
                    c.a = Mathf.Lerp(fromDim, toDimAlpha, u);
                    _ornamentInspectDim.color = c;
                }
                yield return null;
            }

            rt.anchoredPosition = toPos;
            rt.localScale = toScale;
            if (_ornamentInspectDim != null)
            {
                var c = _ornamentInspectDim.color;
                c.a = toDimAlpha;
                _ornamentInspectDim.color = c;
            }

            _ornamentInspectRoutine = null;
            onDone?.Invoke();
        }

        void StopOrnamentInspectRoutine()
        {
            if (_ornamentInspectRoutine == null)
                return;
            StopCoroutine(_ornamentInspectRoutine);
            _ornamentInspectRoutine = null;
        }

        Camera OverlayCamera()
        {
            var canvas = _ornamentInspectOverlay != null
                ? _ornamentInspectOverlay.GetComponentInParent<Canvas>()
                : GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;
            return canvas.worldCamera;
        }

        void StopLoad()
        {
            if (_loadRoutine != null)
            {
                StopCoroutine(_loadRoutine);
                _loadRoutine = null;
            }
        }
    }
}
