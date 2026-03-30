using System;
using System.Collections.ObjectModel;

namespace Enjaz.Models;

public class Certificate
{
	private string _certificateType = string.Empty;

	public int Id { get; set; }

	public int Sequence { get; set; }

	public string CertificateNumber { get; set; } = string.Empty;

	public int? ReceptionId { get; set; }

	public string RecipientName { get; set; } = string.Empty;

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

	public string Description { get; set; } = string.Empty;

	public DateTime IssueDate { get; set; } = DateTime.Now;

	public DateTime? ExpiryDate { get; set; }

	public int CreatedBy { get; set; }

	public string? CreatedByName { get; set; }

	public string IssuingAuthority { get; set; } = string.Empty;

	public string? AnalysisType { get; set; }

	public string? Sender { get; set; }

	public string? Supplier { get; set; }

	public string? Origin { get; set; }

	public string? DeclarationNumber { get; set; }

	public string? PolicyNumber { get; set; }

	public string? NotificationNumber { get; set; }

	public string? FinancialReceiptNumber { get; set; }

	public string? SpecialistName { get; set; }

	public string? SectionHeadName { get; set; }

	public string? ManagerName { get; set; }

	public string? Notes { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	public int? UpdatedBy { get; set; }

	public string? UpdatedByName { get; set; }

	public DateTime? UpdatedAt { get; set; }

	public int SampleCount { get; set; }

	public int EnvironmentalSampleCount
	{
		get
		{
			if (!string.IsNullOrEmpty(CertificateType) && (CertificateType.Contains("بيئية") || CertificateType.Contains("بيييه") || CertificateType.Contains("بيئيه")))
			{
				return SampleCount;
			}
			return 0;
		}
	}

	public int ConsumableSampleCount
	{
		get
		{
			if (!string.IsNullOrEmpty(CertificateType) && CertificateType.Contains("استهلاكية"))
			{
				return SampleCount;
			}
			return 0;
		}
	}

	public ObservableCollection<Sample> Samples { get; set; } = new ObservableCollection<Sample>();
}
