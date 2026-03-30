using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Enjaz.Views
{
    public partial class LockScreenWindow : Window
    {
        public LockScreenWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TransitionToLogin();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            TransitionToLogin();
        }

        private void TransitionToLogin()
        {
            if (Application.Current is App app)
            {
                var loginWindow = app.ServiceProvider.GetRequiredService<LoginWindow>();
                
                // Trigger the session expired message context manually if needed
                if (loginWindow.DataContext is ViewModels.LoginViewModel lvm)
                {
                    lvm.ShowSessionExpiredMessage();
                }

                loginWindow.Show();
                this.Close();
            }
            else
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}
