using System;

namespace Enjaz.Models
{
    /// <summary>
    /// حالات نتيجة فحص تفرد رقم العينة
    /// Sample uniqueness check result status
    /// </summary>
    public enum SampleCheckResult
    {
        /// <summary>فريد — غير مستخدم</summary>
        Unique,
        /// <summary>مكرر في شهادة نشطة — يُمنع الإدخال</summary>
        DuplicateActive,
        /// <summary>وُجد في شهادة محذوفة — مسموح مع إشعار تنبيهي</summary>
        FoundInDeleted
    }

    /// <summary>
    /// كائن نتيجة فحص تفرد رقم العينة
    /// Sample uniqueness check result object
    /// </summary>
    public sealed class SampleUniquenessResult
    {
        public SampleCheckResult Status { get; init; } = SampleCheckResult.Unique;
        public int? CertificateId { get; init; }
        public string? CertificateNumber { get; init; }
        public DateTime? IssueDate { get; init; }
        public string? Sender { get; init; }
        public string? SampleNumber { get; init; }
    }
}
