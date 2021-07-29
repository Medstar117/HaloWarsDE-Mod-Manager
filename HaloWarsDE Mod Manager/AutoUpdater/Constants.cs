using System.IO;

namespace AutoUpdater
{
    public static class Constants
    {
        // Configurable
        public const string ReleasePackageName = "AutoUpdatePackage.zip";
        public const string MainRepoURL = "https://api.github.com/repos/Medstar117/HWDE-Mod-Manager";

        // Pathing
        public static string InstallationDirectory = Directory.GetCurrentDirectory();
        public static string UpdatesDirectory = Path.Combine(InstallationDirectory, "Updates");
        public static string PrerequisitesDirectory = Path.Combine(UpdatesDirectory, "Prerequisites");
    }
}
