using System;
using Enjaz.Models;

namespace Enjaz.Services;

public class NavigationService : INavigationService
{
	private NavigationDestination _currentDestination = NavigationDestination.Home;

	public NavigationDestination CurrentDestination => _currentDestination;

	public event Action<NavigationDestination>? NavigationChanged;

	public void NavigateTo(NavigationDestination destination)
	{
		if (_currentDestination != destination)
		{
			_currentDestination = destination;
			this.NavigationChanged?.Invoke(destination);
			LoggerService.LogInfo($"[NavigationService] Navigated to: {destination}");
		}
	}
}
