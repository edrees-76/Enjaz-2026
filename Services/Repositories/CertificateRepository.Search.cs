using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;
using Enjaz.Models;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// البحث والتحقق والإكمال التلقائي — Search, Validation & AutoComplete
    /// </summary>
    public partial class CertificateRepository
    {
        #region Search Operations

        /// <summary>
        /// الحصول على عدد الشهادات المطابقة للبحث
        /// Get count of certificates matching search criteria
        /// </summary>
        public Task<int> GetSearchCertificatesCountAsync(string searchTerm, string searchCriteria, DateTime? startDate = null, DateTime? endDate = null, bool showDeleted = false)
        {
             return _db.ExecuteWithRetryAsync(async () =>
             {
                 using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                
                var query = "SELECT COUNT(*) FROM Certificates c LEFT JOIN Users u ON c.CreatedBy = u.Id WHERE 1=1";
                
                var (sqlFilter, parameters) = BuildSearchFilter(searchTerm, searchCriteria, startDate, endDate);
                query += sqlFilter;

                using var command = new SqliteCommand(query, connection);
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value);
                }

                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }, "GetSearchCertificatesCountAsync");
        }

        private (string sql, Dictionary<string, object> parameters) BuildSearchFilter(string searchTerm, string searchCriteria, DateTime? startDate, DateTime? endDate)
        {
            var sql = "";
            var parameters = new Dictionary<string, object>();

            // 1. Date filters are additive if they have values
            if (startDate.HasValue)
            {
                sql += " AND date(c.IssueDate) >= date(@StartDate)";
                parameters.Add("@StartDate", startDate.Value.ToString("yyyy-MM-dd"));
            }
            if (endDate.HasValue)
            {
                sql += " AND date(c.IssueDate) <= date(@EndDate)";
                parameters.Add("@EndDate", endDate.Value.ToString("yyyy-MM-dd"));
            }

            // 2. Text filters
            if (searchCriteria != "التاريخ" && !string.IsNullOrWhiteSpace(searchTerm))
            {
                string normSearch = searchTerm
                    .Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا")
                    .Replace("ة", "ه").Replace("ى", "ي");
                
                parameters.Add("@SearchTerm", $"%{normSearch}%");

                // Helper to apply normalization to a column in SQL
                string SqlNorm(string col) => $"REPLACE(REPLACE(REPLACE(REPLACE(REPLACE({col}, 'أ', 'ا'), 'إ', 'ا'), 'آ', 'ا'), 'ة', 'ه'), 'ى', 'ي')";

                switch (searchCriteria)
                {
                    case "رقم العينة": 
                        sql += $" AND EXISTS (SELECT 1 FROM Samples s WHERE s.CertificateId = c.Id AND {SqlNorm("s.SampleNumber")} LIKE @SearchTerm)"; 
                        break;
                    case "رقم الشهادة": 
                        sql += $" AND {SqlNorm("c.CertificateNumber")} LIKE @SearchTerm"; 
                        break;
                    case "رقم الاخطار": 
                        sql += $" AND {SqlNorm("c.NotificationNumber")} LIKE @SearchTerm"; 
                        break;
                    case "رقم الاقرار الجمركى": 
                        sql += $" AND {SqlNorm("c.DeclarationNumber")} LIKE @SearchTerm"; 
                        break;
                    case "الجهة المرسلة": 
                        sql += $" AND {SqlNorm("c.Sender")} LIKE @SearchTerm"; 
                        break;
                    case "المورد": 
                        sql += $" AND {SqlNorm("c.Supplier")} LIKE @SearchTerm"; 
                        break;
                    case "رقم الايصال المالى": 
                        sql += $" AND {SqlNorm("c.FinancialReceiptNumber")} LIKE @SearchTerm"; 
                        break;
                    case "رقم البوليصة": 
                        sql += $" AND {SqlNorm("c.PolicyNumber")} LIKE @SearchTerm"; 
                        break;
                    case "اسم المستخدم": 
                        sql += $" AND ({SqlNorm("u.Username")} LIKE @SearchTerm OR {SqlNorm("u.FullName")} LIKE @SearchTerm)"; 
                        break;
                    default: // الكل
                        sql += $@" AND (
                            {SqlNorm("c.RecipientName")} LIKE @SearchTerm OR 
                            {SqlNorm("c.CertificateNumber")} LIKE @SearchTerm OR 
                            {SqlNorm("c.Supplier")} LIKE @SearchTerm OR 
                            {SqlNorm("c.NotificationNumber")} LIKE @SearchTerm OR 
                            {SqlNorm("c.DeclarationNumber")} LIKE @SearchTerm OR 
                            {SqlNorm("c.Sender")} LIKE @SearchTerm OR 
                            {SqlNorm("c.FinancialReceiptNumber")} LIKE @SearchTerm OR 
                            {SqlNorm("c.PolicyNumber")} LIKE @SearchTerm OR
                            EXISTS (SELECT 1 FROM Samples s WHERE s.CertificateId = c.Id AND {SqlNorm("s.SampleNumber")} LIKE @SearchTerm))"; 
                        break;
                }
            }

            return (sql, parameters);
        }

        /// <summary>
        /// البحث عن الشهادات بشكل غير متزامن مع تقسيم الصفحات
        /// Search certificates asynchronously with pagination
        /// </summary>
        public Task<List<Certificate>> SearchCertificatesAsync(string searchTerm, string searchCriteria, int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null, bool showDeleted = false)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var certificates = new List<Certificate>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                int offset = (pageNumber - 1) * pageSize;
                
                var query = @"SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, 
                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, 
                              c.CreatedBy, u.FullName, datetime(c.CreatedAt, 'localtime'),
                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,
                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,
                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,
                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt,
                              (SELECT COUNT(*) FROM Samples s WHERE s.CertificateId = c.Id) AS SampleCount,
                              c.ReceptionId
                              FROM Certificates c 
                              LEFT JOIN Users u ON c.CreatedBy = u.Id
                              WHERE 1=1";
                
                var (sqlFilter, parameters) = BuildSearchFilter(searchTerm, searchCriteria, startDate, endDate);
                query += sqlFilter;

                query += " ORDER BY c.CreatedAt DESC LIMIT @Limit OFFSET @Offset;";

                // Search uses manual SqliteCommand due to dynamic filter building
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Limit", pageSize);
                command.Parameters.AddWithValue("@Offset", offset);

                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value);
                }

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    certificates.Add(new Certificate
                    {
                        Id = reader.GetInt32(0),
                        CertificateNumber = reader.GetString(1),
                        RecipientName = reader.GetString(2),
                        CertificateType = reader.GetString(3),
                        Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        IssueDate = DateTime.ParseExact(reader.GetString(5), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                        ExpiryDate = reader.IsDBNull(6) ? null : (DateTime?)DateTime.ParseExact(reader.GetString(6), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                        IssuingAuthority = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        CreatedBy = reader.GetInt32(8),
                        CreatedByName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        CreatedAt = DateTime.ParseExact(reader.GetString(10), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                        
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
                        UpdatedAt = reader.IsDBNull(25) ? null : (DateTime?)DateTime.ParseExact(reader.GetString(25), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                        SampleCount = reader.GetInt32(26),
                        ReceptionId = reader.IsDBNull(27) ? null : (int?)reader.GetInt32(27)
                    });
                }
                return certificates;
            }, "SearchCertificatesAsync");
        }

        public Task<List<Certificate>> GetCertificatesBySenderAndDateAsync(string sender, DateTime startDate, DateTime endDate)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var certificates = new List<Certificate>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                // 1. التطبيع العربي (Normalization) لاسم الجهة
                string normSender = sender
                    .Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا")
                    .Replace("ة", "ه").Replace("ى", "ي");

                // دالة التطبيع في SQL
                string SqlNorm(string col) => $"REPLACE(REPLACE(REPLACE(REPLACE(REPLACE({col}, 'أ', 'ا'), 'إ', 'ا'), 'آ', 'ا'), 'ة', 'ه'), 'ى', 'ي')";

                var query = $@"SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, 
                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, 
                              c.CreatedBy, COALESCE(u.FullName, ''), datetime(c.CreatedAt, 'localtime'),
                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,
                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,
                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,
                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt,
                              (SELECT COUNT(*) FROM Samples s WHERE s.CertificateId = c.Id) AS SampleCount
                              FROM Certificates c 
                              LEFT JOIN Users u ON c.CreatedBy = u.Id
                              WHERE {SqlNorm("c.Sender")} LIKE @Sender
                              AND date(c.IssueDate) >= date(@StartDate)
                              AND date(c.IssueDate) <= date(@EndDate)
                              ORDER BY c.IssueDate ASC;";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Sender", $"%{normSender}%");
                command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    certificates.Add(MapCertificateFromReader(reader));
                }
                
                // 2. جلب العينات لكل شهادة باستخدام Dapper (لعرض أرقام العينات في رسالة الإحالة)
                if (certificates.Count > 0)
                {
                    var certIds = certificates.Select(c => c.Id).ToList();
                    var allSamples = await connection.QueryAsync<Sample>(
                        "SELECT Id, CertificateId, Root, SampleNumber, Description, MeasurementDate, Result, IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137 FROM Samples WHERE CertificateId IN @Ids ORDER BY Root ASC",
                        new { Ids = certIds });
                    
                    var samplesByCert = allSamples.GroupBy(s => s.CertificateId).ToDictionary(g => g.Key, g => g.ToList());
                    foreach (var cert in certificates)
                    {
                        cert.Samples = new ObservableCollection<Sample>(
                            samplesByCert.ContainsKey(cert.Id) ? samplesByCert[cert.Id] : new List<Sample>());
                    }
                }
                
                return certificates;
            }, "GetCertificatesBySenderAndDateAsync");
        }

        #endregion

        #region AutoComplete & Validation

        /// <summary>
        /// الحصول على القيم الفريدة لعمود معين لاقتراحات الملء التلقائي
        /// Get unique values for a specific column for AutoComplete suggestions
        /// </summary>
        public Task<List<string>> GetDistinctFieldValuesAsync(string columnName)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                // Validate column name against a whitelist to prevent SQL injection
                var allowedColumns = new HashSet<string> { 
                    "RecipientName", "Sender", "Supplier", "Origin", "AnalysisType", 
                    "SpecialistName", "SectionHeadName", "ManagerName", "Result" 
                };

                if (!allowedColumns.Contains(columnName))
                    return new List<string>();

                var query = $"SELECT DISTINCT {columnName} FROM Certificates WHERE {columnName} IS NOT NULL AND {columnName} != '' ORDER BY {columnName} ASC";
                
                // If the column is "Result", check the Samples table instead
                if (columnName == "Result")
                {
                    query = $"SELECT DISTINCT {columnName} FROM Samples WHERE {columnName} IS NOT NULL AND {columnName} != '' ORDER BY {columnName} ASC";
                }

                var results = await connection.QueryAsync<string>(query);
                return results.AsList();
            }, "GetDistinctFieldValuesAsync");
        }

        public Task<bool> IsFinancialReceiptDuplicateAsync(string receiptNumber, int? excludeId = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(receiptNumber)) return false;

                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM Certificates WHERE FinancialReceiptNumber = @ReceiptNumber";
                if (excludeId.HasValue)
                {
                    query += " AND Id != @ExcludeId";
                }

                var count = await connection.ExecuteScalarAsync<int>(query, new 
                { 
                    ReceiptNumber = receiptNumber.Trim(), 
                    ExcludeId = excludeId ?? 0 
                });
                return count > 0;
            }, "IsFinancialReceiptDuplicateAsync");
        }

        #endregion
    }
}
