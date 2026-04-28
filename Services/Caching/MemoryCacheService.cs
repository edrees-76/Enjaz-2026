using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Enjaz.Services.Caching
{
    /// <summary>
    /// تطبيق لخدمة التخزين المؤقت باستخدام الذاكرة (MemoryCache)
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        
        // للاحتفاظ بقائمة المفاتيح لتمكين مسح الكاش بالكامل إذا لزم الأمر
        private readonly ConcurrentDictionary<string, bool> _cacheKeys = new();

        public MemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async Task<T> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> getItemCallback, TimeSpan? absoluteExpiration = null)
        {
            if (_memoryCache.TryGetValue(cacheKey, out object? cached) && cached is T cachedValue)
            {
                return cachedValue;
            }

            // لم يتم العثور على القيمة في الكاش، جلب البيانات من المصدر الأساسي (كقاعدة البيانات)
            var freshValue = await getItemCallback();

            // إذا لم يتم تمرير مدة محددة، فالمدة الافتراضية هي 10 دقائق
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(10)
            };

            // تخزين القيمة في الكاش
            _memoryCache.Set(cacheKey, freshValue, cacheEntryOptions);
            _cacheKeys.TryAdd(cacheKey, true);

            return freshValue;
        }

        public void Remove(string cacheKey)
        {
            _memoryCache.Remove(cacheKey);
            _cacheKeys.TryRemove(cacheKey, out _);
        }

        public void Clear()
        {
            foreach (var key in _cacheKeys.Keys)
            {
                _memoryCache.Remove(key);
            }
            _cacheKeys.Clear();
        }
    }
}
