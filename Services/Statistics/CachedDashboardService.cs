using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enjaz.Services.Caching;

namespace Enjaz.Services.Statistics
{
    /// <summary>
    /// مزخرف التخزين المؤقت (Cache Decorator) لخدمة إحصائيات لوحة التحكم.
    /// يقلل الضغط عن قاعدة البيانات بتخزين النتيجة في الذاكرة لفترة محددة.
    /// </summary>
    public class CachedDashboardService : IDashboardService
    {
        private readonly IDashboardService _inner;
        private readonly ICacheService _cache;

        public CachedDashboardService(IDashboardService inner, ICacheService cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public Task<List<int>> GetAvailableYearsAsync()
        {
            // تخزين السنوات المتاحة لمدة 30 دقيقة (تتغير ببطء شديد)
            return _cache.GetOrSetAsync(
                "Dashboard_AvailableYears",
                () => _inner.GetAvailableYearsAsync(),
                TimeSpan.FromMinutes(30));
        }

        public Task<DashboardData> GetDashboardDataAsync(int year, DateTime today)
        {
            // تخزين إحصائيات السنة الحالية في الذاكرة لمدة 5 دقائق
            // يتم استخدام التاريخ (اليوم) والسنة في المفتاح لضمان دقة البيانات المعروضة
            string cacheKey = $"Dashboard_Data_{year}_{today:yyyyMMdd}";
            
            return _cache.GetOrSetAsync(
                cacheKey,
                () => _inner.GetDashboardDataAsync(year, today),
                TimeSpan.FromMinutes(5));
        }
    }
}
