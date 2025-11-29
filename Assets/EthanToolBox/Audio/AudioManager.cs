using System.Collections.Generic;
using UnityEngine;

namespace EthanToolBox.Core.Audio
{
    public class AudioManager : MonoBehaviour, IAudioManager
    {
        private AudioSource _musicSource;
        private List<AudioSource> _sfxSources = new List<AudioSource>();
        private float _masterVolume = 1f;

        private void Awake()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableSfxSource();
            source.clip = clip;
            source.volume = volume * _masterVolume;
            source.Play();
        }

        public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true)
        {
            if (clip == null) return;

            _musicSource.clip = clip;
            _musicSource.volume = volume * _masterVolume;
            _musicSource.loop = loop;
            _musicSource.Play();
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            _musicSource.volume = _musicSource.volume * _masterVolume; // Update current music immediately
        }

        private AudioSource GetAvailableSfxSource()
        {
            foreach (var source in _sfxSources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            var newSource = gameObject.AddComponent<AudioSource>();
            _sfxSources.Add(newSource);
            return newSource;
        }
    }
}
