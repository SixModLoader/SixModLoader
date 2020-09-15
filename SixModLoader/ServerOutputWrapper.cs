using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ServerOutput;

namespace SixModLoader
{
    internal static class ServerOutputWrapper
    {
        public static void Start()
        {
            SixModLoader.Instance.Harmony.PatchAll(typeof(TcpConsolePatch));

            var portString = Environment.GetCommandLineArgs().FirstOrDefault(x => x.StartsWith("-console"))?.Substring("console".Length + 1);
            if (ServerStatic.ServerOutput == null && portString != null && ushort.TryParse(portString, out var port))
            {
                ServerStatic.ServerOutput = new TcpConsole(port);
                ServerStatic.ServerOutput.Start();
                ServerStatic.IsDedicated = true;

                Logger.Info("Started TcpClient!");
                ServerConsole.AddLog($"SixModLoader - {SixModLoader.Instance.Version}", ConsoleColor.Green);
            }
        }

        public static class TcpConsolePatch
        {
            [HarmonyPatch(typeof(TcpConsole), nameof(TcpConsole.Start))]
            public static class StartPatch
            {
                private static List<TcpConsole> Started { get; } = new List<TcpConsole>();

                public static bool Prefix(TcpConsole __instance)
                {
                    if (Started.Contains(__instance))
                    {
                        return false;
                    }

                    Started.Add(__instance);
                    return true;
                }
            }
        }
    }
}