// Built-ins
using System;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
using System.Diagnostics;

// Personal
using HaloWarsDE_Mod_Manager;
using DataSerialization.Serializable;
using DataSerialization;

namespace Globals
{
    public static class Main
    {
        // ---------- No-Touchy Variables ----------

        // Manager-specific
        public static string ManagerVer = "1.0";
        public static readonly string ConfigFilePath = $"{Directory.GetCurrentDirectory()}\\Data\\UserConfig.dat";

        // Launch commands
        public const string Launch_HWDE_Steam = "/C start steam://rungameid/459220";
        public const string Launch_HWDE_MS = "/C start shell:AppsFolder\\Microsoft.BulldogThreshold_8wekyb3d8bbwe!xgameFinal";

        // ---------- Configurable Variables ----------
        public static string GameDistro = null;
        public static string UserModsFolder = null;
        public static bool LaunchedFromShortcut = false;
        public static int TimeoutDelay = 8;

        public static void ModScan()
        {
            /*********************************************************
             * Scan a user's mods folder for any mod manifest files
             * and dynamically add each mod's metadata into "ModList".
             ********************************************************/

            Logging.WriteLogEntry($"Loading mods from {UserModsFolder}...");

            // Clear the list, if there's any data in there.
            MainWindow.ModList.Clear();

            // Always manually add the Vanilla option.
            MainWindow.ModList.Add(new Mod(null));

            // Iterate recursively through the user's mods folder.
            int mod_count = 0;
            foreach (string f in Directory.EnumerateFiles(UserModsFolder, "*.hwmod", SearchOption.AllDirectories))
            {
                // Add mod to mod list
                MainWindow.ModList.Add(new Mod(f));
                mod_count += 1;
            }

            Logging.WriteLogEntry($"{mod_count} mod(s) detected and loaded.");
        }
    }

    public class Logging
    {
        private static readonly string LogFileDir = $"{Directory.GetCurrentDirectory()}\\Data\\Logs";
        private static readonly string CurrentLogFileName = $"AppLog [{DateTime.Now:MM-dd-yyyy HH_mm_ss}].txt";
        private static readonly string LogFilePath = Path.Combine(LogFileDir, CurrentLogFileName);

        public static void WriteLogEntry(string entry)
        {
            /***********************************************
             * Writes a given entry to the current log file.
             **********************************************/

            // Create logging directory
            if (!Directory.Exists(LogFileDir))
                _ = Directory.CreateDirectory(LogFileDir);

            // Build the textual log entry
            string log_entry = $"[{DateTime.Now:HH:mm:ss}] {entry}\n";

            // Write the log entry to the current log file.
            File.AppendAllText(LogFilePath, log_entry);
        }
    }

    public class ProgressBarManager
    {
        public static void InitProgressBar(int min = 0, int max = 100)
        {
            if (!Main.LaunchedFromShortcut)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType() == typeof(MainWindow))
                        {
                            (window as MainWindow).pBar.Minimum = min;
                            (window as MainWindow).pBar.Maximum = max;
                            (window as MainWindow).pBar.Value = 0;
                            (window as MainWindow).pBarLabel.Visibility = Visibility.Visible;
                            (window as MainWindow).pBar.Visibility = Visibility.Visible;
                        }
                    }
                });
            }
        }

        public static void ResetProgressBar()
        {
            if (!Main.LaunchedFromShortcut)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType() == typeof(MainWindow))
                        {
                            (window as MainWindow).pBarLabel.Visibility = Visibility.Hidden;
                            (window as MainWindow).pBar.Visibility = Visibility.Hidden;
                            (window as MainWindow).pBar.Value = 0;
                            (window as MainWindow).pBar.Minimum = 0;
                            (window as MainWindow).pBar.Maximum = 100;
                        }
                    }
                });
            }
        }

        public static void SetProgressBarData(int value, bool increment = false, string text = null)
        {
            if (!Main.LaunchedFromShortcut)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType() == typeof(MainWindow))
                        {
                            if (increment)
                                (window as MainWindow).pBar.Value += value;
                            else
                                (window as MainWindow).pBar.Value = value;

                            if (text != null)
                                (window as MainWindow).pBarLabel.Content = text;
                        }
                    }
                });
            }
        }
    }

    public class OmniUpdater
    {
        private static readonly string newUpdaterPath = $"{Directory.GetCurrentDirectory()}\\AutoUpdater.new";
        private static readonly string oldUpdaterPath = $"{Directory.GetCurrentDirectory()}\\AutoUpdater.exe";

        public static void PreCheck()
        {
            // Replace the old AutoUpdater.exe with the new one (if a new one exists)
            if (File.Exists(newUpdaterPath))
            {
                try
                {
                    if (File.Exists(oldUpdaterPath))
                        File.Delete(oldUpdaterPath);
                    File.Move(newUpdaterPath, oldUpdaterPath);
                }
                catch (IOException)
                {
                    // Wait 3 seconds, then try again
                    System.Threading.Thread.Sleep(3000);
                    if (File.Exists(oldUpdaterPath))
                        File.Delete(oldUpdaterPath);
                    File.Move(newUpdaterPath, oldUpdaterPath);
                }
            }

            // If ManagerData.dat exists, update the manager's version accordingly
            // before checking for updates.
            if (File.Exists(MainWindow.ManagerDataFilePath))
            {
                using (TextReader reader = new StringReader(File.ReadAllText(MainWindow.ManagerDataFilePath)))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ManagerData));
                    ManagerData managerData = (ManagerData)serializer.Deserialize(reader);
                    Main.ManagerVer = managerData.Version.PatchLevel;
                }
            }
        }

        public static void CheckForUpdates()
        {
            Logging.WriteLogEntry("Checking for updates...");

            // Set up the auto
            ProcessStartInfo AU_StartInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = Path.Combine(Directory.GetCurrentDirectory(), "AutoUpdater.exe"),
                Arguments = $"-c {Main.ManagerVer}"
            };
            Process AutoUpdater = new Process { StartInfo = AU_StartInfo };

            // Check if an update exists on GitHub
            Logging.WriteLogEntry("Launching AutoUpdater...");
            _ = AutoUpdater.Start();

            // Wait for the Auto-Updater to finish
            Logging.WriteLogEntry("Checking for updates...");
            AutoUpdater.WaitForExit();

            // Get the exit code of AutoUpdater to check if we neeed to update or not
            switch (AutoUpdater.ExitCode)
            {
                // No Update
                case 0:
                    Logging.WriteLogEntry("No update found; continuing with normal functionality.");
                    break;

                // Update
                case 1:
                    Logging.WriteLogEntry("Update detected. Prompting user for input.");
                    MessageBoxResult update_querry = MessageBox.Show("A newer version of this mod manager is available!\nWould you like to update now?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (update_querry == MessageBoxResult.No)
                        Logging.WriteLogEntry("User denied update; continuing with normal functionality.");

                    else
                    {
                        Logging.WriteLogEntry("User accepted update; launching AutoUpdater...");

                        // Get current PID and overwrite AutoUpdater arguments
                        Process CurrentProcess = Process.GetCurrentProcess();
                        AutoUpdater.StartInfo.CreateNoWindow = false;
                        AutoUpdater.StartInfo.Arguments = $"-u --auto {Main.ManagerVer} {CurrentProcess.Id}";

                        // Start the updater and close this manager
                        _ = AutoUpdater.Start();
                        Logging.WriteLogEntry("Shutting down for updates. Have a nice day! :)");
                        Application.Current.Shutdown();
                    }
                    break;

                default:
                    Logging.WriteLogEntry("[ERROR] Error in launching AutoUpdater.exe; proceeding with application startup.");
                    break;
            }
        }

        /*
        private static void CheckForModUpdates(dynamic modObject, bool isGit)
        {
            if (isGit)
            {
                using (var modRepo = new Repository(Path.Combine(Globals.UserModsFolder, modObject.UpdatePath)))
                {
                    if (modRepo.Diff.Compare<TreeChanges>().Count > 0)
                    {

                    }
                }
            }
            else
            {

            }
        }
        */
    }
}
