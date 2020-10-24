using HarmonyLib;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Weapon
{
    public class PlayerWeaponChangeAttachmentsEvent : PlayerEvent, ICancellableEvent
    {
        public bool Cancelled { get; set; }

        public string Info { get; set; }

        public PlayerWeaponChangeAttachmentsEvent(ReferenceHub player, string info) : base(player)
        {
            Info = info;
        }

        public override string ToString()
        {
            return $"{base.ToString()}{{{Info}}}";
        }

        [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.CallCmdChangeModPreferences))]
        public static class Patch
        {
            public static bool Prefix(WeaponManager __instance, ref string info)
            {
                var @event = new PlayerWeaponChangeAttachmentsEvent
                (
                    __instance.GetComponent<ReferenceHub>(),
                    info
                );

                EventManager.Instance.Broadcast(@event);
                info = @event.Info;
                return !@event.Cancelled;
            }
        }
    }
}