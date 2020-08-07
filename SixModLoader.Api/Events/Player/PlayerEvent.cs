using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player
{
    public class PlayerEvent : Event
    {
        public ReferenceHub Player { get; set; }
    }
}