using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;
using Enjaz.Models;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// تطبيق مستودع العينات مفصولاً بالكامل كجزء من معمارية DDD
    /// </summary>
    public class SampleRepository : ISampleRepository
    {
        private readonly DatabaseService _db;
        private readonly Services.Caching.ICacheService _cacheService;

        public SampleRepository(DatabaseService db, Services.Caching.ICacheService cacheService)
        {
            _db = db;
            _cacheService = cacheService;
        }

        public SampleRepository(DatabaseService db) 
            : this(db, new Services.Caching.MemoryCacheService(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions())))
        {
        }

        public Task<bool> AddSampleAsync(Sample sample)
        {
            // تطبيق التحقق (Business Rule Validation) قبل ملامسة قاعدة البيانات
            sample.Validate(); // (يمكن تمرير نوع الشهادة لاحقاً إذا احتجنا)

            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"INSERT INTO Samples (CertificateId, Root, SampleNumber, Description, MeasurementDate, Result, 
                                                   IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137)
                              VALUES (@CertificateId, @Root, @SampleNumber, @Description, @MeasurementDate, @Result,
                                      @IsotopeK40, @IsotopeRa226, @IsotopeTh232, @IsotopeRa, @IsotopeCs137);";

                var rows = await connection.ExecuteAsync(query, new
                {
                    sample.CertificateId,
                    sample.Root,
                    SampleNumber = sample.SampleNumber ?? "",
                    Description = sample.Description ?? "",
                    MeasurementDate = sample.MeasurementDate.ToString("yyyy-MM-dd"),
                    Result = sample.Result ?? "",
                    IsotopeK40 = sample.IsotopeK40 ?? "",
                    IsotopeRa226 = sample.IsotopeRa226 ?? "",
                    IsotopeTh232 = sample.IsotopeTh232 ?? "",
                    IsotopeRa = sample.IsotopeRa ?? "",
                    IsotopeCs137 = sample.IsotopeCs137 ?? ""
                });

                if (rows > 0) _cacheService.Clear();
                return rows > 0;
            }, "SampleRepository.AddSampleAsync");
        }

        public Task<List<Sample>> GetSamplesByCertificateIdAsync(int certificateId)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT s.Id, s.CertificateId, s.Root, s.SampleNumber, s.Description, s.MeasurementDate, s.Result,
                                     s.IsotopeK40, s.IsotopeRa226, s.IsotopeTh232, s.IsotopeRa, s.IsotopeCs137 
                              FROM Samples s
                              WHERE s.CertificateId = @CertificateId 
                              ORDER BY s.Root;";

                var result = await connection.QueryAsync<Sample>(query, new { CertificateId = certificateId });
                return result.ToList();
            }, "SampleRepository.GetSamplesByCertificateIdAsync");
        }

        public Task<List<Sample>> GetSamplesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT s.Id, s.CertificateId, s.Root, s.SampleNumber, s.Description, s.MeasurementDate, s.Result,
                                     s.IsotopeK40, s.IsotopeRa226, s.IsotopeTh232, s.IsotopeRa, s.IsotopeCs137 
                              FROM Samples s
                              INNER JOIN Certificates c ON s.CertificateId = c.Id
                              WHERE date(c.IssueDate) >= date(@StartDate) 
                              AND date(c.IssueDate) <= date(@EndDate)
                              ORDER BY c.IssueDate ASC, s.Root ASC;";

                var result = await connection.QueryAsync<Sample>(query, new 
                { 
                    StartDate = startDate.ToString("yyyy-MM-dd"),
                    EndDate = endDate.ToString("yyyy-MM-dd")
                });
                return result.ToList();
            }, "SampleRepository.GetSamplesByDateRangeAsync");
        }

        public Task<bool> DeleteSamplesByCertificateIdAsync(int certificateId)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                var rows = await connection.ExecuteAsync("DELETE FROM Samples WHERE CertificateId = @CertificateId;", new { CertificateId = certificateId });
                if (rows >= 0) _cacheService.Clear();
                return rows >= 0;
            }, "SampleRepository.DeleteSamplesByCertificateIdAsync");
        }
    }
}
