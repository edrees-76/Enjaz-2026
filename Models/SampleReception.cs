using System;
using System.Collections.ObjectModel;

namespace Enjaz.Models
{
    /// <summary>
    /// نموذج استلام العينات (Sample Reception Model)
    /// </summary>
    public class SampleReception
    {
        public int Id { get; set; }
        public int Sequence { get; set; }

        /// <summary>
        /// رقم طلب التحليل (يدوي)
        /// Analysis Request Number (Manual Input)
        /// </summary>
        public string AnalysisRequestNumber { get; set; } = string.Empty;

        public string? NotificationNumber { get; set; }
        public string? DeclarationNumber { get; set; }
        public string? Supplier { get; set; }
        public string? Sender { get; set; }
        public string? Origin { get; set; }
        public string? PolicyNumber { get; set; }
        
        /// <summary>
        /// رقم الإيصال المالي (اختياري هنا)
        /// </summary>
        public string? FinancialReceiptNumber { get; set; }

        private string _certificateType = string.Empty;
        /// <summary>
        /// نوع العينة/الشهادة المؤملة (بيئية أو استهلاكية)
        /// </summary>
        public string CertificateType 
        { 
            get => _certificateType; 
            set => _certificateType = value?.Replace("شهادة بيئية", "عينات بيئية").Replace("شهادة استهلاكية", "عينات استهلاكية") ?? string.Empty; 
        }

        public DateTime Date { get; set; } = DateTime.Now;

        /// <summary>
        /// حالة الاستلام (مثل: لم يتم إصدار شهادة، أو تم إصدار شهادة)
        /// </summary>
        public string Status { get; set; } = "لم يتم إصدار شهادة";

        public int CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? UpdatedBy { get; set; }
        public string? UpdatedByName { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// عدد العينات
        /// </summary>
        public int SampleCount => Samples.Count;

        public ObservableCollection<ReceptionSample> Samples { get; set; } = new ObservableCollection<ReceptionSample>();
    }
}
