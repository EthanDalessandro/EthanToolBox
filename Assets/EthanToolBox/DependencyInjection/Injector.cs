using System;
using System.Collections;
using System.Collections.Generic;
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
#if UNITY_EDITOR
            _container.BeginContext(type);
#endif

            try
            {
                // 1. Inject Fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                // Handle [InjectAll]
                if (field.IsDefined(typeof(InjectAllAttribute), true))
                {
                    InjectAllToField(target, field);
                    continue;
                }

                // Handle [Inject]
                var injectAttr = field.GetCustomAttribute<InjectAttribute>(true);
                if (injectAttr != null)
                {
                    InjectToField(target, field, injectAttr.Optional);
                }
            }

            // 2. Inject Properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (!property.CanWrite) continue;

                // Handle [InjectAll]
                if (property.IsDefined(typeof(InjectAllAttribute), true))
                {
                    InjectAllToProperty(target, property);
                    continue;
                }

                // Handle [Inject]
                var injectAttr = property.GetCustomAttribute<InjectAttribute>(true);
                if (injectAttr != null)
                {
                    InjectToProperty(target, property, injectAttr.Optional);
                }
            }

            // 3. Inject Methods
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var injectAttr = method.GetCustomAttribute<InjectAttribute>(true);
                if (injectAttr != null)
                {
                    InjectToMethod(target, method, injectAttr.Optional);
                }
            }
            }
            finally
            {
#if UNITY_EDITOR
                _container.EndContext();
#endif
            }
        }

        private void InjectToField(object target, FieldInfo field, bool optional)
        {
            if (_container.TryResolve(field.FieldType, out var service))
            {
                field.SetValue(target, service);
            }
            else if (!optional)
            {
                throw new Exception($"[DI] Cannot resolve required service: {field.FieldType.Name} for {target.GetType().Name}.{field.Name}");
            }
            // If optional and not found, leave as null/default
        }

        private void InjectToProperty(object target, PropertyInfo property, bool optional)
        {
            if (_container.TryResolve(property.PropertyType, out var service))
            {
                property.SetValue(target, service);
            }
            else if (!optional)
            {
                throw new Exception($"[DI] Cannot resolve required service: {property.PropertyType.Name} for {target.GetType().Name}.{property.Name}");
            }
        }

        private void InjectToMethod(object target, MethodInfo method, bool optional)
        {
            var parameters = method.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                if (_container.TryResolve(parameters[i].ParameterType, out var service))
                {
                    args[i] = service;
                }
                else if (!optional)
                {
                    throw new Exception($"[DI] Cannot resolve required service: {parameters[i].ParameterType.Name} for {target.GetType().Name}.{method.Name}()");
                }
            }

            method.Invoke(target, args);
        }

        private void InjectAllToField(object target, FieldInfo field)
        {
            var elementType = GetElementType(field.FieldType);
            if (elementType == null)
            {
                Debug.LogWarning($"[DI] [InjectAll] requires List<T> or T[] type. Field: {field.Name}");
                return;
            }

            var list = ResolveAllAsList(elementType);

            if (field.FieldType.IsArray)
            {
                var array = Array.CreateInstance(elementType, list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    array.SetValue(list[i], i);
                }
                field.SetValue(target, array);
            }
            else
            {
                field.SetValue(target, list);
            }
        }

        private void InjectAllToProperty(object target, PropertyInfo property)
        {
            var elementType = GetElementType(property.PropertyType);
            if (elementType == null)
            {
                Debug.LogWarning($"[DI] [InjectAll] requires List<T> or T[] type. Property: {property.Name}");
                return;
            }

            var list = ResolveAllAsList(elementType);

            if (property.PropertyType.IsArray)
            {
                var array = Array.CreateInstance(elementType, list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    array.SetValue(list[i], i);
                }
                property.SetValue(target, array);
            }
            else
            {
                property.SetValue(target, list);
            }
        }

        private Type GetElementType(Type collectionType)
        {
            if (collectionType.IsArray)
            {
                return collectionType.GetElementType();
            }

            if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return collectionType.GetGenericArguments()[0];
            }

            return null;
        }

        private IList ResolveAllAsList(Type elementType)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType);

            foreach (var type in _container.GetAllRegisteredTypes())
            {
                if (elementType.IsAssignableFrom(type))
                {
                    var instance = _container.GetInstance(type);
                    if (instance != null)
                    {
                        list.Add(instance);
                    }
                }
            }

            return list;
        }
    }
}
