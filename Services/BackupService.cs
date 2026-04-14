using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Enjaz.Services.Security;

namespace Enjaz.Services
{
    /// <summary>
    /// المنسق الآمن لعمليات النسخ الاحتياطي (Backup Orchestrator Application Service)
    /// </summary>
    public class BackupService
    {
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly CryptoPolicyConfig _cryptoPolicy;
        private readonly EncryptionEngine _encryptionEngine;
        private readonly IStorageProvider _storageProvider;

        public BackupService(DatabaseService dbService, SettingsService settingsService)
        {
            _dbService = dbService;
            _settingsService = settingsService;
            
            // تهيئة معمارية الأمان
            _cryptoPolicy = CryptoPolicyConfig.Default;
            _encryptionEngine = new EncryptionEngine(_cryptoPolicy);
            _storageProvider = new LocalSystemStorageProvider();
        }

        /// <summary>
        /// الحصول على مسار قاعدة البيانات المطلق
        /// </summary>
        private string GetDatabasePath()
        {
            // استخراج المسار من سلسلة الاتصال (Connection String)
            var builder = new SqliteConnectionStringBuilder(_dbService.ConnectionString);
            return builder.DataSource;
        }

        /// <summary>
        /// إجراء عملية النسخ الاحتياطي بأمان (Zero Intermediate Storage) مع مراعاة الإلغاء
        /// </summary>
        public async Task<string?> PerformBackupAsync(string? targetDirectory = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                string sourcePath = GetDatabasePath();
                if (!File.Exists(sourcePath))
                {
                    LoggerService.LogError("فشل التخزين: ملف قاعدة البيانات الأصلي غير موجود.", new FileNotFoundException(sourcePath));
                    return null;
                }

                // 1. مزامنة بيانات قاعدة البيانات المؤقتة (WAL Checkpoint) للقرص قبل النسخ لضمان اكتمال البيانات
                try
                {
                    _dbService.ExecuteWithRetry(() => 
                    {
                        using var connection = new SqliteConnection(_dbService.ConnectionString);
                        connection.Open();
                        using var cmd = new SqliteCommand("PRAGMA wal_checkpoint(FULL);", connection);
                        cmd.ExecuteNonQuery();
                    }, "WAL Checkpoint Prepare Backup");
                }
                catch (Exception ex)
                {
                    LoggerService.LogWarning("أخفق إجراء مزامنة WAL، ولكن ستستمر عملية النسخ: " + ex.Message);
                }

                // 2. إعداد مسارات الحفظ
                string backupDir = targetDirectory ?? _settingsService.Current.BackupPath;
                if (string.IsNullOrWhiteSpace(backupDir))
                {
                    backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Enjaz_Backups");
                }

                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                // استخدام امتداد جديد يعبر عن الملف المشفر (Enjaz Database Backup)
                string finalFileName = $"certificates_backup_{timestamp}.edb";
                string tmpFileName = $"certificates_backup_{timestamp}.tmp";

                string finalDestinationPath = Path.Combine(backupDir, finalFileName);
                string tempDestinationPath = Path.Combine(backupDir, tmpFileName);

                try
                {
                    // 3. فتح دفق القراءة الأصلي بشكل متشارك لمنع قفل النظام
                    using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // 4. فتح دفق الكتابة المشفر المؤقت
                        using (var targetStream = _storageProvider.CreateBackupStream(tempDestinationPath))
                        {
                            // 5. تمرير التدفقات إلى محرك التشفير ليقوم بضخ التشفير القطعي (Streaming Encryption)
                            cancellationToken.ThrowIfCancellationRequested();
                            
                            // ملاحظة: محرك التشفير الان اصبح مصغراً وآمناً (يقوم بـ Flush عبر disposal الخاص بالـ AesGcm)
                            _encryptionEngine.EncryptStream(sourceStream, targetStream);
                        }
                    }

                    // 6. التحقق من الإلغاء قبل الاعتماد
                    cancellationToken.ThrowIfCancellationRequested();

                    // 7. الاعتماد الذري: الملف اكتمل تشفيره بنجاح، أعد التسمية للملف النهائي (Atomic Rename)
                    _storageProvider.CommitBackup(tempDestinationPath, finalDestinationPath);

                    // 8. تحديث الحالة
                    _settingsService.UpdateLastBackupDate();
                    LoggerService.LogInfo($"تم حفظ الدفق المشفر بنجاح بمعمارية AES-GCM (Zero Raw Storage) في: {finalDestinationPath}");
                    
                    // 9. مسار الحفظ السحابي (Cloud Sync)
                    string cloudPath = _settingsService.Current.CloudSyncPath;
                    if (!string.IsNullOrWhiteSpace(cloudPath))
                    {
                        try 
                        {
                            if (!Directory.Exists(cloudPath)) 
                                Directory.CreateDirectory(cloudPath);
                                
                            string cloudDestination = Path.Combine(cloudPath, finalFileName);
                            File.Copy(finalDestinationPath, cloudDestination, true);
                            LoggerService.LogInfo($"تمت المزامنة السحابية للنسخة المشفرة في: {cloudDestination}");
                        }
                        catch (Exception cloudEx)
                        {
                            LoggerService.LogError("فشل المزامنة السحابية للملف المشفر", cloudEx);
                        }
                    }

                    return finalDestinationPath;
                }
                catch (OperationCanceledException)
                {
                    LoggerService.LogInfo("تم إلغاء النسخ الاحتياطي من قبل المستخدم. جاري الإحباط وتنظيف الملف المؤقت.");
                    _storageProvider.AbortBackup(tempDestinationPath);
                    return null;
                }
                catch (Exception ex)
                {
                    LoggerService.LogError("انهيار حرج في منسق التشفير للنسخ الاحتياطي.", ex);
                    _storageProvider.AbortBackup(tempDestinationPath);
                    return null;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// استعادة البيانات من نسخة مشفرة بالكامل.
        /// يتم القراءة مباشرة وفك التشفير إلى ملف قاعدة بيانات بديل.
        /// </summary>
        public async Task<bool> RestoreEncryptedBackupAsync(string encryptedFilePath, string targetDbPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    LoggerService.LogInfo($"بدء استعادة النسخة المشفرة من: {encryptedFilePath}");

                    // مسار مؤقت لفك التشفير لضمان عدم تلف الـ DB الأصلي لو فشل فك التشفير
                    string tempDbPath = targetDbPath + ".restoring.tmp";

                    using (var encryptedStream = _storageProvider.OpenBackupStream(encryptedFilePath))
                    {
                        using (var decryptedDbStream = _storageProvider.CreateBackupStream(tempDbPath))
                        {
                            // مرور البيانات عبر محرك فك التشفير الدقيق
                            _encryptionEngine.DecryptStream(encryptedStream, decryptedDbStream);
                        }
                    }

                    // إغلاق أي اتصال مفتوح لضمان فك القفل التام عن ملف قاعدة البيانات
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    // نقل الملف المؤقت بقوة ليحل محل قاعدة البيانات الحالية
                    _storageProvider.CommitBackup(tempDbPath, targetDbPath);
                    
                    LoggerService.LogInfo("تم استرجاع النظام النقي وفك التشفير بنجاح تام.");
                    return true;
                }
                catch (Exception ex)
                {
                    LoggerService.LogError("فشل في استعادة النسخة المشفرة (اختلال الترويسة أو المفاتيح).", ex);
                    return false;
                }
            });
        }

        public async Task AutoBackupAsync()
        {
            if (!_settingsService.Current.AutoBackupEnabled) return;

            var timeSinceLastBackup = DateTime.Now - _settingsService.Current.LastBackupDate;
            if (timeSinceLastBackup.TotalHours >= _settingsService.Current.AutoBackupIntervalHours)
            {
                LoggerService.LogInfo("بدء النسخ الاحتياطي التلقائي المشفر...");
                await PerformBackupAsync();
            }
        }
    }
}
