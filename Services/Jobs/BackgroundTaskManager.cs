using System;
using System.Threading;
using System.Threading.Tasks;

namespace Enjaz.Services.Jobs
{
    /// <summary>
    /// مدير المهام في الخلفية لضمان عدم تجميد واجهة المستخدم أثناء إجراء العمليات الطويلة (مثل النسخ الاحتياطي)
    /// </summary>
    public class BackgroundTaskManager
    {
        private readonly BackupService _backupService;
        private Timer? _timer;

        public BackgroundTaskManager(BackupService backupService)
        {
            _backupService = backupService;
        }

        /// <summary>
        /// بدء تشغيل المهام الخلفية
        /// </summary>
        public void Start()
        {
            LoggerService.LogInfo("Background Task Manager Started.");
            // يتم تشغيل المهمة لأول مرة بعد 5 دقائق من فتح البرنامج، ثم تتكرر كل ساعة
            _timer = new Timer(async _ => await ExecuteJobsAsync(), null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));
        }

        private async Task ExecuteJobsAsync()
        {
            try
            {
                // خدمة النسخ الاحتياطي تحتوي بالفعل على شروط التحقق من الإعدادات والوقت
                await _backupService.AutoBackupAsync();
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Background Task Manager: Failed to execute AutoBackup", ex);
            }
        }

        /// <summary>
        /// إيقاف المهام الخلفية عند إغلاق البرنامج
        /// </summary>
        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, 0);
            _timer?.Dispose();
            LoggerService.LogInfo("Background Task Manager Stopped.");
        }
    }
}
