using UnityEngine;

namespace EthanToolBox.Core.Audio
{
    public interface IAudioManager
    {
        void PlaySfx(AudioClip clip, float volume = 1f);
        void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true);
        void SetMasterVolume(float volume);
    }
}
