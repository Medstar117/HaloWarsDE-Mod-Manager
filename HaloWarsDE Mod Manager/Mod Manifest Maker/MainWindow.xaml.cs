using HaloWarsDE_Mod_Manager.Core.Serialization;
using Ookii.Dialogs.Wpf;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Mod_Manifest_Maker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ModManifest? ManifestData { get; set; } = new();

        // YOU HAVE TO SET RootDirectory!
        private string RootDirectory { get; set; } = string.Empty;
        private DirectoryInfo ModDataDirectory => new(Path.Combine(RootDirectory, "ModData"));
        private FileInfo ManifestFile          => new($"{RootDirectory}\\{ReplaceInvalidChars($"{ModName_TextBox.Text} v{ModVersion_TextBox.Text}")}.hwmod");
        
        private bool ModFolderValid => ModFolder_TextBox.Text != string.Empty  && Directory.Exists(ModFolder_TextBox.Text) && ModFolder_TextBox.Text.Contains("ModData");
        private bool EnableSaving   => (ModName_TextBox.Text  != string.Empty) && (ModAuthor_TextBox.Text != string.Empty) && ModFolderValid && (ModVersion_TextBox.Text != string.Empty);
    
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Opens an existing mod manifest (*.hwmod).
        /// </summary>
        private void OpenManifestClick(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog modManifest = new()
            {
                Filter      = "Halo Wars: DE mods (*.hwmod)|*.hwmod",
                Title       = "Select a Halo Wars: DE mod manifest to edit",
                FileName    = Directory.GetCurrentDirectory(),
                Multiselect = false
            };

            if (modManifest.ShowDialog() == true)
            {
                RootDirectory = Path.GetDirectoryName(modManifest.FileName) ?? string.Empty;
                ManifestData  = ManifestSerializer.DeserializeManifest(modManifest.FileName);

                if (ManifestData != null )
                {
                    switch (ManifestData.ManifestVersion)
                    {
                        case "1":
                            // Check if ModData exists next to the manifest file
                            if (!ModDataDirectory.Exists)
                            {
                                ModDataDirectory.Create();
                                MessageBox.Show("A new \"ModData\" folder was created next to the selected manifest.\n\nPlease place your mod's files in this folder.",
                                    "Folder Created", MessageBoxButton.OK, MessageBoxImage.Information);
                            }

                            // Required Data
                            ModName_TextBox.Text    = ManifestData.Required.Title;
                            ModAuthor_TextBox.Text  = ManifestData.Required.Author;
                            ModVersion_TextBox.Text = ManifestData.Required.Version;
                            ModFolder_TextBox.Text  = ModDataDirectory.FullName;

                            // Optional Data
                            Banner_TextBox.Text = ManifestData.Optional.Banner.RelativePath;
                            Icon_TextBox.Text   = ManifestData.Optional.Icon.RelativePath;
                            Desc_TextBox.Text   = ManifestData.Optional.Desc.Text;
                            break;
                    }
                }
            }
            RequiredData_TextChanged(null, null);
        }

        /// <summary>
        /// Saves the new manifest to a .hwmod file.
        /// </summary>
        private void SaveManifestClick(object sender, RoutedEventArgs e)
        {
            // Null check
            if (ManifestData == null)
                return;

            // Delete existing file
            if (ManifestFile.Exists)
                ManifestFile.Delete();

            // Serialize the data
            ManifestSerializer.SerializeManifest(ManifestData, ManifestFile.FullName);

            if (ManifestFile.Exists)
            {
                MessageBox.Show($"Saved manifest at {ManifestFile}.", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            MessageBox.Show($"Could not save mod manifest!.", "Save Unsuccessful", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Whenever a textbox's value changes, this checks if one is able to save the manifest or not.
        /// </summary>
        private void RequiredData_TextChanged(object? sender, TextChangedEventArgs? e)
        {
            SaveButton.IsEnabled     = EnableSaving;
            Banner_TextBox.IsEnabled = Banner_Button.IsEnabled = Icon_Button.IsEnabled = Icon_TextBox.IsEnabled = ModFolderValid;
            Banner_Button.ToolTip    = Icon_Button.ToolTip     = ModFolderValid ? null : "Your mod's ModData folder must be a valid directory on your filesystem!";
        }

        private void FileBrowser_Click(object sender, RoutedEventArgs e)
        {
            string resourceName = ((Button)sender).Name.Replace("_Button", string.Empty);
            
            switch(resourceName)
            {
                case "Banner":
                    FileBrowserPrompt($"Select a custom {resourceName.ToLower()} for your mod", "PNG Files (*.png)|*.png|Bitmap Files (*.bmp)|*.bmp", ref Banner_TextBox);
                    break;

                case "Icon":
                    FileBrowserPrompt($"Select a custom {resourceName.ToLower()} for your mod", "Icon Files (*.ico)|*.ico", ref Icon_TextBox);
                    break;

                case "ModFolder":
                    VistaFolderBrowserDialog modDataSelector = new()
                    {
                        Description = "Select your mod's ModData folder",
                        UseDescriptionForTitle = true,
                        SelectedPath = Directory.GetCurrentDirectory()
                    };

                    if (modDataSelector.ShowDialog() == true)
                    {
                        DirectoryInfo selectedFolder = new(modDataSelector.SelectedPath);
                        if (selectedFolder.Name != "ModData")
                        {
                            MessageBox.Show("The selected folder must be named \"ModData\".", "Incorrect Folder Name", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        ModFolder_TextBox.Text = selectedFolder.FullName;
                        RootDirectory          = selectedFolder.Parent?.FullName ?? string.Empty;

                        // Clear optional pathing data since paths will no longer be relative
                        Banner_TextBox.Text    = Icon_TextBox.Text = string.Empty;
                    }
                    RequiredData_TextChanged(null, null);
                    break;
            }
        }

        #region Utility
        private void FileBrowserPrompt(string title, string filter, ref TextBox textBox)
        {
            VistaOpenFileDialog dialog = new()
            {
                Title       = title,
                Multiselect = false,
                FileName    = Directory.GetCurrentDirectory(),
                Filter      = filter
            };

            if (dialog.ShowDialog() == true)
            {
                if (dialog.FileName.Contains(RootDirectory))
                {
                    textBox.Text = dialog.FileName.Replace(RootDirectory + "\\", string.Empty);
                    return;
                }

                MessageBox.Show($"File must be in a relative directory to the following path:\n\n{RootDirectory}",
                    "Relative Path Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static string ReplaceInvalidChars(string filePath)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                filePath = filePath.Replace(c, '_');
            return filePath;
        }
        #endregion
    }
}
