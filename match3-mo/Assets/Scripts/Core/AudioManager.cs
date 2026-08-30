using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    [Serializable]
    public class AudioClipEntry
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop;
    }

    public class AudioManager : MonoBehaviour
    {
        public const string Bgm1Name = "bgm-1";
        public const string Bgm2Name = "bgm-2";

        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips")]
        [SerializeField] List<AudioClipEntry> clips = new List<AudioClipEntry>();
        [SerializeField] float bgmCrossfadeSeconds = 1.25f;

        AudioSource _bgmA;
        AudioSource _bgmB;
        AudioSource _sfxSource;
        AudioClip _targetClip;
        Coroutine _fadeRoutine;
        bool _hasBgmDirective;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
        }

        void Start()
        {
            if (_hasBgmDirective)
                return;
            PlayNamedBgm(Bgm1Name);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ApplyForHomeVideo(HomeVideoId id)
        {
            switch (id)
            {
                case HomeVideoId.Micro1:
                case HomeVideoId.Micro2:
                case HomeVideoId.Micro3:
                case HomeVideoId.Micro4:
                    FadeOutBgm();
                    return;
                case HomeVideoId.VacationDay:
                case HomeVideoId.VacationStreet:
                case HomeVideoId.VacationNight:
                    PlayNamedBgm(Bgm2Name);
                    return;
                default:
                    PlayNamedBgm(Bgm1Name);
                    return;
            }
        }

        public void ApplyForMatch3(HomeVideoId streetVideoId)
        {
            if (streetVideoId == HomeVideoId.VacationStreet)
                PlayNamedBgm(Bgm2Name);
            else
                PlayNamedBgm(Bgm1Name);
        }

        public void Play(string clipName)
        {
            var entry = FindClip(clipName);
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"AudioManager: clip '{clipName}' not found.");
                return;
            }

            if (entry.loop)
                PlayNamedBgm(clipName);
            else
                PlaySfx(entry.clip, entry.volume);
        }

        public void Stop(string clipName)
        {
            var entry = FindClip(clipName);
            if (entry == null || entry.clip == null)
                return;

            if (_targetClip == entry.clip || IsPlayingClip(entry.clip))
                FadeOutBgm();
        }

        public void PlayNamedBgm(string clipName)
        {
            var entry = FindClip(clipName);
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"AudioManager: clip '{clipName}' not found.");
                return;
            }

            if (_targetClip == entry.clip)
                return;

            _hasBgmDirective = true;
            _targetClip = entry.clip;
            CrossfadeTo(entry);
        }

        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            PlayBgm(clip, loop, 1f);
        }

        public void PlayBgm(AudioClip clip, bool loop, float volume)
        {
            if (clip == null)
                return;

            if (_targetClip == clip)
                return;

            _hasBgmDirective = true;
            _targetClip = clip;
            var entry = new AudioClipEntry
            {
                name = clip.name,
                clip = clip,
                volume = Mathf.Clamp01(volume),
                loop = loop
            };
            CrossfadeTo(entry);
        }

        public void StopBgm()
        {
            FadeOutBgm();
        }

        public void FadeOutBgm()
        {
            _hasBgmDirective = true;
            if (_targetClip == null && _fadeRoutine == null && !IsAnyBgmPlaying())
                return;

            _targetClip = null;
            CrossfadeTo(null);
        }

        public void PlaySfx(AudioClip clip)
        {
            PlaySfx(clip, 1f);
        }

        public void PlaySfx(AudioClip clip, float volume)
        {
            if (clip == null || _sfxSource == null)
                return;

            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void SetBgmVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (_bgmA != null)
                _bgmA.volume = volume;
            if (_bgmB != null)
                _bgmB.volume = volume;
        }

        public void SetSfxVolume(float volume)
        {
            if (_sfxSource != null)
                _sfxSource.volume = Mathf.Clamp01(volume);
        }

        void CrossfadeTo(AudioClipEntry next)
        {
            EnsureSources();
            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(CrossfadeRoutine(next));
        }

        IEnumerator CrossfadeRoutine(AudioClipEntry next)
        {
            AudioSource incoming = null;
            float targetVolume = next != null ? Mathf.Clamp01(next.volume) : 0f;

            if (next != null)
            {
                incoming = SourceAlreadyPlaying(next.clip);
                if (incoming == null)
                {
                    incoming = IncomingSource();
                    incoming.clip = next.clip;
                    incoming.loop = next.loop;
                    incoming.volume = 0f;
                    incoming.Play();
                }
            }

            float duration = Mathf.Max(0.01f, bgmCrossfadeSeconds);
            float t = 0f;
            float a0 = _bgmA.volume;
            float b0 = _bgmB.volume;
            float a1 = incoming == _bgmA ? targetVolume : 0f;
            float b1 = incoming == _bgmB ? targetVolume : 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                _bgmA.volume = Mathf.Lerp(a0, a1, u);
                _bgmB.volume = Mathf.Lerp(b0, b1, u);
                yield return null;
            }

            _bgmA.volume = a1;
            _bgmB.volume = b1;
            StopIfSilent(_bgmA);
            StopIfSilent(_bgmB);
            _fadeRoutine = null;
        }

        AudioSource IncomingSource()
        {
            bool aPlaying = _bgmA.isPlaying && _bgmA.clip != null;
            bool bPlaying = _bgmB.isPlaying && _bgmB.clip != null;
            if (aPlaying && !bPlaying)
                return _bgmB;
            if (bPlaying && !aPlaying)
                return _bgmA;
            if (aPlaying && bPlaying)
                return _bgmA.volume <= _bgmB.volume ? _bgmA : _bgmB;
            return _bgmA;
        }

        AudioSource SourceAlreadyPlaying(AudioClip clip)
        {
            if (clip == null)
                return null;
            if (_bgmA.isPlaying && _bgmA.clip == clip)
                return _bgmA;
            if (_bgmB.isPlaying && _bgmB.clip == clip)
                return _bgmB;
            return null;
        }

        static void StopIfSilent(AudioSource source)
        {
            if (source == null || source.volume > 0.0001f)
                return;
            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        bool IsAnyBgmPlaying()
        {
            return (_bgmA != null && _bgmA.isPlaying) || (_bgmB != null && _bgmB.isPlaying);
        }

        bool IsPlayingClip(AudioClip clip)
        {
            if (clip == null)
                return false;
            return (_bgmA != null && _bgmA.isPlaying && _bgmA.clip == clip)
                || (_bgmB != null && _bgmB.isPlaying && _bgmB.clip == clip);
        }

        AudioClipEntry FindClip(string clipName)
        {
            if (string.IsNullOrEmpty(clipName) || clips == null)
                return null;

            for (int i = 0; i < clips.Count; i++)
            {
                var entry = clips[i];
                if (entry != null && string.Equals(entry.name, clipName, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        void EnsureSources()
        {
            if (_bgmA != null && _bgmB != null && _sfxSource != null)
                return;

            var sources = GetComponents<AudioSource>();
            if (sources.Length >= 1)
                _bgmA = sources[0];
            else
                _bgmA = gameObject.AddComponent<AudioSource>();

            if (sources.Length >= 2)
                _bgmB = sources[1];
            else
                _bgmB = gameObject.AddComponent<AudioSource>();

            if (sources.Length >= 3)
                _sfxSource = sources[2];
            else
                _sfxSource = gameObject.AddComponent<AudioSource>();

            ConfigureBgmSource(_bgmA);
            ConfigureBgmSource(_bgmB);
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
        }

        static void ConfigureBgmSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
        }
    }
}
