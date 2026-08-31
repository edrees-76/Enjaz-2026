using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Enjaz.Models;
using Enjaz.Helpers;
using Dapper;
using System.Linq;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// مستودع الشهادات - إدارة عمليات الشهادات والعينات
    /// Certificate Repository - Manages Certificates and Samples operations
    /// </summary>
    public partial class CertificateRepository : ICertificateRepository
    {
        private readonly DatabaseService _db;
        private readonly UserService _userService;
        private readonly ISampleRepository _sampleRepository;
        private readonly Services.Caching.ICacheService _cacheService;

        private static void ConvertToLocalTime(Certificate certificate)
        {
            if (certificate == null) return;

            if (certificate.CreatedAt.Kind == DateTimeKind.Unspecified)
                certificate.CreatedAt = DateTime.SpecifyKind(certificate.CreatedAt, DateTimeKind.Utc).ToLocalTime();

            if (certificate.UpdatedAt.HasValue && certificate.UpdatedAt.Value.Kind == DateTimeKind.Unspecified)
                certificate.UpdatedAt = DateTime.SpecifyKind(certificate.UpdatedAt.Value, DateTimeKind.Utc).ToLocalTime();
        }

        private static void ConvertAllToLocalTime(List<Certificate> certificates)
        {
            if (certificates == null) return;
            foreach (var c in certificates) ConvertToLocalTime(c);
        }

        public CertificateRepository(DatabaseService db, UserService userService, ISampleRepository sampleRepository, Services.Caching.ICacheService cacheService)
        {
            _db = db;
            _userService = userService;
            _sampleRepository = sampleRepository;
            _cacheService = cacheService;
        }

        public CertificateRepository(DatabaseService db, UserService userService)
            : this(db, userService, new SampleRepository(db), new Services.Caching.MemoryCacheService(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions())))
        {
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

            var issueDate = SafeParseDate(reader.GetString(5), "yyyy-MM-dd HH:mm:ss");
            var createdAt = SafeParseDate(reader.GetString(10), "yyyy-MM-dd HH:mm:ss");

            if (issueDate.TimeOfDay == TimeSpan.Zero && createdAt.TimeOfDay != TimeSpan.Zero)
            {
                issueDate = issueDate.Date.Add(createdAt.TimeOfDay);
            }

            var certificate = new Certificate
            {
                Id = reader.GetInt32(0),
                CertificateNumber = reader.GetString(1),
                RecipientName = reader.GetString(2),
                CertificateType = reader.GetString(3),
                Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                IssueDate = issueDate,
                ExpiryDate = reader.IsDBNull(6) ? null : SafeParseDate(reader.GetString(6), "yyyy-MM-dd"),
                IssuingAuthority = reader.IsDBNull(7) ? "" : reader.GetString(7),
                CreatedBy = reader.GetInt32(8),
                CreatedByName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                CreatedAt = createdAt,
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
                SampleCount = columnCount > 26 && !reader.IsDBNull(26) ? reader.GetInt32(26) : 0,
                ReceptionId = columnCount > 27 && !reader.IsDBNull(27) ? (int?)reader.GetInt32(27) : null
            };

            // Apply UTC to Local conversion
            ConvertToLocalTime(certificate);
            
            return certificate;
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
                              c.CreatedBy, c.CreatedByName, c.CreatedAt,
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
                              c.CreatedBy, c.CreatedByName, c.CreatedAt,
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
                              c.CreatedBy, c.CreatedByName, c.CreatedAt,
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
                              c.CreatedBy, c.CreatedByName, c.CreatedAt,
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

                    int validUserId = certificate.CreatedBy;
                    // Validate CreatedBy using Dapper
                    var userCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM Users WHERE Id = @Uid", new { Uid = certificate.CreatedBy }, transaction);
                    if (userCount == 0)
                    {
                        var adminId = await connection.ExecuteScalarAsync<int?>("SELECT Id FROM Users WHERE Role = 2 LIMIT 1", null, transaction);
                        if (adminId.HasValue)
                            validUserId = adminId.Value;
                        else
                        {
                             var anyId = await connection.ExecuteScalarAsync<int?>("SELECT Id FROM Users LIMIT 1", null, transaction);
                             validUserId = anyId ?? 1;
                        }
                    }

                    var parameters = new
                    {
                        CertificateNumber = "TEMP-" + Guid.NewGuid(),
                        certificate.RecipientName,
                        certificate.CertificateType,
                        Description = certificate.Description ?? "",
                        IssueDate = certificate.IssueDate.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                        ExpiryDate = certificate.ExpiryDate.HasValue ? certificate.ExpiryDate.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : null,
                        IssuingAuthority = certificate.IssuingAuthority ?? "",
                        CreatedBy = validUserId,
                        CreatedByName = certificate.CreatedByName ?? "",
                        AnalysisType = certificate.AnalysisType ?? "",
                        Sender = certificate.Sender ?? "",
                        Supplier = certificate.Supplier ?? "",
                        Origin = certificate.Origin ?? "",
                        DeclarationNumber = certificate.DeclarationNumber ?? "",
                        PolicyNumber = certificate.PolicyNumber ?? "",
                        NotificationNumber = certificate.NotificationNumber ?? "",
                        FinancialReceiptNumber = certificate.FinancialReceiptNumber ?? "",
                        SpecialistName = certificate.SpecialistName ?? "",
                        SectionHeadName = certificate.SectionHeadName ?? "",
                        ManagerName = certificate.ManagerName ?? "",
                        Notes = certificate.Notes ?? "",
                        ReceptionId = certificate.ReceptionId
                    };

                    int newId = await connection.ExecuteScalarAsync<int>(query, parameters, transaction);
                    
                    // تحديث حالة الاستلام المرتبط (إن وجد) إلى "تم إصدار شهادة"
                    if (certificate.ReceptionId.HasValue)
                    {
                        await connection.ExecuteAsync("UPDATE SampleReceptions SET Status = 'تم إصدار شهادة' WHERE Id = @ReceptionId", 
                            new { ReceptionId = certificate.ReceptionId.Value }, transaction);
                    }

                    bool isEnvironmental = certificate.CertificateType.Contains("بيئية");
                    string typeCode = isEnvironmental ? "E" : "C";
                    string year = certificate.IssueDate.ToString("yy");
                    
                    string patternE = $"RM-E-{year}-%";
                    string patternC = $"RM-C-{year}-%";
                    string patternU = $"RM-{year}-%"; // للنمط الموحد القديم
                    
                    var lastNumStr = await connection.ExecuteScalarAsync<string>(
                        "SELECT CertificateNumber FROM Certificates WHERE (CertificateNumber LIKE @PatE OR CertificateNumber LIKE @PatC OR CertificateNumber LIKE @PatU) AND Id != @CurrentId ORDER BY Id DESC LIMIT 1;", 
                        new { PatE = patternE, PatC = patternC, PatU = patternU, CurrentId = newId }, transaction);
                    
                    int nextSequence = 1;
                    if (!string.IsNullOrEmpty(lastNumStr))
                    {
                        var parts = lastNumStr.Split('-');
                        if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1], out int lastSeq))
                        {
                            nextSequence = lastSeq + 1;
                        }
                    }
                    
                    string finalNumber = $"RM-{typeCode}-{year}-{nextSequence:D4}";

                    await connection.ExecuteAsync("UPDATE Certificates SET CertificateNumber = @Num WHERE Id = @Id;", 
                        new { Num = finalNumber, Id = newId }, transaction);

                    if (certificate.Samples != null && certificate.Samples.Count > 0)
                    {
                        int rootNumber = 1;
                        var insertSampleQuery = @"INSERT INTO Samples 
                            (CertificateId, Root, SampleNumber, Description, MeasurementDate, Result,
                             IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137)
                            VALUES (@CertificateId, @Root, @SampleNumber, @Description, @MeasurementDate, @Result,
                                    @IsotopeK40, @IsotopeRa226, @IsotopeTh232, @IsotopeRa, @IsotopeCs137)";
                                    
                        var sampleParams = certificate.Samples.Select(s => new
                        {
                            CertificateId = newId,
                            Root = rootNumber++,
                            SampleNumber = s.SampleNumber ?? "",
                            Description = s.Description ?? "",
                            MeasurementDate = s.MeasurementDate.ToString("yyyy-MM-dd"),
                            Result = s.Result ?? "",
                            IsotopeK40 = s.IsotopeK40 ?? "",
                            IsotopeRa226 = s.IsotopeRa226 ?? "",
                            IsotopeTh232 = s.IsotopeTh232 ?? "",
                            IsotopeRa = s.IsotopeRa ?? "",
                            IsotopeCs137 = s.IsotopeCs137 ?? ""
                        });
                        
                        await connection.ExecuteAsync(insertSampleQuery, sampleParams, transaction);
                    }

                    transaction.Commit();
                    
                    certificate.CertificateNumber = finalNumber;
                    await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "إنشاء", 
                        $"شهادة جديدة رقم {finalNumber} - رقم إخطار {certificate.NotificationNumber ?? "بدون"}", newId);
                    
                    _cacheService.Clear(); // Cache invalidation
                    
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

                    var parameters = new
                    {
                        certificate.Id,
                        certificate.RecipientName,
                        certificate.CertificateType,
                        Description = certificate.Description ?? "",
                        IssueDate = certificate.IssueDate.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                        ExpiryDate = certificate.ExpiryDate.HasValue ? certificate.ExpiryDate.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : null,
                        IssuingAuthority = certificate.IssuingAuthority ?? "",
                        AnalysisType = certificate.AnalysisType ?? "",
                        Sender = certificate.Sender ?? "",
                        Supplier = certificate.Supplier ?? "",
                        Origin = certificate.Origin ?? "",
                        DeclarationNumber = certificate.DeclarationNumber ?? "",
                        PolicyNumber = certificate.PolicyNumber ?? "",
                        NotificationNumber = certificate.NotificationNumber ?? "",
                        FinancialReceiptNumber = certificate.FinancialReceiptNumber ?? "",
                        SpecialistName = certificate.SpecialistName ?? "",
                        SectionHeadName = certificate.SectionHeadName ?? "",
                        ManagerName = certificate.ManagerName ?? "",
                        Notes = certificate.Notes ?? "",
                        UpdatedBy = certificate.UpdatedBy,
                        UpdatedByName = certificate.UpdatedByName,
                        UpdatedAt = certificate.UpdatedAt.HasValue ? certificate.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                        ReceptionId = certificate.ReceptionId
                    };
                    
                    await connection.ExecuteAsync(query, parameters, transaction);

                    if (certificate.Samples != null)
                    {
                        await connection.ExecuteAsync("DELETE FROM Samples WHERE CertificateId = @CertificateId", new { CertificateId = certificate.Id }, transaction);

                        if (certificate.Samples.Count > 0)
                        {
                            int rootNumber = 1;
                            var insertSampleQuery = @"INSERT INTO Samples 
                                (CertificateId, Root, SampleNumber, Description, MeasurementDate, Result,
                                 IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137)
                                VALUES (@CertificateId, @Root, @SampleNumber, @Description, @MeasurementDate, @Result,
                                        @IsotopeK40, @IsotopeRa226, @IsotopeTh232, @IsotopeRa, @IsotopeCs137)";
                                        
                            var sampleParams = certificate.Samples.Select(s => new
                            {
                                CertificateId = certificate.Id,
                                Root = rootNumber++,
                                SampleNumber = s.SampleNumber ?? "",
                                Description = s.Description ?? "",
                                MeasurementDate = s.MeasurementDate.ToString("yyyy-MM-dd"),
                                Result = s.Result ?? "",
                                IsotopeK40 = s.IsotopeK40 ?? "",
                                IsotopeRa226 = s.IsotopeRa226 ?? "",
                                IsotopeTh232 = s.IsotopeTh232 ?? "",
                                IsotopeRa = s.IsotopeRa ?? "",
                                IsotopeCs137 = s.IsotopeCs137 ?? ""
                            });
                            
                            await connection.ExecuteAsync(insertSampleQuery, sampleParams, transaction);
                        }
                    }

                    transaction.Commit();
                    
                    await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "تعديل", 
                        $"{changes} (رقم الشهادة: {certificate.CertificateNumber})", certificate.Id);
                    
                    _cacheService.Clear(); // Cache invalidation
                    
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

        #region Sample Uniqueness Validation — فحص تفرد رقم العينة

        /// <summary>
        /// فحص تفرد رقم العينة بالنسبة للجهة المرسلة وسنة الإصدار (IssueDate.Year)
        /// Check sample uniqueness for sender and fiscal year
        /// </summary>
        public System.Threading.Tasks.Task<SampleUniquenessResult> CheckSampleUniquenessAsync(
            string sampleNumber,
            string sender,
            int year,
            int? excludeCertificateId = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                string normSampleNumber = SampleValidationHelper.NormalizeSampleNumber(sampleNumber);
                string normSender = SampleValidationHelper.NormalizeSender(sender);

                if (string.IsNullOrEmpty(normSampleNumber) || string.IsNullOrEmpty(normSender))
                {
                    return new SampleUniquenessResult { Status = SampleCheckResult.Unique };
                }

                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                // 1. فحص الشهادات النشطة لنفس السنة
                var query = @"SELECT c.Id, c.CertificateNumber, c.IssueDate, c.Sender, s.SampleNumber
                              FROM Samples s
                              INNER JOIN Certificates c ON s.CertificateId = c.Id
                              WHERE strftime('%Y', c.IssueDate) = @Year";

                if (excludeCertificateId.HasValue)
                {
                    query += " AND c.Id != @ExcludeId";
                }

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Year", year.ToString());
                    if (excludeCertificateId.HasValue)
                    {
                        command.Parameters.AddWithValue("@ExcludeId", excludeCertificateId.Value);
                    }

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int certId = reader.GetInt32(0);
                        string certNumber = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        string issueDateStr = reader.GetString(2);
                        string rowSender = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        string rowSampleNumber = reader.IsDBNull(4) ? "" : reader.GetString(4);

                        if (SampleValidationHelper.NormalizeSender(rowSender) == normSender &&
                            SampleValidationHelper.NormalizeSampleNumber(rowSampleNumber) == normSampleNumber)
                        {
                            DateTime issueDate = DateTime.TryParse(issueDateStr, out var parsedDate) ? parsedDate : DateTime.MinValue;
                            return new SampleUniquenessResult
                            {
                                Status = SampleCheckResult.DuplicateActive,
                                CertificateId = certId,
                                CertificateNumber = certNumber,
                                IssueDate = issueDate,
                                Sender = rowSender,
                                SampleNumber = rowSampleNumber
                            };
                        }
                    }
                }

                // 2. فحص السجلات المحذوفة (من جدول AuditLogs)
                try
                {
                    var auditQuery = @"SELECT Timestamp, Details FROM AuditLogs 
                                       WHERE (Action = 'حذف' OR Action LIKE '%حذف%' OR Action = 'Delete')
                                       AND strftime('%Y', Timestamp) = @Year";
                    using var auditCmd = new SqliteCommand(auditQuery, connection);
                    auditCmd.Parameters.AddWithValue("@Year", year.ToString());
                    using var auditReader = await auditCmd.ExecuteReaderAsync();
                    while (await auditReader.ReadAsync())
                    {
                        string timestampStr = auditReader.GetString(0);
                        string details = auditReader.IsDBNull(1) ? "" : auditReader.GetString(1);

                        if (!string.IsNullOrEmpty(details) &&
                            details.Contains(sampleNumber) &&
                            SampleValidationHelper.NormalizeSender(details).Contains(normSender))
                        {
                            DateTime delDate = DateTime.TryParse(timestampStr, out var pDate) ? pDate : DateTime.MinValue;
                            return new SampleUniquenessResult
                            {
                                Status = SampleCheckResult.FoundInDeleted,
                                CertificateNumber = "محذوفة",
                                IssueDate = delDate,
                                Sender = sender,
                                SampleNumber = sampleNumber
                            };
                        }
                    }
                }
                catch
                {
                    // Ignore audit log search error if non-critical
                }

                return new SampleUniquenessResult { Status = SampleCheckResult.Unique };
            }, "CheckSampleUniquenessAsync");
        }

        #endregion

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
