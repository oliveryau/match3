using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Match3
{
    /// <summary>
    /// Normal: play once, no drag / street UI.
    /// HorizontalDrag: pan while looping.
    /// Segmented: loop with pause points; at each pause show Continue + Left or Right (alternating).
    /// Looping is driven by HomeVideoEntry.loop, not the VideoPlayer inspector.
    /// </summary>
    public class HomeVideoDirector : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] HomeVideoCatalog catalog;
        [SerializeField] VideoPlayer videoPlayer;
        [SerializeField] RectTransform videoRect;
        [SerializeField] GameObject streetUi;
        [SerializeField] GameObject continueButton;
        [SerializeField] Image leftButtonImage;
        [SerializeField] Image rightButtonImage;
        [SerializeField] Image leftStar1;
        [SerializeField] Image leftStar2;
        [SerializeField] Image leftStar3;
        [SerializeField] Image rightStar1;
        [SerializeField] Image rightStar2;
        [SerializeField] Image rightStar3;
        [SerializeField] HomeVideoId videoId = HomeVideoId.NormalDay;

        HomeVideoEntry _entry;
        readonly List<float> _pauseTimes = new List<float>();
        int _pauseIndex;
        bool _watchingPause;
        bool _needWrapBeforePause;
        double _lastTime;
        float _armRealtime;
        bool _dragEnabled;
        Coroutine _loadRoutine;

        void Awake()
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
            if (videoRect == null)
                videoRect = transform as RectTransform;

            if (continueButton != null)
            {
                var button = continueButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveListener(OnContinuePressed);
                    button.onClick.AddListener(OnContinuePressed);
                }
            }

            WireStreetButton(leftButtonImage, OnLeftLevelPressed);
            WireStreetButton(rightButtonImage, OnRightLevelPressed);
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

        void OnEnable()
        {
            if (videoPlayer != null)
                videoPlayer.loopPointReached += OnLoopPointReached;
            Play(videoId);
        }

        void OnDisable()
        {
            StopLoad();
            _watchingPause = false;
            _dragEnabled = false;
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnLoopPointReached;
                if (videoPlayer.isPlaying)
                    videoPlayer.Pause();
            }
            HidePauseUi();
            SetStreetUiVisible(false);
        }

        void Update()
        {
#if UNITY_EDITOR
            if (TryEditorSwitchVideo())
                return;
#endif
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
            StopLoad();
            _loadRoutine = StartCoroutine(LoadAndStart());
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

        void EnterMatch3(StreetMatch3Slot slot)
        {
            if (_entry == null || _entry.mode != HomeVideoPlaybackMode.Segmented)
                return;
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("HomeVideoDirector: GameManager missing; cannot open Match3.");
                return;
            }

            GameManager.Instance.LoadMatch3FromStreet(videoId, slot);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragEnabled || videoRect == null || eventData == null)
                return;

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

        IEnumerator LoadAndStart()
        {
            _watchingPause = false;
            _dragEnabled = false;
            HidePauseUi();
            SetStreetUiVisible(false);
            ResetPan();

            if (catalog == null || videoPlayer == null)
                yield break;

            _entry = catalog.GetEntry(videoId);
            if (_entry == null || _entry.clip == null)
                yield break;

            videoPlayer.Stop();
            videoPlayer.playOnAwake = false;
            videoPlayer.clip = _entry.clip;
            videoPlayer.isLooping = _entry.loop;
            videoPlayer.Prepare();
            while (videoPlayer != null && !videoPlayer.isPrepared)
                yield return null;

            if (!isActiveAndEnabled || videoPlayer == null)
                yield break;

            switch (_entry.mode)
            {
                case HomeVideoPlaybackMode.HorizontalDrag:
                    _dragEnabled = true;
                    videoPlayer.Play();
                    yield break;

                case HomeVideoPlaybackMode.Normal:
                    videoPlayer.Play();
                    yield break;

                case HomeVideoPlaybackMode.Segmented:
                    ApplyStreetButtonSprites();
                    BuildPauseTimes();
                    if (_pauseTimes.Count == 0)
                    {
                        videoPlayer.Play();
                        yield break;
                    }

                    _pauseIndex = 0;
                    videoPlayer.time = 0;
                    videoPlayer.Play();
                    BeginWatchingCurrentPause(needWrap: false);
                    yield break;
            }
        }

        void OnLoopPointReached(VideoPlayer source)
        {
            if (_entry == null || _entry.loop)
                return;
            if (source != null && source.isPlaying)
                source.Pause();
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
