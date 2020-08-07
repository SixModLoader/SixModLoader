using System.Collections.Generic;
using System.Linq;
using SixModLoader.Events;

namespace SixModLoader.Mods
{
    public abstract class ModEvent : Event
    {
        public List<ModContainer> Mods { get; set; } = SixModLoader.Instance.ModManager.Mods;

        public void Call()
        {
            Call(SixModLoader.Instance.EventManager.Handlers
                .Where(x => x.Key.IsInstanceOfType(this))
                .SelectMany(x => x.Value)
                .Where(x => Mods.Select(m => m.AbstractInstance).Contains(x.Instance))
                .ToList());
        }
    }

    public class ModEnableEvent : ModEvent
    {
    }

    public class ModDisableEvent : ModEvent
    {
    }

    public class ModReloadEvent : ModEvent
    {
    }
}