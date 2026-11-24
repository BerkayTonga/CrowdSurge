using UnityEngine;

namespace CrowdSurge
{
    /// <summary>
    /// Plays music and pooled one-shot effects. Survives scene reloads (so music never restarts and clips are
    /// synthesized only once) and respects the sound / music settings.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int VoiceCount = 8;
        private const float MusicVolume = 0.3f;

        public static AudioManager Instance { get; private set; }

        private AudioSource _music;
        private AudioSource[] _voices;
        private AudioClip[] _clips;
        private float[] _lastPlayed;
        private int _nextVoice;
        private float _musicScale = 1f;

        /// <summary>Returns the persistent instance, creating it on first use.</summary>
        public static AudioManager GetOrCreate()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("Audio");
            DontDestroyOnLoad(go);
            return go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            Instance = this;

            var sfxValues = (Sfx[])System.Enum.GetValues(typeof(Sfx));
            _clips = new AudioClip[sfxValues.Length];
            _lastPlayed = new float[sfxValues.Length];
            foreach (var sfx in sfxValues) _clips[(int)sfx] = Resources.Load<AudioClip>("Audio/" + sfx) ?? SynthClips.Create(sfx);

            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = Resources.Load<AudioClip>("Audio/Music") ?? SynthClips.CreateMusic();
            _music.loop = true;
            _music.playOnAwake = false;

            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
            }
            ApplySettings();
        }

        /// <summary>Re-reads the sound settings. Call after the player toggles them.</summary>
        public void ApplySettings()
        {
            _music.mute = !SaveData.MusicOn;
            _music.volume = MusicVolume * _musicScale * SaveData.MusicVolume;
        }

        public void PlayMusic()
        {
            if (!_music.isPlaying) _music.Play();
        }

        /// <param name="scale">1 = normal; lower while paused or on result screens.</param>
        public void SetMusicScale(float scale)
        {
            _musicScale = scale;
            _music.volume = MusicVolume * scale * SaveData.MusicVolume;
        }

        public void Play(Sfx sfx, float volume = 1f, float pitchJitter = 0.06f, float minInterval = 0.04f)
            => PlayPitched(sfx, volume, 1f + Random.Range(-pitchJitter, pitchJitter), minInterval);

        public void PlayPitched(Sfx sfx, float volume, float pitch, float minInterval = 0.04f)
        {
            if (!SaveData.SfxOn) return;
            int id = (int)sfx;
            float now = Time.unscaledTime;
            if (now - _lastPlayed[id] < minInterval) return;
            _lastPlayed[id] = now;

            var voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % VoiceCount;
            voice.pitch = pitch;
            voice.PlayOneShot(_clips[id], volume * SaveData.SfxVolume);
        }

        public static void SoundPitched(Sfx sfx, float volume, float pitch, float minInterval = 0.04f)
        {
            if (Instance != null) Instance.PlayPitched(sfx, volume, pitch, minInterval);
        }

        /// <summary>Static helper so gameplay code stays free of null checks.</summary>
        public static void Sound(Sfx sfx, float volume = 1f, float pitchJitter = 0.06f, float minInterval = 0.04f)
        {
            if (Instance != null) Instance.Play(sfx, volume, pitchJitter, minInterval);
        }
    }
}
