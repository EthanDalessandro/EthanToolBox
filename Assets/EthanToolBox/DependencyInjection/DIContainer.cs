using System;
using System.Collections.Generic;
using System.Linq;

namespace EthanToolBox.Core.DependencyInjection
{
    public class DIContainer
    {
        private readonly Dictionary<Type, Func<object>> _registrations = new Dictionary<Type, Func<object>>();
        private readonly DIContainer _parentContainer;

        public DIContainer(DIContainer parent = null)
        {
            _parentContainer = parent;
        }
        
#if UNITY_EDITOR
        // --- Editor Data ---
        // Profiling Data: Type -> Initialization Time (ms)
        public Dictionary<Type, double> InitializationTimes { get; private set; } = new Dictionary<Type, double>();

        // Cycle Detection Data: List of circular paths detected
        public List<string> DetectedCycles { get; private set; } = new List<string>();

        // Duplicate Registration Warnings
        public List<string> DuplicateWarnings { get; private set; } = new List<string>();

        // Resolution History Log
        public List<string> ResolutionLog { get; private set; } = new List<string>();
#endif
        
        // --- Dependency Graph Data ---
#if UNITY_EDITOR
        // Key: Consumer Type, Value: List of Dependencies (Types that yielded an instance)
        public Dictionary<Type, HashSet<Type>> DependencyGraph { get; private set; } = new Dictionary<Type, HashSet<Type>>();
        
        // Track the current resolution context (who is asking for a dependency?)
        private Stack<Type> _resolutionStack = new Stack<Type>();

        // Called by Injector or Resolve recursively
        public void BeginContext(Type consumerType)
        {
            // Cycle Detection
            if (_resolutionStack.Contains(consumerType))
            {
                var cyclePath = string.Join(" -> ", _resolutionStack.Reverse().Select(t => t.Name)) + " -> " + consumerType.Name;
                
#if UNITY_EDITOR
                DetectedCycles.Add(cyclePath);
                UnityEngine.Debug.LogError($"[DI] Circular dependency detected: {cyclePath}");
#endif
                throw new InvalidOperationException($"[DI] Circular dependency detected: {cyclePath}");
            }

            _resolutionStack.Push(consumerType);
            if (!DependencyGraph.ContainsKey(consumerType))
            {
                DependencyGraph[consumerType] = new HashSet<Type>();
            }
        }

        public void EndContext()
        {
            if (_resolutionStack.Count > 0)
                _resolutionStack.Pop();
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
#endif

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
#if UNITY_EDITOR
            if (_registrations.ContainsKey(serviceType))
            {
                var warning = $"Duplicate registration for {serviceType.Name}";
                DuplicateWarnings.Add(warning);
                UnityEngine.Debug.LogWarning($"[DI] {warning}");
            }
#endif
            _registrations[serviceType] = () => instance;
        }



        public object Resolve(Type serviceType)
        {
            if (_registrations.TryGetValue(serviceType, out var factory))
            {
#if UNITY_EDITOR
                TrackDependency(serviceType);
                ResolutionLog.Add($"{System.DateTime.Now:HH:mm:ss.fff} -> {serviceType.Name}");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = factory();
                sw.Stop();

                if (!InitializationTimes.ContainsKey(serviceType))
                {
                    InitializationTimes[serviceType] = sw.Elapsed.TotalMilliseconds;
                }
                return result;
#else
                return factory();
#endif
            }

            if (_parentContainer != null)
            {
                // We don't track parent dependencies in this container's graph/profiler to avoid duplication/confusion
                return _parentContainer.Resolve(serviceType);
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
            return _registrations.ContainsKey(serviceType) || (_parentContainer != null && _parentContainer.IsRegistered(serviceType));
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
#if UNITY_EDITOR
                TrackDependency(serviceType);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                service = factory();
                sw.Stop();
                
                if (!InitializationTimes.ContainsKey(serviceType))
                {
                    InitializationTimes[serviceType] = sw.Elapsed.TotalMilliseconds;
                }
                return true;
#else
                service = factory();
                return true;
#endif
            }
            
            if (_parentContainer != null)
            {
                return _parentContainer.TryResolve(serviceType, out service);
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

