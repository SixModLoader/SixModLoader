using HarmonyLib;
using Searching;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Inventory
{
    public class PlayerPickupItemEvent : PlayerEvent, ICancellableEvent
    {
        public Pickup Pickup { get; private set; }
        public bool Cancelled { get; set; }

        [HarmonyPatch(typeof(ItemSearchCompletor), nameof(ItemSearchCompletor.Complete))]
        public class Patch
        {
            public static bool Prefix(ItemSearchCompletor __instance)
            {
                var @event = new PlayerPickupItemEvent
                {
                    Player = __instance.Hub,
                    Pickup = __instance.TargetPickup
                };

                EventManager.Instance.Broadcast(@event);

                return !@event.Cancelled;
            }
        }
    }
}