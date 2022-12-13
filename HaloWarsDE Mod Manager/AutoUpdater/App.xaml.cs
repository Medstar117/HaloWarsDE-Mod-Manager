using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows;
using AutoUpdater.Core;
using AutoUpdater.Core.Serialization;
using Newtonsoft.Json;

namespace AutoUpdater
{
    public partial class App : Application
    {
        internal static MainWindow downloadWindow = new();

        internal static Release? ReleaseData      { get; private set; }
        internal static Uri? RemoteFileUri        => Utils.GetPatchFileUri(ReleaseData);
        internal static Version? RemoteAppVersion => ReleaseData != null ? new(ReleaseData.tag_name) : null;
        internal static bool UpdateExists         => (RemoteFileUri != null) && (RemoteAppVersion > Constants.CurrentAppVersion);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DownloadReleaseData();

            // if one exists, alert the user
            if (!UpdateExists)
            {
                Current.Shutdown(0);
                return;
            }

            // Ask if user wants to apply update
            MessageBoxResult result = MessageBox.Show($"A newer version of this mod manager is available. Would you like to update now?\n\n" +
                                                      $"{RemoteAppVersion} > {Constants.CurrentAppVersion}", "Update Available",
                                                      MessageBoxButton.YesNo, MessageBoxImage.Information);

            // Update denied
            if (result != MessageBoxResult.Yes)
            {
                Current.Shutdown(0);
                return;
            }

            // Clear existing updates directory
            if (Constants.UpdatesPath.Exists)
                Constants.UpdatesPath.Delete(true);

            Constants.UpdatesPath.Create();

            // Download the update
            downloadWindow.Show();

            Thread downloader = new(() =>
            {
                try
                {
                    Current.Dispatcher.Invoke(() => downloadWindow.ProgressBar.IsIndeterminate = false);

                    using (WebClient client = new())
                    {
                        // Event handlers
                        client.DownloadProgressChanged += UpdateProgressbar;
                        client.DownloadFileCompleted   += OnDownloadComplete;

                        // Downloading
                        client.Headers.Add("user-agent", "HaloWarsDE Mod Manager");
                        client.DownloadFileAsync(RemoteFileUri, Constants.PackagePath.FullName);
                    }
                }
                catch { Current.Dispatcher.Invoke(() => Current.Shutdown(0)); }
            });
            downloader.Start();
        }

        private void OnDownloadComplete(object? sender, AsyncCompletedEventArgs e)
        {
            Current.Dispatcher.Invoke(() =>
            {
                downloadWindow.Close();

                // Launch installer
                Process installer = new();
                installer.StartInfo.UseShellExecute = false;
                installer.StartInfo.FileName = Constants.PackagePath.FullName;
                installer.Start();

                // Shut down and launch installer for updater
                Current.Shutdown(1);
            });
        }

        private void UpdateProgressbar(object sender, DownloadProgressChangedEventArgs e)
        {
            Current.Dispatcher.Invoke(() =>
            {
                double bytesIn    = Convert.ToDouble(e.BytesReceived);
                double totalBytes = Convert.ToDouble(e.TotalBytesToReceive);
                double percentage = (bytesIn / totalBytes) * 100;

                downloadWindow.ProgressBar.Value = percentage;
                downloadWindow.SubLabel = $"Progress...{Math.Round(percentage, 2)}%";
            });
        }

        private static void DownloadReleaseData()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                using (WebClient client = new())
                {
                    // Add required :user-agent" for GitHub API request
                    client.Headers.Add("user-agent", Constants.RepoName);

                    // Deserialize JSON data into a parseable object
                    Console.WriteLine("Downloading JSON data for latest release...");
                    string releaseJSON = client.DownloadString(Constants.LatestReleaseUri);
                    ReleaseData = JsonConvert.DeserializeObject<Release>(releaseJSON);
                }
            }
            catch (WebException)
            {
                Console.WriteLine("[ERROR] Failed to querry GitHub API!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Source);
                Console.WriteLine(ex.Message);
            }
        }
    }
}
