using System;
using System.IO;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using MEC;
using SixModLoader.Api;
using SixModLoader.Events;

namespace SixModLoader.Compatibility.Exiled
{
    public class ExiledLoader
    {
        public static bool PathsPatch()
        {
            Logger.Info("Overwrote Exiled paths");
            Paths.Exiled = SixModLoader.Instance.ModManager.GetMod<ExiledMod>().Instance.ModDirectory;
            Paths.Plugins = Path.Combine(Paths.Exiled, "Plugins");
            Paths.Configs = Paths.Exiled;
            Paths.Config = Path.Combine(Paths.Exiled, "config.yml");
            Paths.Log = Path.Combine(Paths.Exiled, "ra.log.txt");
            Paths.Dependencies = Path.Combine(Paths.Plugins, "dependencies");
            return false;
        }

        public static void ModsCommandPatch(ref string response)
        {
            var plugins = global::Exiled.Loader.Loader.Plugins;
            response += $"\nExiled plugins ({plugins.Count}): " + string.Join(", ", plugins.Select(mod => mod.Name));
        }
        
        public static void Load(Harmony harmony)
        {
            Logger.Info("Loading Exiled plugins");
            harmony.Patch(AccessTools.Method(typeof(Paths), nameof(Paths.Reload)), new HarmonyMethod(typeof(ExiledLoader), nameof(PathsPatch)));
            Paths.Reload();

            File.Open(Paths.Config, FileMode.OpenOrCreate, FileAccess.Read).Dispose();
            Timing.CallDelayed(0.25f, () =>
            {
                global::Exiled.Loader.Loader.Config.Environment = EnvironmentType.Production;
                global::Exiled.Loader.Loader.Run();

                try
                {
                    EventManager.Instance.Register(new EventsCompatibility());
                }
                catch (Exception e) when (e is InvalidOperationException || e is TypeLoadException || e is FileNotFoundException)
                {
                    Logger.Warn("Exiled Events plugin not found!");
                }

                harmony.Patch(AccessTools.Method(typeof(SixModLoaderCommand.ModsCommand), nameof(SixModLoaderCommand.ModsCommand.Execute)), postfix: new HarmonyMethod(typeof(ExiledLoader), nameof(ModsCommandPatch)));
            });
        }
    }
}