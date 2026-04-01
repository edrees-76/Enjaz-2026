using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Enjaz.Models;
using Enjaz.ViewModels;

namespace Enjaz.Views
{
    /// <summary>
    /// الواجهة الرئيسية - Code Behind
    /// Main Window - Code Behind
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isLoggingOut = false;
        private DashboardWindow? _dashboardWindow;

        /// <summary>
        /// إنشاء النافذة الرئيسية مع بيانات المستخدم
        /// Create main window with user data
        /// </summary>
        /// <param name="viewModel">ViewModel المحقون</param>
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // إضافة معالج إغلاق النافذة
            this.Closing += MainWindow_Closing;
            
            // Subscribe to open dashboard event
            viewModel.ReportingVM.RequestOpenDashboard += () =>
            {
                // Singleton pattern for Dashboard window
                if (_dashboardWindow != null)
                {
                    _dashboardWindow.Activate();
                    if (_dashboardWindow.WindowState == WindowState.Minimized)
                        _dashboardWindow.WindowState = WindowState.Normal;
                    return;
                }

                _dashboardWindow = new DashboardWindow
                {
                    DataContext = viewModel.ReportingVM,
                    Owner = this
                };

                // Clear reference when closed
                _dashboardWindow.Closed += (s, e) => 
                {
                    _dashboardWindow = null;
                    viewModel.IsDashboardOpen = false; // Update VM state
                };
                
                // Show non-modal
                _dashboardWindow.Show();
                viewModel.IsDashboardOpen = true; // Update VM state
            };
        }

        /// <summary>
        /// عند إغلاق النافذة الرئيسية، يتم العودة لواجهة تسجيل الدخول
        /// When main window closes, return to login screen
        /// </summary>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // إذا كان هناك تسجيل خروج قيد التنفيذ بالفعل، لا تفعل شيئاً واترك النافذة تغلق بصورة طبيعية بمجرد استدعاء Close() من ViewModel
            if (_isLoggingOut) return;

            if (DataContext is MainViewModel currentVm && currentVm.IsAnyModalOpen)
            {
                e.Cancel = true;
                currentVm.SetNotification("نافذة مفتوحة", "يرجى إغلاق النافذة الحالية أو إنهاء العملية القائمة قبل إغلاق المنظومة.", NotificationType.Warning);
                return;
            }

            // إلغاء الإغلاق الفوري والبدء في إجراءات تسجيل الخروج مع التأكيد
            e.Cancel = true;
            
            if (DataContext is MainViewModel vm)
            {
                // استدعاء PerformLogoutAsync مع true لطلب التأكيد عبر DialogHost
                _ = vm.PerformLogoutAsync(true);
            }
        }

        /// <summary>
        /// تعيين حالة تسجيل الخروج (يُستخدم من ViewModel)
        /// Set logout state (used from ViewModel)
        /// </summary>
        public void SetLoggingOut()
        {
            _isLoggingOut = true;
        }
    }
}
