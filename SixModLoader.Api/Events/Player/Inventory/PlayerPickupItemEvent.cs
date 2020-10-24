using HarmonyLib;
using Searching;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Inventory
{
    public class PlayerPickupItemEvent : PlayerEvent, ICancellableEvent
    {
        public bool Cancelled { get; set; }

        public Pickup Pickup { get; }

        public PlayerPickupItemEvent(ReferenceHub player, Pickup pickup) : base(player)
        {
            Pickup = pickup;
        }

        public override string ToString()
        {
            return $"{base.ToString()}{{{Player.Format()}, {Pickup.itemId}}}";
        }

        [HarmonyPatch(typeof(ItemSearchCompletor), nameof(ItemSearchCompletor.Complete))]
        public class Patch
        {
            public static bool Prefix(ItemSearchCompletor __instance)
            {
                var @event = new PlayerPickupItemEvent
                (
                    __instance.Hub,
                    __instance.TargetPickup
                );

                EventManager.Instance.Broadcast(@event);
                return !@event.Cancelled;
            }
        }
    }
}