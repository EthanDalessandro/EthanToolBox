using System;

namespace EthanToolBox.Core.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class InjectAttribute : Attribute
    {
        /// <summary>
        /// If true, the field will be set to null/default if the service is not registered.
        /// If false (default), an exception will be thrown.
        /// </summary>
        public bool Optional { get; set; } = false;
    }
}

