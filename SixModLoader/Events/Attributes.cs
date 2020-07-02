using System;

namespace SixModLoader.Events
{
    [AttributeUsage(AttributeTargets.Method)]
    public class EventHandlerAttribute : Attribute
    {
        public bool AutomaticEventType { get; set; }
        public Type EventType { get; set; }

        public EventHandlerAttribute(Type eventType)
        {
            EventType = eventType;
        }

        public EventHandlerAttribute()
        {
            AutomaticEventType = true;
        }
    }
}
