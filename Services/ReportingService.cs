using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enjaz.Models;
using Enjaz.Services.Repositories;

namespace Enjaz.Services
{
    /// <summary>
    /// خدمة التقارير - مسؤولة عن جلب وتجميع البيانات للتقارير
    /// Reporting Service - Responsible for fetching and aggregating data for reports
    /// </summary>
    public class ReportingService
    {
        private readonly CertificateRepository _certificateRepository;
        private readonly ISampleRepository _sampleRepository;

        public ReportingService(CertificateRepository certificateRepository, ISampleRepository sampleRepository)
        {
            _certificateRepository = certificateRepository;
            _sampleRepository = sampleRepository;
        }

        /// <summary>
        /// جلب الشهادات ضمن نطاق زمني محدد
        /// </summary>
        public async Task<List<Certificate>> GetCertificatesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _certificateRepository.GetCertificatesByDateRangeAsync(startDate, endDate);
        }

        /// <summary>
        /// جلب العينات للشهادات ضمن نطاق زمني
        /// </summary>
        public async Task<List<Sample>> GetSamplesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _sampleRepository.GetSamplesByDateRangeAsync(startDate, endDate);
        }

        /// <summary>
        /// إحصائيات ملخصة للفترة الزمنية
        /// </summary>
        public async Task<ReportSummary> GetReportSummaryAsync(DateTime startDate, DateTime endDate)
        {
            var certificates = await GetCertificatesByDateRangeAsync(startDate, endDate);
            var samples = await GetSamplesByDateRangeAsync(startDate, endDate);

            return new ReportSummary
            {
                TotalCertificates = certificates.Count,
                TotalSamples = samples.Count,
                EnvironmentalCertificates = certificates.Count(c => c.CertificateType?.Contains("بيئية") == true),
                ConsumableCertificates = certificates.Count(c => c.CertificateType?.Contains("استهلاكية") == true),
                EnvironmentalSamples = certificates.Sum(c => c.EnvironmentalSampleCount),
                ConsumableSamples = certificates.Sum(c => c.ConsumableSampleCount),
                StartDate = startDate,
                EndDate = endDate
            };
        }

        /// <summary>
        /// توزيع الموردين (أكثر 10 موردين)
        /// </summary>
        public async Task<List<DistributionItem>> GetTopSuppliersAsync(DateTime startDate, DateTime endDate, int top = 10)
        {
            var certificates = await GetCertificatesByDateRangeAsync(startDate, endDate);
            
            return certificates
                .Where(c => !string.IsNullOrEmpty(c.Supplier))
                .GroupBy(c => c.Supplier)
                .Select(g => new DistributionItem { Name = g.Key!, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .ToList();
        }

        /// <summary>
        /// توزيع الجهات المرسلة (أكثر 10 جهات)
        /// </summary>
        public async Task<List<DistributionItem>> GetTopSendersAsync(DateTime startDate, DateTime endDate, int top = 10)
        {
            var certificates = await GetCertificatesByDateRangeAsync(startDate, endDate);
            
            return certificates
                .Where(c => !string.IsNullOrEmpty(c.Sender))
                .GroupBy(c => c.Sender)
                .Select(g => new DistributionItem { Name = g.Key!, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .ToList();
        }

        /// <summary>
        /// توزيع بلدان المنشأ
        /// </summary>
        public async Task<List<DistributionItem>> GetTopOriginsAsync(DateTime startDate, DateTime endDate, int top = 10)
        {
            var certificates = await GetCertificatesByDateRangeAsync(startDate, endDate);
            
            return certificates
                .Where(c => !string.IsNullOrEmpty(c.Origin))
                .GroupBy(c => c.Origin)
                .Select(g => new DistributionItem { Name = g.Key!, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .ToList();
        }

        public async Task<List<string>> GetUniqueSendersAsync()
        {
            return await _certificateRepository.GetDistinctFieldValuesAsync("Sender");
        }

        public async Task<(ReportSummary Summary, List<Certificate> Certificates)> GetSenderReportDataAsync(DateTime startDate, DateTime endDate, string sender)
        {
            var allCerts = await GetCertificatesByDateRangeAsync(startDate, endDate);
            var senderCerts = allCerts.Where(c => c.Sender == sender).ToList();

            var summary = new ReportSummary
            {
                TotalCertificates = senderCerts.Count,
                TotalSamples = senderCerts.Sum(c => c.SampleCount),
                EnvironmentalCertificates = senderCerts.Count(c => c.CertificateType?.Contains("بيئية") == true || c.CertificateType?.Contains("بيييه") == true || c.CertificateType?.Contains("بيئيه") == true),
                ConsumableCertificates = senderCerts.Count(c => c.CertificateType?.Contains("استهلاكية") == true),
                EnvironmentalSamples = senderCerts.Sum(c => c.EnvironmentalSampleCount),
                ConsumableSamples = senderCerts.Sum(c => c.ConsumableSampleCount),
                StartDate = startDate,
                EndDate = endDate
            };

            return (summary, senderCerts);
        }

        /// <summary>
        /// توزيع الشهادات حسب الشهور للمقارنة
        /// </summary>
        public async Task<List<DistributionItem>> GetMonthlyDistributionAsync(DateTime startDate, DateTime endDate)
        {
            var certificates = await GetCertificatesByDateRangeAsync(startDate, endDate);
            
            return certificates
                .GroupBy(c => new { c.IssueDate.Year, c.IssueDate.Month })
                .Select(g => new DistributionItem 
                { 
                    Name = $"{g.Key.Year}/{g.Key.Month:D2}", 
                    Count = g.Count() 
                })
                .OrderBy(x => x.Name)
                .ToList();
        }

        /// <summary>
        /// تحليل أكثر أنواع التحاليل (السلع) طلبًا
        /// </summary>
        public async Task<List<DistributionItem>> GetTopAnalysisTypesAsync(DateTime startDate, DateTime endDate, int top = 10)
        {
            var certificates = await GetCertificatesByDateRangeAsync(startDate, endDate);
            
            return certificates
                .Where(c => !string.IsNullOrEmpty(c.AnalysisType))
                .GroupBy(c => c.AnalysisType)
                .Select(g => new DistributionItem { Name = g.Key!, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .ToList();
        }
    }

    /// <summary>
    /// ملخص التقرير
    /// </summary>
    public class ReportSummary
    {
        public int TotalCertificates { get; set; }
        public int TotalSamples { get; set; }
        public int EnvironmentalCertificates { get; set; }
        public int ConsumableCertificates { get; set; }
        public int EnvironmentalSamples { get; set; }
        public int ConsumableSamples { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// عنصر توزيع (للرسوم البيانية)
    /// </summary>
    public class DistributionItem
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
