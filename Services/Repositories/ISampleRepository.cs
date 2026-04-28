using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enjaz.Models;

namespace Enjaz.Services.Repositories
{
    /// <summary>
    /// واجهة إدارة العينات (Domain Repository Interface)
    /// </summary>
    public interface ISampleRepository
    {
        Task<bool> AddSampleAsync(Sample sample);
        Task<List<Sample>> GetSamplesByCertificateIdAsync(int certificateId);
        Task<List<Sample>> GetSamplesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> DeleteSamplesByCertificateIdAsync(int certificateId);
    }
}
