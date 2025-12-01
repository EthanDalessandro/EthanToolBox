using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace EthanToolBox.Core.DependencyInjection
{
    [AddComponentMenu("EthanToolBox/DI/Default Composition Root")]
    public class DefaultCompositionRoot : DICompositionRoot
    {
        protected override void Configure(DIContainer container)
        {
            // Default implementation does nothing
        }

        protected override IEnumerable<Assembly> GetAssembliesToScan()
        {
            // Always scan the package assembly
            yield return typeof(DefaultCompositionRoot).Assembly;

            // Try to scan Assembly-CSharp
            Assembly assemblyCSharp = null;
            try
            {
                // Iterate over all loaded assemblies to find Assembly-CSharp
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "Assembly-CSharp")
                    {
                        assemblyCSharp = asm;
                        break;
                    }
                }
            }
            catch { }

            if (assemblyCSharp != null)
            {
                yield return assemblyCSharp;
            }
        }
    }
}
