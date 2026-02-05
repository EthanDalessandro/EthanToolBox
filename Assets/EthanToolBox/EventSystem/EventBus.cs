using System;
using System.Collections.Generic;

namespace EthanToolBox.Core.EventSystem
{
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Subscribe<T>(Action handler);
        void Unsubscribe<T>(Action handler);
        void Fire<T>(T signal);
    }

    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate> _subscribers = new Dictionary<Type, Delegate>();

        public void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type)) _subscribers[type] = null;
            _subscribers[type] = Delegate.Combine(_subscribers[type], handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var d))
            {
                var current = Delegate.Remove(d, handler);
                if (current == null) _subscribers.Remove(type);
                else _subscribers[type] = current;
            }
        }

        // --- Parameterless Support (Action) ---

        public void Subscribe<T>(Action handler)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type)) _subscribers[type] = null;
            // We wrap the Action in an Action<T> to store it comfortably in the same dictionary system
            // Or simpler: We just store it as Action and Invoke handles it?
            // Problem: Delegate.Combine needs same type.
            // Solution: We create a wrapper: (T _) => handler()
            Action<T> wrapper = (_) => handler();
            
            // To be able to Unsubscribe later, we need to map the original handler to the wrapper.
            // This adds complexity (need a Dictionary<Action, Action<T>> cache).
            // A simpler approach for lightweight system:
            // Store separate dictionaries? Or just allow the user to use the lambda syntax?
            
            // Wait, the user wants cleaner syntax. 
            // If we use wrapper, Unsubscribe(handler) won't work easily.
            
            // Let's implement a secondary dictionary for parameterless handlers for mix-and-match safety.
            var key = typeof(Wrapper<T>);
            if (!_subscribers.ContainsKey(key)) _subscribers[key] = null;
            _subscribers[key] = Delegate.Combine(_subscribers[key], handler);
        }

        public void Unsubscribe<T>(Action handler)
        {
             var key = typeof(Wrapper<T>);
             if (_subscribers.TryGetValue(key, out var d))
             {
                 var current = Delegate.Remove(d, handler);
                 if (current == null) _subscribers.Remove(key);
                 else _subscribers[key] = current;
             }
        }

        // Helper type to differentiate keys in the dictionary
        private class Wrapper<T> {}

        public void Fire<T>(T signal)
        {
            var type = typeof(T);
            
            // 1. Call Handlers with Payload (Action<T>)
            if (_subscribers.TryGetValue(type, out var d))
            {
                var callback = d as Action<T>;
                callback?.Invoke(signal);
            }

            // 2. Call Handlers without Payload (Action)
            var key = typeof(Wrapper<T>);
            if (_subscribers.TryGetValue(key, out var dVoid))
            {
                var callback = dVoid as Action;
                callback?.Invoke();
            }
        }
    }
}
