using HarmonyLib;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Inventory
{
    using Inventory = global::Inventory;

    public class PlayerChangeItemEvent : PlayerEvent
    {
        public Inventory.SyncItemInfo OldItem { get; }
        public Inventory.SyncItemInfo NewItem { get; }

        public PlayerChangeItemEvent(ReferenceHub player, Inventory.SyncItemInfo oldItem, Inventory.SyncItemInfo newItem) : base(player)
        {
            OldItem = oldItem;
            NewItem = newItem;
        }

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
                (
                    ReferenceHub.GetHub(__instance.gameObject),
                    __instance.GetItemByUniq(__instance.NetworkitemUniq),
                    __instance.GetItemByUniq(value)
                );

                EventManager.Instance.Broadcast(@event);
            }
        }
    }
}