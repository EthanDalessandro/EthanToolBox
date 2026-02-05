using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace EthanToolBox.Core.DependencyInjection
{
    [DefaultExecutionOrder(-1000)]
    public abstract class DICompositionRoot : MonoBehaviour
    {
        public static Injector Instance { get; private set; }
        private static HashSet<int> _injectedInstances = new HashSet<int>();

        protected DIContainer Container;
        protected Injector Injector;

        protected virtual void Awake()
        {
            Container = new DIContainer();
            Injector = new Injector(Container);
            Instance = Injector;
            _injectedInstances.Clear();

            Configure(Container);
            RegisterServices(Container);

            // Inject into all MonoBehaviours in the scene
            var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mb in allMonoBehaviours)
            {
                if (mb == this) continue;
                InjectAndTrack(mb);
            }
        }

        /// <summary>
        /// Call this from OnEnable to request late injection.
        /// Safe to call multiple times - will only inject once.
        /// </summary>
        public static void RequestInjection(MonoBehaviour target)
        {
            if (Instance == null || target == null) return;

            int id = target.GetInstanceID();
            if (!_injectedInstances.Contains(id))
            {
                Instance.Inject(target);
                _injectedInstances.Add(id);
            }
        }

        private static void InjectAndTrack(MonoBehaviour mb)
        {
            Instance.Inject(mb);
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
                            var instance = FindFirstObjectByType(type, FindObjectsInactive.Include);
                            if (instance != null)
                            {
                                container.RegisterSingleton(serviceType, instance);
                            }
                            else
                            {
                                var go = new GameObject(type.Name);
                                instance = go.AddComponent(type);
                                container.RegisterSingleton(serviceType, instance);
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


