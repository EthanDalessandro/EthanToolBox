using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EthanToolBox.Core.DependencyInjection
{
    // S'exécute avant tous les autres MonoBehaviours (Awake order = -1000)
    [DefaultExecutionOrder(-1000)]
    [AddComponentMenu("EthanToolBox/DI/DI Bootstrapper")]
    public class DIBootstrapper : MonoBehaviour
    {
        // Dictionnaire : Type → instance du service
        private readonly Dictionary<Type, object> _services = new();

        private void Awake()
        {
            // Parcourt tous les MonoBehaviours de la scène pour trouver les [Service]
            MonoBehaviour[] allMB = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            RegisterServices(allMB);
            InjectScene(allMB);
        }

        private void RegisterServices(MonoBehaviour[] allMB)
        {
            foreach (MonoBehaviour mb in allMB)
            {
                if (mb == this) continue;

                //GetType retourne le type concret et getcustomattribute va venir chercher l'attribut en question
                
                ServiceAttribute attribute = mb.GetType().GetCustomAttribute<ServiceAttribute>();
                if (attribute == null) continue;

                // La clé = l'interface déclarée, ou le type lui-même si pas précisé
                Type key = attribute.ServiceType ?? mb.GetType();
                _services[key] = mb;
            }
        }

        private void InjectScene(MonoBehaviour[] allMB)
        {
            // Flags pour accéder aux membres publics ET privés des instances
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // Parcourt tous les MonoBehaviours de la scène (actifs et inactifs)
            foreach (MonoBehaviour mb in allMB)
            {
                if (mb == this) continue;

                Type type = mb.GetType();

                //  Injection dans les fields 
                foreach (FieldInfo field in type.GetFields(flags))
                {
                    InjectAttribute attribute = field.GetCustomAttribute<InjectAttribute>();
                    if (attribute == null) continue;

                    if (_services.TryGetValue(field.FieldType, out object service))
                        field.SetValue(mb, service); // Injecte l'instance dans le field
                    else if (!attribute.Optional)
                        Debug.LogError($"[DI] {field.FieldType.Name} not registered (required by {type.Name}.{field.Name})");
                }

                //  Injection dans les properties 
                foreach (PropertyInfo prop in type.GetProperties(flags))
                {
                    if (!prop.CanWrite) continue;
                    InjectAttribute attribute = prop.GetCustomAttribute<InjectAttribute>();
                    if (attribute == null) continue;

                    if (_services.TryGetValue(prop.PropertyType, out object service))
                        prop.SetValue(mb, service); // Injecte l'instance dans la property
                    else if (!attribute.Optional)
                        Debug.LogError($"[DI] {prop.PropertyType.Name} not registered (required by {type.Name}.{prop.Name})");
                }
            }
        }
    }
}
