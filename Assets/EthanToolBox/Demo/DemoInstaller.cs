using EthanToolBox.Core.DependencyInjection;
using UnityEngine;

namespace EthanToolBox.Demo
{
    public class DemoInstaller : CompositionRoot
    {
        protected override void Configure(DIContainer container)
        {
            // Register the service as a Singleton
            container.RegisterSingleton<IDemoService>(new DemoService());
            
            Debug.Log("DemoInstaller: Services Registered.");
        }
    }
}
