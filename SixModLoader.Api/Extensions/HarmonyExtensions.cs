using System;
using System.Reflection;
using HarmonyLib;
using SixModLoader.Mods;

namespace SixModLoader.Api.Extensions
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AutoHarmonyAttribute : Attribute
    {
    }

    public static class HarmonyExtensions
    {
        public static void Unpatch(this Harmony harmony)
        {
            harmony.UnpatchAll(harmony.Id);
        }

        [HarmonyPatch(typeof(ModEvent), nameof(ModEvent.Call))]
        public static class Patch
        {
            public static void Prefix(ModEvent __instance)
            {
                foreach (var mod in __instance.Mods)
                {
                    foreach (var property in mod.Type.GetProperties())
                    {
                        var attribute = property.GetCustomAttribute<AutoHarmonyAttribute>();
                        if (attribute == null)
                            continue;

                        if (__instance.GetType() == typeof(ModEnableEvent))
                        {
                            if (!(property.GetValue(mod.AbstractInstance) is Harmony harmony))
                            {
                                harmony = new Harmony(mod.Info.Id);
                                property.SetValue(mod.AbstractInstance, harmony);
                            }

                            Logger.Info($"[{mod.Info.Name}] Patching Harmony");
                            harmony.PatchAll(mod.Assembly);
                        }

                        if (__instance.GetType() == typeof(ModDisableEvent))
                        {
                            if (property.GetValue(mod.AbstractInstance) is Harmony harmony)
                            {
                                Logger.Info($"[{mod.Info.Name}] Unpatching Harmony");
                                harmony.Unpatch();
                            }
                        }
                    }
                }
            }
        }
    }
}