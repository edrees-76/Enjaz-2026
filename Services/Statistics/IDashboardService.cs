using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enjaz.Models;

namespace Enjaz.Services.Statistics
{
    /// <summary>
    /// كائن نقل البيانات لجمع كافة إحصائيات لوحة التحكم في طلب واحد
    /// </summary>
    public class DashboardData
    {
        public List<int> AvailableYears { get; set; } = new();
        public int TotalCertificates { get; set; }
        public int TodayCertificates { get; set; }
        public int EnvCertificates { get; set; }
        public int ConCertificates { get; set; }
        
        public int TotalSamples { get; set; }
        public int TodaySamples { get; set; }
        public int EnvSamples { get; set; }
        public int ConSamples { get; set; }

        public List<AuditLog> RecentActivities { get; set; } = new();

        public Dictionary<int, int> MonthlyEnvCertificates { get; set; } = new();
        public Dictionary<int, int> MonthlyConCertificates { get; set; } = new();
        
        public Dictionary<int, int> MonthlyEnvSamples { get; set; } = new();
        public Dictionary<int, int> MonthlyConSamples { get; set; } = new();
    }

    /// <summary>
    /// واجهة خدمة إحصائيات لوحة التحكم
    /// </summary>
    public interface IDashboardService
    {
        Task<DashboardData> GetDashboardDataAsync(int year, DateTime today);
        Task<List<int>> GetAvailableYearsAsync();
    }
}
