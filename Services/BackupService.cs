using System;
using System.IO;
using System.Threading.Tasks;

namespace Enjaz.Services
{
    public class BackupService
    {
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public BackupService(DatabaseService dbService, SettingsService settingsService)
        {
            _dbService = dbService;
            _settingsService = settingsService;
        }

        /// <summary>
        /// Performs a backup of the database file.
        /// </summary>
        /// <param name="targetDirectory">Optional directory to save the backup to. If null, uses the path from settings.</param>
        /// <returns>The path to the created backup file, or null if failed.</returns>
        public async Task<string?> PerformBackupAsync(string? targetDirectory = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string sourcePath = GetDatabasePath();
                    if (!File.Exists(sourcePath))
                    {
                        LoggerService.LogError("Backup failed: Source database file not found.", new FileNotFoundException(sourcePath));
                        return null;
                    }

                    string backupDir = targetDirectory ?? _settingsService.Current.BackupPath;
                    
                    if (string.IsNullOrWhiteSpace(backupDir))
                    {
                        backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Enjaz_Backups");
                    }

                    if (!Directory.Exists(backupDir))
                    {
                        Directory.CreateDirectory(backupDir);
                    }

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string fileName = $"certificates_backup_{timestamp}.db";
                    string destinationPath = Path.Combine(backupDir, fileName);

                    bool success = _dbService.BackupDatabaseAsync(destinationPath).GetAwaiter().GetResult();
                    if (!success) return null;
                    
                    _settingsService.UpdateLastBackupDate();
                    LoggerService.LogInfo($"Backup created successfully: {destinationPath}");
                    
                    // Cloud Sync Backup Process
                    string cloudPath = _settingsService.Current.CloudSyncPath;
                    if (!string.IsNullOrWhiteSpace(cloudPath))
                    {
                        try 
                        {
                            if (!Directory.Exists(cloudPath)) 
                                Directory.CreateDirectory(cloudPath);
                                
                            string cloudDestination = Path.Combine(cloudPath, fileName);
                            File.Copy(destinationPath, cloudDestination, true);
                            LoggerService.LogInfo($"Cloud Sync Backup created successfully at: {cloudDestination}");
                        }
                        catch (Exception cloudEx)
                        {
                            LoggerService.LogError("Failed to sync backup to cloud folder", cloudEx);
                        }
                    }

                    return destinationPath;
                }
                catch (Exception ex)
                {
                    LoggerService.LogError("Backup failed", ex);
                    return null;
                }
            });
        }

        /// <summary>
        /// Gets the absolute path to the current database file.
        /// </summary>
        private string GetDatabasePath()
        {
            // The DatabaseService stores the DB in LocalApplicationData
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appDataFolder, "Enjaz", "certificates.db");
        }

        /// <summary>
        /// Performs an automatic backup if enabled and due.
        /// </summary>
        public async Task AutoBackupAsync()
        {
            if (!_settingsService.Current.AutoBackupEnabled) return;

            // Simple logic: if more than AutoBackupIntervalHours passed since last backup
            var timeSinceLastBackup = DateTime.Now - _settingsService.Current.LastBackupDate;
            if (timeSinceLastBackup.TotalHours >= _settingsService.Current.AutoBackupIntervalHours)
            {
                LoggerService.LogInfo("Starting automatic backup...");
                await PerformBackupAsync();
            }
        }
    }
}
