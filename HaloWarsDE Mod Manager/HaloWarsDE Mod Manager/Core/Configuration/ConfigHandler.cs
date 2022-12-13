using HaloWarsDE_Mod_Manager.Core.Diagnostics;
using System.IO;
using System.Xml;
using System.Windows;
using System.Xml.Serialization;
using WPFCustomMessageBox.Net6;

namespace HaloWarsDE_Mod_Manager.Core.Configuration
{
    [XmlRoot("Config", IsNullable = false)]
    public class Config
    {
        [XmlAttribute]
        public string? ReleaseVer { get; set; }   = Constants.AppVersion;

        [XmlElement("Distro", IsNullable = false)]
        public string GameDistro { get; set; }   = "Steam";

        [XmlElement("ModsDir", IsNullable = false)]
        public string ModsDir { get; set; }      = "DEFAULT";

        [XmlElement("TimeoutDelay", IsNullable = false)]
        public string TimeoutDelay { get; set; } = "8";
    }

    internal static class ConfigHandler
    {
        private static readonly XmlSerializerNamespaces xns = new();
        private static readonly XmlSerializer serializer = new(typeof(Config));
        private static readonly XmlWriterSettings xws = new() { OmitXmlDeclaration = true, Indent = true };

        public static string ConfigFilePath = $"{Directory.GetCurrentDirectory()}\\Data\\Config.dat";

        /// <summary>
        /// Handler for loading/creating "Config.dat".
        /// </summary>
        internal static bool Run()
        {
            // Load user configuration
            if (File.Exists(ConfigFilePath))
            {
                Logger.LogInfo("User configuration file found; loading data entries...");
                LoadConfig();
                return true;
            }
            
            // First time setup (no config file found)
            Logger.LogInfo("First time setup detected. Creating default user mods folder and new configuration file...");
            if (!Directory.Exists(App.DefaultUserModsFolder))
                Directory.CreateDirectory(App.DefaultUserModsFolder);
            return CreateConfig();
        }

        /// <summary>
        /// Create a new "Config.dat" file in the Data folder.
        /// </summary>
        public static bool CreateConfig()
        {
            // Ask for game distro
            MessageBoxResult result = CustomMessageBox.ShowYesNo(messageBoxText:"Please select your game distribution (you can change this later).",
                                                                 caption:"First Time Setup", yesButtonText:"Steam", noButtonText:"Microsoft Store");

            // Check if window was closed prematurely
            if (result == MessageBoxResult.None)
            {
                Logger.LogError("No configuration selected.");
                return false;
            }

            // Write appropriate config data
            App.UserConfig.GameDistro = (result == MessageBoxResult.Yes) ? "Steam" : "Microsoft Store";
            WriteConfigData();
            return true;
        }

        /// <summary>
        /// Load an existing "Config.dat" file from the Data folder.
        /// </summary>
        public static void LoadConfig()
        {
#pragma warning disable CS8600, CS8601 // Converting null literal or possible null value to non-nullable type.
            using (TextReader reader = new StringReader(File.ReadAllText(ConfigFilePath)))
                App.UserConfig = (Config)serializer.Deserialize(reader);
#pragma warning restore CS8600, CS8601 // Converting null literal or possible null value to non-nullable type.
        }

        /// <summary>
        /// Writes new config data to "Data\\Config.dat".
        /// </summary>
        public static void WriteConfigData()
        {
            // Remove default namespaces
            if (xns.Count != 1) { xns.Add(string.Empty, string.Empty); }

            // Write new data
            using (XmlWriter writer = XmlWriter.Create(ConfigFilePath, xws))
                serializer.Serialize(writer, App.UserConfig, xns);

            // Reload config data and rescan for installed mods
            LoadConfig();
            App.ScanForMods();
        }
    }
}
