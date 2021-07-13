// Built-ins
using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Serialization;
using System.Collections.ObjectModel;

// Packages
using LibGit2Sharp;
using Ookii.Dialogs.Wpf;

// Personal
using Globals;
using static Globals.Main;
using DataSerialization.Downloadable;
using HaloWarsDE_Mod_Manager.Modules;

namespace HaloWarsDE_Mod_Manager
{
    /// <summary>
    /// Interaction logic for OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        // ----- Settings Variables -----
        public bool FirstChange = false;
        public bool DistroModified = false;
        public bool ModPathModified = false;
        public bool TimeoutDelayModified = false;

        // ----- Downloader Variables -----
        private static GitHubRepo repository;
        private static bool dataSet = false;

        // ----- Mod Stuff -----
        private readonly Uri SupportedModsXML_URL = new Uri("https://raw.githubusercontent.com/Medstar117/HaloWarsDE-Mod-Manager/data_tracker/supported_mods.xml");
        private readonly string SupportedModsJSON_FILE = $"{Directory.GetCurrentDirectory()}\\Data\\supported_mods.xml";

        public static ObservableCollection<Mod> DownloadableModList { get; } = new ObservableCollection<Mod>();


        public OptionsWindow()
        {
            DataContext = this;

            // Initialize the window
            InitializeComponent();

            // Display currently-set global data
            DistroComboBox.SelectedValue = GameDistro;
            FilePathTextBox.Text = UserModsFolder;
            TimeoutDelay_IntUpDwn.Value = TimeoutDelay;
            ModManagerVerLabel.Content = $"Mod Manager Version: {ManagerVer}";
            AddModsToList();
        }

        // ----- Window Functions -----
        private void Change_Tab(object sender, RoutedEventArgs e)
        {
            string buttonPressed = (sender as Button).Content.ToString();

            for (int index = 0; index < TabDisplay.Items.Count; index++)
                if ((TabDisplay.Items[index] as TabItem).Header.ToString() == buttonPressed)
                    TabDisplay.SelectedIndex = index;

        }

        private void AddModsToList()
        {
            if (DownloadableModList.Count == 0)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        // Grab list of supported mods
                        client.Headers.Add("user-agent", "HaloWarsDE Mod Manager");
                        string xmlData = client.DownloadString(SupportedModsXML_URL);
                        
                        // Deserialize downloaded data
                        XmlSerializer serializer = new XmlSerializer(typeof(SupportedMods));
                        using (TextReader reader = new StringReader(xmlData))
                        {
                            SupportedMods supported = (SupportedMods)serializer.Deserialize(reader);

                            // Add all deserialized mods to the ModList
                            foreach (ModEntry downloadable_mod in supported.Mods)
                            {
                                bool isInstalled = false;
                                foreach (DataSerialization.Mod installed_mod in MainWindow.ModList)
                                {
                                    if (installed_mod.ModID == downloadable_mod.ModID)
                                    {
                                        isInstalled = true;
                                        break;
                                    }
                                }
                                DownloadableModList.Add(new Mod(downloadable_mod.Title,
                                                                downloadable_mod.Author,
                                                                downloadable_mod.Info.Description,
                                                                downloadable_mod.Info.ShowcaseArtURL,
                                                                downloadable_mod.Info.DownloadURL,
                                                                isInstalled));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _ = MessageBox.Show("Error" + e);
                }
            }

            ListBox_DownloadableModsList.SelectedIndex = 0;
        }


        // ----- Settings Functions -----
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Write new config data if either DataModified flag is set to "true"
            if (DistroModified || ModPathModified || TimeoutDelayModified)
                ConfigHandler.WriteConfigData(DistroComboBox.SelectedValue.ToString(), FilePathTextBox.Text);
        }

        private void DistroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ignore the first change since its called to set the data on initialization
            if (FirstChange)
                DistroModified = DistroComboBox.SelectedValue.ToString() != GameDistro;
            else
                FirstChange = true;
        }

        private void TimeoutDelay_IntUpDwn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TimeoutDelay_IntUpDwn.Value != TimeoutDelay && TimeoutDelay_IntUpDwn.Value != null)
            {
                TimeoutDelayModified = true;
                TimeoutDelay = (int)TimeoutDelay_IntUpDwn.Value;
            }
        }

        private void Prompt_ModFolderBrowser(object sender, RoutedEventArgs e)
        {
            // Set up new folder browser dialog box
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog
            {
                SelectedPath = UserModsFolder,
                Description = "Please Select Your Mods Folder",
                UseDescriptionForTitle = true
            };

            // Show the File Explorer window and protect GameConfig.dat from being deleted accidentally
            using (FileStream protectGameConfig = new FileStream(MainWindow.GameConfigFile_UMF, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // If the dialog box is showing, set the text box to be the selected path
                if (dialog.ShowDialog() == true)
                    FilePathTextBox.Text = dialog.SelectedPath;

                protectGameConfig.Close();
            }

            // Set DataModified flag
            ModPathModified = FilePathTextBox.Text != UserModsFolder;
        }


        // ----- Downloader Functions -----
        private void Button_DownloadMod_Click(object sender, RoutedEventArgs e)
        {
            Mod SelectedMod = ListBox_DownloadableModsList.SelectedItem as Mod;
            Uri downloadableURL = new Uri(SelectedMod.DownloadURL);

            Task DownloadMod = new Task(() =>
            {
                Dispatcher.Invoke(() => { Button_DownloadMod2.IsEnabled = false; });

                using (UrlTestClient client = new UrlTestClient())
                {
                    switch (client.IsURLValid(downloadableURL))
                    {
                        case UrlTestClient.SupportedHostnames.Unsupported:
                            _ = MessageBox.Show("An unsupported hostname is detected in this mod's 'DownloadURL' element.\n\n" +
                                "This mod cannot be downloaded at this time, as this manager doesn't know how to download it (yet).",
                                "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Dispatcher.Invoke(() => { Button_DownloadMod2.IsEnabled = true; });
                            break;

                        case UrlTestClient.SupportedHostnames.ModDB:
                            _ = MessageBox.Show("Valid: ModDB");
                            Dispatcher.Invoke(() => { Button_DownloadMod2.IsEnabled = true; });
                            break;

                        case UrlTestClient.SupportedHostnames.GitHub:
                            _ = MessageBox.Show("Valid: GitHub");
                            Dispatcher.Invoke(() => { Button_DownloadMod2.IsEnabled = true; });
                            break;

                        default:
                            _ = MessageBox.Show("An invalid/inaccessible URL has been provided for this mod.\n\n" +
                                "Please make sure the provided 'DownloadURL' section includes a supported hostname and/or an accessible download page.",
                                "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Dispatcher.Invoke(() => { Button_DownloadMod2.IsEnabled = true; });
                            break;
                    }
                }
            });

            DownloadMod.Start();

            /*
            string clone_url = repository.clone_url;
            string branch = ComboBox_GitBranch.SelectedItem.ToString();
            string clonePath = Path.Combine(Globals.UserModsFolder, repository.full_name.Replace("/", "-") + "-" + branch);
            if (!Directory.Exists(clonePath))
            {
                Task DownloadMod = new Task(() =>
                {
                    Dispatcher.Invoke(() => { Button_DownloadMod.IsEnabled = false; });
                    try
                    {
                        _ = Repository.Clone(clone_url, clonePath, new CloneOptions { BranchName = branch, OnTransferProgress = BarTransferProgress });
                        ProgressBarManager.ResetProgressBar();
                        dataSet = false;
                        MessageBox.Show("Successfully cloned mod to mods folder!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error in cloning mod to mods folder!" + "\n" + ex);
                    }

                    Dispatcher.Invoke(() => { Globals.ModScan(); });
                });

                DownloadMod.Start();
            }
            else
                MessageBox.Show("Mod folder already exists!");
            */
        }

        private void Button_CheckURL_Click(object sender, RoutedEventArgs e)
        {
            using (WebClient client = new UrlTestClient())
            {
                string url = URLTextBox.Text;

                if (!string.IsNullOrWhiteSpace(url))
                {
                    try
                    {
                        // Check if the head of the web page can be retrieved
                        _ = client.DownloadString(url);
                        switch (new Uri(url).Host)
                        {
                            // GitHub Handling
                            case "github.com":
                            case "www.github.com":
                                Label_URLType.Foreground = new SolidColorBrush(Colors.LimeGreen);
                                Label_URLType.Content = "GitHub";
                                Button_DownloadMod.IsEnabled = true;
                                ComboBox_GitBranch.IsEnabled = true;
                                ComboBox_Label.Foreground = new SolidColorBrush(Colors.White);
                                ComboBox_GitBranch.Foreground = new SolidColorBrush(Colors.Black);

                                repository = new GitHubRepo(url);

                                //int index = 0;
                                //foreach (var branch in repository.branches)
                                //{
                                //    ComboBox_GitBranch.Items.Add(branch);
                                //    if (branch == repository.default_branch)
                                //        ComboBox_GitBranch.SelectedIndex = index;
                                //    index++;
                                //}

                                break;

                            // Everything else
                            default:
                                Label_URLType.Foreground = new SolidColorBrush(Colors.Red);
                                Label_URLType.Content = "Unsupported";
                                break;
                        }
                    }
                    catch (Exception error)
                    {
                        _ = MessageBox.Show(error.ToString());
                        // If the head can't be retrieved, mark the URL as invalid
                        Label_URLType.Foreground = new SolidColorBrush(Colors.Red);
                        Label_URLType.Content = "Invalid";
                    }
                }
            }
        } // Obsolete

        private void URLTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            /***********************************************************************
             * Basically resets the content on everything if the user starts editing
             * the URL.
             **********************************************************************/

            Label_URLType.Content = "";
            Button_DownloadMod.IsEnabled = false;
            ComboBox_GitBranch.IsEnabled = false;
            ComboBox_Label.Foreground = new SolidColorBrush(Colors.DimGray);
            ComboBox_GitBranch.Foreground = new SolidColorBrush(Colors.DimGray);
            ComboBox_GitBranch.Items.Clear();
        } // Obsolete

        public static bool BarTransferProgress(TransferProgress progress)
        {
            if (!dataSet)
            {
                ProgressBarManager.InitProgressBar(0, progress.TotalObjects);
                dataSet = true;
            }

            ProgressBarManager.SetProgressBarData(progress.IndexedObjects, false, $"Objects: {progress.IndexedObjects} of {progress.TotalObjects} [Bytes: {progress.ReceivedBytes}]");
            return true;
        }

        private void ListBox_DownloadableModsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dynamic SelectedMod = ListBox_DownloadableModsList.SelectedItem as dynamic;

            // Set the description box text
            if (DescriptionTextBox != null)
            {
                DescriptionTextBox.Clear();

                if (SelectedMod != null)
                    DescriptionTextBox.Text = $"\nAuthor: {SelectedMod.Author}\n\n\n" + $"" + SelectedMod.Description;
                else
                    ListBox_DownloadableModsList.SelectedIndex = 0;
            }

            // Set the banner art
            if (ModBannerArt != null)
            {
                ModBannerArt.Source = null;

                if (SelectedMod != null)
                {
                    ModBannerArt.Source = SelectedMod.ShowcaseArt;
                }
            }
        }

        
    }


    // ----- Custom Classes -----
    public class UrlTestClient : WebClient
    {
        /********************************************************************
         * Override for simply checking if a URL is valid by getting the HEAD
         * of a web page.
         *******************************************************************/

        public enum SupportedHostnames
        {
            Invalid,
            Unsupported,
            ModDB,
            GitHub
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest req = base.GetWebRequest(address);

            if (req.Method == "GET")
                req.Method = "HEAD";

            return req;
        }

        public SupportedHostnames IsURLValid(Uri url)
        {
            try
            {
                _ = DownloadString(url);
                switch (url.Host)
                {
                    case "github.com":
                    case "www.github.com":
                        return SupportedHostnames.GitHub;

                    case "moddb.com":
                    case "www.moddb.com":
                        return SupportedHostnames.ModDB;

                    default:
                        return SupportedHostnames.Unsupported;
                }
            }
            catch
            {
                return SupportedHostnames.Invalid;
            }
        }
    }
    /*
    public class Cryptographer
    {
        public bool CompareHashes(Mod mod, string filepath)
        {
            if (mod.ID == GenerateFileHash(filepath))
                return true;
            else
                return false;
        }

        public string GenerateFileHash(string filepath)
        {
            using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filepath))
                    return BitConverter.ToString(md5.ComputeHash(stream)).ToLowerInvariant();
        }
    }
    */
}
