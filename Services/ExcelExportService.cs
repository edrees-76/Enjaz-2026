using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Enjaz.Models;

namespace Enjaz.Services
{
    /// <summary>
    /// خدمة تصدير البيانات إلى Excel
    /// Export data to Excel service
    /// </summary>
    public class ExcelExportService
    {
        /// <summary>
        /// تصدير الشهادات إلى ملف CSV (متوافق مع Excel)
        /// Export certificates to CSV file (Excel compatible)
        /// </summary>
        public bool ExportCertificatesToCsv(IEnumerable<Certificate> certificates, string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                
                // UTF-8 BOM for Arabic support in Excel
                sb.Append('\uFEFF');
                
                // العناوين
                sb.AppendLine(string.Join(",", new[]
                {
                    "رقم الشهادة",
                    "اسم المستلم",
                    "نوع الشهادة",
                    "تاريخ الإصدار",
                    "جهة الإصدار",
                    "المرسل",
                    "المورد",
                    "بلد المنشأ",
                    "رقم الإقرار الجمركي",
                    "رقم البوليصة",
                    "رقم الإيصال المالي",
                    "اسم الأخصائي",
                    "رئيس القسم",
                    "المدير",
                    "عدد العينات",
                    "ملاحظات"
                }));

                // البيانات
                foreach (var cert in certificates)
                {
                    sb.AppendLine(string.Join(",", new[]
                    {
                        EscapeCsvField(cert.CertificateNumber),
                        EscapeCsvField(cert.RecipientName),
                        EscapeCsvField(cert.CertificateType),
                        cert.IssueDate.ToString("yyyy-MM-dd"),
                        EscapeCsvField(cert.IssuingAuthority ?? ""),
                        EscapeCsvField(cert.Sender ?? ""),
                        EscapeCsvField(cert.Supplier ?? ""),
                        EscapeCsvField(cert.Origin ?? ""),
                        EscapeCsvField(cert.DeclarationNumber ?? ""),
                        EscapeCsvField(cert.PolicyNumber ?? ""),
                        EscapeCsvField(cert.FinancialReceiptNumber ?? ""),
                        EscapeCsvField(cert.SpecialistName ?? ""),
                        EscapeCsvField(cert.SectionHeadName ?? ""),
                        EscapeCsvField(cert.ManagerName ?? ""),
                        cert.SampleCount.ToString(),
                        EscapeCsvField(cert.Notes ?? "")
                    }));
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                LoggerService.LogInfo($"Exported certificates to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Excel export failed", ex);
                return false;
            }
        }

        /// <summary>
        /// تصدير العينات إلى ملف CSV
        /// Export samples to CSV file
        /// </summary>
        public bool ExportSamplesToCsv(IEnumerable<Sample> samples, string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append('\uFEFF'); // UTF-8 BOM

                // العناوين
                sb.AppendLine(string.Join(",", new[]
                {
                    "معرف العينة",
                    "رقم الشهادة",
                    "رقم العينة",
                    "النتيجة",
                    "تاريخ القياس",
                    "K40",
                    "Ra226",
                    "Th232",
                    "Cs137",
                    "Ra"
                }));

                // البيانات
                foreach (var sample in samples)
                {
                    sb.AppendLine(string.Join(",", new[]
                    {
                        sample.Id.ToString(),
                        sample.CertificateId.ToString(),
                        EscapeCsvField(sample.SampleNumber ?? ""),
                        EscapeCsvField(sample.Result ?? ""),
                        sample.MeasurementDate.ToString("yyyy-MM-dd"),
                        sample.IsotopeK40 ?? "",
                        sample.IsotopeRa226 ?? "",
                        sample.IsotopeTh232 ?? "",
                        sample.IsotopeCs137 ?? "",
                        sample.IsotopeRa ?? ""
                    }));
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                LoggerService.LogInfo($"Exported samples to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Samples export failed", ex);
                return false;
            }
        }

        /// <summary>
        /// تهريب الحقول لتنسيق CSV
        /// </summary>
        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            // إذا كان الحقل يحتوي على فاصلة أو علامة اقتباس أو سطر جديد
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            
            return "\"" + field + "\"";
        }
    }
}
