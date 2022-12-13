using System.Collections.Generic;

namespace AutoUpdater.Core.Serialization
{
#pragma warning disable CS8618, CS0649
    internal class Release
    {
        public string tag_name;
        public List<ReleaseAsset> assets;
    }

    internal class ReleaseAsset
    {
        public string name;
        public string browser_download_url;
    }
#pragma warning restore CS8618, CS0649
}
