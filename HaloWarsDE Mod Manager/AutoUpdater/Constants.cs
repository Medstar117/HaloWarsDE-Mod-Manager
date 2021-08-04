using System.IO;

namespace AutoUpdater
{
    public static class Constants
    {
        // Configurable
        public const string RepoOwner = "Medstar117";
        public const string RepoURL = "HaloWarsDE-Mod-Manager";

        public const string ReleasePackageName = "AutoUpdatePackage.zip";
        public static readonly string ApiRepoURL = "https://api.github.com/repos/" + $"{RepoOwner}/{RepoURL}";
        public static readonly string GithubRepoURL = "https://github.com/" + $"{RepoOwner}/{RepoURL}";

        // Pathing
        public static string InstallationDirectory = Directory.GetCurrentDirectory();
        public static string UpdatesDirectory = Path.Combine(InstallationDirectory, "Updates");
        public static string PrerequisitesDirectory = Path.Combine(UpdatesDirectory, "Prerequisites");
    }
}
