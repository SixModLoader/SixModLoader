using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Class
{
    public class PlayerRoleChangeEvent : PlayerEvent, ICancellableEvent
    {
        public RoleType RoleType { get; set; }
        public List<ItemType> Items { get; set; }
        public bool Lite { get; set; }
        public bool Escape { get; set; }

        public bool Cancelled { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}{{{RoleType}, items: [{Items.Join()}], lite: {Lite}, escape: {Escape}{(Cancelled ? ", cancelled" : string.Empty)}}}";
        }

        [HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.SetPlayersClass))]
        public class Patch
        {
            public static PlayerRoleChangeEvent Invoke(ReferenceHub player, RoleType roleType, bool lite, bool escape)
            {
                var @event = new PlayerRoleChangeEvent
                {
                    Player = player,
                    RoleType = roleType,
                    Items = player.characterClassManager.Classes.SafeGet(roleType).startItems.ToList(),
                    Lite = lite,
                    Escape = escape
                };

                EventManager.Instance.Broadcast(@event);
                return @event;
            }

            private static readonly MethodInfo m_Invoke = AccessTools.Method(typeof(Patch), nameof(Invoke));
            private static readonly MethodInfo m_SetClassIDAdv = AccessTools.Method(typeof(CharacterClassManager), nameof(CharacterClassManager.SetClassIDAdv));
            private static readonly FieldInfo f_startItems = AccessTools.Field(typeof(Role), nameof(Role.startItems));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
            {
                var codeInstructions = instructions.ToList();

                var label = iLGenerator.DefineLabel();
                codeInstructions.Last().labels.Add(label);

                var eventIndex = iLGenerator.DeclareLocal(typeof(PlayerRoleChangeEvent)).LocalIndex;

                codeInstructions.InsertRange(codeInstructions
                        .FindIndex(x => x.Calls(m_SetClassIDAdv)) - 4,
                    new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_1), // load role
                        new CodeInstruction(OpCodes.Ldarg_3), // load lite
                        new CodeInstruction(OpCodes.Ldarg_S, 4), // load escape
                        new CodeInstruction(OpCodes.Call, m_Invoke), // call event
                        new CodeInstruction(OpCodes.Stloc_S, eventIndex), // store event

                        new CodeInstruction(OpCodes.Ldloc_S, eventIndex), // load event
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleChangeEvent), nameof(Cancelled))), // get cancelled
                        new CodeInstruction(OpCodes.Brtrue_S, label), // return if true

                        new CodeInstruction(OpCodes.Ldloc_S, eventIndex), // load event
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleChangeEvent), nameof(Lite))), // get lite
                        new CodeInstruction(OpCodes.Starg_S, 3), // store lite

                        new CodeInstruction(OpCodes.Ldloc_S, eventIndex), // load event
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleChangeEvent), nameof(Escape))), // get escape
                        new CodeInstruction(OpCodes.Starg_S, 4), // store escape
                        new CodeInstruction(OpCodes.Ldloc_0) // load hub (hacky way to leave labels as they were in original code)
                    });

                var startItems = codeInstructions.FindIndex(x => x.LoadsField(f_startItems)) - 4;
                codeInstructions.RemoveRange(startItems, 5);
                codeInstructions.InsertRange(startItems, new[]
                {
                    new CodeInstruction(OpCodes.Ldloc_S, eventIndex), // load event
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerRoleChangeEvent), nameof(Items))), // get items
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<ItemType>), nameof(List<ItemType>.ToArray)))
                });

                return codeInstructions;
            }
        }
    }
}