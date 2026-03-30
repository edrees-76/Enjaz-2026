using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Enjaz.Models;

public class ReferralLetter
{
	public int Id { get; set; }

	[NotMapped]
	public int Sequence { get; set; }

	public DateTime GeneratedAt { get; set; }

	public string SenderName { get; set; } = string.Empty;

	public int CertificateCount { get; set; }

	public int SampleCount { get; set; }

	public string OutputPath { get; set; } = string.Empty;

	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }

	public string IncludedColumns { get; set; } = string.Empty;
}
