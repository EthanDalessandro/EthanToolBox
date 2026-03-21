using System;

namespace EthanToolBox.Core.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ServiceAttribute : Attribute
    {
        public Type ServiceType { get; }

        public ServiceAttribute(Type serviceType = null)
        {
            ServiceType = serviceType;
        }
    }
}
