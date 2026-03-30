using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Enjaz.Helpers;

namespace Enjaz.Services
{
    public class ArchiveService
    {
        private readonly DatabaseService _dbService;
        private readonly string _archiveDbPath;

        public ArchiveService(DatabaseService dbService)
        {
            _dbService = dbService;
            
            // Set archive path next to the main database
            string mainDbPath = new SqliteConnectionStringBuilder(dbService.ConnectionString).DataSource;
            string directory = Path.GetDirectoryName(mainDbPath) ?? "";
            _archiveDbPath = Path.Combine(directory, "archive.db");
        }

        public async Task<int> ArchiveOldLogsAsync(int monthsToKeep)
        {
            DateTime cutoffDate = DateTime.Now.AddMonths(-monthsToKeep);
            string cutoffDateStr = cutoffDate.ToString("yyyy-MM-dd HH:mm:ss");

            int archivedCount = 0;

            try
            {
                InitializeArchiveDatabase();

                using (var mainConnection = new SqliteConnection(_dbService.ConnectionString))
                {
                    await mainConnection.OpenAsync();

                    string attachQuery = $"ATTACH DATABASE '{_archiveDbPath}' AS archiveDB;";
                    using (var attachCmd = new SqliteCommand(attachQuery, mainConnection))
                    {
                        attachCmd.ExecuteNonQuery();
                    }

                    using (var transaction = mainConnection.BeginTransaction())
                    {
                        try
                        {
                            string copyQuery = @"
                                INSERT INTO archiveDB.AuditLogs (UserId, UserName, Action, Details, Timestamp, ReferenceId)
                                SELECT UserId, UserName, Action, Details, Timestamp, ReferenceId
                                FROM main.AuditLogs
                                WHERE Timestamp < @CutoffDate;";

                            using (var copyCmd = new SqliteCommand(copyQuery, mainConnection, transaction))
                            {
                                copyCmd.Parameters.AddWithValue("@CutoffDate", cutoffDateStr);
                                archivedCount = await copyCmd.ExecuteNonQueryAsync();
                            }

                            if (archivedCount > 0)
                            {
                                string deleteQuery = "DELETE FROM main.AuditLogs WHERE Timestamp < @CutoffDate;";
                                using (var deleteCmd = new SqliteCommand(deleteQuery, mainConnection, transaction))
                                {
                                    deleteCmd.Parameters.AddWithValue("@CutoffDate", cutoffDateStr);
                                    await deleteCmd.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            LoggerService.LogInfo($"Archived {archivedCount} logs older than {cutoffDateStr} to {_archiveDbPath}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            LoggerService.LogError("Log archive transaction failed", ex);
                            throw;
                        }
                        finally
                        {
                            using (var detachCmd = new SqliteCommand("DETACH DATABASE archiveDB;", mainConnection))
                            {
                                try { detachCmd.ExecuteNonQuery(); } catch { /* Ignore */ }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Critical error in ArchiveOldLogsAsync", ex);
                throw;
            }

            return archivedCount;
        }

        public async Task<int> ArchiveYearAsync(int year)
        {
            int certCount = 0;
            try
            {
                InitializeArchiveDatabase();

                string startYear = $"{year}-01-01 00:00:00";
                string endYear = $"{year}-12-31 23:59:59";

                using (var mainConnection = new SqliteConnection(_dbService.ConnectionString))
                {
                    await mainConnection.OpenAsync();

                    string attachQuery = $"ATTACH DATABASE '{_archiveDbPath}' AS archiveDB;";
                    using (var attachCmd = new SqliteCommand(attachQuery, mainConnection))
                    {
                        attachCmd.ExecuteNonQuery();
                    }

                    using (var transaction = mainConnection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Copy Samples for certificates of that year
                            string copySamplesQuery = @"
                                INSERT INTO archiveDB.Samples (CertificateId, Root, SampleNumber, Description, MeasurementDate, Result, IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137)
                                SELECT s.CertificateId, s.Root, s.SampleNumber, s.Description, s.MeasurementDate, s.Result, s.IsotopeK40, s.IsotopeRa226, s.IsotopeTh232, s.IsotopeRa, s.IsotopeCs137
                                FROM main.Samples s
                                INNER JOIN main.Certificates c ON s.CertificateId = c.Id
                                WHERE c.IssueDate >= @Start AND c.IssueDate <= @End;";

                            using (var cmd = new SqliteCommand(copySamplesQuery, mainConnection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Start", startYear);
                                cmd.Parameters.AddWithValue("@End", endYear);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // 2. Copy Certificates
                            string copyCertsQuery = @"
                                INSERT INTO archiveDB.Certificates (Id, CertificateNumber, RecipientName, CertificateType, Description, IssueDate, ExpiryDate, IssuingAuthority, CreatedBy, AnalysisType, Sender, Supplier, Origin, DeclarationNumber, PolicyNumber, NotificationNumber, FinancialReceiptNumber, SpecialistName, SectionHeadName, ManagerName, Notes, CreatedAt, UpdatedBy, UpdatedByName, UpdatedAt)
                                SELECT Id, CertificateNumber, RecipientName, CertificateType, Description, IssueDate, ExpiryDate, IssuingAuthority, CreatedBy, AnalysisType, Sender, Supplier, Origin, DeclarationNumber, PolicyNumber, NotificationNumber, FinancialReceiptNumber, SpecialistName, SectionHeadName, ManagerName, Notes, CreatedAt, UpdatedBy, UpdatedByName, UpdatedAt
                                FROM main.Certificates
                                WHERE IssueDate >= @Start AND IssueDate <= @End;";

                            using (var cmd = new SqliteCommand(copyCertsQuery, mainConnection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Start", startYear);
                                cmd.Parameters.AddWithValue("@End", endYear);
                                certCount = await cmd.ExecuteNonQueryAsync();
                            }

                            if (certCount > 0)
                            {
                                // Delete Samples first due to constraints (though SQLite cascade might be on)
                                string deleteSamplesQuery = @"
                                    DELETE FROM main.Samples 
                                    WHERE CertificateId IN (SELECT Id FROM main.Certificates WHERE IssueDate >= @Start AND IssueDate <= @End);";
                                using (var cmd = new SqliteCommand(deleteSamplesQuery, mainConnection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@Start", startYear);
                                    cmd.Parameters.AddWithValue("@End", endYear);
                                    await cmd.ExecuteNonQueryAsync();
                                }

                                string deleteCertsQuery = "DELETE FROM main.Certificates WHERE IssueDate >= @Start AND IssueDate <= @End;";
                                using (var cmd = new SqliteCommand(deleteCertsQuery, mainConnection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@Start", startYear);
                                    cmd.Parameters.AddWithValue("@End", endYear);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            LoggerService.LogInfo($"Archived {certCount} certificates for year {year} to {_archiveDbPath}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            LoggerService.LogError($"Archive transaction for year {year} failed", ex);
                            throw;
                        }
                        finally
                        {
                            using (var detachCmd = new SqliteCommand("DETACH DATABASE archiveDB;", mainConnection))
                            {
                                try { detachCmd.ExecuteNonQuery(); } catch { /* Ignore */ }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"Critical error in Archiving year {year}", ex);
                throw;
            }
            return certCount;
        }

        private void InitializeArchiveDatabase()
        {
            if (!File.Exists(_archiveDbPath))
            {
                File.Create(_archiveDbPath).Close();
            }

            using (var connection = new SqliteConnection($"Data Source={_archiveDbPath}"))
            {
                connection.Open();

                string createTables = @"
                    CREATE TABLE IF NOT EXISTS AuditLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER,
                        UserName TEXT,
                        Action TEXT NOT NULL,
                        Details TEXT,
                        Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,
                        ReferenceId INTEGER
                    );

                    CREATE TABLE IF NOT EXISTS Certificates (
                        Id INTEGER PRIMARY KEY,
                        CertificateNumber TEXT NOT NULL,
                        RecipientName TEXT NOT NULL,
                        CertificateType TEXT NOT NULL,
                        Description TEXT,
                        IssueDate TEXT NOT NULL,
                        ExpiryDate TEXT,
                        IssuingAuthority TEXT,
                        CreatedBy INTEGER NOT NULL,
                        AnalysisType TEXT,
                        Sender TEXT,
                        Supplier TEXT,
                        Origin TEXT,
                        DeclarationNumber TEXT,
                        PolicyNumber TEXT,
                        NotificationNumber TEXT,
                        FinancialReceiptNumber TEXT,
                        SpecialistName TEXT,
                        SectionHeadName TEXT,
                        ManagerName TEXT,
                        Notes TEXT,
                        CreatedAt TEXT,
                        UpdatedBy INTEGER,
                        UpdatedByName TEXT,
                        UpdatedAt TEXT
                    );

                    CREATE TABLE IF NOT EXISTS Samples (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CertificateId INTEGER NOT NULL,
                        Root INTEGER NOT NULL,
                        SampleNumber TEXT,
                        Description TEXT,
                        MeasurementDate TEXT,
                        Result TEXT,
                        IsotopeK40 TEXT,
                        IsotopeRa226 TEXT,
                        IsotopeTh232 TEXT,
                        IsotopeRa TEXT,
                        IsotopeCs137 TEXT
                    );";

                using (var command = new SqliteCommand(createTables, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
        
        public string GetArchivePath() => _archiveDbPath;
    }
}
