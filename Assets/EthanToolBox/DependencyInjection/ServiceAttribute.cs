using System;

namespace EthanToolBox.DependencyInjection
{
    // Marque une classe comme "service".
    // Le DIBootstrapper va automatiquement l'enregistrer au démarrage.
    //
    // Exemple :
    //   [Service]
    //   public class AudioManager : MonoBehaviour { ... }
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute : Attribute { }
}
