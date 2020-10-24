using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player
{
    public class PlayerDeathEvent : PlayerEvent
    {
        public PlayerDeathEvent(ReferenceHub player) : base(player)
        {
        }

        public override string ToString()
        {
            return $"{base.ToString()}{{{Player.Format()}}}";
        }

        [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.HurtPlayer))]
        public class Patch
        {
            public static void Invoke(PlayerStats playerStats)
            {
                var @event = new PlayerDeathEvent
                (
                    playerStats.ccm._hub
                );

                EventManager.Instance.Broadcast(@event);
            }

            private static readonly MethodInfo m_Invoke = AccessTools.Method(typeof(Patch), nameof(Invoke));
            private static readonly MethodInfo m_SetHPAmount = AccessTools.Method(typeof(PlayerStats), nameof(PlayerStats.SetHPAmount));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codeInstructions = instructions.ToList();

                var index = codeInstructions.FindIndex(x => x.Calls(m_SetHPAmount)) - 1;

                codeInstructions.InsertRange(index, new[]
                {
                    new CodeInstruction(OpCodes.Call, m_Invoke), // call event
                    new CodeInstruction(OpCodes.Ldloc_S, 5) // load this
                });

                return codeInstructions;
            }
        }
    }
}