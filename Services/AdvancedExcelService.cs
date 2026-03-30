using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Enjaz.Models;

namespace Enjaz.Services
{
    /// <summary>
    /// خدمة تصدير Excel المتقدمة
    /// Advanced Excel Export Service using ClosedXML
    /// </summary>
    public class AdvancedExcelService
    {
        /// <summary>
        /// تصدير التقرير إلى ملف Excel متقدم
        /// </summary>
        public bool ExportReport(
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
                using var workbook = new XLWorkbook();
                
                // ورقة الملخص
                CreateSummarySheet(workbook, summary, topSuppliers, topSenders);
                
                // ورقة الرسوم البيانية
                if (chartImages != null && chartImages.Any())
                {
                    CreateChartsSheet(workbook, chartImages);
                }

                // ورقة التفاصيل
                CreateDetailsSheet(workbook, certificates, selectedColumns);

                workbook.SaveAs(filePath);
                LoggerService.LogInfo($"تم تصدير التقرير بنجاح: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("فشل تصدير Excel", ex);
                return false;
            }
        }

        private void CreateSummarySheet(XLWorkbook workbook, ReportSummary summary, 
            List<DistributionItem> topSuppliers, List<DistributionItem> topSenders)
        {
            var ws = workbook.Worksheets.Add("الملخص");
            ws.RightToLeft = true;

            // العنوان
            ws.Cell("A1").Value = "تقرير الفترة";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Range("A1:D1").Merge();

            // الفترة
            ws.Cell("A2").Value = $"من {summary.StartDate:yyyy/MM/dd} إلى {summary.EndDate:yyyy/MM/dd}";
            ws.Range("A2:D2").Merge();

            // الإحصائيات
            ws.Cell("A4").Value = "الإحصائيات العامة";
            ws.Cell("A4").Style.Font.Bold = true;
            ws.Cell("A4").Style.Font.FontSize = 14;

            ws.Cell("A5").Value = "إجمالي الشهادات";
            ws.Cell("B5").Value = summary.TotalCertificates;

            ws.Cell("A6").Value = "إجمالي العينات";
            ws.Cell("B6").Value = summary.TotalSamples;

            ws.Cell("A7").Value = "إجمالي شهادات عينات بيئية";
            ws.Cell("B7").Value = summary.EnvironmentalCertificates;
            
            ws.Cell("A8").Value = "إجمالي عينات بيئية";
            ws.Cell("B8").Value = summary.EnvironmentalSamples;

            ws.Cell("A9").Value = "إجمالي شهادات عينات استهلاكية";
            ws.Cell("B9").Value = summary.ConsumableCertificates;
            
            ws.Cell("A10").Value = "إجمالي عينات استهلاكية";
            ws.Cell("B10").Value = summary.ConsumableSamples;

            // أعلى الموردين
            ws.Cell("A12").Value = "أعلى الموردين";
            ws.Cell("A12").Style.Font.Bold = true;
            ws.Cell("A12").Style.Font.FontSize = 14;

            int row = 13;
            foreach (var supplier in topSuppliers.Take(5))
            {
                ws.Cell($"A{row}").Value = supplier.Name;
                ws.Cell($"B{row}").Value = supplier.Count;
                row++;
            }

            // أعلى الجهات المرسلة
            row += 2;
            ws.Cell($"A{row}").Value = "أعلى الجهات المرسلة";
            ws.Cell($"A{row}").Style.Font.Bold = true;
            ws.Cell($"A{row}").Style.Font.FontSize = 14;
            row++;

            foreach (var sender in topSenders.Take(5))
            {
                ws.Cell($"A{row}").Value = sender.Name;
                ws.Cell($"B{row}").Value = sender.Count;
                row++;
            }

            // تنسيق العمود
            ws.Column("A").Width = 30;
            ws.Column("B").Width = 15;
        }

        private void CreateDetailsSheet(XLWorkbook workbook, List<Certificate> certificates, List<string> selectedColumns)
        {
            var ws = workbook.Worksheets.Add("التفاصيل");
            ws.RightToLeft = true;

            // القاموس لتحويل أسماء الخصائص إلى عناوين عربية
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

            // إذا لم يتم تحديد أعمدة، استخدم الكل
            if (selectedColumns == null || !selectedColumns.Any())
            {
                selectedColumns = columnHeaders.Keys.ToList();
            }

            // كتابة العناوين
            int col = 1;
            foreach (var prop in selectedColumns)
            {
                if (columnHeaders.ContainsKey(prop))
                {
                    ws.Cell(1, col).Value = columnHeaders[prop];
                    ws.Cell(1, col).Style.Font.Bold = true;
                    ws.Cell(1, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#1B4F72");
                    ws.Cell(1, col).Style.Font.FontColor = XLColor.White;
                    col++;
                }
            }

            // كتابة البيانات
            int row = 2;
            foreach (var cert in certificates)
            {
                col = 1;
                foreach (var prop in selectedColumns)
                {
                    object? value = prop switch
                    {
                        "CertificateNumber" => cert.CertificateNumber,
                        "CertificateType" => cert.CertificateType,
                        "IssueDate" => cert.IssueDate.ToString("yyyy/MM/dd"),
                        "Sender" => cert.Sender,
                        "Supplier" => cert.Supplier,
                        "Origin" => cert.Origin,
                        "NotificationNumber" => cert.NotificationNumber,
                        "DeclarationNumber" => cert.DeclarationNumber,
                        "FinancialReceiptNumber" => cert.FinancialReceiptNumber,
                        "SampleCount" => cert.SampleCount,
                        "EnvironmentalSampleCount" => cert.EnvironmentalSampleCount,
                        "ConsumableSampleCount" => cert.ConsumableSampleCount,
                        "CreatedByName" => cert.CreatedByName,
                        _ => ""
                    };

                    ws.Cell(row, col).Value = value?.ToString() ?? "";
                    col++;
                }

                // تنسيق صفوف بديلة
                if (row % 2 == 0)
                {
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");
                }

                row++;
            }

            // تنسيق الجدول
            ws.Columns().AdjustToContents();
            ws.Range(1, 1, 1, col - 1).Style.Border.BottomBorder = XLBorderStyleValues.Thick;
        }


        private void CreateChartsSheet(XLWorkbook workbook, Dictionary<string, byte[]> chartImages)
        {
            var sheet = workbook.Worksheets.Add("الرسوم البيانية");
            sheet.RightToLeft = true;

            int currentRow = 2;
            int currentColumn = 2;

            foreach (var chart in chartImages)
            {
                // Title
                sheet.Cell(currentRow, currentColumn).Value = chart.Key;
                sheet.Cell(currentRow, currentColumn).Style.Font.Bold = true;
                sheet.Cell(currentRow, currentColumn).Style.Font.FontSize = 14;

                // Image
                using (var ms = new MemoryStream(chart.Value))
                {
                    var picture = sheet.AddPicture(ms)
                        .MoveTo(sheet.Cell(currentRow + 1, currentColumn))
                        .Scale(0.8); // Adjust scale as needed
                }

                // Move to next position
                // Check if we should move to next column or next row
                // For simplicity, let's just stack them vertically with spacing
                currentRow += 25; 
            }
            
            sheet.Columns().AdjustToContents();
        }

        public string? ExportReportWithDialog(
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
                        FileName = $"تقرير_شامل_{summary.StartDate:yyyyMMdd}_{summary.EndDate:yyyyMMdd}",
                        DefaultExt = ".xlsx",
                        Filter = "Excel Workbook (.xlsx)|*.xlsx"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                         if (ExportReport(certificates, summary, selectedColumns, topSuppliers, topSenders, chartImages, dialog.FileName))
                        {
                            resultPath = dialog.FileName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.LogError("Error in ExportReportWithDialog", ex);
                    throw; // Rethrow to let ViewModel handle it
                }
            });
            return resultPath;
        }
        public async System.Threading.Tasks.Task<string?> ExportSenderReportWithDialogAsync(
            List<Certificate> certificates,
            ReportSummary summary,
            List<string> selectedColumns,
            string senderName)
        {
            string? resultPath = null;
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = $"تقرير_{senderName}_{summary.StartDate:yyyyMMdd}",
                        DefaultExt = ".xlsx",
                        Filter = "Excel Workbook (.xlsx)|*.xlsx"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        using var workbook = new XLWorkbook();
                        var ws = workbook.Worksheets.Add("تقرير الجهة");
                        ws.RightToLeft = true;

                        // 1. العنوان الرئيسي
                        ws.Cell("A1").Value = $"تقرير الجهة المرسلة: {senderName}";
                        ws.Cell("A1").Style.Font.Bold = true;
                        ws.Cell("A1").Style.Font.FontSize = 18;
                        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range("A1:F1").Merge();

                        ws.Cell("A2").Value = $"من {summary.StartDate:yyyy/MM/dd} إلى {summary.EndDate:yyyy/MM/dd}";
                        ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range("A2:F2").Merge();

                        // 2. إحصائيات الملخص (6 بطاقات)
                        int startStatsRow = 4;
                        // الصف الأول من الإحصائيات
                        ws.Range(startStatsRow, 1, startStatsRow + 1, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Cell(startStatsRow, 1).Value = "إجمالي الشهادات";
                        ws.Cell(startStatsRow + 1, 1).Value = summary.TotalCertificates;
                        ws.Cell(startStatsRow + 1, 1).Style.Font.FontColor = XLColor.FromHtml("#1B4F72");
                        ws.Cell(startStatsRow + 1, 1).Style.Font.FontSize = 16;
                        ws.Cell(startStatsRow + 1, 1).Style.Font.Bold = true;

                        ws.Range(startStatsRow, 3, startStatsRow + 1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Cell(startStatsRow, 3).Value = "إجمالي شهادات عينات بيئية";
                        ws.Cell(startStatsRow + 1, 3).Value = summary.EnvironmentalCertificates;

                        ws.Range(startStatsRow, 5, startStatsRow + 1, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Cell(startStatsRow, 5).Value = "إجمالي شهادات عينات استهلاكية";
                        ws.Cell(startStatsRow + 1, 5).Value = summary.ConsumableCertificates;

                        // الصف الثاني من الإحصائيات
                        int secondStatsRow = 7;
                        ws.Range(secondStatsRow, 1, secondStatsRow + 1, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Cell(secondStatsRow, 1).Value = "إجمالي العينات";
                        ws.Cell(secondStatsRow + 1, 1).Value = summary.TotalSamples;
                        ws.Cell(secondStatsRow + 1, 1).Style.Font.FontColor = XLColor.FromHtml("#1B4F72");
                        ws.Cell(secondStatsRow + 1, 1).Style.Font.FontSize = 16;
                        ws.Cell(secondStatsRow + 1, 1).Style.Font.Bold = true;

                        ws.Range(secondStatsRow, 3, secondStatsRow + 1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Cell(secondStatsRow, 3).Value = "إجمالي عينات بيئية";
                        ws.Cell(secondStatsRow + 1, 3).Value = summary.EnvironmentalSamples;

                        ws.Range(secondStatsRow, 5, secondStatsRow + 1, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Cell(secondStatsRow, 5).Value = "إجمالي عينات استهلاكية";
                        ws.Cell(secondStatsRow + 1, 5).Value = summary.ConsumableSamples;

                        // تنميق بطاقات الإحصائيات
                        var statsRange = ws.Range(startStatsRow, 1, secondStatsRow + 1, 6);
                        statsRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        statsRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        statsRange.Style.Font.Bold = true;

                        // 3. جدول التفاصيل
                        int startTableRow = 10;
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

                        // رأس الجدول
                        int currentColumn = 1;
                        foreach (var col in selectedColumns)
                        {
                            if (columnHeaders.ContainsKey(col))
                            {
                                var cell = ws.Cell(startTableRow, currentColumn);
                                cell.Value = columnHeaders[col];
                                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B4F72");
                                cell.Style.Font.FontColor = XLColor.White;
                                cell.Style.Font.Bold = true;
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                currentColumn++;
                            }
                        }

                        // بيانات الجدول
                        int currentRow = startTableRow + 1;
                        foreach (var cert in certificates)
                        {
                            currentColumn = 1;
                            foreach (var col in selectedColumns)
                            {
                                var cell = ws.Cell(currentRow, currentColumn);
                                cell.Value = col switch
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
                                    "SampleCount" => cert.SampleCount,
                                    "EnvironmentalSampleCount" => cert.EnvironmentalSampleCount,
                                    "ConsumableSampleCount" => cert.ConsumableSampleCount,
                                    "CreatedByName" => cert.CreatedByName ?? "",
                                    _ => ""
                                };

                                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                cell.Style.Border.OutsideBorderColor = XLColor.LightGray;
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                if (currentRow % 2 == 0)
                                {
                                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");
                                }
                                currentColumn++;
                            }
                            currentRow++;
                        }

                        ws.Columns().AdjustToContents();
                        workbook.SaveAs(dialog.FileName);
                        resultPath = dialog.FileName;
                        LoggerService.LogInfo($"تم تصدير تقرير الجهة المطور إلى Excel: {resultPath}");
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

        public async System.Threading.Tasks.Task<string?> ExportCertificatesWithDialogAsync(
            List<Certificate> certificates,
            List<string> selectedColumns)
        {
            string? resultPath = null;
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = $"تقرير_بيانات_{DateTime.Now:yyyyMMdd_HHmm}",
                        DefaultExt = ".xlsx",
                        Filter = "Excel Workbook (.xlsx)|*.xlsx"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        using var workbook = new XLWorkbook();
                        CreateDetailsSheet(workbook, certificates, selectedColumns);
                        workbook.SaveAs(dialog.FileName);
                        resultPath = dialog.FileName;
                        LoggerService.LogInfo($"تم تصدير البيانات إلى Excel: {resultPath}");
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
}
