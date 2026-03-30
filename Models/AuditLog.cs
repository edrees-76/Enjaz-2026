using System;

namespace Enjaz.Models
{
    /// <summary>
    /// نموذج سجل النشاطات - يمثل عملية قام بها مستخدم
    /// Audit Log Model - Represents an operation performed by a user
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }
        
        /// <summary>
        /// معرف المستخدم (يمكن أن يكون فارغاً إذا تم حذف المستخدم لاحقاً)
        /// </summary>
        public int? UserId { get; set; }
        
        /// <summary>
        /// اسم المستخدم وقت العملية
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        
        /// <summary>
        /// نوع العملية (تعديل، حذف، إضافة، إلخ)
        /// </summary>
        public string Action { get; set; } = string.Empty;
        
        /// <summary>
        /// وصف تفصيلي لما حدث
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// توقيت العملية
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// معرف المرجع المرتبط (مثل رقم معرف الشهادة)
        /// </summary>
        public int? ReferenceId { get; set; }

        /// <summary>
        /// التنسيق للعرض في الجدول
        /// </summary>
        public string TimeFormatted => Timestamp.ToString("yyyy/MM/dd HH:mm:ss");
    }
}
