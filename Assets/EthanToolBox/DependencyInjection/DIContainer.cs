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
    }
}
