using HaloWarsDE_Mod_Manager.Core.Diagnostics;
using HaloWarsDE_Mod_Manager.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Text;
using System.Threading.Tasks;

namespace HaloWarsDE_Mod_Manager.Core.Versioning
{
    // TODO: Possibly replace with WinGUP?

    internal static class AutoUpdater
    {
        private static readonly Uri RepoReleaseUri;
        private static readonly DownloadForm downloader;

        #region Constants
        // Configurable
        private static readonly string RepoOwner = "Medstar117";
        private static readonly string RepoUrl = "HaloWarsDE-Mod-Manager";
        private static readonly string ReleasePackageName = "AutoUpdatePackage.exe";

        // Urls
        private static readonly string ApiRepoUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoUrl}";
        private static readonly string GitHubRepoUrl = $"https://github.com/{RepoOwner}/{RepoUrl}";
        #endregion

        private static bool CheckConnectivity(Uri url, double timeoutSecs = 5)
        {
            int timeoutMS = Convert.ToInt32(timeoutSecs * 1000);
            Logger.LogInfo($"Checking internet connectivity to {url}...");

            // Build request
            HttpWebRequest githubRequest = (HttpWebRequest)WebRequest.Create(url);
            githubRequest.Timeout = timeoutMS;
            githubRequest.KeepAlive = false;
            githubRequest.UserAgent = "HWDE_MM";

            // Wait for response
            try
            {
                using (HttpWebResponse githubResponse = (HttpWebResponse)githubRequest.GetResponse()) return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Could not reach remote host. Please check your interntet connection");
                Logger.LogException(ex);
                return false;
            }
        }

        internal static bool CheckForUpdates()
        {
            if (!CheckConnectivity(RepoReleaseUri))
                return false;

            Logger.LogInfo("Checking for updates...");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11| SecurityProtocolType.Tls12;
        
            return true;
        }

        internal static void InitUpdate()
        {
            // Clear existing updates directory
            if (Directory.Exists(App.UpdaterPath))
            {
                Directory.Delete(App.UpdaterPath, true);
                Logger.LogInfo($"Creating new \"Updates\" folder...");
                Directory.CreateDirectory(App.UpdaterPath);
            }

            // Show downloader form
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => { downloader.Show(); }));


        }
    }
}
