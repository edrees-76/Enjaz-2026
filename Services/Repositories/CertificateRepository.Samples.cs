using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;
using Enjaz.Models;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// عمليات العينات — Sample CRUD Operations
    /// </summary>
    public partial class CertificateRepository
    {
        #region Sample Operations - عمليات العينات

        /// <summary>
        /// إضافة عينة جديدة بشكل غير متزامن
        /// Add new sample asynchronously
        /// </summary>
        public Task<bool> AddSampleAsync(Sample sample)
        {
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

                return rows > 0;
            }, "AddSampleAsync");
        }

        /// <summary>
        /// الحصول على عينات شهادة معينة بشكل غير متزامن
        /// Get samples for a certificate asynchronously
        /// </summary>
        public Task<List<Sample>> GetSamplesByCertificateIdAsync(int certificateId)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var samples = new List<Sample>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT s.Id, s.CertificateId, s.Root, s.SampleNumber, s.Description, s.MeasurementDate, s.Result,
                                     s.IsotopeK40, s.IsotopeRa226, s.IsotopeTh232, s.IsotopeRa, s.IsotopeCs137 
                              FROM Samples s
                              INNER JOIN Certificates c ON s.CertificateId = c.Id
                              WHERE s.CertificateId = @CertificateId 
                              ORDER BY s.Root;";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@CertificateId", certificateId);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    samples.Add(MapSampleFromReader((SqliteDataReader)reader));
                }
                return samples;
            }, "GetSamplesByCertificateIdAsync");
        }

        /// <summary>
        /// جلب كافة العينات ضمن نطاق زمني لجميع الشهادات - يحل مشكلة N+1
        /// </summary>
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

                var samples = new List<Sample>();
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    samples.Add(MapSampleFromReader((SqliteDataReader)reader));
                }
                return samples;
            }, "GetSamplesByDateRangeAsync");
        }

        /// <summary>
        /// حذف عينات شهادة معينة بشكل غير متزامن
        /// Delete samples by certificate ID asynchronously
        /// </summary>
        public Task<bool> DeleteSamplesByCertificateIdAsync(int certificateId)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                var rows = await connection.ExecuteAsync("DELETE FROM Samples WHERE CertificateId = @CertificateId;", new { CertificateId = certificateId });
                return rows >= 0;
            }, "DeleteSamplesByCertificateIdAsync");
        }

        #endregion
    }
}
