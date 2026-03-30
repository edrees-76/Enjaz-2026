using System;
using System.IO;

namespace Enjaz.Services;

public class LoggerService : ILoggerService
{
	private static readonly string LogDirectory;

	private static readonly object LockObj;

	private const int MaxLogAgeDays = 30;

	static LoggerService()
	{
		LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
		LockObj = new object();
		try
		{
			if (!Directory.Exists(LogDirectory))
			{
				Directory.CreateDirectory(LogDirectory);
			}
			CleanupOldLogs();
		}
		catch
		{
		}
	}

	private static string GetCurrentLogPath()
	{
		return Path.Combine(LogDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");
	}

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

	void ILoggerService.LogInfo(string message)
	{
		LogInfo(message);
	}

	void ILoggerService.LogError(string message, Exception? ex)
	{
		LogError(message, ex);
	}

	void ILoggerService.LogWarning(string message)
	{
		LogWarning(message);
	}

	private static void Log(string level, string message, Exception? ex = null)
	{
		try
		{
			lock (LockObj)
			{
				using StreamWriter streamWriter = File.AppendText(GetCurrentLogPath());
				streamWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}");
				if (ex != null)
				{
					streamWriter.WriteLine("Exception: " + ex.Message);
					streamWriter.WriteLine("Stack Trace: " + ex.StackTrace);
				}
				streamWriter.WriteLine(new string('-', 50));
			}
		}
		catch
		{
		}
	}

	private static void CleanupOldLogs()
	{
		try
		{
			string[] files = Directory.GetFiles(LogDirectory, "app_*.log");
			string[] array = files;
			foreach (string fileName in array)
			{
				FileInfo fileInfo = new FileInfo(fileName);
				if (fileInfo.CreationTime < DateTime.Now.AddDays(-30.0))
				{
					fileInfo.Delete();
				}
			}
		}
		catch
		{
		}
	}
}
