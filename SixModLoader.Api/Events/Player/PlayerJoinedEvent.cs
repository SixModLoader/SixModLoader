using HarmonyLib;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player
{
    public class PlayerJoinedEvent : PlayerEvent
    {
        public PlayerJoinedEvent(ReferenceHub player) : base(player)
        {
        }

        public override string ToString()
        {
            return $"{base.ToString()}{{{Player.Format()}}}";
        }

        [HarmonyPatch(typeof(NicknameSync), nameof(NicknameSync.SetNick))]
        public class Patch
        {
            public static void Postfix(NicknameSync __instance)
            {
                EventManager.Instance.Broadcast(new PlayerJoinedEvent(__instance._hub));
            }
        }
    }
}