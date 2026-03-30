using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Enjaz.Models;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// رسائل الإحالة والتنبيهات — Referral Letters & Alert Queries
    /// </summary>
    public partial class CertificateRepository
    {
        #region Referral Letters History - سجل رسائل الإحالة

        public System.Threading.Tasks.Task<bool> AddReferralLetterAsync(ReferralLetter letter)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"INSERT INTO ReferralLetters (SenderName, CertificateCount, SampleCount, OutputPath, StartDate, EndDate, IncludedColumns)
                              VALUES (@SenderName, @CertificateCount, @SampleCount, @OutputPath, @StartDate, @EndDate, @IncludedColumns);";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@SenderName", letter.SenderName);
                command.Parameters.AddWithValue("@CertificateCount", letter.CertificateCount);
                command.Parameters.AddWithValue("@SampleCount", letter.SampleCount);
                command.Parameters.AddWithValue("@OutputPath", letter.OutputPath);
                command.Parameters.AddWithValue("@StartDate", letter.StartDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@EndDate", letter.EndDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@IncludedColumns", letter.IncludedColumns);

                return await command.ExecuteNonQueryAsync() > 0;
            }, "AddReferralLetterAsync");
        }

        public System.Threading.Tasks.Task LogReferralLetterGenerationAsync(int? userId, string userName, string senderName, int certificateCount)
        {
            return _db.LogActionAsync(userId, userName, "إصدار رسالة إحالة", $"تم توليد رسالة إحالة موجهة إلى '{senderName}' تحتوي على {certificateCount} شهادة.");
        }

        public System.Threading.Tasks.Task<List<ReferralLetter>> GetReferralLettersAsync()
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var letters = new List<ReferralLetter>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = "SELECT Id, GeneratedAt, SenderName, CertificateCount, SampleCount, OutputPath, StartDate, EndDate, IncludedColumns FROM ReferralLetters ORDER BY GeneratedAt DESC;";
                using var command = new SqliteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    letters.Add(new ReferralLetter
                    {
                        Id = reader.GetInt32(0),
                        GeneratedAt = DateTime.Parse(reader.GetString(1)),
                        SenderName = reader.GetString(2),
                        CertificateCount = reader.GetInt32(3),
                        SampleCount = reader.GetInt32(4),
                        OutputPath = reader.GetString(5),
                        StartDate = DateTime.Parse(reader.GetString(6)),
                        EndDate = DateTime.Parse(reader.GetString(7)),
                        IncludedColumns = reader.IsDBNull(8) ? "" : reader.GetString(8)
                    });
                }
                return letters;
            }, "GetReferralLettersAsync");
        }

        #endregion

        #region Alert Queries - استعلامات التنبيهات

        /// <summary>
        /// جلب الشهادات التي تقترب من انتهاء الصلاحية
        /// </summary>
        public System.Threading.Tasks.Task<List<Certificate>> GetExpiringCertificatesAsync(int daysAhead)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var certificates = new List<Certificate>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var cutoffDate = DateTime.Now.AddDays(daysAhead).ToString("yyyy-MM-dd");
                var today = DateTime.Now.ToString("yyyy-MM-dd");

                var query = @"SELECT Id, CertificateNumber, RecipientName, ExpiryDate 
                              FROM Certificates 
                              WHERE ExpiryDate IS NOT NULL 
                              AND date(ExpiryDate) >= date(@Today) 
                              AND date(ExpiryDate) <= date(@Cutoff)
                              ORDER BY ExpiryDate ASC;";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Today", today);
                command.Parameters.AddWithValue("@Cutoff", cutoffDate);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    certificates.Add(new Certificate
                    {
                        Id = reader.GetInt32(0),
                        CertificateNumber = reader.GetString(1),
                        RecipientName = reader.GetString(2),
                        ExpiryDate = DateTime.ParseExact(reader.GetString(3), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    });
                }
                return certificates;
            }, "GetExpiringCertificatesAsync");
        }

        /// <summary>
        /// جلب العينات ذات النتائج غير الاعتيادية (مرفوض، غير صالح، إلخ)
        /// </summary>
        public System.Threading.Tasks.Task<List<Sample>> GetUnusualSamplesAsync()
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var samples = new List<Sample>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var unusualKeywords = new[] { "%مرفوض%", "%غير صالح%", "%فشل%", "%unfit%", "%rejected%", "%failed%", "%غير مطابق%" };
                
                var query = "SELECT Id, CertificateId, SampleNumber, Result FROM Samples WHERE 1=0";
                foreach (var k in unusualKeywords) query += " OR Result LIKE '" + k + "'";
                query += " ORDER BY Id DESC LIMIT 20;";

                using var command = new SqliteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    samples.Add(new Sample
                    {
                        Id = reader.GetInt32(0),
                        CertificateId = reader.GetInt32(1),
                        SampleNumber = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Result = reader.IsDBNull(3) ? "" : reader.GetString(3)
                    });
                }
                return samples;
            }, "GetUnusualSamplesAsync");
        }

        /// <summary>
        /// الحصول على إحصائيات الشهادات حسب النوع
        /// </summary>
        public System.Threading.Tasks.Task<(int total, int consumable, int environmental, int totalSamples)> GetCertificateCountsByTypeAsync()
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                
                var query = @"SELECT 
                              COUNT(*) as Total,
                              SUM(CASE WHEN CertificateType LIKE '%استهلاكية%' THEN 1 ELSE 0 END) as Consumable,
                              SUM(CASE WHEN CertificateType LIKE '%بيئية%' THEN 1 ELSE 0 END) as Environmental,
                              (SELECT COUNT(*) FROM Samples) as TotalSamples
                              FROM Certificates;";
                
                using var command = new SqliteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return (
                        reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                        reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
                    );
                }
                return (0, 0, 0, 0);
            }, "GetCertificateCountsByTypeAsync");
        }

        #endregion
    }
}
