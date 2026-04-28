using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enjaz.Services.Repositories;

namespace Enjaz.Services.Statistics
{
    /// <summary>
    /// خدمة إحصائيات لوحة التحكم الأساسية (تجلب البيانات من المستودعات)
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly CertificateRepository _certificateRepository;
        private readonly DatabaseService _databaseService;

        public DashboardService(CertificateRepository certificateRepository, DatabaseService databaseService)
        {
            _certificateRepository = certificateRepository;
            _databaseService = databaseService;
        }

        public Task<List<int>> GetAvailableYearsAsync()
        {
            return _certificateRepository.GetAvailableYearsAsync();
        }

        public async Task<DashboardData> GetDashboardDataAsync(int year, DateTime today)
        {
            // إطلاق جميع الاستعلامات بشكل متوازي (Parallel Execution) للحصول على أفضل أداء
            var totalCertTask = _certificateRepository.GetTotalCertificatesCountAsync(false, year);
            var todayCertTask = _certificateRepository.GetCertificatesCountByDateAsync(today);
            var envCertTask = _certificateRepository.GetCertificatesCountByTypeAsync("بيئية", year);
            var conCertTask = _certificateRepository.GetCertificatesCountByTypeAsync("استهلاكية", year);

            var totalSampleTask = _certificateRepository.GetTotalSamplesCountAsync(year);
            var todaySampleTask = _certificateRepository.GetSamplesCountByDateAsync(today);
            var envSampleTask = _certificateRepository.GetSamplesCountByTypeAsync("بيئية", year);
            var conSampleTask = _certificateRepository.GetSamplesCountByTypeAsync("استهلاكية", year);
            
            var auditLogsTask = _databaseService.GetAuditLogsAsync(limit: 5);

            var envStatsTask = _certificateRepository.GetMonthlyStatisticsByTypeAsync(year, "بيئية");
            var conStatsTask = _certificateRepository.GetMonthlyStatisticsByTypeAsync(year, "استهلاكية");

            var envSampleStatsTask = _certificateRepository.GetMonthlySamplesStatisticsByTypeAsync(year, "بيئية");
            var conSampleStatsTask = _certificateRepository.GetMonthlySamplesStatisticsByTypeAsync(year, "استهلاكية");

            await Task.WhenAll(
                totalCertTask, todayCertTask, envCertTask, conCertTask,
                totalSampleTask, todaySampleTask, envSampleTask, conSampleTask,
                auditLogsTask,
                envStatsTask, conStatsTask,
                envSampleStatsTask, conSampleStatsTask
            );

            return new DashboardData
            {
                TotalCertificates = await totalCertTask,
                TodayCertificates = await todayCertTask,
                EnvCertificates = await envCertTask,
                ConCertificates = await conCertTask,
                
                TotalSamples = await totalSampleTask,
                TodaySamples = await todaySampleTask,
                EnvSamples = await envSampleTask,
                ConSamples = await conSampleTask,
                
                RecentActivities = await auditLogsTask,
                
                MonthlyEnvCertificates = await envStatsTask,
                MonthlyConCertificates = await conStatsTask,
                
                MonthlyEnvSamples = await envSampleStatsTask,
                MonthlyConSamples = await conSampleStatsTask
            };
        }
    }
}
