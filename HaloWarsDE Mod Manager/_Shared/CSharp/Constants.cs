using System;
using System.IO;
using System.Diagnostics;


namespace HaloWarsDE_Mod_Manager.Shared
{
    namespace Main
    {
        public static class Constants
        {
            // General
            public static string ManagerVer = FileVersionInfo.GetVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.FriendlyName)).FileVersion;
            public static readonly string ConfigFilePath = $"{Directory.GetCurrentDirectory()}\\Data\\UserConfig.dat";

            // LocalAppData Paths
            private static readonly string LocalAppData_System = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            public static readonly string LocalAppData_MS = $"{LocalAppData_System}\\Packages\\Microsoft.BulldogThreshold_8wekyb3d8bbwe\\LocalState";
            public static readonly string LocalAppData_Steam = $"{LocalAppData_System}\\Halo Wars";

            // Internal Folder and File Paths
            public static readonly string ManagerDataFilePath = $"{Directory.GetCurrentDirectory()}\\Data\\ManagerData.dat";
            public static readonly string DefaultUserModsFolder = $"{Directory.GetCurrentDirectory()}\\HWDE Mods";

            // Launch Commands
            public const string Launch_HWDE_Steam = "/C start steam://rungameid/459220";
            public const string Launch_HWDE_MS = "/C start shell:AppsFolder\\Microsoft.BulldogThreshold_8wekyb3d8bbwe!xgameFinal";
        }
    }

    namespace AutoUpdater
    {
        public static class Constants
        {
            // Configurable
            public const string RepoOwner = "Medstar117";
            public const string RepoURL = "HaloWarsDE-Mod-Manager";

            public const string ReleasePackageName = "AutoUpdatePackage.exe";
            public static readonly string ApiRepoURL = "https://api.github.com/repos/" + $"{RepoOwner}/{RepoURL}";
            public static readonly string GithubRepoURL = "https://github.com/" + $"{RepoOwner}/{RepoURL}";

            // Pathing
            public static string InstallationDirectory = Directory.GetCurrentDirectory();
            public static string UpdatesDirectory = Path.Combine(InstallationDirectory, "Updates");
        }
    }
}