using System;

namespace EthanToolBox.Core.EventBus
{
    public interface IEventBus
    {
        /// <summary>
        /// Subscribes a listener to a specific event type.
        /// </summary>
        void Subscribe<TEvent>(Action<TEvent> listener) where TEvent : struct;

        /// <summary>
        /// Unsubscribes a listener from a specific event type.
        /// </summary>
        void Unsubscribe<TEvent>(Action<TEvent> listener) where TEvent : struct;

        /// <summary>
        /// Raises an event, invoking all subscribed listeners.
        /// </summary>
        void Raise<TEvent>(TEvent eventItem) where TEvent : struct;
    }
}
