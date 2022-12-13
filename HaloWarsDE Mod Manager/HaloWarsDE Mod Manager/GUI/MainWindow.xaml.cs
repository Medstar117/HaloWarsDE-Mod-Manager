using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using Ookii.Dialogs.Wpf;
using HaloWarsDE_Mod_Manager.Core.Serialization;
using System.ComponentModel;
using System.Collections.Generic;
using HaloWarsDE_Mod_Manager.Core.Diagnostics;

namespace HaloWarsDE_Mod_Manager.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public OptionsWindow? OptionsWindow { get; set; }
        public MediaElement? BackgroundAnimation { get { return ((VisualBrush)Resources["Background.Animated"]).Visual as MediaElement; } }

        public List<(Task<bool> task, string? displayText)> PlayGameProgressBarTasks { get; private set; } = new();

        public MainWindow()
        {
            // Pre-Init
            DataContext = App.ModList;
            InitializeComponent();

            // Post-Init
            ModListBox.SelectedIndex = 0;
            #region Set Background Animation Data
            if (BackgroundAnimation!= null)
            {
                BackgroundAnimation.Source      = new Uri(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Layout", "MainWindow", "AnimatedBackground.wmv"));
                BackgroundAnimation.Loaded     += BackgroundAnimation_Loaded;
                BackgroundAnimation.MediaEnded += BackgroundAnimation_MediaEnded;
            }
            #endregion
        }

        #region Background Animation Handling
        private void BackgroundAnimation_Loaded(object sender, RoutedEventArgs e)
            => BackgroundAnimation?.Play();

        private void BackgroundAnimation_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (BackgroundAnimation!= null)
                BackgroundAnimation.Position = TimeSpan.FromSeconds(0);
        }
        #endregion

        #region Events
        /// <summary>
        /// Handles the rendering of graphics and description text whenever a mod is selected.
        /// </summary>
        private void ModListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Mod SelectedMod = (Mod)ModListBox.SelectedItem;

            if (DescriptionTextBox != null)
            {
                if (SelectedMod != null)
                    DescriptionTextBox.Text = SelectedMod.ManifestData != null ? SelectedMod.Description :
                    $"Failed to load mod manifest (.hwmod file) in the following directory (malformed manifest?):\n\n{SelectedMod.ManifestDirectory}";
                else
                    ModListBox.SelectedIndex = 0;
            }

            if (ModBannerArt != null)
                if (SelectedMod != null)
                    ModBannerArt.Source = new BitmapImage(SelectedMod.BannerArt);
        }

        /// <summary>
        /// Allows window to be moved while holding down left mouse button.
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
        #endregion

        #region Button Click Handlers
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void ExitButton_Click(object sender, RoutedEventArgs e)     => App.ExitManager();

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
            { OptionsWindow = new(); OptionsWindow.ShowDialog(); }

        /// <summary>
        /// Opens a File Explorer window for browsing one's mods folder.
        /// One can drag and drop new mods through this window, as well as
        /// execute any normal file operations.
        /// 
        /// Once closed, the mod manager will reload the mods list.
        /// </summary>
        private void ModFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Set up file explorer window
            VistaFileDialog dialog = new VistaOpenFileDialog
            {
                Title = "Mod Folder Browser",
                InitialDirectory = App.UserModsFolder
            };
            dialog.ShowDialog();

            // Refresh the ModList (in case anything new was added)
            Application.Current.Dispatcher.Invoke(() => { App.ScanForMods(); ModListBox.SelectedIndex = 0; });
        }

        /// <summary>
        /// Creates a desktop shortcut for the selected mod.
        /// </summary>
        private void CreateShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            Mod selectedMod = (Mod)ModListBox.SelectedItem;
            string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Launch {selectedMod.Title}.lnk");

            // Make sure the shortcut doesn't already exist
            if (File.Exists(shortcutPath))
            {
                MessageBox.Show($"Shortcut for \"{selectedMod.Title}\" already exists on your desktop!");
                return;
            }

            // Set up IWsh objects
            IWshRuntimeLibrary.WshShell wsh = new();
            IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(shortcutPath);

            // Set shortcut properties
            shortcut.Arguments        = $"--mod_id \"{selectedMod.ModID}\"";
            shortcut.TargetPath       = Path.Combine(Directory.GetCurrentDirectory(), "HaloWarsDE Mod Manager.exe");
            shortcut.Description      = $"Shortcut for the HW:DE mod \"{selectedMod.Title}\".";
            shortcut.WorkingDirectory = Directory.GetCurrentDirectory();
            if (selectedMod.ShortcutIcon != null)
                shortcut.IconLocation = selectedMod.ShortcutIcon.AbsolutePath;
            
            // Save the shortcut and inform the user
            shortcut.Save(); MessageBox.Show($"Desktop shortcut created for \"{selectedMod.Title}\".");
        }

        /// <summary>
        /// Launches the game with the selected mod in the ModListBox.
        /// </summary>
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            // Grab the selected mod from the ModList
            //Task playGameTask = new(() => App.PlayGame((Mod)ModListBox.SelectedItem));
            //playGameTask.Start();
            //PlayGameProgressBarTasks.Add((new Task(() => App.PlayGame((Mod)ModListBox.SelectedItem)), ""));
            Mod selectedMod = (Mod)ModListBox.SelectedItem;
            PlayGameProgressBarTasks.Clear();
            PlayGameProgressBarTasks.Add((new Task<bool>(() => App.CleanLocalAppData()), "Pre-cleaning game LocalAppData folder..."));
            PlayGameProgressBarTasks.Add((new Task<bool>(() => App.LinkModToLocalAppData(selectedMod)), selectedMod.IsVanilla ? "Loading vanilla game..." : $"Linking mod \"{selectedMod.Title}\" information to LocalAppData folder..."));
            PlayGameProgressBarTasks.Add((new Task<bool>(() => App.LaunchGameProcess()), "Launching Halo Wars: Definitive Edition..."));
            PlayGameProgressBarTasks.Add((new Task<bool>(() => App.CatchGameProcess()), "Trying to catch game process..."));
            PlayGameProgressBarTasks.Add((new Task<bool>(() => {
                Dispatcher.Invoke(() =>
                {
                    WindowState = WindowState.Minimized;
                    BackgroundAnimation?.Pause();
                });
                return App.NotifyAndWait(selectedMod);
            }), $"Caught game process! Minimizing mod manager..."));
            InitProgressBar();
        }
        #endregion

        #region Progress Bar Handlers
        public void InitProgressBar()
        {
            PlayButton.IsEnabled = false;
            pBar.Minimum = 0; pBar.Maximum = PlayGameProgressBarTasks.Count;
            pBar.Visibility = Visibility.Visible;
            pBarLabel.Visibility = Visibility.Visible;

            BackgroundWorker worker = new() { WorkerReportsProgress = true };
            worker.DoWork             += IteratePlayGameTasks;
            worker.ProgressChanged    += UpdateProgressBar;
            worker.RunWorkerAsync();
        }

        public void ResetWindowLayout()
        {
            Dispatcher.Invoke(() =>
            {
                PlayButton.IsEnabled = true;
                WindowState = WindowState.Normal;
                BackgroundAnimation?.Play();


                pBarLabel.Visibility = Visibility.Hidden;
                pBar.Visibility = Visibility.Hidden;
                pBar.Value = 0;
            });
        }

        private void UpdateProgressBar(object? sender, ProgressChangedEventArgs e)
        {
            pBar.Value = e.ProgressPercentage;
            if (PlayGameProgressBarTasks[e.ProgressPercentage].displayText != null)
                pBarLabel.Content = PlayGameProgressBarTasks[e.ProgressPercentage].displayText;
        }

        private void IteratePlayGameTasks(object? sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < PlayGameProgressBarTasks.Count; i++)
            {
                (sender as BackgroundWorker)?.ReportProgress(i);
                PlayGameProgressBarTasks[i].task.Start();
                PlayGameProgressBarTasks[i].task.Wait();

                if (!PlayGameProgressBarTasks[i].task.Result)
                {
                    Logger.LogFatal($"Launch failed on task #{i+1}! Aborting game launch...");
                    ResetWindowLayout();
                    break;
                }
            }
        }
        #endregion
    }
}
