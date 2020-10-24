using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Inventory
{
    using Inventory = global::Inventory;

    public class PlayerDropItemEvent : PlayerEvent, ICancellableEvent
    {
        public bool Cancelled { get; set; }

        public Inventory.SyncItemInfo Item { get; set; }

        public PlayerDropItemEvent(ReferenceHub player, Inventory.SyncItemInfo item) : base(player)
        {
            Item = item;
        }

        public override string ToString()
        {
            return $"{base.ToString()}{{{Player.Format()}, {Item}}}";
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CallCmdDropItem))]
        public class Patch
        {
            public static PlayerDropItemEvent Invoke(Inventory inventory, Inventory.SyncItemInfo item)
            {
                var @event = new PlayerDropItemEvent
                (
                    ReferenceHub.GetHub(inventory.gameObject),
                    item
                );

                EventManager.Instance.Broadcast(@event);
                return @event;
            }

            private static readonly MethodInfo m_Invoke = AccessTools.Method(typeof(Patch), nameof(Invoke));
            private static readonly MethodInfo m_SetPickup = AccessTools.Method(typeof(Inventory), nameof(Inventory.SetPickup));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
            {
                var codeInstructions = instructions.ToList();

                var index = codeInstructions
                    .FindIndex(x => x.Calls(m_SetPickup)) - 20;

                var label = iLGenerator.DefineLabel();
                codeInstructions.Last().labels.Add(label);

                var eventIndex = iLGenerator.DeclareLocal(typeof(PlayerDropItemEvent)).LocalIndex;

                codeInstructions.InsertRange(index, new[]
                {
                    new CodeInstruction(OpCodes.Ldloc_0), // load item
                    new CodeInstruction(OpCodes.Call, m_Invoke), // call event
                    new CodeInstruction(OpCodes.Stloc_S, eventIndex), // store event

                    new CodeInstruction(OpCodes.Ldloc_S, eventIndex), // load event
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerDropItemEvent), nameof(Cancelled))), // get cancelled
                    new CodeInstruction(OpCodes.Brtrue_S, label), // return if true

                    new CodeInstruction(OpCodes.Ldloc_S, eventIndex), // load event
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(PlayerDropItemEvent), nameof(Item))), // get item
                    new CodeInstruction(OpCodes.Stloc_0), // store event item to local item
                    new CodeInstruction(OpCodes.Ldarg_0) // load this (hacky way to leave labels as they were in original code)
                });

                return codeInstructions;
            }
        }
    }
}