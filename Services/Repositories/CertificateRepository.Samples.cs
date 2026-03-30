using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
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
        public System.Threading.Tasks.Task<bool> AddSampleAsync(Sample sample)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"INSERT INTO Samples (CertificateId, Root, SampleNumber, Description, MeasurementDate, Result, 
                                                   IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137)
                              VALUES (@CertificateId, @Root, @SampleNumber, @Description, @MeasurementDate, @Result,
                                      @IsotopeK40, @IsotopeRa226, @IsotopeTh232, @IsotopeRa, @IsotopeCs137);";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@CertificateId", sample.CertificateId);
                command.Parameters.AddWithValue("@Root", sample.Root);
                command.Parameters.AddWithValue("@SampleNumber", sample.SampleNumber ?? "");
                command.Parameters.AddWithValue("@Description", sample.Description ?? "");
                command.Parameters.AddWithValue("@MeasurementDate", sample.MeasurementDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@Result", sample.Result ?? "");
                
                command.Parameters.AddWithValue("@IsotopeK40", sample.IsotopeK40 ?? "");
                command.Parameters.AddWithValue("@IsotopeRa226", sample.IsotopeRa226 ?? "");
                command.Parameters.AddWithValue("@IsotopeTh232", sample.IsotopeTh232 ?? "");
                command.Parameters.AddWithValue("@IsotopeRa", sample.IsotopeRa ?? "");
                command.Parameters.AddWithValue("@IsotopeCs137", sample.IsotopeCs137 ?? "");

                return await command.ExecuteNonQueryAsync() > 0;
            }, "AddSampleAsync");
        }

        /// <summary>
        /// الحصول على عينات شهادة معينة بشكل غير متزامن
        /// Get samples for a certificate asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<List<Sample>> GetSamplesByCertificateIdAsync(int certificateId)
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
                    samples.Add(new Sample
                    {
                        Id = reader.GetInt32(0),
                        CertificateId = reader.GetInt32(1),
                        Root = reader.GetInt32(2),
                        SampleNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        MeasurementDate = DateTime.Parse(reader.GetString(5)),
                        Result = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        IsotopeK40 = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        IsotopeRa226 = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        IsotopeTh232 = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        IsotopeRa = reader.IsDBNull(10) ? "" : reader.GetString(10),
                        IsotopeCs137 = reader.IsDBNull(11) ? "" : reader.GetString(11)
                    });
                }
                return samples;
            }, "GetSamplesByCertificateIdAsync");
        }

        /// <summary>
        /// جلب كافة العينات ضمن نطاق زمني لجميع الشهادات - يحل مشكلة N+1
        /// </summary>
        public System.Threading.Tasks.Task<List<Sample>> GetSamplesByDateRangeAsync(DateTime startDate, DateTime endDate)
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
                              WHERE date(c.IssueDate) >= date(@StartDate) 
                              AND date(c.IssueDate) <= date(@EndDate)
                              ORDER BY c.IssueDate ASC, s.Root ASC;";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    samples.Add(new Sample
                    {
                        Id = reader.GetInt32(0),
                        CertificateId = reader.GetInt32(1),
                        Root = reader.GetInt32(2),
                        SampleNumber = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        MeasurementDate = DateTime.Parse(reader.GetString(5)),
                        Result = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        IsotopeK40 = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        IsotopeRa226 = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        IsotopeTh232 = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        IsotopeRa = reader.IsDBNull(10) ? "" : reader.GetString(10),
                        IsotopeCs137 = reader.IsDBNull(11) ? "" : reader.GetString(11)
                    });
                }
                return samples;
            }, "GetSamplesByDateRangeAsync");
        }

        /// <summary>
        /// حذف عينات شهادة معينة بشكل غير متزامن
        /// Delete samples by certificate ID asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<bool> DeleteSamplesByCertificateIdAsync(int certificateId)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                var query = "DELETE FROM Samples WHERE CertificateId = @CertificateId;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@CertificateId", certificateId);
                return await command.ExecuteNonQueryAsync() >= 0;
            }, "DeleteSamplesByCertificateIdAsync");
        }

        #endregion
    }
}
