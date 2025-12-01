using UnityEngine;
using System.Threading.Tasks;

namespace EthanToolBox.Core.Audio
{
    public interface IAudioManager
    {
        void PlayMusic(SoundData data, float fadeDuration = 1f);
        void StopMusic(float fadeDuration = 1f);
        void PlaySfx(SoundData data, Vector3 position = default);
        void PlayUi(SoundData data);
        void SetGlobalVolume(AudioChannel channel, float volume);
        float GetGlobalVolume(AudioChannel channel);
        void PauseAll();
        void ResumeAll();
    }
}
