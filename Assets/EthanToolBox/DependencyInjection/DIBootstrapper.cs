using System.Reflection;
using UnityEngine;

namespace EthanToolBox.DependencyInjection
{
    // Point d'entrée du système DI.
    // Ce MonoBehaviour doit être présent dans chaque scène (sur un GameObject vide).
    //
    // Il s'exécute AVANT tous les autres scripts grâce à DefaultExecutionOrder(-1000).
    // Au démarrage il fait deux passes :
    //   1. Trouve tous les [Service] dans la scène → les enregistre dans le container
    //   2. Trouve tous les [Inject] dans la scène  → les remplit avec les services trouvés
    [DefaultExecutionOrder(-1000)]
    public class DIBootstrapper : MonoBehaviour
    {
        private DIContainer _container;

        private void Awake()
        {
            _container = new DIContainer();

            // Récupère tous les MonoBehaviour présents dans la scène
            var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            // --- Passe 1 : enregistrement des services ---
            foreach (var behaviour in allBehaviours)
            {
                // Vérifie si la classe a l'attribut [Service]
                var hasService = behaviour.GetType().IsDefined(typeof(ServiceAttribute), inherit: true);
                if (hasService)
                {
                    _container.Register(behaviour.GetType(), behaviour);
                    Debug.Log($"[DI] Service enregistré : {behaviour.GetType().Name}");
                }
            }

            // --- Passe 2 : injection dans les consommateurs ---
            foreach (var behaviour in allBehaviours)
            {
                InjectInto(behaviour);
            }
        }

        // Parcourt les champs du MonoBehaviour et injecte les services marqués [Inject].
        private void InjectInto(MonoBehaviour target)
        {
            // On récupère tous les champs (publics et privés) de la classe
            var fields = target.GetType().GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            foreach (var field in fields)
            {
                // On cherche l'attribut [Inject] sur ce champ
                var injectAttr = field.GetCustomAttribute<InjectAttribute>();
                if (injectAttr == null) continue;

                // On essaie de trouver le service correspondant au type du champ
                if (_container.TryResolve(field.FieldType, out var service))
                {
                    field.SetValue(target, service);
                }
                else if (!injectAttr.Optional)
                {
                    // Si le champ n'est pas optionnel, on affiche une erreur claire
                    Debug.LogError(
                        $"[DI] Impossible d'injecter '{field.FieldType.Name}' dans '{target.GetType().Name}'. " +
                        $"As-tu ajouté [Service] sur '{field.FieldType.Name}' et placé le GameObject dans la scène ?"
                    );
                }
            }
        }
    }
}
