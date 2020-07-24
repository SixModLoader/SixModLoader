using NuGet.Versioning;

namespace SixModLoader
{
    public static class Utils
    {
        public static string Pluralize(this string text, int count)
        {
            return text + (count == 1 ? "" : "s");
        }

        public static NuGetVersion ToNuGetVersion(this SemanticVersion semanticVersion)
        {
            return new NuGetVersion(semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch, semanticVersion.ReleaseLabels, semanticVersion.Metadata);
        }
    }
}