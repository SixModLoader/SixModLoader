using System.Collections.Generic;
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

        public uint AppId { get; set; } = 996560;
        public uint DepotId { get; set; } = ContentDownloader.INVALID_DEPOT_ID;
        public ulong ManifestId { get; set; } = ContentDownloader.INVALID_MANIFEST_ID;
        public string Branch { get; set; } = "public";

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, "Downloading SCP:SL references to " + InstallDirectory);

            if (!AccountSettingsStore.Loaded)
            {
                AccountSettingsStore.LoadFromFile("account.config");
            }

            ContentDownloader.InitializeSteam3(null, null);

            ContentDownloader.Config.MaxServers = 32;
            ContentDownloader.Config.MaxDownloads = 8;

            ContentDownloader.Config.InstallDirectory = InstallDirectory;
            ContentDownloader.Config.UsingFileList = true;
            ContentDownloader.Config.FilesToDownloadRegex = new List<Regex>();
            ContentDownloader.Config.FilesToDownload = new List<string>();

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            foreach (var fileEntry in new[]
            {
                "SCPSL_Data/Managed/Assembly-CSharp.dll",
                "SCPSL_Dzata/Managed/Assembly-CSharp-firstpass.dll",
                "SCPSL_Data/Managed/CommandSystem.Core.dll",

                "SCPSL_Data/Managed/Mirror.dll",
                "SCPSL_Data/Managed/UnityEngine.dll",
                "SCPSL_Data/Managed/UnityEngine.CoreModule.dll",
                "SCPSL_Data/Managed/UnityEngine.PhysicsModule.dll"
            })
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

            ContentDownloader.DownloadAppAsync(AppId, DepotId, ManifestId, Branch, "linux", "64", null, false, false).GetAwaiter().GetResult();

            return true;
        }
    }
}