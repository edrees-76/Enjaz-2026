using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// إحصائيات لوحة القيادة — Dashboard Statistics
    /// </summary>
    public partial class CertificateRepository
    {
        #region Dashboard Statistics

        public Task<int> GetTotalCertificatesCountAsync(bool showDeleted = false, int? year = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                var query = showDeleted
                    ? "SELECT COUNT(*) FROM Certificates WHERE (@Year IS NULL OR strftime('%Y', IssueDate) = @Year)"
                    : "SELECT COUNT(*) FROM Certificates WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND (@Year IS NULL OR strftime('%Y', IssueDate) = @Year)";
                return await connection.ExecuteScalarAsync<int>(query, new { Year = year?.ToString() });
            }, "GetTotalCertificatesCountAsync");
        }

        public Task<int> GetCertificatesCountByDateAsync(DateTime date)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Certificates WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND DATE(IssueDate) = DATE(@Date)",
                    new { Date = date.ToString("yyyy-MM-dd") });
            }, "GetCertificatesCountByDateAsync");
        }

        public Task<Dictionary<int, int>> GetMonthlyStatisticsAsync(int year)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var stats = new Dictionary<int, int>();
                for (int i = 1; i <= 12; i++) stats[i] = 0;

                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT strftime('%m', IssueDate) as Month, COUNT(*) as Count 
                              FROM Certificates 
                              WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND strftime('%Y', IssueDate) = @Year 
                              GROUP BY Month";

                var rows = await connection.QueryAsync(query, new { Year = year.ToString() });
                foreach (var row in rows)
                {
                    int month = Convert.ToInt32((string)row.Month);
                    int count = (int)(long)row.Count;
                    if (stats.ContainsKey(month))
                        stats[month] = count;
                }
                return stats;
            }, "GetMonthlyStatisticsAsync");
        }

        public Task<Dictionary<int, int>> GetMonthlyStatisticsByTypeAsync(int year, string type)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var stats = new Dictionary<int, int>();
                for (int i = 1; i <= 12; i++) stats[i] = 0;

                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT strftime('%m', IssueDate) as Month, COUNT(*) as Count 
                              FROM Certificates 
                              WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND strftime('%Y', IssueDate) = @Year 
                              AND CertificateType LIKE @Type
                              GROUP BY Month";

                var rows = await connection.QueryAsync(query, new { Year = year.ToString(), Type = "%" + type + "%" });
                foreach (var row in rows)
                {
                    int month = Convert.ToInt32((string)row.Month);
                    int count = (int)(long)row.Count;
                    if (stats.ContainsKey(month))
                        stats[month] = count;
                }
                return stats;
            }, "GetMonthlyStatisticsByTypeAsync");
        }

        public Task<int> GetCertificatesCountByTypeAsync(string type, int? year = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Certificates WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND CertificateType LIKE @Type AND (@Year IS NULL OR strftime('%Y', IssueDate) = @Year)",
                    new { Type = "%" + type + "%", Year = year?.ToString() });
            }, "GetCertificatesCountByTypeAsync");
        }

        public Task<int> GetTotalSamplesCountAsync(int? year = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Samples s INNER JOIN Certificates c ON s.CertificateId = c.Id WHERE (c.IsDeleted IS NULL OR c.IsDeleted = 0) AND (@Year IS NULL OR strftime('%Y', c.IssueDate) = @Year)",
                    new { Year = year?.ToString() });
            }, "GetTotalSamplesCountAsync");
        }

        public Task<int> GetSamplesCountByDateAsync(DateTime date)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Samples s 
                      INNER JOIN Certificates c ON s.CertificateId = c.Id 
                      WHERE (c.IsDeleted IS NULL OR c.IsDeleted = 0) AND DATE(c.IssueDate) = DATE(@Date)",
                    new { Date = date.ToString("yyyy-MM-dd") });
            }, "GetSamplesCountByDateAsync");
        }

        public Task<int> GetSamplesCountByTypeAsync(string type, int? year = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Samples s 
                      INNER JOIN Certificates c ON s.CertificateId = c.Id 
                      WHERE (c.IsDeleted IS NULL OR c.IsDeleted = 0) AND c.CertificateType LIKE @Type AND (@Year IS NULL OR strftime('%Y', c.IssueDate) = @Year)",
                    new { Type = "%" + type + "%", Year = year?.ToString() });
            }, "GetSamplesCountByTypeAsync");
        }

        public Task<Dictionary<int, int>> GetMonthlySamplesStatisticsByTypeAsync(int year, string type)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var stats = new Dictionary<int, int>();
                for (int i = 1; i <= 12; i++) stats[i] = 0;

                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT strftime('%m', c.IssueDate) as Month, COUNT(*) as Count 
                              FROM Samples s
                              INNER JOIN Certificates c ON s.CertificateId = c.Id
                              WHERE strftime('%Y', c.IssueDate) = @Year 
                              AND c.CertificateType LIKE @Type
                              AND c.IsDeleted = 0
                              GROUP BY Month";

                var rows = await connection.QueryAsync(query, new { Year = year.ToString(), Type = "%" + type + "%" });
                foreach (var row in rows)
                {
                    int month = Convert.ToInt32((string)row.Month);
                    int count = (int)(long)row.Count;
                    if (stats.ContainsKey(month))
                        stats[month] = count;
                }
                return stats;
            }, "GetMonthlySamplesStatisticsByTypeAsync");
        }

        #endregion
    }
}
