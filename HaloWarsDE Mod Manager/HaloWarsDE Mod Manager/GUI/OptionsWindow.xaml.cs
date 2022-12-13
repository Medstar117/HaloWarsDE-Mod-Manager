using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Navigation;

using Ookii.Dialogs.Wpf;
using HaloWarsDE_Mod_Manager.Core.Configuration;

namespace HaloWarsDE_Mod_Manager.GUI
{
    /// <summary>
    /// Interaction logic for OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        public MediaElement? BackgroundAnimation { get { return ((VisualBrush)Resources["Background.Animated"]).Visual as MediaElement; } }

        #region Used to check if data was modified
        private bool DistroModified { get; set; } = false;
        private bool ModFolderModified { get; set; } = false;
        private bool TimeoutDelayModified { get; set; } = false;
        private bool IsDirty => DistroModified || TimeoutDelayModified || ModFolderModified;
        #endregion

        public OptionsWindow()
        {
            // Pre-Init
            DataContext = this;
            InitializeComponent();

            // Post-Init
            #region Set Background Animation Data
            if (BackgroundAnimation != null)
            {
                BackgroundAnimation.Source = new Uri(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Layout", "OptionsWindow", "AnimatedBackground.wmv"));
                BackgroundAnimation.Loaded += BackgroundAnimation_Loaded;
                BackgroundAnimation.MediaEnded += BackgroundAnimation_MediaEnded;
            }
            #endregion

            // Have to set this here due to WPF bug in XAML
            foreach (Button button in ButtonList.Children)
                button.Click += ChangeTab;

            #region Display currently-set global data
            DistroComboBox.SelectedValue = App.GameDistro;
            FilePathTextBox.Text         = App.UserModsFolder;
            TimeoutDelayIntUpDown.Value  = App.TimeoutDelay;
            VersionLabel.Content         = $"Version: {Constants.AppVersion}";
            #endregion
        }

        #region Background Animation Handling
        private void BackgroundAnimation_Loaded(object sender, RoutedEventArgs e)
            => BackgroundAnimation?.Play();

        private void BackgroundAnimation_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (BackgroundAnimation != null)
                BackgroundAnimation.Position = TimeSpan.FromSeconds(0);
        }
        #endregion

        #region Events
        private void ChangeTab(object sender, RoutedEventArgs e)
        {
            for (int index = 0; index < TabbedDisplay.Items.Count; index++)
                if (((TabItem)TabbedDisplay.Items[index]).Header.ToString() == ((Button)sender).Content.ToString())
                    TabbedDisplay.SelectedIndex = index;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void DistroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DirectoryInfo dirCheck = ($"{DistroComboBox.SelectedValue}" == "Steam") ? Constants.LocalAppData_Steam : Constants.LocalAppData_MS;
            
            // Check if the selected distro is installed
            if (dirCheck.Exists)
            {
                DistroModified = $"{DistroComboBox.SelectedValue}" != App.GameDistro;
                return;
            }

            // The new game distro is not detected. Set the value back to what it was
            MessageBox.Show($"Game distribution \"{DistroComboBox.SelectedValue}\" not detected!\nIf this is in error, make sure you've launched the game on its own at least once.",
                                    "Game Distro Not Detected", MessageBoxButton.OK, MessageBoxImage.Error);
            DistroComboBox.SelectedValue = DistroComboBox.SelectedValue.ToString() == "Steam" ? "Microsoft Store" : "Steam";
        }

        private void TimeoutDelayIntUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
            => TimeoutDelayModified = (int)TimeoutDelayIntUpDown.Value != App.TimeoutDelay;

        private void OpenModFolderBrowser(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog modFolder = new()
            {
                SelectedPath = App.UserModsFolder,
                Description = "Please select your mods folder",
                UseDescriptionForTitle = true,
            };

            if (modFolder.ShowDialog() == true)
                FilePathTextBox.Text = modFolder.SelectedPath;

            ModFolderModified = FilePathTextBox.Text != App.UserModsFolder;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsDirty)
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                // Set new data and update the config folder
                App.GameDistro     = DistroComboBox.SelectedValue.ToString();
                App.TimeoutDelay   = (int)TimeoutDelayIntUpDown.Value;
                App.UserModsFolder = FilePathTextBox.Text;
                ConfigHandler.WriteConfigData();
#pragma warning restore CS8601 // Possible null reference assignment.
            }
        }
        #endregion
    }
}
