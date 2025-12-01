using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace EthanToolBox.Core.DependencyInjection
{
    [DefaultExecutionOrder(-1000)]
    public abstract class DICompositionRoot : MonoBehaviour
    {
        protected DIContainer Container;
        protected Injector Injector;

        protected virtual void Awake()
        {
            Container = new DIContainer();
            Injector = new Injector(Container);

            Configure(Container);
            RegisterServices(Container);

            // Inject into all MonoBehaviours in the scene (optional, but useful for auto-injection)
            // For better performance, you might want to manually register objects to inject.
            var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in allMonoBehaviours)
            {
                // Skip the CompositionRoot itself to avoid circular issues or double init if not careful
                if (mb == this) continue;
                
                Injector.Inject(mb);
            }
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
                            // It's a MonoBehaviour, try to find it in the scene
                            var instance = FindFirstObjectByType(type);
                            if (instance != null)
                            {
                                container.RegisterSingleton(serviceType, instance);
                            }
                            else
                            {
                                // Optional: Create it if missing? 
                                // For now, let's create a new GameObject for it.
                                var go = new GameObject(type.Name);
                                instance = go.AddComponent(type);
                                container.RegisterSingleton(serviceType, instance);
                            }
                        }
                        else
                        {
                            // It's a normal class
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
