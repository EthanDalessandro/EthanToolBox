using System.Collections.Generic;
using UnityEngine;

namespace EthanToolBox.Core.Audio
{
    public class AudioSourcePool : MonoBehaviour
    {
        private readonly Queue<AudioSource> _pool = new Queue<AudioSource>();
        private Transform _parent;
        private int _initialSize = 10;

        public void Initialize(int size, Transform parent)
        {
            _initialSize = size;
            _parent = parent;

            for (int i = 0; i < _initialSize; i++)
            {
                CreateNewSource();
            }
        }

        private AudioSource CreateNewSource()
        {
            var go = new GameObject("PooledAudioSource");
            go.transform.SetParent(_parent);
            var source = go.AddComponent<AudioSource>();
            go.SetActive(false);
            _pool.Enqueue(source);
            return source;
        }

        public AudioSource Get()
        {
            if (_pool.Count == 0)
            {
                CreateNewSource();
            }

            var source = _pool.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }

        public void Return(AudioSource source)
        {
            if (source == null) return;

            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;

            // Reset all properties to defaults to avoid state bleeding between uses
            source.loop = false;
            source.pitch = 1f;
            source.volume = 1f;
            source.spatialBlend = 0f;
            source.dopplerLevel = 1f;
            source.minDistance = 1f;
            source.maxDistance = 500f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.priority = 128;

            source.gameObject.SetActive(false);
            _pool.Enqueue(source);
        }
    }
}
