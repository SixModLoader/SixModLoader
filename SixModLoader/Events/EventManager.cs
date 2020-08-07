using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace SixModLoader.Events
{
    public class EventHandler
    {
        public Type EventType { get; }

        [CanBeNull]
        public object Instance { get; }

        [NotNull]
        public MethodInfo Method { get; }

        public int Priority { get; }

        public EventHandler(Type eventType, [CanBeNull] object instance, [NotNull] MethodInfo method)
        {
            EventType = eventType;
            Instance = instance;
            Method = method;
            Priority = method.GetCustomAttribute<PriorityAttribute>()?.Priority ?? (int) global::SixModLoader.Priority.Normal;
        }

        public override string ToString()
        {
            return Method.GetType().FullName ?? base.ToString();
        }
    }

    public class EventManager
    {
        public static EventManager Instance { get; private set; }

        public EventManager()
        {
            Instance = this;
        }

        public Dictionary<Type, List<EventHandler>> Handlers { get; } = new Dictionary<Type, List<EventHandler>>();

        public void Broadcast(Event @event)
        {
            foreach (var list in Handlers.Where(x => x.Key.IsInstanceOfType(@event)).Select(x => x.Value))
            {
                @event.Call(list);
            }

            Logger.Debug($"Called {@event} in {@event.Handlers.Count} handler(s)");
        }

        public void Register(object obj)
        {
            Register(obj.GetType(), obj);
        }

        public void RegisterStatic(Type type)
        {
            Register(type, null);
        }

        public void Register(Type type, object obj)
        {
            foreach (var methodInfo in type.GetMethods(AccessTools.all))
            {
                var attribute = methodInfo.GetCustomAttribute<EventHandlerAttribute>();
                if (attribute == null) continue;

                if (attribute.AutomaticEventType)
                {
                    attribute.EventType = methodInfo.GetParameters()[0].ParameterType;
                }

                if (methodInfo.GetParameters().Length > 0 && methodInfo.GetParameters()[0].ParameterType != attribute.EventType)
                    continue;

                if ((!methodInfo.IsStatic && obj == null) || (methodInfo.IsStatic && obj != null))
                    continue;

                var list = Handlers.GetValueSafe(attribute.EventType) ?? new List<EventHandler>();
                Handlers[attribute.EventType] = list;

                list.Add(new EventHandler(type, obj, methodInfo));
                Logger.Debug($"Registered {attribute.EventType} handler {methodInfo.Name}");
            }
        }

        public void Unregister(object obj)
        {
            Unregister(obj.GetType(), obj);
        }

        public void UnregisterStatic(Type type)
        {
            Unregister(type, null);
        }

        public void Unregister(Type type, object obj)
        {
            foreach (var pair in Handlers)
            {
                pair.Value.RemoveAll(x => x.Method.DeclaringType == type && x.Instance == obj);
            }
        }
    }
}