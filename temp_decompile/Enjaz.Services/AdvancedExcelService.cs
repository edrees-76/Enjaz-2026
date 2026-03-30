using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Enjaz.Models;
using Microsoft.Win32;

namespace Enjaz.Services;

public class AdvancedExcelService
{
	public bool ExportReport(List<Certificate> certificates, ReportSummary summary, List<string> selectedColumns, List<DistributionItem> topSuppliers, List<DistributionItem> topSenders, Dictionary<string, byte[]>? chartImages, string filePath)
	{
		try
		{
			using XLWorkbook xLWorkbook = new XLWorkbook();
			CreateSummarySheet(xLWorkbook, summary, topSuppliers, topSenders);
			if (chartImages != null && chartImages.Any())
			{
				CreateChartsSheet(xLWorkbook, chartImages);
			}
			CreateDetailsSheet(xLWorkbook, certificates, selectedColumns);
			xLWorkbook.SaveAs(filePath);
			LoggerService.LogInfo("تم تصدير التقرير بنجاح: " + filePath);
			return true;
		}
		catch (Exception ex)
		{
			LoggerService.LogError("فشل تصدير Excel", ex);
			return false;
		}
	}

	private void CreateSummarySheet(XLWorkbook workbook, ReportSummary summary, List<DistributionItem> topSuppliers, List<DistributionItem> topSenders)
	{
		IXLWorksheet iXLWorksheet = workbook.Worksheets.Add("الملخص");
		iXLWorksheet.RightToLeft = true;
		iXLWorksheet.Cell("A1").Value = "تقرير الفترة";
		iXLWorksheet.Cell("A1").Style.Font.Bold = true;
		iXLWorksheet.Cell("A1").Style.Font.FontSize = 18.0;
		iXLWorksheet.Range("A1:D1").Merge();
		iXLWorksheet.Cell("A2").Value = $"من {summary.StartDate:yyyy/MM/dd} إلى {summary.EndDate:yyyy/MM/dd}";
		iXLWorksheet.Range("A2:D2").Merge();
		iXLWorksheet.Cell("A4").Value = "الإحصائيات العامة";
		iXLWorksheet.Cell("A4").Style.Font.Bold = true;
		iXLWorksheet.Cell("A4").Style.Font.FontSize = 14.0;
		iXLWorksheet.Cell("A5").Value = "إجمالي الشهادات";
		iXLWorksheet.Cell("B5").Value = summary.TotalCertificates;
		iXLWorksheet.Cell("A6").Value = "إجمالي العينات";
		iXLWorksheet.Cell("B6").Value = summary.TotalSamples;
		iXLWorksheet.Cell("A7").Value = "إجمالي شهادات عينات بيئية";
		iXLWorksheet.Cell("B7").Value = summary.EnvironmentalCertificates;
		iXLWorksheet.Cell("A8").Value = "إجمالي عينات بيئية";
		iXLWorksheet.Cell("B8").Value = summary.EnvironmentalSamples;
		iXLWorksheet.Cell("A9").Value = "إجمالي شهادات عينات استهلاكية";
		iXLWorksheet.Cell("B9").Value = summary.ConsumableCertificates;
		iXLWorksheet.Cell("A10").Value = "إجمالي عينات استهلاكية";
		iXLWorksheet.Cell("B10").Value = summary.ConsumableSamples;
		iXLWorksheet.Cell("A12").Value = "أعلى الموردين";
		iXLWorksheet.Cell("A12").Style.Font.Bold = true;
		iXLWorksheet.Cell("A12").Style.Font.FontSize = 14.0;
		int num = 13;
		foreach (DistributionItem item in topSuppliers.Take(5))
		{
			iXLWorksheet.Cell($"A{num}").Value = item.Name;
			iXLWorksheet.Cell($"B{num}").Value = item.Count;
			num++;
		}
		num += 2;
		iXLWorksheet.Cell($"A{num}").Value = "أعلى الجهات المرسلة";
		iXLWorksheet.Cell($"A{num}").Style.Font.Bold = true;
		iXLWorksheet.Cell($"A{num}").Style.Font.FontSize = 14.0;
		num++;
		foreach (DistributionItem item2 in topSenders.Take(5))
		{
			iXLWorksheet.Cell($"A{num}").Value = item2.Name;
			iXLWorksheet.Cell($"B{num}").Value = item2.Count;
			num++;
		}
		iXLWorksheet.Column("A").Width = 30.0;
		iXLWorksheet.Column("B").Width = 15.0;
	}

	private void CreateDetailsSheet(XLWorkbook workbook, List<Certificate> certificates, List<string> selectedColumns)
	{
		IXLWorksheet iXLWorksheet = workbook.Worksheets.Add("التفاصيل");
		iXLWorksheet.RightToLeft = true;
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			{ "CertificateNumber", "رقم الشهادة" },
			{ "CertificateType", "نوع الشهادة" },
			{ "IssueDate", "تاريخ الإصدار" },
			{ "Sender", "الجهة المرسلة" },
			{ "Supplier", "المورد" },
			{ "Origin", "بلد المنشأ" },
			{ "NotificationNumber", "رقم الإخطار" },
			{ "DeclarationNumber", "رقم الإقرار الجمركي" },
			{ "FinancialReceiptNumber", "رقم الإيصال المالي" },
			{ "SampleCount", "عدد العينات" },
			{ "EnvironmentalSampleCount", "عدد عينات بيئية" },
			{ "ConsumableSampleCount", "عدد عينات استهلاكية" },
			{ "CreatedByName", "اسم المستخدم" }
		};
		if (selectedColumns == null || !selectedColumns.Any())
		{
			selectedColumns = dictionary.Keys.ToList();
		}
		int num = 1;
		foreach (string selectedColumn in selectedColumns)
		{
			if (dictionary.ContainsKey(selectedColumn))
			{
				iXLWorksheet.Cell(1, num).Value = dictionary[selectedColumn];
				iXLWorksheet.Cell(1, num).Style.Font.Bold = true;
				iXLWorksheet.Cell(1, num).Style.Fill.BackgroundColor = XLColor.FromHtml("#1B4F72");
				iXLWorksheet.Cell(1, num).Style.Font.FontColor = XLColor.White;
				num++;
			}
		}
		int num2 = 2;
		foreach (Certificate certificate in certificates)
		{
			num = 1;
			foreach (string selectedColumn2 in selectedColumns)
			{
				if (1 == 0)
				{
				}
				object obj = selectedColumn2 switch
				{
					"CertificateNumber" => certificate.CertificateNumber, 
					"CertificateType" => certificate.CertificateType, 
					"IssueDate" => certificate.IssueDate.ToString("yyyy/MM/dd"), 
					"Sender" => certificate.Sender, 
					"Supplier" => certificate.Supplier, 
					"Origin" => certificate.Origin, 
					"NotificationNumber" => certificate.NotificationNumber, 
					"DeclarationNumber" => certificate.DeclarationNumber, 
					"FinancialReceiptNumber" => certificate.FinancialReceiptNumber, 
					"SampleCount" => certificate.SampleCount, 
					"EnvironmentalSampleCount" => certificate.EnvironmentalSampleCount, 
					"ConsumableSampleCount" => certificate.ConsumableSampleCount, 
					"CreatedByName" => certificate.CreatedByName, 
					_ => "", 
				};
				if (1 == 0)
				{
				}
				iXLWorksheet.Cell(num2, num).Value = obj?.ToString() ?? "";
				num++;
			}
			if (num2 % 2 == 0)
			{
				iXLWorksheet.Row(num2).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");
			}
			num2++;
		}
		iXLWorksheet.Columns().AdjustToContents();
		iXLWorksheet.Range(1, 1, 1, num - 1).Style.Border.BottomBorder = XLBorderStyleValues.Thick;
	}

	private void CreateChartsSheet(XLWorkbook workbook, Dictionary<string, byte[]> chartImages)
	{
		IXLWorksheet iXLWorksheet = workbook.Worksheets.Add("الرسوم البيانية");
		iXLWorksheet.RightToLeft = true;
		int num = 2;
		int column = 2;
		foreach (KeyValuePair<string, byte[]> chartImage in chartImages)
		{
			iXLWorksheet.Cell(num, column).Value = chartImage.Key;
			iXLWorksheet.Cell(num, column).Style.Font.Bold = true;
			iXLWorksheet.Cell(num, column).Style.Font.FontSize = 14.0;
			using (MemoryStream stream = new MemoryStream(chartImage.Value))
			{
				IXLPicture iXLPicture = iXLWorksheet.AddPicture(stream).MoveTo(iXLWorksheet.Cell(num + 1, column)).Scale(0.8);
			}
			num += 25;
		}
		iXLWorksheet.Columns().AdjustToContents();
	}

	public string? ExportReportWithDialog(List<Certificate> certificates, ReportSummary summary, List<string> selectedColumns, List<DistributionItem> topSuppliers, List<DistributionItem> topSenders, Dictionary<string, byte[]>? chartImages = null)
	{
		string resultPath = null;
		((DispatcherObject)Application.Current).Dispatcher.Invoke((Action)delegate
		{
			try
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					FileName = $"تقرير_شامل_{summary.StartDate:yyyyMMdd}_{summary.EndDate:yyyyMMdd}",
					DefaultExt = ".xlsx",
					Filter = "Excel Workbook (.xlsx)|*.xlsx"
				};
				if (saveFileDialog.ShowDialog() == true && ExportReport(certificates, summary, selectedColumns, topSuppliers, topSenders, chartImages, saveFileDialog.FileName))
				{
					resultPath = saveFileDialog.FileName;
				}
			}
			catch (Exception ex)
			{
				LoggerService.LogError("Error in ExportReportWithDialog", ex);
				throw;
			}
		});
		return resultPath;
	}

	public async Task<string?> ExportSenderReportWithDialogAsync(List<Certificate> certificates, ReportSummary summary, List<string> selectedColumns, string senderName)
	{
		string resultPath = null;
		await ((DispatcherObject)Application.Current).Dispatcher.InvokeAsync((Action)delegate
		{
			try
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					FileName = $"تقرير_{senderName}_{summary.StartDate:yyyyMMdd}",
					DefaultExt = ".xlsx",
					Filter = "Excel Workbook (.xlsx)|*.xlsx"
				};
				if (saveFileDialog.ShowDialog() == true)
				{
					using (XLWorkbook xLWorkbook = new XLWorkbook())
					{
						IXLWorksheet iXLWorksheet = xLWorkbook.Worksheets.Add("تقرير الجهة");
						iXLWorksheet.RightToLeft = true;
						iXLWorksheet.Cell("A1").Value = "تقرير الجهة المرسلة: " + senderName;
						iXLWorksheet.Cell("A1").Style.Font.Bold = true;
						iXLWorksheet.Cell("A1").Style.Font.FontSize = 18.0;
						iXLWorksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
						iXLWorksheet.Range("A1:F1").Merge();
						iXLWorksheet.Cell("A2").Value = $"من {summary.StartDate:yyyy/MM/dd} إلى {summary.EndDate:yyyy/MM/dd}";
						iXLWorksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
						iXLWorksheet.Range("A2:F2").Merge();
						int num = 4;
						iXLWorksheet.Range(num, 1, num + 1, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
						iXLWorksheet.Cell(num, 1).Value = "إجمالي الشهادات";
						iXLWorksheet.Cell(num + 1, 1).Value = summary.TotalCertificates;
						iXLWorksheet.Cell(num + 1, 1).Style.Font.FontColor = XLColor.FromHtml("#1B4F72");
						iXLWorksheet.Cell(num + 1, 1).Style.Font.FontSize = 16.0;
						iXLWorksheet.Cell(num + 1, 1).Style.Font.Bold = true;
						iXLWorksheet.Range(num, 3, num + 1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
						iXLWorksheet.Cell(num, 3).Value = "إجمالي شهادات عينات بيئية";
						iXLWorksheet.Cell(num + 1, 3).Value = summary.EnvironmentalCertificates;
						iXLWorksheet.Range(num, 5, num + 1, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
						iXLWorksheet.Cell(num, 5).Value = "إجمالي شهادات عينات استهلاكية";
						iXLWorksheet.Cell(num + 1, 5).Value = summary.ConsumableCertificates;
						int num2 = 7;
						iXLWorksheet.Range(num2, 1, num2 + 1, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
						iXLWorksheet.Cell(num2, 1).Value = "إجمالي العينات";
						iXLWorksheet.Cell(num2 + 1, 1).Value = summary.TotalSamples;
						iXLWorksheet.Cell(num2 + 1, 1).Style.Font.FontColor = XLColor.FromHtml("#1B4F72");
						iXLWorksheet.Cell(num2 + 1, 1).Style.Font.FontSize = 16.0;
						iXLWorksheet.Cell(num2 + 1, 1).Style.Font.Bold = true;
						iXLWorksheet.Range(num2, 3, num2 + 1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
						iXLWorksheet.Cell(num2, 3).Value = "إجمالي عينات بيئية";
						iXLWorksheet.Cell(num2 + 1, 3).Value = summary.EnvironmentalSamples;
						iXLWorksheet.Range(num2, 5, num2 + 1, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
						iXLWorksheet.Cell(num2, 5).Value = "إجمالي عينات استهلاكية";
						iXLWorksheet.Cell(num2 + 1, 5).Value = summary.ConsumableSamples;
						IXLRange iXLRange = iXLWorksheet.Range(num, 1, num2 + 1, 6);
						iXLRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
						iXLRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
						iXLRange.Style.Font.Bold = true;
						int num3 = 10;
						Dictionary<string, string> dictionary = new Dictionary<string, string>
						{
							{ "CertificateNumber", "رقم الشهادة" },
							{ "CertificateType", "نوع الشهادة" },
							{ "IssueDate", "تاريخ الإصدار" },
							{ "Sender", "الجهة المرسلة" },
							{ "Supplier", "المورد" },
							{ "Origin", "بلد المنشأ" },
							{ "NotificationNumber", "رقم الإخطار" },
							{ "DeclarationNumber", "رقم الإقرار الجمركي" },
							{ "FinancialReceiptNumber", "رقم الإيصال المالي" },
							{ "SampleCount", "عدد العينات" },
							{ "EnvironmentalSampleCount", "عدد عينات بيئية" },
							{ "ConsumableSampleCount", "عدد عينات استهلاكية" },
							{ "CreatedByName", "اسم المستخدم" }
						};
						int num4 = 1;
						foreach (string selectedColumn in selectedColumns)
						{
							if (dictionary.ContainsKey(selectedColumn))
							{
								IXLCell iXLCell = iXLWorksheet.Cell(num3, num4);
								iXLCell.Value = dictionary[selectedColumn];
								iXLCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B4F72");
								iXLCell.Style.Font.FontColor = XLColor.White;
								iXLCell.Style.Font.Bold = true;
								iXLCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
								num4++;
							}
						}
						int num5 = num3 + 1;
						foreach (Certificate certificate in certificates)
						{
							num4 = 1;
							foreach (string selectedColumn2 in selectedColumns)
							{
								IXLCell iXLCell2 = iXLWorksheet.Cell(num5, num4);
								IXLCell iXLCell3 = iXLCell2;
								if (1 == 0)
								{
								}
								XLCellValue value = selectedColumn2 switch
								{
									"CertificateNumber" => certificate.CertificateNumber, 
									"CertificateType" => certificate.CertificateType ?? "", 
									"IssueDate" => certificate.IssueDate.ToString("yyyy/MM/dd"), 
									"Sender" => certificate.Sender ?? "", 
									"Supplier" => certificate.Supplier ?? "", 
									"Origin" => certificate.Origin ?? "", 
									"NotificationNumber" => certificate.NotificationNumber ?? "", 
									"DeclarationNumber" => certificate.DeclarationNumber ?? "", 
									"FinancialReceiptNumber" => certificate.FinancialReceiptNumber ?? "", 
									"SampleCount" => certificate.SampleCount, 
									"EnvironmentalSampleCount" => certificate.EnvironmentalSampleCount, 
									"ConsumableSampleCount" => certificate.ConsumableSampleCount, 
									"CreatedByName" => certificate.CreatedByName ?? "", 
									_ => "", 
								};
								if (1 == 0)
								{
								}
								iXLCell3.Value = value;
								iXLCell2.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
								iXLCell2.Style.Border.OutsideBorderColor = XLColor.LightGray;
								iXLCell2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
								if (num5 % 2 == 0)
								{
									iXLCell2.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");
								}
								num4++;
							}
							num5++;
						}
						iXLWorksheet.Columns().AdjustToContents();
						xLWorkbook.SaveAs(saveFileDialog.FileName);
						resultPath = saveFileDialog.FileName;
						LoggerService.LogInfo("تم تصدير تقرير الجهة المطور إلى Excel: " + resultPath);
						return;
					}
				}
			}
			catch (Exception ex)
			{
				LoggerService.LogError("Error in ExportSenderReportWithDialogAsync", ex);
				throw;
			}
		});
		return resultPath;
	}

	public async Task<string?> ExportCertificatesWithDialogAsync(List<Certificate> certificates, List<string> selectedColumns)
	{
		string resultPath = null;
		await ((DispatcherObject)Application.Current).Dispatcher.InvokeAsync((Action)delegate
		{
			try
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					FileName = $"تقرير_بيانات_{DateTime.Now:yyyyMMdd_HHmm}",
					DefaultExt = ".xlsx",
					Filter = "Excel Workbook (.xlsx)|*.xlsx"
				};
				if (saveFileDialog.ShowDialog() == true)
				{
					using (XLWorkbook xLWorkbook = new XLWorkbook())
					{
						CreateDetailsSheet(xLWorkbook, certificates, selectedColumns);
						xLWorkbook.SaveAs(saveFileDialog.FileName);
						resultPath = saveFileDialog.FileName;
						LoggerService.LogInfo("تم تصدير البيانات إلى Excel: " + resultPath);
						return;
					}
				}
			}
			catch (Exception ex)
			{
				LoggerService.LogError("Error in ExportCertificatesWithDialogAsync", ex);
				throw;
			}
		});
		return resultPath;
	}
}
