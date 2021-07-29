// Built-ins
using System.IO;
using System.Xml;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Serialization;

// Personal
using static Globals.Logging;
using static Globals.Main;

namespace HaloWarsDE_Mod_Manager.Modules
{
    [XmlRoot("Config", IsNullable = false)]
    public sealed class UserConfig
    {
        [XmlAttribute]
        public string ReleaseVer;

        [XmlElement("Distro", IsNullable = false)]
        public string GameDistro;

        [XmlElement("ModsDir", IsNullable = false)]
        public string ModsDir;

        [XmlElement("TimeoutDelay", IsNullable = false)]
        public string TimeoutDelay;
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
                    WriteConfigData("Steam", MainWindow.DefaultUserModsFolder, true);
                    break;

                case MessageBoxResult.No:
                    WriteConfigData("Microsoft Store", MainWindow.DefaultUserModsFolder, true);
                    break;

                default:
                    WriteConfigData("Steam", MainWindow.DefaultUserModsFolder, true);
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

                // Assign UserModsFolder
                UserModsFolder = deserialized.ModsDir;
                _ = int.TryParse(deserialized.TimeoutDelay, out TimeoutDelay);

                // Refresh mod manifest and game config variables
                MainWindow.ModManifestFile = $"{UserModsFolder}\\ModManifest.txt";
                MainWindow.GameConfigFile_UMF = $"{UserModsFolder}\\GameConfig.dat";

                // Set the selected game distribution from the user
                // and set the corresponding data depending on the
                // detected distribution.
                switch (deserialized.GameDistro)
                {
                    case "Steam":
                        GameDistro = "Steam";
                        MainWindow.LaunchCommand = Launch_HWDE_Steam;
                        MainWindow.LocalAppData_Selected = MainWindow.LocalAppData_Steam;
                        break;

                    case "Microsoft Store":
                        GameDistro = "Microsoft Store";
                        MainWindow.LaunchCommand = Launch_HWDE_MS;
                        MainWindow.LocalAppData_Selected = MainWindow.LocalAppData_MS;
                        break;
                }
            }
        }

        public static void WriteConfigData(string distro, string mods_dir, bool newly_created = false)
        {
            /***************************************************
		     * Writes new config data to "Data\\UserConfig.dat".
			 **************************************************/

            // Create new UserConfig object and remove namespaces
            UserConfig config = new UserConfig();
            if (xns.Count != 1) { xns.Add(string.Empty, string.Empty); }

            // Set data
            config.ReleaseVer = ManagerVer;
            config.GameDistro = distro;
            config.ModsDir = mods_dir;
            config.TimeoutDelay = TimeoutDelay.ToString();

            using (XmlWriter writer = XmlWriter.Create(ConfigFilePath, xws))
                serializer.Serialize(writer, config, xns);

            if (!newly_created)
                MainWindow.RelocateDataFolder(MainWindow.RelocateActions.Restore);

            LoadConfig();
            MainWindow.RelocateDataFolder(MainWindow.RelocateActions.Replace);
            ModScan();
        }

        public static bool Run()
        {
            /*******************************************
			* Handler for loading/creating "UserConfig.dat".
			*******************************************/

            // Check if config file exists
            if (File.Exists(ConfigFilePath))
            {
                // If config file exists, load its data
                WriteLogEntry("User configuration file found; loading data entries...");
                LoadConfig();
                return true;
            }
            else
            {
                // If config file doesn't exist, create a new one
                WriteLogEntry("First time setup detected. Creating default user mods folder and new configuration file...");

                // Create the default user mods folder if it doesn't already exist
                if (!Directory.Exists(MainWindow.DefaultUserModsFolder))
                    _ = Directory.CreateDirectory(MainWindow.DefaultUserModsFolder);

                // Create the new config file
                CreateConfig();
                return false;
            }
        }
    }
}
