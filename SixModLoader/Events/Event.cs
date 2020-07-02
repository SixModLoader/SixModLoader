using System;
using System.Collections.Generic;
using System.Linq;

namespace SixModLoader.Events
{
    public abstract class Event
    {
        public HashSet<EventHandler> Handlers { get; } = new HashSet<EventHandler>();

        public void Call(List<EventHandler> handlers)
        {
            Handlers.UnionWith(handlers);
            foreach (var handler in handlers.OrderByDescending(x => x.Priority))
            {
                Call(handler);
            }
        }

        public void Call(EventHandler handler)
        {
            Handlers.Add(handler);
            try
            {
                handler.Method.Invoke(handler.Instance, handler.Method.GetParameters().Length > 0 ? new object[] {this} : new object[0]);
            }
            catch (Exception e)
            {
                Logger.Error(new Exception($"Exception occured while {GetType().FullName} in {handler}", e).ToString());
            }
        }
    }

    public interface ICancellableEvent
    {
        bool Cancelled { get; set; }
    }
}