using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace EthanToolBox.Core.DependencyInjection
{
    [DefaultExecutionOrder(-1000)]
    public abstract class DICompositionRoot : MonoBehaviour
    {
        // Track all active roots to find parents/contexts
        private static readonly List<DICompositionRoot> _activeRoots = new();
        private static HashSet<int> _injectedInstances = new();

        [Header("Context Settings")]
        [Tooltip("If true, this Root will persist across scenes and act as a Global Parent.")]
        [SerializeField] private bool _isGlobal = false;

        public DIContainer Container { get; protected set; }
        public Injector Injector { get; protected set; }

        protected virtual void Awake()
        {
            if (_isGlobal)
            {
                if (_activeRoots.Exists(r => r._isGlobal))
                {
                    Debug.LogWarning("[DI] Multiple Global Roots detected. Destroying duplicate.");
                    Destroy(gameObject);
                    return;
                }
                DontDestroyOnLoad(gameObject);
            }

            _activeRoots.Add(this);

            // 1. Find Parent Container
            DIContainer parentContainer = null;
            if (!_isGlobal)
            {
                var globalRoot = _activeRoots.Find(r => r._isGlobal);
                if (globalRoot != null)
                {
                    parentContainer = globalRoot.Container;
                }
            }

            // 2. Create Container
            Container = new DIContainer(parentContainer);
            Injector = new Injector(Container);
            _injectedInstances.Clear();

            // Default Registrations
            Container.RegisterSingleton(typeof(EventSystem.IEventBus), new EventSystem.EventBus());

            // 3. Configure & Register
            Configure(Container);
            RegisterServices(Container);

            // 4. Inject into Scene Objects
            var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mb in allMonoBehaviours)
            {
                if (mb == this) continue;
                // Only inject objects belonging to THIS scene (unless we are global/fallback)
                if (mb.gameObject.scene == gameObject.scene || _isGlobal)
                {
                    InjectAndTrack(mb);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            _activeRoots.Remove(this);
        }

        /// <summary>
        /// Call this from OnEnable to request late injection.
        /// Finds the best matching Context (Scene > Global) for the target.
        /// </summary>
        public static void RequestInjection(MonoBehaviour target)
        {
            if (target == null) return;

            int id = target.GetInstanceID();
            if (_injectedInstances.Contains(id)) return;

            // Find the best root for this target
            var root = GetRootFor(target);
            if (root != null)
            {
                root.Injector.Inject(target);
                _injectedInstances.Add(id);
            }
            else
            {
                Debug.LogWarning($"[DI] No Context found for {target.name} in scene {target.gameObject.scene.name}");
            }
        }

        private static DICompositionRoot GetRootFor(MonoBehaviour target)
        {
            // 1. Try find root in same scene
            var sceneRoot = _activeRoots.Find(r => r.gameObject.scene == target.gameObject.scene);
            if (sceneRoot != null) return sceneRoot;

            // 2. Fallback to Global
            return _activeRoots.Find(r => r._isGlobal);
        }

        #region Spawn

        /// <summary>
        /// Spawns a prefab (from pool if available) and automatically injects all [Inject] dependencies.
        /// </summary>
        public static T Spawn<T>(T prefab) where T : Component
        {
            var instance = Instantiate(prefab);
            InjectIfNew(instance.gameObject);
            return instance;
        }

        /// <summary>
        /// Spawns a prefab under a parent and automatically injects all [Inject] dependencies.
        /// </summary>
        public static T Spawn<T>(T prefab, Transform parent) where T : Component
        {
            var instance = Instantiate(prefab, parent);
            InjectIfNew(instance.gameObject);
            return instance;
        }

        /// <summary>
        /// Spawns a prefab under a parent and automatically injects all [Inject] dependencies.
        /// </summary>
        public static T Spawn<T>(T prefab, Transform parent, bool worldPositionStays) where T : Component
        {
            var instance = Instantiate(prefab, parent, worldPositionStays);
            InjectIfNew(instance.gameObject);
            return instance;
        }

        /// <summary>
        /// Spawns a prefab at position/rotation and automatically injects all [Inject] dependencies.
        /// </summary>
        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            var instance = Instantiate(prefab, position, rotation);
            InjectIfNew(instance.gameObject);
            return instance;
        }

        /// <summary>
        /// Spawns a prefab at position/rotation under a parent and automatically injects all [Inject] dependencies.
        /// </summary>
        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Component
        {
            var instance = Instantiate(prefab, position, rotation, parent);
            InjectIfNew(instance.gameObject);
            return instance;
        }

        /// <summary>
        /// Spawns a GameObject prefab and automatically injects all [Inject] dependencies.
        /// </summary>
        public static GameObject Spawn(GameObject prefab)
        {
            var instance = Instantiate(prefab);
            InjectIfNew(instance);
            return instance;
        }

        /// <summary>
        /// Spawns a GameObject prefab under a parent and automatically injects all [Inject] dependencies.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Transform parent)
        {
            var instance = Instantiate(prefab, parent);
            InjectIfNew(instance);
            return instance;
        }

        /// <summary>
        /// Spawns a GameObject prefab under a parent and automatically injects all [Inject] dependencies.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Transform parent, bool worldPositionStays)
        {
            var instance = Instantiate(prefab, parent, worldPositionStays);
            InjectIfNew(instance);
            return instance;
        }

        /// <summary>
        /// Spawns a GameObject prefab at position/rotation and automatically injects all [Inject] dependencies.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var instance = Instantiate(prefab, position, rotation);
            InjectIfNew(instance);
            return instance;
        }

        /// <summary>
        /// Spawns a GameObject prefab at position/rotation under a parent and automatically injects all [Inject] dependencies.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            var instance = Instantiate(prefab, position, rotation, parent);
            InjectIfNew(instance);
            return instance;
        }

        #endregion

        private static void InjectIfNew(GameObject instance)
        {
            var monoBehaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in monoBehaviours)
            {
                // Only inject if not already injected
                RequestInjection(mb);
            }
        }

        private void InjectAndTrack(MonoBehaviour mb)
        {
            Injector.Inject(mb);
            _injectedInstances.Add(mb.GetInstanceID());
        }

        private void RegisterServices(DIContainer container)
        {
            foreach (var assembly in GetAssembliesToScan())
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    var attribute = type.GetCustomAttribute<ServiceAttribute>();
                    if (attribute != null)
                    {
                        var serviceType = attribute.ServiceType ?? type;

                        if (typeof(MonoBehaviour).IsAssignableFrom(type))
                        {
                            // FindObjectsByType finds ALL scenes. We need to filter.

                            var instances = FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
                            foreach (var obj in instances)
                            {
                                var comp = obj as Component;
                                if (comp != null && comp.gameObject.scene == gameObject.scene)
                                {
                                    container.RegisterSingleton(serviceType, comp);
                                    break; // Assume 1 per scene for now
                                }
                            }

                            if (!container.IsRegistered(serviceType))
                            {
                                // Only create if we couldn't find it
                                if (instances.Length == 0) // Strictly no instance 
                                {
                                    var go = new GameObject(type.Name);
                                    // Ensure it spawns in our scene
                                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
                                    var instance = go.AddComponent(type);
                                    container.RegisterSingleton(serviceType, instance);
                                }
                            }
                        }
                        else
                        {
                            if (attribute.Lazy)
                            {
                                container.RegisterSingleton(serviceType, () => System.Activator.CreateInstance(type));
                            }
                            else
                            {
                                var instance = System.Activator.CreateInstance(type);
                                container.RegisterSingleton(serviceType, instance);
                            }
                        }
                    }
                }
            }
        }

        protected virtual IEnumerable<Assembly> GetAssembliesToScan()
        {
            yield return this.GetType().Assembly;
        }

        protected abstract void Configure(DIContainer container);
    }
}


