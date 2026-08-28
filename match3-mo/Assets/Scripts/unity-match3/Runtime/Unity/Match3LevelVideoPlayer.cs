using System;
using UnityEngine;
using UnityEngine.Video;

namespace Match3
{
    /// <summary>
    /// Match3 level reaction videos from <see cref="Match3LevelConfig"/>:
    /// video1 loops by default; video2 on exact-3 matches; video3 on 4+ or gold peach.
    /// After video2/3 end, returns to looping video1.
    /// </summary>
    public class Match3LevelVideoPlayer : MonoBehaviour
    {
        public static Match3LevelVideoPlayer Instance { get; private set; }

        [SerializeField] VideoPlayer videoPlayer;
        [Tooltip("Desired playback rate. Mobile often ignores VideoPlayer.playbackSpeed; we re-apply after prepare and fall back to time scrubbing.")]
        [SerializeField] float playbackSpeed = 2f;

        VideoClip _video1;
        VideoClip _video2;
        VideoClip _video3;
        bool _mute1 = true;
        bool _mute2 = true;
        bool _mute3 = true;
        bool _playingReaction;
        bool _manualSpeed;
        float _desiredSpeed = 2f;

        void Awake()
        {
            Instance = this;
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer != null)
            {
                // Prefer inspector VideoPlayer speed if already set (e.g. scene value 2).
                if (videoPlayer.playbackSpeed > 0.01f)
                    playbackSpeed = videoPlayer.playbackSpeed;
                _desiredSpeed = Mathf.Max(0.01f, playbackSpeed);

                videoPlayer.playOnAwake = false;
                videoPlayer.skipOnDrop = true;
                videoPlayer.playbackSpeed = _desiredSpeed;
                videoPlayer.prepareCompleted += OnPrepareCompleted;
                videoPlayer.loopPointReached += OnLoopPointReached;
            }
        }

        void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnPrepareCompleted;
                videoPlayer.loopPointReached -= OnLoopPointReached;
            }
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (!_manualSpeed || videoPlayer == null || !videoPlayer.isPlaying || !videoPlayer.isPrepared)
                return;

            // Native clock is ~1x; scrub the rest so effective rate ≈ _desiredSpeed.
            double extra = Time.unscaledDeltaTime * (_desiredSpeed - 1f);
            if (Math.Abs(extra) < 0.0001)
                return;

            double length = videoPlayer.length;
            if (length <= 0)
                return;

            double next = videoPlayer.time + extra;
            if (next >= length)
            {
                if (videoPlayer.isLooping)
                    next %= length;
                else
                    next = length;
            }
            else if (next < 0)
            {
                next = videoPlayer.isLooping ? length + (next % length) : 0;
            }

            videoPlayer.time = next;
        }

        public void Configure(Match3LevelConfig level)
        {
            _video1 = level != null ? level.video1 : null;
            _video2 = level != null ? level.video2 : null;
            _video3 = level != null ? level.video3 : null;
            _mute1 = level == null || level.muteVideo1;
            _mute2 = level == null || level.muteVideo2;
            _mute3 = level == null || level.muteVideo3;
            PlayIdle();
        }

        /// <param name="maxMatchRunLength">Longest same-color run cleared (0 if none).</param>
        /// <param name="goldPeachBurst">True if the player detonated a gold peach.</param>
        public void NotifyClear(int maxMatchRunLength, bool goldPeachBurst)
        {
            if (goldPeachBurst || maxMatchRunLength >= 4)
            {
                PlayReaction(_video3, _mute3);
                return;
            }

            if (maxMatchRunLength == 3)
                PlayReaction(_video2, _mute2);
        }

        void PlayIdle()
        {
            _playingReaction = false;
            if (_video1 == null || videoPlayer == null)
                return;

            if (videoPlayer.clip == _video1 && videoPlayer.isPlaying && videoPlayer.isLooping)
            {
                ApplyMute(_mute1);
                ApplyPlaybackSpeed();
                return;
            }

            videoPlayer.isLooping = true;
            videoPlayer.clip = _video1;
            ApplyMute(_mute1);
            videoPlayer.Play();
            ApplyPlaybackSpeed();
        }

        void PlayReaction(VideoClip clip, bool mute)
        {
            if (clip == null || videoPlayer == null)
                return;

            _playingReaction = true;
            videoPlayer.isLooping = false;
            videoPlayer.clip = clip;
            videoPlayer.time = 0;
            ApplyMute(mute);
            videoPlayer.Play();
            ApplyPlaybackSpeed();
        }

        void OnPrepareCompleted(VideoPlayer source)
        {
            ApplyPlaybackSpeed();
        }

        void ApplyPlaybackSpeed()
        {
            if (videoPlayer == null)
                return;

            _desiredSpeed = Mathf.Max(0.01f, playbackSpeed);

            // canSetPlaybackSpeed is only valid after Prepare.
            if (videoPlayer.isPrepared && videoPlayer.canSetPlaybackSpeed)
            {
                videoPlayer.playbackSpeed = _desiredSpeed;
                _manualSpeed = false;
                return;
            }

            // Android (and some devices) often report false — keep component at 1 and scrub in Update.
            videoPlayer.playbackSpeed = 1f;
            _manualSpeed = !Mathf.Approximately(_desiredSpeed, 1f);
        }

        void ApplyMute(bool mute)
        {
            if (videoPlayer == null)
                return;

            if (mute)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                return;
            }

            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            if (videoPlayer.controlledAudioTrackCount > 0)
                videoPlayer.SetDirectAudioMute(0, false);
        }

        void OnLoopPointReached(VideoPlayer source)
        {
            if (!_playingReaction)
                return;
            PlayIdle();
        }
    }
}
