using System;

namespace EthanToolBox.Core.DependencyInjection
{
    /// <summary>
    /// Injects all registered instances of the element type into a List or Array.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAllAttribute : Attribute
    {
    }
}
