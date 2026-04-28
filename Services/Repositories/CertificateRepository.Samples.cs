using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enjaz.Models;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// عمليات العينات — Sample CRUD Operations
    /// [DEPRECATED] هذه الدوال تعمل الآن كوكيل (Adapter) لمستودع العينات المستقل.
    /// سيتم التخلص من هذه الدوال تماماً في المرحلة الرابعة عند تحديث الـ ViewModels.
    /// </summary>
    public partial class CertificateRepository
    {
        #region Sample Operations - عمليات العينات (Adapters)

        [Obsolete("Use ISampleRepository.AddSampleAsync instead.")]
        public Task<bool> AddSampleAsync(Sample sample)
            => _sampleRepository.AddSampleAsync(sample);

        [Obsolete("Use ISampleRepository.GetSamplesByCertificateIdAsync instead.")]
        public Task<List<Sample>> GetSamplesByCertificateIdAsync(int certificateId)
            => _sampleRepository.GetSamplesByCertificateIdAsync(certificateId);

        [Obsolete("Use ISampleRepository.GetSamplesByDateRangeAsync instead.")]
        public Task<List<Sample>> GetSamplesByDateRangeAsync(DateTime startDate, DateTime endDate)
            => _sampleRepository.GetSamplesByDateRangeAsync(startDate, endDate);

        [Obsolete("Use ISampleRepository.DeleteSamplesByCertificateIdAsync instead.")]
        public Task<bool> DeleteSamplesByCertificateIdAsync(int certificateId)
            => _sampleRepository.DeleteSamplesByCertificateIdAsync(certificateId);

        #endregion
    }
}
