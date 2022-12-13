using Microsoft.VisualBasic.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HaloWarsDE_Mod_Manager.Core.Versioning.Old
{
    public static class PatchData
    {
        public static Uri FileURI = null;
        public static Version Version = null;
        public static readonly Uri RepoReleaseURI = new Uri($"{ApiRepoURL}/releases/latest");

        private static void ParseJSON(Release releaseData)
        {
            Log.Information("Parsing JSON data...");
            //Logging.WriteLogEntry("Parsing JSON data...");

            // Get and set download URL
            foreach (ReleaseAsset asset in releaseData.assets)
                if (asset.name == ReleasePackageName)
                    FileURI = new Uri(asset.browser_download_url);

            // Set version info
            Version = new Version(releaseData.tag_name);
        }

        public static void FetchUpdateData()
        {
            try
            {
                using (WebClient github_client = new WebClient())
                {
                    github_client.Headers.Add("user-agent", "HaloWarsDE-Mod-Manager"); // Required by Github's API

                    // Download and parse JSON data
                    //Logging.WriteLogEntry("Downloading JSON data for latest release...");
                    Log.Information("Downloading JSON data for latest release...");
                    Release releaseInfo = JsonConvert.DeserializeObject<Release>(github_client.DownloadString(RepoReleaseURI));
                    ParseJSON(releaseInfo);

                    //Logging.WriteLogEntry((FileURI == null) ? "[ERROR] Failed to parse JSON info!" : "Found all data for updating!");
                    if (FileURI == null)
                    {
                        Log.Error("Failed to parse JSON info!");
                        return;
                    }
                    Log.Information("Found all data for updating!");
                }
            }
            catch (WebException e)
            {
                //Logging.WriteLogEntry("[ERROR] Could not fetch latest release info!");
                Log.Error("Could not fetch latest release info!");
                App.WriteExceptionInfo(e);
            }
            catch (Exception e)
            {
                //Logging.WriteLogEntry("[ERROR] Unknown exception has occurred!");
                Log.Error("Unknown exception has occurred!");
                App.WriteExceptionInfo(e);
            }
        }
    }
}
