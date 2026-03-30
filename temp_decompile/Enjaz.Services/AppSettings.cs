using System;
using System.IO;

namespace Enjaz.Services;

public class AppSettings
{
	public string BackupPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Enjaz_Backups");

	public bool AutoBackupEnabled { get; set; } = true;

	public int AutoBackupIntervalHours { get; set; } = 24;

	public DateTime LastBackupDate { get; set; } = DateTime.MinValue;

	public string RememberedUsername { get; set; } = string.Empty;

	public bool RememberMeEnabled { get; set; } = false;

	public string CurrentTheme { get; set; } = "DefaultBlue";

	public bool AppIsDarkMode { get; set; } = true;

	public bool LoginIsDarkMode { get; set; } = true;

	public double FontSizeScale { get; set; } = 1.0;

	public bool EnableAlerts { get; set; } = true;

	public string? DatabasePath { get; set; } = null;
}
