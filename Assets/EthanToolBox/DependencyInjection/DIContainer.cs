using System;
using System.Collections.Generic;
using System.Linq;

namespace EthanToolBox.Core.DependencyInjection
{
    public partial class DIContainer
    {
        private readonly Dictionary<Type, Func<object>> _registrations = new();
        private readonly DIContainer _parentContainer;

        public DIContainer(DIContainer parent = null)
        {
            _parentContainer = parent;
        }

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
            CheckDuplicateRegistration(serviceType);
            _registrations[serviceType] = () => instance;
        }

        public object Resolve(Type serviceType)
        {
            if (_registrations.TryGetValue(serviceType, out var factory))
            {
                OnResolveStart(serviceType);
                var result = factory();
                OnResolveEnd(serviceType);
                return result;
            }

            if (_parentContainer != null)
            {
                return _parentContainer.Resolve(serviceType);
            }

            throw new Exception($"Service of type {serviceType.Name} is not registered.");
        }

        public TService Resolve<TService>()
        {
            return (TService)Resolve(typeof(TService));
        }

        public bool IsRegistered<TService>()
        {
            return IsRegistered(typeof(TService));
        }

        public bool IsRegistered(Type serviceType)
        {
            return _registrations.ContainsKey(serviceType) || (_parentContainer != null && _parentContainer.IsRegistered(serviceType));
        }

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
                OnResolveStart(serviceType);
                service = factory();
                OnResolveEnd(serviceType);
                return true;
            }

            if (_parentContainer != null)
            {
                return _parentContainer.TryResolve(serviceType, out service);
            }

            service = null;
            return false;
        }

        public List<T> ResolveAll<T>()
        {
            var results = new List<T>();
            var targetType = typeof(T);

            foreach (var kvp in _registrations)
            {
                if (targetType.IsAssignableFrom(kvp.Key))
                {
                    OnResolveStart(kvp.Key);
                    var instance = kvp.Value();
                    OnResolveEnd(kvp.Key);

                    if (instance is T typed)
                    {
                        results.Add(typed);
                    }
                }
            }
            return results;
        }

        public IEnumerable<Type> GetAllRegisteredTypes()
        {
            return _registrations.Keys;
        }

        public object GetInstance(Type serviceType)
        {
            if (_registrations.TryGetValue(serviceType, out var factory))
            {
                return factory();
            }
            return null;
        }

        partial void CheckDuplicateRegistration(Type serviceType);
        partial void OnResolveStart(Type serviceType);
        partial void OnResolveEnd(Type serviceType);

        public void BeginContext(Type consumerType)
        {
            OnBeginContext(consumerType);
        }

        public void EndContext()
        {
            OnEndContext();
        }

        partial void OnBeginContext(Type consumerType);
        partial void OnEndContext();
    }

#if UNITY_EDITOR
    public partial class DIContainer
    {
        // --- Data ---
        public Dictionary<Type, double> InitializationTimes { get; private set; } = new Dictionary<Type, double>();
        public List<string> DetectedCycles { get; private set; } = new List<string>();
        public List<string> DuplicateWarnings { get; private set; } = new List<string>();
        public Dictionary<Type, HashSet<Type>> DependencyGraph { get; private set; } = new Dictionary<Type, HashSet<Type>>();

        // --- Internal State ---
        private Stack<Type> _resolutionStack = new Stack<Type>();
        private System.Diagnostics.Stopwatch _stopwatch;

        // --- Implementations ---

        partial void CheckDuplicateRegistration(Type serviceType)
        {
            if (_registrations.ContainsKey(serviceType))
            {
                var warning = $"Duplicate registration for {serviceType.Name}";
                DuplicateWarnings.Add(warning);
                UnityEngine.Debug.LogWarning($"[DI] {warning}");
            }
        }

        partial void OnBeginContext(Type consumerType)
        {
            // Cycle Detection
            if (_resolutionStack.Contains(consumerType))
            {
                var cyclePath = string.Join(" -> ", _resolutionStack.Reverse().Select(t => t.Name)) + " -> " + consumerType.Name;
                DetectedCycles.Add(cyclePath);
                UnityEngine.Debug.LogError($"[DI] Circular dependency detected: {cyclePath}");
                throw new InvalidOperationException($"[DI] Circular dependency detected: {cyclePath}");
            }

            _resolutionStack.Push(consumerType);
            if (!DependencyGraph.ContainsKey(consumerType))
            {
                DependencyGraph[consumerType] = new HashSet<Type>();
            }
        }

        partial void OnEndContext()
        {
            if (_resolutionStack.Count > 0)
                _resolutionStack.Pop();
        }

        partial void OnResolveStart(Type serviceType)
        {
            TrackDependency(serviceType);
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        partial void OnResolveEnd(Type serviceType)
        {
            _stopwatch?.Stop();
            if (_stopwatch != null && !InitializationTimes.ContainsKey(serviceType))
            {
                InitializationTimes[serviceType] = _stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        private void TrackDependency(Type resolvedServiceType)
        {
            if (_resolutionStack.Count > 0)
            {
                var consumer = _resolutionStack.Peek();
                if (DependencyGraph.ContainsKey(consumer))
                {
                    DependencyGraph[consumer].Add(resolvedServiceType);
                }
            }
        }
    }
#endif
}

