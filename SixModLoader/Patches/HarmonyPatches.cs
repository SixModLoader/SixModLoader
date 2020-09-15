using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace SixModLoader.Patches
{
    public class HarmonyPatches
    {
        [HarmonyPatch(typeof(Harmony), nameof(Harmony.PatchAll), typeof(Assembly))]
        public static class IgnorePatchAll
        {
            internal static List<Type> Ignore { get; } = new List<Type>();

            public static Type[] Proxy(Assembly assembly)
            {
                return AccessTools.GetTypesFromAssembly(assembly).Where(x => !Ignore.Contains(x)).ToArray();
            }

            private static readonly MethodInfo m_Proxy = AccessTools.Method(typeof(IgnorePatchAll), nameof(Proxy));
            private static readonly MethodInfo m_GetTypesFromAssembly = AccessTools.Method(typeof(AccessTools), nameof(AccessTools.GetTypesFromAssembly));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return instructions.MethodReplacer(m_GetTypesFromAssembly, m_Proxy);
            }
        }

        [HarmonyPatch(typeof(PatchClassProcessor), nameof(PatchClassProcessor.Patch))]
        public static class PatchLogger
        {
            public static void Postfix(PatchClassProcessor __instance, List<MethodInfo> __result, Harmony ___instance)
            {
                if (__result == null) return;

                foreach (var methodInfo in __result)
                {
                    Logger.Debug($"{___instance.Id} patched {methodInfo.FullDescription()}");
                }
            }
        }
    }
}