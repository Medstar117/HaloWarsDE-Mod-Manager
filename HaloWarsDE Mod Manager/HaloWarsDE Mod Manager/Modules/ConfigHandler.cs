// Built-ins
using System.IO;
using System.Xml;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Serialization;

// Personal
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

            // Write new config dataDefaultUserModsFolder
            switch (result)
            {
                case MessageBoxResult.Yes:
                    WriteConfigData("Steam", "DEFAULT", true);
                    break;

                case MessageBoxResult.No:
                    WriteConfigData("Microsoft Store", "DEFAULT", true);
                    break;

                default:
                    WriteConfigData("Steam", "DEFAULT", true);
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
                UserModsFolder = (deserialized.ModsDir == "DEFAULT" || !Directory.Exists(deserialized.ModsDir)) ? App.Constants.DefaultUserModsFolder : deserialized.ModsDir;
                _ = int.TryParse(deserialized.TimeoutDelay, out TimeoutDelay);

                // Refresh mod manifest and game config variables
                App.Constants.ModManifestFile = $"{UserModsFolder}\\ModManifest.txt";
                App.Constants.GameConfigFile_UMF = $"{UserModsFolder}\\GameConfig.dat";

                // Set the selected game distribution from the user
                // and set the corresponding data depending on the
                // detected distribution.
                switch (deserialized.GameDistro)
                {
                    case "Steam":
                        GameDistro = "Steam";
                        MainWindow.LaunchCommand = Launch_HWDE_Steam;
                        App.Constants.LocalAppData_Selected = App.Constants.LocalAppData_Steam;
                        break;

                    case "Microsoft Store":
                        GameDistro = "Microsoft Store";
                        MainWindow.LaunchCommand = Launch_HWDE_MS;
                        App.Constants.LocalAppData_Selected = App.Constants.LocalAppData_MS;
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
                App.RelocateDataFolder(App.RelocateActions.Restore);

            LoadConfig();
            App.RelocateDataFolder(App.RelocateActions.Replace);
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
                Logging.WriteLogEntry("User configuration file found; loading data entries...");
                LoadConfig();
                return true;
            }
            else
            {
                // If config file doesn't exist, create a new one
                Logging.WriteLogEntry("First time setup detected. Creating default user mods folder and new configuration file...");

                // Create the default user mods folder if it doesn't already exist
                if (!Directory.Exists(App.Constants.DefaultUserModsFolder))
                    _ = Directory.CreateDirectory(App.Constants.DefaultUserModsFolder);

                // Create the new config file
                CreateConfig();
                return false;
            }
        }
    }
}
