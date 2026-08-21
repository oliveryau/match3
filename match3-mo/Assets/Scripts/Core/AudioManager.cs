using UnityEngine;

namespace Match3
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource _bgmSource;
        private AudioSource _sfxSource;

        private void Awake()
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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            if (clip == null)
                return;

            _bgmSource.loop = loop;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying)
                return;

            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public void StopBgm()
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null)
                return;

            _sfxSource.PlayOneShot(clip);
        }

        public void SetBgmVolume(float volume)
        {
            _bgmSource.volume = Mathf.Clamp01(volume);
        }

        public void SetSfxVolume(float volume)
        {
            _sfxSource.volume = Mathf.Clamp01(volume);
        }

        private void EnsureSources()
        {
            var sources = GetComponents<AudioSource>();
            if (sources.Length >= 2)
            {
                _bgmSource = sources[0];
                _sfxSource = sources[1];
            }
            else
            {
                _bgmSource = sources.Length == 1 ? sources[0] : gameObject.AddComponent<AudioSource>();
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }

            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
        }
    }
}
