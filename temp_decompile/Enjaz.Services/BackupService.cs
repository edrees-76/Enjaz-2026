using System;
using System.IO;
using System.Threading.Tasks;

namespace Enjaz.Services;

public class BackupService
{
	private readonly DatabaseService _dbService;

	private readonly SettingsService _settingsService;

	public BackupService(DatabaseService dbService, SettingsService settingsService)
	{
		_dbService = dbService;
		_settingsService = settingsService;
	}

	public async Task<string?> PerformBackupAsync(string? targetDirectory = null)
	{
		return await Task.Run(delegate
		{
			try
			{
				string databasePath = GetDatabasePath();
				if (!File.Exists(databasePath))
				{
					LoggerService.LogError("Backup failed: Source database file not found.", new FileNotFoundException(databasePath));
					return (string)null;
				}
				string text = targetDirectory ?? _settingsService.Current.BackupPath;
				if (string.IsNullOrWhiteSpace(text))
				{
					text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Enjaz_Backups");
				}
				if (!Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
				}
				string text2 = DateTime.Now.ToString("yyyyMMdd_HHmmss");
				string path = "certificates_backup_" + text2 + ".db";
				string text3 = Path.Combine(text, path);
				if (!_dbService.BackupDatabaseAsync(text3).GetAwaiter().GetResult())
				{
					return (string)null;
				}
				_settingsService.UpdateLastBackupDate();
				LoggerService.LogInfo("Backup created successfully: " + text3);
				return text3;
			}
			catch (Exception ex)
			{
				LoggerService.LogError("Backup failed", ex);
				return (string)null;
			}
		});
	}

	private string GetDatabasePath()
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		return Path.Combine(folderPath, "Enjaz", "certificates.db");
	}

	public async Task AutoBackupAsync()
	{
		if (_settingsService.Current.AutoBackupEnabled && (DateTime.Now - _settingsService.Current.LastBackupDate).TotalHours >= (double)_settingsService.Current.AutoBackupIntervalHours)
		{
			LoggerService.LogInfo("Starting automatic backup...");
			await PerformBackupAsync();
		}
	}
}
