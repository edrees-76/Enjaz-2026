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
    }
}
