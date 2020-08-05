using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using NuGet.Versioning;
using Octokit;
using SharpCompress.Common;
using SharpCompress.Readers;
using SixModLoader.Api;
using SixModLoader.Api.Events.Server;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;
using SixModLoader.Launcher.EasyMetadata;
using SixModLoader.Mods;

namespace SixModLoader.Compatibility.Exiled
{
    [NuGetLibrary("Octokit", "0.48.0", "net46")]
    [NuGetLibrary("SharpCompress", "0.26.0", "net46")]

    #region EasyMetadata

    [GithubPackageLibrary("SixModLoader.Launcher.EasyMetadata", "0.1.0", "SixModLoader", "net472")]
    [NuGetLibrary("System.Reflection.Metadata", "1.8.1", "net461")]
    [NuGetLibrary("System.Collections.Immutable", "1.7.1", "netstandard2.0")]

    #endregion

    [Mod("SixModLoader.Compatibility.Exiled")]
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

                SemanticVersion version = null;

                if (File.Exists(loaderPath) && File.Exists(apiPath))
                {
                    var assemblyInfo = new AssemblyInfo(loaderPath);

                    if (SemanticVersion.TryParse(assemblyInfo.Version, out version))
                    {
                        Logger.Info("Exiled " + version);
                    }
                    else
                    {
                        Logger.Error("Corrupted Exiled assembly!");
                    }
                }

                var gitHubClient = new GitHubClient(new ProductHeaderValue("SixModLoader", SixModLoader.Instance.Version.ToString()));

                var rateLimit = await gitHubClient.Miscellaneous.GetRateLimits();
                if (rateLimit.Resources.Core.Remaining <= 0)
                {
                    Logger.Warn($"GitHub API rate limit reached, skipping auto update. (try again in {rateLimit.Resources.Core.Reset})");
                }
                else
                {
                    var releases = await gitHubClient.Repository.Release.GetAll("galaxy119", "EXILED");

                    var newerRelease = releases
                        // .Where(x => version == null || x.Prerelease == version?.IsPrerelease) TODO wait for exiled to stop abusing semver
                        .Select(x => (Release: x, Version: SemanticVersion.TryParse(x.TagName, out var v) ? v : null))
                        .Where(x => x.Version != null)
                        .OrderByDescending(x => x.Version)
                        .FirstOrDefault(x => x.Version.CompareTo(version) > 0);

                    if (newerRelease != default)
                    {
                        Logger.Info("Updating Exiled to version: " + newerRelease.Version);
                        using var httpClient = new HttpClient();

                        var reader = ReaderFactory.Open(await httpClient.GetStreamAsync(newerRelease.Release.Assets.Single(x => x.Name == "Exiled.tar.gz").BrowserDownloadUrl));

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
                }

                Logger.Info("Loading Exiled");

                Logger.Info("Loaded " + Assembly.LoadFile(loaderPath));
                Logger.Info("Loaded " + Assembly.LoadFile(apiPath));

                Loaded = true;
            }).Wait();
        }

        [EventHandler(typeof(ServerConsoleReadyEvent))]
        public void OnServerConsoleReady()
        {
            if (!Loaded)
                return;

            ExiledLoader.Load(Harmony);
        }
    }
}