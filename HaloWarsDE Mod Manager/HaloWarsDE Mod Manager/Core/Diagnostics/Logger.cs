using System;
using System.IO;
using System.Text;

namespace HaloWarsDE_Mod_Manager.Core.Diagnostics
{
    public static class Logger
    {
        // Private Members
        private static bool _logToFile = false;
        private static string _logFileName = $"AppLog [{DateTime.Now:MM-dd-yyyy HH_mm_ss}].txt";
        private static string _logFileDirectory = $"{Directory.GetCurrentDirectory()}\\Data\\Logs";

        // Public Members
        public static bool LogToFile { get => _logToFile; set => _logToFile = value; }
        public static string LogFileName { get => _logFileName; set => _logFileName = value; }
        public static string LogFileDirectory { get => _logFileDirectory; set => _logFileDirectory = value; }
        public static string LogFilePath => Path.Combine(_logFileDirectory, _logFileName);

        // Private Methods
        private static void Log(string message)
        {
            // Create log containing directory
            if (!Directory.Exists(_logFileDirectory))
                Directory.CreateDirectory(_logFileDirectory);

            // Build textual log entry
            string entry = $"[{DateTime.Now:MM-dd-yyyy HH:mm:ss}] {message}\n";

            // Write entry to current log file
            File.AppendAllText(LogFilePath, entry);
        }

        // Public Methods
        public static void LogCritical(string message)
            => Log($"[CRIT] {message}");

        public static void LogDebug(string message)
            => Log($"[DBUG] {message}");

        public static void LogError(string message)
            => Log($"[ERR ] {message}");

        public static void LogInfo(string message)
            => Log($"[INFO] {message}");

        public static void LogWarning(string message)
            => Log($"[WARN] {message}");

        public static void LogFatal(string message, Exception? ex = null)
        {
            Log($"[FATL] {message}");
            if (ex != null) LogException(ex);
        }

        public static void LogException(Exception ex)
        {
            StringBuilder message = new();
            message.AppendLine("********** EXCEPTION CAUGHT **********");
            message.AppendLine("Dumping available data...");
            message.AppendLine($"\tStackTrace: {ex.StackTrace}");
            message.AppendLine($"\tSource:     {ex.Source}");
            message.AppendLine($"\tTargetSite: {ex.TargetSite}");
            message.AppendLine($"\tMessage:    {ex.Message}");
            
            if (ex.InnerException != null)
            {
                message.AppendLine($"\tStackTrace: {ex.InnerException.StackTrace}");
                message.AppendLine($"\tSource:     {ex.InnerException.Source}");
                message.AppendLine($"\tTargetSite: {ex.InnerException.TargetSite}");
                message.AppendLine($"\tMessage:    {ex.InnerException.Message}");
            }

            Log(message.ToString());
        }
    }
}
