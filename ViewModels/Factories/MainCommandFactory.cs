using System;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;

namespace Enjaz.ViewModels.Factories
{
    /// <summary>
    /// مصنع الأوامر الخاص بالواجهة الرئيسية لتقليل حجم الكود وتطبيق مبدأ Single Responsibility.
    /// </summary>
    public class MainCommandFactory
    {
        private readonly MainViewModel _viewModel;
        private readonly INavigationService _navigationService;

        public MainCommandFactory(MainViewModel viewModel, INavigationService navigationService)
        {
            _viewModel = viewModel;
            _navigationService = navigationService;
        }

        public ICommand CreateShowHomeCommand() => 
            new RelayCommand(_ => { if (_viewModel.CheckNavigationSafety()) _navigationService.NavigateTo(NavigationDestination.Home); });

        public ICommand CreateShowSettingsCommand() => 
            new RelayCommand(_ => { if (_viewModel.CheckNavigationSafety()) _navigationService.NavigateTo(NavigationDestination.Settings); });

        public ICommand CreateShowHelpCommand() => 
            new RelayCommand(_ => { if (_viewModel.CheckNavigationSafety()) _navigationService.NavigateTo(NavigationDestination.Help); });

        public ICommand CreateShowAboutCommand() => 
            new RelayCommand(_ => { if (_viewModel.CheckNavigationSafety()) _navigationService.NavigateTo(NavigationDestination.About); });

        public ICommand CreateShowAdminProceduresCommand() => 
            new RelayCommand(_ => 
            {
                if (!_viewModel.CheckNavigationSafety()) return;
                _navigationService.NavigateTo(NavigationDestination.AdminProcedures);
                _viewModel.AdminProceduresVM.CurrentStep = 1;
                _ = _viewModel.AdminProceduresVM.RefreshSendersAsync();
                _ = _viewModel.AdminProceduresVM.LoadReferralHistoryAsync();
            });

        public ICommand CreateShowSampleReceptionsCommand() => 
            new AsyncRelayCommand(async _ => 
            {
                if (!_viewModel.CheckNavigationSafety()) return;
                _navigationService.NavigateTo(NavigationDestination.SampleReceptions);
                await _viewModel.SampleReceptionsVM.LoadReceptionsAsync();
            });

        public ICommand CreateShowCertificatesCommand() => 
            new AsyncRelayCommand(async _ => 
            {
                if (!_viewModel.CheckNavigationSafety()) return;
                _navigationService.NavigateTo(NavigationDestination.Certificates);
                await _viewModel.CertificatesVM.LoadCertificatesAsync();
            });

        public ICommand CreateShowUsersCommand() => 
            new AsyncRelayCommand(async _ => 
            {
                if (!_viewModel.CheckNavigationSafety()) return;
                _navigationService.NavigateTo(NavigationDestination.Users);
                await _viewModel.UsersVM.LoadUsersAsync();
                await _viewModel.UsersVM.LoadActivitiesAsync();
            }, _ => _viewModel.CurrentUser?.CanManageUsers ?? false);

        public ICommand CreateShowReportsCommand() => 
            new RelayCommand(_ => 
            {
                if (!_viewModel.CheckNavigationSafety()) return;
                _navigationService.NavigateTo(NavigationDestination.Reports);
                _viewModel.ReportingVM.CurrentStep = 1;
            });

        public ICommand CreateRefreshCommand() => 
            new AsyncRelayCommand(async _ => 
            {
                await _viewModel.DashboardVM.LoadDashboardDataAsync();
                if (_viewModel.CurrentView == NavigationDestination.SampleReceptions) await _viewModel.SampleReceptionsVM.LoadReceptionsAsync();
                if (_viewModel.CurrentView == NavigationDestination.Certificates) await _viewModel.CertificatesVM.LoadCertificatesAsync();
                if (_viewModel.CurrentView == NavigationDestination.Users)
                {
                    await _viewModel.UsersVM.LoadUsersAsync();
                    await _viewModel.UsersVM.LoadActivitiesAsync();
                }
                if (_viewModel.CurrentView == NavigationDestination.AdminProcedures) await _viewModel.AdminProceduresVM.RefreshSendersAsync();
            });

        public ICommand CreateLogoutCommand(Func<object?, System.Threading.Tasks.Task> logoutAction) => 
            new AsyncRelayCommand(logoutAction);
            
        public ICommand CreateCloseDetailsCommand() => 
            new RelayCommand(_ => _viewModel.IsViewingDetails = false);
            
        public ICommand CreateEscapeCommand(Action escapeAction) => 
            new RelayCommand(_ => escapeAction());
            
        public ICommand CreateSubmitSecurityChallengeCommand(Func<System.Threading.Tasks.Task> submitAction) => 
            new AsyncRelayCommand(async _ => await submitAction());
            
        public ICommand CreateCancelSecurityChallengeCommand(Action cancelAction) => 
            new RelayCommand(_ => cancelAction());

        public ICommand CreateToggleSidebarCommand() => 
            new RelayCommand(_ => _viewModel.IsSidebarVisible = !_viewModel.IsSidebarVisible);

        public ICommand CreateToggleThemeCommand() => 
            new RelayCommand(_ => _viewModel.AppIsDarkMode = !_viewModel.AppIsDarkMode);

        public ICommand CreateShowContextualHelpCommand(Action<string> showHelpAction) => 
            new RelayCommand(obj => showHelpAction(obj?.ToString() ?? string.Empty));
            
        public ICommand CreateConfirmCommand(Action confirmAction) => 
            new RelayCommand(_ => confirmAction());
            
        public ICommand CreateCancelCommand(Action cancelAction) => 
            new RelayCommand(_ => cancelAction());
    }
}
