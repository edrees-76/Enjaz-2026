using System;

namespace Enjaz.Services;

public class AppAlert
{
	public AlertType Type { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Message { get; set; } = string.Empty;

	public int ReferenceId { get; set; }

	public DateTime Timestamp { get; set; }
}
