using System;
using System.IO;

namespace Enjaz.Services
{
    /// <summary>
    /// خدمة التسجيل — تدعم كلاً من الاستدعاء الثابت (Static) والحقن عبر DI
    /// Logger service — supports both static calls (backward compat) and DI injection
    /// </summary>
    public class LoggerService : ILoggerService
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly object LockObj = new object();
        private const int MaxLogAgeDays = 30;

        static LoggerService()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
                CleanupOldLogs();
            }
            catch { /* Fail silently */ }
        }

        private static string GetCurrentLogPath()
        {
            return Path.Combine(LogDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");
        }

        // ── Static methods (backward compatibility for 27+ files) ──

        public static void LogError(string message, Exception? ex = null)
        {
            Log("ERROR", message, ex);
        }

        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public static void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        // ── ILoggerService instance methods (for DI injection) ──

        void ILoggerService.LogInfo(string message) => LogInfo(message);
        void ILoggerService.LogError(string message, Exception? ex) => LogError(message, ex);
        void ILoggerService.LogWarning(string message) => LogWarning(message);

        // ── Core logging ──

        private static void Log(string level, string message, Exception? ex = null)
        {
            try
            {
                lock (LockObj)
                {
                    using (StreamWriter writer = File.AppendText(GetCurrentLogPath()))
                    {
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}");
                        if (ex != null)
                        {
                            writer.WriteLine($"Exception: {ex.Message}");
                            writer.WriteLine($"Stack Trace: {ex.StackTrace}");
                        }
                        writer.WriteLine(new string('-', 50));
                    }
                }
            }
            catch { /* Fail silently */ }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var files = Directory.GetFiles(LogDirectory, "app_*.log");
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < DateTime.Now.AddDays(-MaxLogAgeDays))
                    {
                        fileInfo.Delete();
                    }
                }
            }
            catch { /* Fail silently */ }
        }
    }
}

