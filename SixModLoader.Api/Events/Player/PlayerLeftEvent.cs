using HarmonyLib;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player
{
    public class PlayerLeftEvent : PlayerEvent
    {
        public PlayerLeftEvent(ReferenceHub player) : base(player)
        {
        }

        public override string ToString()
        {
            return $"{base.ToString()}{{{Player.Format()}}}";
        }

        [HarmonyPatch(typeof(ReferenceHub), nameof(ReferenceHub.OnDestroy))]
        public class Patch
        {
            public static void Postfix(ReferenceHub __instance)
            {
                EventManager.Instance.Broadcast(new PlayerLeftEvent(__instance));
            }
        }
    }
}