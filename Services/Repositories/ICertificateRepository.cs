using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enjaz.Models;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// واجهة مستودع الشهادات
    /// Certificate Repository Interface
    /// </summary>
    public interface ICertificateRepository
    {
        Task<List<int>> GetAvailableYearsAsync();
        Task<List<Certificate>> GetCertificatesPaginatedAsync(int pageNumber, int pageSize, bool showDeleted = false);
        Task<List<Certificate>> GetCertificatesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Certificate?> GetCertificateByReceptionIdAsync(int receptionId);
        Task<Certificate?> GetCertificateByIdAsync(int id);
        
        Task<int> AddCertificateAsync(Certificate certificate);
        Task<bool> UpdateCertificateAsync(Certificate certificate);
        
        bool IsFinancialReceiptNumberUnique(string receiptNumber, int? excludeCertificateId = null);
        Task<List<AuditLog>> GetCertificateHistoryAsync(int certificateId);
        string GenerateCertificateNumber();

        // Sample Adapters (To be removed in Phase 4 when ViewModels are updated)
        [Obsolete("Use ISampleRepository directly instead.")]
        Task<bool> AddSampleAsync(Sample sample);
        [Obsolete("Use ISampleRepository directly instead.")]
        Task<List<Sample>> GetSamplesByCertificateIdAsync(int certificateId);
        [Obsolete("Use ISampleRepository directly instead.")]
        Task<List<Sample>> GetSamplesByDateRangeAsync(DateTime startDate, DateTime endDate);
        [Obsolete("Use ISampleRepository directly instead.")]
        Task<bool> DeleteSamplesByCertificateIdAsync(int certificateId);
    }
}
