using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CommandSystem;
using HarmonyLib;
using RemoteAdmin;
using SixModLoader.Api.Extensions;

namespace SixModLoader.Api.Patches
{
    public class CommandProcessorPatch
    {
        [HarmonyPatch]
        public class ProcessQueryPatch
        {
            private static readonly MethodInfo m_ProcessQuery = AccessTools.Method(typeof(CommandProcessor), nameof(CommandProcessor.ProcessQuery));
            private static readonly MethodInfo m_ProcessGameConsoleQuery = AccessTools.Method(typeof(QueryProcessor), nameof(QueryProcessor.ProcessGameConsoleQuery));

            public static IEnumerable<MethodBase> TargetMethods()
            {
                return new MethodBase[] { m_ProcessQuery, m_ProcessGameConsoleQuery };
            }

            private static readonly MethodInfo m_ToUpper = AccessTools.Method(typeof(string), nameof(string.ToUpper));
            private static readonly MethodInfo m_Command = AccessTools.PropertyGetter(typeof(ICommand), nameof(ICommand.Command));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                var codeInstructions = instructions.ToList();

                var max = 2;
                for (var i = 0; i < codeInstructions.Count; i++)
                {
                    if (max <= 0)
                        break;

                    if (
                        (original == m_ProcessQuery
                            ? codeInstructions[i].opcode == OpCodes.Ldfld && ((FieldInfo) codeInstructions[i].operand).Name == "query" && ((FieldInfo) codeInstructions[i].operand).FieldType == typeof(string[])
                            : codeInstructions[i].opcode == OpCodes.Ldloc_0) &&
                        codeInstructions[i + 1].opcode == OpCodes.Ldc_I4_0 &&
                        codeInstructions[i + 2].opcode == OpCodes.Ldelem_Ref &&
                        codeInstructions[i + 3].Calls(m_ToUpper) &&
                        codeInstructions[i + 4].opcode == OpCodes.Ldstr && ((string) codeInstructions[i + 4].operand).StartsWith("#")
                    )
                    {
                        max--;
                        codeInstructions.RemoveRange(original == m_ProcessQuery ? i - 1 : i, original == m_ProcessQuery ? 4 : 3);
                        codeInstructions.InsertRange(original == m_ProcessQuery ? i - 1 : i, new[]
                        {
                            new CodeInstruction(OpCodes.Ldloc_S, original == m_ProcessQuery ? 8 : 1),
                            new CodeInstruction(OpCodes.Callvirt, m_Command)
                        });
                    }
                }

                if (max != 0)
                {
                    Logger.Error("ProcessQueryPatch failed!");
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