using System;
using System.Collections.Generic;
using System.Linq;

namespace EthanToolBox.Core.DependencyInjection
{
    public class DIContainer
    {
        private readonly Dictionary<Type, Func<object>> _registrations = new Dictionary<Type, Func<object>>();

        public void RegisterSingleton<TService>(TService instance)
        {
            _registrations[typeof(TService)] = () => instance;
        }

        public void RegisterSingleton<TService>(Func<TService> factory)
        {
            var lazy = new Lazy<TService>(factory);
            _registrations[typeof(TService)] = () => lazy.Value;
        }

        public void RegisterTransient<TService>(Func<TService> factory)
        {
            _registrations[typeof(TService)] = () => factory();
        }

        public void RegisterSingleton(Type serviceType, Func<object> factory)
        {
            var lazy = new Lazy<object>(factory);
            _registrations[serviceType] = () => lazy.Value;
        }

        public void RegisterSingleton(Type serviceType, object instance)
        {
            _registrations[serviceType] = () => instance;
        }

        public object Resolve(Type serviceType)
        {
            if (_registrations.TryGetValue(serviceType, out var factory))
            {
                return factory();
            }

            throw new Exception($"Service of type {serviceType.Name} is not registered.");
        }

        public TService Resolve<TService>()
        {
            return (TService)Resolve(typeof(TService));
        }

        /// <summary>
        /// Check if a service type is registered.
        /// </summary>
        public bool IsRegistered<TService>()
        {
            return IsRegistered(typeof(TService));
        }

        public bool IsRegistered(Type serviceType)
        {
            return _registrations.ContainsKey(serviceType);
        }

        /// <summary>
        /// Try to resolve a service without throwing an exception.
        /// Returns true if successful, false if not registered.
        /// </summary>
        public bool TryResolve<TService>(out TService service)
        {
            if (TryResolve(typeof(TService), out var obj))
            {
                service = (TService)obj;
                return true;
            }
            service = default;
            return false;
        }

        public bool TryResolve(Type serviceType, out object service)
        {
            if (_registrations.TryGetValue(serviceType, out var factory))
            {
                service = factory();
                return true;
            }
            service = null;
            return false;
        }

        /// <summary>
        /// Resolve all services that are assignable to the given type.
        /// </summary>
        public List<T> ResolveAll<T>()
        {
            var results = new List<T>();
            var targetType = typeof(T);

            foreach (var kvp in _registrations)
            {
                if (targetType.IsAssignableFrom(kvp.Key))
                {
                    var instance = kvp.Value();
                    if (instance is T typed)
                    {
                        results.Add(typed);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Get all registered service types. Used for debugging.
        /// </summary>
        public IEnumerable<Type> GetAllRegisteredTypes()
        {
            return _registrations.Keys;
        }

        /// <summary>
        /// Get the instance for a registered type. Used for debugging.
        /// </summary>
        public object GetInstance(Type serviceType)
        {
            if (_registrations.TryGetValue(serviceType, out var factory))
            {
                return factory();
            }
            return null;
        }
    }
}

