using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EthanToolBox.Core.DependencyInjection
{
    [DefaultExecutionOrder(-1000)]
    [AddComponentMenu("EthanToolBox/DI/DI Bootstrapper")]
    public class DIBootstrapper : MonoBehaviour
    {
        private readonly Dictionary<Type, Func<object>> _services = new();

        private void Awake()
        {
            RegisterServices();
            InjectScene();
        }

        private void RegisterServices()
        {
            var assemblies = new[] { GetType().Assembly, GetAssemblyCSharp() };

            foreach (var assembly in assemblies)
            {
                if (assembly == null) continue;

                foreach (var type in assembly.GetTypes())
                {
                    var attr = type.GetCustomAttribute<ServiceAttribute>();
                    if (attr == null) continue;

                    var key = attr.ServiceType ?? type;

                    if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        var found = FindFirstObjectByType(type) as Component;
                        if (found == null)
                        {
                            var go = new GameObject(type.Name);
                            found = go.AddComponent(type);
                        }
                        _services[key] = () => found;
                    }
                    else
                    {
                        var instance = Activator.CreateInstance(type);
                        _services[key] = () => instance;
                    }
                }
            }
        }

        private void InjectScene()
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == this) continue;

                var type = mb.GetType();

                foreach (var field in type.GetFields(flags))
                {
                    var attr = field.GetCustomAttribute<InjectAttribute>();
                    if (attr == null) continue;

                    if (_services.TryGetValue(field.FieldType, out var factory))
                        field.SetValue(mb, factory());
                    else if (!attr.Optional)
                        Debug.LogError($"[DI] {field.FieldType.Name} not registered (required by {type.Name}.{field.Name})");
                }

                foreach (var prop in type.GetProperties(flags))
                {
                    if (!prop.CanWrite) continue;
                    var attr = prop.GetCustomAttribute<InjectAttribute>();
                    if (attr == null) continue;

                    if (_services.TryGetValue(prop.PropertyType, out var factory))
                        prop.SetValue(mb, factory());
                    else if (!attr.Optional)
                        Debug.LogError($"[DI] {prop.PropertyType.Name} not registered (required by {type.Name}.{prop.Name})");
                }
            }
        }

        private static Assembly GetAssemblyCSharp()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                if (asm.GetName().Name == "Assembly-CSharp") return asm;
            return null;
        }
    }
}
