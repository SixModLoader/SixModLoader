﻿using System.Collections.Generic;
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
        public PlayerStats.HitInfo HitInfo { get; set; }
        public float Distance { get; set; }
        public string HitboxType { get; set; }

        public float Damage
        {
            get => HitInfo.Amount;
            set
            {
                var hitInfo = HitInfo;
                hitInfo.Amount = value;
                HitInfo = hitInfo;
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()}{{{HitInfo.Amount}}}";
        }

        [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.CallCmdShoot))]
        public class Patch
        {
            public static PlayerShotByPlayerEvent Invoke(WeaponManager weaponManager, CharacterClassManager target, PlayerStats.HitInfo hitInfo, float distance, string hitboxType)
            {
                var @event = new PlayerShotByPlayerEvent
                {
                    Player = ReferenceHub.GetHub(target.gameObject),
                    Shooter = weaponManager._hub,
                    HitInfo = hitInfo,
                    Distance = distance,
                    HitboxType = hitboxType
                };

                EventManager.Instance.Broadcast(@event);
                return @event;
            }

            private static readonly MethodInfo m_Invoke = AccessTools.Method(typeof(Patch), nameof(Invoke));
            private static readonly MethodInfo m_HurtPlayer = AccessTools.Method(typeof(PlayerStats), nameof(PlayerStats.HurtPlayer));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
            {
                var codeInstructions = instructions.ToList();

                var index = codeInstructions
                    .FindIndex(x => x.Calls(m_HurtPlayer)) - 33;

                var eventLocal = iLGenerator.DeclareLocal(typeof(PlayerShotByPlayerEvent)).LocalIndex;
                var hitInfoLocal = iLGenerator.DeclareLocal(typeof(PlayerStats.HitInfo)).LocalIndex;

                var hitInfoIndex = codeInstructions.FindIndex(x => x.Is(OpCodes.Newobj, AccessTools.FirstConstructor(typeof(PlayerStats.HitInfo), c => true))) - 29;

                var hitInfo = codeInstructions.GetRange(hitInfoIndex, 30);
                codeInstructions.RemoveAll(x => hitInfo.Contains(x));

                codeInstructions.InsertRange(hitInfoIndex, new[]
                {
                    new CodeInstruction(OpCodes.Ldloc_S, eventLocal), // load event
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerShotByPlayerEvent), nameof(HitInfo))), // get HitInfo
                });

                var label = iLGenerator.DefineLabel();
                codeInstructions.First(x => x.opcode == OpCodes.Ret).labels.Add(label);

                codeInstructions.InsertRange(index, new[]
                {
                    new CodeInstruction(OpCodes.Stloc_S, hitInfoLocal), // save hitinfo

                    new CodeInstruction(OpCodes.Ldarg_0), // load this
                    new CodeInstruction(OpCodes.Ldloc_S, 3), // load target
                    new CodeInstruction(OpCodes.Ldloc_S, hitInfoLocal), // load hitinfo
                    new CodeInstruction(OpCodes.Ldloc_S, 10), // load distance local
                    new CodeInstruction(OpCodes.Ldarg_2), // load hitboxType
                    new CodeInstruction(OpCodes.Call, m_Invoke), // call event
                    new CodeInstruction(OpCodes.Stloc_S, eventLocal), // store event

                    new CodeInstruction(OpCodes.Ldloc_S, eventLocal), // load event
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerShotByPlayerEvent), nameof(Cancelled))), // get cancelled
                    new CodeInstruction(OpCodes.Brtrue, label), // return if true
                });
                codeInstructions.InsertRange(index, hitInfo);

                return codeInstructions;
            }
        }
    }
}