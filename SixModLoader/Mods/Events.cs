using System.Collections.Generic;
using System.Linq;
using SixModLoader.Events;

namespace SixModLoader.Mods
{
    public abstract class ModEvent : Event
    {
        public List<ModContainer> Mods { get; } = SixModLoader.Instance.ModManager.Mods;

        protected ModEvent()
        {
        }

        protected ModEvent(List<ModContainer> mods)
        {
            Mods = mods;
        }

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
        public ModEnableEvent()
        {
        }

        public ModEnableEvent(List<ModContainer> mods) : base(mods)
        {
        }
    }

    public class ModDisableEvent : ModEvent
    {
        public ModDisableEvent()
        {
        }

        public ModDisableEvent(List<ModContainer> mods) : base(mods)
        {
        }
    }

    public class ModReloadEvent : ModEvent
    {
        public ModReloadEvent()
        {
        }

        public ModReloadEvent(List<ModContainer> mods) : base(mods)
        {
        }
    }
}