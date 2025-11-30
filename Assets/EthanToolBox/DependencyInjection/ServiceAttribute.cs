using System;

namespace EthanToolBox.Core.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ServiceAttribute : Attribute
    {
        public Type ServiceType { get; }
        public bool Lazy { get; }

        public ServiceAttribute(Type serviceType = null, bool lazy = true)
        {
            ServiceType = serviceType;
            Lazy = lazy;
        }
    }
}
