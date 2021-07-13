using System;
using System.Windows;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace HaloWarsDE_Mod_Manager.Modules
{
    public class GitHubRepo
    {
        #pragma warning disable 1006
        // Callable items
        public string short_name { get; set; }
        public string full_name { get; set; }
        public string clone_url { get; set; }
        public string default_branch { get; set; }
        public string latest_release_url { get; set; }
        public List<string> branches { get; set; }
        #pragma warning restore 1006

        // Internal Deserialization Classes
        #pragma warning disable 0649
        private class RepoInfo
        {
            public string name;
            public string full_name;
            public string clone_url;
            public string default_branch;
        }

        private class Release
        {
            public List<ReleaseAsset> assets;

            public class ReleaseAsset
            {
                public string browser_download_url;
            }
        }
        #pragma warning restore 0649

        // Internal Functions
        private static dynamic GetResponse(WebClient client, Uri API_Uri, string type)
        {
            client.Headers.Add("user-agent", "HaloWarsDE Mod Manager");
            string API_Response = client.DownloadString(API_Uri);

            switch (type)
            {
                case "Repo":
                    return JsonConvert.DeserializeObject<RepoInfo>(API_Response);

                case "Release":
                    return JsonConvert.DeserializeObject<Release>(API_Response);

                case "Branches":
                    List<string> string_branches = new List<string>();
                    var definition = new[] { new { name = "" } };

                    var branches = JsonConvert.DeserializeAnonymousType(API_Response, definition);
                    foreach (var branch in branches)
                        string_branches.Add(branch.name);

                    return string_branches;

                default:
                    return null;
            }
        }

        //private void DownloadMod(dynamic modObj)
        /*
        private void DownloadMod(string GitHubURL, string ModName, string branch = "master")
        {
            try
            {
                //string newModPath = Repository.Clone(modObj.UpdateURL, Path.Combine(Globals.UserModsFolder, modObj.Title), new CloneOptions { BranchName = modObj.UpdateURLBranch });
                string newModPath = Repository.Clone(GitHubURL, Path.Combine(Globals.UserModsFolder, ModName), new CloneOptions { BranchName = branch });
                Globals.WriteLogEntry($"New mod cloned to \"{newModPath}\"");
            }
            catch (Exception)
            {
                Globals.WriteLogEntry("[ERROR] Unable to clone GitHub repository to disk!");
                MessageBox.Show("Could not clone repository to disk!", "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        */


        // Main Container
        public GitHubRepo(string url)
        {
            Uri repoURI = new Uri(url.Replace("github.com", "api.github.com/repos"));
            Uri releasesURI = new Uri($"{repoURI}/releases/latest");
            Uri branchesURI = new Uri($"{repoURI}/branches");

            try
            {
                using (WebClient API_Client = new WebClient())
                {
                    // Data Gathering
                    RepoInfo repo = GetResponse(API_Client, repoURI, "Repo");
                    Release release = GetResponse(API_Client, releasesURI, "Release");
                    List<string> repo_branches = GetResponse(API_Client, branchesURI, "Branches");

                    // Data Assignment
                    this.short_name = repo.name;
                    this.full_name = repo.full_name;
                    this.clone_url = repo.clone_url;
                    this.latest_release_url = release.assets[0].browser_download_url;
                    this.default_branch = repo.default_branch;
                    this.branches = repo_branches;
                }
            }
            catch (WebException e)
            {
                MessageBox.Show("Error in fetching repository information.\nPlease refer to the error message below:\n\n" + e.ToString(), "GitHub API Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
