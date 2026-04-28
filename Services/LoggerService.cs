using System;
using System.IO;
using Serilog;

namespace Enjaz.Services
{
    /// <summary>
    /// خدمة التسجيل — تستخدم الآن Serilog لضمان أداء مؤسسي وسجلات مفصلة
    /// Logger service — Enterprise-Grade Logging via Serilog (Backward compatible)
    /// </summary>
    public class LoggerService : ILoggerService
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        static LoggerService()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                // SF6: Enterprise Observability setup using Serilog
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .WriteTo.File(
                        path: Path.Combine(LogDirectory, "enjaz_log_.txt"),
                        rollingInterval: RollingInterval.Day, // Daily rolling files
                        retainedFileCountLimit: 30,           // Keep only last 30 days
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();
            }
            catch { /* Fail silently */ }
        }

        // ── Static methods (backward compatibility) ──

        public static void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
                Log.Error(ex, message);
            else
                Log.Error(message);
        }

        public static void LogInfo(string message)
        {
            Log.Information(message);
        }

        public static void LogWarning(string message)
        {
            Log.Warning(message);
        }

        // ── ILoggerService instance methods (for DI injection) ──

        void ILoggerService.LogInfo(string message) => LogInfo(message);
        void ILoggerService.LogError(string message, Exception? ex) => LogError(message, ex);
        void ILoggerService.LogWarning(string message) => LogWarning(message);
    }
}

