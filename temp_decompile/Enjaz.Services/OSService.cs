using System;
using System.Diagnostics;
using System.IO;

namespace Enjaz.Services;

public class OSService : IOSService
{
	public void OpenFile(string path)
	{
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = path,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			LoggerService.LogError("فشل فتح الملف: " + path, ex);
		}
	}

	public void OpenDirectory(string path)
	{
		if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
		{
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = path,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			LoggerService.LogError("فشل فتح المجلد: " + path, ex);
		}
	}

	public void PrintFile(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo(path)
			{
				Verb = "print",
				UseShellExecute = true,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			});
			LoggerService.LogInfo("[OSService] Sent to print: " + path);
		}
		catch (Exception ex)
		{
			LoggerService.LogError("[OSService] Failed to print: " + path, ex);
		}
	}

	public void PrintFileTo(string path, string printerName)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo(path)
			{
				Verb = "printto",
				Arguments = "\"" + printerName + "\"",
				UseShellExecute = true,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			});
			LoggerService.LogInfo("[OSService] Sent to print (Printer: " + printerName + "): " + path);
		}
		catch (Exception ex)
		{
			LoggerService.LogError("[OSService] Failed to print to " + printerName + ": " + path, ex);
			PrintFile(path);
		}
	}
}
