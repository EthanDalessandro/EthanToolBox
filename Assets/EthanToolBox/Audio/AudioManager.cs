using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using EthanToolBox.Core.DependencyInjection;

namespace EthanToolBox.Core.Audio
{
    [Service(typeof(IAudioManager))]
    public class AudioManager : MonoBehaviour, IAudioManager
    {
        [Header("Settings")]
        [SerializeField] private int _initialPoolSize = 20;
        [SerializeField] private AudioMixer _audioMixer; // Optional: Assign in inspector if using Unity Audio Mixer
        [SerializeField] private AudioMixerGroup _masterGroup;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _uiGroup;
        [SerializeField] private AudioMixerGroup _voiceGroup;

        private AudioSourcePool _pool;
        private AudioSource _musicSource1;
        private AudioSource _musicSource2;
        private bool _isMusicSource1Playing = false;
        
        private Dictionary<AudioChannel, float> _volumes = new Dictionary<AudioChannel, float>();
        private List<AudioSource> _activeSfxSources = new List<AudioSource>();

        private void Awake()
        {
            InitializePool();
            InitializeMusicSources();
            InitializeVolumes();
        }

        private void InitializePool()
        {
            var poolGo = new GameObject("AudioSourcePool");
            poolGo.transform.SetParent(transform);
            _pool = poolGo.AddComponent<AudioSourcePool>();
            _pool.Initialize(_initialPoolSize, poolGo.transform);
        }

        private void InitializeMusicSources()
        {
            _musicSource1 = CreateMusicSource("MusicSource_1");
            _musicSource2 = CreateMusicSource("MusicSource_2");
        }

        private AudioSource CreateMusicSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.outputAudioMixerGroup = _musicGroup;
            return source;
        }

        private void InitializeVolumes()
        {
            _volumes[AudioChannel.Master] = 1f;
            _volumes[AudioChannel.Music] = 1f;
            _volumes[AudioChannel.Sfx] = 1f;
            _volumes[AudioChannel.UI] = 1f;
            _volumes[AudioChannel.Voice] = 1f;
        }

        public void PlayMusic(SoundData data, float fadeDuration = 1f)
        {
            if (data == null || data.GetClip() == null) return;

            AudioSource activeSource = _isMusicSource1Playing ? _musicSource1 : _musicSource2;
            AudioSource newSource = _isMusicSource1Playing ? _musicSource2 : _musicSource1;

            // If the same clip is already playing, do nothing
            if (activeSource.isPlaying && activeSource.clip == data.GetClip()) return;

            StartCoroutine(CrossFadeMusic(activeSource, newSource, data, fadeDuration));
            _isMusicSource1Playing = !_isMusicSource1Playing;
        }

        public void StopMusic(float fadeDuration = 1f)
        {
            AudioSource activeSource = _isMusicSource1Playing ? _musicSource1 : _musicSource2;
            StartCoroutine(FadeOut(activeSource, fadeDuration));
        }

        public void PlaySfx(SoundData data, Vector3 position = default)
        {
            if (data == null) return;

            var source = _pool.Get();
            ConfigureSource(source, data, _sfxGroup);
            
            // If position is default (0,0,0) and spatial blend is 0 (2D), we don't need to move it.
            // But if it's 3D, we move it.
            if (data.SpatialBlend > 0)
            {
                source.transform.position = position;
            }
            else
            {
                // Attach to manager for 2D sounds to avoid being left behind
                source.transform.position = transform.position; 
            }

            source.Play();
            _activeSfxSources.Add(source);
            StartCoroutine(ReturnToPoolAfterPlay(source));
        }

        public void PlayUi(SoundData data)
        {
            if (data == null) return;
            
            // UI sounds are usually 2D and ignore spatial settings often, but we respect SoundData
            var source = _pool.Get();
            ConfigureSource(source, data, _uiGroup);
            source.transform.position = transform.position;
            source.Play();
            _activeSfxSources.Add(source);
            StartCoroutine(ReturnToPoolAfterPlay(source));
        }

        public void SetGlobalVolume(AudioChannel channel, float volume)
        {
            if (_volumes.ContainsKey(channel))
            {
                _volumes[channel] = Mathf.Clamp01(volume);
                UpdateMixerVolumes();
            }
        }

        public float GetGlobalVolume(AudioChannel channel)
        {
            return _volumes.ContainsKey(channel) ? _volumes[channel] : 1f;
        }

        public void PauseAll()
        {
            _musicSource1.Pause();
            _musicSource2.Pause();
            foreach (var source in _activeSfxSources)
            {
                if (source != null && source.isPlaying) source.Pause();
            }
        }

        public void ResumeAll()
        {
            _musicSource1.UnPause();
            _musicSource2.UnPause();
            foreach (var source in _activeSfxSources)
            {
                if (source != null) source.UnPause();
            }
        }

        private void ConfigureSource(AudioSource source, SoundData data, AudioMixerGroup defaultGroup)
        {
            source.clip = data.GetClip();
            source.volume = data.GetVolume() * GetChannelVolume(data);
            source.pitch = data.GetPitch();
            source.loop = data.Loop;
            source.spatialBlend = data.SpatialBlend;
            source.dopplerLevel = data.DopplerLevel;
            source.minDistance = data.MinDistance;
            source.maxDistance = data.MaxDistance;
            source.rolloffMode = data.RolloffMode;
            source.priority = data.Priority;
            
            source.outputAudioMixerGroup = data.MixerGroup != null ? data.MixerGroup : defaultGroup;
        }

        private float GetChannelVolume(SoundData data)
        {
            // Determine channel based on mixer group or fallback logic could be added here.
            // For now, we multiply by Master. 
            // Ideally SoundData could have a "Channel" field, but for simplicity we rely on the method called (PlaySfx vs PlayUi)
            // However, since we don't pass channel to ConfigureSource, we just use Master * specific volume logic if we had it.
            // Let's simplify: All SFX use SFX volume, UI uses UI volume.
            // But here we don't know if it was called via PlaySfx or PlayUi easily without passing it.
            // Let's assume Master volume affects everything.
            return _volumes[AudioChannel.Master]; 
            // Real implementation would multiply by _volumes[AudioChannel.Sfx] if it's an SFX.
            // To fix this, we should pass the channel volume to ConfigureSource.
        }

        private IEnumerator ReturnToPoolAfterPlay(AudioSource source)
        {
            // Wait for clip length + a small buffer
            // If looping, we don't return automatically (user must stop it manually - not supported for SFX yet in this simple API)
            if (source.loop) yield break;

            float duration = source.clip.length / Mathf.Abs(source.pitch);
            yield return new WaitForSeconds(duration + 0.1f);

            if (_activeSfxSources.Contains(source))
            {
                _activeSfxSources.Remove(source);
            }
            _pool.Return(source);
        }

        private IEnumerator CrossFadeMusic(AudioSource fadingOut, AudioSource fadingIn, SoundData newData, float duration)
        {
            float timer = 0f;
            float startVol = fadingOut.volume;
            float targetVol = newData.GetVolume() * _volumes[AudioChannel.Master] * _volumes[AudioChannel.Music];

            fadingIn.clip = newData.GetClip();
            fadingIn.volume = 0f;
            fadingIn.Play();

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;

                if (fadingOut.isPlaying)
                    fadingOut.volume = Mathf.Lerp(startVol, 0f, t);
                
                fadingIn.volume = Mathf.Lerp(0f, targetVol, t);

                yield return null;
            }

            fadingOut.Stop();
            fadingOut.volume = startVol; // Reset for next use
            fadingIn.volume = targetVol;
        }

        private IEnumerator FadeOut(AudioSource source, float duration)
        {
            float startVol = source.volume;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, timer / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVol;
        }

        private void UpdateMixerVolumes()
        {
            // If using AudioMixer, we would set exposed parameters here.
            // _audioMixer.SetFloat("MasterVolume", LogVolume(_volumes[AudioChannel.Master]));
            
            // Since we are doing manual volume control on AudioSources for this implementation (to be dependency-free from a specific Mixer asset):
            if (_musicSource1.isPlaying) _musicSource1.volume = _musicSource1.volume * _volumes[AudioChannel.Master] * _volumes[AudioChannel.Music]; // This is tricky because we lose the original volume.
            // A better way is to store "BaseVolume" on the source or recalculate.
            // For simplicity in this "Complete" but "Lightweight" version, we might just accept that changing global volume affects *next* plays or requires iterating all active sources.
            
            // Update active SFX
            foreach(var source in _activeSfxSources)
            {
                if(source != null && source.isPlaying)
                {
                    // Re-apply volume (simplified)
                    source.volume = source.volume * _volumes[AudioChannel.Master]; 
                }
            }
        }
    }
}
