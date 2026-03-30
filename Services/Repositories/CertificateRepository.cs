using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Enjaz.Models;
using Enjaz.Helpers;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// مستودع الشهادات - إدارة عمليات الشهادات والعينات
    /// Certificate Repository - Manages Certificates and Samples operations
    /// </summary>
    public partial class CertificateRepository
    {
        private readonly DatabaseService _db;
        private readonly UserService _userService;

        public CertificateRepository(DatabaseService db, UserService userService)
        {
            _db = db;
            _userService = userService;
        }

        #region Mapping Helpers â€” DRY (Don't Repeat Yourself)

        /// <summary>
        /// طھط­ظˆظٹظ„ طµظپ ظ‚ط§ط¹ط¯ط© ط§ظ„ط¨ظٹط§ظ†ط§طھ ط¥ظ„ظ‰ ظƒط§ط¦ظ† ط´ظ‡ط§ط¯ط© â€” ظ†ظ‚ط·ط© ظ…ط±ظƒط²ظٹط© ظˆط§ط­ط¯ط© ظ„طھط¬ظ†ط¨ ط§ظ„طھظƒط±ط§ط±
        /// Maps a DB row to a Certificate object â€” single centralized point to avoid duplication.
        /// Expected column order: Id, CertificateNumber, RecipientName, CertificateType, Description,
        ///   IssueDate, ExpiryDate, IssuingAuthority, CreatedBy, CreatedByName, CreatedAt,
        ///   AnalysisType, Sender, Supplier, Origin, DeclarationNumber, PolicyNumber,
        ///   NotificationNumber, FinancialReceiptNumber, SpecialistName, SectionHeadName,
        ///   ManagerName, Notes, UpdatedBy, UpdatedByName, UpdatedAt
        /// Optional columns at idx 26+: SampleCount, ReceptionId
        /// </summary>
        private static Certificate MapCertificateFromReader(SqliteDataReader reader, int columnCount = 0)
        {
            if (columnCount == 0) columnCount = reader.FieldCount;

            return new Certificate
            {
                Id = reader.GetInt32(0),
                CertificateNumber = reader.GetString(1),
                RecipientName = reader.GetString(2),
                CertificateType = reader.GetString(3),
                Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                IssueDate = SafeParseDate(reader.GetString(5), "yyyy-MM-dd"),
                ExpiryDate = reader.IsDBNull(6) ? null : SafeParseDate(reader.GetString(6), "yyyy-MM-dd"),
                IssuingAuthority = reader.IsDBNull(7) ? "" : reader.GetString(7),
                CreatedBy = reader.GetInt32(8),
                CreatedByName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                CreatedAt = SafeParseDate(reader.GetString(10), "yyyy-MM-dd HH:mm:ss"),
                AnalysisType = reader.IsDBNull(11) ? "" : reader.GetString(11),
                Sender = reader.IsDBNull(12) ? "" : reader.GetString(12),
                Supplier = reader.IsDBNull(13) ? "" : reader.GetString(13),
                Origin = reader.IsDBNull(14) ? "" : reader.GetString(14),
                DeclarationNumber = reader.IsDBNull(15) ? "" : reader.GetString(15),
                PolicyNumber = reader.IsDBNull(16) ? "" : reader.GetString(16),
                NotificationNumber = reader.IsDBNull(17) ? "" : reader.GetString(17),
                FinancialReceiptNumber = reader.IsDBNull(18) ? "" : reader.GetString(18),
                SpecialistName = reader.IsDBNull(19) ? "" : reader.GetString(19),
                SectionHeadName = reader.IsDBNull(20) ? "" : reader.GetString(20),
                ManagerName = reader.IsDBNull(21) ? "" : reader.GetString(21),
                Notes = reader.IsDBNull(22) ? "" : reader.GetString(22),
                UpdatedBy = reader.IsDBNull(23) ? null : (int?)reader.GetInt32(23),
                UpdatedByName = reader.IsDBNull(24) ? "" : reader.GetString(24),
                UpdatedAt = reader.IsDBNull(25) ? null : (DateTime?)SafeParseDate(reader.GetString(25), "yyyy-MM-dd HH:mm:ss"),
                // Optional columns â€” only if present in the query
                SampleCount = columnCount > 26 && !reader.IsDBNull(26) ? reader.GetInt32(26) : 0,
                ReceptionId = columnCount > 26 && reader.GetName(columnCount - 1) == "ReceptionId" && !reader.IsDBNull(columnCount - 1) ? (int?)reader.GetInt32(columnCount - 1) : null
            };
        }

        /// <summary>
        /// تحويل صف قاعدة البيانات إلى كائن عينة
        /// Maps a DB row to a Sample object.
        /// Expected: Id, CertificateId, Root, SampleNumber, Description, MeasurementDate, Result,
        ///   IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137
        /// </summary>
        private static Sample MapSampleFromReader(SqliteDataReader reader)
        {
            return new Sample
            {
                Id = reader.GetInt32(0),
                CertificateId = reader.GetInt32(1),
                Root = reader.GetInt32(2),
                SampleNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                MeasurementDate = SafeParseDate(reader.GetString(5), "yyyy-MM-dd"),
                Result = reader.IsDBNull(6) ? "" : reader.GetString(6),
                IsotopeK40 = reader.IsDBNull(7) ? "" : reader.GetString(7),
                IsotopeRa226 = reader.IsDBNull(8) ? "" : reader.GetString(8),
                IsotopeTh232 = reader.IsDBNull(9) ? "" : reader.GetString(9),
                IsotopeRa = reader.IsDBNull(10) ? "" : reader.GetString(10),
                IsotopeCs137 = reader.IsDBNull(11) ? "" : reader.GetString(11)
            };
        }

        /// <summary>
        /// تحليل التاريخ بشكل آمن مع fallback لعدة صيغ
        /// Safely parse date with multiple format fallback to prevent FormatException
        /// </summary>
        private static DateTime SafeParseDate(string dateStr, string primaryFormat)
        {
            if (DateTime.TryParseExact(dateStr, primaryFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var result))
                return result;
            // Fallback: try common formats
            string[] fallbackFormats = { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd", "dd/MM/yyyy", "MM/dd/yyyy" };
            if (DateTime.TryParseExact(dateStr, fallbackFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
                return result;
            // Last resort: general parse
            if (DateTime.TryParse(dateStr, out result))
                return result;
            LoggerService.LogWarning($"Failed to parse date: '{dateStr}' with format '{primaryFormat}'. Using DateTime.MinValue.");
            return DateTime.MinValue;
        }

        public async System.Threading.Tasks.Task<List<int>> GetAvailableYearsAsync()
        {
            var years = new List<int>();
            await _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                
                // Fetch distinct years from both Certificates and Receptions
                var query = @"
                    SELECT DISTINCT strftime('%Y', IssueDate) as Y FROM Certificates WHERE Y IS NOT NULL AND Y != ''
                    UNION
                    SELECT DISTINCT strftime('%Y', Date) as Y FROM SampleReceptions WHERE Y IS NOT NULL AND Y != ''
                    ORDER BY Y DESC;";
                
                using var command = new SqliteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0) && int.TryParse(reader.GetString(0), out int year))
                    {
                        if (!years.Contains(year))
                            years.Add(year);
                    }
                }
            });
            
            int currentYear = DateTime.Now.Year;
            if (!years.Contains(currentYear))
                years.Insert(0, currentYear);
                
            years.Sort((a,b) => b.CompareTo(a)); // Descending
            return years;
        }

        #endregion

        private string GetCertificateSummary(Certificate c)
        {
            if (c == null) return "غير معروف";
            var summary = new List<string>();
            summary.Add($"رقم الشهادة: {c.CertificateNumber}");
            summary.Add($"النوع: {c.CertificateType}");
            summary.Add($"الجهة: {c.RecipientName}");
            if (!string.IsNullOrEmpty(c.Sender)) summary.Add($"المرسل: {c.Sender}");
            if (!string.IsNullOrEmpty(c.Supplier)) summary.Add($"المورد: {c.Supplier}");
            if (!string.IsNullOrEmpty(c.Origin)) summary.Add($"المنشأ: {c.Origin}");
            if (!string.IsNullOrEmpty(c.NotificationNumber)) summary.Add($"الإخطار: {c.NotificationNumber}");
            if (!string.IsNullOrEmpty(c.DeclarationNumber)) summary.Add($"الإقرار: {c.DeclarationNumber}");
            if (!string.IsNullOrEmpty(c.FinancialReceiptNumber)) summary.Add($"الإيصال: {c.FinancialReceiptNumber}");
            if (!string.IsNullOrEmpty(c.PolicyNumber)) summary.Add($"البوليصة: {c.PolicyNumber}");
            if (!string.IsNullOrEmpty(c.AnalysisType)) summary.Add($"التحليل: {c.AnalysisType}");
            if (!string.IsNullOrEmpty(c.SpecialistName)) summary.Add($"المختص: {c.SpecialistName}");
            if (!string.IsNullOrEmpty(c.SectionHeadName)) summary.Add($"رئيس القسم: {c.SectionHeadName}");
            if (!string.IsNullOrEmpty(c.ManagerName)) summary.Add($"المدير: {c.ManagerName}");
            
            return string.Join(" | ", summary);
        }

        private string GetCertificateChanges(Certificate oldCert, Certificate newCert)
        {
            var changes = new List<string>();
            if (oldCert == null || newCert == null) return "تعديل بيانات";

            // Helper to format change
            string Fmt(string field, string? oldV, string? newV) => $"{field}: من ({oldV ?? "فارغ"}) إلى ({newV ?? "فارغ"})";

            if (oldCert.RecipientName != newCert.RecipientName) changes.Add(Fmt("الجهة", oldCert.RecipientName, newCert.RecipientName));
            if (oldCert.CertificateType != newCert.CertificateType) changes.Add(Fmt("النوع", oldCert.CertificateType, newCert.CertificateType));
            if (oldCert.Description != newCert.Description) changes.Add(Fmt("الوصف", oldCert.Description, newCert.Description));
            if (oldCert.IssueDate.Date != newCert.IssueDate.Date) changes.Add(Fmt("تاريخ الإصدار", oldCert.IssueDate.ToString("yyyy/MM/dd"), newCert.IssueDate.ToString("yyyy/MM/dd")));
            if (oldCert.ExpiryDate != newCert.ExpiryDate) changes.Add(Fmt("تاريخ الصلاحية", oldCert.ExpiryDate?.ToString("yyyy/MM/dd"), newCert.ExpiryDate?.ToString("yyyy/MM/dd")));
            if (oldCert.IssuingAuthority != newCert.IssuingAuthority) changes.Add(Fmt("جهة الإصدار", oldCert.IssuingAuthority, newCert.IssuingAuthority));
            if (oldCert.Sender != newCert.Sender) changes.Add(Fmt("الجهة المرسلة", oldCert.Sender, newCert.Sender));
            if (oldCert.Supplier != newCert.Supplier) changes.Add(Fmt("المورد", oldCert.Supplier, newCert.Supplier));
            if (oldCert.Origin != newCert.Origin) changes.Add(Fmt("بلد المنشأ", oldCert.Origin, newCert.Origin));
            if (oldCert.DeclarationNumber != newCert.DeclarationNumber) changes.Add(Fmt("الإقرار الجمركي", oldCert.DeclarationNumber, newCert.DeclarationNumber));
            if (oldCert.NotificationNumber != newCert.NotificationNumber) changes.Add(Fmt("رقم الإخطار", oldCert.NotificationNumber, newCert.NotificationNumber));
            if (oldCert.PolicyNumber != newCert.PolicyNumber) changes.Add(Fmt("رقم البوليصة", oldCert.PolicyNumber, newCert.PolicyNumber));
            if (oldCert.FinancialReceiptNumber != newCert.FinancialReceiptNumber) changes.Add(Fmt("الإيصال المالي", oldCert.FinancialReceiptNumber, newCert.FinancialReceiptNumber));
            if (oldCert.AnalysisType != newCert.AnalysisType) changes.Add(Fmt("نوع التحليل", oldCert.AnalysisType, newCert.AnalysisType));
            if (oldCert.SpecialistName != newCert.SpecialistName) changes.Add(Fmt("المختص", oldCert.SpecialistName, newCert.SpecialistName));
            if (oldCert.SectionHeadName != newCert.SectionHeadName) changes.Add(Fmt("رئيس القسم", oldCert.SectionHeadName, newCert.SectionHeadName));
            if (oldCert.ManagerName != newCert.ManagerName) changes.Add(Fmt("المدير", oldCert.ManagerName, newCert.ManagerName));
            if (oldCert.Notes != newCert.Notes) changes.Add(Fmt("الملاحظات", oldCert.Notes, newCert.Notes));

            // مقارنة العينات بشكل تفصيلي
            var oldSamples = oldCert.Samples?.ToList() ?? new List<Sample>();
            var newSamples = newCert.Samples?.ToList() ?? new List<Sample>();

            // 1. العينات المضافة
            var addedSamples = newSamples.Where(ns => ns.Id == 0 || !oldSamples.Any(os => os.Id == ns.Id)).ToList();
            foreach (var s in addedSamples)
            {
                changes.Add($"قام بإضافة عينة رقم ({s.SampleNumber}) ووصفها ({s.Description}) ونتيجتها ({s.Result})");
            }

            // 2. العينات المعدلة
            foreach (var sOld in oldSamples)
            {
                var sNew = newSamples.FirstOrDefault(ns => ns.Id == sOld.Id);
                if (sNew == null) continue;

                var sampleChanges = new List<string>();
                if (sOld.SampleNumber != sNew.SampleNumber) sampleChanges.Add(Fmt("رقم العينة", sOld.SampleNumber, sNew.SampleNumber));
                if (sOld.Description != sNew.Description) sampleChanges.Add(Fmt("الوصف", sOld.Description, sNew.Description));
                if (sOld.Result != sNew.Result) sampleChanges.Add(Fmt("النتيجة", sOld.Result, sNew.Result));
                
                // النظائر
                if (sOld.IsotopeK40 != sNew.IsotopeK40) sampleChanges.Add(Fmt("K40", sOld.IsotopeK40, sNew.IsotopeK40));
                if (sOld.IsotopeRa226 != sNew.IsotopeRa226) sampleChanges.Add(Fmt("Ra226", sOld.IsotopeRa226, sNew.IsotopeRa226));
                if (sOld.IsotopeTh232 != sNew.IsotopeTh232) sampleChanges.Add(Fmt("Th232", sOld.IsotopeTh232, sNew.IsotopeTh232));
                if (sOld.IsotopeRa != sNew.IsotopeRa) sampleChanges.Add(Fmt("Ra", sOld.IsotopeRa, sNew.IsotopeRa));
                if (sOld.IsotopeCs137 != sNew.IsotopeCs137) sampleChanges.Add(Fmt("Cs137", sOld.IsotopeCs137, sNew.IsotopeCs137));

                if (sampleChanges.Count > 0)
                {
                    changes.Add($"تعديل عينة (رقم {sOld.SampleNumber}): {string.Join(" | ", sampleChanges)}");
                }
            }

            var deletedSamples = oldSamples.Where(os => os.Id > 0 && !newSamples.Any(ns => ns.Id == os.Id)).ToList();
            foreach (var s in deletedSamples)
            {
                changes.Add($"قام بحذف عينة رقم ({s.SampleNumber}) ووصفها ({s.Description})");
            }

            return changes.Count > 0 ? string.Join(" | ", changes) : "تعديل بدون تغيير في الحقول الأساسية";
        }

        /// <summary>
        /// الحصول على الشهادات مع تقسيم الصفحات بشكل غير متزامن
        /// Get certificates paginated asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<List<Certificate>> GetCertificatesPaginatedAsync(int pageNumber, int pageSize, bool showDeleted = false)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var certificates = new List<Certificate>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                int offset = (pageNumber - 1) * pageSize;

                var query = @"SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, 
                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, 
                              c.CreatedBy, c.CreatedByName, datetime(c.CreatedAt, 'localtime'),
                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,
                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,
                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,
                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt,
                              (SELECT COUNT(*) FROM Samples s WHERE s.CertificateId = c.Id) AS SampleCount
                              FROM Certificates c 
                              ORDER BY c.CreatedAt DESC
                              LIMIT @Limit OFFSET @Offset;";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Limit", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    try 
                    {
                        certificates.Add(MapCertificateFromReader((SqliteDataReader)reader));
                    }
                    catch (Exception ex)
                    {
                        // Skip bad record but log it
                        LoggerService.LogError("Failed to map certificate record, skipping...", ex);
                        continue;
                    }
                }
                return certificates;
            }, "GetCertificatesPaginatedAsync");
        }

        /// <summary>
        /// الحصول على جميع الشهادات بشكل غير متزامن
        /// Get all certificates — DEPRECATED: use GetCertificatesPaginatedAsync instead
        /// </summary>
        [Obsolete("Use GetCertificatesPaginatedAsync for better memory management")]
        public System.Threading.Tasks.Task<List<Certificate>> GetAllCertificatesAsync()
        {
             LoggerService.LogWarning("GetAllCertificatesAsync called — consider using pagination instead");
             return GetCertificatesPaginatedAsync(1, 200);
        }

        /// <summary>
        /// جلب الشهادات ضمن نطاق زمني محدد - محسن للاستعلام من قاعدة البيانات
        /// </summary>
        public System.Threading.Tasks.Task<List<Certificate>> GetCertificatesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var certificates = new List<Certificate>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, 
                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, 
                              c.CreatedBy, c.CreatedByName, datetime(c.CreatedAt, 'localtime'),
                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,
                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,
                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,
                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt,
                              (SELECT COUNT(*) FROM Samples s WHERE s.CertificateId = c.Id) AS SampleCount
                              FROM Certificates c 
                              WHERE date(c.IssueDate) >= date(@StartDate) 
                              AND date(c.IssueDate) <= date(@EndDate)
                              ORDER BY c.IssueDate ASC;";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    certificates.Add(MapCertificateFromReader((SqliteDataReader)reader));
                }
                return certificates;
            }, "GetCertificatesByDateRangeAsync");
        }

        /// <summary>
        /// التحقق من تفرد رقم الإيصال المالي
        /// Check if Financial Receipt Number is unique
        /// </summary>
        /// <param name="receiptNumber">رقم الإيصال</param>
        /// <param name="excludeCertificateId">معرف الشهادة المستثناة (للتعديل)</param>
        /// <returns>true if unique, false if duplicate exists</returns>
        public bool IsFinancialReceiptNumberUnique(string receiptNumber, int? excludeCertificateId = null)
        {
            if (string.IsNullOrWhiteSpace(receiptNumber))
                return true; // Empty receipt numbers are allowed (not required)

            using var connection = new SqliteConnection(_db.ConnectionString);
            connection.Open();

            var query = excludeCertificateId.HasValue
                ? "SELECT COUNT(*) FROM Certificates WHERE FinancialReceiptNumber = @ReceiptNumber AND Id != @ExcludeId"
                : "SELECT COUNT(*) FROM Certificates WHERE FinancialReceiptNumber = @ReceiptNumber";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@ReceiptNumber", receiptNumber);
            if (excludeCertificateId.HasValue)
            {
                command.Parameters.AddWithValue("@ExcludeId", excludeCertificateId.Value);
            }

            var count = Convert.ToInt32(command.ExecuteScalar());
            return count == 0;
        }

        /// <summary>
        /// الحصول على شهادة بواسطة معرف الاستلام المرتبط بشكل غير متزامن
        /// Get certificate by associated ReceptionId asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<Certificate?> GetCertificateByReceptionIdAsync(int receptionId)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, 
                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, 
                              c.CreatedBy, c.CreatedByName, datetime(c.CreatedAt, 'localtime'),
                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,
                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,
                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,
                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt
                              FROM Certificates c 
                              WHERE c.ReceptionId = @ReceptionId;";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@ReceptionId", receptionId);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var cert = MapCertificateFromReader((SqliteDataReader)reader);
                    cert.Samples = new System.Collections.ObjectModel.ObservableCollection<Sample>();

                    reader.Close(); 

                    var samplesQuery = @"SELECT Id, CertificateId, Root, SampleNumber, Description, MeasurementDate, Result,
                                      IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137
                                      FROM Samples WHERE CertificateId = @Id ORDER BY Root ASC";
                    
                    using var samplesCmd = new SqliteCommand(samplesQuery, connection);
                    samplesCmd.Parameters.AddWithValue("@Id", cert.Id);
                    using var samplesReader = await samplesCmd.ExecuteReaderAsync();
                    
                    while (await samplesReader.ReadAsync())
                    {
                        cert.Samples.Add(MapSampleFromReader((SqliteDataReader)samplesReader));
                    }

                    return cert;
                }
                return null;
            }, "GetCertificateByReceptionIdAsync");
        }

        /// <summary>
        /// الحصول على شهادة بواسطة المعرف بشكل غير متزامن
        /// Get certificate by ID asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<Certificate?> GetCertificateByIdAsync(int id)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, 
                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, 
                              c.CreatedBy, c.CreatedByName, datetime(c.CreatedAt, 'localtime'),
                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,
                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,
                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,
                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt, c.ReceptionId
                              FROM Certificates c 
                              WHERE c.Id = @Id;";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var cert = MapCertificateFromReader((SqliteDataReader)reader);
                    cert.Samples = new System.Collections.ObjectModel.ObservableCollection<Sample>();

                    // Close the first reader to execute the second query
                    reader.Close(); 

                    // Load Samples
                    var samplesQuery = @"SELECT Id, CertificateId, Root, SampleNumber, Description, MeasurementDate, Result,
                                      IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137
                                      FROM Samples WHERE CertificateId = @Id ORDER BY Root ASC";
                    
                    using var samplesCmd = new SqliteCommand(samplesQuery, connection);
                    samplesCmd.Parameters.AddWithValue("@Id", id);
                    using var samplesReader = await samplesCmd.ExecuteReaderAsync();
                    
                    while (await samplesReader.ReadAsync())
                    {
                        cert.Samples.Add(MapSampleFromReader((SqliteDataReader)samplesReader));
                    }

                    return cert;
                }
                return null;
            }, "GetCertificateByIdAsync");
        }

        /// <summary>
        /// إضافة شهادة جديدة بشكل غير متزامن
        /// Add new certificate asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<int> AddCertificateAsync(Certificate certificate)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. Insert with temporary number
                    var query = @"INSERT INTO Certificates 
                                 (CertificateNumber, RecipientName, CertificateType, Description, 
                                  IssueDate, ExpiryDate, IssuingAuthority, CreatedBy, CreatedByName,
                                  AnalysisType, Sender, Supplier, Origin, DeclarationNumber,
                                  PolicyNumber, NotificationNumber, FinancialReceiptNumber,
                                  SpecialistName, SectionHeadName, ManagerName, Notes, ReceptionId)
                                 VALUES (@CertificateNumber, @RecipientName, @CertificateType, @Description,
                                         @IssueDate, @ExpiryDate, @IssuingAuthority, @CreatedBy, @CreatedByName,
                                         @AnalysisType, @Sender, @Supplier, @Origin, @DeclarationNumber,
                                         @PolicyNumber, @NotificationNumber, @FinancialReceiptNumber,
                                         @SpecialistName, @SectionHeadName, @ManagerName, @Notes, @ReceptionId);
                                 SELECT last_insert_rowid();";

                    using var command = new SqliteCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("@CertificateNumber", "TEMP-" + Guid.NewGuid());
                    command.Parameters.AddWithValue("@RecipientName", certificate.RecipientName);
                    command.Parameters.AddWithValue("@CertificateType", certificate.CertificateType);
                    command.Parameters.AddWithValue("@Description", certificate.Description ?? "");
                    command.Parameters.AddWithValue("@IssueDate", certificate.IssueDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                    command.Parameters.AddWithValue("@ExpiryDate", certificate.ExpiryDate.HasValue ? certificate.ExpiryDate.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : DBNull.Value);
                    command.Parameters.AddWithValue("@IssuingAuthority", certificate.IssuingAuthority ?? "");
                    
                    // Validate CreatedBy
                    var checkUserCmd = new SqliteCommand("SELECT COUNT(*) FROM Users WHERE Id = @Uid", connection, transaction);
                    checkUserCmd.Parameters.AddWithValue("@Uid", certificate.CreatedBy);
                    long userCount = (long)(await checkUserCmd.ExecuteScalarAsync() ?? 0);
                    
                    int validUserId = certificate.CreatedBy;
                    if (userCount == 0)
                    {
                        var getAdminCmd = new SqliteCommand("SELECT Id FROM Users WHERE Role = 2 LIMIT 1", connection, transaction);
                        object? adminId = await getAdminCmd.ExecuteScalarAsync();
                        if (adminId != null)
                        {
                            validUserId = Convert.ToInt32(adminId);
                        }
                        else
                        {
                             var getUserCmd = new SqliteCommand("SELECT Id FROM Users LIMIT 1", connection, transaction);
                             object? anyId = await getUserCmd.ExecuteScalarAsync();
                             validUserId = anyId != null ? Convert.ToInt32(anyId) : 1;
                        }
                    }

                    command.Parameters.AddWithValue("@CreatedBy", validUserId);
                    command.Parameters.AddWithValue("@CreatedByName", certificate.CreatedByName ?? "");
                    
                    command.Parameters.AddWithValue("@AnalysisType", certificate.AnalysisType ?? "");
                    command.Parameters.AddWithValue("@Sender", certificate.Sender ?? "");
                    command.Parameters.AddWithValue("@Supplier", certificate.Supplier ?? "");
                    command.Parameters.AddWithValue("@Origin", certificate.Origin ?? "");
                    command.Parameters.AddWithValue("@DeclarationNumber", certificate.DeclarationNumber ?? "");
                    command.Parameters.AddWithValue("@PolicyNumber", certificate.PolicyNumber ?? "");
                    command.Parameters.AddWithValue("@NotificationNumber", certificate.NotificationNumber ?? "");
                    command.Parameters.AddWithValue("@FinancialReceiptNumber", certificate.FinancialReceiptNumber ?? "");
                    command.Parameters.AddWithValue("@SpecialistName", certificate.SpecialistName ?? "");
                    command.Parameters.AddWithValue("@SectionHeadName", certificate.SectionHeadName ?? "");
                    command.Parameters.AddWithValue("@ManagerName", certificate.ManagerName ?? "");
                    command.Parameters.AddWithValue("@Notes", certificate.Notes ?? "");
                    command.Parameters.AddWithValue("@ReceptionId", (object?)certificate.ReceptionId ?? DBNull.Value);

                    var idObj = await command.ExecuteScalarAsync();
                    if (idObj == null) throw new Exception("Failed to retrieve ID");
                    int newId = Convert.ToInt32(idObj);
                    
                    // تحديث حالة الاستلام المرتبط (إن وجد) إلى "تم إصدار شهادة"
                    if (certificate.ReceptionId.HasValue)
                    {
                        var updateReceptionQuery = "UPDATE SampleReceptions SET Status = 'تم إصدار شهادة' WHERE Id = @ReceptionId";
                        using var statusUpdateCmd = new SqliteCommand(updateReceptionQuery, connection, transaction);
                        statusUpdateCmd.Parameters.AddWithValue("@ReceptionId", certificate.ReceptionId.Value);
                        await statusUpdateCmd.ExecuteNonQueryAsync();
                    }

                    bool isEnvironmental = certificate.CertificateType.Contains("بيئية");
                    string typeCode = isEnvironmental ? "E" : "C";
                    string year = certificate.IssueDate.ToString("yy");
                    
                    // البحث عن أعلى رقم متسلسل طµط¯ط± ظپظٹ ط§ظ„ط³ظ†ط© ط§ظ„ط­ط§ظ„ظٹط© ظپظٹ ط§ظ„ظ†ط¸ط§ظ… ظƒظƒظ„ (ظ„ط¶ظ…ط§ظ† طھطھط§ط¨ط¹ ط§ظ„ط¹ط¯ط§ط¯)
                    // نفحص كافة الأنماط ط§ظ„ظ…ظ…ظƒظ†ط© ظ„ط¶ظ…ط§ظ† ط§ظ„ط­طµظˆظ„ ط¹ظ„ظ‰ ط§ظ„طھط³ظ„ط³ظ„ ط§ظ„طµط­ظٹط­
                    string patternE = $"RM-E-{year}-%";
                    string patternC = $"RM-C-{year}-%";
                    string patternU = $"RM-{year}-%"; // للنمط الموحد ط§ظ„ط°ظٹ طھظ… طھط¬ط±ط¨طھظ‡ ط³ط§ط¨ظ‚ط§ظ‹
                    
                    var maxCmd = new SqliteCommand(
                        "SELECT CertificateNumber FROM Certificates WHERE (CertificateNumber LIKE @PatE OR CertificateNumber LIKE @PatC OR CertificateNumber LIKE @PatU) AND Id != @CurrentId ORDER BY Id DESC LIMIT 1;", 
                        connection, transaction);
                    maxCmd.Parameters.AddWithValue("@PatE", patternE);
                    maxCmd.Parameters.AddWithValue("@PatC", patternC);
                    maxCmd.Parameters.AddWithValue("@PatU", patternU);
                    maxCmd.Parameters.AddWithValue("@CurrentId", newId);
                    
                    object? lastNumObj = await maxCmd.ExecuteScalarAsync();
                    int nextSequence = 1;
                    
                    if (lastNumObj != null && lastNumObj != DBNull.Value)
                    {
                        string lastNum = lastNumObj.ToString() ?? "";
                        var parts = lastNum.Split('-');
                        // الرقم المتسلسل هو ط§ظ„ط¬ط²ط، ط§ظ„ط£ط®ظٹط± ظپظٹ ط§ظ„طھظ†ط³ظٹظ‚ ط§ظ„ظ…ط¹طھظ…ط¯ (ط³ظˆط§ط، 3 ط£ظˆ 4 ط£ط¬ط²ط§ط،)
                        if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1], out int lastSeq))
                        {
                            nextSequence = lastSeq + 1;
                        }
                    }
                    
                    int sequenceInYear = nextSequence;
                    // التنسيق الهجين ط§ظ„ظ…ط¹طھظ…ط¯: RM-ط§ظ„ظ†ظˆط¹-ط§ظ„ط³ظ†ط©-ط§ظ„ط±ظ‚ظ… (4 ط®ط§ظ†ط§طھ)
                    string finalNumber = $"RM-{typeCode}-{year}-{sequenceInYear:D4}";

                    var updateCmd = new SqliteCommand("UPDATE Certificates SET CertificateNumber = @Num WHERE Id = @Id;", connection, transaction);
                    updateCmd.Parameters.AddWithValue("@Num", finalNumber);
                    updateCmd.Parameters.AddWithValue("@Id", newId);
                    await updateCmd.ExecuteNonQueryAsync();

                    if (certificate.Samples != null && certificate.Samples.Any())
                    {
                        int rootNumber = 1;
                        foreach (var sample in certificate.Samples)
                        {
                            var insertSampleQuery = @"INSERT INTO Samples 
                                (CertificateId, Root, SampleNumber, Description, MeasurementDate, Result,
                                 IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137)
                                VALUES (@CertificateId, @Root, @SampleNumber, @Description, @MeasurementDate, @Result,
                                        @IsotopeK40, @IsotopeRa226, @IsotopeTh232, @IsotopeRa, @IsotopeCs137)";
                            
                            using var sampleCmd = new SqliteCommand(insertSampleQuery, connection, transaction);
                            sampleCmd.Parameters.AddWithValue("@CertificateId", newId);
                            sampleCmd.Parameters.AddWithValue("@Root", rootNumber++);
                            sampleCmd.Parameters.AddWithValue("@SampleNumber", sample.SampleNumber ?? "");
                            sampleCmd.Parameters.AddWithValue("@Description", sample.Description ?? "");
                            sampleCmd.Parameters.AddWithValue("@MeasurementDate", sample.MeasurementDate.ToString("yyyy-MM-dd"));
                            sampleCmd.Parameters.AddWithValue("@Result", sample.Result ?? "");
                            sampleCmd.Parameters.AddWithValue("@IsotopeK40", sample.IsotopeK40 ?? "");
                            sampleCmd.Parameters.AddWithValue("@IsotopeRa226", sample.IsotopeRa226 ?? "");
                            sampleCmd.Parameters.AddWithValue("@IsotopeTh232", sample.IsotopeTh232 ?? "");
                            sampleCmd.Parameters.AddWithValue("@IsotopeRa", sample.IsotopeRa ?? "");
                            sampleCmd.Parameters.AddWithValue("@IsotopeCs137", sample.IsotopeCs137 ?? "");
                            await sampleCmd.ExecuteNonQueryAsync();
                        }
                    }

                    transaction.Commit();
                    
                    certificate.CertificateNumber = finalNumber;
                    await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "إنشاء", 
                        $"شهادة جديدة رقم {finalNumber} - رقم إخطار {certificate.NotificationNumber ?? "بدون"}", newId);
                    
                    return newId;
                }
                catch (Exception ex)
                {
                    LoggerService.LogError("Failed to add certificate async (Transaction)", ex);
                    transaction.Rollback();
                    return -1;
                }
            }, "AddCertificateAsync");
        }

        /// <summary>
        /// تحديث بيانات الشهادة والعينات بشكل غير متزامن
        /// Update certificate data and samples asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<bool> UpdateCertificateAsync(Certificate certificate)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                // Fetch old data for comparison
                var oldCert = await GetCertificateByIdAsync(certificate.Id);
                string changes = oldCert != null ? GetCertificateChanges(oldCert, certificate) : "إضافة جديدة";

                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var query = @"UPDATE Certificates SET 
                             RecipientName = @RecipientName, 
                             CertificateType = @CertificateType, 
                             Description = @Description,
                             IssueDate = @IssueDate, 
                             ExpiryDate = @ExpiryDate, 
                             IssuingAuthority = @IssuingAuthority,
                             AnalysisType = @AnalysisType,
                             Sender = @Sender,
                             Supplier = @Supplier,
                             Origin = @Origin,
                             DeclarationNumber = @DeclarationNumber,
                             PolicyNumber = @PolicyNumber,
                             NotificationNumber = @NotificationNumber,
                             FinancialReceiptNumber = @FinancialReceiptNumber,
                             SpecialistName = @SpecialistName,
                             SectionHeadName = @SectionHeadName,
                             ManagerName = @ManagerName,
                             Notes = @Notes,
                             UpdatedBy = @UpdatedBy,
                             UpdatedByName = @UpdatedByName,
                             UpdatedAt = @UpdatedAt,
                             ReceptionId = @ReceptionId
                             WHERE Id = @Id;";

                    using var command = new SqliteCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("@Id", certificate.Id);
                    command.Parameters.AddWithValue("@RecipientName", certificate.RecipientName);
                    command.Parameters.AddWithValue("@CertificateType", certificate.CertificateType);
                    command.Parameters.AddWithValue("@Description", certificate.Description ?? "");
                    command.Parameters.AddWithValue("@IssueDate", certificate.IssueDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                    command.Parameters.AddWithValue("@ExpiryDate", certificate.ExpiryDate.HasValue ? certificate.ExpiryDate.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : DBNull.Value);
                    command.Parameters.AddWithValue("@IssuingAuthority", certificate.IssuingAuthority ?? "");
                    
                    command.Parameters.AddWithValue("@AnalysisType", certificate.AnalysisType ?? "");
                    command.Parameters.AddWithValue("@Sender", certificate.Sender ?? "");
                    command.Parameters.AddWithValue("@Supplier", certificate.Supplier ?? "");
                    command.Parameters.AddWithValue("@Origin", certificate.Origin ?? "");
                    command.Parameters.AddWithValue("@DeclarationNumber", certificate.DeclarationNumber ?? "");
                    command.Parameters.AddWithValue("@PolicyNumber", certificate.PolicyNumber ?? "");
                    command.Parameters.AddWithValue("@NotificationNumber", certificate.NotificationNumber ?? "");
                    command.Parameters.AddWithValue("@FinancialReceiptNumber", certificate.FinancialReceiptNumber ?? "");
                    command.Parameters.AddWithValue("@SpecialistName", certificate.SpecialistName ?? "");
                    command.Parameters.AddWithValue("@SectionHeadName", certificate.SectionHeadName ?? "");
                    command.Parameters.AddWithValue("@ManagerName", certificate.ManagerName ?? "");
                    command.Parameters.AddWithValue("@Notes", certificate.Notes ?? "");
                    command.Parameters.AddWithValue("@UpdatedBy", (object?)certificate.UpdatedBy ?? DBNull.Value);
                    command.Parameters.AddWithValue("@UpdatedByName", (object?)certificate.UpdatedByName ?? DBNull.Value);
                    command.Parameters.AddWithValue("@UpdatedAt", certificate.UpdatedAt.HasValue ? certificate.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
                    command.Parameters.AddWithValue("@ReceptionId", (object?)certificate.ReceptionId ?? DBNull.Value);

                    await command.ExecuteNonQueryAsync();

                    if (certificate.Samples != null)
                    {
                        var deleteCmd = new SqliteCommand("DELETE FROM Samples WHERE CertificateId = @CertificateId", connection, transaction);
                        deleteCmd.Parameters.AddWithValue("@CertificateId", certificate.Id);
                        await deleteCmd.ExecuteNonQueryAsync();

                        int rootNumber = 1;
                        foreach (var sample in certificate.Samples)
                        {
                            var insertSampleQuery = @"INSERT INTO Samples 
                                (CertificateId, Root, SampleNumber, Description, MeasurementDate, Result,
                                 IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137)
                                VALUES (@CertificateId, @Root, @SampleNumber, @Description, @MeasurementDate, @Result,
                                        @IsotopeK40, @IsotopeRa226, @IsotopeTh232, @IsotopeRa, @IsotopeCs137)";
                            
                            using var sampleCmd = new SqliteCommand(insertSampleQuery, connection, transaction);
                            sampleCmd.Parameters.AddWithValue("@CertificateId", certificate.Id);
                            sampleCmd.Parameters.AddWithValue("@Root", rootNumber++);
                            sampleCmd.Parameters.AddWithValue("@SampleNumber", sample.SampleNumber ?? "");
                            sampleCmd.Parameters.AddWithValue("@Description", sample.Description ?? "");
                            sampleCmd.Parameters.AddWithValue("@MeasurementDate", sample.MeasurementDate.ToString("yyyy-MM-dd"));
                            sampleCmd.Parameters.AddWithValue("@Result", sample.Result ?? "");
                            sampleCmd.Parameters.AddWithValue("@IsotopeK40", sample.IsotopeK40 ?? "");
                            sampleCmd.Parameters.AddWithValue("@IsotopeRa226", sample.IsotopeRa226 ?? "");
                            sampleCmd.Parameters.AddWithValue("@IsotopeTh232", sample.IsotopeTh232 ?? "");
                            sampleCmd.Parameters.AddWithValue("@IsotopeRa", sample.IsotopeRa ?? "");
                            sampleCmd.Parameters.AddWithValue("@IsotopeCs137", sample.IsotopeCs137 ?? "");
                            await sampleCmd.ExecuteNonQueryAsync();
                        }
                    }

                    transaction.Commit();
                    
                    await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "تعديل", 
                        $"{changes} (رقم الشهادة: {certificate.CertificateNumber})", certificate.Id);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    LoggerService.LogError("Failed to update certificate async (Transaction)", ex);
                    transaction.Rollback();
                    return false;
                }
            }, "UpdateCertificateAsync");
        }



        /// <summary>
        /// الحصول على سجل تاريخ العمليات لشهادة معينة
        /// </summary>
        public System.Threading.Tasks.Task<List<AuditLog>> GetCertificateHistoryAsync(int certificateId)
        {
            return _db.GetLogsByReferenceIdAsync(certificateId);
        }

        /// <summary>
        /// إنشاء رقم شهادة فريد
        /// Generate unique certificate number
        /// </summary>
        public string GenerateCertificateNumber()
        {
            return $"CERT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }
    }
}
