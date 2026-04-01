using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Enjaz.Models;
using Enjaz.Helpers;

namespace Enjaz.Services
{
    /// <summary>
    /// خدمة قاعدة البيانات - إدارة جميع عمليات SQLite
    /// Database Service - Manages all SQLite operations
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;
        private static readonly System.Threading.SemaphoreSlim _maintenanceLock = new System.Threading.SemaphoreSlim(1, 1);

        public string ConnectionString => _connectionString;

        /// <summary>
        /// Constructor for testing (in-memory SQLite)
        /// </summary>
        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
            _dbPath = "memory"; // Dummy value for _dbPath when testing
            InitializeDatabase();
        }

        /// <summary>
        /// إنشاء مثيل جديد من خدمة قاعدة البيانات باستخدام الإعدادات
        /// </summary>
        public DatabaseService(SettingsService settingsService)
        {
            string? customDbPath = settingsService.Current.DatabasePath;

            if (string.IsNullOrEmpty(customDbPath))
            {
                // تحديد مسار قاعدة البيانات في مجلد بيانات التطبيق المحلي للمستخدم (LocalApplicationData)
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appSpecificFolder = Path.Combine(appDataFolder, "Enjaz");
                
                // التأكد من وجود المجلد
                if (!Directory.Exists(appSpecificFolder))
                {
                    Directory.CreateDirectory(appSpecificFolder);
                }
                
                _dbPath = Path.Combine(appSpecificFolder, "certificates.db");
            }
            else
            {
                _dbPath = customDbPath;
                // تأكد من أن المسار ينتهي باسم الملف
                if (!Path.HasExtension(_dbPath))
                {
                    _dbPath = Path.Combine(_dbPath, "certificates.db");
                }

                string? directory = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    try
                    {
                        Directory.CreateDirectory(directory);
                    }
                    catch (Exception ex)
                    {
                        LoggerService.LogError($"Failed to create network directory: {directory}. Falling back to default.", ex);
                        // Fallback logic could be added here if needed
                    }
                }
            }

            _connectionString = $"Data Source={_dbPath}";

            // إنشاء قاعدة البيانات والجداول إذا لم تكن موجودة
            InitializeDatabase();

            // إجراء نسخة احتياطية تلقائية عند التشغيل
            AutoBackup();
        }

        /// <summary>
        /// تصفير بيانات الشهادات والعينات (إجراء لمرة واحدة للعام الجديد)
        /// </summary>
        public void ResetDatabase()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var query = @"
                    DELETE FROM Samples; 
                    DELETE FROM Certificates; 
                    DELETE FROM ReceptionSamples;
                    DELETE FROM SampleReceptions;
                    DELETE FROM ReferralLetters;
                    DELETE FROM AuditLogs;
                    DELETE FROM sqlite_sequence WHERE name IN ('Certificates', 'Samples', 'SampleReceptions', 'ReceptionSamples', 'ReferralLetters', 'AuditLogs');
                    VACUUM;";
                using var command = new SqliteCommand(query, connection);
                command.ExecuteNonQuery();
                LoggerService.LogInfo("Database Reset: All records have been cleared, except users.");
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Database Reset Failed", ex);
            }
        }

        /// <summary>
        /// إعادة ضبط البرنامج: مسح شامل لجميع البيانات مع استثناء حساب المدير الحالي
        /// </summary>
        /// <param name="currentUserId">معرف المستخدم الحالي لعدم مسحه</param>
        /// <returns>true if successful</returns>
        public async System.Threading.Tasks.Task<bool> PerformFactoryResetAsync(int currentUserId)
        {
            await _maintenanceLock.WaitAsync();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. مسح الشهادات والعينات ورسائل الإحالة
                    var clearDataQuery = @"
                        DELETE FROM Samples;
                        DELETE FROM Certificates;
                        DELETE FROM AuditLogs;
                        DELETE FROM ReferralLetters;
                        DELETE FROM SampleReceptions;
                        DELETE FROM ReceptionSamples;";
                    using (var cmd = new SqliteCommand(clearDataQuery, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 2. مسح جميع المستخدمين باستثناء المدير الحالي
                    var clearUsersQuery = "DELETE FROM Users WHERE Id != @AdminId;";
                    using (var cmd = new SqliteCommand(clearUsersQuery, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@AdminId", currentUserId);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. إعادة ضبط تسلسل المعرفات (ID Auto-increment)
                    var resetSeqQuery = "DELETE FROM sqlite_sequence;";
                    using (var cmd = new SqliteCommand(resetSeqQuery, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    
                    // 4. تنظيف قاعدة البيانات فيزيائياً
                    using (var cmd = new SqliteCommand("VACUUM;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    LoggerService.LogInfo($"Factory Reset performed by User ID: {currentUserId}");
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    LoggerService.LogError("Factory Reset transaction failed", ex);
                    return false;
                }
            }
            finally
            {
                _maintenanceLock.Release();
            }
        }

        #region Retry Logic (Error Handling Improvement)
        
        private const int MaxRetries = 3;
        private const int BaseDelayMs = 100;

        /// <summary>
        /// تنفيذ عملية قاعدة البيانات مع إعادة المحاولة عند الفشل
        /// Execute database operation with retry on failure
        /// </summary>
        internal T ExecuteWithRetry<T>(Func<T> operation, string operationName = "Database Operation")
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    var startTime = DateTime.Now;
                    var result = operation();
                    var duration = (DateTime.Now - startTime).TotalMilliseconds;
                    
                    // Performance Monitoring: Log slow operations (> 500ms)
                    if (duration > 500)
                    {
                        LoggerService.LogWarning($"Slow DB Operation: {operationName} took {duration:F0}ms");
                    }
                    
                    return result;
                }
                catch (SqliteException ex) when (attempt < MaxRetries && IsTransientError(ex))
                {
                    attempt++;
                    int delay = BaseDelayMs * (int)Math.Pow(2, attempt); // Exponential backoff
                    LoggerService.LogWarning($"DB Retry {attempt}/{MaxRetries} for {operationName}: {ex.Message}. Waiting {delay}ms...");
                    // Note: This sync overload is unused — all repos use ExecuteWithRetryAsync.
                    // Thread.Sleep is acceptable here since it only runs in a non-UI context.
                    System.Threading.Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"DB Operation Failed: {operationName}", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// تنفيذ عملية قاعدة البيانات بشكل غير متزامن مع إعادة المحاولة عند الفشل
        /// Execute database operation asynchronously with retry on failure
        /// </summary>
        internal async System.Threading.Tasks.Task<T> ExecuteWithRetryAsync<T>(Func<System.Threading.Tasks.Task<T>> operation, string operationName = "Database Operation")
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    var startTime = DateTime.Now;
                    var result = await operation();
                    var duration = (DateTime.Now - startTime).TotalMilliseconds;
                    
                    if (duration > 500)
                    {
                        LoggerService.LogWarning($"Slow Async DB Operation: {operationName} took {duration:F0}ms");
                    }
                    
                    return result;
                }
                catch (SqliteException ex) when (attempt < MaxRetries && IsTransientError(ex))
                {
                    attempt++;
                    int delay = BaseDelayMs * (int)Math.Pow(2, attempt);
                    LoggerService.LogWarning($"Async DB Retry {attempt}/{MaxRetries} for {operationName}: {ex.Message}. Waiting {delay}ms...");
                    await System.Threading.Tasks.Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"Async DB Operation Failed: {operationName}", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// تنفيذ عملية قاعدة البيانات (بدون قيمة إرجاع) بشكل غير متزامن
        /// Execute void database operation asynchronously with retry
        /// </summary>
        internal async System.Threading.Tasks.Task ExecuteWithRetryAsync(Func<System.Threading.Tasks.Task> operation, string operationName = "Database Operation")
        {
            await ExecuteWithRetryAsync<bool>(async () => { await operation(); return true; }, operationName);
        }

        /// <summary>
        /// تنفيذ عملية قاعدة البيانات مع إعادة المحاولة (بدون قيمة إرجاع)
        /// Execute void database operation with retry
        /// </summary>
        internal void ExecuteWithRetry(Action operation, string operationName = "Database Operation")
        {
            ExecuteWithRetry(() => { operation(); return true; }, operationName);
        }

        /// <summary>
        /// التحقق مما إذا كان الخطأ قابلاً للإعادة
        /// Check if the error is transient and retryable
        /// </summary>
        private static bool IsTransientError(SqliteException ex)
        {
            // SQLite error codes that are transient
            return ex.SqliteErrorCode == 5   // SQLITE_BUSY
                || ex.SqliteErrorCode == 6   // SQLITE_LOCKED
                || ex.SqliteErrorCode == 7;  // SQLITE_NOMEM (temporary resource issue)
        }

        #endregion

        /// <summary>
        /// التحقق مما إذا كان المسار يقع على شبكة (UNC أو قرص شبكة)
        /// </summary>
        private bool IsNetworkPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path.StartsWith(@"\\") || path.StartsWith("//")) return true;
            try
            {
                string root = Path.GetPathRoot(path) ?? "";
                if (!string.IsNullOrEmpty(root))
                {
                    var driveInfo = new DriveInfo(root);
                    return driveInfo.DriveType == DriveType.Network;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// تهيئة قاعدة البيانات وإنشاء الجداول
        /// Initialize database and create tables
        /// </summary>
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // تفعيل نظام WAL للمحلي فقط، لتجنب مشاكل قفل الملفات عبر الشبكة نستخدم DELETE
            bool isNetwork = IsNetworkPath(_dbPath);
            string journalMode = isNetwork ? "DELETE" : "WAL";

            using (var walCommand = new SqliteCommand($"PRAGMA journal_mode={journalMode}; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON; PRAGMA encoding='UTF-8';", connection))
            {
                walCommand.ExecuteNonQuery();
            }

            // إنشاء جدول المستخدمين مع حقل Role
            var createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    FullName TEXT NOT NULL,
                    Role INTEGER DEFAULT 1,
                    IsActive INTEGER DEFAULT 1,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );";

            // إنشاء جدول سجل النشاطات (Audit Logs)
            var createAuditLogsTable = @"
                CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER,
                    UserName TEXT,
                    Action TEXT NOT NULL,
                    Details TEXT,
                    Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,
                    ReferenceId INTEGER,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );";

            // إنشاء جدول الشهادات
            var createCertificatesTable = @"
                CREATE TABLE IF NOT EXISTS Certificates (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CertificateNumber TEXT UNIQUE NOT NULL,
                    RecipientName TEXT NOT NULL,
                    CertificateType TEXT NOT NULL,
                    Description TEXT,
                    IssueDate TEXT NOT NULL,
                    ExpiryDate TEXT,
                    IssuingAuthority TEXT,
                    CreatedBy INTEGER NOT NULL,
                    
                    -- New Fields
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
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    UpdatedBy INTEGER,
                    UpdatedByName TEXT,
                    UpdatedAt TEXT,
                    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
                );";

            // إنشاء جدول العينات
            var createSamplesTable = @"
                CREATE TABLE IF NOT EXISTS Samples (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CertificateId INTEGER NOT NULL,
                    Root INTEGER NOT NULL,
                    SampleNumber TEXT,
                    Description TEXT,
                    MeasurementDate TEXT,
                    Result TEXT,
                    
                    -- Environmental Fields
                    IsotopeK40 TEXT,
                    IsotopeRa226 TEXT,
                    IsotopeTh232 TEXT,
                    IsotopeRa TEXT,
                    IsotopeCs137 TEXT,

                    FOREIGN KEY (CertificateId) REFERENCES Certificates(Id) ON DELETE CASCADE
                );";

            // إنشاء جدول رسائل الإحالة (Referral Letters History)
            var createReferralLettersTable = @"
                CREATE TABLE IF NOT EXISTS ReferralLetters (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GeneratedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    SenderName TEXT NOT NULL,
                    CertificateCount INTEGER NOT NULL,
                    SampleCount INTEGER NOT NULL,
                    OutputPath TEXT NOT NULL,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL,
                    IncludedColumns TEXT
                );";

            // إنشاء جدول الاستلامات (SampleReceptions)
            var createSampleReceptionsTable = @"
                CREATE TABLE IF NOT EXISTS SampleReceptions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AnalysisRequestNumber TEXT NOT NULL,
                    NotificationNumber TEXT,
                    DeclarationNumber TEXT,
                    Supplier TEXT,
                    Sender TEXT,
                    Origin TEXT,
                    PolicyNumber TEXT,
                    FinancialReceiptNumber TEXT,
                    CertificateType TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Status TEXT DEFAULT 'لم يتم إصدار شهادة',
                    CreatedBy INTEGER NOT NULL,
                    CreatedByName TEXT,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    UpdatedBy INTEGER,
                    UpdatedByName TEXT,
                    UpdatedAt TEXT,
                    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
                );";

            var createReceptionSamplesTable = @"
                CREATE TABLE IF NOT EXISTS ReceptionSamples (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ReceptionId INTEGER NOT NULL,
                    SampleNumber TEXT,
                    Description TEXT,
                    FOREIGN KEY (ReceptionId) REFERENCES SampleReceptions(Id) ON DELETE CASCADE
                );";

            using (var command = new SqliteCommand(createUsersTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createAuditLogsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createCertificatesTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createSamplesTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createReferralLettersTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createSampleReceptionsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createReceptionSamplesTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // إنشاء جدول محتوى المساعدة الذكي
            var createHelpContentTable = @"
                CREATE TABLE IF NOT EXISTS HelpContent (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Abstract TEXT,
                    ContentSimple TEXT,
                    ContentAdvanced TEXT,
                    Category TEXT,
                    IconKind TEXT,
                    Keywords TEXT,
                    RelatedView TEXT,
                    VideoUrl TEXT,
                    Views INTEGER DEFAULT 0,
                    IsHelpfulCount INTEGER DEFAULT 0
                );";

            using (var command = new SqliteCommand(createHelpContentTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // ترقية قاعدة البيانات (إضافة عمود UserName إذا لم يكن موجوداً)
            UpgradeDatabase(connection);

            // إنشاء الفهارس لتحسين الأداء
            CreateIndexes(connection);

            // إعداد محرك البحث المتقدم (FTS5)
            SetupFTS(connection);

            // تحديث مسميات الحالات القديمة في قاعدة البيانات
            MigrateStatuses(connection);
        }

        /// <summary>
        /// تحديث مسميات الحالات القديمة إلى المسميات الجديدة
        /// </summary>
        private void MigrateStatuses(SqliteConnection connection)
        {
            try
            {
                var migrateQuery = @"
                    UPDATE SampleReceptions SET Status = 'لم يتم إصدار شهادة' WHERE Status IN ('جديد', 'قيد الانتظار', 'في انتظار إصدار شهادة');
                    UPDATE SampleReceptions SET Status = 'تم إصدار شهادة' WHERE Status IN ('مكتملة', 'مكتمل');";
                using var command = new SqliteCommand(migrateQuery, connection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                LoggerService.LogWarning("Migration of statuses failed: " + ex.Message);
            }
        }

        /// <summary>
        /// إنشاء الفهارس لتحسين سرعة البحث والفرز
        /// </summary>
        private void CreateIndexes(SqliteConnection connection)
        {
            try
            {
                var query = @"
                    CREATE INDEX IF NOT EXISTS IX_Certificates_IssueDate ON Certificates(IssueDate);
                    CREATE INDEX IF NOT EXISTS IX_Certificates_CertificateNumber ON Certificates(CertificateNumber);
                    CREATE INDEX IF NOT EXISTS IX_Certificates_Sender ON Certificates(Sender);
                    CREATE INDEX IF NOT EXISTS IX_Certificates_Supplier ON Certificates(Supplier);
                    CREATE INDEX IF NOT EXISTS IX_Certificates_CreatedAt ON Certificates(CreatedAt);
                    CREATE INDEX IF NOT EXISTS IX_ReferralLetters_GeneratedAt ON ReferralLetters(GeneratedAt);
                    CREATE INDEX IF NOT EXISTS IX_ReferralLetters_SenderName ON ReferralLetters(SenderName);
                    CREATE INDEX IF NOT EXISTS IX_Samples_CertificateId ON Samples(CertificateId);
                    CREATE INDEX IF NOT EXISTS IX_SampleReceptions_Status ON SampleReceptions(Status);";
                
                using var command = new SqliteCommand(query, connection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error creating database indexes", ex);
            }
        }

        /// <summary>
        /// إعداد محرك البحث المتقدم (Full-Text Search) لزيادة سرعة البحث في السجلات الضخمة
        /// </summary>
        private void SetupFTS(SqliteConnection connection)
        {
            try
            {
                // 1. إنشاء جدول البحث الافتراضي (FTS5)
                var createFtsTable = @"
                    CREATE VIRTUAL TABLE IF NOT EXISTS Certificates_FTS USING fts5(
                        CertificateNumber,
                        Sender,
                        Supplier,
                        RecipientName,
                        NotificationNumber,
                        DeclarationNumber,
                        content='Certificates',
                        content_rowid='Id'
                    );";
                
                using (var cmd = new SqliteCommand(createFtsTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // 2. إنشاء المحفزات (Triggers) للمزامنة التلقائية
                var triggers = @"
                    CREATE TRIGGER IF NOT EXISTS trg_Certificates_ai AFTER INSERT ON Certificates BEGIN
                        INSERT INTO Certificates_FTS(rowid, CertificateNumber, Sender, Supplier, RecipientName, NotificationNumber, DeclarationNumber)
                        VALUES (new.Id, new.CertificateNumber, new.Sender, new.Supplier, new.RecipientName, new.NotificationNumber, new.DeclarationNumber);
                    END;

                    CREATE TRIGGER IF NOT EXISTS trg_Certificates_ad AFTER DELETE ON Certificates BEGIN
                        INSERT INTO Certificates_FTS(Certificates_FTS, rowid, CertificateNumber, Sender, Supplier, RecipientName, NotificationNumber, DeclarationNumber)
                        VALUES('delete', old.Id, old.CertificateNumber, old.Sender, old.Supplier, old.RecipientName, old.NotificationNumber, old.DeclarationNumber);
                    END;

                    CREATE TRIGGER IF NOT EXISTS trg_Certificates_au AFTER UPDATE ON Certificates BEGIN
                        INSERT INTO Certificates_FTS(Certificates_FTS, rowid, CertificateNumber, Sender, Supplier, RecipientName, NotificationNumber, DeclarationNumber)
                        VALUES('delete', old.Id, old.CertificateNumber, old.Sender, old.Supplier, old.RecipientName, old.NotificationNumber, old.DeclarationNumber);
                        INSERT INTO Certificates_FTS(rowid, CertificateNumber, Sender, Supplier, RecipientName, NotificationNumber, DeclarationNumber)
                        VALUES (new.Id, new.CertificateNumber, new.Sender, new.Supplier, new.RecipientName, new.NotificationNumber, new.DeclarationNumber);
                    END;";

                using (var cmd = new SqliteCommand(triggers, connection))
                {
                    cmd.ExecuteNonQuery();
                }
                
                // 3. إعادة بناء الكشاف فقط عند أول تشغيل (عندما يكون الكشاف فارغاً)
                // FTS5 triggers keep index in sync — rebuild only if empty to avoid startup slowdown
                var checkFtsQuery = "SELECT COUNT(*) FROM Certificates_FTS;";
                long ftsCount = 0;
                using (var checkCmd = new SqliteCommand(checkFtsQuery, connection))
                {
                    ftsCount = (long)(checkCmd.ExecuteScalar() ?? 0);
                }
                if (ftsCount == 0)
                {
                    var populateQuery = "INSERT INTO Certificates_FTS(Certificates_FTS) VALUES('rebuild');";
                    using (var cmd = new SqliteCommand(populateQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
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

        /// <summary>
        /// التحقق من وجود عمود في جدول معين
        /// </summary>
        private bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
        {
            try
            {
                var query = $"PRAGMA table_info({tableName});";
                using var command = new SqliteCommand(query, connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    // الحقل الثاني (الفهرس 1) في نتائج PRAGMA table_info هو اسم العمود
                    var name = reader.GetString(1);
                    if (name.Equals(columnName, StringComparison.OrdinalIgnoreCase))
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

        /// <summary>
        /// ترقية قاعدة البيانات لإضافة أعمدة جديدة
        /// </summary>
        private void UpgradeDatabase(SqliteConnection connection)
        {
            try
            {
                // SF5: DB Version Tracking using PRAGMA user_version
                const int CurrentSchemaVersion = 5;
                int dbVersion = 0;
                using (var vCmd = new SqliteCommand("PRAGMA user_version;", connection))
                {
                    dbVersion = Convert.ToInt32(vCmd.ExecuteScalar());
                }
                LoggerService.LogInfo($"Database schema version: {dbVersion} (target: {CurrentSchemaVersion})");

                if (dbVersion >= CurrentSchemaVersion)
                {
                    LoggerService.LogInfo("Database schema is up to date. Skipping migrations.");
                    return;
                }

                // 1. إضافة عمود Role للمستخدمين
                if (!ColumnExists(connection, "Users", "Role"))
                {
                    var addColumn = "ALTER TABLE Users ADD COLUMN Role INTEGER DEFAULT 1;";
                    using var cmd = new SqliteCommand(addColumn, connection);
                    cmd.ExecuteNonQuery();

                    // محاولة تحديث الأدوار بناءً على IsAdmin القديم إن وجد
                    if (ColumnExists(connection, "Users", "IsAdmin"))
                    {
                        var updateRoles = "UPDATE Users SET Role = CASE WHEN IsAdmin = 1 THEN 2 ELSE 1 END;";
                        using var updateCmd = new SqliteCommand(updateRoles, connection);
                        try { updateCmd.ExecuteNonQuery(); } catch { /* Ignore issues during data migration */ }
                    }
                }

                // 1.1. إضافة عمود Specialization للمستخدمين
                if (!ColumnExists(connection, "Users", "Specialization"))
                {
                    var addColumn = "ALTER TABLE Users ADD COLUMN Specialization TEXT;";
                    using var cmd = new SqliteCommand(addColumn, connection);
                    cmd.ExecuteNonQuery();
                }

                // 1.2. إضافة عمود CreatedBy للشهادات
                if (!ColumnExists(connection, "Certificates", "CreatedBy"))
                {
                    var addColumn = "ALTER TABLE Certificates ADD COLUMN CreatedBy INTEGER NOT NULL DEFAULT 1;";
                    using var cmd = new SqliteCommand(addColumn, connection);
                    cmd.ExecuteNonQuery();
                }

                // 1.3. إضافة عمود CreatedByName للشهادات
                if (!ColumnExists(connection, "Certificates", "CreatedByName"))
                {
                    var addColumn = "ALTER TABLE Certificates ADD COLUMN CreatedByName TEXT;";
                    using var cmd = new SqliteCommand(addColumn, connection);
                    cmd.ExecuteNonQuery();
                }

                // 1.4. إضافة عمود IsActive للمستخدمين
                if (!ColumnExists(connection, "Users", "IsActive"))
                {
                    var addColumn = "ALTER TABLE Users ADD COLUMN IsActive INTEGER DEFAULT 1;";
                    using var cmd = new SqliteCommand(addColumn, connection);
                    cmd.ExecuteNonQuery();
                }

                // 1.5. إضافة عمود IsEditor للمستخدمين
                if (!ColumnExists(connection, "Users", "IsEditor"))
                {
                    var addColumn = "ALTER TABLE Users ADD COLUMN IsEditor INTEGER DEFAULT 1;";
                    using var cmd = new SqliteCommand(addColumn, connection);
                    cmd.ExecuteNonQuery();
                }

                // 1.6. إضافة عمود Permissions للمستخدمين
                if (!ColumnExists(connection, "Users", "Permissions"))
                {
                    var addColumn = "ALTER TABLE Users ADD COLUMN Permissions TEXT DEFAULT '';";
                    using var cmd = new SqliteCommand(addColumn, connection);
                    cmd.ExecuteNonQuery();
                }

                // 1.7. إضافة عمود ReferenceId لجدول AuditLogs
                if (!ColumnExists(connection, "AuditLogs", "ReferenceId"))
                {
                    var addColumn = "ALTER TABLE AuditLogs ADD COLUMN ReferenceId INTEGER;";
                    using var cmd = new SqliteCommand(addColumn, connection);
                    cmd.ExecuteNonQuery();
                }

                // 2. ضمان وجود جدول Samples
                var createSamplesTable = @"
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
                        IsotopeCs137 TEXT,
                        FOREIGN KEY (CertificateId) REFERENCES Certificates(Id) ON DELETE CASCADE
                    );";
                using (var cmd = new SqliteCommand(createSamplesTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // 3. إضافة الأعمدة الجديدة لجدول الشهادات
                var certColumns = new Dictionary<string, string>
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

                foreach (var col in certColumns)
                {
                    if (!ColumnExists(connection, "Certificates", col.Key))
                    {
                        var addCol = $"ALTER TABLE Certificates ADD COLUMN {col.Key} {col.Value};";
                        using var cmd = new SqliteCommand(addCol, connection);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Ensure soft-delete flag exists (required by dashboard/statistics queries)
                if (!ColumnExists(connection, "Certificates", "IsDeleted"))
                {
                    var addCol = "ALTER TABLE Certificates ADD COLUMN IsDeleted INTEGER DEFAULT 0;";
                    using var cmd = new SqliteCommand(addCol, connection);
                    cmd.ExecuteNonQuery();
                }

                // 4. إضافة أعمدة النظائر لجدول العينات
                var sampleColumns = new Dictionary<string, string>
                {
                    { "IsotopeK40", "TEXT" },
                    { "IsotopeRa226", "TEXT" },
                    { "IsotopeTh232", "TEXT" },
                    { "IsotopeRa", "TEXT" },
                    { "IsotopeCs137", "TEXT" }
                };

                foreach (var col in sampleColumns)
                {
                    if (!ColumnExists(connection, "Samples", col.Key))
                    {
                        var addCol = $"ALTER TABLE Samples ADD COLUMN {col.Key} {col.Value};";
                        using var cmd = new SqliteCommand(addCol, connection);
                        cmd.ExecuteNonQuery();
                    }
                }

                // 4.5. إضافة الأعمدة الجديدة لجدول الاستلامات
                if (!ColumnExists(connection, "SampleReceptions", "FinancialReceiptNumber"))
                {
                    var addCol = "ALTER TABLE SampleReceptions ADD COLUMN FinancialReceiptNumber TEXT;";
                    using var cmd = new SqliteCommand(addCol, connection);
                    cmd.ExecuteNonQuery();
                }

                // 5. ضمان وجود جدول ReferralLetters
                var createReferralLettersTable = @"
                    CREATE TABLE IF NOT EXISTS ReferralLetters (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        GeneratedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                        SenderName TEXT NOT NULL,
                        CertificateCount INTEGER NOT NULL,
                        SampleCount INTEGER NOT NULL,
                        OutputPath TEXT NOT NULL,
                        StartDate TEXT NOT NULL,
                        EndDate TEXT NOT NULL,
                        IncludedColumns TEXT
                    );";
                using (var cmd = new SqliteCommand(createReferralLettersTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // SF5: Stamp the current version after successful upgrade
                using (var vCmd = new SqliteCommand($"PRAGMA user_version = {CurrentSchemaVersion};", connection))
                {
                    vCmd.ExecuteNonQuery();
                }
                LoggerService.LogInfo($"Database upgraded to schema version {CurrentSchemaVersion}.");
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Database Upgrade Critical Error", ex);
            }
        }

        // CreateDefaultAdmin moved to UserRepository

        // User methods moved to Services/Repositories/UserRepository.cs


        /// <summary>
        /// إجراء نسخة احتياطية تلقائية في مجلد فرعي
        /// </summary>
        public void AutoBackup()
        {
            try
            {
                string backupFolder = Path.Combine(Path.GetDirectoryName(_dbPath) ?? "", "Backups");
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                // الحفاظ على آخر 5 نسخ فقط
                var files = Directory.GetFiles(backupFolder, "backup_*.db");
                if (files.Length >= 5)
                {
                    Array.Sort(files);
                    File.Delete(files[0]);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string destination = Path.Combine(backupFolder, $"backup_{timestamp}.db");
                
                BackupDatabase(destination);
                LoggerService.LogInfo($"Automatic backup created: {destination}");
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Auto backup failed", ex);
            }
        }

        /// <summary>
        /// إنشاء نسخة احتياطية لقاعدة البيانات
        /// Create database backup
        /// </summary>
        public bool BackupDatabase(string destinationPath)
        {
            _maintenanceLock.Wait();
            try
            {
                if (!File.Exists(_dbPath)) return false;

                using (var source = new SqliteConnection(_connectionString))
                using (var destination = new SqliteConnection($"Data Source={destinationPath}"))
                {
                    source.Open();
                    destination.Open();
                    source.BackupDatabase(destination);
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

        /// <summary>
        /// إنشاء نسخة احتياطية لقاعدة البيانات (غير متزامن)
        /// Create database backup (Async)
        /// </summary>
        public async System.Threading.Tasks.Task<bool> BackupDatabaseAsync(string destinationPath)
        {
            await _maintenanceLock.WaitAsync();
            try
            {
                // Ensure the source file exists
                if (!File.Exists(_dbPath))
                {
                    LoggerService.LogError("Database file not found during backup.");
                    return false;
                }

                // Create a temporary backup first to avoid locking issues if possible, 
                // but for SQLite simple file copy is usually enough if no write transaction is active.
                // Using SqliteConnection.BackupDatabase is safer for active connections.
                
                using (var source = new SqliteConnection(_connectionString))
                using (var destination = new SqliteConnection($"Data Source={destinationPath}"))
                {
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

        /// <summary>
        /// استعادة قاعدة البيانات من نسخة احتياطية
        /// Restore database from backup
        /// </summary>
        public bool RestoreDatabase(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath)) return false;

                // Close any potential connections to release file lock
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Overwrite the current db with the backup
                File.Copy(backupPath, _dbPath, true);
                
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Database restore failed", ex);
                return false;
            }
        }

        /// <summary>
        /// تسجيل نشاط مستخدم في جدول سجل النشاطات
        /// </summary>
        public System.Threading.Tasks.Task LogActionAsync(int? userId, string userName, string action, string details, int? referenceId = null)
        {
            return ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                var query = "INSERT INTO AuditLogs (UserId, UserName, Action, Details, ReferenceId) VALUES (@UserId, @UserName, @Action, @Details, @ReferenceId)";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                command.Parameters.AddWithValue("@UserName", (object?)userName ?? DBNull.Value);
                command.Parameters.AddWithValue("@Action", action);
                command.Parameters.AddWithValue("@Details", details);
                command.Parameters.AddWithValue("@ReferenceId", (object?)referenceId ?? DBNull.Value);
                await command.ExecuteNonQueryAsync();
            }, "LogActionAsync");
        }

        /// <summary>
        /// الحصول على سجل نشاطات المستخدمين مع دعم الفلترة
        /// </summary>
        public System.Threading.Tasks.Task<List<AuditLog>> GetAuditLogsAsync(int? userId = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 200)
        {
            return ExecuteWithRetryAsync(async () =>
            {
                var logs = new List<AuditLog>();
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                
                var queryBuilder = new System.Text.StringBuilder("SELECT Id, UserId, UserName, Action, Details, datetime(Timestamp, 'localtime'), ReferenceId FROM AuditLogs WHERE 1=1");
                
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
                    // Add one day to include the end date fully (until 23:59:59 effectively) if comparing date only, 
                    // but usually endDate from UI might be midnight. Let's assume user picks a day, we want up to the end of that day.
                    queryBuilder.Append(" AND Timestamp < @EndDate");
                }

                queryBuilder.Append(" ORDER BY Id DESC LIMIT @Limit");

                using var command = new SqliteCommand(queryBuilder.ToString(), connection);
                command.Parameters.AddWithValue("@Limit", limit);
                
                if (userId.HasValue)
                    command.Parameters.AddWithValue("@UserId", userId.Value);
                
                if (startDate.HasValue)
                    command.Parameters.AddWithValue("@StartDate", startDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                
                if (endDate.HasValue)
                    command.Parameters.AddWithValue("@EndDate", endDate.Value.AddDays(1).Date.ToString("yyyy-MM-dd HH:mm:ss"));

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    logs.Add(new AuditLog
                    {
                        Id = reader.GetInt32(0),
                        UserId = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1),
                        UserName = reader.IsDBNull(2) ? "غير معروف" : reader.GetString(2),
                        Action = reader.GetString(3),
                        Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Timestamp = DateTime.Parse(reader.GetString(5)),
                        ReferenceId = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6)
                    });
                }
                return logs;
            }, "GetAuditLogsAsync");
        }

        /// <summary>
        /// الحصول على سجل النشاطات لمرجع معين (مثل شهادة)
        /// </summary>
        public System.Threading.Tasks.Task<List<AuditLog>> GetLogsByReferenceIdAsync(int referenceId)
        {
            return ExecuteWithRetryAsync(async () =>
            {
                var logs = new List<AuditLog>();
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                
                var query = "SELECT Id, UserId, UserName, Action, Details, datetime(Timestamp, 'localtime'), ReferenceId FROM AuditLogs WHERE ReferenceId = @RefId ORDER BY Timestamp DESC";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@RefId", referenceId);
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    logs.Add(new AuditLog
                    {
                        Id = reader.GetInt32(0),
                        UserId = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1),
                        UserName = reader.IsDBNull(2) ? "غير معروف" : reader.GetString(2),
                        Action = reader.GetString(3),
                        Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Timestamp = DateTime.Parse(reader.GetString(5)),
                        ReferenceId = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6)
                    });
                }
                return logs;
            }, "GetLogsByReferenceIdAsync");
        }

        /// <summary>
        /// ترقية قاعدة بيانات من منظومة اتقان القديمة لتتوافق مع معايير إنجاز 2026
        /// Upgrades an old Atqaan database to Enjaz 2026 standards
        /// </summary>
        public async System.Threading.Tasks.Task<bool> UpgradeAtqaanDatabaseAsync(string targetDbPath)
        {
            try
            {
                string targetConnString = $"Data Source={targetDbPath}";
                using var connection = new SqliteConnection(targetConnString);
                await connection.OpenAsync();

                // 1. إضافة الجداول الجديدة إذا لم تكن موجودة
                var newTables = new[]
                {
                    @"CREATE TABLE IF NOT EXISTS ReferralLetters (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        GeneratedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                        SenderName TEXT NOT NULL,
                        CertificateCount INTEGER NOT NULL,
                        SampleCount INTEGER NOT NULL,
                        OutputPath TEXT NOT NULL,
                        StartDate TEXT NOT NULL,
                        EndDate TEXT NOT NULL,
                        IncludedColumns TEXT
                    );",
                    @"CREATE TABLE IF NOT EXISTS SampleReceptions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AnalysisRequestNumber TEXT NOT NULL,
                        NotificationNumber TEXT,
                        DeclarationNumber TEXT,
                        Supplier TEXT,
                        Sender TEXT,
                        Origin TEXT,
                        PolicyNumber TEXT,
                        FinancialReceiptNumber TEXT,
                        CertificateType TEXT NOT NULL,
                        Date TEXT NOT NULL,
                        Status TEXT DEFAULT 'لم يتم إصدار شهادة',
                        CreatedBy INTEGER NOT NULL,
                        CreatedByName TEXT,
                        CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                        UpdatedBy INTEGER,
                        UpdatedByName TEXT,
                        UpdatedAt TEXT,
                        FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
                    );",
                    @"CREATE TABLE IF NOT EXISTS ReceptionSamples (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ReceptionId INTEGER NOT NULL,
                        SampleNumber TEXT,
                        Description TEXT,
                        FOREIGN KEY (ReceptionId) REFERENCES SampleReceptions(Id) ON DELETE CASCADE
                    );"
                };

                foreach (var sql in newTables)
                {
                    using var cmd = new SqliteCommand(sql, connection);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. إضافة أعمدة جديدة للجداول الموجودة (Certificates)
                bool hasReceptionId = false;
                using (var checkCmd = new SqliteCommand("PRAGMA table_info(Certificates);", connection))
                {
                    using var reader = await checkCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (reader.GetString(1).Equals("ReceptionId", StringComparison.OrdinalIgnoreCase))
                        {
                            hasReceptionId = true;
                            break;
                        }
                    }
                }

                if (!hasReceptionId)
                {
                    using var alterCmd = new SqliteCommand("ALTER TABLE Certificates ADD COLUMN ReceptionId INTEGER;", connection);
                    await alterCmd.ExecuteNonQueryAsync();
                }

                // 2.5 تحديث مسميات الحالات (Language Migration)
                var updateStatusQuery = @"
                    UPDATE SampleReceptions SET Status = 'لم يتم إصدار شهادة' WHERE Status IN ('في انتظار إصدار شهادة', 'جديد', 'قيد الانتظار');
                    UPDATE SampleReceptions SET Status = 'تم إصدار شهادة' WHERE Status = 'مكتملة';
                ";
                using (var statusCmd = new SqliteCommand(updateStatusQuery, connection))
                {
                    await statusCmd.ExecuteNonQueryAsync();
                }

                // 3. محرك النقل الذكي: إنشاء سجلات استلام للشهادات القديمة التي ليس لها سجل
                var getCertsQuery = "SELECT Id, CertificateNumber, RecipientName, CertificateType, IssueDate, Sender, Supplier, Origin, DeclarationNumber, PolicyNumber, NotificationNumber, FinancialReceiptNumber, CreatedBy, CreatedByName FROM Certificates WHERE ReceptionId IS NULL";
                
                var migratedCerts = new List<dynamic>();
                using (var cmd = new SqliteCommand(getCertsQuery, connection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        migratedCerts.Add(new {
                            Id = reader.GetInt32(0),
                            Num = reader.GetString(1),
                            Recipient = reader.GetString(2),
                            Type = reader.GetString(3),
                            Date = reader.GetString(4),
                            Sender = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Supplier = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            Origin = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            DecNum = reader.IsDBNull(8) ? "" : reader.GetString(8),
                            PolNum = reader.IsDBNull(9) ? "" : reader.GetString(9),
                            NotNum = reader.IsDBNull(10) ? "" : reader.GetString(10),
                            FinNum = reader.IsDBNull(11) ? "" : reader.GetString(11),
                            Uid = reader.GetInt32(12),
                            Uname = reader.IsDBNull(13) ? "نظام الترقية" : reader.GetString(13)
                        });
                    }
                }

                int seq = 1;
                foreach (var cert in migratedCerts)
                {
                    // محاولة توحيد تنسيق التاريخ (Normalization)
                    string formattedDate = cert.Date;
                    if (DateTime.TryParse(cert.Date, out DateTime dt))
                    {
                        formattedDate = dt.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    // توليد رقم طلب تحليل حسب الحل (2): AT-YY-SEQ
                    string year = dt != default ? dt.ToString("yy") : (cert.Date.Length >= 4 ? cert.Date.Substring(2, 2) : "25");
                    string reqNum = $"AT-{year}-{seq:D4}";
                    seq++;

                    // إنشاء سجل الاستلام
                    var insertReqSql = @"INSERT INTO SampleReceptions 
                        (AnalysisRequestNumber, NotificationNumber, DeclarationNumber, Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, CertificateType, Date, Status, CreatedBy, CreatedByName)
                        VALUES (@ReqNum, @NotNum, @DecNum, @Supplier, @Sender, @Origin, @PolNum, @FinNum, @Type, @Date, 'مكتمل (مهاجر)', @Uid, @Uname);
                        SELECT last_insert_rowid();";
                    
                    using var insCmd = new SqliteCommand(insertReqSql, connection);
                    insCmd.Parameters.AddWithValue("@ReqNum", reqNum);
                    insCmd.Parameters.AddWithValue("@NotNum", cert.NotNum);
                    insCmd.Parameters.AddWithValue("@DecNum", cert.DecNum);
                    insCmd.Parameters.AddWithValue("@Supplier", cert.Supplier);
                    insCmd.Parameters.AddWithValue("@Sender", cert.Sender);
                    insCmd.Parameters.AddWithValue("@Origin", cert.Origin);
                    insCmd.Parameters.AddWithValue("@PolNum", cert.PolNum);
                    insCmd.Parameters.AddWithValue("@FinNum", cert.FinNum);
                    insCmd.Parameters.AddWithValue("@Type", cert.Type);
                    insCmd.Parameters.AddWithValue("@Date", formattedDate);
                    insCmd.Parameters.AddWithValue("@Uid", cert.Uid);
                    insCmd.Parameters.AddWithValue("@Uname", cert.Uname);

                    long newReceptionId = (long)(await insCmd.ExecuteScalarAsync() ?? 0);

                    // ربط الشهادة بالسجل الجديد
                    using var updCmd = new SqliteCommand("UPDATE Certificates SET ReceptionId = @Rid WHERE Id = @Id", connection);
                    updCmd.Parameters.AddWithValue("@Rid", newReceptionId);
                    updCmd.Parameters.AddWithValue("@Id", cert.Id);
                    await updCmd.ExecuteNonQueryAsync();
                    
                    // نقل العينات إلى جدول عينات الاستلام أيضاً لرؤيتها في القسم الجديد
                    using var moveSamplesCmd = new SqliteCommand("INSERT INTO ReceptionSamples (ReceptionId, SampleNumber, Description) SELECT @Rid, SampleNumber, Description FROM Samples WHERE CertificateId = @Id", connection);
                    moveSamplesCmd.Parameters.AddWithValue("@Rid", newReceptionId);
                    moveSamplesCmd.Parameters.AddWithValue("@Id", cert.Id);
                    await moveSamplesCmd.ExecuteNonQueryAsync();
                }

                // 4. ترميم التواريخ (Date Repair) لضمان توافق فلاتر البحث في التقارير ورسائل الإحالة
                var certRecords = new List<dynamic>();
                using (var getDatesCmd = new SqliteCommand("SELECT Id, IssueDate, ExpiryDate FROM Certificates", connection))
                using (var reader = await getDatesCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        certRecords.Add(new { 
                            Id = reader.GetInt32(0), 
                            IssueDate = reader.IsDBNull(1) ? "" : reader.GetString(1), 
                            ExpiryDate = reader.IsDBNull(2) ? "" : reader.GetString(2) 
                        });
                    }
                }

                foreach (var cert in certRecords)
                {
                    string? normIssue = NormalizeDateString(cert.IssueDate);
                    string? normExpiry = string.IsNullOrEmpty(cert.ExpiryDate) ? null : NormalizeDateString(cert.ExpiryDate);
                    
                    if (normIssue != cert.IssueDate || normExpiry != cert.ExpiryDate)
                    {
                        using var updDatesCmd = new SqliteCommand("UPDATE Certificates SET IssueDate = @Issue, ExpiryDate = @Expiry WHERE Id = @Id", connection);
                        updDatesCmd.Parameters.AddWithValue("@Issue", normIssue ?? cert.IssueDate);
                        updDatesCmd.Parameters.AddWithValue("@Expiry", (object?)normExpiry ?? DBNull.Value);
                        updDatesCmd.Parameters.AddWithValue("@Id", cert.Id);
                        await updDatesCmd.ExecuteNonQueryAsync();
                    }
                }

                // 5. توحيد أنواع العينات والحالات (Data Normalization) لتبديل مسميات اتقان الطويلة
                string[] normalizeTypeQueries = {
                    "UPDATE SampleReceptions SET Status = 'مكتمل' WHERE Status LIKE '%مكتمل%'",
                    "UPDATE Certificates SET CertificateType = 'عينات استهلاكية' WHERE CertificateType LIKE '%استهلاكية%'",
                    "UPDATE Certificates SET CertificateType = 'عينات بيئية' WHERE CertificateType LIKE '%بيئية%' OR CertificateType LIKE '%بينية%'",
                    "UPDATE SampleReceptions SET CertificateType = 'عينات استهلاكية' WHERE CertificateType LIKE '%استهلاكية%'",
                    "UPDATE SampleReceptions SET CertificateType = 'عينات بيئية' WHERE CertificateType LIKE '%بيئية%' OR CertificateType LIKE '%بينية%'"
                };

                foreach (var q in normalizeTypeQueries)
                {
                    using var normCmd = new SqliteCommand(q, connection);
                    await normCmd.ExecuteNonQueryAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Atqaan Migration Failed", ex);
                return false;
            }
        }

        private string? NormalizeDateString(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return null;
            
            string[] formats = { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss", "MM/dd/yyyy", "d/M/yyyy" };
            if (DateTime.TryParseExact(dateStr, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result.ToString("yyyy-MM-dd");
            }
            
            if (DateTime.TryParse(dateStr, out result))
            {
                return result.ToString("yyyy-MM-dd");
            }
            
            return dateStr;
        }
    }
}
