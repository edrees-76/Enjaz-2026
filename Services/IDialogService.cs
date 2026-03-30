namespace Enjaz.Services
{
    /// <summary>
    /// واجهة خدمة الحوارات — لفصل ViewModels عن واجهات WPF (MVVM compliance)
    /// Dialog Service interface — decouples ViewModels from WPF Views
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// عرض نافذة حوار اختيار ملف للحفظ
        /// Show a Save File dialog
        /// </summary>
        /// <param name="filter">File filter (e.g. "CSV Files (*.csv)|*.csv")</param>
        /// <param name="defaultExt">Default extension</param>
        /// <param name="defaultFileName">Default file name</param>
        /// <returns>Selected file path, or null if cancelled</returns>
        string? ShowSaveFileDialog(string filter, string defaultExt, string defaultFileName);

        /// <summary>
        /// عرض نافذة حوار اختيار ملف للفتح
        /// Show an Open File dialog
        /// </summary>
        /// <param name="filter">File filter</param>
        /// <returns>Selected file path, or null if cancelled</returns>
        string? ShowOpenFileDialog(string filter);

        /// <summary>
        /// عرض نافذة حوار اختيار مجلد
        /// Show a Folder Browser dialog
        /// </summary>
        /// <returns>Selected folder path, or null if cancelled</returns>
        string? ShowFolderBrowserDialog();
    }

    /// <summary>
    /// تنفيذ خدمة الحوارات باستخدام WPF
    /// WPF implementation of Dialog Service
    /// </summary>
    public class WpfDialogService : IDialogService
    {
        public string? ShowSaveFileDialog(string filter, string defaultExt, string defaultFileName)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                DefaultExt = defaultExt,
                FileName = defaultFileName
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowOpenFileDialog(string filter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = filter
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowFolderBrowserDialog()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }
    }
}
