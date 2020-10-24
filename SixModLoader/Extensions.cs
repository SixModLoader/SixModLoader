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
        /// <summary>
        /// Pluralizes <paramref name="text"/> based on <paramref name="count"/>
        /// </summary>
        public static string Pluralize(this string text, int count)
        {
            return text + (count == 1 ? "" : "s");
        }

        /// <summary>
        /// Converts SemanticVersion to NuGetVersion
        /// </summary>
        public static NuGetVersion ToNuGetVersion(this SemanticVersion semanticVersion)
        {
            return new NuGetVersion(semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch, semanticVersion.ReleaseLabels, semanticVersion.Metadata);
        }

        /// <inheritdoc cref="PatchAll"/>
        public static List<MethodInfo> PatchAll<T>(this Harmony harmony)
        {
            return harmony.PatchAll(typeof(T));
        }

        /// <summary>
        /// <see cref="Harmony.PatchAll()"/> but only for one type
        /// </summary>
        /// <remarks>
        /// Excludes <paramref name="type"/> from further <see cref="Harmony.PatchAll()"/>
        /// </remarks>
        public static List<MethodInfo> PatchAll(this Harmony harmony, Type type)
        {
            HarmonyPatches.IgnorePatchAll.Ignore.Add(type);
            return type.GetNestedTypes().SelectMany(harmony.PatchAll).Concat(harmony.CreateClassProcessor(type).Patch() ?? new List<MethodInfo>(0)).ToList();
        }

        /// <summary>
        /// Gets all successfully loaded types from a given assembly, shortcut for <see cref="AccessTools.GetTypesFromAssembly"/>
        /// </summary>
        /// <param name="assembly">The assembly</param>
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            return AccessTools.GetTypesFromAssembly(assembly);
        }

        /// <summary>
        /// Dumps <paramref name="dumped"/> to debug logger
        /// </summary>
        /// <param name="dumped">Dumped object</param>
        /// <param name="transform">Optional transformation of <paramref name="dumped"/></param>
        /// <typeparam name="T">Type of <paramref name="dumped"/></typeparam>
        /// <returns>Original <paramref name="dumped"/></returns>
        public static T Dump<T>(this T dumped, Func<T, object> transform = null)
        {
            var transformed = transform == null ? dumped : transform.Invoke(dumped);
            Logger.GetLogger(Assembly.GetCallingAssembly()).Debug(transformed);

            return dumped;
        }
    }
}