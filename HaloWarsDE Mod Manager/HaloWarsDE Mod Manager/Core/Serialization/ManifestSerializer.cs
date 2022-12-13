using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Xml.Serialization;
using System.Security.Cryptography;

namespace HaloWarsDE_Mod_Manager.Core.Serialization
{
    [XmlRoot("HWMod", IsNullable = false)]
    public class ModManifest
    {
        [XmlAttribute]
        public string ManifestVersion { get; set; } = "1";

        [XmlAttribute]
        public string? ModID { get; set; }

        [XmlElement("RequiredData", IsNullable = false)]
        public RequriedData Required = new();

        [XmlElement("OptionalData", IsNullable = true)]
        public OptionalData Optional = new();

        #region Internal Classes
        public class RequriedData
        {
            [XmlAttribute]
            public string? Title { get; set; }

            [XmlAttribute]
            public string? Author { get; set; }

            [XmlAttribute]
            public string? Version { get; set; }
        }

        public class OptionalData
        {
            //[XmlElement("UpdateData", IsNullable = true)]
            //public UpdateData UpdateInfo;

            [XmlElement("BannerArt", IsNullable = true)]
            public BannerArt Banner = new();

            [XmlElement("Icon", IsNullable = true)]
            public IconArt Icon = new();

            [XmlElement("Description", IsNullable = true)]
            public Description Desc = new();

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
                public string? RelativePath { get; set; }
            }

            public class IconArt
            {
                [XmlText]
                public string? RelativePath { get; set; }
            }

            public class Description
            {
                [XmlText]
                public string? Text { get; set; }
            }
        }
        #endregion
    }

    public static class ManifestSerializer
    {
        private static readonly XmlSerializerNamespaces xns = new();
        private static readonly XmlSerializer xs = new(typeof(ModManifest));
        private static readonly XmlWriterSettings xws = new() { OmitXmlDeclaration = true, Indent = true };

        #region Serialization
        /// <summary>
        /// Deserializes a ModManifest from a filepath to a new object.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static ModManifest? DeserializeManifest(string filename)
        {
            try
            {
                using (TextReader reader = new StringReader(File.ReadAllText(filename)))
                    return xs.Deserialize(reader) as ModManifest;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Serializes a ModManifest object to an Xml file
        /// to a specified filepath.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="outFile"></param>
        public static void SerializeManifest(ModManifest data, string outFile)
        {
            // Remove auto-filled namespaces
            if (xns.Count != 1) { xns.Add(string.Empty, string.Empty); }

            // Ensure a ModID is set
            data.ModID ??= GenerateModID(data);

            using (XmlWriter xw = XmlWriter.Create(outFile, xws))
                xs.Serialize(xw, data, xns);
        }
        #endregion

        #region Utility
        /// <summary>
        /// Creates a SHA256 hash unique for a given mod.
        /// Takes the provided mod's title, author, and version
        /// to generate the hash.
        /// </summary>
        /// <param name="manifestData"></param>
        /// <returns></returns>
        public static string GenerateModID(ModManifest manifestData)
        {
            StringBuilder modID = new();
            using (SHA256 hasher = SHA256.Create())
            {
                // Hash data
                string data = $"<{manifestData.Required.Title}-{manifestData.Required.Author}-{manifestData.Required.Version}>";
                byte[] encodedString = new UTF8Encoding(false).GetBytes(data);
                byte[] hashedBytes = hasher.ComputeHash(encodedString);
                
                // Build ModID string
                for (int i = 0; i < hashedBytes.Length; i++)
                    modID.Append(hashedBytes[i].ToString("X2"));
                return modID.ToString().Replace("-", string.Empty);
            }
        }
        #endregion
    }
}
