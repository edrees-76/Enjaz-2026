using System.Collections.Generic;
using System.Threading.Tasks;
using Enjaz.Models;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// واجهة مستودع الاستلامات والإحالات
    /// Reception Repository Interface
    /// </summary>
    public interface IReceptionRepository
    {
        Task<List<string>> GetDistinctSendersAsync();
        Task<int> AddSampleReceptionAsync(SampleReception reception);
        Task<bool> UpdateSampleReceptionAsync(SampleReception reception);
        Task<bool> DeleteSampleReceptionAsync(int id);
        Task<List<SampleReception>> GetAllReceptionsAsync();
        Task<List<SampleReception>> GetPendingReceptionsAsync();
        Task<List<SampleReception>> GetDelayedPendingReceptionsAsync(int daysDelayed);
        Task<List<SampleReception>> SearchSampleReceptionsAsync(string searchTerm);
        Task<List<SampleReception>> SearchReceptionsByFieldAsync(string field, string value);
        Task<bool> UpdateReceptionStatusAsync(int receptionId, string newStatus);
        Task<SampleReception?> GetReceptionByIdAsync(int id);
    }
}
