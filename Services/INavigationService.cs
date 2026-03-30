using System;
using Enjaz.Models;

namespace Enjaz.Services
{
    /// <summary>
    /// واجهة خدمة التنقل
    /// Navigation Service Interface
    /// </summary>
    public interface INavigationService
    {
        NavigationDestination CurrentDestination { get; }
        event Action<NavigationDestination>? NavigationChanged;
        void NavigateTo(NavigationDestination destination);
    }

    /// <summary>
    /// تنفيذ خدمة التنقل
    /// Navigation Service Implementation
    /// </summary>
    public class NavigationService : INavigationService
    {
        private NavigationDestination _currentDestination = NavigationDestination.Home;

        public NavigationDestination CurrentDestination => _currentDestination;

        public event Action<NavigationDestination>? NavigationChanged;

        public void NavigateTo(NavigationDestination destination)
        {
            if (_currentDestination == destination) return;

            _currentDestination = destination;
            NavigationChanged?.Invoke(destination);
            LoggerService.LogInfo($"[NavigationService] Navigated to: {destination}");
        }
    }
}
