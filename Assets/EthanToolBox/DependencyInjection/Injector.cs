using System;
using System.Reflection;
using UnityEngine;

namespace EthanToolBox.Core.DependencyInjection
{
    public class Injector
    {
        private readonly DIContainer _container;

        public Injector(DIContainer container)
        {
            _container = container;
        }

        public void Inject(object target)
        {
            var type = target.GetType();

            // 1. Inject Fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.IsDefined(typeof(InjectAttribute), true))
                {
                    var service = _container.Resolve(field.FieldType);
                    field.SetValue(target, service);
                }
            }

            // 2. Inject Properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (property.IsDefined(typeof(InjectAttribute), true) && property.CanWrite)
                {
                    var service = _container.Resolve(property.PropertyType);
                    property.SetValue(target, service);
                }
            }

            // 3. Inject Methods
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (method.IsDefined(typeof(InjectAttribute), true))
                {
                    var parameters = method.GetParameters();
                    var args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        args[i] = _container.Resolve(parameters[i].ParameterType);
                    }
                    method.Invoke(target, args);
                }
            }
        }
    }
}
