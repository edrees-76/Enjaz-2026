using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;
using Enjaz.Models;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// رسائل الإحالة والتنبيهات — Referral Letters & Alert Queries
    /// </summary>
    public partial class CertificateRepository
    {
        #region Referral Letters History - سجل رسائل الإحالة

        public Task<bool> AddReferralLetterAsync(ReferralLetter letter)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"INSERT INTO ReferralLetters (SenderName, CertificateCount, SampleCount, OutputPath, StartDate, EndDate, IncludedColumns)
                              VALUES (@SenderName, @CertificateCount, @SampleCount, @OutputPath, @StartDate, @EndDate, @IncludedColumns);";

                var rows = await connection.ExecuteAsync(query, new
                {
                    letter.SenderName,
                    letter.CertificateCount,
                    letter.SampleCount,
                    letter.OutputPath,
                    StartDate = letter.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = letter.EndDate.ToString("yyyy-MM-dd"),
                    letter.IncludedColumns
                });

                return rows > 0;
            }, "AddReferralLetterAsync");
        }

        public Task LogReferralLetterGenerationAsync(int? userId, string userName, string senderName, int certificateCount)
        {
            return _db.LogActionAsync(userId, userName, "إصدار رسالة إحالة", $"تم توليد رسالة إحالة موجهة إلى '{senderName}' تحتوي على {certificateCount} شهادة.");
        }

        public Task<List<ReferralLetter>> GetReferralLettersAsync()
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = "SELECT Id, GeneratedAt, SenderName, CertificateCount, SampleCount, OutputPath, StartDate, EndDate, IncludedColumns FROM ReferralLetters ORDER BY GeneratedAt DESC;";

                var letters = new List<ReferralLetter>();
                using var reader = await connection.ExecuteReaderAsync(query);
                while (reader.Read())
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
        public Task<List<Certificate>> GetExpiringCertificatesAsync(int daysAhead)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT Id, CertificateNumber, RecipientName, ExpiryDate 
                              FROM Certificates 
                              WHERE ExpiryDate IS NOT NULL 
                              AND date(ExpiryDate) >= date(@Today) 
                              AND date(ExpiryDate) <= date(@Cutoff)
                              ORDER BY ExpiryDate ASC;";

                var certificates = new List<Certificate>();
                using var reader = await connection.ExecuteReaderAsync(query, new
                {
                    Today = DateTime.Now.ToString("yyyy-MM-dd"),
                    Cutoff = DateTime.Now.AddDays(daysAhead).ToString("yyyy-MM-dd")
                });
                while (reader.Read())
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
        public Task<List<Sample>> GetUnusualSamplesAsync()
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                // Parameterized approach for unusual keywords
                var query = @"SELECT Id, CertificateId, SampleNumber, Result FROM Samples 
                              WHERE Result LIKE '%مرفوض%' 
                              OR Result LIKE '%غير صالح%' 
                              OR Result LIKE '%فشل%' 
                              OR Result LIKE '%unfit%' 
                              OR Result LIKE '%rejected%' 
                              OR Result LIKE '%failed%' 
                              OR Result LIKE '%غير مطابق%'
                              ORDER BY Id DESC LIMIT 20;";

                var samples = new List<Sample>();
                using var reader = await connection.ExecuteReaderAsync(query);
                while (reader.Read())
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
        public Task<(int total, int consumable, int environmental, int totalSamples)> GetCertificateCountsByTypeAsync()
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

                using var reader = await connection.ExecuteReaderAsync(query);
                if (reader.Read())
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
