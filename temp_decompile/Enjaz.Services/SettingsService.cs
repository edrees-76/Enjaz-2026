using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Enjaz.Services;

public class SettingsService
{
	private readonly string _settingsFilePath;

	private AppSettings _currentSettings;

	public AppSettings Current => _currentSettings;

	public SettingsService()
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string text = Path.Combine(folderPath, "Enjaz");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		_settingsFilePath = Path.Combine(text, "settings.json");
		_currentSettings = LoadSettings();
	}

	public AppSettings LoadSettings()
	{
		try
		{
			if (File.Exists(_settingsFilePath))
			{
				byte[] array = File.ReadAllBytes(_settingsFilePath);
				string json;
				try
				{
					byte[] bytes = ProtectedData.Unprotect(array, null, DataProtectionScope.CurrentUser);
					json = Encoding.UTF8.GetString(bytes);
				}
				catch (CryptographicException)
				{
					json = Encoding.UTF8.GetString(array);
					LoggerService.LogInfo("Migrating settings to encrypted format");
				}
				AppSettings appSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
				SaveSettings(appSettings);
				return appSettings;
			}
		}
		catch (Exception ex2)
		{
			LoggerService.LogError("Failed to load settings", ex2);
		}
		return new AppSettings();
	}

	public void SaveSettings(AppSettings settings)
	{
		try
		{
			_currentSettings = settings;
			string s = JsonSerializer.Serialize(settings, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			byte[] bytes2 = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
			File.WriteAllBytes(_settingsFilePath, bytes2);
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
