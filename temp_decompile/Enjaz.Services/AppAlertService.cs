using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enjaz.Models;
using Enjaz.Services.Repositories;

namespace Enjaz.Services;

public class AppAlertService : IAppAlertService
{
	private readonly SampleReceptionRepository _receptionRepository;

	public AppAlertService(SampleReceptionRepository receptionRepository)
	{
		_receptionRepository = receptionRepository;
	}

	public async Task<List<AppAlert>> GetCurrentAlertsAsync()
	{
		List<AppAlert> alerts = new List<AppAlert>();
		try
		{
			int delayDays = 7;
			foreach (SampleReception rec in await _receptionRepository.GetDelayedPendingReceptionsAsync(delayDays))
			{
				alerts.Add(new AppAlert
				{
					Type = AlertType.Warning,
					Title = "شحنة معلقة - لم يتم إصدار شهادة",
					Message = $"الاخطار رقم [{rec.NotificationNumber}] الجهة المرسلة لـ [{rec.Sender}] الجهة الموردة لـ [{rec.Supplier}] تم استلامها بتاريخ [{rec.Date:yyyy/MM/dd}] ولم يتم إصدار شهادة لها حتى الآن.",
					ReferenceId = rec.Id,
					Timestamp = DateTime.Now
				});
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			LoggerService.LogError("Error fetching alerts", ex2);
		}
		return alerts;
	}
}
