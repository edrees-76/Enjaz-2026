using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// إحصائيات لوحة القيادة — Dashboard Statistics
    /// </summary>
    public partial class CertificateRepository
    {
        #region Dashboard Statistics

        public System.Threading.Tasks.Task<int> GetTotalCertificatesCountAsync(bool showDeleted = false, int? year = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                // Fix: use showDeleted flag to filter soft-deleted records
                var query = showDeleted
                    ? "SELECT COUNT(*) FROM Certificates WHERE (@Year IS NULL OR strftime('%Y', IssueDate) = @Year)"
                    : "SELECT COUNT(*) FROM Certificates WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND (@Year IS NULL OR strftime('%Y', IssueDate) = @Year)";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Year", year?.ToString() ?? (object)DBNull.Value);
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }, "GetTotalCertificatesCountAsync");
        }

        public System.Threading.Tasks.Task<int> GetCertificatesCountByDateAsync(DateTime date)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                var command = new SqliteCommand("SELECT COUNT(*) FROM Certificates WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND DATE(IssueDate) = DATE(@Date)", connection);
                command.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }, "GetCertificatesCountByDateAsync");
        }

        public System.Threading.Tasks.Task<Dictionary<int, int>> GetMonthlyStatisticsAsync(int year)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var stats = new Dictionary<int, int>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                
                for (int i = 1; i <= 12; i++) stats[i] = 0;

                var query = @"SELECT strftime('%m', IssueDate) as Month, COUNT(*) as Count 
                              FROM Certificates 
                              WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND strftime('%Y', IssueDate) = @Year 
                              GROUP BY Month";
                
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Year", year.ToString());
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int month = Convert.ToInt32(reader.GetString(0));
                    int count = reader.GetInt32(1);
                    if (stats.ContainsKey(month))
                        stats[month] = count;
                }
                return stats;
            }, "GetMonthlyStatisticsAsync");
        }

        public System.Threading.Tasks.Task<Dictionary<int, int>> GetMonthlyStatisticsByTypeAsync(int year, string type)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var stats = new Dictionary<int, int>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                
                for (int i = 1; i <= 12; i++) stats[i] = 0;

                var query = @"SELECT strftime('%m', IssueDate) as Month, COUNT(*) as Count 
                              FROM Certificates 
                              WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND strftime('%Y', IssueDate) = @Year 
                              AND CertificateType LIKE @Type
                              GROUP BY Month";
                
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Year", year.ToString());
                command.Parameters.AddWithValue("@Type", "%" + type + "%");
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int month = Convert.ToInt32(reader.GetString(0));
                    int count = reader.GetInt32(1);
                    if (stats.ContainsKey(month))
                        stats[month] = count;
                }
                return stats;
            }, "GetMonthlyStatisticsByTypeAsync");
        }

        public System.Threading.Tasks.Task<int> GetCertificatesCountByTypeAsync(string type, int? year = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                var query = "SELECT COUNT(*) FROM Certificates WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND CertificateType LIKE @Type AND (@Year IS NULL OR strftime('%Y', IssueDate) = @Year)";
                var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Type", "%" + type + "%");
                command.Parameters.AddWithValue("@Year", year?.ToString() ?? (object)DBNull.Value);
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }, "GetCertificatesCountByTypeAsync");
        }

        public System.Threading.Tasks.Task<int> GetTotalSamplesCountAsync(int? year = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                var query = "SELECT COUNT(*) FROM Samples s INNER JOIN Certificates c ON s.CertificateId = c.Id WHERE (c.IsDeleted IS NULL OR c.IsDeleted = 0) AND (@Year IS NULL OR strftime('%Y', c.IssueDate) = @Year)";
                var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Year", year?.ToString() ?? (object)DBNull.Value);
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }, "GetTotalSamplesCountAsync");
        }

        public System.Threading.Tasks.Task<int> GetSamplesCountByDateAsync(DateTime date)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                var command = new SqliteCommand(@"SELECT COUNT(*) FROM Samples s 
                    INNER JOIN Certificates c ON s.CertificateId = c.Id 
                    WHERE (c.IsDeleted IS NULL OR c.IsDeleted = 0) AND DATE(c.IssueDate) = DATE(@Date)", connection);
                command.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }, "GetSamplesCountByDateAsync");
        }

        public System.Threading.Tasks.Task<int> GetSamplesCountByTypeAsync(string type, int? year = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                var query = @"SELECT COUNT(*) FROM Samples s 
                    INNER JOIN Certificates c ON s.CertificateId = c.Id 
                    WHERE (c.IsDeleted IS NULL OR c.IsDeleted = 0) AND c.CertificateType LIKE @Type AND (@Year IS NULL OR strftime('%Y', c.IssueDate) = @Year)";
                var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Type", "%" + type + "%");
                command.Parameters.AddWithValue("@Year", year?.ToString() ?? (object)DBNull.Value);
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }, "GetSamplesCountByTypeAsync");
        }

        public System.Threading.Tasks.Task<Dictionary<int, int>> GetMonthlySamplesStatisticsByTypeAsync(int year, string type)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var stats = new Dictionary<int, int>();
                using var connection = new SqliteConnection(_db.ConnectionString);
                await connection.OpenAsync();
                
                for (int i = 1; i <= 12; i++) stats[i] = 0;

                var query = @"SELECT strftime('%m', c.IssueDate) as Month, COUNT(*) as Count 
                              FROM Samples s
                              INNER JOIN Certificates c ON s.CertificateId = c.Id
                              WHERE strftime('%Y', c.IssueDate) = @Year 
                              AND c.CertificateType LIKE @Type
                              AND c.IsDeleted = 0
                              GROUP BY Month";
                
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Year", year.ToString());
                command.Parameters.AddWithValue("@Type", "%" + type + "%");
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int month = Convert.ToInt32(reader.GetString(0));
                    int count = reader.GetInt32(1);
                    if (stats.ContainsKey(month))
                        stats[month] = count;
                }
                return stats;
            }, "GetMonthlySamplesStatisticsByTypeAsync");
        }

        #endregion
    }
}
