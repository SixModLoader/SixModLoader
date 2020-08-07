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

        public static void Initialize()
        {
            SixModLoader.Instance.Harmony
                .CreateProcessor(AccessTools.Method(typeof(ModEvent), nameof(ModEvent.Call), new Type[0]))
                .AddPrefix(AccessTools.Method(typeof(Patch), nameof(Patch.Prefix)))
                .Patch();
        }

        public class Patch
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
                            var harmony = property.GetValue(mod.AbstractInstance) as Harmony;

                            if (harmony == null)
                            {
                                harmony = new Harmony(mod.Info.Id);
                                property.SetValue(mod.AbstractInstance, harmony);
                            }

                            Logger.Info($"[{mod.Info.Name}] Patching Harmony");
                            harmony.PatchAll(mod.Assembly);
                        }

                        if (__instance.GetType() == typeof(ModDisableEvent))
                        {
                            var harmony = property.GetValue(mod.AbstractInstance) as Harmony;
                            if (harmony != null)
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