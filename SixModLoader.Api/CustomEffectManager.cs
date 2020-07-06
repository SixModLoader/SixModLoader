using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using HarmonyLib;
using Mirror;

namespace SixModLoader.Api
{
    public class CustomEffectManager
    {
        private static CustomEffectManager Instance { get; set; }

        public CustomEffectManager()
        {
            Instance = this;
        }

        public List<Type> CustomPlayerEffects { get; } = new List<Type>();

        public void Register<T>() where T : PlayerEffect
        {
            CustomPlayerEffects.Add(typeof(T));
        }

        /// <summary>
        /// Patch injecting custom effects to <see cref="PlayerEffectsController"/>
        /// </summary>
        [HarmonyPatch(typeof(PlayerEffectsController), nameof(PlayerEffectsController.Awake))]
        public static class EffectsRegistryPatch
        {
            public static void Prefix(PlayerEffectsController __instance)
            {
                var hub = ReferenceHub.GetHub(__instance.gameObject);
                foreach (var effect in Instance.CustomPlayerEffects)
                {
                    __instance.AllEffects.Add(effect, (PlayerEffect) Activator.CreateInstance(effect, new object[] {hub}));
                }
            }
        }

        /// <summary>
        /// Dirty workaround to hide custom effects from client
        /// </summary>
        [HarmonyPatch(typeof(SyncList<byte>), nameof(SyncList<byte>.OnSerializeAll))]
        public static class SerializeAllPatch
        {
            public static void Prefix(SyncList<byte> __instance, out IList<byte> __state)
            {
                __state = null;

                if (ReferenceHub.HostHub == null)
                    return;

                if (ReferenceHub.Hubs.Values.All(x => x.playerEffectsController.syncEffectsIntensity != __instance))
                    return;
                
                Logger.Debug("Hiding custom effects (all)");

                __state = __instance.objects.ToList();

                var effects = ReferenceHub.HostHub.playerEffectsController.AllEffects.Keys;
                for (var i = 0; i < effects.Count; i++)
                {
                    if (Instance.CustomPlayerEffects.Contains(effects.ElementAt(i)))
                    {
                        __instance.objects.RemoveAt(i);
                    }
                }
                
                __instance.Flush();
            }

            public static void Postfix(SyncList<byte> __instance, IList<byte> __state)
            {
                if (__state == null)
                    return;

                __instance.objects.Clear();
                foreach (var b in __state)
                {
                    __instance.objects.Add(b);
                }

                __instance.Flush();
            }
        }

        /// <inheritdoc cref="SerializeAllPatch"/>
        [HarmonyPatch(typeof(SyncList<byte>), nameof(SyncList<byte>.OnSerializeDelta))]
        public static class SerializeDeltaPatch
        {
            public static void Prefix(SyncList<byte> __instance, out IList<byte> __state)
            {
                __state = null;

                if (ReferenceHub.HostHub == null)
                    return;
                
                if (ReferenceHub.Hubs.Values.All(x => x.playerEffectsController.syncEffectsIntensity != __instance))
                    return;
                
                Logger.Debug("Hiding custom effects (delta)");

                var effects = ReferenceHub.HostHub.playerEffectsController.AllEffects.Keys;

                for (var i = 0; i < __instance.changes.Count; i++)
                {
                    var change = __instance.changes[i];
                    __instance.changes[i] = new SyncList<byte>.Change {index = change.index - effects.Take(change.index).Count(x => Instance.CustomPlayerEffects.Contains(x)), item = change.item, operation = change.operation};
                }
            }
        }
    }
}