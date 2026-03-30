using System;

namespace Enjaz.Models;

public class AuditLog
{
	public int Id { get; set; }

	public int? UserId { get; set; }

	public string UserName { get; set; } = string.Empty;

	public string Action { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public DateTime Timestamp { get; set; } = DateTime.Now;

	public int? ReferenceId { get; set; }

	public string TimeFormatted => Timestamp.ToString("yyyy/MM/dd HH:mm:ss");
}
