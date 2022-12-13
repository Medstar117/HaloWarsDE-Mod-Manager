using System;
using System.IO;

namespace HaloWarsDE_Mod_Manager.Core.Serialization
{
    public class Mod
    {
        #region Required
        public string? ModID   => ManifestData.ModID;
        public string  Title   => ManifestData.Required.Title   ?? (!IsVanilla ? "[Missing Element: Title]"   : "Vanilla");
        public string  Author  => ManifestData.Required.Author  ?? (!IsVanilla ? "[Missing Element: Author]"  : "Ensemble Studios");
        public string  Version => ManifestData.Required.Version ?? (!IsVanilla ? "[Missing Element: Version]" : "1.12185.2.0");
        #endregion

        #region Optional
        public string Description => $"Author: {Author}\n\n{Desc}";
        public Uri BannerArt      => GetResourceUri(ManifestData.Optional.Banner.RelativePath, "Layout/DefaultBannerArt.png");
        public Uri ShortcutIcon   => GetResourceUri(ManifestData.Optional.Icon.RelativePath,   "Icon_Blagoicons.ico");
        private string Desc => ManifestData.Optional.Desc.Text ?? (!IsVanilla ? "[Missing Element: Description]" :
                               "The classic Halo Wars: Definitive Edition Experience.\n\nFinish the fight, commander!");
        #endregion

        #region Integrity
        public string? ManifestDirectory { get; } = null;
        public bool IsVanilla => ManifestDirectory == null;
        public bool IsValid   => IsVanilla || (ModID != null && ModID == ManifestSerializer.GenerateModID(ManifestData));
        #endregion

        // TODO: Should a handler be here so that a null value can't exist?
        public ModManifest ManifestData { get; } = new();

        /// <summary>
        /// Constructs a custom Mod object for a given path to a mod manifest.
        /// </summary>
        /// <param name="manifestPath"></param>
        public Mod(string? manifestPath = null)
        {
            // Load non-vanilla data
            if (manifestPath != null)
            {
#pragma warning disable CS8601 // Possible null reference assignment.
                ManifestData      = ManifestSerializer.DeserializeManifest(manifestPath);
                ManifestDirectory = Path.GetDirectoryName(manifestPath);
#pragma warning restore CS8601 // Possible null reference assignment.
            }
        }

        /// <summary>
        /// Generates a Uri to a provided resource.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="embeddedResource"></param>
        /// <returns>A valid Uri to a resource.</returns>
        private Uri GetResourceUri(string? relativePath, string embeddedResource)
        {
            // If not vanilla and if a custom resource exists
            if (!IsVanilla && relativePath != null)
            {
                // Check if the path actually leads to a file
                string bannerPath = Path.Combine(Path.GetDirectoryName(ManifestDirectory) ?? string.Empty, relativePath);
                if (File.Exists(bannerPath)) return new Uri(bannerPath);
            }

            // If all else fails, return something useful
            return new Uri($"pack://application:,,,/Resources/{embeddedResource}");
        }
    }
}
