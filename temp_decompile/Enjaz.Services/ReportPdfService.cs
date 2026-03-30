using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Enjaz.Models;
using Microsoft.Win32;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Enjaz.Services;

public class ReportPdfService
{
	public ReportPdfService()
	{
		Settings.License = LicenseType.Community;
	}

	public bool GenerateReportPdf(List<Certificate> certificates, ReportSummary summary, List<string> selectedColumns, List<DistributionItem> topSuppliers, List<DistributionItem> topSenders, Dictionary<string, byte[]>? chartImages, string filePath)
	{
		try
		{
			Dictionary<string, string> columnHeaders = new Dictionary<string, string>
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
				selectedColumns = new List<string> { "CertificateNumber", "CertificateType", "IssueDate", "Sender", "Supplier", "SampleCount" };
			}
			Document document = Document.Create(delegate(IDocumentContainer container)
			{
				container.Page(delegate(PageDescriptor page)
				{
					page.Size(PageSizes.A4.Landscape());
					page.MarginVertical(20f);
					page.MarginHorizontal(25f);
					page.DefaultTextStyle((TextStyle x) => x.FontFamily("Arial").FontSize(10f));
					page.ContentFromRightToLeft();
					page.Header().Column(delegate(ColumnDescriptor col)
					{
						col.Item().AlignCenter().Text("تقرير الفترة الزمنية")
							.FontSize(22f)
							.Bold();
						col.Item().AlignCenter().Text($"من {summary.StartDate:yyyy/MM/dd} إلى {summary.EndDate:yyyy/MM/dd}")
							.FontSize(14f);
						col.Item().Height(15f);
					});
					page.Content().Column(delegate(ColumnDescriptor column)
					{
						column.Item().PaddingBottom(15f).Table(delegate(TableDescriptor table)
						{
							table.ColumnsDefinition(delegate(TableColumnsDefinitionDescriptor cols)
							{
								cols.RelativeColumn();
								cols.RelativeColumn();
							});
							table.Cell().Border(1f).BorderColor(Colors.Grey.Lighten2)
								.Padding(10f)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي الشهادات").Bold();
									c.Item().Text(summary.TotalCertificates.ToString()).FontSize(18f)
										.Bold()
										.FontColor(Colors.Blue.Medium);
								});
							table.Cell().Border(1f).BorderColor(Colors.Grey.Lighten2)
								.Padding(10f)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي العينات").Bold();
									c.Item().Text(summary.TotalSamples.ToString()).FontSize(18f)
										.Bold()
										.FontColor(Colors.Blue.Medium);
								});
							table.Cell().Border(1f).BorderColor(Colors.Grey.Lighten2)
								.Padding(10f)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي شهادات عينات بيئية").Bold();
									c.Item().Text(summary.EnvironmentalCertificates.ToString()).FontSize(16f)
										.Bold();
								});
							table.Cell().Border(1f).BorderColor(Colors.Grey.Lighten2)
								.Padding(10f)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي عينات بيئية").Bold();
									c.Item().Text(summary.EnvironmentalSamples.ToString()).FontSize(16f)
										.Bold();
								});
							table.Cell().Border(1f).BorderColor(Colors.Grey.Lighten2)
								.Padding(10f)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي شهادات عينات استهلاكية").Bold();
									c.Item().Text(summary.ConsumableCertificates.ToString()).FontSize(16f)
										.Bold();
								});
							table.Cell().Border(1f).BorderColor(Colors.Grey.Lighten2)
								.Padding(10f)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي عينات استهلاكية").Bold();
									c.Item().Text(summary.ConsumableSamples.ToString()).FontSize(16f)
										.Bold();
								});
						});
						column.Item().PaddingTop(10f).Table(delegate(TableDescriptor table)
						{
							table.ColumnsDefinition(delegate(TableColumnsDefinitionDescriptor columns)
							{
								foreach (string selectedColumn in selectedColumns)
								{
									columns.RelativeColumn();
								}
							});
							table.Header(delegate(TableCellDescriptor header)
							{
								foreach (string selectedColumn2 in selectedColumns)
								{
									if (columnHeaders.ContainsKey(selectedColumn2))
									{
										header.Cell().Background("#1B4F72").Padding(5f)
											.Text(columnHeaders[selectedColumn2])
											.FontColor(Colors.White)
											.Bold()
											.FontSize(9f);
									}
								}
							});
							int num = 0;
							foreach (Certificate certificate in certificates)
							{
								Color color = ((num % 2 == 0) ? Colors.White : Colors.Grey.Lighten4);
								foreach (string selectedColumn3 in selectedColumns)
								{
									if (1 == 0)
									{
									}
									string text = selectedColumn3 switch
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
										"SampleCount" => certificate.SampleCount.ToString(), 
										"EnvironmentalSampleCount" => certificate.EnvironmentalSampleCount.ToString(), 
										"ConsumableSampleCount" => certificate.ConsumableSampleCount.ToString(), 
										"CreatedByName" => certificate.CreatedByName ?? "", 
										_ => "", 
									};
									if (1 == 0)
									{
									}
									string text2 = text;
									table.Cell().Background(color).Border(0.5f)
										.BorderColor(Colors.Grey.Lighten2)
										.Padding(4f)
										.Text(text2)
										.FontSize(8f);
								}
								num++;
							}
						});
						if (chartImages != null && chartImages.Any())
						{
							List<KeyValuePair<string, byte[]>> charts = chartImages.ToList();
							int i;
							for (i = 0; i < charts.Count; i += 2)
							{
								column.Item().PageBreak();
								column.Item().PaddingBottom(20f).AlignCenter()
									.Text("تحليل البيانات (الرسوم البيانية)")
									.FontSize(18f)
									.Bold()
									.FontColor("#1B4F72");
								column.Item().Row(delegate(RowDescriptor row)
								{
									row.Spacing(20f);
									KeyValuePair<string, byte[]> chart1 = charts[i];
									row.RelativeItem().Border(1f).BorderColor(Colors.Grey.Lighten2)
										.Padding(10f)
										.Column(delegate(ColumnDescriptor c)
										{
											c.Item().PaddingBottom(10f).AlignCenter()
												.Text(chart1.Key)
												.FontSize(14f)
												.Bold();
											c.Item().Height(380f).Image(chart1.Value)
												.FitArea();
										});
									if (i + 1 < charts.Count)
									{
										KeyValuePair<string, byte[]> chart2 = charts[i + 1];
										row.RelativeItem().Border(1f).BorderColor(Colors.Grey.Lighten2)
											.Padding(10f)
											.Column(delegate(ColumnDescriptor c)
											{
												c.Item().PaddingBottom(10f).AlignCenter()
													.Text(chart2.Key)
													.FontSize(14f)
													.Bold();
												c.Item().Height(380f).Image(chart2.Value)
													.FitArea();
											});
									}
									else
									{
										row.RelativeItem().Column(delegate(ColumnDescriptor c)
										{
											c.Item().Text("");
										});
									}
								});
							}
						}
					});
					page.Footer().AlignCenter().Text(delegate(TextDescriptor text)
					{
						text.Span("صفحة ");
						text.CurrentPageNumber();
						text.Span(" من ");
						text.TotalPages();
					});
				});
			});
			document.GeneratePdf(filePath);
			LoggerService.LogInfo("تم توليد تقرير PDF: " + filePath);
			return true;
		}
		catch (Exception ex)
		{
			LoggerService.LogError("فشل توليد تقرير PDF", ex);
			throw new Exception("فشل إنشاء التقرير: " + ex.Message, ex);
		}
	}

	public bool GenerateSenderReportPdf(List<Certificate> certificates, ReportSummary summary, List<string> selectedColumns, string senderName, string filePath)
	{
		try
		{
			Dictionary<string, string> columnHeaders = new Dictionary<string, string>
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
			Document document = Document.Create(delegate(IDocumentContainer container)
			{
				container.Page(delegate(PageDescriptor page)
				{
					page.Size(PageSizes.A4.Landscape());
					page.MarginVertical(20f);
					page.MarginHorizontal(25f);
					page.DefaultTextStyle((TextStyle x) => x.FontFamily("Arial").FontSize(10f));
					page.ContentFromRightToLeft();
					page.Header().Column(delegate(ColumnDescriptor col)
					{
						col.Item().AlignCenter().Text("تقرير الجهة المرسلة: " + senderName)
							.FontSize(22f)
							.Bold();
						col.Item().AlignCenter().Text($"من {summary.StartDate:yyyy/MM/dd} إلى {summary.EndDate:yyyy/MM/dd}")
							.FontSize(14f);
						col.Item().Height(15f);
					});
					page.Content().Column(delegate(ColumnDescriptor column)
					{
						column.Item().PaddingBottom(15f).Table(delegate(TableDescriptor table)
						{
							table.ColumnsDefinition(delegate(TableColumnsDefinitionDescriptor cols)
							{
								cols.RelativeColumn();
								cols.RelativeColumn();
								cols.RelativeColumn();
							});
							table.Cell().Padding(5f).Border(1f)
								.BorderColor(Colors.Grey.Lighten2)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي الشهادات").Bold();
									c.Item().Text(summary.TotalCertificates.ToString()).FontSize(18f)
										.Bold()
										.FontColor(Colors.Blue.Medium);
								});
							table.Cell().Padding(5f).Border(1f)
								.BorderColor(Colors.Grey.Lighten2)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي شهادات عينات بيئية").Bold();
									c.Item().Text(summary.EnvironmentalCertificates.ToString()).FontSize(16f)
										.Bold();
								});
							table.Cell().Padding(5f).Border(1f)
								.BorderColor(Colors.Grey.Lighten2)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي شهادات عينات استهلاكية").Bold();
									c.Item().Text(summary.ConsumableCertificates.ToString()).FontSize(16f)
										.Bold();
								});
							table.Cell().Padding(5f).Border(1f)
								.BorderColor(Colors.Grey.Lighten2)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي العينات").Bold();
									c.Item().Text(summary.TotalSamples.ToString()).FontSize(18f)
										.Bold()
										.FontColor(Colors.Blue.Medium);
								});
							table.Cell().Padding(5f).Border(1f)
								.BorderColor(Colors.Grey.Lighten2)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي عينات بيئية").Bold();
									c.Item().Text(summary.EnvironmentalSamples.ToString()).FontSize(16f)
										.Bold();
								});
							table.Cell().Padding(5f).Border(1f)
								.BorderColor(Colors.Grey.Lighten2)
								.Column(delegate(ColumnDescriptor c)
								{
									c.Item().Text("إجمالي عينات استهلاكية").Bold();
									c.Item().Text(summary.ConsumableSamples.ToString()).FontSize(16f)
										.Bold();
								});
						});
						column.Item().PaddingTop(10f).Table(delegate(TableDescriptor table)
						{
							table.ColumnsDefinition(delegate(TableColumnsDefinitionDescriptor columnsDef)
							{
								foreach (string selectedColumn in selectedColumns)
								{
									columnsDef.RelativeColumn();
								}
							});
							table.Header(delegate(TableCellDescriptor header)
							{
								foreach (string selectedColumn2 in selectedColumns)
								{
									if (columnHeaders.ContainsKey(selectedColumn2))
									{
										header.Cell().Background("#1B4F72").Padding(5f)
											.Text(columnHeaders[selectedColumn2])
											.FontColor(Colors.White)
											.Bold()
											.FontSize(9f);
									}
								}
							});
							int num = 0;
							foreach (Certificate certificate in certificates)
							{
								Color color = ((num % 2 == 0) ? Colors.White : Colors.Grey.Lighten4);
								foreach (string selectedColumn3 in selectedColumns)
								{
									if (1 == 0)
									{
									}
									string text = selectedColumn3 switch
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
										"SampleCount" => certificate.SampleCount.ToString(), 
										"EnvironmentalSampleCount" => certificate.EnvironmentalSampleCount.ToString(), 
										"ConsumableSampleCount" => certificate.ConsumableSampleCount.ToString(), 
										"CreatedByName" => certificate.CreatedByName ?? "", 
										_ => "", 
									};
									if (1 == 0)
									{
									}
									string text2 = text;
									table.Cell().Background(color).Border(0.5f)
										.BorderColor(Colors.Grey.Lighten2)
										.Padding(4f)
										.Text(text2)
										.FontSize(8f);
								}
								num++;
							}
						});
					});
					page.Footer().AlignCenter().Text(delegate(TextDescriptor text)
					{
						text.Span("صفحة ");
						text.CurrentPageNumber();
						text.Span(" من ");
						text.TotalPages();
					});
				});
			});
			document.GeneratePdf(filePath);
			return true;
		}
		catch (Exception ex)
		{
			LoggerService.LogError("فشل توليد تقرير الجهة PDF", ex);
			throw new Exception("فشل إنشاء تقرير الجهة: " + ex.Message, ex);
		}
	}

	public string? GenerateSenderReportWithDialog(List<Certificate> certificates, ReportSummary summary, List<string> selectedColumns, string senderName)
	{
		string resultPath = null;
		((DispatcherObject)Application.Current).Dispatcher.Invoke((Action)delegate
		{
			try
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					FileName = $"تقرير_{senderName}_{summary.StartDate:yyyyMMdd}",
					DefaultExt = ".pdf",
					Filter = "PDF Files (.pdf)|*.pdf"
				};
				if (saveFileDialog.ShowDialog() == true && GenerateSenderReportPdf(certificates, summary, selectedColumns, senderName, saveFileDialog.FileName))
				{
					resultPath = saveFileDialog.FileName;
				}
			}
			catch (Exception ex)
			{
				LoggerService.LogError("Error in GenerateSenderReportWithDialog", ex);
				throw;
			}
		});
		return resultPath;
	}

	public string? GenerateReportWithDialog(List<Certificate> certificates, ReportSummary summary, List<string> selectedColumns, List<DistributionItem> topSuppliers, List<DistributionItem> topSenders, Dictionary<string, byte[]>? chartImages = null)
	{
		string resultPath = null;
		((DispatcherObject)Application.Current).Dispatcher.Invoke((Action)delegate
		{
			try
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					FileName = $"تقرير_{summary.StartDate:yyyyMMdd}_{summary.EndDate:yyyyMMdd}",
					DefaultExt = ".pdf",
					Filter = "PDF Files (.pdf)|*.pdf"
				};
				if (saveFileDialog.ShowDialog() == true && GenerateReportPdf(certificates, summary, selectedColumns, topSuppliers, topSenders, chartImages, saveFileDialog.FileName))
				{
					resultPath = saveFileDialog.FileName;
				}
			}
			catch (Exception ex)
			{
				LoggerService.LogError("Error in GenerateReportWithDialog", ex);
				throw;
			}
		});
		return resultPath;
	}
}
