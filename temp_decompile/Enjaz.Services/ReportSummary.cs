using System;

namespace Enjaz.Services;

public class ReportSummary
{
	public int TotalCertificates { get; set; }

	public int TotalSamples { get; set; }

	public int EnvironmentalCertificates { get; set; }

	public int ConsumableCertificates { get; set; }

	public int EnvironmentalSamples { get; set; }

	public int ConsumableSamples { get; set; }

	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }
}
