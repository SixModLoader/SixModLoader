using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Weapon
{
    public class PlayerShotByPlayerEvent : PlayerEvent, ICancellableEvent
    {
        public bool Cancelled { get; set; }
        public ReferenceHub Shooter { get; set; }
        public float Damage { get; set; }
        public float Distance { get; set; }
        public string HitboxType { get; set; }
        
        [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.CallCmdShoot))]
        public class Patch
        {
            public static PlayerShotByPlayerEvent Invoke(WeaponManager weaponManager, CharacterClassManager target, float damage, float distance, string hitboxType)
            {
                var @event = new PlayerShotByPlayerEvent
                {
                    Player = ReferenceHub.GetHub(target.gameObject),
                    Shooter = weaponManager._hub,
                    Damage = damage,
                    Distance = distance,
                    HitboxType = hitboxType
                };

                EventManager.Instance.Broadcast(@event);
                return @event;
            }

            private static readonly MethodInfo m_Invoke = AccessTools.Method(typeof(Patch), nameof(Invoke));
            private static readonly MethodInfo m_HurtPlayer = AccessTools.Method(typeof(PlayerStats), nameof(PlayerStats.HurtPlayer));

            // TODO new beta moved final damage changing to HitInfo constructor (capture HitInfo?)
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
            {
                var codeInstructions = instructions.ToList();

                var index = codeInstructions
                    .FindIndex(x => x.opcode == OpCodes.Callvirt && (MethodInfo)x.operand == m_HurtPlayer) - 33;
                
                var label = iLGenerator.DefineLabel();
                codeInstructions.Last().labels.Add(label);
                
                var eventIndex = iLGenerator.DeclareLocal(typeof(PlayerShotByPlayerEvent)).LocalIndex;

                codeInstructions.InsertRange(index, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0), // load this
                    new CodeInstruction(OpCodes.Ldloc_S, 3), // load target
                    new CodeInstruction(OpCodes.Ldloc_S, 11), // load damage local
                    new CodeInstruction(OpCodes.Ldloc_S, 10), // load distance local
                    new CodeInstruction(OpCodes.Ldarg_2), // load hitboxType
                    new CodeInstruction(OpCodes.Call, m_Invoke), // call event
                    new CodeInstruction(OpCodes.Stloc_S, eventIndex), // store event

                    new CodeInstruction(OpCodes.Ldloc_S, eventIndex), // load event
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerShotByPlayerEvent), nameof(Cancelled))), // get cancelled
                    new CodeInstruction(OpCodes.Brtrue_S, label), // return if true

                    new CodeInstruction(OpCodes.Ldloc_S, eventIndex), // load event
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerShotByPlayerEvent), nameof(Damage))), // get damage
                    new CodeInstruction(OpCodes.Stloc_S, 11) // store event damage to local damage
                });

                return codeInstructions;
            }
        }
    }
}
