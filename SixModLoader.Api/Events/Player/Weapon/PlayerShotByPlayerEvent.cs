using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Weapon
{
    public class PlayerShotByPlayerEvent : PlayerEvent, ICancellableEvent
    {
        public bool Cancelled { get; set; }

        public ReferenceHub Shooter { get; }
        public PlayerStats.HitInfo HitInfo { get; set; }
        public float Distance { get; }
        public HitBoxType HitBoxType { get; }

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

        public PlayerShotByPlayerEvent(ReferenceHub player, ReferenceHub shooter, PlayerStats.HitInfo hitInfo, float distance, HitBoxType hitBoxType) : base(player)
        {
            Shooter = shooter;
            HitInfo = hitInfo;
            Distance = distance;
            HitBoxType = hitBoxType;
        }

        public override string ToString()
        {
            return $"{base.ToString()}{{{Shooter.Format()} -> {Player.Format()} (damage: {HitInfo.Amount})}}";
        }

        [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.CallCmdShoot))]
        public class Patch
        {
            public static PlayerShotByPlayerEvent Invoke(WeaponManager weaponManager, ReferenceHub target, PlayerStats.HitInfo hitInfo, float distance, HitBoxType hitBoxType)
            {
                var @event = new PlayerShotByPlayerEvent
                (
                    target,
                    weaponManager._hub,
                    hitInfo,
                    distance,
                    hitBoxType
                );

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
                    new CodeInstruction(OpCodes.Ldloc_S, 12), // load distance local
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
