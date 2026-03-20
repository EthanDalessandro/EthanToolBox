using System;

namespace EthanToolBox.DependencyInjection
{
    // Marque un champ à injecter automatiquement.
    // Le DIBootstrapper va remplir ce champ avec le service correspondant.
    //
    // Exemple :
    //   [Inject] private AudioManager _audio;
    //
    // Si optional = true, pas d'erreur si le service n'est pas trouvé.
    //   [Inject(optional: true)] private AudioManager _audio;
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
