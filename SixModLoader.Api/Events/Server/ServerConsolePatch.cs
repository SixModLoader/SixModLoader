using HarmonyLib;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Server
{
    public class ServerConsoleReadyEvent : Event
    {
        [HarmonyPatch(typeof(ServerConsole), nameof(ServerConsole.Start))]
        public class Patch
        {
            public static void Postfix()
            {
                EventManager.Instance.Broadcast(new ServerConsoleReadyEvent());
            }
        }
    }
}