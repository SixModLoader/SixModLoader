using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SixModLoader.Events;
using UnityEngine;

namespace SixModLoader.Api.Events.Player.Weapon
{
    public class PlayerShootEvent : PlayerEvent, ICancellableEvent
    {
        public bool Cancelled { get; set; }
        public Vector3 TargetPos { get; set; }
        public GameObject Target { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}{{{Player.characterClassManager.UserId}}}";
        }

        [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.CallCmdShoot))]
        public class Patch
        {
            public static PlayerShootEvent Invoke(WeaponManager weaponManager, GameObject target, Vector3 targetPos)
            {
                var @event = new PlayerShootEvent
                {
                    Player = weaponManager._hub,
                    Target = target,
                    TargetPos = targetPos
                };

                EventManager.Instance.Broadcast(@event);
                return @event;
            }

            private static readonly MethodInfo m_Invoke = AccessTools.Method(typeof(Patch), nameof(Invoke));
            private static readonly MethodInfo m_ModifyDuration = AccessTools.Method(typeof(global::Inventory.SyncListItemInfo), nameof(global::Inventory.SyncListItemInfo.ModifyDuration));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
            {
                var codeInstructions = instructions.ToList();

                var index = codeInstructions
                    .FindIndex(x => x.Calls(m_ModifyDuration)) - 13;

                var label = iLGenerator.DefineLabel();
                codeInstructions.Last().labels.Add(label);

                codeInstructions.InsertRange(index, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_1), // load target
                    new CodeInstruction(OpCodes.Ldarg_S, 5), // load targetPos
                    new CodeInstruction(OpCodes.Call, m_Invoke), // call event
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerShootEvent), nameof(Cancelled))), // get cancelled
                    new CodeInstruction(OpCodes.Brtrue_S, label), // return if true
                    new CodeInstruction(OpCodes.Ldarg_0) // load this (hacky way to leave labels as they were in original code)
                });

                return codeInstructions;
            }
        }
    }
}