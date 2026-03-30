using System.Windows;
using MaterialDesignThemes.Wpf;
using Enjaz.Models;
using Enjaz.ViewModels;

namespace Enjaz.Views
{
    public partial class ReceptionSearchWindow : Window
    {
        private readonly ReceptionSearchViewModel _viewModel;

        /// <summary>
        /// النموذج المختار — يُقرأ من الكود الذي فتح النافذة بعد إغلاقها
        /// </summary>
        public SampleReception? SelectedReception { get; private set; }

        public ReceptionSearchWindow(ReceptionSearchViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.ReceptionSelected += OnReceptionSelected;

            // ربط التنبيهات الموحدة
            _viewModel.PropertyChanged += (s, e) => 
            {
                if (e.PropertyName == nameof(_viewModel.IsNotificationDialogOpen) && _viewModel.IsNotificationDialogOpen)
                {
                    DialogHost.Show(new object(), "SearchDialogHost");
                }
            };

            _viewModel.RequestConfirmation += (title, message, type, callback, icon) => 
            {
                // Note: For simplicity in Search Window, we use the standard notification or a simplified confirm
                // But normally we'd set IsConfirmMode and Show.
                _viewModel.IsConfirmMode = true;
                _viewModel.NotificationTitle = title;
                _viewModel.NotificationMessage = message;
                _viewModel.NotificationType = type;
                
                DialogHost.Show(new object(), "SearchDialogHost");
            };

            // Focus on search box when loaded
            Loaded += (s, e) => SearchValueBox.Focus();
        }

        private void OnReceptionSelected(SampleReception reception)
        {
            SelectedReception = reception;
            DialogResult = true;
            Close();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedResult != null)
            {
                _viewModel.ConfirmSelection();
            }
            else
            {
                _viewModel.SetNotification("تنبيه", "يرجى اختيار نموذج استلام من الجدول أولاً", NotificationType.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
