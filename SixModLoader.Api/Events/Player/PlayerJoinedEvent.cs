using HarmonyLib;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player
{
    public class PlayerJoinedEvent : PlayerEvent
    {
        [HarmonyPatch(typeof(NicknameSync), nameof(NicknameSync.SetNick))]
        public class Patch
        {
            public static void Postfix(NicknameSync __instance)
            {
                var @event = new PlayerJoinedEvent
                {
                    Player = __instance.hub
                };

                EventManager.Instance.Broadcast(@event);
            }
        }
    }
}
