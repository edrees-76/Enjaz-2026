using System.Collections.Generic;
using System.Threading.Tasks;

namespace Enjaz.Services;

public interface IAppAlertService
{
	Task<List<AppAlert>> GetCurrentAlertsAsync();
}
