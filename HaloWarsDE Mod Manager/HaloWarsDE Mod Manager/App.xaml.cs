using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Reflection;
using HaloWarsDE_Mod_Manager.Core.Diagnostics;
using System.Diagnostics;

using Microsoft.Toolkit.Uwp.Notifications;

using HaloWarsDE_Mod_Manager.Core.Configuration;
using System.Security.Principal;
using System.Collections.ObjectModel;
using HaloWarsDE_Mod_Manager.GUI;
using HaloWarsDE_Mod_Manager.Core.Serialization;
using Monitor.Core.Utilities;
using WPFCustomMessageBox.Net6;

using Medstar.CodeSnippets;
using System.Security.AccessControl;
using static Medstar.CodeSnippets.PermissionsManager;

namespace HaloWarsDE_Mod_Manager
{
    public static class Constants
    {
        // App Info
        public static string  AppPath    => Path.Combine(Directory.GetCurrentDirectory(), $"{AppDomain.CurrentDomain.FriendlyName}.exe");
        public static string  AppName    => Path.GetFileNameWithoutExtension(AppPath);
        public static string? AppVersion => FileVersionInfo.GetVersionInfo(AppPath).FileVersion;

        // LocalAppData Paths
        //private static readonly DirectoryInfo LocalAppData_System = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        private static readonly DirectoryInfo LocalAppData_System = new($"C:\\Users\\{WindowsIdentity.GetCurrent().Name.Split('\\')[1]}\\AppData\\Local");
        public  static readonly DirectoryInfo LocalAppData_Steam  = new(Path.Combine(LocalAppData_System.FullName, "Halo Wars"));
        public  static readonly DirectoryInfo LocalAppData_MS     = new(Path.Combine(LocalAppData_System.FullName, "Packages", "Microsoft.BulldogThreshold_8wekyb3d8bbwe", "LocalState"));

        // Launch Commands
        public static readonly string Launch_HWDE_Steam = "/C start steam://rungameid/459220";
        public static readonly string Launch_HWDE_MS    = "/C start shell:AppsFolder\\Microsoft.BulldogThreshold_8wekyb3d8bbwe!xgameFinal";
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex? _instanceMutex = null;
        private static MainWindow? MainWindowStatic;
        public static Process GameProcess { get; set; } = new();
        public static ObservableCollection<Mod> ModList { get; }  = new();
        public static bool LaunchedFromShortcut { get; private set; } = false;

        // Pathing
        public static readonly string InstallationDirectory = Directory.GetCurrentDirectory();
        public static readonly string DefaultUserModsFolder = Path.Combine(InstallationDirectory, "HWDE Mods");
        public static readonly string UpdaterPath           = Path.Combine(InstallationDirectory, "AutoUpdater.exe");

        #region Dll Imports
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        #endregion

        #region Config Data
        // The main shabang
        public static Config UserConfig                = new();
        
        // Configurable
        public static string GameDistro                { set => UserConfig.GameDistro   = value;      get => UserConfig.GameDistro; }
        public static int TimeoutDelay                 { set => UserConfig.TimeoutDelay = $"{value}"; get { _ = int.TryParse(UserConfig.TimeoutDelay, out int timeoutDelay); return timeoutDelay; } }
        public static string UserModsFolder            { set => UserConfig.ModsDir      = value;      get => (UserConfig.ModsDir == "DEFAULT" || !Directory.Exists(UserConfig.ModsDir)) ? DefaultUserModsFolder : UserConfig.ModsDir; }
        
        // Read-only
        public static string LaunchCommand             => (GameDistro == "Steam") ? Constants.Launch_HWDE_Steam : Constants.Launch_HWDE_MS;
        public static DirectoryInfo LocalAppDataFolder => (GameDistro == "Steam") ? Constants.LocalAppData_Steam : Constants.LocalAppData_MS;
        public static string ModManifestFile           => Path.Combine(LocalAppDataFolder.FullName, "ModManifest.txt");
        #endregion

        #region Overrides
        /// <summary>
        /// Handles what happens when the mod manager starts
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            Logger.LogInfo("Application Started!");

            base.OnStartup(e);

            #region Ensure single instance
            _instanceMutex = new Mutex(true, Constants.AppName, out bool createdNew);

            if (!createdNew)
            {
                // Ask if this new process should start or if the old one should remain
                Logger.LogWarning("Found existing instance of the mod manager. Prompting user for input...");
                MessageBoxResult result = CustomMessageBox.ShowYesNo(messageBoxText: "An existing instance of the Halo Wars: DE Mod Manager is running!",
                                                                  caption: "Manager Already Running", yesButtonText: "Create New Instance", noButtonText: "Exit",
                                                                  MessageBoxImage.Warning);

                // Kill the current process
                if (result != MessageBoxResult.Yes)
                    return;

                // Kill the old process
                Process[] targets = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location));
                foreach (Process target in targets)
                    if (target.ProcessName.Contains(Constants.AppName) && target.Id != Environment.ProcessId) { target.Kill(true); }
            }
            #endregion

            try
            {
                // Launch AutoUpdater
                Logger.LogInfo("Launching Auto Updater...");

                Process AutoUpdater = new();
                AutoUpdater.StartInfo.UseShellExecute = false;
                AutoUpdater.StartInfo.FileName = UpdaterPath;
                AutoUpdater.Start();

                Logger.LogInfo("Waiting for Auto Updater to close...");
                AutoUpdater.WaitForExit();

                // Check if an update was downloaded
                if (AutoUpdater.ExitCode == 1)
                {
                    Logger.LogInfo("Update available. Shutting down...");
                    ExitManager();
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in launching Auto Updater.");
                Logger.LogException(ex);
            }

            Logger.LogInfo("No update available.");

            // Load user config
            if (!ConfigHandler.Run())
            {
                ExitManager();
                return;
            }

            // Semi-check if the game is installed (BUG)
            if (!LocalAppDataFolder.Exists)
            {
                MessageBox.Show("Game LocalAppData directory not found for the current user.\n\n" +
                                "Please ensure that you are logged into the profile that installed the game.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogError("Game LocalAppData directory not found! Exiting application...");
                ExitManager();
                return;
            }

            ScanForMods();

            // Determine if the mod manager was launched headless (no GUI, launches a mod directly)
            if (e.Args.Length > 1)
            {
                for (int index = 1; index < e.Args.Length; index++)
                {
                    if (e.Args[index] == "--mod_id")
                    {
                        foreach (Mod mod in ModList)
                        {
                            if (mod.ModID == e.Args[index + 1])
                            {
                                Logger.LogInfo($"Launching game with {mod.Title}");
                                LaunchedFromShortcut = true;
                                PlayGame(mod);
                                ExitManager();
                                return;
                            }
                        }
                    }
                }
            }

            // Open the main window like normal
            Logger.LogInfo("Displaying main window...");
            MainWindowStatic = new();
            MainWindowStatic.Show();
            MainWindowStatic.Activate();
        }

        /// <summary>
        /// Calls some cleanup code whenever the mod manager closes.
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            _instanceMutex = null; CleanLocalAppData();
            Logger.LogInfo("Application closed. Have a nice day :)");
        }
        #endregion

        #region Utility
        /// <summary>
        /// Scan a user's mods folder for any mod manifest files
        /// and dynamically add each mod's metadata into "ModList".
        /// </summary>
        internal static void ScanForMods()
        {
            Logger.LogInfo($"Loading mods from {UserModsFolder}...");

            // Clear ModList and add the vanilla option
            ModList.Clear(); ModList.Add(new Mod());

            // Add each mod from the user's mods folder to ModList
            foreach (string file in Directory.EnumerateFiles(UserModsFolder, "*.hwmod", SearchOption.AllDirectories))
                ModList.Add(new Mod(file));

            Logger.LogInfo($"{ModList.Count - 1} mod{(ModList.Count - 1 != 1 ? "s" : string.Empty)} detected and loaded.");
        }

        /// <summary>
        /// Clear out any traces of mod data from LocalAppData.
        /// </summary>
        /// <returns></returns>
        public static bool CleanLocalAppData()
        {
            Logger.LogInfo("Cleaning up Halo Wars: DE AppData directory...");

            try
            {
                // Remove ModManifest.txt
                if (File.Exists(ModManifestFile))
                    File.Delete(ModManifestFile);

                // Delete any leftover ModData junctions
                if (JunctionPoint.Exists(Path.Combine(LocalAppDataFolder.FullName, "ModData")))
                    JunctionPoint.Delete(Path.Combine(LocalAppDataFolder.FullName, "ModData"));

                // Double check on directory cleanup
                if (JunctionPoint.Exists(Path.Combine(LocalAppDataFolder.FullName, "ModData")) && File.Exists(ModManifestFile))
                    throw new IOException("Failed to clean Halo Wars: DE's LocalAppData folder!");

                // All OK
                Logger.LogInfo("Successfully cleaned Halo Wars: DE's LocalAppData directory.");
                return true;
            }
            catch (IOException ioEx)
            {
                // Display error message
                MessageBox.Show($"{ioEx.Message}\n\nPlease see the latest log file for details.", "Cleanup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogError(ioEx.Message + $"Please manually delete ModManifest.txt and/or the ModData folder in the following path before running the game without the mod manager: {LocalAppDataFolder.FullName}");
                return false;
            }
        }

        /// <summary>
        /// Adds a directory junction to the game's LocalAppData folder
        /// </summary>
        /// <returns></returns>
        internal static bool LinkModToLocalAppData(Mod selectedMod)
        {
            try
            {
                // Load regular game
                if (selectedMod.IsVanilla)
                {
                    Logger.LogInfo("Loading vanilla game...");
                    return true;
                }

                // Load the mod
                if (selectedMod.IsValid)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    // Mainly for Microsoft Store distros so that data can be accessed
                    AddDirectorySecurity(Path.Combine(selectedMod.ManifestDirectory, "ModData"),
                                       SID.AllApplicationPackages, FileSystemRights.Read, AccessControlType.Allow);
#pragma warning restore CS8604 // Possible null reference argument.

                    // Create junction in LocalAppData folder pointing to mod's ModData folder
                    // Used so that mod files don't actually have to move anywhere
                    JunctionPoint.Create(Path.Combine(LocalAppDataFolder.FullName, "ModData"),
                                         Path.Combine(selectedMod.ManifestDirectory, "ModData"), true);

                    // Create ModManifest.txt pointing to the ModData directory junction
                    using (StreamWriter manifest = File.CreateText(ModManifestFile))
                        manifest.WriteLine(Path.Combine(LocalAppDataFolder.FullName, "ModData"));

                    Logger.LogInfo($"Loading selected mod: {selectedMod.Title}");
                    return true;
                }

                // Something went wrong
                string errorMessage = $"Selected mod \"{selectedMod.Title}\" is invalid! Has either an incorrect or missing ModID in mod manifest.";
                Logger.LogWarning(errorMessage);
                MessageBox.Show(errorMessage + "\nPlease ensure the mod's manifest was created using ModManifestMaker.exe in the manager's installation directory!",
                                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show("Error in launching game. See most recent log for more details.", "Launch Error", MessageBoxButton.OK);
                return false;
            }
        }

        public static bool LaunchGameProcess()
        {
            try
            {
                // Configure game process info
                ProcessStartInfo gameStartInfo = new()
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = "cmd.exe",
                    Arguments = LaunchCommand
                };

                #region Start the game and time how long it takes to launch
                Process Game = new() { StartInfo = gameStartInfo };
                Stopwatch timeout = new();

                Game.Start(); timeout.Start();
                while (!Game.HasExited)
                    if (timeout.ElapsedMilliseconds > TimeoutDelay * 1000)
                        throw new TimeoutException("Game took too long to start!");
                timeout.Stop();
                return true;
                #endregion
            }
            catch (TimeoutException tEx)
            {
                Logger.LogError(tEx.Message);
                MessageBox.Show($"{tEx.Message}\nTry adjusting the TimeoutDelay in Options.",
                                "Timeout Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show("Error in launching game. See most recent log for details.", "Launch Error", MessageBoxButton.OK);
                return false;
            }
        }

        public static bool CatchGameProcess()
        {
            try
            {
                GameProcess = Process.GetProcessById(GetGameProcessId());
                return GameProcess != null;
            }
            catch (TimeoutException tEx)
            {
                Logger.LogError(tEx.Message);
                MessageBox.Show($"{tEx.Message}\nTry adjusting the TimeoutDelay in Options.",
                                "Timeout Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show("Error in launching game. See most recent log for details.", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static int GetGameProcessId()
        {
            int GameProcessId = -1;
            Stopwatch sw = new(); sw.Start();
            while (GameProcessId == -1)
            {
                if (sw.ElapsedMilliseconds > TimeoutDelay * 1000)
                    throw new TimeoutException("Could not get handle to game process!");

                if (GameDistro == "Microsoft Store")
                    GameProcessId = UWPProcFetcher.GetProcessID("xgameFinal");

                Process[] ProcSnapshot = Process.GetProcessesByName("xgameFinal");
                if (ProcSnapshot.Length > 0)
                    foreach (Process proc in ProcSnapshot)
                        if (proc.ProcessName.Contains("xgameFinal"))
                            GameProcessId = proc.Id;
            }
            sw.Stop();
            return GameProcessId;
        }

        internal static bool NotifyAndWait(Mod selectedMod)
        {
            new ToastContentBuilder().AddText("Game Process Caught!")
                .AddText($"HW:DE Mod Manager {(LaunchedFromShortcut ? "running in background." : "minimized to taskbar.")}")
                .Show(toast => { toast.ExpirationTime = DateTime.Now.AddSeconds(5); });
            GameProcess.Exited += (sender, e) => OnGameClosed(sender, e, selectedMod);
            GameProcess.WaitForExitAsync();
            return true;
        }
        #endregion

        /// <summary>
        /// Wrapper for calling the "shutdown" function.
        /// </summary>
        public static void ExitManager()
            { try { Current.Shutdown(); }
              catch (Exception ex) { Logger.LogException(ex); } }

        /// <summary>
        /// Launch the game and load the selected mod.
        /// </summary>
        /// <param name="mod"></param>
        public static void PlayGame(Mod mod)
        {
            CleanLocalAppData();
            LinkModToLocalAppData(mod);
            LaunchGameProcess();
            CatchGameProcess();
            NotifyAndWait(mod);
        }

        /// <summary>
        /// Does some cleanup work whenever the game is closed.
        /// </summary>
        private static void OnGameClosed(object? sender, EventArgs e, Mod mod)
        {
            CleanLocalAppData();
            if (!mod.IsVanilla && mod.ManifestDirectory != null)
                RemoveDirectorySecurity(Path.Combine(mod.ManifestDirectory, "ModData"), SID.AllApplicationPackages,
                                        FileSystemRights.Read, AccessControlType.Allow);

            if (!LaunchedFromShortcut)
            {
                Current.Dispatcher.Invoke(() => MainWindowStatic?.ResetWindowLayout());
                SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
            }
        }
    }
}
