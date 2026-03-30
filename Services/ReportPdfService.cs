using System;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Enjaz.Models;

namespace Enjaz.Services
{
    /// <summary>
    /// خدمة تصدير التقارير إلى PDF
    /// Report PDF Export Service
    /// </summary>
    public class ReportPdfService
    {
        public ReportPdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// توليد تقرير PDF
        /// </summary>
        public bool GenerateReportPdf(
            List<Certificate> certificates,
            ReportSummary summary,
            List<string> selectedColumns,
            List<DistributionItem> topSuppliers,
            List<DistributionItem> topSenders,
            Dictionary<string, byte[]>? chartImages,
            string filePath)
        {
            try
            {
                var columnHeaders = new Dictionary<string, string>
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

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.MarginVertical(20);
                        page.MarginHorizontal(25);
                        page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));
                        page.ContentFromRightToLeft();

                        page.Header().Column(col =>
                        {
                            col.Item().AlignCenter().Text("تقرير الفترة الزمنية").FontSize(22).Bold();
                            col.Item().AlignCenter().Text($"من {summary.StartDate:yyyy/MM/dd} إلى {summary.EndDate:yyyy/MM/dd}").FontSize(14);
                            col.Item().Height(15);
                        });

                        page.Content().Column(column =>
                        {
                            // 1. ملخص الإحصائيات (6 مؤشرات) باستخدام Table بدلاً من Grid لتجنب التحذير
                            column.Item().PaddingBottom(15).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn();
                                    cols.RelativeColumn();
                                });

                                // الصف الأول
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                                {
                                    c.Item().Text("إجمالي الشهادات").Bold();
                                    c.Item().Text(summary.TotalCertificates.ToString()).FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                                });
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                                {
                                    c.Item().Text("إجمالي العينات").Bold();
                                    c.Item().Text(summary.TotalSamples.ToString()).FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                                });

                                // الصف الثاني
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                                {
                                    c.Item().Text("إجمالي شهادات عينات بيئية").Bold();
                                    c.Item().Text(summary.EnvironmentalCertificates.ToString()).FontSize(16).Bold();
                                });
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                                {
                                    c.Item().Text("إجمالي عينات بيئية").Bold();
                                    c.Item().Text(summary.EnvironmentalSamples.ToString()).FontSize(16).Bold();
                                });

                                // الصف الثالث
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                                {
                                    c.Item().Text("إجمالي شهادات عينات استهلاكية").Bold();
                                    c.Item().Text(summary.ConsumableCertificates.ToString()).FontSize(16).Bold();
                                });
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                                {
                                    c.Item().Text("إجمالي عينات استهلاكية").Bold();
                                    c.Item().Text(summary.ConsumableSamples.ToString()).FontSize(16).Bold();
                                });
                            });

                             // 2. جدول البيانات (يلي الملخص مباشرة)
                            column.Item().PaddingTop(10).Table(table =>
                            {
                                // تعريف الأعمدة — عمود التسلسل أولاً
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30); // عمود م (التسلسل)
                                    foreach (var _ in selectedColumns)
                                    {
                                        columns.RelativeColumn();
                                    }
                                });

                                // رأس الجدول
                                table.Header(header =>
                                {
                                    header.Cell().Background("#1B4F72").Padding(5)
                                        .Text("م").FontColor(Colors.White).Bold().FontSize(9);
                                    foreach (var col in selectedColumns)
                                    {
                                        if (columnHeaders.ContainsKey(col))
                                        {
                                            header.Cell().Background("#1B4F72").Padding(5)
                                                .Text(columnHeaders[col]).FontColor(Colors.White).Bold().FontSize(9);
                                        }
                                    }
                                });

                                // صفوف البيانات
                                int rowIndex = 0;
                                foreach (var cert in certificates)
                                {
                                    var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                    
                                    // عمود التسلسل
                                    table.Cell().Background(bgColor).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(4).AlignCenter().Text((rowIndex + 1).ToString()).FontSize(8).Bold();

                                    foreach (var col in selectedColumns)
                                    {
                                        string value = col switch
                                        {
                                            "CertificateNumber" => cert.CertificateNumber,
                                            "CertificateType" => cert.CertificateType ?? "",
                                            "IssueDate" => cert.IssueDate.ToString("yyyy/MM/dd"),
                                            "Sender" => cert.Sender ?? "",
                                            "Supplier" => cert.Supplier ?? "",
                                            "Origin" => cert.Origin ?? "",
                                            "NotificationNumber" => cert.NotificationNumber ?? "",
                                            "DeclarationNumber" => cert.DeclarationNumber ?? "",
                                            "FinancialReceiptNumber" => cert.FinancialReceiptNumber ?? "",
                                            "SampleCount" => cert.SampleCount.ToString(),
                                            "EnvironmentalSampleCount" => cert.EnvironmentalSampleCount.ToString(),
                                            "ConsumableSampleCount" => cert.ConsumableSampleCount.ToString(),
                                            "CreatedByName" => cert.CreatedByName ?? "",
                                            _ => ""
                                        };

                                        table.Cell().Background(bgColor).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                            .Padding(4).Text(value).FontSize(8);
                                    }
                                    rowIndex++;
                                }
                            });



                            // 3. تحليل البيانات - الرسوم البيانية (صفحات مستقلة)
                            if (chartImages != null && chartImages.Any())
                            {
                                var charts = chartImages.ToList();
                                // We iterate charts in pairs of 2
                                for (int i = 0; i < charts.Count; i += 2)
                                {
                                    // Start a NEW Page for every pair
                                    column.Item().PageBreak();

                                    // Header for this chart page
                                    column.Item().PaddingBottom(20).AlignCenter().Text("تحليل البيانات (الرسوم البيانية)").FontSize(18).Bold().FontColor("#1B4F72");

                                    // Side-by-Side Row
                                    column.Item().Row(row =>
                                    {
                                        row.Spacing(20);

                                        // Chart 1 (Left)
                                        var chart1 = charts[i];
                                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                                        {
                                            c.Item().PaddingBottom(10).AlignCenter().Text(chart1.Key).FontSize(14).Bold();
                                            // Maximize height for side-by-side view (approx 380pt fits well in A4 Landscape)
                                            c.Item().Height(380).Image(chart1.Value).FitArea();
                                        });

                                        // Chart 2 (Right)
                                        if (i + 1 < charts.Count)
                                        {
                                            var chart2 = charts[i + 1];
                                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                                            {
                                                c.Item().PaddingBottom(10).AlignCenter().Text(chart2.Key).FontSize(14).Bold();
                                                c.Item().Height(380).Image(chart2.Value).FitArea();
                                            });
                                        }
                                        else
                                        {
                                            // Empty filler to maintain left chart width
                                            row.RelativeItem().Column(c => c.Item().Text(""));
                                        }
                                    });
                                }
                            }
                        });

                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("صفحة ");
                            text.CurrentPageNumber();
                            text.Span(" من ");
                            text.TotalPages();
                        });
                    });
                });

                document.GeneratePdf(filePath);
                LoggerService.LogInfo($"تم توليد تقرير PDF: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("فشل توليد تقرير PDF", ex);
                throw new Exception($"فشل إنشاء التقرير: {ex.Message}", ex);
            }
        }

        public bool GenerateSenderReportPdf(
            List<Certificate> certificates,
            ReportSummary summary,
            List<string> selectedColumns,
            string senderName,
            string filePath)
        {
            try
            {
                var columnHeaders = new Dictionary<string, string>
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

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.MarginVertical(20);
                        page.MarginHorizontal(25);
                        page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));
                        page.ContentFromRightToLeft();

                        page.Header().Column(col =>
                        {
                            col.Item().AlignCenter().Text($"تقرير الجهة المرسلة: {senderName}").FontSize(22).Bold();
                            col.Item().AlignCenter().Text($"من {summary.StartDate:yyyy/MM/dd} إلى {summary.EndDate:yyyy/MM/dd}").FontSize(14);
                            col.Item().Height(15);
                        });

                        page.Content().Column(column =>
                        {
                            // 1. ملخص الإحصائيات (6 مؤشرات) باستخدام Table بدلاً من Grid لتجنب التحذير
                            column.Item().PaddingBottom(15).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn();
                                    cols.RelativeColumn();
                                    cols.RelativeColumn();
                                });

                                // الصف الأول
                                table.Cell().Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                                {
                                    c.Item().Text("إجمالي الشهادات").Bold();
                                    c.Item().Text(summary.TotalCertificates.ToString()).FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                                });
                                table.Cell().Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                                {
                                    c.Item().Text("إجمالي شهادات عينات بيئية").Bold();
                                    c.Item().Text(summary.EnvironmentalCertificates.ToString()).FontSize(16).Bold();
                                });
                                table.Cell().Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                                {
                                    c.Item().Text("إجمالي شهادات عينات استهلاكية").Bold();
                                    c.Item().Text(summary.ConsumableCertificates.ToString()).FontSize(16).Bold();
                                });

                                // الصف الثاني
                                table.Cell().Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                                {
                                    c.Item().Text("إجمالي العينات").Bold();
                                    c.Item().Text(summary.TotalSamples.ToString()).FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                                });
                                table.Cell().Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                                {
                                    c.Item().Text("إجمالي عينات بيئية").Bold();
                                    c.Item().Text(summary.EnvironmentalSamples.ToString()).FontSize(16).Bold();
                                });
                                table.Cell().Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                                {
                                    c.Item().Text("إجمالي عينات استهلاكية").Bold();
                                    c.Item().Text(summary.ConsumableSamples.ToString()).FontSize(16).Bold();
                                });
                            });

                             // 2. جدول البيانات
                            column.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columnsDef =>
                                {
                                    columnsDef.ConstantColumn(30); // عمود م (التسلسل)
                                    foreach (var _ in selectedColumns)
                                    {
                                        columnsDef.RelativeColumn();
                                    }
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#1B4F72").Padding(5)
                                        .Text("م").FontColor(Colors.White).Bold().FontSize(9);
                                    foreach (var col in selectedColumns)
                                    {
                                        if (columnHeaders.ContainsKey(col))
                                        {
                                            header.Cell().Background("#1B4F72").Padding(5)
                                                .Text(columnHeaders[col]).FontColor(Colors.White).Bold().FontSize(9);
                                        }
                                    }
                                });

                                int rowIndex = 0;
                                foreach (var cert in certificates)
                                {
                                    var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                    
                                    // عمود التسلسل
                                    table.Cell().Background(bgColor).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(4).AlignCenter().Text((rowIndex + 1).ToString()).FontSize(8).Bold();

                                    foreach (var col in selectedColumns)
                                    {
                                        string value = col switch
                                        {
                                            "CertificateNumber" => cert.CertificateNumber,
                                            "CertificateType" => cert.CertificateType ?? "",
                                            "IssueDate" => cert.IssueDate.ToString("yyyy/MM/dd"),
                                            "Sender" => cert.Sender ?? "",
                                            "Supplier" => cert.Supplier ?? "",
                                            "Origin" => cert.Origin ?? "",
                                            "NotificationNumber" => cert.NotificationNumber ?? "",
                                            "DeclarationNumber" => cert.DeclarationNumber ?? "",
                                            "FinancialReceiptNumber" => cert.FinancialReceiptNumber ?? "",
                                            "SampleCount" => cert.SampleCount.ToString(),
                                            "EnvironmentalSampleCount" => cert.EnvironmentalSampleCount.ToString(),
                                            "ConsumableSampleCount" => cert.ConsumableSampleCount.ToString(),
                                            "CreatedByName" => cert.CreatedByName ?? "",
                                            _ => ""
                                        };

                                        table.Cell().Background(bgColor).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                            .Padding(4).Text(value).FontSize(8);
                                    }
                                    rowIndex++;
                                }
                            });
                        });

                        page.Footer().AlignCenter().Text(text =>
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
                throw new Exception($"فشل إنشاء تقرير الجهة: {ex.Message}", ex);
            }
        }

        public string? GenerateSenderReportWithDialog(
            List<Certificate> certificates,
            ReportSummary summary,
            List<string> selectedColumns,
            string senderName)
        {
            string? resultPath = null;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = $"تقرير_{senderName}_{summary.StartDate:yyyyMMdd}",
                        DefaultExt = ".pdf",
                        Filter = "PDF Files (.pdf)|*.pdf"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        if (GenerateSenderReportPdf(certificates, summary, selectedColumns, senderName, dialog.FileName))
                        {
                            resultPath = dialog.FileName;
                        }
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

        public string? GenerateReportWithDialog(
            List<Certificate> certificates,
            ReportSummary summary,
            List<string> selectedColumns,
            List<DistributionItem> topSuppliers,
            List<DistributionItem> topSenders,
            Dictionary<string, byte[]>? chartImages = null)
        {
            string? resultPath = null;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = $"تقرير_{summary.StartDate:yyyyMMdd}_{summary.EndDate:yyyyMMdd}",
                        DefaultExt = ".pdf",
                        Filter = "PDF Files (.pdf)|*.pdf"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        if (GenerateReportPdf(certificates, summary, selectedColumns, topSuppliers, topSenders, chartImages, dialog.FileName))
                        {
                            resultPath = dialog.FileName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.LogError("Error in GenerateReportWithDialog", ex);
                    throw; // Rethrow to let ViewModel handle it
                }
            });
            return resultPath;
        }
    }
}
