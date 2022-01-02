// Built-in
using System;
using System.Windows;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;

// Nuget Packages
using Newtonsoft.Json;

// Personal - Namespaces
using HaloWarsDE_Mod_Manager.Shared.DataSerialization.AutoUpdater;

// Personal - Statics
using static HaloWarsDE_Mod_Manager.Shared.Main.Constants;
using static HaloWarsDE_Mod_Manager.Shared.AutoUpdater.Constants;

namespace HaloWarsDE_Mod_Manager.Modules
{
    public static class PatchData
    {
        public static Uri FileURI = null;
        public static Version Version = null;
        private static Uri RepoReleaseURI => new Uri($"{ApiRepoURL}/releases/latest");

        public static Uri GrabPatchFileUri(Release releaseData)
        {
            foreach (ReleaseAsset asset in releaseData.assets)
            {
                if (asset.name == ReleasePackageName)
                {
                    return new Uri(asset.browser_download_url);
                }
            }

            return null;
        }

        public static void FetchUpdateData()
        {
            try
            {
                using (WebClient github_client = new WebClient())
                {
                    github_client.Headers.Add("user-agent","HaloWarsDE-Mod-Manager"); // Required by Github's API

                    // Download JSON data
                    Logging.WriteLogEntry("Downloading JSON data for latest release...");
                    string releaseJSON = github_client.DownloadString(RepoReleaseURI);
                    Release releaseInfo = JsonConvert.DeserializeObject<Release>(releaseJSON);

                    // Assign values
                    Logging.WriteLogEntry("Parsing JSON data...");
                    FileURI = GrabPatchFileUri(releaseInfo);
                    Version = new Version(releaseInfo.tag_name);

                    Logging.WriteLogEntry((FileURI == null) ? "[ERROR] Failed to parse JSON info!" : "Found all data for updating!");
                }
            }
            catch (WebException)
            {
                Logging.WriteLogEntry("[ERROR] Could not fetch latest release info!");
            }
            catch (Exception e)
            {
                Logging.WriteExceptionInfo(e);
            }
        }
    }

    public class AutoUpdater
    {
        public static string PackagePath;
        public static bool? UpdateExists = false;
        public static DownloadWindow downWin = new DownloadWindow();

        public static void CheckForUpdates()
        {
            Logging.WriteLogEntry("Checking for updates...");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            PatchData.FetchUpdateData();
            UpdateExists = PatchData.FileURI != null ? PatchData.Version > new Version(ManagerVer) : (bool?)null;
        }

        public static void InitUpdate()
        {
            try
            {
                // Clear existing Updates directory
                if (Directory.Exists(UpdatesDirectory))
                    Directory.Delete(UpdatesDirectory, true);

                // Create new updates directory
                Logging.WriteLogEntry($"Creating extraction directory \"{UpdatesDirectory}\"...");
                Directory.CreateDirectory(UpdatesDirectory);
                Application.Current.Dispatcher.BeginInvoke(new Action(delegate
                {
                    downWin.Show();
                }));

                // Download latest update package
                PackagePath = Path.Combine(UpdatesDirectory, ReleasePackageName);
                Thread thread = new Thread(() =>
                {
                    Logging.WriteLogEntry("Downloading latest update package...");
                    using (WebClient client = new WebClient())
                    {
                        // Event handlers
                        client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                        client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                        
                        // Downloading
                        client.Headers.Add("user-agent", "HaloWarsDE Mod Manager");
                        client.DownloadFileAsync(PatchData.FileURI, PackagePath);
                    }
                });
                thread.Start();
            }
            catch (WebException)
            {
                Logging.WriteLogEntry("[ERROR] Could not fetch latest release package!");
            }
            catch (Exception e)
            {
                Logging.WriteExceptionInfo(e);
            }
        }

        private static void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = (bytesIn / totalBytes) * 100;
                downWin.Pbar_DownloadProgress.Value = int.Parse(Math.Truncate(percentage).ToString());
            }));
        }

        private static void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                downWin.Close();

                // Launch installer
                Process installer = new Process();
                installer.StartInfo.UseShellExecute = false;
                installer.StartInfo.FileName = PackagePath;
                installer.Start();

                // Shut down and launch installer for updater
                Logging.WriteLogEntry("Shutting down for updates. Have a nice day! :)");
                Application.Current.Shutdown();
            }));
        }
    }
}
