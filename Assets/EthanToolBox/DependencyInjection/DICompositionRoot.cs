using UnityEngine;

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

        protected abstract void Configure(DIContainer container);
    }
}
