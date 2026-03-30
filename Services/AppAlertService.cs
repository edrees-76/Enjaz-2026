using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enjaz.Models;
using Enjaz.Services.Repositories;

namespace Enjaz.Services
{
    public interface IAppAlertService
    {
        Task<List<AppAlert>> GetCurrentAlertsAsync();
    }

    public class AppAlertService : IAppAlertService
    {
        private readonly SampleReceptionRepository _receptionRepository;

        public AppAlertService(SampleReceptionRepository receptionRepository)
        {
            _receptionRepository = receptionRepository;
        }

        public async Task<List<AppAlert>> GetCurrentAlertsAsync()
        {
            var alerts = new List<AppAlert>();

            try
            {
                // Fetch receptions pending for more than 7 days
                int delayDays = 7;
                var delayedReceptions = await _receptionRepository.GetDelayedPendingReceptionsAsync(delayDays);
                
                foreach (var rec in delayedReceptions)
                {
                    alerts.Add(new AppAlert
                    {
                        Type = AlertType.Warning,
                        Title = "شحنة معلقة - لم يتم إصدار شهادة",
                        Message = $"الاخطار رقم [{rec.NotificationNumber}] الجهة المرسلة لـ [{rec.Sender}] الجهة الموردة لـ [{rec.Supplier}] تم استلامها بتاريخ [{rec.Date:yyyy/MM/dd}] ولم يتم إصدار شهادة لها حتى الآن.",
                        ReferenceId = rec.Id, // We use reception ID to potentially navigate to it later if supported
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error fetching alerts", ex);
            }

            return alerts;
        }
    }

    public enum AlertType { Info, Warning, Error }

    public class AppAlert
    {
        public AlertType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int ReferenceId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
