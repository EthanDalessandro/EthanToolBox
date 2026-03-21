using System;

namespace EthanToolBox.DependencyInjection
{
    // Marque une classe comme "service".
    // Le DIBootstrapper va automatiquement l'enregistrer au démarrage.
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute : Attribute { }
}
