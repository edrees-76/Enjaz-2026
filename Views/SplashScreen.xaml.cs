using System;
using System.Windows;

namespace Enjaz.Views
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
        }

        public void UpdateMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusMessage.Text = message;
            });
        }
        
        public void UpdateProgress(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                LoadProgress.IsIndeterminate = false;
                LoadProgress.Value = progress;
            });
        }
    }
}
