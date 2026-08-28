using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [Header("Phone Notification (3+ stars on Normal Day)")]
        [SerializeField] GameObject phoneNotificationButton;
        [SerializeField] GameObject phoneBubbleUi;
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
        [Header("Photo Frame Popup")]
        [SerializeField] GameObject photoFramePopup;
        [SerializeField] TMP_Text photoFrameHeaderText;
        [SerializeField] Image photoFrameCollectionImage;
        [SerializeField] GameObject photoFrameOkButton;
        [SerializeField] Sprite suzhouFanPopupSprite;
        [SerializeField] Sprite friendsPhotoPopupSprite;
        [Header("Drag Indicators (Normal / Vacation Day)")]
        [SerializeField] GameObject dragIndicatorsUi;
        [SerializeField] HomeVideoId videoId = HomeVideoId.NormalDay;

        const string SuzhouFanPopupHeader = "获得苏州纪念品";
        const string FriendsPhotoPopupHeader = "获得与朋友\n的火锅回忆";
        const float DragIndicatorIdleSeconds = 3f;

        HomeVideoEntry _entry;
        readonly List<float> _pauseTimes = new List<float>();
        int _pauseIndex;
        bool _watchingPause;
        bool _needWrapBeforePause;
        double _lastTime;
        float _armRealtime;
        bool _dragEnabled;
        float _dragIdleElapsed;
        Coroutine _loadRoutine;
        Coroutine _phoneNotificationPulse;
        Coroutine _mapTravelPulse;
        bool _prepareFailed;
        string _lastError;
        /// <summary>False = normal home, true = vacation home.</summary>
        bool _atVacation;
        Color _travelImageColor = Color.white;
        enum PhotoFramePopupKind { None, SuzhouFan, FriendsPhoto }
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
                    button.onClick.RemoveListener(OnContinuePressed);
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
            WireMainButton(mapCloseButton, OnMapClosePressed);
            WireMainButton(mapTravelButton, OnMapTravelPressed);
            WireMainButton(phoneNotificationButton, OnPhoneNotificationPressed);
            WireMainButton(phoneBubbleAgreeButton, OnPhoneBubbleAgreePressed);
            WireMainButton(phoneBubbleCloseButton, OnPhoneBubbleClosePressed);
            WireMainButton(photoFrameOkButton, OnPhotoFrameOkPressed);

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
            if (phoneBubbleUi != null)
                phoneBubbleUi.SetActive(false);
            if (phoneNotificationButton != null)
                phoneNotificationButton.SetActive(false);
            if (travelLockedUi != null)
                travelLockedUi.SetActive(false);
            HidePhotoFramePopup();
            SetPhotoFramesVisible(false, false);
            HideDragIndicators();
        }

        static void WireStreetButton(Image image, UnityEngine.Events.UnityAction handler)
        {
            if (image == null)
                return;
            var button = image.GetComponent<Button>();
            if (button == null)
                return;
            button.onClick.RemoveListener(handler);
            button.onClick.AddListener(handler);
        }

        static void WireMainButton(GameObject go, UnityEngine.Events.UnityAction handler)
        {
            if (go == null)
                return;
            var button = go.GetComponent<Button>();
            if (button == null)
                return;
            button.onClick.RemoveListener(handler);
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
            var startId = HomeVideoId.NormalDay;
            if (GameManager.Instance != null &&
                GameManager.Instance.TryConsumeHomeResume(out var resumeId))
                startId = resumeId;

            Play(startId);
        }

        void OnDisable()
        {
            StopLoad();
            _watchingPause = false;
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
            HidePhoneBubble();
            HidePhoneNotification();
            HidePhotoFramePopup();
            SetPhotoFramesVisible(false, false);
            HideDragIndicators();
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
            UpdateDragIndicators();

            if (!_watchingPause || videoPlayer == null || !videoPlayer.isPrepared)
                return;
            if (Time.realtimeSinceStartup < _armRealtime)
                return;

            double time = videoPlayer.time;

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
            // While the notification is up, Phone opens the bubble instead of photos.
            if (phoneNotificationButton != null && phoneNotificationButton.activeSelf)
            {
                if (phoneBubbleUi != null)
                    phoneBubbleUi.SetActive(true);
                return;
            }

            if (phonePhotosUi != null)
                phonePhotosUi.SetActive(true);
            RefreshPhoneFoods();
        }

        public void OnPhonePhotosClosePressed()
        {
            if (phonePhotosUi != null)
                phonePhotosUi.SetActive(false);
        }

        void RefreshPhoneFoods()
        {
            SetPhoneFoodActive(phoneFood1, HomeVideoId.NormalStreet, StreetMatch3Slot.Left);
            SetPhoneFoodActive(phoneFood2, HomeVideoId.NormalStreet, StreetMatch3Slot.Right);
            SetPhoneFoodActive(phoneFood3, HomeVideoId.VacationStreet, StreetMatch3Slot.Left);
            SetPhoneFoodActive(phoneFood4, HomeVideoId.VacationStreet, StreetMatch3Slot.Right);
            // Hotpot: either Micro3 level at 3★ unlocks Food (5).
            SetPhoneFoodActive(phoneFood5, HasAnyMicro3ThreeStarClear());
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

            HidePhonePhotos();
            HideMapUi();
            HidePhoneBubble();
            Play(_atVacation || videoId == HomeVideoId.VacationDay
                ? HomeVideoId.VacationStreet
                : HomeVideoId.NormalStreet);
        }

        public void OnTravelPressed()
        {
            // Normal Day + unlocked: open map first; Micro2 starts from map Travel.
            if (videoId != HomeVideoId.NormalDay || !HasTravelUnlocked())
                return;

            HidePhonePhotos();
            HidePhoneBubble();
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
            HidePhoneBubble();
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
            HidePhoneBubble();

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
            HidePhoneBubble();
            Play(HomeVideoId.VacationDay);
        }

        public void OnPhoneNotificationPressed()
        {
            if (videoId != HomeVideoId.NormalDay || !HasTravelUnlocked())
                return;
            if (phoneBubbleUi != null)
                phoneBubbleUi.SetActive(true);
        }

        public void OnPhoneBubbleAgreePressed()
        {
            HidePhoneBubble();
            HidePhoneNotification();
            HidePhonePhotos();
            HideMapUi();
            Play(HomeVideoId.Micro3);
        }

        public void OnPhoneBubbleClosePressed()
        {
            HidePhoneBubble();
        }

        void HidePhonePhotos()
        {
            if (phonePhotosUi != null)
                phonePhotosUi.SetActive(false);
        }

        void HidePhoneBubble()
        {
            if (phoneBubbleUi != null)
                phoneBubbleUi.SetActive(false);
        }

        void HidePhoneNotification()
        {
            StopPhoneNotificationPulse();
            if (phoneNotificationButton != null)
                phoneNotificationButton.SetActive(false);
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

        void LoadMatch3(StreetMatch3Slot slot)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("HomeVideoDirector: GameManager missing; cannot open Match3.");
                return;
            }

            GameManager.Instance.LoadMatch3FromStreet(videoId, slot);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            NotifyPlayerDragged();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragEnabled || videoRect == null || eventData == null)
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

        void UpdateDragIndicators()
        {
            if (!_dragEnabled || !IsDayHomeVideo(videoId))
            {
                _dragIdleElapsed = 0f;
                HideDragIndicators();
                return;
            }

            _dragIdleElapsed += Time.unscaledDeltaTime;
            if (_dragIdleElapsed >= DragIndicatorIdleSeconds)
                SetGoActive(dragIndicatorsUi, true);
        }

        void NotifyPlayerDragged()
        {
            if (!_dragEnabled || !IsDayHomeVideo(videoId))
                return;

            _dragIdleElapsed = 0f;
            HideDragIndicators();
        }

        void HideDragIndicators() => SetGoActive(dragIndicatorsUi, false);

        static bool IsDayHomeVideo(HomeVideoId id) =>
            id == HomeVideoId.NormalDay || id == HomeVideoId.VacationDay;

        IEnumerator LoadAndStart()
        {
            _watchingPause = false;
            _dragEnabled = false;
            _dragIdleElapsed = 0f;
            _prepareFailed = false;
            _lastError = null;
            HidePauseUi();
            SetStreetUiVisible(false);
            HideHotpotUi();
            HideMapUi();
            HideDragIndicators();
            ResetPan();
            RefreshMainButtons();
            RefreshPhotoFramesForCurrentVideo();

            if (catalog == null || videoPlayer == null)
            {
                Debug.LogError("HomeVideoDirector: catalog or VideoPlayer is missing.");
                yield break;
            }

            _entry = catalog.GetEntry(videoId);
            if (_entry == null || _entry.clip == null)
            {
                Debug.LogError($"HomeVideoDirector: no clip for {videoId}.");
                yield break;
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

            if (micro)
            {
                SetGoActive(phoneButton, false);
                SetGoActive(streetButton, false);
                SetGoActive(travelButton, false);
                SetGoActive(homeButton, false);
                SetGoActive(roomButton, false);
                RefreshTravelLockState(travelVisible: false);
                RefreshPhoneNotification(show: false);
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
                RefreshPhoneNotification(show: false);
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
                RefreshPhoneNotification(show: false);
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
                RefreshPhoneNotification(show: false);
                return;
            }

            // Normal Day / other: phone + street + travel; home/room hidden
            SetGoActive(phoneButton, true);
            SetGoActive(streetButton, true);
            SetGoActive(travelButton, true);
            SetGoActive(homeButton, false);
            SetGoActive(roomButton, false);
            RefreshTravelLockState(travelVisible: true);
            RefreshPhoneNotification(show: normalDay && ShouldShowPhoneNotification());
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

        void RefreshPhoneNotification(bool show)
        {
            if (!show)
            {
                HidePhoneNotification();
                HidePhoneBubble();
                return;
            }

            if (phoneNotificationButton == null)
                return;

            if (!phoneNotificationButton.activeSelf)
                phoneNotificationButton.SetActive(true);
            StartPhoneNotificationPulse();
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

        static void SetGoActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }

        static bool IsStreet(HomeVideoId id) =>
            id == HomeVideoId.NormalStreet || id == HomeVideoId.VacationStreet;

        static bool IsMicro(HomeVideoId id) =>
            id == HomeVideoId.Micro1 || id == HomeVideoId.Micro2
            || id == HomeVideoId.Micro3 || id == HomeVideoId.Micro4;

        static bool UsesNativeAspect(HomeVideoId id)
        {
            return IsStreet(id) || IsMicro(id);
        }

        void OnLoopPointReached(VideoPlayer source)
        {
            if (_entry == null)
                return;

            // Travel sequence: Micro2 → Vacation Day
            if (videoId == HomeVideoId.Micro2)
            {
                Play(HomeVideoId.VacationDay);
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
            SetStreetUiVisible(true);

            bool showLeft = (pauseIndex % 2) == 0;
            SetButtonVisible(leftButtonImage, showLeft && _entry != null && _entry.leftButtonSprite != null);
            SetButtonVisible(rightButtonImage, !showLeft && _entry != null && _entry.rightButtonSprite != null);

            if (showLeft)
                RefreshButtonStars(StreetMatch3Slot.Left, leftStar1, leftStar2, leftStar3);
            else
                RefreshButtonStars(StreetMatch3Slot.Right, rightStar1, rightStar2, rightStar3);

            if (continueButton != null)
                continueButton.SetActive(true);
        }

        void RefreshButtonStars(StreetMatch3Slot slot, Image s1, Image s2, Image s3)
        {
            int stars = PlayerProgress.GetStars(videoId, slot);
            Match3StarVisuals.Apply(s1, s2, s3, stars);
        }

        void HidePauseUi()
        {
            SetButtonVisible(leftButtonImage, false);
            SetButtonVisible(rightButtonImage, false);
            if (continueButton != null)
                continueButton.SetActive(false);
            SetStreetUiVisible(false);
        }

        static void SetButtonVisible(Image image, bool visible)
        {
            if (image == null)
                return;
            image.gameObject.SetActive(visible);
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

        void RefreshPhotoFramesForCurrentVideo()
        {
            PlayerProgress.SyncPhotoCollectibleUnlocks();

            bool normalDay = videoId == HomeVideoId.NormalDay;
            bool showSuzhou = normalDay && HasSuzhouFanUnlocked();
            bool showFriends = normalDay && HasFriendsPhotoUnlocked();
            SetPhotoFramesVisible(showSuzhou, showFriends);

            if (!normalDay)
            {
                HidePhotoFramePopup();
                return;
            }

            TryShowPendingPhotoFramePopup();
        }

        void SetPhotoFramesVisible(bool showSuzhou, bool showFriends)
        {
            bool any = showSuzhou || showFriends;
            if (photoFramesRoot != null)
            {
                if (photoFramesRoot.gameObject.activeSelf != any)
                    photoFramesRoot.gameObject.SetActive(any);
            }

            SetGoActive(suzhouFanUi, showSuzhou);
            SetGoActive(friendsPhotoUi, showFriends);
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
                ShowPhotoFramePopup(PhotoFramePopupKind.FriendsPhoto);
        }

        void ShowPhotoFramePopup(PhotoFramePopupKind kind)
        {
            _activePhotoFramePopup = kind;
            if (photoFramePopup != null)
                photoFramePopup.SetActive(true);

            if (kind == PhotoFramePopupKind.SuzhouFan)
            {
                if (photoFrameHeaderText != null)
                    photoFrameHeaderText.text = SuzhouFanPopupHeader;
                if (photoFrameCollectionImage != null && suzhouFanPopupSprite != null)
                    photoFrameCollectionImage.sprite = suzhouFanPopupSprite;
            }
            else if (kind == PhotoFramePopupKind.FriendsPhoto)
            {
                if (photoFrameHeaderText != null)
                    photoFrameHeaderText.text = FriendsPhotoPopupHeader;
                if (photoFrameCollectionImage != null && friendsPhotoPopupSprite != null)
                    photoFrameCollectionImage.sprite = friendsPhotoPopupSprite;
            }
        }

        public void OnPhotoFrameOkPressed()
        {
            if (_activePhotoFramePopup == PhotoFramePopupKind.SuzhouFan)
                PlayerProgress.MarkSuzhouFanPopupSeen();
            else if (_activePhotoFramePopup == PhotoFramePopupKind.FriendsPhoto)
                PlayerProgress.MarkFriendsPhotoPopupSeen();

            HidePhotoFramePopup();
            TryShowPendingPhotoFramePopup();
        }

        void HidePhotoFramePopup()
        {
            _activePhotoFramePopup = PhotoFramePopupKind.None;
            if (photoFramePopup != null)
                photoFramePopup.SetActive(false);
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
