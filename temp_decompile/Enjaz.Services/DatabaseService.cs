using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Enjaz.Models;
using Microsoft.Data.Sqlite;

namespace Enjaz.Services;

public class DatabaseService
{
	private readonly string _connectionString;

	private readonly string _dbPath;

	private static readonly SemaphoreSlim _maintenanceLock = new SemaphoreSlim(1, 1);

	private const int MaxRetries = 3;

	private const int BaseDelayMs = 100;

	public string ConnectionString => _connectionString;

	public DatabaseService(SettingsService settingsService)
	{
		string databasePath = settingsService.Current.DatabasePath;
		if (string.IsNullOrEmpty(databasePath))
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string text = Path.Combine(folderPath, "Enjaz");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			_dbPath = Path.Combine(text, "certificates.db");
		}
		else
		{
			_dbPath = databasePath;
			if (!Path.HasExtension(_dbPath))
			{
				_dbPath = Path.Combine(_dbPath, "certificates.db");
			}
			string directoryName = Path.GetDirectoryName(_dbPath);
			if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
			{
				try
				{
					Directory.CreateDirectory(directoryName);
				}
				catch (Exception ex)
				{
					LoggerService.LogError("Failed to create network directory: " + directoryName + ". Falling back to default.", ex);
				}
			}
		}
		_connectionString = "Data Source=" + _dbPath;
		InitializeDatabase();
		AutoBackup();
	}

	public void ResetDatabase()
	{
		try
		{
			using SqliteConnection sqliteConnection = new SqliteConnection(_connectionString);
			sqliteConnection.Open();
			string commandText = "\n                    DELETE FROM Samples; \n                    DELETE FROM Certificates; \n                    DELETE FROM sqlite_sequence WHERE name IN ('Certificates', 'Samples');\n                    VACUUM;";
			using SqliteCommand sqliteCommand = new SqliteCommand(commandText, sqliteConnection);
			sqliteCommand.ExecuteNonQuery();
			LoggerService.LogInfo("Database Reset: All certificates and samples have been cleared for the new year.");
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Database Reset Failed", ex);
		}
	}

	public async Task<bool> PerformFactoryResetAsync(int currentUserId)
	{
		await _maintenanceLock.WaitAsync();
		try
		{
			using SqliteConnection connection = new SqliteConnection(_connectionString);
			await connection.OpenAsync();
			using SqliteTransaction transaction = connection.BeginTransaction();
			try
			{
				string clearDataQuery = "\n                        DELETE FROM Samples;\n                        DELETE FROM Certificates;\n                        DELETE FROM AuditLogs;\n                        DELETE FROM ReferralLetters;";
				using (SqliteCommand cmd = new SqliteCommand(clearDataQuery, connection, transaction))
				{
					cmd.ExecuteNonQuery();
				}
				string clearUsersQuery = "DELETE FROM Users WHERE Id != @AdminId;";
				using (SqliteCommand cmd2 = new SqliteCommand(clearUsersQuery, connection, transaction))
				{
					cmd2.Parameters.AddWithValue("@AdminId", currentUserId);
					cmd2.ExecuteNonQuery();
				}
				string resetSeqQuery = "DELETE FROM sqlite_sequence;";
				using (SqliteCommand cmd3 = new SqliteCommand(resetSeqQuery, connection, transaction))
				{
					cmd3.ExecuteNonQuery();
				}
				transaction.Commit();
				using (SqliteCommand cmd4 = new SqliteCommand("VACUUM;", connection))
				{
					cmd4.ExecuteNonQuery();
				}
				LoggerService.LogInfo($"Factory Reset performed by User ID: {currentUserId}");
				return true;
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				transaction.Rollback();
				LoggerService.LogError("Factory Reset transaction failed", ex2);
				return false;
			}
		}
		finally
		{
			_maintenanceLock.Release();
		}
	}

	internal T ExecuteWithRetry<T>(Func<T> operation, string operationName = "Database Operation")
	{
		int num = 0;
		while (true)
		{
			try
			{
				DateTime now = DateTime.Now;
				T result = operation();
				double totalMilliseconds = (DateTime.Now - now).TotalMilliseconds;
				if (totalMilliseconds > 500.0)
				{
					LoggerService.LogWarning($"Slow DB Operation: {operationName} took {totalMilliseconds:F0}ms");
				}
				return result;
			}
			catch (SqliteException ex) when (num < 3 && IsTransientError(ex))
			{
				num++;
				int num2 = 100 * (int)Math.Pow(2.0, num);
				LoggerService.LogWarning($"DB Retry {num}/{3} for {operationName}: {ex.Message}. Waiting {num2}ms...");
				Task.Delay(num2).GetAwaiter().GetResult();
			}
			catch (Exception ex2)
			{
				LoggerService.LogError("DB Operation Failed: " + operationName, ex2);
				throw;
			}
		}
	}

	internal async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName = "Database Operation")
	{
		int attempt = 0;
		while (true)
		{
			try
			{
				DateTime startTime = DateTime.Now;
				T result = await operation();
				double duration = (DateTime.Now - startTime).TotalMilliseconds;
				if (duration > 500.0)
				{
					LoggerService.LogWarning($"Slow Async DB Operation: {operationName} took {duration:F0}ms");
				}
				return result;
			}
			catch (SqliteException ex) when (attempt < 3 && IsTransientError(ex))
			{
				attempt++;
				int delay = 100 * (int)Math.Pow(2.0, attempt);
				LoggerService.LogWarning($"Async DB Retry {attempt}/{3} for {operationName}: {ex.Message}. Waiting {delay}ms...");
				await Task.Delay(delay);
			}
			catch (Exception ex2)
			{
				Exception ex3 = ex2;
				LoggerService.LogError("Async DB Operation Failed: " + operationName, ex3);
				throw;
			}
		}
	}

	internal async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName = "Database Operation")
	{
		await ExecuteWithRetryAsync(async delegate
		{
			await operation();
			return true;
		}, operationName);
	}

	internal void ExecuteWithRetry(Action operation, string operationName = "Database Operation")
	{
		ExecuteWithRetry(delegate
		{
			operation();
			return true;
		}, operationName);
	}

	private static bool IsTransientError(SqliteException ex)
	{
		return ex.SqliteErrorCode == 5 || ex.SqliteErrorCode == 6 || ex.SqliteErrorCode == 7;
	}

	private void InitializeDatabase()
	{
		using SqliteConnection sqliteConnection = new SqliteConnection(_connectionString);
		sqliteConnection.Open();
		using (SqliteCommand sqliteCommand = new SqliteCommand("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON;", sqliteConnection))
		{
			sqliteCommand.ExecuteNonQuery();
		}
		string commandText = "\n                CREATE TABLE IF NOT EXISTS Users (\n                    Id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    Username TEXT UNIQUE NOT NULL,\n                    PasswordHash TEXT NOT NULL,\n                    FullName TEXT NOT NULL,\n                    Role INTEGER DEFAULT 1,\n                    IsActive INTEGER DEFAULT 1,\n                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP\n                );";
		string commandText2 = "\n                CREATE TABLE IF NOT EXISTS AuditLogs (\n                    Id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    UserId INTEGER,\n                    UserName TEXT,\n                    Action TEXT NOT NULL,\n                    Details TEXT,\n                    Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,\n                    ReferenceId INTEGER,\n                    FOREIGN KEY (UserId) REFERENCES Users(Id)\n                );";
		string commandText3 = "\n                CREATE TABLE IF NOT EXISTS Certificates (\n                    Id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    CertificateNumber TEXT UNIQUE NOT NULL,\n                    RecipientName TEXT NOT NULL,\n                    CertificateType TEXT NOT NULL,\n                    Description TEXT,\n                    IssueDate TEXT NOT NULL,\n                    ExpiryDate TEXT,\n                    IssuingAuthority TEXT,\n                    CreatedBy INTEGER NOT NULL,\n                    \n                    -- New Fields\n                    AnalysisType TEXT,\n                    Sender TEXT,\n                    Supplier TEXT,\n                    Origin TEXT,\n                    DeclarationNumber TEXT,\n                    PolicyNumber TEXT,\n                    NotificationNumber TEXT,\n                    FinancialReceiptNumber TEXT,\n                    SpecialistName TEXT,\n                    SectionHeadName TEXT,\n                    ManagerName TEXT,\n                    Notes TEXT,\n                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,\n                    UpdatedBy INTEGER,\n                    UpdatedByName TEXT,\n                    UpdatedAt TEXT,\n                    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)\n                );";
		string commandText4 = "\n                CREATE TABLE IF NOT EXISTS Samples (\n                    Id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    CertificateId INTEGER NOT NULL,\n                    Root INTEGER NOT NULL,\n                    SampleNumber TEXT,\n                    Description TEXT,\n                    MeasurementDate TEXT,\n                    Result TEXT,\n                    \n                    -- Environmental Fields\n                    IsotopeK40 TEXT,\n                    IsotopeRa226 TEXT,\n                    IsotopeTh232 TEXT,\n                    IsotopeRa TEXT,\n                    IsotopeCs137 TEXT,\n\n                    FOREIGN KEY (CertificateId) REFERENCES Certificates(Id) ON DELETE CASCADE\n                );";
		string commandText5 = "\n                CREATE TABLE IF NOT EXISTS ReferralLetters (\n                    Id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    GeneratedAt TEXT DEFAULT CURRENT_TIMESTAMP,\n                    SenderName TEXT NOT NULL,\n                    CertificateCount INTEGER NOT NULL,\n                    SampleCount INTEGER NOT NULL,\n                    OutputPath TEXT NOT NULL,\n                    StartDate TEXT NOT NULL,\n                    EndDate TEXT NOT NULL,\n                    IncludedColumns TEXT\n                );";
		string commandText6 = "\n                CREATE TABLE IF NOT EXISTS SampleReceptions (\n                    Id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    AnalysisRequestNumber TEXT NOT NULL,\n                    NotificationNumber TEXT,\n                    DeclarationNumber TEXT,\n                    Supplier TEXT,\n                    Sender TEXT,\n                    Origin TEXT,\n                    PolicyNumber TEXT,\n                    FinancialReceiptNumber TEXT,\n                    CertificateType TEXT NOT NULL,\n                    Date TEXT NOT NULL,\n                    Status TEXT DEFAULT 'في انتظار إصدار شهادة',\n                    CreatedBy INTEGER NOT NULL,\n                    CreatedByName TEXT,\n                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,\n                    UpdatedBy INTEGER,\n                    UpdatedByName TEXT,\n                    UpdatedAt TEXT,\n                    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)\n                );";
		string commandText7 = "\n                CREATE TABLE IF NOT EXISTS ReceptionSamples (\n                    Id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    ReceptionId INTEGER NOT NULL,\n                    SampleNumber TEXT,\n                    Description TEXT,\n                    FOREIGN KEY (ReceptionId) REFERENCES SampleReceptions(Id) ON DELETE CASCADE\n                );";
		using (SqliteCommand sqliteCommand2 = new SqliteCommand(commandText, sqliteConnection))
		{
			sqliteCommand2.ExecuteNonQuery();
		}
		using (SqliteCommand sqliteCommand3 = new SqliteCommand(commandText2, sqliteConnection))
		{
			sqliteCommand3.ExecuteNonQuery();
		}
		using (SqliteCommand sqliteCommand4 = new SqliteCommand(commandText3, sqliteConnection))
		{
			sqliteCommand4.ExecuteNonQuery();
		}
		using (SqliteCommand sqliteCommand5 = new SqliteCommand(commandText4, sqliteConnection))
		{
			sqliteCommand5.ExecuteNonQuery();
		}
		using (SqliteCommand sqliteCommand6 = new SqliteCommand(commandText5, sqliteConnection))
		{
			sqliteCommand6.ExecuteNonQuery();
		}
		using (SqliteCommand sqliteCommand7 = new SqliteCommand(commandText6, sqliteConnection))
		{
			sqliteCommand7.ExecuteNonQuery();
		}
		using (SqliteCommand sqliteCommand8 = new SqliteCommand(commandText7, sqliteConnection))
		{
			sqliteCommand8.ExecuteNonQuery();
		}
		string commandText8 = "\n                CREATE TABLE IF NOT EXISTS HelpContent (\n                    Id INTEGER PRIMARY KEY AUTOINCREMENT,\n                    Title TEXT NOT NULL,\n                    Abstract TEXT,\n                    ContentSimple TEXT,\n                    ContentAdvanced TEXT,\n                    Category TEXT,\n                    IconKind TEXT,\n                    Keywords TEXT,\n                    RelatedView TEXT,\n                    VideoUrl TEXT,\n                    Views INTEGER DEFAULT 0,\n                    IsHelpfulCount INTEGER DEFAULT 0\n                );";
		using (SqliteCommand sqliteCommand9 = new SqliteCommand(commandText8, sqliteConnection))
		{
			sqliteCommand9.ExecuteNonQuery();
		}
		UpgradeDatabase(sqliteConnection);
		CreateIndexes(sqliteConnection);
		SetupFTS(sqliteConnection);
		MigrateStatuses(sqliteConnection);
	}

	private void MigrateStatuses(SqliteConnection connection)
	{
		try
		{
			string commandText = "\n                    UPDATE SampleReceptions SET Status = 'في انتظار إصدار شهادة' WHERE Status IN ('جديد', 'قيد الانتظار');\n                    UPDATE SampleReceptions SET Status = 'تم إصدار شهادة' WHERE Status = 'مكتملة';";
			using SqliteCommand sqliteCommand = new SqliteCommand(commandText, connection);
			sqliteCommand.ExecuteNonQuery();
		}
		catch (Exception ex)
		{
			LoggerService.LogWarning("Migration of statuses failed: " + ex.Message);
		}
	}

	private void CreateIndexes(SqliteConnection connection)
	{
		try
		{
			string commandText = "\n                    CREATE INDEX IF NOT EXISTS IX_Certificates_IssueDate ON Certificates(IssueDate);\n                    CREATE INDEX IF NOT EXISTS IX_Certificates_CertificateNumber ON Certificates(CertificateNumber);\n                    CREATE INDEX IF NOT EXISTS IX_Certificates_Sender ON Certificates(Sender);\n                    CREATE INDEX IF NOT EXISTS IX_Certificates_Supplier ON Certificates(Supplier);\n                    CREATE INDEX IF NOT EXISTS IX_Certificates_CreatedAt ON Certificates(CreatedAt);\n                    CREATE INDEX IF NOT EXISTS IX_ReferralLetters_GeneratedAt ON ReferralLetters(GeneratedAt);\n                    CREATE INDEX IF NOT EXISTS IX_ReferralLetters_SenderName ON ReferralLetters(SenderName);\n                    CREATE INDEX IF NOT EXISTS IX_Samples_CertificateId ON Samples(CertificateId);\n                    CREATE INDEX IF NOT EXISTS IX_SampleReceptions_Status ON SampleReceptions(Status);";
			using SqliteCommand sqliteCommand = new SqliteCommand(commandText, connection);
			sqliteCommand.ExecuteNonQuery();
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Error creating database indexes", ex);
		}
	}

	private void SetupFTS(SqliteConnection connection)
	{
		try
		{
			string commandText = "\n                    CREATE VIRTUAL TABLE IF NOT EXISTS Certificates_FTS USING fts5(\n                        CertificateNumber,\n                        Sender,\n                        Supplier,\n                        RecipientName,\n                        NotificationNumber,\n                        DeclarationNumber,\n                        content='Certificates',\n                        content_rowid='Id'\n                    );";
			using (SqliteCommand sqliteCommand = new SqliteCommand(commandText, connection))
			{
				sqliteCommand.ExecuteNonQuery();
			}
			string commandText2 = "\n                    CREATE TRIGGER IF NOT EXISTS trg_Certificates_ai AFTER INSERT ON Certificates BEGIN\n                        INSERT INTO Certificates_FTS(rowid, CertificateNumber, Sender, Supplier, RecipientName, NotificationNumber, DeclarationNumber)\n                        VALUES (new.Id, new.CertificateNumber, new.Sender, new.Supplier, new.RecipientName, new.NotificationNumber, new.DeclarationNumber);\n                    END;\n\n                    CREATE TRIGGER IF NOT EXISTS trg_Certificates_ad AFTER DELETE ON Certificates BEGIN\n                        INSERT INTO Certificates_FTS(Certificates_FTS, rowid, CertificateNumber, Sender, Supplier, RecipientName, NotificationNumber, DeclarationNumber)\n                        VALUES('delete', old.Id, old.CertificateNumber, old.Sender, old.Supplier, old.RecipientName, old.NotificationNumber, old.DeclarationNumber);\n                    END;\n\n                    CREATE TRIGGER IF NOT EXISTS trg_Certificates_au AFTER UPDATE ON Certificates BEGIN\n                        INSERT INTO Certificates_FTS(Certificates_FTS, rowid, CertificateNumber, Sender, Supplier, RecipientName, NotificationNumber, DeclarationNumber)\n                        VALUES('delete', old.Id, old.CertificateNumber, old.Sender, old.Supplier, old.RecipientName, old.NotificationNumber, old.DeclarationNumber);\n                        INSERT INTO Certificates_FTS(rowid, CertificateNumber, Sender, Supplier, RecipientName, NotificationNumber, DeclarationNumber)\n                        VALUES (new.Id, new.CertificateNumber, new.Sender, new.Supplier, new.RecipientName, new.NotificationNumber, new.DeclarationNumber);\n                    END;";
			using (SqliteCommand sqliteCommand2 = new SqliteCommand(commandText2, connection))
			{
				sqliteCommand2.ExecuteNonQuery();
			}
			string commandText3 = "SELECT COUNT(*) FROM Certificates_FTS;";
			long num = 0L;
			using (SqliteCommand sqliteCommand3 = new SqliteCommand(commandText3, connection))
			{
				num = (long)(sqliteCommand3.ExecuteScalar() ?? ((object)0));
			}
			if (num == 0)
			{
				string commandText4 = "INSERT INTO Certificates_FTS(Certificates_FTS) VALUES('rebuild');";
				using (SqliteCommand sqliteCommand4 = new SqliteCommand(commandText4, connection))
				{
					sqliteCommand4.ExecuteNonQuery();
				}
				LoggerService.LogInfo("FTS5 index rebuilt (first run).");
			}
			LoggerService.LogInfo("FTS5 Search Engine initialized successfully.");
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Error setting up FTS5 Search Engine", ex);
		}
	}

	private bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
	{
		try
		{
			string commandText = "PRAGMA table_info(" + tableName + ");";
			using SqliteCommand sqliteCommand = new SqliteCommand(commandText, connection);
			using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
			while (sqliteDataReader.Read())
			{
				string text = sqliteDataReader.GetString(1);
				if (text.Equals(columnName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			LoggerService.LogWarning($"Error checking if column {columnName} exists in {tableName}: {ex.Message}");
		}
		return false;
	}

	private void UpgradeDatabase(SqliteConnection connection)
	{
		try
		{
			int num = 0;
			using (SqliteCommand sqliteCommand = new SqliteCommand("PRAGMA user_version;", connection))
			{
				num = Convert.ToInt32(sqliteCommand.ExecuteScalar());
			}
			LoggerService.LogInfo($"Database schema version: {num} (target: {5})");
			if (num >= 5)
			{
				LoggerService.LogInfo("Database schema is up to date. Skipping migrations.");
				return;
			}
			if (!ColumnExists(connection, "Users", "Role"))
			{
				string commandText = "ALTER TABLE Users ADD COLUMN Role INTEGER DEFAULT 1;";
				using SqliteCommand sqliteCommand2 = new SqliteCommand(commandText, connection);
				sqliteCommand2.ExecuteNonQuery();
				if (ColumnExists(connection, "Users", "IsAdmin"))
				{
					string commandText2 = "UPDATE Users SET Role = CASE WHEN IsAdmin = 1 THEN 2 ELSE 1 END;";
					using SqliteCommand sqliteCommand3 = new SqliteCommand(commandText2, connection);
					try
					{
						sqliteCommand3.ExecuteNonQuery();
					}
					catch
					{
					}
				}
			}
			if (!ColumnExists(connection, "Users", "Specialization"))
			{
				string commandText3 = "ALTER TABLE Users ADD COLUMN Specialization TEXT;";
				using SqliteCommand sqliteCommand4 = new SqliteCommand(commandText3, connection);
				sqliteCommand4.ExecuteNonQuery();
			}
			if (!ColumnExists(connection, "Certificates", "CreatedBy"))
			{
				string commandText4 = "ALTER TABLE Certificates ADD COLUMN CreatedBy INTEGER NOT NULL DEFAULT 1;";
				using SqliteCommand sqliteCommand5 = new SqliteCommand(commandText4, connection);
				sqliteCommand5.ExecuteNonQuery();
			}
			if (!ColumnExists(connection, "Certificates", "CreatedByName"))
			{
				string commandText5 = "ALTER TABLE Certificates ADD COLUMN CreatedByName TEXT;";
				using SqliteCommand sqliteCommand6 = new SqliteCommand(commandText5, connection);
				sqliteCommand6.ExecuteNonQuery();
			}
			if (!ColumnExists(connection, "Users", "IsActive"))
			{
				string commandText6 = "ALTER TABLE Users ADD COLUMN IsActive INTEGER DEFAULT 1;";
				using SqliteCommand sqliteCommand7 = new SqliteCommand(commandText6, connection);
				sqliteCommand7.ExecuteNonQuery();
			}
			if (!ColumnExists(connection, "Users", "IsEditor"))
			{
				string commandText7 = "ALTER TABLE Users ADD COLUMN IsEditor INTEGER DEFAULT 1;";
				using SqliteCommand sqliteCommand8 = new SqliteCommand(commandText7, connection);
				sqliteCommand8.ExecuteNonQuery();
			}
			if (!ColumnExists(connection, "Users", "Permissions"))
			{
				string commandText8 = "ALTER TABLE Users ADD COLUMN Permissions TEXT DEFAULT '';";
				using SqliteCommand sqliteCommand9 = new SqliteCommand(commandText8, connection);
				sqliteCommand9.ExecuteNonQuery();
			}
			if (!ColumnExists(connection, "AuditLogs", "ReferenceId"))
			{
				string commandText9 = "ALTER TABLE AuditLogs ADD COLUMN ReferenceId INTEGER;";
				using SqliteCommand sqliteCommand10 = new SqliteCommand(commandText9, connection);
				sqliteCommand10.ExecuteNonQuery();
			}
			string commandText10 = "\n                    CREATE TABLE IF NOT EXISTS Samples (\n                        Id INTEGER PRIMARY KEY AUTOINCREMENT,\n                        CertificateId INTEGER NOT NULL,\n                        Root INTEGER NOT NULL,\n                        SampleNumber TEXT,\n                        Description TEXT,\n                        MeasurementDate TEXT,\n                        Result TEXT,\n                        IsotopeK40 TEXT,\n                        IsotopeRa226 TEXT,\n                        IsotopeTh232 TEXT,\n                        IsotopeRa TEXT,\n                        IsotopeCs137 TEXT,\n                        FOREIGN KEY (CertificateId) REFERENCES Certificates(Id) ON DELETE CASCADE\n                    );";
			using (SqliteCommand sqliteCommand11 = new SqliteCommand(commandText10, connection))
			{
				sqliteCommand11.ExecuteNonQuery();
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>
			{
				{ "AnalysisType", "TEXT" },
				{ "Sender", "TEXT" },
				{ "Supplier", "TEXT" },
				{ "Origin", "TEXT" },
				{ "DeclarationNumber", "TEXT" },
				{ "PolicyNumber", "TEXT" },
				{ "NotificationNumber", "TEXT" },
				{ "FinancialReceiptNumber", "TEXT" },
				{ "SpecialistName", "TEXT" },
				{ "SectionHeadName", "TEXT" },
				{ "ManagerName", "TEXT" },
				{ "Notes", "TEXT" },
				{ "ReceptionId", "INTEGER" },
				{ "UpdatedBy", "INTEGER" },
				{ "UpdatedByName", "TEXT" },
				{ "UpdatedAt", "TEXT" }
			};
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				if (!ColumnExists(connection, "Certificates", item.Key))
				{
					string commandText11 = $"ALTER TABLE Certificates ADD COLUMN {item.Key} {item.Value};";
					using SqliteCommand sqliteCommand12 = new SqliteCommand(commandText11, connection);
					sqliteCommand12.ExecuteNonQuery();
				}
			}
			if (!ColumnExists(connection, "Certificates", "IsDeleted"))
			{
				string commandText12 = "ALTER TABLE Certificates ADD COLUMN IsDeleted INTEGER DEFAULT 0;";
				using SqliteCommand sqliteCommand13 = new SqliteCommand(commandText12, connection);
				sqliteCommand13.ExecuteNonQuery();
			}
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>
			{
				{ "IsotopeK40", "TEXT" },
				{ "IsotopeRa226", "TEXT" },
				{ "IsotopeTh232", "TEXT" },
				{ "IsotopeRa", "TEXT" },
				{ "IsotopeCs137", "TEXT" }
			};
			foreach (KeyValuePair<string, string> item2 in dictionary2)
			{
				if (!ColumnExists(connection, "Samples", item2.Key))
				{
					string commandText13 = $"ALTER TABLE Samples ADD COLUMN {item2.Key} {item2.Value};";
					using SqliteCommand sqliteCommand14 = new SqliteCommand(commandText13, connection);
					sqliteCommand14.ExecuteNonQuery();
				}
			}
			if (!ColumnExists(connection, "SampleReceptions", "FinancialReceiptNumber"))
			{
				string commandText14 = "ALTER TABLE SampleReceptions ADD COLUMN FinancialReceiptNumber TEXT;";
				using SqliteCommand sqliteCommand15 = new SqliteCommand(commandText14, connection);
				sqliteCommand15.ExecuteNonQuery();
			}
			string commandText15 = "\n                    CREATE TABLE IF NOT EXISTS ReferralLetters (\n                        Id INTEGER PRIMARY KEY AUTOINCREMENT,\n                        GeneratedAt TEXT DEFAULT CURRENT_TIMESTAMP,\n                        SenderName TEXT NOT NULL,\n                        CertificateCount INTEGER NOT NULL,\n                        SampleCount INTEGER NOT NULL,\n                        OutputPath TEXT NOT NULL,\n                        StartDate TEXT NOT NULL,\n                        EndDate TEXT NOT NULL,\n                        IncludedColumns TEXT\n                    );";
			using (SqliteCommand sqliteCommand16 = new SqliteCommand(commandText15, connection))
			{
				sqliteCommand16.ExecuteNonQuery();
			}
			using (SqliteCommand sqliteCommand17 = new SqliteCommand($"PRAGMA user_version = {5};", connection))
			{
				sqliteCommand17.ExecuteNonQuery();
			}
			LoggerService.LogInfo($"Database upgraded to schema version {5}.");
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Database Upgrade Critical Error", ex);
		}
	}

	public void AutoBackup()
	{
		try
		{
			string text = Path.Combine(Path.GetDirectoryName(_dbPath) ?? "", "Backups");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string[] files = Directory.GetFiles(text, "backup_*.db");
			if (files.Length >= 5)
			{
				Array.Sort(files);
				File.Delete(files[0]);
			}
			string text2 = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string text3 = Path.Combine(text, "backup_" + text2 + ".db");
			BackupDatabase(text3);
			LoggerService.LogInfo("Automatic backup created: " + text3);
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Auto backup failed", ex);
		}
	}

	public bool BackupDatabase(string destinationPath)
	{
		_maintenanceLock.Wait();
		try
		{
			if (!File.Exists(_dbPath))
			{
				return false;
			}
			using (SqliteConnection sqliteConnection = new SqliteConnection(_connectionString))
			{
				using SqliteConnection sqliteConnection2 = new SqliteConnection("Data Source=" + destinationPath);
				sqliteConnection.Open();
				sqliteConnection2.Open();
				sqliteConnection.BackupDatabase(sqliteConnection2);
			}
			return true;
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Database backup failed", ex);
			return false;
		}
		finally
		{
			_maintenanceLock.Release();
		}
	}

	public async Task<bool> BackupDatabaseAsync(string destinationPath)
	{
		await _maintenanceLock.WaitAsync();
		try
		{
			if (!File.Exists(_dbPath))
			{
				LoggerService.LogError("Database file not found during backup.");
				return false;
			}
			using (SqliteConnection source = new SqliteConnection(_connectionString))
			{
				using SqliteConnection destination = new SqliteConnection("Data Source=" + destinationPath);
				source.Open();
				destination.Open();
				source.BackupDatabase(destination);
			}
			return true;
		}
		finally
		{
			_maintenanceLock.Release();
		}
	}

	public bool RestoreDatabase(string backupPath)
	{
		try
		{
			if (!File.Exists(backupPath))
			{
				return false;
			}
			SqliteConnection.ClearAllPools();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			File.Copy(backupPath, _dbPath, overwrite: true);
			return true;
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Database restore failed", ex);
			return false;
		}
	}

	public Task LogActionAsync(int? userId, string userName, string action, string details, int? referenceId = null)
	{
		return ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_connectionString);
			await connection.OpenAsync();
			string query = "INSERT INTO AuditLogs (UserId, UserName, Action, Details, ReferenceId) VALUES (@UserId, @UserName, @Action, @Details, @ReferenceId)";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@UserId", ((object)userId) ?? DBNull.Value);
			command.Parameters.AddWithValue("@UserName", ((object)userName) ?? ((object)DBNull.Value));
			command.Parameters.AddWithValue("@Action", action);
			command.Parameters.AddWithValue("@Details", details);
			command.Parameters.AddWithValue("@ReferenceId", ((object)referenceId) ?? DBNull.Value);
			await command.ExecuteNonQueryAsync();
		}, "LogActionAsync");
	}

	public Task<List<AuditLog>> GetAuditLogsAsync(int? userId = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 200)
	{
		return ExecuteWithRetryAsync(async delegate
		{
			List<AuditLog> logs = new List<AuditLog>();
			using SqliteConnection connection = new SqliteConnection(_connectionString);
			await connection.OpenAsync();
			StringBuilder queryBuilder = new StringBuilder("SELECT Id, UserId, UserName, Action, Details, datetime(Timestamp, 'localtime'), ReferenceId FROM AuditLogs WHERE 1=1");
			if (userId.HasValue)
			{
				queryBuilder.Append(" AND UserId = @UserId");
			}
			if (startDate.HasValue)
			{
				queryBuilder.Append(" AND Timestamp >= @StartDate");
			}
			if (endDate.HasValue)
			{
				queryBuilder.Append(" AND Timestamp < @EndDate");
			}
			queryBuilder.Append(" ORDER BY Id DESC LIMIT @Limit");
			using SqliteCommand command = new SqliteCommand(queryBuilder.ToString(), connection);
			command.Parameters.AddWithValue("@Limit", limit);
			if (userId.HasValue)
			{
				command.Parameters.AddWithValue("@UserId", userId.Value);
			}
			if (startDate.HasValue)
			{
				command.Parameters.AddWithValue("@StartDate", startDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
			}
			if (endDate.HasValue)
			{
				command.Parameters.AddWithValue("@EndDate", endDate.Value.AddDays(1.0).Date.ToString("yyyy-MM-dd HH:mm:ss"));
			}
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				logs.Add(new AuditLog
				{
					Id = reader.GetInt32(0),
					UserId = (reader.IsDBNull(1) ? ((int?)null) : new int?(reader.GetInt32(1))),
					UserName = (reader.IsDBNull(2) ? "غير معروف" : reader.GetString(2)),
					Action = reader.GetString(3),
					Description = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
					Timestamp = DateTime.Parse(reader.GetString(5)),
					ReferenceId = (reader.IsDBNull(6) ? ((int?)null) : new int?(reader.GetInt32(6)))
				});
			}
			return logs;
		}, "GetAuditLogsAsync");
	}

	public Task<List<AuditLog>> GetLogsByReferenceIdAsync(int referenceId)
	{
		return ExecuteWithRetryAsync(async delegate
		{
			List<AuditLog> logs = new List<AuditLog>();
			using SqliteConnection connection = new SqliteConnection(_connectionString);
			await connection.OpenAsync();
			string query = "SELECT Id, UserId, UserName, Action, Details, datetime(Timestamp, 'localtime'), ReferenceId FROM AuditLogs WHERE ReferenceId = @RefId ORDER BY Timestamp DESC";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@RefId", referenceId);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				logs.Add(new AuditLog
				{
					Id = reader.GetInt32(0),
					UserId = (reader.IsDBNull(1) ? ((int?)null) : new int?(reader.GetInt32(1))),
					UserName = (reader.IsDBNull(2) ? "غير معروف" : reader.GetString(2)),
					Action = reader.GetString(3),
					Description = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
					Timestamp = DateTime.Parse(reader.GetString(5)),
					ReferenceId = (reader.IsDBNull(6) ? ((int?)null) : new int?(reader.GetInt32(6)))
				});
			}
			return logs;
		}, "GetLogsByReferenceIdAsync");
	}
}
