using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Enjaz.Models;
using Enjaz.Helpers;
using System.Threading.Tasks;

namespace Enjaz.Services.Repositories
{
    public class SampleReceptionRepository
    {
        private readonly DatabaseService _db;
        private readonly UserService _userService;

        public SampleReceptionRepository(DatabaseService db, UserService userService)
        {
            _db = db;
            _userService = userService;
        }

        public Task<List<string>> GetDistinctSendersAsync()
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var senders = new List<string>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                var query = "SELECT DISTINCT Sender FROM SampleReceptions WHERE Sender IS NOT NULL AND Sender != ''";
                using var command = new SqliteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    senders.Add(reader.GetString(0));
                }
                return senders;
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

                    using var command = new SqliteCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("@AnalysisRequestNumber", reception.AnalysisRequestNumber);
                    command.Parameters.AddWithValue("@NotificationNumber", reception.NotificationNumber ?? "");
                    command.Parameters.AddWithValue("@DeclarationNumber", reception.DeclarationNumber ?? "");
                    command.Parameters.AddWithValue("@Supplier", reception.Supplier ?? "");
                    command.Parameters.AddWithValue("@Sender", reception.Sender ?? "");
                    command.Parameters.AddWithValue("@Origin", reception.Origin ?? "");
                    command.Parameters.AddWithValue("@PolicyNumber", reception.PolicyNumber ?? "");
                    command.Parameters.AddWithValue("@FinancialReceiptNumber", reception.FinancialReceiptNumber ?? "");
                    command.Parameters.AddWithValue("@CertificateType", reception.CertificateType);
                    command.Parameters.AddWithValue("@Date", reception.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("@Status", reception.Status);
                    command.Parameters.AddWithValue("@CreatedBy", _userService.CurrentUser?.Id ?? 1);
                    command.Parameters.AddWithValue("@CreatedByName", _userService.CurrentUser?.FullName ?? "النظام");

                    var idObj = await command.ExecuteScalarAsync();
                    if (idObj == null) throw new Exception("Failed to retrieve ID");
                    int newId = Convert.ToInt32(idObj);

                    if (reception.Samples != null)
                    {
                        foreach (var sample in reception.Samples)
                        {
                            var insertSampleQuery = @"INSERT INTO ReceptionSamples 
                                (ReceptionId, SampleNumber, Description)
                                VALUES (@ReceptionId, @SampleNumber, @Description)";
                            
                            using var sampleCmd = new SqliteCommand(insertSampleQuery, connection, transaction);
                            sampleCmd.Parameters.AddWithValue("@ReceptionId", newId);
                            sampleCmd.Parameters.AddWithValue("@SampleNumber", sample.SampleNumber ?? "");
                            sampleCmd.Parameters.AddWithValue("@Description", sample.Description ?? "");
                            await sampleCmd.ExecuteNonQueryAsync();
                        }
                    }

                    transaction.Commit();
                    
                    await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "إنشاء استلام", 
                        $"إيصال استلام جديد رقم {reception.AnalysisRequestNumber}", newId);
                    
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

                    using var command = new SqliteCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("@Id", reception.Id);
                    command.Parameters.AddWithValue("@AnalysisRequestNumber", reception.AnalysisRequestNumber);
                    command.Parameters.AddWithValue("@NotificationNumber", reception.NotificationNumber ?? "");
                    command.Parameters.AddWithValue("@DeclarationNumber", reception.DeclarationNumber ?? "");
                    command.Parameters.AddWithValue("@Supplier", reception.Supplier ?? "");
                    command.Parameters.AddWithValue("@Sender", reception.Sender ?? "");
                    command.Parameters.AddWithValue("@Origin", reception.Origin ?? "");
                    command.Parameters.AddWithValue("@PolicyNumber", reception.PolicyNumber ?? "");
                    command.Parameters.AddWithValue("@FinancialReceiptNumber", reception.FinancialReceiptNumber ?? "");
                    command.Parameters.AddWithValue("@CertificateType", reception.CertificateType);
                    command.Parameters.AddWithValue("@Date", reception.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("@Status", reception.Status);
                    command.Parameters.AddWithValue("@UpdatedBy", _userService.CurrentUser?.Id ?? 1);
                    command.Parameters.AddWithValue("@UpdatedByName", _userService.CurrentUser?.FullName ?? "النظام");

                    await command.ExecuteNonQueryAsync();

                    if (reception.Samples != null)
                    {
                        var deleteCmd = new SqliteCommand("DELETE FROM ReceptionSamples WHERE ReceptionId = @ReceptionId", connection, transaction);
                        deleteCmd.Parameters.AddWithValue("@ReceptionId", reception.Id);
                        await deleteCmd.ExecuteNonQueryAsync();

                        foreach (var sample in reception.Samples)
                        {
                            var insertSampleQuery = @"INSERT INTO ReceptionSamples 
                                (ReceptionId, SampleNumber, Description)
                                VALUES (@ReceptionId, @SampleNumber, @Description)";
                            
                            using var sampleCmd = new SqliteCommand(insertSampleQuery, connection, transaction);
                            sampleCmd.Parameters.AddWithValue("@ReceptionId", reception.Id);
                            sampleCmd.Parameters.AddWithValue("@SampleNumber", sample.SampleNumber ?? "");
                            sampleCmd.Parameters.AddWithValue("@Description", sample.Description ?? "");
                            await sampleCmd.ExecuteNonQueryAsync();
                        }
                    }

                    transaction.Commit();
                    
                    await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "تعديل استلام", 
                        $"تعديل بيانات الاستلام رقم طلب التحليل {reception.AnalysisRequestNumber}", reception.Id);
                    
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
                await connection.OpenAsync();
                
                var query = "DELETE FROM SampleReceptions WHERE Id = @Id;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                
                int rows = await command.ExecuteNonQueryAsync();
                if (rows > 0)
                {
                    await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "حذف استلام", 
                        $"حذف استلام برقم معرف {id}", id);
                    return true;
                }
                return false;
            }, "DeleteSampleReceptionAsync");
        }

        public Task<List<SampleReception>> GetAllReceptionsAsync()
        {
             return _db.ExecuteWithRetryAsync(async () =>
             {
                 var receptions = new List<SampleReception>();
                 using var connection = new SqliteConnection(_db.ConnectionString);
                 await connection.OpenAsync();

                 var query = @"SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, 
                               Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, 
                               CertificateType, Date, Status, CreatedBy, CreatedByName
                               FROM SampleReceptions 
                               ORDER BY Date DESC LIMIT 200;";

                 using var command = new SqliteCommand(query, connection);
                 using var reader = await command.ExecuteReaderAsync();

                 while (await reader.ReadAsync())
                 {
                     receptions.Add(new SampleReception
                     {
                         Id = reader.GetInt32(0),
                         AnalysisRequestNumber = reader.GetString(1),
                         NotificationNumber = reader.IsDBNull(2) ? "" : reader.GetString(2),
                         DeclarationNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                         Supplier = reader.IsDBNull(4) ? "" : reader.GetString(4),
                         Sender = reader.IsDBNull(5) ? "" : reader.GetString(5),
                         Origin = reader.IsDBNull(6) ? "" : reader.GetString(6),
                         PolicyNumber = reader.IsDBNull(7) ? "" : reader.GetString(7),
                         FinancialReceiptNumber = reader.IsDBNull(8) ? "" : reader.GetString(8),
                         CertificateType = reader.GetString(9),
                         Date = ParseDbDate(reader.GetString(10)),
                         Status = reader.GetString(11),
                         CreatedBy = reader.GetInt32(12),
                         CreatedByName = reader.IsDBNull(13) ? "" : reader.GetString(13)
                     });
                 }
                 
                 foreach (var rec in receptions)
                 {
                     rec.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
                 }
                 
                 return receptions;
             }, "GetAllReceptionsAsync");
        }

        public Task<List<SampleReception>> GetPendingReceptionsAsync()
        {
             return _db.ExecuteWithRetryAsync(async () =>
             {
                 var receptions = new List<SampleReception>();
                 using var connection = new SqliteConnection(_db.ConnectionString);
                 await connection.OpenAsync();

                 var query = @"SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, 
                               Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, 
                               CertificateType, Date, Status, CreatedBy, CreatedByName
                               FROM SampleReceptions 
                               WHERE Status = 'لم يتم إصدار شهادة'
                               ORDER BY Date DESC;";

                 using var command = new SqliteCommand(query, connection);
                 using var reader = await command.ExecuteReaderAsync();

                 while (await reader.ReadAsync())
                 {
                     receptions.Add(new SampleReception
                     {
                         Id = reader.GetInt32(0),
                         AnalysisRequestNumber = reader.GetString(1),
                         NotificationNumber = reader.IsDBNull(2) ? "" : reader.GetString(2),
                         DeclarationNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                         Supplier = reader.IsDBNull(4) ? "" : reader.GetString(4),
                         Sender = reader.IsDBNull(5) ? "" : reader.GetString(5),
                         Origin = reader.IsDBNull(6) ? "" : reader.GetString(6),
                         PolicyNumber = reader.IsDBNull(7) ? "" : reader.GetString(7),
                         FinancialReceiptNumber = reader.IsDBNull(8) ? "" : reader.GetString(8),
                         CertificateType = reader.GetString(9),
                         Date = ParseDbDate(reader.GetString(10)),
                         Status = reader.GetString(11),
                         CreatedBy = reader.GetInt32(12),
                         CreatedByName = reader.IsDBNull(13) ? "" : reader.GetString(13)
                     });
                 }
                 
                 foreach (var rec in receptions)
                 {
                     rec.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
                 }
                 
                 return receptions;
             }, "GetPendingReceptionsAsync");
        }

        public Task<List<SampleReception>> GetDelayedPendingReceptionsAsync(int daysDelayed)
        {
             return _db.ExecuteWithRetryAsync(async () =>
             {
                 var receptions = new List<SampleReception>();
                 using var connection = new SqliteConnection(_db.ConnectionString);
                 await connection.OpenAsync();

                 var query = @"SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, 
                               Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, 
                               CertificateType, Date, Status, CreatedBy, CreatedByName
                               FROM SampleReceptions 
                               WHERE Status = 'لم يتم إصدار شهادة' AND Date <= @ThresholdDate
                               ORDER BY Date DESC;";

                 DateTime thresholdDate = DateTime.Now.AddDays(-daysDelayed);
                 string thresholdDateString = thresholdDate.ToString("yyyy-MM-dd HH:mm:ss");

                 using var command = new SqliteCommand(query, connection);
                 command.Parameters.AddWithValue("@ThresholdDate", thresholdDateString);
                 
                 using var reader = await command.ExecuteReaderAsync();

                 while (await reader.ReadAsync())
                 {
                     receptions.Add(new SampleReception
                     {
                         Id = reader.GetInt32(0),
                         AnalysisRequestNumber = reader.GetString(1),
                         NotificationNumber = reader.IsDBNull(2) ? "" : reader.GetString(2),
                         DeclarationNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                         Supplier = reader.IsDBNull(4) ? "" : reader.GetString(4),
                         Sender = reader.IsDBNull(5) ? "" : reader.GetString(5),
                         Origin = reader.IsDBNull(6) ? "" : reader.GetString(6),
                         PolicyNumber = reader.IsDBNull(7) ? "" : reader.GetString(7),
                         FinancialReceiptNumber = reader.IsDBNull(8) ? "" : reader.GetString(8),
                         CertificateType = reader.GetString(9),
                         Date = ParseDbDate(reader.GetString(10)),
                         Status = reader.GetString(11),
                         CreatedBy = reader.GetInt32(12),
                         CreatedByName = reader.IsDBNull(13) ? "" : reader.GetString(13)
                     });
                 }
                 
                 foreach (var rec in receptions)
                 {
                     rec.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
                 }
                 
                 return receptions;
             }, "GetDelayedPendingReceptionsAsync");
        }

        public Task<List<SampleReception>> SearchSampleReceptionsAsync(string searchTerm)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var receptions = new List<SampleReception>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, 
                              Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, 
                              CertificateType, Date, Status, CreatedBy, CreatedByName
                              FROM SampleReceptions 
                              WHERE 
                              AnalysisRequestNumber LIKE @Search 
                              OR NotificationNumber LIKE @Search 
                              OR DeclarationNumber LIKE @Search 
                              OR Sender LIKE @Search 
                              OR Supplier LIKE @Search 
                              OR PolicyNumber LIKE @Search
                              ORDER BY Date DESC LIMIT 200;";

                string searchPattern = $"%{searchTerm}%";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Search", searchPattern);
                
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    receptions.Add(new SampleReception
                    {
                        Id = reader.GetInt32(0),
                        AnalysisRequestNumber = reader.GetString(1),
                        NotificationNumber = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        DeclarationNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Supplier = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Sender = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        Origin = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        PolicyNumber = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        FinancialReceiptNumber = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        CertificateType = reader.GetString(9),
                        Date = ParseDbDate(reader.GetString(10)),
                        Status = reader.GetString(11),
                        CreatedBy = reader.GetInt32(12),
                        CreatedByName = reader.IsDBNull(13) ? "" : reader.GetString(13)
                    });
                }
                
                foreach (var rec in receptions)
                {
                    rec.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
                }

                return receptions;
            }, "SearchSampleReceptionsAsync");
        }

        private async Task<System.Collections.ObjectModel.ObservableCollection<ReceptionSample>> GetSamplesForReceptionAsync(SqliteConnection connection, int receptionId)
        {
            var samples = new System.Collections.ObjectModel.ObservableCollection<ReceptionSample>();
            var samplesQuery = @"SELECT Id, SampleNumber, Description
                                 FROM ReceptionSamples 
                                 WHERE ReceptionId = @ReceptionId";
            
            using var samplesCmd = new SqliteCommand(samplesQuery, connection);
            samplesCmd.Parameters.AddWithValue("@ReceptionId", receptionId);
            using var samplesReader = await samplesCmd.ExecuteReaderAsync();
            
            int count = 1;
            while (await samplesReader.ReadAsync())
            {
                samples.Add(new ReceptionSample
                {
                    Id = samplesReader.GetInt32(0),
                    ReceptionId = receptionId,
                    Root = (count++).ToString(),
                    SampleNumber = samplesReader.IsDBNull(1) ? "" : samplesReader.GetString(1),
                    Description = samplesReader.IsDBNull(2) ? "" : samplesReader.GetString(2)
                });
            }
            return samples;
        }

        /// <summary>
        /// بحث في نماذج الاستلام بمعيار محدد (رقم العينة / رقم الإخطار / رقم الإقرار)
        /// </summary>
        public Task<List<SampleReception>> SearchReceptionsByFieldAsync(string field, string value)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var receptions = new List<SampleReception>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                string query;
                string searchPattern = $"%{value}%";

                if (field == "رقم العينة")
                {
                    // البحث عبر جدول العينات المرتبط
                    query = @"SELECT DISTINCT sr.Id, sr.AnalysisRequestNumber, sr.NotificationNumber, sr.DeclarationNumber, 
                              sr.Supplier, sr.Sender, sr.Origin, sr.PolicyNumber, sr.FinancialReceiptNumber, 
                              sr.CertificateType, sr.Date, sr.Status, sr.CreatedBy, sr.CreatedByName
                              FROM SampleReceptions sr
                              INNER JOIN ReceptionSamples rs ON rs.ReceptionId = sr.Id
                              WHERE rs.SampleNumber LIKE @Search
                              ORDER BY sr.Date DESC LIMIT 100;";
                }
                else if (field == "رقم الإخطار" || field == "رقم الاخطار")
                {
                    query = @"SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, 
                              Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, 
                              CertificateType, Date, Status, CreatedBy, CreatedByName
                              FROM SampleReceptions 
                              WHERE NotificationNumber LIKE @Search
                              ORDER BY Date DESC LIMIT 100;";
                }
                else // رقم الإقرار
                {
                    query = @"SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, 
                              Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, 
                              CertificateType, Date, Status, CreatedBy, CreatedByName
                              FROM SampleReceptions 
                              WHERE DeclarationNumber LIKE @Search
                              ORDER BY Date DESC LIMIT 100;";
                }

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Search", searchPattern);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    receptions.Add(new SampleReception
                    {
                        Id = reader.GetInt32(0),
                        AnalysisRequestNumber = reader.GetString(1),
                        NotificationNumber = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        DeclarationNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Supplier = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Sender = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        Origin = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        PolicyNumber = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        FinancialReceiptNumber = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        CertificateType = reader.GetString(9),
                        Date = ParseDbDate(reader.GetString(10)),
                        Status = reader.GetString(11),
                        CreatedBy = reader.GetInt32(12),
                        CreatedByName = reader.IsDBNull(13) ? "" : reader.GetString(13)
                    });
                }

                foreach (var rec in receptions)
                {
                    rec.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
                }

                return receptions;
            }, "SearchReceptionsByFieldAsync");
        }

        /// <summary>
        /// تحديث حالة الاستلام (مثلاً من "لم يتم إصدار شهادة" إلى "تم إصدار شهادة")
        /// </summary>
        public Task<bool> UpdateReceptionStatusAsync(int receptionId, string newStatus)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = "UPDATE SampleReceptions SET Status = @Status, UpdatedBy = @UpdatedBy, UpdatedByName = @UpdatedByName, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = @Id;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Id", receptionId);
                command.Parameters.AddWithValue("@Status", newStatus);
                command.Parameters.AddWithValue("@UpdatedBy", _userService.CurrentUser?.Id ?? 1);
                command.Parameters.AddWithValue("@UpdatedByName", _userService.CurrentUser?.FullName ?? "النظام");

                int rows = await command.ExecuteNonQueryAsync();
                return rows > 0;
            }, "UpdateReceptionStatusAsync");
        }

        public Task<SampleReception?> GetReceptionByIdAsync(int id)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, 
                               Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, 
                               CertificateType, Date, Status, CreatedBy, CreatedByName
                               FROM SampleReceptions 
                               WHERE Id = @Id;";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var rec = new SampleReception
                    {
                        Id = reader.GetInt32(0),
                        AnalysisRequestNumber = reader.GetString(1),
                        NotificationNumber = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        DeclarationNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Supplier = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Sender = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        Origin = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        PolicyNumber = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        FinancialReceiptNumber = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        CertificateType = reader.GetString(9),
                        Date = ParseDbDate(reader.GetString(10)),
                        Status = reader.GetString(11),
                        CreatedBy = reader.GetInt32(12),
                        CreatedByName = reader.IsDBNull(13) ? "" : reader.GetString(13)
                    };
                    
                    rec.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
                    return rec;
                }
                
                return null;
            }, "GetReceptionByIdAsync");
        }
        private DateTime ParseDbDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return DateTime.Now;
            
            string[] formats = { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss", "MM/dd/yyyy" };
            if (DateTime.TryParseExact(dateStr, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            
            return DateTime.TryParse(dateStr, out result) ? result : DateTime.Now;
        }
    }
}
