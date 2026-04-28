using System;
using System.Threading.Tasks;

namespace Enjaz.Services.Caching
{
    /// <summary>
    /// واجهة خدمة التخزين المؤقت
    /// Cache Service Interface
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// الحصول على القيمة من الكاش، وإن لم تكن موجودة يقوم بجلبها وتخزينها.
        /// </summary>
        Task<T> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> getItemCallback, TimeSpan? absoluteExpiration = null);
        
        /// <summary>
        /// إزالة مفتاح معين من الكاش.
        /// </summary>
        void Remove(string cacheKey);
        
        /// <summary>
        /// مسح الكاش بالكامل.
        /// </summary>
        void Clear();
    }
}
