using HarmonyLib;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Inventory
{
    using Inventory = global::Inventory;

    public class PlayerChangeItemEvent : PlayerEvent
    {
        public Inventory.SyncItemInfo OldItem { get; private set; }
        public Inventory.SyncItemInfo NewItem { get; private set; }

        public override string ToString()
        {
            return $"{base.ToString()}{{{OldItem.id} ({OldItem.uniq}) -> {NewItem.id} ({NewItem.uniq})}}";
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.NetworkitemUniq), MethodType.Setter)]
        public class Patch
        {
            public static void Prefix(Inventory __instance, ref int value)
            {
                var @event = new PlayerChangeItemEvent
                {
                    Player = ReferenceHub.GetHub(__instance.gameObject),
                    OldItem = __instance.GetItemByUniq(__instance.NetworkitemUniq),
                    NewItem = __instance.GetItemByUniq(value)
                };
                EventManager.Instance.Broadcast(@event);
                
                // TODO changing new item
                // if (i != @event.NewItem.uniq)
                // {
                //     i = @event.NewItem.uniq;
                //     __instance.NetworkcurItem = @event.NewItem.id;
                //     __instance.SetDirtyBit(1UL);
                // }
            }
        }
    }
}