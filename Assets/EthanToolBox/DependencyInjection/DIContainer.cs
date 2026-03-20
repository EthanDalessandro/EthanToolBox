using System;
using System.Collections.Generic;
using UnityEngine;

namespace EthanToolBox.DependencyInjection
{
    // Stocke tous les services enregistrés.
    // C'est simplement un dictionnaire : Type → instance.
    public class DIContainer
    {
        // Clé   : le type du service (ex: typeof(AudioManager))
        // Valeur: l'instance du service
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        // Enregistre un service dans le container.
        // Si le type est déjà enregistré, on log un avertissement.
        public void Register(Type type, object instance)
        {
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[DI] '{type.Name}' est déjà enregistré. L'ancienne instance sera remplacée.");
            }

            _services[type] = instance;
        }

        // Récupère un service par son type.
        // Lance une exception si le service n'est pas trouvé.
        public object Resolve(Type type)
        {
            if (_services.TryGetValue(type, out var instance))
                return instance;

            throw new Exception($"[DI] Service '{type.Name}' introuvable. As-tu bien ajouté [Service] sur la classe ?");
        }

        // Récupère un service, ou null si non trouvé (version sans exception).
        public bool TryResolve(Type type, out object instance)
        {
            return _services.TryGetValue(type, out instance);
        }
    }
}
