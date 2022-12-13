using System;
using System.IO;
using System.Diagnostics;

namespace AutoUpdater.Core
{
    internal static class Constants
    {
        // The path to the app we want to check the versioning of
        public static readonly string AppName = "HaloWarsDE Mod Manager";

        // Configurable GitHub-related fields
        public static readonly string RepoOwner = "Medstar117";
        public static readonly string RepoName  = "HaloWarsDE-Mod-Manager";
        public static readonly string ReleasePackageName = "AutoUpdatePackage.exe";

#pragma warning disable CS8604 // Possible null reference argument.
        public static Version? CurrentAppVersion => new(FileVersionInfo.GetVersionInfo(AppPath.FullName).FileVersion);
#pragma warning restore CS8604 // Possible null reference argument.

        #region Filepaths
        public static FileInfo AppPath          => new(Path.Combine(Directory.GetCurrentDirectory(), $"{AppName}.exe"));
        public static DirectoryInfo UpdatesPath => new(Path.Combine(Directory.GetCurrentDirectory(), "Updates"));
        public static FileInfo PackagePath      => new(Path.Combine(UpdatesPath.FullName, ReleasePackageName));
        #endregion

        #region Uri Stuff
        public static Uri ApiRepoUrl       => new($"https://api.github.com/repos/{RepoOwner}/{RepoName}");
        public static Uri GitHubRepoUrl    => new($"https://github.com/{RepoOwner}/{RepoName}");
        public static Uri LatestReleaseUri => new($"{ApiRepoUrl}/releases/latest");
        #endregion
    }
}
