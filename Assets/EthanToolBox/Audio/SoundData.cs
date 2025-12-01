using UnityEngine;
using UnityEngine.Audio;

namespace EthanToolBox.Core.Audio
{
    public enum AudioChannel
    {
        Master,
        Music,
        Sfx,
        UI,
        Voice
    }

    [CreateAssetMenu(fileName = "NewSoundData", menuName = "EthanToolBox/Audio/Sound Data")]
    public class SoundData : ScriptableObject
    {
        [Header("Audio Clip")]
        [Tooltip("The audio clip to play. If multiple are provided, one will be chosen at random.")]
        public AudioClip[] Clips;

        [Header("Settings")]
        [Range(0f, 1f)] public float Volume = 1f;
        [Range(0.1f, 3f)] public float Pitch = 1f;
        public bool Loop = false;

        [Header("Randomization")]
        [Range(0f, 0.5f)] public float VolumeVariance = 0f;
        [Range(0f, 0.5f)] public float PitchVariance = 0f;

        [Header("Spatial Settings")]
        [Range(0f, 1f)] public float SpatialBlend = 0f; // 0 = 2D, 1 = 3D
        [Range(0f, 1.1f)] public float DopplerLevel = 1f;
        public float MinDistance = 1f;
        public float MaxDistance = 500f;
        public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Mixer")]
        public AudioMixerGroup MixerGroup;

        [Header("Priority")]
        [Range(0, 256)] public int Priority = 128; // 0 = High, 256 = Low

        public AudioClip GetClip()
        {
            if (Clips == null || Clips.Length == 0) return null;
            return Clips[Random.Range(0, Clips.Length)];
        }

        public float GetVolume()
        {
            return Volume + Random.Range(-VolumeVariance, VolumeVariance);
        }

        public float GetPitch()
        {
            return Pitch + Random.Range(-PitchVariance, PitchVariance);
        }
    }
}
