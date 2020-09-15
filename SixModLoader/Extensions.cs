using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NuGet.Versioning;
using SixModLoader.Patches;

namespace SixModLoader
{
    public static class Extensions
    {
        public static string Pluralize(this string text, int count)
        {
            return text + (count == 1 ? "" : "s");
        }

        public static NuGetVersion ToNuGetVersion(this SemanticVersion semanticVersion)
        {
            return new NuGetVersion(semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch, semanticVersion.ReleaseLabels, semanticVersion.Metadata);
        }

        public static List<MethodInfo> PatchAll<T>(this Harmony harmony)
        {
            return harmony.PatchAll(typeof(T));
        }

        public static List<MethodInfo> PatchAll(this Harmony harmony, Type type)
        {
            HarmonyPatches.IgnorePatchAll.Ignore.Add(type);
            return type.GetNestedTypes().SelectMany(harmony.PatchAll).Concat(harmony.CreateClassProcessor(type).Patch() ?? new List<MethodInfo>(0)).ToList();
        }
    }
}