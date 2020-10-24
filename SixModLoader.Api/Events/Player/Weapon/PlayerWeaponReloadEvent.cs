using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CustomPlayerEffects;
using HarmonyLib;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Weapon
{
    public class PlayerWeaponReloadEvent : PlayerEvent, ICancellableEvent
    {
        public bool Cancelled { get; set; }

        public bool AnimationOnly { get; }

        public PlayerWeaponReloadEvent(ReferenceHub player, bool animationOnly) : base(player)
        {
            AnimationOnly = animationOnly;
        }

        public override string ToString()
        {
            return $"{base.ToString()}{{{AnimationOnly}}}";
        }

        [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.CallCmdReload))]
        public class Patch
        {
            public static PlayerWeaponReloadEvent Invoke(WeaponManager weaponManager, bool animationOnly)
            {
                var @event = new PlayerWeaponReloadEvent
                (
                    ReferenceHub.GetHub(weaponManager.gameObject),
                    animationOnly
                );

                EventManager.Instance.Broadcast(@event);
                return @event;
            }

            private static readonly MethodInfo m_Invoke = AccessTools.Method(typeof(Patch), nameof(Invoke));
            private static readonly MethodInfo m_ServerDisable = AccessTools.Method(typeof(PlayerEffect), nameof(PlayerEffect.ServerDisable));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
            {
                var codeInstructions = instructions.ToList();

                var index = codeInstructions
                    .FindIndex(x => x.Calls(m_ServerDisable)) + 1;

                var label = iLGenerator.DefineLabel();
                codeInstructions.Last().labels.Add(label);

                codeInstructions.InsertRange(index, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0), // load this
                    new CodeInstruction(OpCodes.Ldarg_1), // load animationOnly
                    new CodeInstruction(OpCodes.Call, m_Invoke), // call event
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerWeaponReloadEvent), nameof(Cancelled))), // get cancelled
                    new CodeInstruction(OpCodes.Brtrue_S, label) // return if true
                });

                return codeInstructions;
            }
        }
    }
}