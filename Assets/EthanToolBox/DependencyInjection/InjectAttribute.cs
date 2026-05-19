using System;

namespace EthanToolBox.DependencyInjection
{
    // Marque un champ à injecter automatiquement.
    // Le DIBootstrapper va remplir ce champ avec le service correspondant.
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectAttribute : Attribute
    {
        public bool Optional { get; }

        public InjectAttribute(bool optional = false)
        {
            Optional = optional;
        }
    }
}
