using System.Windows;
using System.Windows.Input;

namespace Enjaz.Views
{
    /// <summary>
    /// واجهة تسجيل الدخول - Code Behind
    /// Login Window - Code Behind
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow(ViewModels.LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // Constructor for compatibility/fallback (though DI uses the one with most parameters)
        public LoginWindow() 
        {
             InitializeComponent();
        }

        /// <summary>
        /// سحب النافذة عند الضغط على الخلفية
        /// Drag window when clicking on background
        /// </summary>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// زر الإغلاق
        /// Close button handler
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// زر التصغير
        /// Minimize button handler
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// زر التكبير
        /// Maximize button handler
        /// </summary>
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }
    }
}
