// Built-ins
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

// Packages
using Ookii.Dialogs.Wpf;

// Personal
//using Globals;
using HaloWarsDE_Mod_Manager.Shared.DataSerialization.Mods;

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
        //public static ObservableCollection<Mod> ModList { get; } = new ObservableCollection<Mod>();
        //public static string LaunchCommand = null;


        // ==================== Internal Functions ====================

        public MainWindow()
        {
            /******************************************
			* Initialization for the MainWindow object.
			******************************************/

            DataContext = App.ModList;
            InitializeComponent();
            ModListBox.SelectedIndex = 0;
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
            ModObj SelectedMod = ModListBox.SelectedItem as ModObj;

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

            App.ExitManager();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            /*******************************************
			* Passes the selected mod object to the
			* PlayGame function.
			*******************************************/

            // Grab the selected mod container.
            ModObj SelectedMod = ModListBox.SelectedItem as ModObj;

            // Play the game
            Task callPlayFunc = new Task(() =>
            {
                App.PlayGame(SelectedMod);
            });

            callPlayFunc.Start();
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            /*****************************************************
			* Displays the Settings window and locks the main GUI.
			*****************************************************/

            OptionsWindow options = new OptionsWindow();
            options.ShowDialog();
        }

        private void CreateShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            // Local variables
            ModObj SelectedMod = ModListBox.SelectedItem as ModObj;
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

                MessageBox.Show($"Created shortcut on your desktop for \"{SelectedMod.Title}\".");
            }
            else
            {
                MessageBox.Show($"Shortcut for \"{SelectedMod.Title}\" already exists on your desktop!");
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
                InitialDirectory = App.UserModsFolder,
                Title = "Mod Folder Browser"
            };

            dialog.ShowDialog();
            // Show the File Explorer window and protect GameConfig.dat from being deleted accidentally
            //using (FileStream protectGameConfig = new FileStream(App.GameConfigFile_UMF, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            //{
            //    dialog.ShowDialog();
            //    protectGameConfig.Close();
            //}

            // Refresh the ModListBox
            Application.Current.Dispatcher.Invoke(() =>
            {
                App.ModScan();
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
                App.ModScan();
                ModListBox.SelectedIndex = 0;
            });
        }
    }
}
