using System;

namespace Enjaz.Models
{
    /// <summary>
    /// نموذج الشهادة - يمثل بيانات الشهادة في النظام
    /// Certificate Model - Represents certificate data in the system
    /// </summary>
    public class Certificate
    {
        /// <summary>
        /// المعرف الفريد للشهادة
        /// </summary>
        public int Id { get; set; }
        public int Sequence { get; set; }

        /// <summary>
        /// رقم الشهادة الفريد
        /// </summary>
        public string CertificateNumber { get; set; } = string.Empty;

        /// <summary>
        /// معرف الاستلام المرتبط (إن وجد)
        /// </summary>
        public int? ReceptionId { get; set; }

        /// <summary>
        /// اسم المستفيد / حامل الشهادة
        /// </summary>
        public string RecipientName { get; set; } = string.Empty;

        private string _certificateType = string.Empty;
        /// <summary>
        /// نوع الشهادة
        /// </summary>
        public string CertificateType 
        { 
            get => _certificateType; 
            set => _certificateType = value?.Replace("شهادة بيئية", "عينات بيئية").Replace("شهادة استهلاكية", "عينات استهلاكية") ?? string.Empty; 
        }

        /// <summary>
        /// وصف أو ملاحظات إضافية
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// تاريخ إصدار الشهادة
        /// </summary>
        public DateTime IssueDate { get; set; } = DateTime.Now;

        /// <summary>
        /// تاريخ انتهاء الصلاحية (اختياري)
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// معرف المستخدم الذي أنشأ الشهادة
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// اسم المستخدم الذي قام بالإنشاء (للعرض فقط)
        /// Created By User Name
        /// </summary>
        public string? CreatedByName { get; set; }

        public string IssuingAuthority { get; set; } = string.Empty;

        /// <summary>
        /// نوع التحليل
        /// Analysis Type
        /// </summary>
        public string? AnalysisType { get; set; }

        // --- بيانات الجهة المرسلة والمورد ---
        public string? Sender { get; set; }
        public string? Supplier { get; set; }

        public string? Origin { get; set; }
        public string? DeclarationNumber { get; set; }
        public string? PolicyNumber { get; set; }
        public string? NotificationNumber { get; set; }
        public string? FinancialReceiptNumber { get; set; }

        // --- المعتمِدين ---
        public string? SpecialistName { get; set; }
        public string? SectionHeadName { get; set; }
        public string? ManagerName { get; set; }

        public string? Notes { get; set; }

        /// <summary>
        /// تاريخ إنشاء السجل
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// معرف المستخدم الذي قام بآخر تعديل
        /// ID of user who last modified the certificate
        /// </summary>
        public int? UpdatedBy { get; set; }

        /// <summary>
        /// اسم المستخدم الذي قام بآخر تعديل (للعرض فقط)
        /// Updated By User Name
        /// </summary>
        public string? UpdatedByName { get; set; }

        /// <summary>
        /// تاريخ آخر تعديل
        /// Timestamp of last modification
        /// </summary>
        public DateTime? UpdatedAt { get; set; }



        /// <summary>
        /// عدد العينات المرتبطة بهذه الشهادة
        /// Sample count for this certificate
        /// </summary>
        public int SampleCount { get; set; }

        /// <summary>
        /// عدد العينات البيئية (محسوب)
        /// Environmental Sample Count (Calculated)
        /// </summary>
        public int EnvironmentalSampleCount 
        {
            get
            {
                if (!string.IsNullOrEmpty(CertificateType) && 
                   (CertificateType.Contains("بيئية") || CertificateType.Contains("بيييه") || CertificateType.Contains("بيئيه")))
                {
                    return SampleCount;
                }
                return 0;
            }
        }

        /// <summary>
        /// عدد العينات الاستهلاكية (محسوب)
        /// Consumable Sample Count (Calculated)
        /// </summary>
        public int ConsumableSampleCount
        {
            get
            {
                if (!string.IsNullOrEmpty(CertificateType) && CertificateType.Contains("استهلاكية"))
                {
                    return SampleCount;
                }
                return 0;
            }
        }

        /// <summary>
        /// قائمة العينات (للواجهة والطباعة)
        /// Samples Collection (For UI and Printing)
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<Sample> Samples { get; set; } = new System.Collections.ObjectModel.ObservableCollection<Sample>();
    }
}
