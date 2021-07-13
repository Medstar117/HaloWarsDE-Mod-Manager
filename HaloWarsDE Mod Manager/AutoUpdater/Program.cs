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

namespace AutoUpdater
{
    public class PatchData
    {
        // "Global" Data
        public Version Version { get; }
        public Uri FileURI { get; }
        private Uri RepoReleaseURI { get; set; }

        public Uri GrabPatchFileUri(Release releaseData, string ReleasePackageName)
        {
            /*************************************************
            * Loop through the data to find the download link
            * for the release (the AutoUpdatePackage.zip file).
            *************************************************/

            Uri temp = null;

            for (int i = 0; i < releaseData.assets.Count; ++i)
            {
                if (releaseData.assets[i].name == ReleasePackageName)
                {
                    temp = new Uri(releaseData.assets[i].browser_download_url);
                    break;
                }
            }

            return temp;
        }

        public PatchData(string MainRepoURL, string ReleasePackageName)
        {
            try
            {
                RepoReleaseURI = new Uri($"{MainRepoURL}/releases/latest");
                using (WebClient client = new WebClient())
                {
                    // ========== GET LATEST RELEASE'S JSON DATA ==========

                    client.Headers.Add("user-agent", "HaloWarsDE Mod Manager"); // This is needed by GitHub's API. Really, it can be anything, but it's recommended to either be one's username or the name of one's application
                    Program.ColorWriteLine("Checking for new version...");

                    // Download JSON data and set it to a Release-type object
                    Program.ColorWrite("\t--Downloading JSON data for repository's latest release...", ConsoleColor.Yellow);
                    string releaseJSON = client.DownloadString(RepoReleaseURI);
                    Release releaseInfo = JsonConvert.DeserializeObject<Release>(releaseJSON);
                    Program.ColorWriteLine("Done!", ConsoleColor.Green);

                    // Set latest release's data to variables
                    Program.ColorWrite("\t--Parsing JSON data...", ConsoleColor.Yellow);
                    FileURI = GrabPatchFileUri(releaseInfo, ReleasePackageName);
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
                Environment.Exit(-1);
            }
        }
    }

    public class Program
    {
        // Constants
        public const string ReleasePackageName = "AutoUpdatePackage.zip";
        public const string MainRepoURL = "https://api.github.com/repos/Medstar117/HWDE-Mod-Manager";

        // Globals
        public static string InstallationDirectory = Directory.GetCurrentDirectory();
        public static string UpdatesDirectory = Path.Combine(InstallationDirectory, "Updates");
        public static string PrerequisitesDirectory = Path.Combine(UpdatesDirectory, "Prerequisites");

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

                // Local Variables
                PatchData LatestPatch = new PatchData(MainRepoURL, ReleasePackageName);
                Version currentVer = new Version(args[2]);

                switch (args[0].ToLower())            // Check the mode detected
                {
                    case "-c":
                        CheckForUpdates(ref currentVer, LatestPatch.Version);
                        break;

                    case "-u":
                        switch (args[1].ToLower())    // Check if the update is automatic or manual
                        {
                            case "--auto":            // Apply update from repository automatically
                                CheckForRunningModManager(int.Parse(args[3]));
                                ApplyUpdate(ref currentVer, UpdatesDirectory, LatestPatch);
                                break;

                            case "--manual":         // Apply update package manually--should only be performed by the AutoUpdater itself since it doesn't ask for the manager's PID!
                                ApplyUpdate(ref currentVer, PrerequisitesDirectory, null, $"{args[3]}");
                                break;
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                ColorWriteLine("[ERROR] The following exception has occurred:\n\n" + e.ToString(), ConsoleColor.Red);
                Environment.Exit(-1);
            }
        }


        // ========== GENERAL FUNCTIONS ==========

        public static void ColorWrite(string text, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.Write(text);
        }

        public static void ColorWriteLine(string text, ConsoleColor foreground = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black)
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.Write(text + "\n");
        }

        private static void CheckForRunningModManager(int pid)
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

        private static string ExtractFromPackage(string patchzip, string filename, string destination = null)
        {
            /*************************************************
            * Extract all files from the downloaded .zip file
            * to the given .zip file's current directory.
            * 
            * This may need to be optimized at some point.
            *************************************************/

            // Unzip the archive
            string extractPath = null;
            using (ZipArchive AutoUpdaterZip = ZipFile.OpenRead(patchzip))
            {
                foreach (ZipArchiveEntry packedFile in AutoUpdaterZip.Entries)
                {
                    if (packedFile.Name == filename)
                    {
                        // If a destination is not specified, extract to the update package's directory
                        extractPath = (destination is null) ?
                                       Path.Combine(new FileInfo(patchzip).DirectoryName, filename) :
                                       Path.Combine(destination, filename);

                        packedFile.ExtractToFile(extractPath);
                        break;
                    }
                }
            }

            return extractPath;
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


        // ========== MAIN FUNCTIONS ==========

        private static void CheckForUpdates(ref Version currentVer, Version patchVer)
        {
            if (patchVer > currentVer)
            {
                ColorWriteLine("Update available!", ConsoleColor.Green);
                Environment.Exit(1);
            }
            else
            {
                ColorWriteLine("No update available!", ConsoleColor.Yellow);
                Environment.Exit(0);
            }
        }

        private static void ParseInstruction(UpdateInstructions.Instruction instruction, string packageFilepath)
        {
            switch (instruction.Action)
            {
                case "MOVE":
                    // Set the expected filepath
                    string fileMovePath = (instruction.FileDirectory == "ROOT") ?
                                           Path.Combine(InstallationDirectory, instruction.FileName) :
                                           Path.Combine(InstallationDirectory, instruction.FileDirectory, instruction.FileName);

                    // Delete old file if it exists
                    if (File.Exists(fileMovePath))
                        File.Delete(fileMovePath);

                    // Extract the file from the update package to the desired directory
                    _ = ExtractFromPackage(packageFilepath, instruction.FileName, fileMovePath);
                    break;

                case "DELETE":
                    // Set the expected filepath
                    string fileDeletePath = (instruction.FileDirectory == "ROOT") ?
                                             Path.Combine(InstallationDirectory, instruction.FileName) :
                                             Path.Combine(InstallationDirectory, instruction.FileDirectory, instruction.FileName);
                    
                    // Delete the specified file, if it exists
                    if (File.Exists(fileDeletePath))
                        File.Delete(fileDeletePath);

                    break;
            }
        }

        private static void ApplyUpdate(ref Version currentVer, string extractDir, PatchData patch = null, string packageFilepath = null)
        {
            // Create temporary directory for update package
            if (!Directory.Exists(extractDir))
            {
                ColorWrite($"Creating extraction directory \"{extractDir}\"...");
                _ = Directory.CreateDirectory(extractDir);
                ColorWriteLine("Done!", ConsoleColor.Green);
            }

            // Check if this is a manual or auto update
            if (packageFilepath is null)
            {
                // Downloads the newest update package if this is an auto update
                packageFilepath = Path.Combine(extractDir, ReleasePackageName);
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("user-agent", "HaloWarsDE Mod Manager");
                    ColorWrite("Downloading latest update package...");
                    client.DownloadFile(patch.FileURI, packageFilepath);
                    ColorWriteLine("Done!", ConsoleColor.Green);
                }
            }

            // Extract and parse "updates.dat"
            ColorWrite("Analyzing 'updates.dat'...");
            string updatesFile = ExtractFromPackage(packageFilepath, "updates.dat");
            UpdateInstructions updateData = new XmlDeserializer().GetUpdateInstructions(updatesFile);
            File.Delete(updatesFile);
            ColorWriteLine("Done!", ConsoleColor.Green);

            // Check for prerequisites
            ColorWrite("\nChecking for any prerequisite versions...");
            if (updateData.Prerequisites.Length > 0)
            {
                ColorWriteLine($"detected {updateData.Prerequisites.Length} prerequisite versions needed!", ConsoleColor.Yellow);
                if (!Directory.Exists(PrerequisitesDirectory))
                    Directory.CreateDirectory(PrerequisitesDirectory);

                // Download all needed prerequisites
                SortedDictionary<Version, string> prereqUpdatePackages = new SortedDictionary<Version, string>();
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("user-agent", "HaloWarsDE Mod Manager");
                    foreach (var prereq in updateData.Prerequisites)
                    {
                        // Check if the mod manager has met the current prerequisite version
                        if (new Version(prereq.Version) > currentVer)
                        {
                            ColorWrite($"\t--Downloading required version: {prereq.Version}...", ConsoleColor.Yellow);

                            // Set the data for the prerequisite file package
                            string prereqFilePath = Path.Combine(PrerequisitesDirectory, $"{prereq.Version}", ReleasePackageName);
                            Uri prereqURL = new Uri($"{MainRepoURL}/releases/download/{prereq.Version}/{ReleasePackageName}");

                            // Download the prerequisite package
                            client.DownloadFile(prereqURL, prereqFilePath);
                            prereqUpdatePackages.Add(new Version(prereq.Version), prereqFilePath);

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
                    foreach (KeyValuePair<Version, string> prereqPackageFile in prereqUpdatePackages)
                    {
                        ColorWrite($"\t--Installing prerequisite version: {prereqPackageFile.Key}...", ConsoleColor.Yellow);
                        ProcessStartInfo PrereqInfo = new ProcessStartInfo
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            FileName = Path.Combine(Directory.GetCurrentDirectory(), "AutoUpdater.exe"),
                            Arguments = $"-u --manual {currentVer} \"{prereqPackageFile.Value}\""
                        };
                        Process AutoUpdater = new Process { StartInfo = PrereqInfo };
                        AutoUpdater.Start();
                        AutoUpdater.WaitForExit();
                        ColorWriteLine("Done!", ConsoleColor.Green);
                    }
                }

                // Clean up
                if (patch != null)
                    Directory.Delete(PrerequisitesDirectory, true);
            }
            else
                ColorWriteLine("no prerequisite versions detected!", ConsoleColor.Green);

            // Install package contents to their respectful locations
            ColorWrite($"\nInstalling version {patch.Version}'s contents...");
            foreach (var instruction in updateData.InstructionList)
                ParseInstruction(instruction, packageFilepath);
            ColorWriteLine("Done!", ConsoleColor.Green);

            // Clean up
            Directory.Delete(extractDir, true);
            
            // Restart mod manager if this was an auto update
            if (packageFilepath is null)
                RestartManager();
        }

    }
}
