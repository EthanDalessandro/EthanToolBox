using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace EthanToolBox.Core.DependencyInjection
{
    [DefaultExecutionOrder(-1000)]
    public abstract class DICompositionRoot : MonoBehaviour
    {
        // Track all active roots to find parents/contexts
        private static readonly List<DICompositionRoot> _activeRoots = new List<DICompositionRoot>();
        private static HashSet<int> _injectedInstances = new HashSet<int>();

        [Header("Context Settings")]
        [Tooltip("If true, this Root will persist across scenes and act as a Global Parent.")]
        [SerializeField] private bool _isGlobal = false;

        public DIContainer Container { get; protected set; } // Changed to public for lookup
        public Injector Injector { get; protected set; }     // Changed to public

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

            // 1. Find Parent Container (Global)
            DIContainer parentContainer = null;
            if (!_isGlobal)
            {
                var globalRoot = _activeRoots.Find(r => r._isGlobal);
                if (globalRoot != null)
                {
                    parentContainer = globalRoot.Container;
                }
            }

            // 2. Create Container (with optional parent)
            Container = new DIContainer(parentContainer);
            Injector = new Injector(Container);
            _injectedInstances.Clear();

            // Default Registrations
            Container.RegisterSingleton(typeof(EthanToolBox.Core.EventSystem.IEventBus), new EthanToolBox.Core.EventSystem.EventBus());

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

                        // Only register if not already registered (to avoid Global vs Scene conflicts? Or override?)
                        // Strategy: Local overrides Parent.
                        // But here we are registering.
                        
                        if (typeof(MonoBehaviour).IsAssignableFrom(type))
                        {
                            // MB Service: Only register if it exists in THIS scene (or Global if we are Global)
                            // FindObjectsByType finds ALL scenes. We need to filter.
                            
                            var instances = FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
                            foreach(var obj in instances) 
                            {
                                var comp = obj as Component;
                                if (comp != null && comp.gameObject.scene == gameObject.scene)
                                {
                                    container.RegisterSingleton(serviceType, comp);
                                    break; // Assume 1 per scene for now
                                }
                            }
                            
                            // If not found and we are supposed to create it? 
                            // Current logic only created new GO if not found.
                            // We should probably only create it if we are the rightful owner.
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


