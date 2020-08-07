using HarmonyLib;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player
{
    public class PlayerLeftEvent : PlayerEvent
    {
        [HarmonyPatch(typeof(ReferenceHub), nameof(ReferenceHub.OnDestroy))]
        public class Patch
        {
            public static void Postfix(ReferenceHub __instance)
            {
                var @event = new PlayerLeftEvent
                {
                    Player = __instance
                };

                EventManager.Instance.Broadcast(@event);
            }
        }
    }
}