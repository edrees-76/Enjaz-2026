using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Enjaz.Services
{
    public class AppSettings
    {
        public string BackupPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Enjaz_Backups");
        public string CloudSyncPath { get; set; } = string.Empty;
        public bool AutoBackupEnabled { get; set; } = true;
        public int AutoBackupIntervalHours { get; set; } = 24;
        public DateTime LastBackupDate { get; set; } = DateTime.MinValue;
        public string RememberedUsername { get; set; } = string.Empty;
        public bool RememberMeEnabled { get; set; } = false;
        
        // New Appearance Settings
        public string CurrentTheme { get; set; } = "DefaultBlue";
        public bool AppIsDarkMode { get; set; } = true;
        public bool LoginIsDarkMode { get; set; } = true;
        public double FontSizeScale { get; set; } = 1.0;
        public bool EnableAlerts { get; set; } = true;
        
        // Network / Data Location
        public string? DatabasePath { get; set; } = null; // null means use default local path
    }

    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private AppSettings _currentSettings;

        public AppSettings Current => _currentSettings;

        public SettingsService()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appSpecificFolder = Path.Combine(appDataFolder, "Enjaz");
            
            if (!Directory.Exists(appSpecificFolder))
            {
                Directory.CreateDirectory(appSpecificFolder);
            }

            _settingsFilePath = Path.Combine(appSpecificFolder, "settings.json");
            _currentSettings = LoadSettings();
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    byte[] fileBytes = File.ReadAllBytes(_settingsFilePath);
                    string json;
                    
                    try
                    {
                        // Try decrypting (new encrypted format)
                        byte[] decrypted = ProtectedData.Unprotect(fileBytes, null, DataProtectionScope.CurrentUser);
                        json = Encoding.UTF8.GetString(decrypted);
                    }
                    catch (CryptographicException)
                    {
                        // Fallback: read as plain JSON (legacy format) and re-save encrypted
                        json = Encoding.UTF8.GetString(fileBytes);
                        LoggerService.LogInfo("Migrating settings to encrypted format");
                    }
                    
                    var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    
                    // Re-save in encrypted format if it was plain
                    SaveSettings(settings);
                    return settings;
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to load settings", ex);
            }
            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                _currentSettings = settings;
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                byte[] encrypted = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_settingsFilePath, encrypted);
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to save settings", ex);
            }
        }

        public void UpdateLastBackupDate()
        {
            _currentSettings.LastBackupDate = DateTime.Now;
            SaveSettings(_currentSettings);
        }
    }
}

