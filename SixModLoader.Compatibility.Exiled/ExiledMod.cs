using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using MEC;
using Newtonsoft.Json.Linq;
using SharpCompress.Common;
using SharpCompress.Readers;
using SixModLoader.Api;
using SixModLoader.Api.Events.Server;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;
using SixModLoader.Mods;

namespace SixModLoader.Compatibility.Exiled
{
    [Mod("SixModLoader.Compatibility.Exiled")]
    [NuGetLibrary("Newtonsoft.Json", "12.0.3", "net45")]
    [NuPkgLibrary("SharpCompress", "0.25.1-default-encoding", "https://nuget.pkg.github.com/SixModLoader/download/SharpCompress/0.25.1-default-encoding/SharpCompress-0.25.1-default-encoding.nupkg", "net472")]
    public class ExiledMod
    {
        public SixModLoader Loader { get; }

        public bool Loaded { get; set; }
        public string ModDirectory { get; }

        [AutoHarmony]
        public Harmony Harmony { get; set; }

        public ExiledMod(SixModLoader loader)
        {
            Loader = loader;
            ModDirectory = Path.Combine(Loader.ModsPath, "Exiled");
        }

        private static string[] FilesToDownload { get; } = {"EXILED/Exiled.Loader.dll", "EXILED/Plugins/Exiled.Events.dll", "EXILED/Plugins/Exiled.Permissions.dll"};

        [EventHandler(typeof(ModEnableEvent))]
        public void OnEnable()
        {
            Task.Run(async () =>
            {
                var loaderPath = Path.Combine(ModDirectory, "Exiled.Loader.dll");
                var apiPath = Path.Combine(ModDirectory, "Plugins", "dependencies", "Exiled.API.dll");

                if (!File.Exists(loaderPath) || !File.Exists(apiPath))
                {
                    Logger.Info("Downloading Exiled...");
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SixModLoader", SixModLoader.Instance.Version.ToString()));

                    var releases = JArray.Parse(await httpClient.GetStringAsync("https://api.github.com/repos/galaxy119/EXILED/releases"));
                    var downloadUrl = releases.SelectToken(@"$[0].assets[?(@.name == 'Exiled.tar.gz')].browser_download_url")!.ToObject<string>();

                    var reader = ReaderFactory.Open(await httpClient.GetStreamAsync(downloadUrl));

                    var extractionOptions = new ExtractionOptions {Overwrite = true};
                    while (reader.MoveToNextEntry())
                    {
                        if (reader.Entry.IsDirectory)
                            continue;

                        var fileName = reader.Entry.Key;

                        if (FilesToDownload.Contains(fileName) || fileName.StartsWith("EXILED/Plugins/dependencies/"))
                        {
                            var path = Path.GetFullPath(ModDirectory) + fileName.Substring(fileName.IndexOf("/", StringComparison.Ordinal));

                            Directory.GetParent(path).Create();
                            reader.WriteEntryToFile(path, extractionOptions);
                            Logger.Info("Downloaded " + fileName);
                        }
                    }
                }

                Logger.Info("Loading Exiled");

                Logger.Info("Loaded " + Assembly.LoadFile(loaderPath));
                Logger.Info("Loaded " + Assembly.LoadFile(apiPath));

                Loaded = true;
            }).Wait();
        }

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

        [EventHandler(typeof(ServerConsoleReadyEvent))]
        public void OnServerConsoleReady()
        {
            if (!Loaded)
                return;

            Logger.Info("Loading Exiled plugins");
            Harmony.Patch(AccessTools.Method(typeof(Paths), nameof(Paths.Reload)), new HarmonyMethod(typeof(ExiledMod), nameof(PathsPatch)));
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

                Harmony.Patch(AccessTools.Method(typeof(SixModLoaderCommand.ModsCommand), nameof(SixModLoaderCommand.ModsCommand.Execute)), postfix: new HarmonyMethod(typeof(ExiledMod), nameof(ModsCommandPatch)));
            });
        }
    }
}