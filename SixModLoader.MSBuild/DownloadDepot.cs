using System.Collections.Generic;
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
        public string OperatingSystem { get; set; } = "linux";

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
            ContentDownloader.Config.FilesToDownload = new List<string>
            {
                "SCPSL_Data/Managed/Assembly-CSharp.dll",
                "SCPSL_Data/Managed/Assembly-CSharp-firstpass.dll",
                "SCPSL_Data/Managed/CommandSystem.Core.dll",

                "SCPSL_Data/Managed/Mirror.dll",
                "SCPSL_Data/Managed/UnityEngine.dll",
                "SCPSL_Data/Managed/UnityEngine.CoreModule.dll",
                "SCPSL_Data/Managed/UnityEngine.PhysicsModule.dll"
            };

            ContentDownloader.DownloadAppAsync(AppId, DepotId, ManifestId, Branch, OperatingSystem, null, null, false, false).GetAwaiter().GetResult();

            return true;
        }
    }
}