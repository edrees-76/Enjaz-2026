using System;
using Enjaz.Models;

namespace Enjaz.Services;

public interface INavigationService
{
	NavigationDestination CurrentDestination { get; }

	event Action<NavigationDestination>? NavigationChanged;

	void NavigateTo(NavigationDestination destination);
}
