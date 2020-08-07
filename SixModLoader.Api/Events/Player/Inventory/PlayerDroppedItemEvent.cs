using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SixModLoader.Events;

namespace SixModLoader.Api.Events.Player.Inventory
{
    using Inventory = global::Inventory;

    public class PlayerDroppedItemEvent : PlayerEvent
    {
        public Inventory.SyncItemInfo Item { get; private set; }
        public Pickup Pickup { get; private set; }

        public class Patch
        {
            public static PlayerDroppedItemEvent Invoke(Inventory inventory, Inventory.SyncItemInfo item, Pickup pickup)
            {
                var @event = new PlayerDroppedItemEvent
                {
                    Player = ReferenceHub.GetHub(inventory.gameObject),
                    Item = item,
                    Pickup = pickup
                };

                EventManager.Instance.Broadcast(@event);
                return @event;
            }

            private static readonly MethodInfo m_Invoke = AccessTools.Method(typeof(Patch), nameof(Invoke));
            private static readonly MethodInfo m_SetPickup = AccessTools.Method(typeof(Inventory), nameof(Inventory.SetPickup));

            [HarmonyPatch(typeof(Inventory), nameof(Inventory.CallCmdDropItem))]
            public static class DropOnePatch
            {
                public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
                {
                    var codeInstructions = instructions.ToList();

                    var index = codeInstructions
                        .FindIndex(x => x.Calls(m_SetPickup)) + 1;

                    var pickupIndex = iLGenerator.DeclareLocal(typeof(Pickup)).LocalIndex;
                    codeInstructions.RemoveAt(index); // remove pop
                    codeInstructions.Insert(index, new CodeInstruction(OpCodes.Stloc_S, pickupIndex));

                    codeInstructions.InsertRange(codeInstructions.Count - 1, new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0), // load this
                        new CodeInstruction(OpCodes.Ldloc_0), // load item
                        new CodeInstruction(OpCodes.Ldloc_S, pickupIndex), // load pickup
                        new CodeInstruction(OpCodes.Call, m_Invoke), // call event
                        new CodeInstruction(OpCodes.Pop)
                    });

                    return codeInstructions;
                }
            }

            [HarmonyPatch(typeof(Inventory), nameof(Inventory.ServerDropAll))]
            public static class DropAllPatch
            {
                private static readonly FieldInfo f_id = AccessTools.Field(typeof(Inventory.SyncItemInfo), nameof(Inventory.SyncItemInfo.id));

                public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var codeInstructions = instructions.ToList();

                    var index_id = codeInstructions.FindIndex(instruction => instruction.LoadsField(f_id));

                    codeInstructions.InsertRange(index_id - 2, new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0), // load this
                        new CodeInstruction(OpCodes.Ldloc_1) // load item
                    });

                    var index_SetPickup = codeInstructions.FindIndex(instruction => instruction.Calls(m_SetPickup));

                    codeInstructions.RemoveAt(index_SetPickup + 1);
                    codeInstructions.InsertRange(index_SetPickup + 1, new[]
                    {
                        new CodeInstruction(OpCodes.Call, m_Invoke) // call event
                    });

                    return codeInstructions;
                }
            }
        }
    }
}