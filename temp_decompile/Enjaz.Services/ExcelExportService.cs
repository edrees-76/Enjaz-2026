using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Enjaz.Models;

namespace Enjaz.Services;

public class ExcelExportService
{
	public bool ExportCertificatesToCsv(IEnumerable<Certificate> certificates, string filePath)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append('\ufeff');
			stringBuilder.AppendLine(string.Join(",", "رقم الشهادة", "اسم المستلم", "نوع الشهادة", "تاريخ الإصدار", "جهة الإصدار", "المرسل", "المورد", "بلد المنشأ", "رقم الإقرار الجمركي", "رقم البوليصة", "رقم الإيصال المالي", "اسم الأخصائي", "رئيس القسم", "المدير", "عدد العينات", "ملاحظات"));
			foreach (Certificate certificate in certificates)
			{
				stringBuilder.AppendLine(string.Join(",", EscapeCsvField(certificate.CertificateNumber), EscapeCsvField(certificate.RecipientName), EscapeCsvField(certificate.CertificateType), certificate.IssueDate.ToString("yyyy-MM-dd"), EscapeCsvField(certificate.IssuingAuthority ?? ""), EscapeCsvField(certificate.Sender ?? ""), EscapeCsvField(certificate.Supplier ?? ""), EscapeCsvField(certificate.Origin ?? ""), EscapeCsvField(certificate.DeclarationNumber ?? ""), EscapeCsvField(certificate.PolicyNumber ?? ""), EscapeCsvField(certificate.FinancialReceiptNumber ?? ""), EscapeCsvField(certificate.SpecialistName ?? ""), EscapeCsvField(certificate.SectionHeadName ?? ""), EscapeCsvField(certificate.ManagerName ?? ""), certificate.SampleCount.ToString(), EscapeCsvField(certificate.Notes ?? "")));
			}
			File.WriteAllText(filePath, stringBuilder.ToString(), Encoding.UTF8);
			LoggerService.LogInfo("Exported certificates to: " + filePath);
			return true;
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Excel export failed", ex);
			return false;
		}
	}

	public bool ExportSamplesToCsv(IEnumerable<Sample> samples, string filePath)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append('\ufeff');
			stringBuilder.AppendLine(string.Join(",", "معرف العينة", "رقم الشهادة", "رقم العينة", "النتيجة", "تاريخ القياس", "K40", "Ra226", "Th232", "Cs137", "Ra"));
			foreach (Sample sample in samples)
			{
				stringBuilder.AppendLine(string.Join(",", sample.Id.ToString(), sample.CertificateId.ToString(), EscapeCsvField(sample.SampleNumber ?? ""), EscapeCsvField(sample.Result ?? ""), sample.MeasurementDate.ToString("yyyy-MM-dd"), sample.IsotopeK40 ?? "", sample.IsotopeRa226 ?? "", sample.IsotopeTh232 ?? "", sample.IsotopeCs137 ?? "", sample.IsotopeRa ?? ""));
			}
			File.WriteAllText(filePath, stringBuilder.ToString(), Encoding.UTF8);
			LoggerService.LogInfo("Exported samples to: " + filePath);
			return true;
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Samples export failed", ex);
			return false;
		}
	}

	private static string EscapeCsvField(string field)
	{
		if (string.IsNullOrEmpty(field))
		{
			return "\"\"";
		}
		if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
		{
			return "\"" + field.Replace("\"", "\"\"") + "\"";
		}
		return "\"" + field + "\"";
	}
}
