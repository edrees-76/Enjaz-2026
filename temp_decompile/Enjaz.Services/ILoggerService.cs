using System;

namespace Enjaz.Services;

public interface ILoggerService
{
	void LogInfo(string message);

	void LogError(string message, Exception? ex = null);

	void LogWarning(string message);
}
