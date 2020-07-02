using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.Loader;
using HarmonyLib;
using MEC;
using SixModLoader.Api.Events.Server;
using SixModLoader.Events;
using SixModLoader.Mods;

namespace SixModLoader.Compatibility.Exiled
{
    [Mod]
    public class ExiledMod
    {
        public SixModLoader Loader { get; }

        public bool Loaded { get; set; }
        public string ModPath { get; }

        public ExiledMod(SixModLoader loader)
        {
            Loader = loader;
            ModPath = Path.Combine(Loader.ModsPath, "Exiled");
        }

        [EventHandler(typeof(ModEnableEvent))]
        public void OnEnable()
        {
            Task.Run(() =>
            {
                var loaderPath = Path.Combine(ModPath, "Exiled.Loader.dll");
                var apiPath = Path.Combine(ModPath, "Plugins", "dependencies", "Exiled.API.dll");

                if (File.Exists(loaderPath) && File.Exists(apiPath))
                {
                    Logger.Info("Loading Exiled");

                    Assembly.LoadFile(loaderPath);
                    Assembly.LoadFile(apiPath);
                    Loaded = true;
                    return;
                }

                Logger.Error("Exiled not found and downloading is borken because github and exiled ci gay");

/*
                Logger.Info("Downloading Exiled...");
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SixModLoader", "0.0.1.0"));
                    var runsResponse = JsonConvert.DeserializeObject<WorkflowRunsResponse>(await httpClient.GetStringAsync("https://api.github.com/repos/galaxy119/EXILED/actions/runs"));
                    Logger.Warn(runsResponse.TotalCount);
                    var run = runsResponse.WorkflowRuns.FirstOrDefault(x => x.HeadBranch == "2.0.0");
                    if (run == null)
                    {
                        Logger.Error("Workflow run not found!");
                        return;
                    }

                    var artifactsResponse = JsonConvert.DeserializeObject<WorkflowArtifactsResponse>(await httpClient.GetStringAsync(run.ArtifactsUrl));
                    var artifact = artifactsResponse.WorkflowArtifacts.FirstOrDefault(x => x.Name == "EXILED DLLs");
                    if (artifact == null)
                    {
                        Logger.Error("Workflow artifact not found!");
                        return;
                    }

                    using (var archive = new ZipArchive(await httpClient.GetStreamAsync(artifact.ArchiveDownloadUrl), ZipArchiveMode.Read))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            Logger.Info(entry.FullName);
                        }
                    }
                }
*/
            }).Wait();
        }

        [EventHandler(typeof(ServerConsoleReadyEvent))]
        public void OnServerConsoleReady()
        {
            if (!Loaded)
                return;

            Logger.Info("Loading Exiled plugins");
            Paths.Exiled = ModPath;
            Paths.Plugins = Path.Combine(Paths.Exiled, "Plugins");
            Paths.Config = Path.Combine(Paths.Exiled, "config.yml");
            Paths.Log = Path.Combine(Paths.Exiled, $"ra.log.txt");
            Paths.Dependencies = Path.Combine(Paths.Plugins, "dependencies");

            File.Open(Paths.Config, FileMode.OpenOrCreate, FileAccess.Read).Dispose();
            Timing.CallDelayed(0.25f, () =>
            {
                AccessTools.Method(typeof(PluginManager), "LoadAllDependencies").Invoke(null, new object[0]);
                PluginManager.LoadAll();

                try
                {
                    EventManager.Instance.Register(new EventsCompatibility());
                }
                catch (Exception e) when (e is InvalidOperationException || e is TypeLoadException || e is FileNotFoundException)
                {
                    Logger.Warn("Exiled Events plugin not found!");
                }
            });
        }
    }
}