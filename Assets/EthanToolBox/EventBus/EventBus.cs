using System;
using System.Collections.Generic;

namespace EthanToolBox.Core.EventBus
{
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate> _subscribers = new Dictionary<Type, Delegate>();

        public void Subscribe<TEvent>(Action<TEvent> listener) where TEvent : struct
        {
            var type = typeof(TEvent);
            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = null;
            }
            _subscribers[type] = Delegate.Combine(_subscribers[type], listener);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> listener) where TEvent : struct
        {
            var type = typeof(TEvent);
            if (_subscribers.ContainsKey(type))
            {
                var currentDel = _subscribers[type];
                _subscribers[type] = Delegate.Remove(currentDel, listener);
                
                if (_subscribers[type] == null)
                {
                    _subscribers.Remove(type);
                }
            }
        }

        public void Raise<TEvent>(TEvent eventItem) where TEvent : struct
        {
            var type = typeof(TEvent);
            if (_subscribers.TryGetValue(type, out var del))
            {
                var callback = del as Action<TEvent>;
                callback?.Invoke(eventItem);
            }
        }
    }
}
