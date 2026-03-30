using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Enjaz.Models
{
    /// <summary>
    /// نموذج العينة - يمثل بيانات عينة واحدة في الشهادة
    /// Sample Model - Represents a single sample in the certificate
    /// </summary>
    public class Sample : INotifyPropertyChanged
    {
        private int _id;
        private int _certificateId;
        private int _root;
        private string _sampleNumber = string.Empty;
        private string _description = string.Empty;
        private DateTime _measurementDate = DateTime.Now;
        private string _result = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int Id 
        { 
            get => _id; 
            set { _id = value; OnPropertyChanged(); } 
        }

        /// <summary>
        /// معرف الشهادة المرتبطة
        /// </summary>
        public int CertificateId 
        { 
            get => _certificateId; 
            set { _certificateId = value; OnPropertyChanged(); } 
        }

        private int? _receptionId;
        /// <summary>
        /// معرف استلام العينات المرتبط
        /// </summary>
        public int? ReceptionId
        {
            get => _receptionId;
            set { _receptionId = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// روت (تسلسل)
        /// Root (Sequence)
        /// </summary>
        public int Root 
        { 
            get => _root; 
            set { _root = value; OnPropertyChanged(); } 
        }

        /// <summary>
        /// رقم العينة
        /// Sample Number
        /// </summary>
        public string SampleNumber 
        { 
            get => _sampleNumber; 
            set { _sampleNumber = value; OnPropertyChanged(); } 
        }

        /// <summary>
        /// وصف العينة
        /// Sample Description
        /// </summary>
        public string Description 
        { 
            get => _description; 
            set { _description = value; OnPropertyChanged(); } 
        }

        /// <summary>
        /// تاريخ القياس
        /// Measurement Date
        /// </summary>
        public DateTime MeasurementDate 
        { 
            get => _measurementDate; 
            set { _measurementDate = value; OnPropertyChanged(); } 
        }

        /// <summary>
        /// نتيجة القياس
        /// Measurement Result
        /// </summary>
        public string Result 
        { 
            get => _result; 
            set { _result = value; OnPropertyChanged(); } 
        }

        // --- Environmental Certificate Fields ---
        // حقول خاصة بالشهادات البيئية
        
        private string _isotopeK40 = string.Empty;
        private string _isotopeRa226 = string.Empty;
        private string _isotopeTh232 = string.Empty;
        private string _isotopeRa = string.Empty;
        private string _isotopeCs137 = string.Empty;

        public string IsotopeK40
        {
            get => _isotopeK40;
            set { _isotopeK40 = value; OnPropertyChanged(); }
        }

        public string IsotopeRa226
        {
            get => _isotopeRa226;
            set { _isotopeRa226 = value; OnPropertyChanged(); }
        }

        public string IsotopeTh232
        {
            get => _isotopeTh232;
            set { _isotopeTh232 = value; OnPropertyChanged(); }
        }

        public string IsotopeRa
        {
            get => _isotopeRa;
            set { _isotopeRa = value; OnPropertyChanged(); }
        }

        public string IsotopeCs137
        {
            get => _isotopeCs137;
            set { _isotopeCs137 = value; OnPropertyChanged(); }
        }
    }
}
