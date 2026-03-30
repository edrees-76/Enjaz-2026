using System.Diagnostics;
using System.IO;

namespace Enjaz.Services
{
    /// <summary>
    /// واجهة خدمة عمليات نظام التشغيل
    /// OS Operations Service Interface
    /// </summary>
    public interface IOSService
    {
        void OpenFile(string path);
        void OpenDirectory(string path);
        void PrintFile(string path);
        void PrintFileTo(string path, string printerName);
    }

    /// <summary>
    /// تنفيذ خدمة عمليات نظام التشغيل
    /// OS Operations Service Implementation
    /// </summary>
    public class OSService : IOSService
    {
        /// <summary>
        /// فتح ملف باستخدام التطبيق الافتراضي
        /// </summary>
        public void OpenFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                LoggerService.LogError($"فشل فتح الملف: {path}", ex);
            }
        }

        /// <summary>
        /// فتح مجلد في مستكشف الملفات
        /// </summary>
        public void OpenDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                LoggerService.LogError($"فشل فتح المجلد: {path}", ex);
            }
        }

        public void PrintFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            try
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    Verb = "print",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                LoggerService.LogInfo($"[OSService] Sent to print: {path}");
            }
            catch (System.Exception ex)
            {
                LoggerService.LogError($"[OSService] Failed to print: {path}", ex);
            }
        }

        public void PrintFileTo(string path, string printerName)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            try
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    Verb = "printto",
                    Arguments = $"\"{printerName}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                LoggerService.LogInfo($"[OSService] Sent to print (Printer: {printerName}): {path}");
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"[OSService] Failed to print to {printerName}: {path}", ex);
                // Fallback to default print
                PrintFile(path);
            }
        }
    }
}
