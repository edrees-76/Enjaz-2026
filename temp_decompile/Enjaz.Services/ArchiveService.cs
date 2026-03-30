using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Enjaz.Services;

public class ArchiveService
{
	private readonly DatabaseService _dbService;

	private readonly string _archiveDbPath;

	public ArchiveService(DatabaseService dbService)
	{
		_dbService = dbService;
		string dataSource = new SqliteConnectionStringBuilder(dbService.ConnectionString).DataSource;
		string path = Path.GetDirectoryName(dataSource) ?? "";
		_archiveDbPath = Path.Combine(path, "archive.db");
	}

	public async Task<int> ArchiveOldLogsAsync(int monthsToKeep)
	{
		string cutoffDateStr = DateTime.Now.AddMonths(-monthsToKeep).ToString("yyyy-MM-dd HH:mm:ss");
		int archivedCount = 0;
		try
		{
			InitializeArchiveDatabase();
			using (SqliteConnection mainConnection = new SqliteConnection(_dbService.ConnectionString))
			{
				await mainConnection.OpenAsync();
				string attachQuery = "ATTACH DATABASE '" + _archiveDbPath + "' AS archiveDB;";
				using (SqliteCommand attachCmd = new SqliteCommand(attachQuery, mainConnection))
				{
					attachCmd.ExecuteNonQuery();
				}
				using SqliteTransaction transaction = mainConnection.BeginTransaction();
				try
				{
					string copyQuery = "\r\n                                INSERT INTO archiveDB.AuditLogs (UserId, UserName, Action, Details, Timestamp, ReferenceId)\r\n                                SELECT UserId, UserName, Action, Details, Timestamp, ReferenceId\r\n                                FROM main.AuditLogs\r\n                                WHERE Timestamp < @CutoffDate;";
					using (SqliteCommand copyCmd = new SqliteCommand(copyQuery, mainConnection, transaction))
					{
						copyCmd.Parameters.AddWithValue("@CutoffDate", cutoffDateStr);
						archivedCount = await copyCmd.ExecuteNonQueryAsync();
					}
					if (archivedCount > 0)
					{
						string deleteQuery = "DELETE FROM main.AuditLogs WHERE Timestamp < @CutoffDate;";
						using SqliteCommand deleteCmd = new SqliteCommand(deleteQuery, mainConnection, transaction);
						deleteCmd.Parameters.AddWithValue("@CutoffDate", cutoffDateStr);
						await deleteCmd.ExecuteNonQueryAsync();
					}
					transaction.Commit();
					LoggerService.LogInfo($"Archived {archivedCount} logs older than {cutoffDateStr} to {_archiveDbPath}");
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					transaction.Rollback();
					LoggerService.LogError("Log archive transaction failed", ex2);
					throw;
				}
				finally
				{
					using SqliteCommand detachCmd = new SqliteCommand("DETACH DATABASE archiveDB;", mainConnection);
					try
					{
						detachCmd.ExecuteNonQuery();
					}
					catch
					{
					}
				}
			}
			return archivedCount;
		}
		catch (Exception ex)
		{
			Exception ex3 = ex;
			LoggerService.LogError("Critical error in ArchiveOldLogsAsync", ex3);
			throw;
		}
	}

	public async Task<int> ArchiveYearAsync(int year)
	{
		int certCount = 0;
		try
		{
			InitializeArchiveDatabase();
			string startYear = $"{year}-01-01 00:00:00";
			string endYear = $"{year}-12-31 23:59:59";
			using (SqliteConnection mainConnection = new SqliteConnection(_dbService.ConnectionString))
			{
				await mainConnection.OpenAsync();
				string attachQuery = "ATTACH DATABASE '" + _archiveDbPath + "' AS archiveDB;";
				using (SqliteCommand attachCmd = new SqliteCommand(attachQuery, mainConnection))
				{
					attachCmd.ExecuteNonQuery();
				}
				using SqliteTransaction transaction = mainConnection.BeginTransaction();
				try
				{
					string copySamplesQuery = "\r\n                                INSERT INTO archiveDB.Samples (CertificateId, Root, SampleNumber, Description, MeasurementDate, Result, IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137)\r\n                                SELECT s.CertificateId, s.Root, s.SampleNumber, s.Description, s.MeasurementDate, s.Result, s.IsotopeK40, s.IsotopeRa226, s.IsotopeTh232, s.IsotopeRa, s.IsotopeCs137\r\n                                FROM main.Samples s\r\n                                INNER JOIN main.Certificates c ON s.CertificateId = c.Id\r\n                                WHERE c.IssueDate >= @Start AND c.IssueDate <= @End;";
					using (SqliteCommand cmd = new SqliteCommand(copySamplesQuery, mainConnection, transaction))
					{
						cmd.Parameters.AddWithValue("@Start", startYear);
						cmd.Parameters.AddWithValue("@End", endYear);
						await cmd.ExecuteNonQueryAsync();
					}
					string copyCertsQuery = "\r\n                                INSERT INTO archiveDB.Certificates (Id, CertificateNumber, RecipientName, CertificateType, Description, IssueDate, ExpiryDate, IssuingAuthority, CreatedBy, AnalysisType, Sender, Supplier, Origin, DeclarationNumber, PolicyNumber, NotificationNumber, FinancialReceiptNumber, SpecialistName, SectionHeadName, ManagerName, Notes, CreatedAt, UpdatedBy, UpdatedByName, UpdatedAt)\r\n                                SELECT Id, CertificateNumber, RecipientName, CertificateType, Description, IssueDate, ExpiryDate, IssuingAuthority, CreatedBy, AnalysisType, Sender, Supplier, Origin, DeclarationNumber, PolicyNumber, NotificationNumber, FinancialReceiptNumber, SpecialistName, SectionHeadName, ManagerName, Notes, CreatedAt, UpdatedBy, UpdatedByName, UpdatedAt\r\n                                FROM main.Certificates\r\n                                WHERE IssueDate >= @Start AND IssueDate <= @End;";
					using (SqliteCommand cmd2 = new SqliteCommand(copyCertsQuery, mainConnection, transaction))
					{
						cmd2.Parameters.AddWithValue("@Start", startYear);
						cmd2.Parameters.AddWithValue("@End", endYear);
						certCount = await cmd2.ExecuteNonQueryAsync();
					}
					if (certCount > 0)
					{
						string deleteSamplesQuery = "\r\n                                    DELETE FROM main.Samples \r\n                                    WHERE CertificateId IN (SELECT Id FROM main.Certificates WHERE IssueDate >= @Start AND IssueDate <= @End);";
						using (SqliteCommand cmd3 = new SqliteCommand(deleteSamplesQuery, mainConnection, transaction))
						{
							cmd3.Parameters.AddWithValue("@Start", startYear);
							cmd3.Parameters.AddWithValue("@End", endYear);
							await cmd3.ExecuteNonQueryAsync();
						}
						string deleteCertsQuery = "DELETE FROM main.Certificates WHERE IssueDate >= @Start AND IssueDate <= @End;";
						using SqliteCommand cmd4 = new SqliteCommand(deleteCertsQuery, mainConnection, transaction);
						cmd4.Parameters.AddWithValue("@Start", startYear);
						cmd4.Parameters.AddWithValue("@End", endYear);
						await cmd4.ExecuteNonQueryAsync();
					}
					transaction.Commit();
					LoggerService.LogInfo($"Archived {certCount} certificates for year {year} to {_archiveDbPath}");
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					transaction.Rollback();
					LoggerService.LogError($"Archive transaction for year {year} failed", ex2);
					throw;
				}
				finally
				{
					using SqliteCommand detachCmd = new SqliteCommand("DETACH DATABASE archiveDB;", mainConnection);
					try
					{
						detachCmd.ExecuteNonQuery();
					}
					catch
					{
					}
				}
			}
			return certCount;
		}
		catch (Exception ex)
		{
			LoggerService.LogError(ex: ex, message: $"Critical error in Archiving year {year}");
			throw;
		}
	}

	private void InitializeArchiveDatabase()
	{
		if (!File.Exists(_archiveDbPath))
		{
			File.Create(_archiveDbPath).Close();
		}
		using SqliteConnection sqliteConnection = new SqliteConnection("Data Source=" + _archiveDbPath);
		sqliteConnection.Open();
		string commandText = "\r\n                    CREATE TABLE IF NOT EXISTS AuditLogs (\r\n                        Id INTEGER PRIMARY KEY AUTOINCREMENT,\r\n                        UserId INTEGER,\r\n                        UserName TEXT,\r\n                        Action TEXT NOT NULL,\r\n                        Details TEXT,\r\n                        Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,\r\n                        ReferenceId INTEGER\r\n                    );\r\n\r\n                    CREATE TABLE IF NOT EXISTS Certificates (\r\n                        Id INTEGER PRIMARY KEY,\r\n                        CertificateNumber TEXT NOT NULL,\r\n                        RecipientName TEXT NOT NULL,\r\n                        CertificateType TEXT NOT NULL,\r\n                        Description TEXT,\r\n                        IssueDate TEXT NOT NULL,\r\n                        ExpiryDate TEXT,\r\n                        IssuingAuthority TEXT,\r\n                        CreatedBy INTEGER NOT NULL,\r\n                        AnalysisType TEXT,\r\n                        Sender TEXT,\r\n                        Supplier TEXT,\r\n                        Origin TEXT,\r\n                        DeclarationNumber TEXT,\r\n                        PolicyNumber TEXT,\r\n                        NotificationNumber TEXT,\r\n                        FinancialReceiptNumber TEXT,\r\n                        SpecialistName TEXT,\r\n                        SectionHeadName TEXT,\r\n                        ManagerName TEXT,\r\n                        Notes TEXT,\r\n                        CreatedAt TEXT,\r\n                        UpdatedBy INTEGER,\r\n                        UpdatedByName TEXT,\r\n                        UpdatedAt TEXT\r\n                    );\r\n\r\n                    CREATE TABLE IF NOT EXISTS Samples (\r\n                        Id INTEGER PRIMARY KEY AUTOINCREMENT,\r\n                        CertificateId INTEGER NOT NULL,\r\n                        Root INTEGER NOT NULL,\r\n                        SampleNumber TEXT,\r\n                        Description TEXT,\r\n                        MeasurementDate TEXT,\r\n                        Result TEXT,\r\n                        IsotopeK40 TEXT,\r\n                        IsotopeRa226 TEXT,\r\n                        IsotopeTh232 TEXT,\r\n                        IsotopeRa TEXT,\r\n                        IsotopeCs137 TEXT\r\n                    );";
		using SqliteCommand sqliteCommand = new SqliteCommand(commandText, sqliteConnection);
		sqliteCommand.ExecuteNonQuery();
	}

	public string GetArchivePath()
	{
		return _archiveDbPath;
	}
}
