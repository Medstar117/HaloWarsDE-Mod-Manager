using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using System.Xml;
using System.Security.Cryptography;

namespace HaloWarsDE_Mod_Manager.Core.Versioning.Old
{
    namespace Internal
    {
        [XmlRoot("Config", IsNullable = false)]
        public sealed class UserConfig
        {
            [XmlAttribute]
            public string ReleaseVer { get; set; }

            [XmlElement("Distro", IsNullable = false)]
            public string GameDistro { get; set; }

            [XmlElement("ModsDir", IsNullable = false)]
            public string ModsDir { get; set; }

            [XmlElement("TimeoutDelay", IsNullable = false)]
            public string TimeoutDelay { get; set; }
        }

        /*
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
        */
    }

    namespace Mods
    {
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

            #region Internal Classes
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
            #endregion
        }

        public class ManifestSerializer
        {
            #region Private Variables
            private static readonly XmlSerializerNamespaces xns = new XmlSerializerNamespaces();
            private static readonly XmlSerializer xs = new XmlSerializer(typeof(ModManifest));
            private static readonly XmlWriterSettings xws = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };
            #endregion

            public static void SerializeManifest(string outFile, string title, string author, string version,
                                                 string banner_path = null, string icon_path = null,
                                                 string description = null, string mod_id = null)
            {
                // Create new manifest object and remove the auto-filled namespaces
                ModManifest data = new ModManifest();
                if (xns.Count != 1) { xns.Add(string.Empty, string.Empty); }

                // Required data
                data.Required.Title = title;
                data.Required.Author = author;
                data.Required.Version = version;

                // Optional data
                data.Optional.Banner.RelativePath = banner_path;
                data.Optional.Icon.RelativePath = icon_path;
                data.Optional.Desc.Text = description;

                // Create unique ModID for the mod; if ModID is provided, don't make a new one
                data.ModID = mod_id ?? EncodeSHA256($"<{data.Required.Title}-{data.Required.Author}-{data.Required.Version}>");

                // Write the above manifest data to a new .hwmod file withing the mod's containing folder
                using (XmlWriter xw = XmlWriter.Create(outFile, xws))
                    xs.Serialize(xw, data, xns);
            }

            public static ModManifest DeserializeManifest(string filename)
            {
                try
                {
                    using (TextReader reader = new StringReader(File.ReadAllText(filename)))
                        return (ModManifest)xs.Deserialize(reader);
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }

            public static string EncodeSHA256(string str)
            {
                using (SHA256 hasher = SHA256.Create())
                {
                    byte[] encodedString = new UTF8Encoding(false).GetBytes(str);
                    byte[] hashedBytes = hasher.ComputeHash(encodedString);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashedBytes.Length; i++)
                        sb.Append(hashedBytes[i].ToString("X2"));

                    return sb.ToString().Replace("-", string.Empty);
                }
            }
        }

        public class ModObj
        {
            // Required data
            public string ModID { get; } = null;
            public string Title { get; } = "Vanilla";
            public string Author { get; } = "Ensemble Studios";
            public string Version { get; } = "1.12185.2.0";

            // Optional data
            public string ShortcutIcon { get; } = null;
            public string Description { get; } = "Author: Ensemble Studios\n\nThe classic Halo Wars: Definitive Edition Experience.\n\nFinish the fight, commander!";
            public Uri BannerArt { get; } = new Uri("pack://application:,,,/Assets/default_banner.png");

            // Update data
            //public Uri UpdateURL { get; }
            //public string UpdateURLBranch { get; }

            // Manager data
            public bool IsValid { get; } = true;
            public bool IsVanilla { get; } = true;
            public string ManifestDirectory { get; } = null;


            public ModObj(string manifestPath = null)
            {
                if (manifestPath != null)
                {
                    ModManifest mod_data = ManifestSerializer.DeserializeManifest(manifestPath);

                    if (mod_data != null)
                    {
                        #region Required data
                        ModID = mod_data.ModID;
                        Title = mod_data.Required.Title ?? "[Missing Element: Title]";
                        Author = mod_data.Required.Author ?? "[Missing Element: Author]";
                        Version = mod_data.Required.Version ?? "[Missing Element: Version]";
                        ManifestDirectory = Path.GetDirectoryName(manifestPath);
                        #endregion

                        #region Optional data
                        Description = $"Author: {Author}\n\n{mod_data.Optional.Desc.Text ?? "[Missing Element: Description]"}";

                        if (mod_data.Optional.Banner.RelativePath != null)
                            if (File.Exists(Path.Combine(Path.GetDirectoryName(manifestPath), mod_data.Optional.Banner.RelativePath)))
                                BannerArt = new Uri(Path.Combine(Path.GetDirectoryName(manifestPath), mod_data.Optional.Banner.RelativePath));

                        if (mod_data.Optional.Icon.RelativePath != null)
                            if (File.Exists(Path.Combine(Path.GetDirectoryName(manifestPath), mod_data.Optional.Icon.RelativePath)))
                                ShortcutIcon = Path.Combine(Path.GetDirectoryName(manifestPath), mod_data.Optional.Icon.RelativePath);

                        // Update data
                        //UpdateURL = (deserialized.UpdateInfo != null && deserialized.UpdateInfo.URL != null) ? new Uri(deserialized.UpdateInfo.URL) : null;
                        //UpdateURLBranch = (UpdateURL != null && (UpdateURL.Host == "github.com" || UpdateURL.Host == "www.github.com")) ? deserialized.UpdateInfo.Branch : null;
                        #endregion

                        // Manager data
                        IsValid = ModID != null && ModID == ManifestSerializer.EncodeSHA256($"<{Title}-{Author}-{Version}>");
                        IsVanilla = false;
                    }
                    else
                    {
                        #region Required data
                        Title = "[Missing Element: Title]";
                        Author = "[Missing Element: Author]";
                        Version = "[Missing Element: Version]";
                        ManifestDirectory = Path.GetDirectoryName(manifestPath);
                        #endregion

                        // Optional data
                        Description = $"Failed to load mod manifest (.hwmod file) in the following directory (malformed manifest?):\n\n{ManifestDirectory}";

                        // Manager data
                        IsValid = false;
                        IsVanilla = false;
                    }
                }
            }
        }
    }

    namespace DownloadManager
    {
        [XmlRoot("SupportedMods", IsNullable = false)]
        public class SupportedMods
        {
            [XmlElement("Mod")]
            public Index[] Mods;
        }

        public class Index
        {
            [XmlAttribute]
            public string ModID;

            [XmlAttribute]
            public string Title;

            [XmlAttribute]
            public string Author;

            [XmlElement("Data")]
            public ModInfo Info;

            [XmlElement("Pages")]
            public Page[] Pages;

            #region Internal Classes
            public class ModInfo
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

            public class Page
            {
                [XmlAttribute]
                public string Type;

                [XmlAttribute]
                public string URL;
            }
            #endregion
        }
    }

    namespace AutoUpdater
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
        /*
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
        */
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
        public class ModCollection : ObservableCollection<ModDownload>
        {
            public void AddMod(ModEntry downloadable_mod, ObservableCollection<Mods.ModObj> ModList)
            {
                bool isInstalled = false;
                foreach (Mods.ModObj installed_mod in ModList)
                {
                    if (installed_mod.ModID == downloadable_mod.ModID)
                    {
                        isInstalled = true;
                        break;
                    }
                }

                Add(new ModDownload(downloadable_mod.Title,
                            downloadable_mod.Author,
                            downloadable_mod.Info.Description,
                            downloadable_mod.Info.ShowcaseArtURL,
                            downloadable_mod.Info.DownloadURL, isInstalled));
            }
        }

        public class ModDownload
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

            public ModDownload(string title, string author, string description, string showcase_art_url, string download_url, bool is_installed)
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
