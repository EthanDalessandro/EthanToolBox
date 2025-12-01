using System;
using System.Collections.Generic;
using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    [Serializable]
    public class ESerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();

            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            this.Clear();

            if (keys.Count != values.Count)
            {
                Debug.LogError($"ESerializableDictionary: Key count ({keys.Count}) does not match Value count ({values.Count}).");
                return;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] != null && !this.ContainsKey(keys[i]))
                {
                    this.Add(keys[i], values[i]);
                }
            }
        }
    }
}
