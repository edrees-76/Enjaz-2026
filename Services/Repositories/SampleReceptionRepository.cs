using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Enjaz.Models;
using Enjaz.Helpers;
using System.Threading.Tasks;
using Dapper;

namespace Enjaz.Services.Repositories
{
    public class SampleReceptionRepository : IReceptionRepository
    {
        /// <summary>
        /// تحويل توقيت UTC إلى التوقيت المحلي بعد جلب البيانات من قاعدة البيانات
        /// </summary>
        private static void ConvertToLocalTime(SampleReception reception)
        {
            // CreatedAt مخزّن بتوقيت UTC (من CURRENT_TIMESTAMP) — نحوّله للمحلي
            if (reception.CreatedAt.Kind == DateTimeKind.Unspecified)
            {
                reception.CreatedAt = DateTime.SpecifyKind(reception.CreatedAt, DateTimeKind.Utc).ToLocalTime();
            }
            // UpdatedAt — نفس المعالجة
            if (reception.UpdatedAt.HasValue && reception.UpdatedAt.Value.Kind == DateTimeKind.Unspecified)
            {
                reception.UpdatedAt = DateTime.SpecifyKind(reception.UpdatedAt.Value, DateTimeKind.Utc).ToLocalTime();
            }
        }

        private static void ConvertAllToLocalTime(List<SampleReception> receptions)
        {
            foreach (var r in receptions)
                ConvertToLocalTime(r);
        }

        private readonly DatabaseService _db;
        private readonly UserService _userService;
        private readonly Services.Caching.ICacheService _cacheService;

        public SampleReceptionRepository(DatabaseService db, UserService userService, Services.Caching.ICacheService cacheService)
        {
            _db = db;
            _userService = userService;
            _cacheService = cacheService;
        }

        public Task<List<string>> GetDistinctSendersAsync()
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                var query = "SELECT DISTINCT Sender FROM SampleReceptions WHERE Sender IS NOT NULL AND Sender != ''";
                var senders = await connection.QueryAsync<string>(query);
                return senders.AsList();
            }, "GetDistinctSendersAsync");
        }

        public Task<int> AddSampleReceptionAsync(SampleReception reception)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var query = @"INSERT INTO SampleReceptions 
                                 (AnalysisRequestNumber, NotificationNumber, DeclarationNumber, Supplier, Sender, Origin, 
                                  PolicyNumber, FinancialReceiptNumber, CertificateType, Date, Status, 
                                  CreatedBy, CreatedByName)
                                 VALUES (@AnalysisRequestNumber, @NotificationNumber, @DeclarationNumber, @Supplier, @Sender, @Origin,
                                         @PolicyNumber, @FinancialReceiptNumber, @CertificateType, @Date, @Status,
                                         @CreatedBy, @CreatedByName);
                                 SELECT last_insert_rowid();";

                    var parameters = new
                    {
                        reception.AnalysisRequestNumber,
                        NotificationNumber = reception.NotificationNumber ?? "",
                        DeclarationNumber = reception.DeclarationNumber ?? "",
                        Supplier = reception.Supplier ?? "",
                        Sender = reception.Sender ?? "",
                        Origin = reception.Origin ?? "",
                        PolicyNumber = reception.PolicyNumber ?? "",
                        FinancialReceiptNumber = reception.FinancialReceiptNumber ?? "",
                        reception.CertificateType,
                        Date = reception.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                        reception.Status,
                        CreatedBy = _userService.CurrentUser?.Id ?? 1,
                        CreatedByName = _userService.CurrentUser?.FullName ?? "النظام"
                    };

                    int newId = await connection.ExecuteScalarAsync<int>(query, parameters, transaction);

                    if (reception.Samples != null && reception.Samples.Any())
                    {
                        var insertSampleQuery = @"INSERT INTO ReceptionSamples 
                            (ReceptionId, SampleNumber, Description)
                            VALUES (@ReceptionId, @SampleNumber, @Description)";
                        
                        var sampleParams = reception.Samples.Select(s => new
                        {
                            ReceptionId = newId,
                            SampleNumber = s.SampleNumber ?? "",
                            Description = s.Description ?? ""
                        });

                        await connection.ExecuteAsync(insertSampleQuery, sampleParams, transaction);
                    }

                    transaction.Commit();
                    
                    await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "إنشاء استلام", 
                        $"إيصال استلام جديد رقم {reception.AnalysisRequestNumber}", newId);
                    
                    _cacheService.Clear();
                    
                    return newId;
                }
                catch (Exception ex)
                {
                    LoggerService.LogError("Failed to add sample reception async (Transaction)", ex);
                    transaction.Rollback();
                    return -1;
                }
            }, "AddSampleReceptionAsync");
        }

        public Task<bool> UpdateSampleReceptionAsync(SampleReception reception)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var query = @"UPDATE SampleReceptions SET 
                             AnalysisRequestNumber = @AnalysisRequestNumber,
                             NotificationNumber = @NotificationNumber,
                             DeclarationNumber = @DeclarationNumber,
                             Supplier = @Supplier,
                             Sender = @Sender,
                             Origin = @Origin,
                             PolicyNumber = @PolicyNumber,
                             FinancialReceiptNumber = @FinancialReceiptNumber,
                             CertificateType = @CertificateType,
                             Date = @Date,
                             Status = @Status,
                             UpdatedBy = @UpdatedBy,
                             UpdatedByName = @UpdatedByName,
                             UpdatedAt = CURRENT_TIMESTAMP
                             WHERE Id = @Id;";

                    var parameters = new
                    {
                        reception.Id,
                        reception.AnalysisRequestNumber,
                        NotificationNumber = reception.NotificationNumber ?? "",
                        DeclarationNumber = reception.DeclarationNumber ?? "",
                        Supplier = reception.Supplier ?? "",
                        Sender = reception.Sender ?? "",
                        Origin = reception.Origin ?? "",
                        PolicyNumber = reception.PolicyNumber ?? "",
                        FinancialReceiptNumber = reception.FinancialReceiptNumber ?? "",
                        reception.CertificateType,
                        Date = reception.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                        reception.Status,
                        UpdatedBy = _userService.CurrentUser?.Id ?? 1,
                        UpdatedByName = _userService.CurrentUser?.FullName ?? "النظام"
                    };

                    await connection.ExecuteAsync(query, parameters, transaction);

                    if (reception.Samples != null)
                    {
                        await connection.ExecuteAsync("DELETE FROM ReceptionSamples WHERE ReceptionId = @ReceptionId", new { ReceptionId = reception.Id }, transaction);

                        if (reception.Samples.Any())
                        {
                            var insertSampleQuery = @"INSERT INTO ReceptionSamples 
                                (ReceptionId, SampleNumber, Description)
                                VALUES (@ReceptionId, @SampleNumber, @Description)";
                                
                            var sampleParams = reception.Samples.Select(s => new
                            {
                                ReceptionId = reception.Id,
                                SampleNumber = s.SampleNumber ?? "",
                                Description = s.Description ?? ""
                            });
                            
                            await connection.ExecuteAsync(insertSampleQuery, sampleParams, transaction);
                        }
                    }

                    transaction.Commit();
                    
                    await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "تعديل استلام", 
                        $"تعديل بيانات الاستلام رقم طلب التحليل {reception.AnalysisRequestNumber}", reception.Id);
                    
                    _cacheService.Clear();
                    
                    return true;
                }
                catch (Exception ex)
                {
                    LoggerService.LogError("Failed to update sample reception async", ex);
                    transaction.Rollback();
                    return false;
                }
            }, "UpdateSampleReceptionAsync");
        }

        public Task<bool> DeleteSampleReceptionAsync(int id)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                var query = "DELETE FROM SampleReceptions WHERE Id = @Id;";
                int rows = await connection.ExecuteAsync(query, new { Id = id });
                
                if (rows > 0)
                {
                    await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "حذف استلام", 
                        $"حذف استلام برقم معرف {id}", id);
                    _cacheService.Clear();
                    return true;
                }
                return false;
            }, "DeleteSampleReceptionAsync");
        }

        public Task<List<SampleReception>> GetAllReceptionsAsync()
        {
             return _db.ExecuteWithRetryAsync(async () =>
             {
                 using var connection = new SqliteConnection(_db.ConnectionString);
                 var query = "SELECT * FROM SampleReceptions ORDER BY Date DESC LIMIT 200;";
                 var receptions = (await connection.QueryAsync<SampleReception>(query)).AsList();
                 
                 ConvertAllToLocalTime(receptions);
                 await PopulateSamplesAsync(connection, receptions);
                 return receptions;
             }, "GetAllReceptionsAsync");
        }

        public Task<List<SampleReception>> GetPendingReceptionsAsync()
        {
             return _db.ExecuteWithRetryAsync(async () =>
             {
                 using var connection = new SqliteConnection(_db.ConnectionString);
                 var query = "SELECT * FROM SampleReceptions WHERE Status = 'لم يتم إصدار شهادة' ORDER BY Date DESC;";
                 var receptions = (await connection.QueryAsync<SampleReception>(query)).AsList();
                 
                 ConvertAllToLocalTime(receptions);
                 await PopulateSamplesAsync(connection, receptions);
                 return receptions;
             }, "GetPendingReceptionsAsync");
        }

        public Task<List<SampleReception>> GetDelayedPendingReceptionsAsync(int daysDelayed)
        {
             return _db.ExecuteWithRetryAsync(async () =>
             {
                 using var connection = new SqliteConnection(_db.ConnectionString);
                 DateTime thresholdDate = DateTime.Now.AddDays(-daysDelayed);
                 string thresholdDateString = thresholdDate.ToString("yyyy-MM-dd HH:mm:ss");

                 var query = "SELECT * FROM SampleReceptions WHERE Status = 'لم يتم إصدار شهادة' AND Date <= @ThresholdDate ORDER BY Date DESC;";
                 var receptions = (await connection.QueryAsync<SampleReception>(query, new { ThresholdDate = thresholdDateString })).AsList();
                 
                 ConvertAllToLocalTime(receptions);
                 await PopulateSamplesAsync(connection, receptions);
                 return receptions;
             }, "GetDelayedPendingReceptionsAsync");
        }

        public Task<List<SampleReception>> SearchSampleReceptionsAsync(string searchTerm)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                string searchPattern = $"%{searchTerm}%";
                var query = @"SELECT * FROM SampleReceptions 
                              WHERE 
                              AnalysisRequestNumber LIKE @Search 
                              OR NotificationNumber LIKE @Search 
                              OR DeclarationNumber LIKE @Search 
                              OR Sender LIKE @Search 
                              OR Supplier LIKE @Search 
                              OR PolicyNumber LIKE @Search
                              ORDER BY Date DESC LIMIT 200;";

                var receptions = (await connection.QueryAsync<SampleReception>(query, new { Search = searchPattern })).AsList();
                
                ConvertAllToLocalTime(receptions);
                await PopulateSamplesAsync(connection, receptions);
                return receptions;
            }, "SearchSampleReceptionsAsync");
        }

        /// <summary>
        /// بحث في نماذج الاستلام بمعيار محدد (رقم العينة / رقم الإخطار / رقم الإقرار)
        /// </summary>
        public Task<List<SampleReception>> SearchReceptionsByFieldAsync(string field, string value)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                string searchPattern = $"%{value}%";
                string query;

                if (field == "رقم العينة")
                {
                    query = @"SELECT DISTINCT sr.* 
                              FROM SampleReceptions sr
                              INNER JOIN ReceptionSamples rs ON rs.ReceptionId = sr.Id
                              WHERE rs.SampleNumber LIKE @Search
                              ORDER BY sr.Date DESC LIMIT 100;";
                }
                else if (field == "رقم الإخطار" || field == "رقم الاخطار")
                {
                    query = @"SELECT * FROM SampleReceptions 
                              WHERE NotificationNumber LIKE @Search
                              ORDER BY Date DESC LIMIT 100;";
                }
                else // رقم الإقرار
                {
                    query = @"SELECT * FROM SampleReceptions 
                              WHERE DeclarationNumber LIKE @Search
                              ORDER BY Date DESC LIMIT 100;";
                }

                var receptions = (await connection.QueryAsync<SampleReception>(query, new { Search = searchPattern })).AsList();

                ConvertAllToLocalTime(receptions);
                await PopulateSamplesAsync(connection, receptions);
                return receptions;
            }, "SearchReceptionsByFieldAsync");
        }

        public Task<bool> UpdateReceptionStatusAsync(int receptionId, string newStatus)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                var query = "UPDATE SampleReceptions SET Status = @Status, UpdatedBy = @UpdatedBy, UpdatedByName = @UpdatedByName, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = @Id;";
                
                var parameters = new
                {
                    Id = receptionId,
                    Status = newStatus,
                    UpdatedBy = _userService.CurrentUser?.Id ?? 1,
                    UpdatedByName = _userService.CurrentUser?.FullName ?? "النظام"
                };

                int rows = await connection.ExecuteAsync(query, parameters);
                if (rows > 0) _cacheService.Clear();
                return rows > 0;
            }, "UpdateReceptionStatusAsync");
        }

        public Task<SampleReception?> GetReceptionByIdAsync(int id)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                var query = "SELECT * FROM SampleReceptions WHERE Id = @Id;";
                var rec = await connection.QueryFirstOrDefaultAsync<SampleReception>(query, new { Id = id });

                if (rec != null) ConvertToLocalTime(rec);

                if (rec != null)
                {
                    rec.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
                }
                
                return rec;
            }, "GetReceptionByIdAsync");
        }

        private async Task<System.Collections.ObjectModel.ObservableCollection<ReceptionSample>> GetSamplesForReceptionAsync(SqliteConnection connection, int receptionId)
        {
            var samplesQuery = "SELECT Id, ReceptionId, SampleNumber, Description FROM ReceptionSamples WHERE ReceptionId = @ReceptionId";
            var samplesList = await connection.QueryAsync<ReceptionSample>(samplesQuery, new { ReceptionId = receptionId });
            
            var samples = new System.Collections.ObjectModel.ObservableCollection<ReceptionSample>();
            int count = 1;
            foreach (var sample in samplesList)
            {
                sample.Root = (count++).ToString();
                samples.Add(sample);
            }
            return samples;
        }

        private async Task PopulateSamplesAsync(SqliteConnection connection, List<SampleReception> receptions)
        {
            if (!receptions.Any()) return;

            var receptionIds = receptions.Select(r => r.Id).ToList();
            var samplesQuery = "SELECT Id, ReceptionId, SampleNumber, Description FROM ReceptionSamples WHERE ReceptionId IN @Ids";
            var allSamples = await connection.QueryAsync<ReceptionSample>(samplesQuery, new { Ids = receptionIds });
            var samplesLookup = allSamples.GroupBy(s => s.ReceptionId).ToDictionary(g => g.Key, g => g.ToList());
            
            foreach (var rec in receptions)
            {
                if (samplesLookup.TryGetValue(rec.Id, out var samples))
                {
                    int count = 1;
                    var obsSamples = new System.Collections.ObjectModel.ObservableCollection<ReceptionSample>();
                    foreach (var sample in samples)
                    {
                        sample.Root = (count++).ToString();
                        obsSamples.Add(sample);
                    }
                    rec.Samples = obsSamples;
                }
                else
                {
                    rec.Samples = new System.Collections.ObjectModel.ObservableCollection<ReceptionSample>();
                }
            }
        }

        #region Sample Uniqueness Validation — فحص تفرد رقم العينة

        /// <summary>
        /// فحص تفرد رقم العينة في مرحلة الاستلام بالنسبة للجهة المرسلة وسنة الاستلام (Date.Year)
        /// يفحص في كل من الشهادات المعتمدة والاستلامات السابقة
        /// </summary>
        public Task<SampleUniquenessResult> CheckSampleUniquenessAsync(
            string sampleNumber,
            string sender,
            int year,
            int? excludeReceptionId = null)
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

                // 1. فحص الشهادات النشطة لنفس السنة والجهة
                var certQuery = @"SELECT c.Id, c.CertificateNumber, c.IssueDate, c.Sender, s.SampleNumber
                                  FROM Samples s
                                  INNER JOIN Certificates c ON s.CertificateId = c.Id
                                  WHERE strftime('%Y', c.IssueDate) = @Year";

                using (var certCommand = new SqliteCommand(certQuery, connection))
                {
                    certCommand.Parameters.AddWithValue("@Year", year.ToString());
                    using var reader = await certCommand.ExecuteReaderAsync();
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
                                CertificateNumber = $"شهادة رقم ({certNumber})",
                                IssueDate = issueDate,
                                Sender = rowSender,
                                SampleNumber = rowSampleNumber
                            };
                        }
                    }
                }

                // 2. فحص استلامات العينات السابقة لنفس السنة والجهة
                var recQuery = @"SELECT r.Id, r.AnalysisRequestNumber, r.Date, r.Sender, rs.SampleNumber
                                 FROM ReceptionSamples rs
                                 INNER JOIN SampleReceptions r ON rs.ReceptionId = r.Id
                                 WHERE strftime('%Y', r.Date) = @Year";

                if (excludeReceptionId.HasValue)
                {
                    recQuery += " AND r.Id != @ExcludeId";
                }

                using (var recCommand = new SqliteCommand(recQuery, connection))
                {
                    recCommand.Parameters.AddWithValue("@Year", year.ToString());
                    if (excludeReceptionId.HasValue)
                    {
                        recCommand.Parameters.AddWithValue("@ExcludeId", excludeReceptionId.Value);
                    }

                    using var reader = await recCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int recId = reader.GetInt32(0);
                        string reqNumber = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        string recDateStr = reader.GetString(2);
                        string rowSender = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        string rowSampleNumber = reader.IsDBNull(4) ? "" : reader.GetString(4);

                        if (SampleValidationHelper.NormalizeSender(rowSender) == normSender &&
                            SampleValidationHelper.NormalizeSampleNumber(rowSampleNumber) == normSampleNumber)
                        {
                            DateTime recDate = DateTime.TryParse(recDateStr, out var parsedDate) ? parsedDate : DateTime.MinValue;
                            return new SampleUniquenessResult
                            {
                                Status = SampleCheckResult.DuplicateActive,
                                CertificateId = recId,
                                CertificateNumber = $"استلام طلب تحليل ({reqNumber})",
                                IssueDate = recDate,
                                Sender = rowSender,
                                SampleNumber = rowSampleNumber
                            };
                        }
                    }
                }

                // 3. فحص السجلات المحذوفة (AuditLogs)
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
                                CertificateNumber = "سجل محذوف",
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
            }, "CheckReceptionSampleUniquenessAsync");
        }

        #endregion
    }
}
