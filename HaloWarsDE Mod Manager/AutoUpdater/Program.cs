// Built-ins
using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.IO.Compression;
using System.Collections.Generic;

// Packages
using Newtonsoft.Json;

// Personal
using DataSerialization.AutoUpdater.XML;
using DataSerialization.AutoUpdater.JSON;
using static AutoUpdater.Constants;

namespace AutoUpdater
{
    public class PatchData
    {
        public Version Version { get; }
        public Uri FileURI { get; }
        private Uri RepoReleaseURI => new Uri($"{ApiRepoURL}/releases/latest");

        public Uri GrabPatchFileUri(Release releaseData)
        {
            /*************************************************
            * Loop through the data to find the download link
            * for the release (the AutoUpdatePackage.zip file).
            *************************************************/

            foreach (ReleaseAsset asset in releaseData.assets)
            {
                if (asset.name == ReleasePackageName)
                {
                    return new Uri(asset.browser_download_url);
                }
            }

            return null; // Just in case no package is found
        }

        public PatchData()
        {
            try
            {
                using (WebClient github_client = new WebClient())
                {
                    github_client.Headers.Add("user-agent", "HaloWarsDE Mod Manager"); // Requried by GitHub's API
                    Program.ColorWriteLine("Checking for new version...");

                    // Download JSON data and set it to a Release-type object
                    Program.ColorWrite("\t--Downloading JSON data for repository's latest release...", ConsoleColor.Yellow);
                    string releaseJSON = github_client.DownloadString(RepoReleaseURI);
                    Release releaseInfo = JsonConvert.DeserializeObject<Release>(releaseJSON);
                    Program.ColorWriteLine("Done!", ConsoleColor.Green);

                    // Set latest release's data to variables
                    Program.ColorWrite("\t--Parsing JSON data...", ConsoleColor.Yellow);
                    FileURI = GrabPatchFileUri(releaseInfo);
                    Version = new Version(releaseInfo.tag_name);
                    Program.ColorWriteLine("Done!", ConsoleColor.Green);

                    // If no patchUri is found (the AutoUpdatePackage.zip file), terminate the program
                    if (FileURI == null)
                    {
                        Program.ColorWriteLine("No update found. Exiting...", ConsoleColor.Yellow);
                        Environment.Exit(-1);
                    }
                    else
                        Program.ColorWriteLine("Found all data for updating!\n", ConsoleColor.Green);

                }
            }
            catch (WebException)
            {
                Program.ColorWriteLine("[ERROR] Could not fetch latest release info!", ConsoleColor.Red);
                Environment.Exit(0);
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // Aesthetics
            ColorWriteLine("*********************************************************", ConsoleColor.Cyan);
            ColorWriteLine("HaloWarsDE Mod Manager Auto-Updater", ConsoleColor.Cyan);
            ColorWriteLine("Author: Medstar\n\t--Based on Blackandfan's modified version of x6767's script", ConsoleColor.Cyan);
            ColorWriteLine("*********************************************************\n\n", ConsoleColor.Cyan);

            try
            {
                // ========== COMMAND LINE PARSING ==========

                /* Arguments:
                 * 
                 * 3 options
                 *
                 *      Check For Updates:     -c Mod_Manager_Version
                 *      Auto Update:           -u --auto Mod_Manager_Version ManagerPID
                 *      Manual Update:         -u --manual Mod_Manager_Version "full\\path\\to\\AutoUpdatePackage.zip"
                 */

                PatchData latest_patch = new PatchData();
                switch (args[0].ToLower())            // Check the mode detected
                {
                    // Check for updates
                    case "-c":
                        if (latest_patch.Version > new Version(args[1]))
                        {
                            ColorWriteLine("Update available!", ConsoleColor.Green);
                            Environment.Exit(1);
                        }
                        else
                        {
                            ColorWriteLine("No update available!", ConsoleColor.Yellow);
                            Environment.Exit(0);
                        }
                        break;

                    // Apply update package
                    case "-u":
                        Version managerVer = new Version(args[2]);
                        switch (args[1].ToLower())
                        {
                            case "--auto":
                                if (Directory.Exists(UpdatesDirectory))
                                    Directory.Delete(UpdatesDirectory, true);
                                CheckForRunningInstance(int.Parse(args[3]));
                                ApplyUpdate(ref managerVer, LatestPatchData: latest_patch);
                                break;

                            case "--manual":
                                ApplyUpdate(ref managerVer, packageZip: $"{args[3]}");
                                break;
                        }
                        break;

                    // No action; user probably launched the application manually
                    default:
                        Environment.Exit(0);
                        break;
                }
            }
            catch (Exception e)
            {
                ColorWriteLine("[ERROR] The following exception has occurred:\n\n" + e.ToString(), ConsoleColor.Red);
                Environment.Exit(-1);
            }
        }


        #region General Purpose Functions
        public static void ColorWrite(string text, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.Write(text);
            Console.ResetColor();
        }

        public static void ColorWriteLine(string text, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        private static void CheckForRunningInstance(int pid)
        {
            /***********************************************************
             * Check if the mod manager is still running or not.
             * Ensures that the manager is closed before doing anything.
             **********************************************************/

            try
            {
                ColorWriteLine("Checking for running mod manager process...", ConsoleColor.Yellow);
                Process modmanager = Process.GetProcessById(pid);
                if (modmanager.ProcessName.Length > 0 || modmanager.ProcessName.Contains("HaloWarsDE Mod Manager"))
                {
                    ColorWrite("\t--Mod manager process detected. Waiting for mod manager to close...", ConsoleColor.Yellow);
                    modmanager.WaitForExit();
                    ColorWriteLine("Done!", ConsoleColor.Green);
                }
                else
                    ColorWriteLine("Mod manager is not running; continuing update.", ConsoleColor.Green);
            }
            catch (ArgumentException)
            {
                ColorWriteLine("Mod manager is not running; continuing update.", ConsoleColor.Green);
            }
        }

        private static void RestartManager()
        {
            ColorWrite("Restarting mod manager...", ConsoleColor.Yellow);

            // Set up data to restart the manager on close
            Process manager = new Process();
            manager.StartInfo.UseShellExecute = false;
            manager.StartInfo.FileName = Path.Combine(InstallationDirectory, "HaloWarsDE Mod Manager.exe");

            // Start the manager, close the AutoUpdater
            _ = manager.Start();
            ColorWriteLine("launched!", ConsoleColor.Green);
            Environment.Exit(0);
        }
        #endregion


        #region Update-Handling Functions
        private static string ExtractFromPackage(string patchZip, string filename, string destination = null)
        {
            /*************************************************
            * Extract all files from the downloaded .zip file
            * to the given .zip file's current directory.
            * 
            * This may need to be optimized at some point.
            *************************************************/

            // Unzip the archive
            string extractPath = null;
            using (ZipArchive AutoUpdaterZip = ZipFile.OpenRead(patchZip))
            {
                foreach (ZipArchiveEntry packedFile in AutoUpdaterZip.Entries)
                {
                    if (packedFile.Name == filename)
                    {
                        // If a destination is not specified, extract to the update package's directory
                        extractPath = (destination is null) ?
                                       Path.Combine(Path.GetDirectoryName(patchZip), filename) :
                                       Path.Combine(destination, filename);

                        packedFile.ExtractToFile(extractPath);
                        break;
                    }
                }
            }

            return extractPath; // Just in case the file couldn't be found
        }

        private static UpdateInstructions ExtractUpdateData(ref string packageZip)
        {
            ColorWrite("Analyzing 'updates.dat'...");
            string updatesFile = ExtractFromPackage(packageZip, "updates.dat");
            UpdateInstructions updateData = new XmlDeserializer().GetUpdateInstructions(updatesFile);
            File.Delete(updatesFile);
            ColorWriteLine("Done!", ConsoleColor.Green);
            return updateData;
        }

        private static void InstallPackageContents(ref UpdateInstructions updateData, string packageZip)
        {
            foreach (UpdateInstructions.Instruction instruction in updateData.InstructionList)
            {
                // Expected filepath to work with
                string instructionFilepath = (instruction.FileDirectory == "ROOT") ? InstallationDirectory : Path.Combine(InstallationDirectory, instruction.FileDirectory);

                switch (instruction.Action)
                {
                    case "MOVE":
                        // Delete old file if it exists
                        if (File.Exists(Path.Combine(instructionFilepath, instruction.FileName)))
                            File.Delete(Path.Combine(instructionFilepath, instruction.FileName));

                        // Extract the file from the update package to the desired directory
                        _ = ExtractFromPackage(packageZip, instruction.FileName, instructionFilepath);
                        break;

                    case "DELETE":
                        // Delete the specified file, if it exists
                        if (File.Exists(Path.Combine(instructionFilepath, instruction.FileName)))
                            File.Delete(Path.Combine(instructionFilepath, instruction.FileName));
                        break;
                }
            }
            ColorWriteLine("Done!", ConsoleColor.Green);
        }

        private static void FetchPrerequisites(ref UpdateInstructions updateData, ref Version managerVer)
        {
            ColorWrite("\nChecking for any prerequisite versions...");
            if (updateData.Prerequisites != null && updateData.Prerequisites.Length > 0)
            {
                // Create prerequisites directory
                ColorWriteLine($"detected {updateData.Prerequisites.Length} prerequisite versions needed!", ConsoleColor.Yellow);
                if (!Directory.Exists(PrerequisitesDirectory))
                    Directory.CreateDirectory(PrerequisitesDirectory);

                // Download all needed prerequisites
                SortedDictionary<Version, string> prereqUpdatePackages = new SortedDictionary<Version, string>();
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("user-agent", "HaloWarsDE Mod Manager");
                    foreach (UpdateInstructions.Prerequisite prereq in updateData.Prerequisites)
                    {
                        // Check if the mod manager has met the current prerequisite version
                        if (new Version(prereq.Version) > managerVer)
                        {
                            ColorWrite($"\t--Downloading required version: {prereq.Version}...", ConsoleColor.Yellow);

                            // Set the data for the prerequisite file package
                            string prereqZip = Path.Combine(PrerequisitesDirectory, $"{prereq.Version}", ReleasePackageName);
                            Uri prereqURL = new Uri($"{GithubRepoURL}/releases/download/{prereq.Version}/{ReleasePackageName}");

                            // Download the prerequisite package
                            if (!Directory.Exists(Path.GetDirectoryName(prereqZip)))
                                Directory.CreateDirectory(Path.GetDirectoryName(prereqZip));
                            client.DownloadFile(prereqURL, prereqZip);
                            prereqUpdatePackages.Add(new Version(prereq.Version), prereqZip);

                            ColorWriteLine("Done!", ConsoleColor.Green);
                        }
                        else
                            ColorWriteLine($"\t--Prereq version {prereq.Version} already met!", ConsoleColor.Green);
                    }
                }

                // Install downloaded prerequisite versions (if any were downloaded)
                if (prereqUpdatePackages.Count > 0)
                {
                    ColorWriteLine("\nInstalling downloaded prerequisites...");
                    foreach (KeyValuePair<Version, string> prereqPackageZip in prereqUpdatePackages)
                    {
                        ColorWrite($"\t--Installing prerequisite version: {prereqPackageZip.Key}...", ConsoleColor.Yellow);
                        ProcessStartInfo PrereqInfo = new ProcessStartInfo
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            FileName = Path.Combine(InstallationDirectory, "AutoUpdater.exe"),
                            Arguments = $"-u --manual {managerVer} \"{prereqPackageZip.Value}\""
                        };
                        Process AutoUpdater = new Process { StartInfo = PrereqInfo };
                        _ = AutoUpdater.Start();
                        AutoUpdater.WaitForExit();
                        ColorWriteLine("Done!", ConsoleColor.Green);
                    }
                }
            }
            else
                ColorWriteLine("None!", ConsoleColor.Green);
        }

        private static void ApplyUpdate(ref Version managerVer, PatchData LatestPatchData = null, string packageZip = null)
        {
            try
            {
                // Check what kind of update we're doing
                bool IsAutoUpdate = LatestPatchData != null;

                // If this is an auto-update, download the latest AutoUpdatePackage.zip
                if (IsAutoUpdate)
                {
                    // Create updates directory
                    if (!Directory.Exists(UpdatesDirectory))
                    {
                        ColorWrite($"Creating extraction directory \"{UpdatesDirectory}\"...");
                        Directory.CreateDirectory(UpdatesDirectory);
                        ColorWriteLine("done!", ConsoleColor.Green);
                    }

                    packageZip = Path.Combine(UpdatesDirectory, ReleasePackageName);
                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("user-agent", "HaloWarsDE Mod Manager");
                        ColorWrite("Downloading latest update package...");
                        client.DownloadFile(LatestPatchData.FileURI, packageZip);
                        ColorWriteLine("done!", ConsoleColor.Green);
                    }
                }

                // Extract and parse "updates.dat"
                UpdateInstructions updateData = ExtractUpdateData(ref packageZip);

                // Check for and install prerequisites
                if (IsAutoUpdate)
                    FetchPrerequisites(ref updateData, ref managerVer);

                // Install the current package
                InstallPackageContents(ref updateData, packageZip);

                // Clean up
                Directory.Delete(Path.GetDirectoryName(packageZip), true);

                // Restart manager if this was an auto-update
                if (IsAutoUpdate)
                    RestartManager();
            }
            catch (Exception e)
            {
                ColorWriteLine(e.Message, ConsoleColor.Red);
                Console.ReadKey();
            }
        }
        #endregion

    }
}
