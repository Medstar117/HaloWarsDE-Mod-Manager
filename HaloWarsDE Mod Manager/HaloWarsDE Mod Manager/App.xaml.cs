using System;
using System.Windows;
using System.IO;
using Monitor.Core.Utilities;

using static Globals.Main;

namespace HaloWarsDE_Mod_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static class Constants
        {
            // LocalAppData paths
            public static readonly string LocalAppData_System = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            public static readonly string LocalAppData_Steam = $"{LocalAppData_System}\\Halo Wars";
            public static readonly string LocalAppData_MS = $"{LocalAppData_System}\\Packages\\Microsoft.BulldogThreshold_8wekyb3d8bbwe\\LocalState";
            public static string LocalAppData_Selected = null;

            // Manager-needed dirs and files
            public static readonly string DefaultUserModsFolder = $"{Directory.GetCurrentDirectory()}\\HWDE Mods";
            public static readonly string ManagerDataFilePath = $"{Directory.GetCurrentDirectory()}\\Data\\ManagerData.dat";
            public static string GameConfigFile_UMF = null;
            public static string ModManifestFile = null;
        }

        //public static void DumpData()
        //{
        //    foreach (FieldInfo field in typeof(Constants).GetType().GetFields())
        //
        //    List<string> fieldNames = typeof(Constants).GetFields().Select(field => field.Name).ToList();
        //    var fieldValues = typeof(Constants).GetFields().Select(field => field.GetValue(Constants)).ToList();
        //}

        public enum RelocateActions
        {
            Replace,
            Restore
        }

        public static void RelocateDataFolder(RelocateActions action)
        {
            /*********************************************************
            * Move GameConfig.dat to the user's mods folder and create
			* a directory junction in place of the normal Halo Wars
			* Local AppData folder.
			*			
            * This makes it to where one doesn't need to move their
            * desired mod's folder upon each launch. It also helps
			* save storage space on one's device that contains the
			* computer's OS (some people have small storage devices
			* for their operating system).
            *********************************************************/

            // Variables containing the filepaths to GameConfig.dat
            string GameLocalAppDataDir_DatFile = Path.Combine(Constants.LocalAppData_Selected, "GameConfig.dat");
            string UserModsDir_DatFile = Path.Combine(UserModsFolder, "GameConfig.dat");

            switch (action)
            {
                case RelocateActions.Replace:
                    Logging.WriteLogEntry($"Relocating GameConfig.dat to {UserModsFolder}...");
                    try
                    {
                        File.Copy(GameLocalAppDataDir_DatFile, UserModsDir_DatFile);                                                // Copy GameConfig.dat from the game's LocalAppData folder to the user's mods folder
                        Directory.Delete(Constants.LocalAppData_Selected, true);                                                    // Delete the game's original LocalAppData older
                        JunctionPoint.Create(Constants.LocalAppData_Selected, UserModsFolder, true);                                // Replace the deleted directory with a junction to the user's mods folder
                        File.SetAttributes(UserModsDir_DatFile, File.GetAttributes(UserModsDir_DatFile) | FileAttributes.Hidden);   // Hide the file to prevent accidental deletions
                        Logging.WriteLogEntry("Directory junction created from user's mods directory to the Halo Wars LocalAppData directory.");
                    }
                    catch (Exception relocateException)
                    {
                        Logging.WriteExceptionInfo(relocateException);
                    }
                    break;

                case RelocateActions.Restore:
                    Logging.WriteLogEntry("Removing the Halo Wars LocalAppData directory junction...");
                    try
                    {
                        File.SetAttributes(UserModsDir_DatFile, File.GetAttributes(UserModsDir_DatFile) & ~FileAttributes.Hidden);  // Unhide GameConfig.dat
                        JunctionPoint.Delete(Constants.LocalAppData_Selected);                                                      // Delete the LocalAppData directory junction
                        Directory.CreateDirectory(Constants.LocalAppData_Selected);                                                 // Re-create the game's original LocalAppData folder
                        File.Copy(UserModsDir_DatFile, GameLocalAppDataDir_DatFile);                                                // Copy GameConfig.dat back to the game's LocalAppData folder
                        File.Delete(UserModsDir_DatFile);                                                                           // Delete the GameConfig.dat file still in the user's mods folder
                        Logging.WriteLogEntry("GameConfig.dat moved back to original directory; game is now in original layout.");
                    }
                    catch (Exception relocateException)
                    {
                        Logging.WriteExceptionInfo(relocateException);
                    }
                    
                    break;
            }
        }
    }

    public class Logging
    {
        private static readonly string LogFileDir = $"{Directory.GetCurrentDirectory()}\\Data\\Logs";
        private static readonly string CurrentLogFileName = $"AppLog [{DateTime.Now:MM-dd-yyyy HH_mm_ss}].txt";
        private static readonly string LogFilePath = Path.Combine(LogFileDir, CurrentLogFileName);

        public static void WriteLogEntry(string entry)
        {
            /***********************************************
             * Writes a given entry to the current log file.
             **********************************************/

            // Create logging directory
            if (!Directory.Exists(LogFileDir))
                _ = Directory.CreateDirectory(LogFileDir);

            // Build the textual log entry
            string log_entry = $"[{DateTime.Now:HH:mm:ss}] {entry}\n";

            // Write the log entry to the current log file.
            File.AppendAllText(LogFilePath, log_entry);
        }

        public static void WriteExceptionInfo(Exception exception)
        {
            WriteLogEntry("Unhandled exception caught. Dumping available data...");
            WriteLogEntry($"StackTrace: {exception.StackTrace}\n\nSource: {exception.Source}\n\nTargetSite: {exception.TargetSite}\n\nMessage: {exception.Message}");
            WriteLogEntry($"Additional information of any inner exceptions....");
            WriteLogEntry($"StackTrace: {exception.InnerException.StackTrace}\n\nSource: {exception.InnerException.Source}\n\nTargetSite: {exception.InnerException.TargetSite}\n\nMessage: {exception.InnerException.Message}");
        }
    }
}
