using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;

namespace DataSerialization
{
    public class Mod
    {
        // Required data
        public string ModID { get; }
        public string Title { get; }
        public string Author { get; }
        public string Version { get; }

        // Optional data
        public string Description { get; }
        public Uri BannerArt { get; }
        public string ShortcutIcon { get; }

        // Update data
        //public Uri UpdateURL { get; }
        //public string UpdateURLBranch { get; }

        // Manager data
        public bool IsValid { get; }
        public bool IsVanilla { get; }
        public string ManifestDirectory { get; }


        public Mod(string manifestPath = null)
        {
            if (manifestPath != null)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Serializable.ModManifest));
                using (TextReader reader = new StringReader(File.ReadAllText(manifestPath)))
                {
                    Serializable.ModManifest mod_data = (Serializable.ModManifest)serializer.Deserialize(reader);

                    // Required data
                    ModID = mod_data.ModID;
                    Title = mod_data.Required.Title ?? "[Missing Element: Title]";
                    Author = mod_data.Required.Author ?? "[Missing Element: Author]";
                    Version = mod_data.Required.Version ?? "[Missing Element: Version]";

                    // Optional data
                    Description = $"Author: {Author}\n\n{mod_data.Optional.Desc.Text ?? "[Missing Element: Description]"}";
                    BannerArt = mod_data.Optional.Banner.RelativePath != null ?
                        new Uri(Path.Combine(Path.GetDirectoryName(manifestPath), mod_data.Optional.Banner.RelativePath)) :
                        new Uri("pack://application:,,,/Assets/default_banner.png");

                    ShortcutIcon = mod_data.Optional.Icon.RelativePath != null ? Path.Combine(Path.GetDirectoryName(manifestPath), mod_data.Optional.Icon.RelativePath) : null;

                    // Update data
                    //UpdateURL = (deserialized.UpdateInfo != null && deserialized.UpdateInfo.URL != null) ? new Uri(deserialized.UpdateInfo.URL) : null;
                    //UpdateURLBranch = (UpdateURL != null && (UpdateURL.Host == "github.com" || UpdateURL.Host == "www.github.com")) ? deserialized.UpdateInfo.Branch : null;

                    // Manager data
                    IsValid = ModID != null && ModID == Serializable.ManifestSerializer.EncodeString_SHA256($"<{Title}-{Author}-{Version}>");
                    IsVanilla = false;
                    ManifestDirectory = Path.GetDirectoryName(manifestPath);
                }
            }
            else
            {
                // Required data
                ModID = null;
                Title = "Vanilla";
                Author = "Ensemble Studios";
                Version = "1.12185.2.0";

                // Optional data
                Description = $"Author: {Author}\n\nThe classic Halo Wars: Definitive Edition Experience.\n\nFinish the fight, commander!";
                BannerArt = new Uri("pack://application:,,,/Assets/default_banner.png");
                ShortcutIcon = null;

                // Manager data
                IsValid = true;
                IsVanilla = true;

                //this.Description = Regex.Unescape(description); for interpreting escape characters
            }
        }
    }

    namespace Serializable
    {
        [XmlRoot("ManagerData", IsNullable = false)]
        public class ManagerData
        {
            [XmlElement("Version", IsNullable = false)]
            public ManagerVersion Version = new ManagerVersion();

            public class ManagerVersion
            {
                [XmlAttribute]
                public string PatchLevel;
            }
        }


        [XmlRoot("HWMod", IsNullable = false)]
        public class ModManifest
        {
            [XmlAttribute]
            public string ManifestVersion = "1";

            [XmlAttribute]
            public string ModID;

            [XmlElement("RequiredData", IsNullable = false)]
            public RequriedData Required = new RequriedData();

            [XmlElement("OptionalData", IsNullable = true)]
            public OptionalData Optional = new OptionalData();

            // ModManifest internal classes
            public class RequriedData
            {
                [XmlAttribute]
                public string Title;

                [XmlAttribute]
                public string Author;

                [XmlAttribute]
                public string Version;
            }

            public class OptionalData
            {
                //[XmlElement("UpdateData", IsNullable = true)]
                //public UpdateData UpdateInfo;

                [XmlElement("BannerArt", IsNullable = true)]
                public BannerArt Banner = new BannerArt();

                [XmlElement("Icon", IsNullable = true)]
                public IconArt Icon = new IconArt();

                [XmlElement("Description", IsNullable = true)]
                public Description Desc = new Description();

                // OptionalData internal classes
                /*
                public class UpdateData
                {
                    [XmlAttribute]
                    public string Branch;

                    [XmlText]
                    public string URL;
                }
                */
                public class BannerArt
                {
                    [XmlText]
                    public string RelativePath;
                }

                public class IconArt
                {
                    [XmlText]
                    public string RelativePath;
                }

                public class Description
                {
                    [XmlText]
                    public string Text;
                }
            }
        }

        public class ManifestSerializer
        {
            // Private variables
            private static readonly XmlSerializerNamespaces xns = new XmlSerializerNamespaces();
            private static readonly XmlSerializer serializer = new XmlSerializer(typeof(ModManifest));
            private static readonly XmlWriterSettings xws = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };

            // Public functions
            public static void SerializeManifest(string filename, string title, string author, string version, string banner_path = null, string icon_path = null, string description = null, string mod_id = null)
            {
                // Create new manifest object and remove namespaces
                ModManifest serializable = new ModManifest();
                if (xns.Count != 1) { xns.Add(string.Empty, string.Empty); }

                // Set required data
                serializable.Required.Title = title;
                serializable.Required.Author = author;
                serializable.Required.Version = version;

                // Set optional data
                serializable.Optional.Banner.RelativePath = banner_path;
                serializable.Optional.Icon.RelativePath = icon_path;
                serializable.Optional.Desc.Text = description;

                // Create unique ModID for the mod; if ModID is provided, do not make a new one
                serializable.ModID = mod_id ?? EncodeString_SHA256($"<{serializable.Required.Title}-{serializable.Required.Author}-{serializable.Required.Version}>");

                // Write the .hwmod file to the mod's containing folder
                using (XmlWriter writer = XmlWriter.Create(filename, xws))
                    serializer.Serialize(writer, serializable, xns);
            }

            public static ModManifest DeserializeManifest(string filename)
            {
                using (TextReader reader = new StringReader(File.ReadAllText(filename)))
                    return (ModManifest)serializer.Deserialize(reader);
            }


            // Private functions
            public static string EncodeString_SHA256(string str)
            {
                using (SHA256 hash = SHA256.Create())
                {
                    byte[] encodedString = new UTF8Encoding(false).GetBytes(str);
                    byte[] hashBytes = hash.ComputeHash(encodedString);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                        sb.Append(hashBytes[i].ToString("X2"));
                    return sb.ToString().Replace("-", string.Empty);
                }
            }
        }
    }

    namespace AutoUpdater
    {
        namespace JSON
        {
            #pragma warning disable 0649
            public class ReleaseAsset
            {
                public string name;
                public string browser_download_url;
            }

            public class Release
            {
                public string tag_name;
                public List<ReleaseAsset> assets;
            }
            #pragma warning restore 0649
        }

        namespace XML
        {
            [XmlRoot("UpdateInstructions")]
            public class UpdateInstructions
            {
                [XmlArray("Prerequisites", IsNullable = true)]
                public Prerequisite[] Prerequisites;

                [XmlArray("InstructionList")]
                public Instruction[] InstructionList;


                // Internal classes
                public class Prerequisite
                {
                    [XmlAttribute]
                    public string Version;
                }

                public class Instruction
                {
                    [XmlAttribute]
                    public string Action;

                    [XmlAttribute]
                    public string FileName;

                    [XmlAttribute]
                    public string FileDirectory;
                }
            }

            public class XmlDeserializer
            {
                // Private variables
                private static readonly XmlSerializer serializer = new XmlSerializer(typeof(UpdateInstructions));

                public UpdateInstructions GetUpdateInstructions(string filepath)
                {
                    using (TextReader reader = new StreamReader(filepath))
                        return (UpdateInstructions)serializer.Deserialize(reader);
                }
            }
        }
    }

    namespace Downloadable // Incomplete
    {
        [XmlRoot("SupportedMods", IsNullable = false)]
        public class SupportedMods
        {
            [XmlElement("Mod")]
            public ModEntry[] Mods;
        }

        public class ModEntry
        {
            // Callable attributes
            [XmlAttribute]
            public string ModID;
            [XmlAttribute]
            public string Title;
            [XmlAttribute]
            public string Author;

            [XmlElement("Data")]
            public ModData Info;

            [XmlArray("HomePages")]
            public HomePage[] HomePages;


            // Internal classes
            public class ModData
            {
                [XmlAttribute]
                public string ShowcaseArtURL;
                [XmlAttribute]
                public string DownloadURL;
                [XmlAttribute]
                public string Branch;
                [XmlText]
                public string Description;
            }

            public class HomePage
            {
                [XmlAttribute]
                public string Type;
                [XmlAttribute]
                public string URL;
            }
        }

        // ----- Mod List Containers -----
        public class ModCollection : ObservableCollection<Mod>
        {
            public void AddMod(ModEntry downloadable_mod, ObservableCollection<DataSerialization.Mod> ModList)
            {
                bool isInstalled = false;
                foreach (DataSerialization.Mod installed_mod in ModList)
                {
                    if (installed_mod.ModID == downloadable_mod.ModID)
                    {
                        isInstalled = true;
                        break;
                    }
                }
                
                Add(new Mod(downloadable_mod.Title,
                            downloadable_mod.Author,
                            downloadable_mod.Info.Description,
                            downloadable_mod.Info.ShowcaseArtURL,
                            downloadable_mod.Info.DownloadURL, isInstalled));
            }
        }

        public class Mod
        {
            public string Title { get; }
            public string Author { get; }
            public string Description { get; }
            public BitmapImage ShowcaseArt { get; }
            public string DownloadURL { get; }
            public bool IsInstalled { get; set; }

            private BitmapImage GrabImageFromURL(string url)
            {
                /*************************************************
                 * Returns a new BitmapImage object based on image
                 * data downloaded from a given URL.
                 ************************************************/

                try
                {
                    using (var client = new WebClient())
                    {
                        BitmapImage image = new BitmapImage();

                        image.BeginInit();
                        image.StreamSource = client.OpenRead(url);
                        image.DecodePixelWidth = 385;
                        image.DecodePixelHeight = 90;
                        image.EndInit();

                        return image;
                    }
                }
                catch
                {
                    return new BitmapImage(new Uri("pack://application:,,,/Assets/default_banner.png"));
                }
            }

            public Mod(string title, string author, string description, string showcase_art_url, string download_url, bool is_installed)
            {
                // Data Assignment
                Title = title;
                Author = author;
                Description = Regex.Unescape(description);
                ShowcaseArt = GrabImageFromURL(showcase_art_url);
                DownloadURL = download_url;
                IsInstalled = is_installed;
            }
        }
    }
}