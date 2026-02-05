using System.Collections.Generic;
using UnityEngine;

namespace EthanToolBox.Core.Pooling
{
    /// <summary>
    /// Internal object pool manager. Automatically manages pools per prefab.
    /// </summary>
    public static class ObjectPool
    {
        // Pool storage: PrefabInstanceID -> Queue of inactive instances
        private static readonly Dictionary<int, Queue<GameObject>> _pools = new Dictionary<int, Queue<GameObject>>();
        
        // Track which prefab an instance came from (for returning)
        private static readonly Dictionary<int, int> _instanceToPrefabId = new Dictionary<int, int>();

        // Parent transform for pooled objects (keeps hierarchy clean)
        private static Transform _poolRoot;
        
        private static Transform PoolRoot
        {
            get
            {
                if (_poolRoot == null)
                {
                    var go = new GameObject("[ObjectPool]");
                    Object.DontDestroyOnLoad(go);
                    go.SetActive(false); // Keep children inactive
                    _poolRoot = go.transform;
                }
                return _poolRoot;
            }
        }

        /// <summary>
        /// Get an instance from the pool, or create a new one if pool is empty.
        /// </summary>
        public static GameObject Get(GameObject prefab)
        {
            int prefabId = prefab.GetInstanceID();
            
            if (_pools.TryGetValue(prefabId, out var pool) && pool.Count > 0)
            {
                var instance = pool.Dequeue();
                instance.transform.SetParent(null);
                instance.SetActive(true);
                
                // Call OnSpawn on IPoolable components
                NotifySpawn(instance);
                
                return instance;
            }
            
            // Pool empty, create new instance
            var newInstance = Object.Instantiate(prefab);
            _instanceToPrefabId[newInstance.GetInstanceID()] = prefabId;
            
            return newInstance;
        }

        /// <summary>
        /// Get an instance with position and rotation.
        /// </summary>
        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var instance = Get(prefab);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        /// <summary>
        /// Get an instance under a parent.
        /// </summary>
        public static GameObject Get(GameObject prefab, Transform parent)
        {
            var instance = Get(prefab);
            instance.transform.SetParent(parent, false);
            return instance;
        }

        /// <summary>
        /// Get an instance under a parent with worldPositionStays option.
        /// </summary>
        public static GameObject Get(GameObject prefab, Transform parent, bool worldPositionStays)
        {
            var instance = Get(prefab);
            instance.transform.SetParent(parent, worldPositionStays);
            return instance;
        }

        /// <summary>
        /// Get an instance with position, rotation, and parent.
        /// </summary>
        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            var instance = Get(prefab);
            instance.transform.SetParent(parent);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        /// <summary>
        /// Return an instance to its pool.
        /// </summary>
        public static void Return(GameObject instance)
        {
            if (instance == null) return;

            int instanceId = instance.GetInstanceID();
            
            if (!_instanceToPrefabId.TryGetValue(instanceId, out int prefabId))
            {
                // Object wasn't spawned via pool, just destroy it
                Object.Destroy(instance);
                return;
            }

            // Call OnRelease on IPoolable components
            NotifyRelease(instance);

            instance.SetActive(false);
            instance.transform.SetParent(PoolRoot);

            if (!_pools.ContainsKey(prefabId))
            {
                _pools[prefabId] = new Queue<GameObject>();
            }
            
            _pools[prefabId].Enqueue(instance);
        }

        /// <summary>
        /// Pre-create instances for a prefab.
        /// </summary>
        public static void Prewarm(GameObject prefab, int count)
        {
            int prefabId = prefab.GetInstanceID();
            
            if (!_pools.ContainsKey(prefabId))
            {
                _pools[prefabId] = new Queue<GameObject>();
            }

            for (int i = 0; i < count; i++)
            {
                var instance = Object.Instantiate(prefab, PoolRoot);
                instance.SetActive(false);
                _instanceToPrefabId[instance.GetInstanceID()] = prefabId;
                _pools[prefabId].Enqueue(instance);
            }
        }

        /// <summary>
        /// Clear all pools and destroy pooled objects.
        /// </summary>
        public static void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj != null) Object.Destroy(obj);
                }
            }
            _pools.Clear();
            _instanceToPrefabId.Clear();
        }

        private static void NotifySpawn(GameObject instance)
        {
            var poolables = instance.GetComponentsInChildren<IPoolable>(true);
            foreach (var p in poolables)
            {
                p.OnSpawn();
            }
        }

        private static void NotifyRelease(GameObject instance)
        {
            var poolables = instance.GetComponentsInChildren<IPoolable>(true);
            foreach (var p in poolables)
            {
                p.OnRelease();
            }
        }
    }
}
