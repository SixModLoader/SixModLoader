using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CommandSystem;
using CommandSystem.Commands;
using HarmonyLib;
using RemoteAdmin;
using SixModLoader.Api.Extensions;

namespace SixModLoader.Api.Patches
{
    public class GameConsoleQueryCommandHandler : CommandHandler
    {
        private GameConsoleQueryCommandHandler()
        {
        }

        public static GameConsoleQueryCommandHandler Create()
        {
            var commandHandler = new GameConsoleQueryCommandHandler();
            commandHandler.LoadGeneratedCommands();
            return commandHandler;
        }

        public override void LoadGeneratedCommands()
        {
            this.RegisterCommand(new HelpCommand(this));
        }
    }

    public class QueryProcessorPatch
    {
        [HarmonyPatch(typeof(QueryProcessor), nameof(QueryProcessor.ProcessGameConsoleQuery))]
        public class ProcessGameConsoleQueryPatch
        {
            public static bool Prefix(QueryProcessor __instance, [HarmonyArgument("query")] string q, bool encrypted)
            {
                var sender = __instance._sender;
                var gameConsoleTransmission = __instance.GCT;

                var query = q.Split(' ');
                if (CommandManager.GameConsoleQueryCommandHandler.TryGetCommand(query[0], out var command))
                {
                    try
                    {
                        var success = command.Execute(query.Segment(1), sender, out var response);
                        if (response != null)
                        {
                            gameConsoleTransmission.SendToClient(__instance.connectionToClient, response, success ? "green" : "red");
                        }
                    }
                    catch (Exception e)
                    {
                        gameConsoleTransmission.SendToClient(__instance.connectionToClient, "Command execution failed! Error: " + e, "red");
                    }

                    return false;
                }

                return true;
            }
        }
    }

    public class CommandProcessorPatch
    {
        [HarmonyPatch(typeof(CommandProcessor), nameof(CommandProcessor.ProcessQuery))]
        public class ProcessQueryPatch
        {
            private static readonly MethodInfo m_ToUpper = AccessTools.Method(typeof(string), nameof(string.ToUpper));
            private static readonly MethodInfo m_Command = AccessTools.PropertyGetter(typeof(ICommand), nameof(ICommand.Command));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codeInstructions = instructions.ToList();

                var max = 2;
                for (var i = 0; i < codeInstructions.Count; i++)
                {
                    if (max <= 0)
                        break;

                    if (
                        codeInstructions[i].opcode == OpCodes.Ldloc_0 &&
                        codeInstructions[i + 1].opcode == OpCodes.Ldfld && ((FieldInfo) codeInstructions[i + 1].operand).Name == "query" && ((FieldInfo) codeInstructions[i + 1].operand).FieldType == typeof(string[]) &&
                        codeInstructions[i + 2].opcode == OpCodes.Ldc_I4_0 &&
                        codeInstructions[i + 3].opcode == OpCodes.Ldelem_Ref &&
                        codeInstructions[i + 4].opcode == OpCodes.Callvirt && (MethodInfo) codeInstructions[i + 4].operand == m_ToUpper &&
                        codeInstructions[i + 5].opcode == OpCodes.Ldstr && ((string) codeInstructions[i + 5].operand).StartsWith("#")
                    )
                    {
                        max--;
                        codeInstructions.RemoveRange(i, 4);
                        codeInstructions.InsertRange(i, new[]
                        {
                            new CodeInstruction(OpCodes.Ldloc_S, 8),
                            new CodeInstruction(OpCodes.Callvirt, m_Command)
                        });
                    }
                }

                return codeInstructions;
            }
        }
    }

    public class MiscPatch
    {
        [HarmonyPatch(typeof(Misc), nameof(Misc.ProcessRaPlayersList))]
        public class ProcessRaPlayersListPatch
        {
            public static bool Prefix(string playerIds, ref List<int> __result)
            {
                try
                {
                    __result = CommandExtensions.MatchPlayers(playerIds).Select(x => x.queryProcessor.PlayerId).ToList();
                }
                catch (Exception e)
                {
                    Logger.Error("RA Match Players Patch\n" + e);
                    __result = null;
                }

                return false;
            }
        }
    }
}