using HarmonyLib;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Weapon
{
    public class PlayerWeaponChangeAttachmentsEvent : PlayerEvent, ICancellableEvent
    {
        public bool Cancelled { get; set; }

        public string Info { get; set; }

        [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.CallCmdChangeModPreferences))]
        public static class Patch
        {
            public static bool Prefix(WeaponManager __instance, ref string info)
            {
                var @event = new PlayerWeaponChangeAttachmentsEvent
                {
                    Player = __instance.GetComponent<ReferenceHub>(),
                    Info = info
                };
                EventManager.Instance.Broadcast(@event);
                info = @event.Info;
                return !@event.Cancelled;
            }
        }
    }
}