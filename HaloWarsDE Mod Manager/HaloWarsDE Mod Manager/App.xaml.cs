// Built-ins
using System;
using System.Reflection;
using System.Windows;
using System.IO;
using Monitor.Core.Utilities;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.Security.AccessControl;

// Packages
using Microsoft.Toolkit.Uwp.Notifications;

// Personal - Namespaces
using UWP.ProcFetcher;
using HaloWarsDE_Mod_Manager.Modules;
using HaloWarsDE_Mod_Manager.Shared.DataSerialization.Internal;
using HaloWarsDE_Mod_Manager.Shared.DataSerialization.Mods;

// Personal - Statics
using static HaloWarsDE_Mod_Manager.Shared.Main.Constants;
using static HaloWarsDE_Mod_Manager.Shared.AutoUpdater.Constants;
using static Medstar.CodeSnippets.PermissionsManager;

namespace HaloWarsDE_Mod_Manager
{
    public partial class App : Application
    {
        #region Global Variables
        // General
        public static ObservableCollection<ModObj> ModList = new ObservableCollection<ModObj>();
        public static bool LaunchedFromShortcut = false;

        // Windows
        public static MainWindow window_main = null;

        // Manager-needed dirs and files
        public static string ModManifestFile = null;
        public static string UserModsFolder = null;

        // Set by user config file
        public static string GameDistro = null;
        public static string LaunchCommand = null;
        public static string LocalAppDataFolder = null;
        public static int TimeoutDelay = 8;
        #endregion

        #region Startup and Exit
        private void OnStartUp(object sender, StartupEventArgs e)
        {
            try
            {
                Logging.WriteLogEntry("Application started!");

                // Check for existing manager instance
                if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
                {
                    MessageBox.Show("An existing instance of the Halo Wars: DE Mod Manager is running!", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown();
                }
                else
                {
                    // Clear existing Updates directory
                    if (Directory.Exists(UpdatesDirectory))
                        Directory.Delete(UpdatesDirectory, true);

                    bool updateDenied = false;
                    AutoUpdater.CheckForUpdates();
                    if (AutoUpdater.UpdateExists == true)
                    {
                        Logging.WriteLogEntry("Update available! Prompting user for input...");
                        MessageBoxResult update_querry = MessageBox.Show("A newer version of this mod manager is available!\nWould you like to update now?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

                        if (update_querry == MessageBoxResult.Yes)
                        {
                            Logging.WriteLogEntry("User accepted update. Downloading new version from repository...");
                            AutoUpdater.InitUpdate();
                        }
                        else
                        {
                            Logging.WriteLogEntry("User denied update. Continuing initialization...");
                            updateDenied = true;
                        }
                    }
                    else if (AutoUpdater.UpdateExists == false)
                    {
                        Logging.WriteLogEntry("No update available.");
                    }
                    else
                    {
                        Logging.WriteLogEntry("[ERROR] Error caught in AutoUpdater. Continuing initialization...");
                    }

                    #region Unused
                    /*
                    // Check for pending changes
                    AutoUpdater.PreCheck();
                    AutoUpdater.CheckForUpdates();
                    bool updateDenied = false;

                    // Check for updates; if true, continue init; false when there's a remote update
                    if (AutoUpdater.UpdateExists)
                    {
                        Logging.WriteLogEntry("Prompting user for input.");
                        MessageBoxResult update_querry = MessageBox.Show("A newer version of this mod manager is available!\nWould you like to update now?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

                        if (update_querry == MessageBoxResult.Yes)
                        {
                            Logging.WriteLogEntry("User accepted update; launching AutoUpdater...");
                            AutoUpdater.InitUpdate();
                        }
                        else
                        {
                            Logging.WriteLogEntry("User denied update; continuing with normal functionality.");
                            updateDenied = true;
                        }
                    }
                    */
                    #endregion

                    // Catch just in case the script tries to continue when AutoUpdate tries to launch
                    if (AutoUpdater.UpdateExists == false || AutoUpdater.UpdateExists == null || (AutoUpdater.UpdateExists == true && updateDenied))
                    {
                        ConfigHandler.Run();

                        if (!Directory.Exists(LocalAppDataFolder))
                        {
                            MessageBox.Show("Game LocalAppData directory not found.\n\nPlease ensure that you have a legally obtained game copy.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Logging.WriteLogEntry("[ERROR] Game LocalAppData directory not found! Exiting application...");
                            Current.Shutdown();
                        }

                        // Scan for mods
                        ModScan();

                        // Determine if the manager was launched headless
                        if (e.Args.Length > 1)
                        {
                            for (int index = 1; index < e.Args.Length; index++)
                            {
                                if (e.Args[index] == "--mod_id")
                                {
                                    foreach (ModObj mod in ModList)
                                    {
                                        if (mod.ModID == e.Args[index + 1])
                                        {
                                            LaunchedFromShortcut = true;
                                            PlayGame(mod);
                                            ExitManager();
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            window_main = new MainWindow();
                            window_main.Show();
                        }
                    }
                }
            }
            catch (Exception unhandledException)
            {
                Logging.WriteExceptionInfo(unhandledException);
                Logging.WriteLogEntry("Variable dump:");
                Logging.DumpData(typeof(Shared.Main.Constants));    // Change this
            }
        }

        public static void ExitManager()
        {
            /**************************************************************** 
            * Exits the manager and cleans up the Halo Wars AppData directory
            * of any leftover traces of a mod. This is to ensure that a mod
            * is never accidentally loaded when one is not using the manager.
            ****************************************************************/

            // Close the program.
            Logging.WriteLogEntry("Application closed. Have a nice day; and thank you for using my mod manager :)");
            ToastNotificationManagerCompat.Uninstall();
            Current.Shutdown();
        }
        #endregion

        #region Game Launching and Mod Management
        public static int GetGameProcessID(ref Stopwatch sw)
        {
            sw.Restart();
            int GameProcessID = -1;
            while (GameProcessID == -1)
            {
                if (sw.ElapsedMilliseconds > TimeoutDelay * 1000) { throw new TimeoutException(); }
                switch (GameDistro)
                {
                    case "Steam":
                        Process[] ProcSnapshot = Process.GetProcessesByName("xgameFinal");
                        if (ProcSnapshot.Length > 0)
                            foreach (Process proc in ProcSnapshot)
                                if (proc.ProcessName.Contains("xgameFinal"))
                                    GameProcessID = proc.Id;
                        break;

                    case "Microsoft Store":
                        GameProcessID = ProcFetcher.GetProcessID("xgameFinal");
                        break;
                }
            }
            sw.Stop();
            return GameProcessID;
        }

        public static void PlayGame(ModObj modObject)
        {
            /*******************************************
			* Launch the game and load the selected mod.
			*******************************************/

            // Init stuff
            bool Start_OK = true;
            ProgressBarManager.InitProgressBar(0, 5);

            // Disable play button to prevent multiple launches
            if (!LaunchedFromShortcut)
                Current.Dispatcher.Invoke(() => { window_main.PlayButton.IsEnabled = false; });

            #region Set up LocalAppData for mod or Vanilla gameplay
            try
            {
                ProgressBarManager.SetProgressBarData(1, true, "Cleaning LocalAppData folder...");
                if (CleanLocalAppData())
                {
                    // Write new ModManifest.txt file and junction the selected mod's ModData folder
                    if (modObject.ModID != null && modObject.IsValid)
                    {
                        ProgressBarManager.SetProgressBarData(1, true, $"Linking mod \"{modObject.Title}\" information to LocalAppData folder...");

                        AddDirectorySecurity(Path.Combine(modObject.ManifestDirectory, "ModData"), SID.AllApplicationPackages,
                                        FileSystemRights.Read, AccessControlType.Allow);

                        // Create junction in LocalAppData folder pointing to mod's ModData folder
                        JunctionPoint.Create(Path.Combine(LocalAppDataFolder, "ModData"), Path.Combine(modObject.ManifestDirectory, "ModData"), true);

                        // Create ModManifest.txt pointing to the ModData directory junction
                        using (StreamWriter manifest = File.CreateText(ModManifestFile))
                            manifest.WriteLine(Path.Combine(LocalAppDataFolder, "ModData"));

                        Logging.WriteLogEntry("Loading selected mod: " + modObject.Title);
                    }
                    else if (modObject.ModID == null && modObject.IsVanilla)
                    {
                        ProgressBarManager.SetProgressBarData(1, true, "Loading vanilla game...");
                        Logging.WriteLogEntry("Loading vanilla game...");
                    }
                    else
                    {
                        Logging.WriteLogEntry($"[WARNING] Selected mod \"{modObject.Title}\" is not valid! Incorrect or missing \"ModID\" element in ModManifest.");
                        MessageBox.Show($"Selected mod \"{modObject.Title}\" is not valid due to an incorrect or lack of ModManifest element \"ModID\".\nThe mod author may have forgotten to update their mod's manifest file!", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Start_OK = false;
                    }
                }
                else
                {
                    Logging.WriteLogEntry($"[WARNING] Could not clean LocalAppData directory of old mod files! Aborting game launch...");
                    Start_OK = false;
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLogEntry($"[ERROR] Unknown exception occurred! Details are as follows:\n\nStackTrace:\n{ex.StackTrace}\n\nMessage:\n{ex.Message}");
                MessageBox.Show("Error in launching game. See most recent log for more details.", "Launch Error", MessageBoxButton.OK);
                Start_OK = false;
            }
            #endregion

            #region Launch the game and handle how everything closes
            try
            {
                if (Start_OK)
                {
                    // Configure process' info.
                    ProgressBarManager.SetProgressBarData(1, true, "Launching Halo Wars: Definitive Edition...");
                    ProcessStartInfo CMD_StartInfo = new ProcessStartInfo
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        FileName = "cmd.exe",
                        Arguments = LaunchCommand
                    };

                    // Create a new process to start the game.
                    Process GameCMD = new Process { StartInfo = CMD_StartInfo };
                    Stopwatch sw = new Stopwatch();

                    // Start the game!
                    GameCMD.Start(); sw.Start();
                    while (!GameCMD.HasExited) { if (sw.ElapsedMilliseconds > TimeoutDelay * 1000) throw new TimeoutException(); }
                    sw.Stop();

                    // If all goes well, fetch the game's PID and wait for the game to exit
                    ProgressBarManager.SetProgressBarData(1, true, "Fetching PID for Halo Wars: Definitive Edition...");
                    Process GameProcess = Process.GetProcessById(GetGameProcessID(ref sw));
                    ProgressBarManager.SetProgressBarData(1, true, $"Caught process for Halo Wars: Definitive Edition! PID: {GameProcess.Id}");
                    if (!LaunchedFromShortcut)
                    {
                        Current.Dispatcher.Invoke(() => window_main.WindowState = WindowState.Minimized);
                        new ToastContentBuilder()
                            .AddText("Game Process Caught!")
                            .AddText("Mod manager has been minimized to taskbar")
                            .Show(toast =>
                            {
                                toast.ExpirationTime = DateTime.Now.AddSeconds(5);
                            });
                    }
                    GameProcess.WaitForExit();
                }
            }
            catch (TimeoutException)
            {
                Logging.WriteLogEntry("[ERROR] Process took too long to start!");
                MessageBox.Show("[ERROR] Process took too long to start!", "Timeout Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                Logging.WriteLogEntry($"[ERROR] Unknown exception occurred! Details are as follows:\n\nStackTrace:\n{ex.StackTrace}\n\nMessage:\n{ex.Message}");
                MessageBox.Show("Error in launching game. See most recent log for more details.", "Launch Error", MessageBoxButton.OK);
            }

            // Clean up and reset
            CleanLocalAppData();
            if (modObject.ModID != null && modObject.IsValid)
                RemoveDirectorySecurity(Path.Combine(modObject.ManifestDirectory, "ModData"), SID.AllApplicationPackages,
                                        FileSystemRights.Read, AccessControlType.Allow);

            ProgressBarManager.ResetProgressBar();
            if (!LaunchedFromShortcut)
            {
                Current.Dispatcher.Invoke(() =>
                {
                    window_main.PlayButton.IsEnabled = true;
                    window_main.WindowState = WindowState.Normal;
                    SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                });
            }
            #endregion
        }

        public static void ModScan()
        {
            /*********************************************************
             * Scan a user's mods folder for any mod manifest files
             * and dynamically add each mod's metadata into "ModList".
             ********************************************************/

            Logging.WriteLogEntry($"Loading mods from {UserModsFolder}...");

            // Clear the list, if there's any data in there.
            ModList.Clear();

            // Always manually add the Vanilla option.
            ModList.Add(new ModObj(null));

            // Iterate recursively through the user's mods folder.
            int mod_count = 0;
            foreach (string f in Directory.EnumerateFiles(UserModsFolder, "*.hwmod", SearchOption.AllDirectories))
            {
                // Add mod to mod list
                ModList.Add(new ModObj(f));
                mod_count += 1;
            }

            Logging.WriteLogEntry($"{mod_count} mod(s) detected and loaded.");
        }

        private static bool CleanLocalAppData()
        {
            Logging.WriteLogEntry("Cleaning up Halo Wars: DE AppData directory...");

            try
            {
                // Remove ModManifest.txt.
                if (File.Exists(ModManifestFile))
                    File.Delete(ModManifestFile);

                // Delete any leftover ModData junctions.
                if (JunctionPoint.Exists(Path.Combine(LocalAppDataFolder, "ModData")))
                    JunctionPoint.Delete(Path.Combine(LocalAppDataFolder, "ModData"));

                // Double check on directory cleanup
                if (!JunctionPoint.Exists(Path.Combine(LocalAppDataFolder, "ModData")) && !File.Exists(ModManifestFile))
                {
                    Logging.WriteLogEntry("Halo Wars LocalAppData directory cleaned. All traces of any previously loaded mods have been purged.");
                    return true;
                }
                else
                    throw new IOException();
            }
            catch (IOException)
            {
                string error_msg = "Failure in cleaning the Halo Wars LocalAppData directory.\n\nPlease see the latest log file to navigate to the directory in question.";
                string error_log = $"Failure in cleaning the Halo Wars LocalAppData directory: '{LocalAppDataFolder}'. Please manually remove ModManifest.txt and/or the ModData folder located in the specified directory before launching the game without the manager.";

                MessageBox.Show(error_msg, "Cleanup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logging.WriteLogEntry("[ERROR] " + error_log);
                return false;
            }
        }
        #endregion

        #region From DllImport
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        #endregion
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
                Directory.CreateDirectory(LogFileDir);

            // Build the textual log entry
            string log_entry = $"[{DateTime.Now:HH:mm:ss}] {entry}\n";

            // Write the log entry to the current log file.
            File.AppendAllText(LogFilePath, log_entry);
        }

        public static void DumpData(Type obj)
        {
            // Use typeof(obj) when passing parameter
            WriteLogEntry("Dumping variable data from Globals...");
            foreach (PropertyInfo prop in obj.GetProperties())
            {
                // Check the type
                Type type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (type == typeof(string))
                    WriteLogEntry(prop.GetValue(obj, null).ToString());
            }
        }

        public static void WriteExceptionInfo(Exception exception)
        {
            WriteLogEntry("Unhandled exception caught. Dumping available data...");
            WriteLogEntry($"StackTrace: {exception.StackTrace}\n\nSource: {exception.Source}\n\nTargetSite: {exception.TargetSite}\n\nMessage: {exception.Message}");
            WriteLogEntry($"Additional information of any inner exceptions....");
            WriteLogEntry($"StackTrace: {exception.InnerException.StackTrace}\n\nSource: {exception.InnerException.Source}\n\nTargetSite: {exception.InnerException.TargetSite}\n\nMessage: {exception.InnerException.Message}");
        }
    }

    public class ProgressBarManager
    {
        public static void InitProgressBar(int min = 0, int max = 100)
        {
            if (!App.LaunchedFromShortcut)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    App.window_main.pBar.Minimum = min;
                    App.window_main.pBar.Maximum = max;
                    App.window_main.pBar.Value = 0;
                    App.window_main.pBarLabel.Visibility = Visibility.Visible;
                    App.window_main.pBar.Visibility = Visibility.Visible;
                });
            }
        }

        public static void ResetProgressBar()
        {
            if (!App.LaunchedFromShortcut)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    App.window_main.pBarLabel.Visibility = Visibility.Hidden;
                    App.window_main.pBar.Visibility = Visibility.Hidden;
                    App.window_main.pBar.Value = 0;
                    App.window_main.pBar.Minimum = 0;
                    App.window_main.pBar.Maximum = 100;
                });
            }
        }

        public static void SetProgressBarData(int value, bool increment = false, string text = null)
        {
            if (!App.LaunchedFromShortcut)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (increment)
                        App.window_main.pBar.Value += value;
                    else
                        App.window_main.pBar.Value = value;

                    if (text != null)
                        App.window_main.pBarLabel.Content = text;
                });
            }
        }
    }

    public class ConfigHandler
    {
        // Private variables
        private static readonly XmlSerializerNamespaces xns = new XmlSerializerNamespaces();
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(UserConfig));
        private static readonly XmlWriterSettings xws = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = true
        };

        private static void CreateConfig()
        {
            /****************************************************
            * Create a new "UserConfig.dat" file in the Data folder.
            ****************************************************/

            // Message box button management
            MessageBoxManager.Yes = "Steam";
            MessageBoxManager.No = "MS Store";
            MessageBoxManager.Register();

            // Grab result of button presses
            MessageBoxResult result = MessageBox.Show("Please select your game distribution (you can change this later).", "First Time Setup", MessageBoxButton.YesNo);

            // Write new config data
            switch (result)
            {
                case MessageBoxResult.Yes:
                    WriteConfigData("Steam", "DEFAULT");
                    break;

                case MessageBoxResult.No:
                    WriteConfigData("Microsoft Store", "DEFAULT");
                    break;

                default:
                    WriteConfigData("Steam", "DEFAULT");
                    break;
            }

            // Unregister MessageBox overrides
            MessageBoxManager.Unregister();
        }

        public static void LoadConfig()
        {
            /**********************************************************
            * Load an existing "UserConfig.dat" file from the Data folder.
            **********************************************************/

            // Load UserConfig.dat
            using (TextReader reader = new StringReader(File.ReadAllText(ConfigFilePath)))
            {
                // Deserialize UserConfig.dat to a usable class
                UserConfig deserialized = (UserConfig)serializer.Deserialize(reader);

                // Assign game distribution (Steam or Microsoft Store)
                App.GameDistro = deserialized.GameDistro;

                // Assign distro-specific data
                App.LaunchCommand = (App.GameDistro == "Steam") ? Launch_HWDE_Steam : Launch_HWDE_MS;
                App.LocalAppDataFolder = (App.GameDistro == "Steam") ? LocalAppData_Steam : LocalAppData_MS;

                // Other data
                int.TryParse(deserialized.TimeoutDelay, out App.TimeoutDelay);
                App.ModManifestFile = $"{App.LocalAppDataFolder}\\ModManifest.txt";
                App.UserModsFolder = (deserialized.ModsDir == "DEFAULT" || !Directory.Exists(deserialized.ModsDir)) ? DefaultUserModsFolder : deserialized.ModsDir;
            }
        }

        public static void WriteConfigData(string distro, string mods_dir)
        {
            /***************************************************
             * Writes new config data to "Data\\UserConfig.dat".
             **************************************************/

            // Create new UserConfig object and remove namespaces
            UserConfig config = new UserConfig();
            if (xns.Count != 1) { xns.Add(string.Empty, string.Empty); }

            // Set data
            config.ModsDir = mods_dir;
            config.GameDistro = distro;
            config.ReleaseVer = ManagerVer;
            config.TimeoutDelay = App.TimeoutDelay.ToString();

            using (XmlWriter writer = XmlWriter.Create(ConfigFilePath, xws))
                serializer.Serialize(writer, config, xns);

            // Reload config data and rescan for installed mods
            LoadConfig();
            App.ModScan();
        }

        public static void Run()
        {
            /*******************************************
            * Handler for loading/creating "UserConfig.dat".
            *******************************************/

            // Check if config file exists
            if (File.Exists(ConfigFilePath))
            {
                // If config file exists, load its data
                Logging.WriteLogEntry("User configuration file found; loading data entries...");
                LoadConfig();
            }
            else
            {
                // If config file doesn't exist, create a new one
                Logging.WriteLogEntry("First time setup detected. Creating default user mods folder and new configuration file...");

                // Create the default user mods folder if it doesn't already exist
                if (!Directory.Exists(DefaultUserModsFolder))
                    Directory.CreateDirectory(DefaultUserModsFolder);

                // Create the new config file
                CreateConfig();
            }
        }
    }

    #region Unused
    /*
    public class AutoUpdater
    {
        private static readonly string newUpdaterPath = $"{Directory.GetCurrentDirectory()}\\AutoUpdater.new";
        private static readonly string oldUpdaterPath = $"{Directory.GetCurrentDirectory()}\\AutoUpdater.exe";
        public static bool UpdateExists = false;

        // Process info
        private static readonly ProcessStartInfo AU_StartInfo_CheckUpdate = new ProcessStartInfo
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            FileName = Path.Combine(Directory.GetCurrentDirectory(), "AutoUpdater.exe"),
            Arguments = $"-c {Constants.ManagerVer}"
        };
        private static readonly ProcessStartInfo AU_StartInfo_InitUpdate = new ProcessStartInfo
        {
            CreateNoWindow = false,
            UseShellExecute = false,
            FileName = Path.Combine(Directory.GetCurrentDirectory(), "AutoUpdater.exe"),
            Arguments = $"-u --auto {Constants.ManagerVer} {Process.GetCurrentProcess().Id}"
        };

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
            if (File.Exists(Constants.ManagerDataFilePath))
            {
                using (TextReader reader = new StringReader(File.ReadAllText(Constants.ManagerDataFilePath)))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Xml.Serializable.ManagerData));
                    Xml.Serializable.ManagerData managerData = (Xml.Serializable.ManagerData)serializer.Deserialize(reader);
                    Constants.ManagerVer = managerData.Version.PatchLevel;
                }
            }
        }

        public static void CheckForUpdates()
        {
            // Check if an update exists on GitHub
            Logging.WriteLogEntry("Launching AutoUpdater...");
            Process AU_Proc = new Process { StartInfo = AU_StartInfo_CheckUpdate };
            AU_Proc.Start();

            // Wait for the Auto-Updater to finish
            Logging.WriteLogEntry("Checking for updates...");
            AU_Proc.WaitForExit();

            if (AU_Proc.ExitCode == 1)
            {
                Logging.WriteLogEntry("Update detected!");
                UpdateExists = true;
            }
            else if (AU_Proc.ExitCode == 0)
            {
                Logging.WriteLogEntry("No update found.");
            }
            else
            {
                Logging.WriteLogEntry("[ERROR] Error in launching AutoUpdater.exe; proceeding with application startup.");
            }
        }

        public static void InitUpdate()
        {
            Process AU_Proc = new Process { StartInfo = AU_StartInfo_InitUpdate };
            AU_Proc.Start();
            Logging.WriteLogEntry("Shutting down for updates. Have a nice day! :)");
            Application.Current.Shutdown();
        }
        
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
        
    }
    */
    
    /*
    public static dynamic GetWindow(Type window_type)
    {
        Window grabbedWindow = null;
        foreach (Window window in Current.Windows)
        {
            if (window.GetType() == window_type)
            {
                grabbedWindow = window;
            }
        }
        return grabbedWindow;
    }

    public static Window GetWindow2(Window window)
    {
        Window temp = null;
        foreach (Window current_window in Current.Windows)
            if (current_window.GetType() == window.GetType())
                temp = current_window;

        return temp;
    }
    */


    /*
    public enum RelocateActions
    {
        Replace,
        Restore
    }

    public static void RelocateDataFolder(RelocateActions action, ModObj mod)
    {
        /*********************************************************
        * Move GameConfig.dat to the folder where the selected
        * mod's manifest file is located and create a directory
        * junction in place of the normal Halo Wars LocalAppData
        * folder.
        *			
        * This makes it to where one doesn't need to move their
        * desired mod's folder upon each launch. It also helps
        * save storage space on one's device that contains the
        * computer's OS (some people have small storage devices
        * for their operating system). Also allows compatibility
        * with Microsoft Store versions since junctioning ModData
        * directly doesn't work (for Steam it will).
        *********************************************************

        // Variables containing the filepaths to GameConfig.dat
        string LocalAppDataDir_DatFile = Path.Combine(LocalAppDataFolder, "GameConfig.dat");
        string ManifestDir_DatFile = Path.Combine(mod.ManifestDirectory, "GameConfig.dat");

        switch (action)
        {
            case RelocateActions.Replace:
                Logging.WriteLogEntry($"Relocating GameConfig.dat to {mod.ManifestDirectory}...");
                try
                {
                    File.Copy(LocalAppDataDir_DatFile, ManifestDir_DatFile, true);                                            // Copy GameConfig.dat from the game's LocalAppData folder to where the mod's .hwmod file is located
                    Directory.Delete(LocalAppDataFolder, true);                                                               // Delete the game's original LocalAppData older
                    JunctionPoint.Create(LocalAppDataFolder, mod.ManifestDirectory, true);                                    // Replace the deleted directory with a junction to where GameConfig.dat is now located
                    File.SetAttributes(ManifestDir_DatFile, File.GetAttributes(ManifestDir_DatFile) | FileAttributes.Hidden); // Hide the file to prevent accidental deletions
                    Logging.WriteLogEntry("Directory junction created between LocalAppData and selected mod folder.");
                }
                catch (Exception relocateException)
                {
                    Logging.WriteExceptionInfo(relocateException);
                }
                break;

            case RelocateActions.Restore:
                Logging.WriteLogEntry("Restoring LocalAppData folder...");
                try
                {
                    File.SetAttributes(ManifestDir_DatFile, File.GetAttributes(ManifestDir_DatFile) & ~FileAttributes.Hidden); // Unhide GameConfig.dat
                    JunctionPoint.Delete(LocalAppDataFolder);                                                                  // Delete the LocalAppData directory junction
                    Directory.CreateDirectory(LocalAppDataFolder);                                                             // Re-create the game's original LocalAppData folder
                    File.Copy(ManifestDir_DatFile, LocalAppDataDir_DatFile, true);                                             // Copy GameConfig.dat back to the game's LocalAppData folder
                    File.Delete(ManifestDir_DatFile);                                                                          // Delete the GameConfig.dat file that was originally copied
                    Logging.WriteLogEntry("GameConfig.dat moved back to original directory; game is now in original layout.");
                }
                catch (Exception relocateException)
                {
                    Logging.WriteExceptionInfo(relocateException);
                }

                break;
        }
    }
    */
    #endregion
}
