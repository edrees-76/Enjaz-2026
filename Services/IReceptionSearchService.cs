using Enjaz.Models;
using Enjaz.Services.Repositories;
using Enjaz.ViewModels;
using Enjaz.Views;
using System.Linq;
using System.Windows;

namespace Enjaz.Services
{
    /// <summary>
    /// واجهة خدمة البحث عن نماذج الاستلام — تفصل ViewModel عن View
    /// Abstracts the ReceptionSearchWindow dialog from the ViewModel (MVVM compliance)
    /// </summary>
    public interface IReceptionSearchService
    {
        /// <summary>
        /// فتح نافذة البحث واختيار نموذج استلام
        /// </summary>
        /// <returns>النموذج المختار، أو null إذا ألغى المستخدم</returns>
        SampleReception? ShowSearchDialog();
    }

    /// <summary>
    /// تنفيذ WPF لخدمة البحث عن نماذج الاستلام
    /// </summary>
    public class WpfReceptionSearchService : IReceptionSearchService
    {
        private readonly SampleReceptionRepository _sampleReceptionRepository;

        public WpfReceptionSearchService(SampleReceptionRepository sampleReceptionRepository)
        {
            _sampleReceptionRepository = sampleReceptionRepository;
        }

        public SampleReception? ShowSearchDialog()
        {
            var searchVM = new ReceptionSearchViewModel(_sampleReceptionRepository);
            var searchWindow = new ReceptionSearchWindow(searchVM);

            // Safe Owner assignment to prevent "Cannot set Owner property to itself"
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow != null && activeWindow != searchWindow)
            {
                searchWindow.Owner = activeWindow;
            }
            else if (Application.Current.MainWindow != null && Application.Current.MainWindow != searchWindow)
            {
                searchWindow.Owner = Application.Current.MainWindow;
            }

            if (searchWindow.ShowDialog() == true && searchWindow.SelectedReception != null)
            {
                return searchWindow.SelectedReception;
            }

            return null;
        }
    }
}
