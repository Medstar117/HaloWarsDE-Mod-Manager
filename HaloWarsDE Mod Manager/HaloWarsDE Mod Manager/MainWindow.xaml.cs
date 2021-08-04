// Built-ins
using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using Monitor.Core.Utilities;
using System.Collections.ObjectModel;

// Packages
using Ookii.Dialogs.Wpf;

// Personal
using Globals;
using DataSerialization;
using HaloWarsDE_Mod_Manager.Modules;
using static UWP.ProcFetcher.ProcFetcher;
using static Globals.Main;

namespace HaloWarsDE_Mod_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// Remember to update the Globals.ManagerVer variable when making a new release!
    /// </summary>
    public partial class MainWindow : Window
    {
        // ==================== Variable Declarations ====================

        // ---------- General Variables and Classes ----------
        public static ObservableCollection<Mod> ModList { get; } = new ObservableCollection<Mod>();
        public static string LaunchCommand = null;


        // ==================== Internal Functions ====================

        public MainWindow()
        {
            /********************************
			* The main window initialization.
			********************************/
            try
            {
                Logging.WriteLogEntry("Application started!");
                DataContext = this;

                // Create a shortcut to the manager on the user's desktop
                string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "HWDE Mod Manager.lnk");
                if (!File.Exists(shortcutPath))
                {
                    // Set up IWsh objects
                    IWshRuntimeLibrary.WshShell wsh = new IWshRuntimeLibrary.WshShell();
                    IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(shortcutPath);

                    // Set shortcut properties and save it
                    shortcut.TargetPath = Path.Combine(Directory.GetCurrentDirectory(), "HaloWarsDE Mod Manager.exe");
                    shortcut.Description = $"Shortcut to the Halo Wars:DE mod manager.";
                    shortcut.WorkingDirectory = Directory.GetCurrentDirectory();
                    shortcut.Save();
                }

                // Check if manager instance is already running
                if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
                {
                    _ = MessageBox.Show("An existing instance of the Halo Wars: DE Mod Manager is running!", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
                else
                {
                    // Check if pending changes exist
                    OmniUpdater.PreCheck();

                    // Check for Updates
                    OmniUpdater.CheckForUpdates();

                    // Run the application config handler
                    bool handler_result = ConfigHandler.Run();

                    // Check if the user has played a skirmish match or not (if GameConfig.dat exists)
                    if (!File.Exists($"{App.Constants.LocalAppData_Selected}\\GameConfig.dat"))
                    {
                        _ = MessageBox.Show("Game configuration file not found.\n\nPlease launch the game at least once to re-generate it.", "Initialization Error", MessageBoxButton.OK);
                        Logging.WriteLogEntry("[ERROR] GameConfig.dat file not found! Exiting application...");
                        Application.Current.Shutdown();
                    }

                    // If the handler result is true, then that means the manager has just booted up
                    // It would be false if this were a first-time setup
                    if (handler_result)
                        App.RelocateDataFolder(App.RelocateActions.Replace);

                    // Scan for mods
                    ModScan();

                    // Determine if the manager is being launched directly or from a shorcut
                    string[] args = Environment.GetCommandLineArgs();
                    if (args.Length > 1)
                    {
                        for (int index = 1; index < args.Length; index++)
                            if (args[index] == "--mod_id")
                                foreach (Mod mod in ModList)
                                    if (mod.ModID == args[index + 1])
                                    {
                                        LaunchedFromShortcut = true;
                                        PlayGame(mod);
                                        ExitManager();
                                    }
                    }
                    else
                    {
                        InitializeComponent();
                        ModListBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteExceptionInfo(e);
                Logging.WriteLogEntry($"Variable dump:\n\nSelected LocalAppData Directory: {App.Constants.LocalAppData_Selected}");
            }
        }

        // ---------- General-Purpose ----------

        private void ExitManager()
        {
            /**************************************************************** 
            * Exits the manager and cleans up the Halo Wars AppData directory
            * of any leftover traces of a mod. This is to ensure that a mod
            * is never accidentally loaded when one is not using the manager.
            ****************************************************************/

            Logging.WriteLogEntry("Cleaning up Halo Wars AppData directory...");

            // Revert junction for the Halo Wars Local AppData directory.
            App.RelocateDataFolder(App.RelocateActions.Restore);

            // Remove ModManifest.txt.
            if (File.Exists(App.Constants.ModManifestFile))
                File.Delete(App.Constants.ModManifestFile);

            // Just to double check that everything has been cleaned up.
            if (!JunctionPoint.Exists(App.Constants.LocalAppData_Selected) && !File.Exists(App.Constants.ModManifestFile))
                Logging.WriteLogEntry("Halo Wars LocalAppData directory cleaned. All traces of any previously loaded mods have been purged.");
            else
            {
                string error_msg = "Failure in restoring the Halo Wars LocalAppData directory.\n\nPlease see the latest log file to navigate to the directory in question.";
                string error_log = $"Failure in restoring the Halo Wars LocalAppData directory: '{App.Constants.LocalAppData_Selected}'. Please manually move GameData.dat from your mods folder before launching the game without the manager.";

                _ = MessageBox.Show(error_msg, "Permissions Error on Cleanup", MessageBoxButton.OK);
                Logging.WriteLogEntry("[ERROR] " + error_log);
            }

            // Close the program.
            Logging.WriteLogEntry("Application closed. Have a nice day; and thank you for using my mod manager :)");
            Application.Current.Shutdown();
        }

        private void PlayGame(Mod modObject)
        {
            /*******************************************
			* Launch the game and load the selected mod.
			*******************************************/
            bool Start_OK = true;

            // Disable play button to prevent multiple launches
            if (!LaunchedFromShortcut)
                Dispatcher.Invoke(() => { PlayButton.IsEnabled = false; });
                        
            try
            {
                ProgressBarManager.InitProgressBar(0, 5);

                // If an existing ModManifest.txt file exists, delete it.
                if (File.Exists(App.Constants.ModManifestFile))
                {
                    ProgressBarManager.SetProgressBarData(1, true, "Deleting existing ModManifest.txt");
                    File.Delete(App.Constants.ModManifestFile);
                }

                // Check if a mod was selected; if so, make a new ModManifest.txt file
                // that points to the selected mod's base directory.
                if (modObject.ModID != null && modObject.IsValid)
                {
                    ProgressBarManager.SetProgressBarData(1, true, $"Creating ModManifest.txt for {modObject.Title}...");
                    using (StreamWriter manifest = File.CreateText(App.Constants.ModManifestFile))
                        manifest.WriteLine($"{App.Constants.LocalAppData_Selected}{modObject.ManifestDirectory.Replace(UserModsFolder, string.Empty)}\\ModData");
                        //manifest.WriteLine(Path.Combine(UserModsFolder, modObject.Base_Dir));

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
                    _ = MessageBox.Show($"Selected mod \"{modObject.Title}\" is not valid due to an incorrect or lack of ModManifest element \"ModID\".\nThe mod author may have forgotten to update their mod's manifest file!", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Start_OK = false;
                }

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
                    File.SetAttributes(App.Constants.GameConfigFile_UMF, File.GetAttributes(App.Constants.GameConfigFile_UMF) & ~FileAttributes.Hidden);
                    _ = GameCMD.Start();
                    sw.Start();
                    while (!GameCMD.HasExited)
                        if (sw.ElapsedMilliseconds > TimeoutDelay * 1000) throw new TimeoutException();
                    sw.Stop();

                    // If all goes well, fetch the game's PID
                    ProgressBarManager.SetProgressBarData(1, true, "Fetching PID for Halo Wars: Definitive Edition...");
                    Process GameProcess = Process.GetProcessById(GetGameProcessID(ref sw));
                    ProgressBarManager.SetProgressBarData(1, true, $"Caught process for Halo Wars: Definitive Edition! PID: {GameProcess.Id}");

                    GameProcess.WaitForExit();
                    File.SetAttributes(App.Constants.GameConfigFile_UMF, File.GetAttributes(App.Constants.GameConfigFile_UMF) | FileAttributes.Hidden);
                }

                // Clean up and reset
                ProgressBarManager.ResetProgressBar();
                if (!LaunchedFromShortcut)
                    Dispatcher.Invoke(() => { PlayButton.IsEnabled = true; });
            }
            catch (TimeoutException)
            {
                Logging.WriteLogEntry("[ERROR] Process took too long to start!");
                _ = MessageBox.Show("[ERROR] Process took too long to start!", "Timeout Error", MessageBoxButton.OK, MessageBoxImage.Warning);

                File.SetAttributes(App.Constants.GameConfigFile_UMF, File.GetAttributes(App.Constants.GameConfigFile_UMF) | FileAttributes.Hidden);
                ProgressBarManager.ResetProgressBar();
                if (!LaunchedFromShortcut)
                    Dispatcher.Invoke(() => { PlayButton.IsEnabled = true; });
            }
            catch (Exception ex)
            {
                Logging.WriteLogEntry($"[ERROR] Unknown exception occurred! Details are as follows:\n\nStackTrace:\n{ex.StackTrace}\n\nMessage:\n{ex.Message}");
                _ = MessageBox.Show("Error in launching game. See most recent log for more details.", "Launch Error", MessageBoxButton.OK);
            }
        }

        private int GetGameProcessID(ref Stopwatch sw)
        {
            int GameProcessID = -1;

            sw.Restart();
            switch (GameDistro)
            {
                case "Steam":
                    while (GameProcessID == -1)
                    {
                        if (sw.ElapsedMilliseconds > TimeoutDelay * 1000) throw new TimeoutException();
                        Process[] currentProcesses = Process.GetProcessesByName("xgameFinal");
                        if (currentProcesses.Length > 0)
                            foreach (Process proc in currentProcesses)
                                if (proc.ProcessName.Contains("xgameFinal"))
                                    GameProcessID = proc.Id;
                    }
                    break;

                case "Microsoft Store":
                    while (GameProcessID == -1)
                    {
                        if (sw.ElapsedMilliseconds > TimeoutDelay * 1000) throw new TimeoutException();
                        GameProcessID = GetProcessID("xgameFinal");
                    }
                    break;
            }
            sw.Stop();
            return GameProcessID;
        }


        // ==================== Event-Driven Functions ====================

        // ---------- General GUI functionality ----------
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            /***************************************************************
            * Allows the GUI to be dragged/moved with the left mouse button.
			***************************************************************/

            DragMove();
        }

        private void ModListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /****************************************************************
			* Dynamically sets the description box text and banner art when
			* selecting a mod.
			****************************************************************/

            // Get the dynamically-selected mod object
            Mod SelectedMod = ModListBox.SelectedItem as Mod;

            // Set the description box text
            if (DescriptionTextBox != null)
            {
                DescriptionTextBox.Clear();

                if (SelectedMod != null)
                    DescriptionTextBox.Text = SelectedMod.Description;
                else
                    ModListBox.SelectedIndex = 0;
            }

            // Set the banner art
            if (ModBannerArt != null)
                if (SelectedMod != null)
                    ModBannerArt.Source = new BitmapImage(SelectedMod.BannerArt);
        }


        // ---------- Buttons ----------
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            /************************************************
			* Minimizes the window...pretty self-explanatory.
			************************************************/

            WindowState = WindowState.Minimized;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            /**************************************************************** 
            * Exits the manager and cleans up the Halo Wars AppData directory
            * of any leftover traces of a mod. This is to ensure that a mod
            * is never accidentally loaded when one is not using the manager.
            * 
            * This is the event-driven function hooked to the exit button.
            ****************************************************************/

            ExitManager();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            /*******************************************
			* Passes the selected mod object to the
			* PlayGame function.
			*******************************************/

            // Grab the selected mod container.
            Mod SelectedMod = ModListBox.SelectedItem as Mod;

            // Play the game
            Task callPlayFunc = new Task(() =>
            {
                PlayGame(SelectedMod);
            });

            callPlayFunc.Start();
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            /*****************************************************
			* Displays the Settings window and locks the main GUI.
			*****************************************************/

            OptionsWindow options = new OptionsWindow();
            _ = options.ShowDialog();
        }

        private void CreateShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            // Local variables
            Mod SelectedMod = ModListBox.SelectedItem as Mod;
            string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"HWDE - {SelectedMod.Title}.lnk");

            if (!File.Exists(shortcutPath))
            {
                // Set up IWsh objects
                IWshRuntimeLibrary.WshShell wsh = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(shortcutPath);

                // Set shortcut properties and save it
                shortcut.Arguments = $"--mod_id \"{SelectedMod.ModID}\"";
                shortcut.TargetPath = Path.Combine(Directory.GetCurrentDirectory(), "HaloWarsDE Mod Manager.exe");
                shortcut.Description = $"Shortcut for the HWDE mod: {SelectedMod.Title}.";
                shortcut.WorkingDirectory = Directory.GetCurrentDirectory();
                if (SelectedMod.ShortcutIcon != null)
                    shortcut.IconLocation = SelectedMod.ShortcutIcon;
                shortcut.Save();

                _ = MessageBox.Show($"Created shortcut on your desktop for \"{SelectedMod.Title}\".");
            }
            else
            {
                _ = MessageBox.Show($"Shortcut for \"{SelectedMod.Title}\" already exists on your desktop!");
            }
        }

        private void ModFolderButton_Click(object sender, RoutedEventArgs e)
        {
            /***********************************************************
             * Open a File Explorer window for browsing the mods folder.
             * One can drag and drop mod files in here as well as other
             * file operations.
             * 
             * After the File Explorer is closed, the manager will scan
             * for mods again and refresh the ModListBox.
             **********************************************************/

            // Set up the File Explorer window
            VistaFileDialog dialog = new VistaOpenFileDialog
            {
                InitialDirectory = UserModsFolder,
                Title = "Mod Folder Browser"
            };

            // Show the File Explorer window and protect GameConfig.dat from being deleted accidentally
            using (FileStream protectGameConfig = new FileStream(App.Constants.GameConfigFile_UMF, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                _ = dialog.ShowDialog();
                protectGameConfig.Close();
            }

            // Refresh the ModListBox
            Application.Current.Dispatcher.Invoke(() =>
            {
                ModScan();
                ModListBox.SelectedIndex = 0;
            });
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            /*******************************************************************
             * Manually refreshes the listbox containing all of the usable mods.
             ******************************************************************/

            Application.Current.Dispatcher.Invoke(() =>
            {
                ModScan();
                ModListBox.SelectedIndex = 0;
            });
        }
    }
}
