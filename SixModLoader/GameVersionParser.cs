using System;
using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace SixModLoader
{
    public static class GameVersionParser
    {
        private static Regex Regex { get; } = new Regex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)( \((?<label>[\w ]+) (?<label_version>\d+)\)$)?", RegexOptions.Compiled);

        public static SemanticVersion Parse()
        {
            var version = CustomNetworkManager.CompatibleVersions[0];
            var match = Regex.Match(version);

            var labels = string.Empty;

            var label = match.Groups["label"];
            if (label.Success)
            {
                switch (label.Value)
                {
                    case "Release Candidate":
                        labels += "rc";
                        break;
                }
            }

            var labelVersion = match.Groups["label_version"];
            if (labelVersion.Success)
            {
                if (!label.Success)
                {
                    throw new InvalidOperationException("Label version exists but not label");
                }

                labels += "." + int.Parse(labelVersion.Value);
            }

            return new SemanticVersion(int.Parse(match.Groups["major"].Value), int.Parse(match.Groups["minor"].Value), int.Parse(match.Groups["patch"].Value), labels);
        }
    }
}