// Built-Ins
using System;
using System.IO;
using System.Windows;
using Ookii.Dialogs.Wpf;
using System.Windows.Controls;

// Personal
using DataSerialization.Serializable;
using static DataSerialization.Serializable.ManifestSerializer;


namespace ModManifestMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Private variables
        private string ModID = null;
        private string HWMOD_Location = null;


        // Main window initialization
        public MainWindow()
        {
            InitializeComponent();
        }


        // Private functions
        private void FileBrowserPrompt(string title, string filter, ref TextBox textbox)
        {
            VistaOpenFileDialog dialog = new VistaOpenFileDialog
            {
                Title = title,
                Multiselect = false,
                FileName = Directory.GetCurrentDirectory(),
                Filter = filter
            };

            if (dialog.ShowDialog() == true)
            {
                if (dialog.FileName.Contains(HWMOD_Location))
                {
                    textbox.Text = dialog.FileName.Replace(HWMOD_Location + "\\", string.Empty);
                }
                else
                    _ = MessageBox.Show($"File must be in a relative directory to the following path:\n\n{HWMOD_Location}", "Relative Path Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        // GUI-bound functions
        private void FileBrowser_Click(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "ModFolder_Button":
                    VistaFolderBrowserDialog baseDir_Dialog = new VistaFolderBrowserDialog
                    {
                        Description = "Select your mod's base file directory",
                        UseDescriptionForTitle = true,
                        SelectedPath = Directory.GetCurrentDirectory()
                    };

                    if (baseDir_Dialog.ShowDialog() == true)
                    {
                        if (Directory.Exists($"{baseDir_Dialog.SelectedPath}\\ModData"))
                        {
                            ModFolder_TextBox.Text = HWMOD_Location = baseDir_Dialog.SelectedPath;
                        }
                        else
                        {
                            MessageBoxResult response = MessageBox.Show("No 'ModData' folder found in selected folder. Are you sure you've selected the right folder for your mod?", "Mod Data Not Found", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (response == MessageBoxResult.Yes)
                            {
                                ModFolder_TextBox.Text = HWMOD_Location = baseDir_Dialog.SelectedPath;
                            }
                        }
                        Banner_TextBox.Text = Icon_TextBox.Text = "";
                    }
                    break;

                case "Banner_Button":
                    FileBrowserPrompt("Select a custom banner for your mod",
                                      "PNG Files (*.png)|*.png|Bitmap Files (*.bmp)|*.bmp",
                                      ref Banner_TextBox);
                    break;

                case "Icon_Button":
                    FileBrowserPrompt("Select a custom icon for your mod's shortcut",
                                      "Icon Files (*.ico)|*.ico",
                                      ref Icon_TextBox);
                    break;
            }
        }

        private void RequiredData_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveButton.IsEnabled = (ModName_TextBox.Text != "") && (ModAuthor_TextBox.Text != "") && (ModFolder_TextBox.Text != "") && Directory.Exists(ModFolder_TextBox.Text + "\\");
            Banner_TextBox.IsEnabled = Banner_Button.IsEnabled = Icon_Button.IsEnabled = Icon_TextBox.IsEnabled = (ModFolder_TextBox.Text != "" && Directory.Exists(ModFolder_TextBox.Text + "\\"));
            Banner_Button.ToolTip = Icon_Button.ToolTip = (ModFolder_TextBox.Text != "" && Directory.Exists(ModFolder_TextBox.Text + "\\")) ? null : "Your mod's base directory must be a valid directory on your filesystem!";
        }

        private void OpenManifest_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog mod_manifest = new VistaOpenFileDialog
            {
                Filter = "Halo Wars: DE mods (*.hwmod)|*.hwmod",
                Title = "Select a Halo Wars: DE mod manifest to edit",
                FileName = Directory.GetCurrentDirectory(),
                Multiselect = false
            };

            if (mod_manifest.ShowDialog() == true)
            {
                // Deserialize the manifest file into a usable object
                ModManifest deserialized = DeserializeManifest(mod_manifest.FileName);

                // Yes, yes, this is a one case switch; this is mainly for future-proofing in case changes are made down the road
                switch (deserialized.ManifestVersion)
                {
                    case "1":
                        // Required data fields
                        ModID = deserialized.ModID;
                        ModName_TextBox.Text = deserialized.Required.Title;
                        ModAuthor_TextBox.Text = deserialized.Required.Author;
                        ModFolder_TextBox.Text = new DirectoryInfo(mod_manifest.FileName + "\\").Parent.FullName;

                        // Optional data fields
                        Banner_TextBox.Text = deserialized.Optional.Banner.RelativePath;
                        Icon_TextBox.Text = deserialized.Optional.Icon.RelativePath;
                        Desc_TextBox.Text = deserialized.Optional.Desc.Text;
                        break;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string hwmod_file = $"{ModFolder_TextBox.Text}\\.hwmod";
                if (File.Exists(hwmod_file))
                {
                    MessageBoxResult result = MessageBox.Show($"An existing .hwmod file exists at {ModFolder_TextBox.Text}.\n\nWould you like to overwrite this existing file?", "Overwrite Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        File.Delete(hwmod_file);
                        SerializeManifest(hwmod_file, ModName_TextBox.Text, ModAuthor_TextBox.Text, Banner_TextBox.Text, Icon_TextBox.Text, Desc_TextBox.Text, ModID);
                    }
                }
                else
                    SerializeManifest(hwmod_file, ModName_TextBox.Text, ModAuthor_TextBox.Text, Banner_TextBox.Text, Icon_TextBox.Text, Desc_TextBox.Text, ModID);

                _ = MessageBox.Show("Mod manifest saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception error)
            {
                _ = MessageBox.Show($"{error.Message}\n\n\n{error.StackTrace}", "Exception Caught", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
