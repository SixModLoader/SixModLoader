using System.IO;
using System.Linq;
using CommandSystem.Commands;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;
using SixModLoader.Events;
using SixModLoader.Mods;
using SixModLoader.Patches;
using UnityEngine.SceneManagement;

namespace SixModLoader
{
    [Mod("SixModLoader")]
    public class SixModLoader
    {
        internal static void Main(string[] args)
        {
            Instance.Load();
        }

        public static SixModLoader Instance { get; } = new SixModLoader();

        public SemanticVersion Version { get; internal set; }

        public VersionRange TargetGameVersion { get; } = VersionRange.Parse("[10.1.1]");

        public bool IsGameCompatible => TargetGameVersion.Satisfies(GameVersionParser.Parse().ToNuGetVersion());

        public string DataPath { get; }
        public string BinPath { get; }
        public string ModsPath { get; }

        public ServiceCollection ServiceCollection { get; } = new ServiceCollection();
        public ServiceProvider Services => ServiceCollection.BuildServiceProvider();

        private SixModLoader()
        {
            ModManager = new ModManager(this);

            DataPath = Path.GetFullPath("SixModLoader");
            BinPath = Path.Combine(DataPath, "bin");
            ModsPath = Path.Combine(DataPath, "mods");
            FileLog.logPath = Path.Combine(DataPath, "harmony.log.txt");

            ServiceCollection
                .AddSingleton(this)
                .AddSingleton(ModManager)
                .AddSingleton(EventManager);
        }

        public Harmony Harmony { get; } = new Harmony("pl.js6pak.SixModLoader");
        public ModManager ModManager { get; }
        public EventManager EventManager { get; } = new EventManager();

        public void Load()
        {
            var loaderContainer = new ModContainer<SixModLoader>(this);
            Version = loaderContainer.Info.Version;
            ModManager.Mods.Add(loaderContainer);
            ServerOutputWrapper.Start();

            Logger.Info("Loaded!");

            Harmony.PatchAll<HarmonyPatches>();

            Directory.CreateDirectory(DataPath);
            Directory.CreateDirectory(ModsPath);

#if DEBUG
            Harmony.DEBUG = true;
#endif

            var loaded = false;
            SceneManager.sceneLoaded += (scene, _) =>
            {
                if (loaded)
                    return;
                loaded = true;

                Harmony.PatchAll();
                Logger.Debug($"Patched {Harmony.GetPatchedMethods().Count()} {"method".Pluralize(Harmony.GetPatchedMethods().Count())}");

                CustomNetworkManager.Modded = true;
                BuildInfoCommand.ModDescription = "SixModLoader\n" +
                                                  $"Version: {Version}" +
#if DEBUG
                                                  " - DEBUG BUILD (DON'T USE THIS IN PRODUCTION)" +
#endif
                                                  $"\nGame version: {GameVersionParser.Parse()}\n" +
                                                  $"Compatible game versions: {TargetGameVersion.ToShortString()} ({(IsGameCompatible ? "COMPATIBLE" : "INCOMPATIBLE")})";

                if (!IsGameCompatible)
                {
                    Logger.Warn("This SixModLoader build was designed for other SCP:SL version!");
                }

                new ModReloadEvent().Call();
                new ModEnableEvent().Call();
            };

            ModManager.Load();
        }
    }
}
