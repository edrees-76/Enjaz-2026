using System;
using System.Collections.ObjectModel;

namespace Enjaz.Models;

public class SampleReception
{
	private string _certificateType = string.Empty;

	public int Id { get; set; }

	public int Sequence { get; set; }

	public string AnalysisRequestNumber { get; set; } = string.Empty;

	public string? NotificationNumber { get; set; }

	public string? DeclarationNumber { get; set; }

	public string? Supplier { get; set; }

	public string? Sender { get; set; }

	public string? Origin { get; set; }

	public string? PolicyNumber { get; set; }

	public string? FinancialReceiptNumber { get; set; }

	public string CertificateType
	{
		get
		{
			return _certificateType;
		}
		set
		{
			_certificateType = value?.Replace("شهادة بيئية", "عينات بيئية").Replace("شهادة استهلاكية", "عينات استهلاكية") ?? string.Empty;
		}
	}

	public DateTime Date { get; set; } = DateTime.Now;

	public string Status { get; set; } = "في انتظار إصدار شهادة";

	public int CreatedBy { get; set; }

	public string? CreatedByName { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	public int? UpdatedBy { get; set; }

	public string? UpdatedByName { get; set; }

	public DateTime? UpdatedAt { get; set; }

	public int SampleCount => Samples.Count;

	public ObservableCollection<ReceptionSample> Samples { get; set; } = new ObservableCollection<ReceptionSample>();
}
