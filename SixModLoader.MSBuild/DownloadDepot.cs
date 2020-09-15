using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DepotDownloader;
using Microsoft.Build.Framework;

namespace SixModLoader.MSBuild
{
    public class DownloadDepot : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string InstallDirectory { get; set; }

        public uint AppId { get; set; } = 996560; // SCP: Secret Laboratory Dedicated Server
        public uint DepotId { get; set; } = 996562; // Linux Depot
        public ulong ManifestId { get; set; } = ContentDownloader.INVALID_MANIFEST_ID;
        public string Branch { get; set; } = "public";
        public string Password { get; set; } = null;

        public string[] Files { get; set; } =
        {
            "SCPSL_Data/Managed/Assembly-CSharp.dll",
            "SCPSL_Data/Managed/Assembly-CSharp-firstpass.dll",
            "SCPSL_Data/Managed/CommandSystem.Core.dll",

            "SCPSL_Data/Managed/Mirror.dll",
            "SCPSL_Data/Managed/UnityEngine.dll",
            "SCPSL_Data/Managed/UnityEngine.CoreModule.dll",
            "SCPSL_Data/Managed/UnityEngine.PhysicsModule.dll"
        };

        public override bool Execute()
        {
            DepotConfigStore.LoadFromFile(Path.Combine(InstallDirectory, ".DepotDownloader", "depot.config"));
            if (DepotConfigStore.Instance.InstalledManifestIDs.TryGetValue(DepotId, out var installedManifest))
            {
                var missing = Files.Where(x => !File.Exists(Path.Combine(InstallDirectory, x))).ToArray();
                if (missing.Any())
                {
                    Log.LogMessage(MessageImportance.High, $"Missing [{string.Join(", ", missing)}] reinstalling");
                }
                else
                {
                    if (ManifestId == installedManifest)
                    {
                        Log.LogMessage(MessageImportance.High, "Correct SCP:SL version installed, skipping");
                        return true;
                    }

                    Log.LogMessage(MessageImportance.High, "Different SCP:SL version installed, reinstalling");
                }
            }

            Log.LogMessage(MessageImportance.High, "Downloading SCP:SL to " + InstallDirectory);

            if (!AccountSettingsStore.Loaded)
            {
                AccountSettingsStore.LoadFromFile("account.config");
            }

            ContentDownloader.InitializeSteam3(null, null);

            ContentDownloader.Config.MaxServers = 32;
            ContentDownloader.Config.MaxDownloads = 8;

            ContentDownloader.Config.BetaPassword = Password;
            ContentDownloader.Config.InstallDirectory = InstallDirectory;
            ContentDownloader.Config.UsingFileList = true;
            ContentDownloader.Config.FilesToDownloadRegex = new List<Regex>();
            ContentDownloader.Config.FilesToDownload = new List<string>();

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            foreach (var fileEntry in Files)
            {
                try
                {
                    string fileEntryProcessed;
                    if (isWindows)
                    {
                        // On Windows, ensure that forward slashes can match either forward or backslashes in depot paths
                        fileEntryProcessed = fileEntry.Replace("/", "[\\\\|/]");
                    }
                    else
                    {
                        // On other systems, treat / normally
                        fileEntryProcessed = fileEntry;
                    }

                    var rgx = new Regex(fileEntryProcessed, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    ContentDownloader.Config.FilesToDownloadRegex.Add(rgx);
                }
                catch
                {
                    // For anything that can't be processed as a Regex, allow both forward and backward slashes to match
                    // on Windows
                    if (isWindows)
                    {
                        ContentDownloader.Config.FilesToDownload.Add(fileEntry.Replace("/", "\\"));
                    }

                    ContentDownloader.Config.FilesToDownload.Add(fileEntry);
                }
            }

            ContentDownloader.DownloadAppAsync(AppId, DepotId, ManifestId, Branch, "linux", "64", null, false, false).ConfigureAwait(false).GetAwaiter().GetResult();
            ContentDownloader.ShutdownSteam3();
            Log.LogMessage(MessageImportance.High, "Downloaded SCP:SL");

            return true;
        }
    }
}