using System;
using System.Text.RegularExpressions;

namespace Enjaz.Helpers
{
    /// <summary>
    /// مساعد مركزي لتطبيع أرقام العينات وأسماء الجهات المرسلة لضمان دقة التحقق من التكرار
    /// Central helper for normalizing sample numbers and sender names for uniqueness validation
    /// 
    /// ⚠️ جميع مسارات التحقق (اللحظي، الداخلي، Repository، الحفظ النهائي) تستخدم هذه الدوال حصرياً
    /// </summary>
    public static class SampleValidationHelper
    {
        /// <summary>
        /// تطبيع رقم العينة: إزالة المسافات الخارجية، تجريد الأصفار البادئة
        /// "00150" → "150", "0000" → "0", " 150 " → "150"
        /// المسافات الداخلية تبقى كما هي (الرقم يُعامل كنص)
        /// </summary>
        public static string NormalizeSampleNumber(string? sampleNumber)
        {
            if (string.IsNullOrWhiteSpace(sampleNumber))
                return string.Empty;

            string trimmed = sampleNumber.Trim();
            string noLeadingZeros = trimmed.TrimStart('0');

            if (string.IsNullOrEmpty(noLeadingZeros))
            {
                // إذا كانت القيمة تتكون من أصفار فقط مثل "0" أو "0000"
                return "0";
            }

            return noLeadingZeros;
        }

        /// <summary>
        /// تطبيع اسم الجهة المرسلة للمقارنة فقط — لا يؤثر على التخزين أو العرض
        /// توحيد الهمزات، الياء والألف المقصورة، التاء المربوطة، وإزالة المسافات المتكررة
        /// "شركة الإتقان" → "شركه الاتقان"
        /// </summary>
        public static string NormalizeSender(string? sender)
        {
            if (string.IsNullOrWhiteSpace(sender))
                return string.Empty;

            string normalized = sender.Trim();

            // 1. توحيد الهمزات (أ، إ، آ → ا)
            normalized = normalized.Replace("أ", "ا")
                                   .Replace("إ", "ا")
                                   .Replace("آ", "ا");

            // 2. توحيد الياء والألف المقصورة (ى → ي)
            normalized = normalized.Replace("ى", "ي");

            // 3. توحيد التاء المربوطة والهاء (ة -> ه) — للمقارنة فقط
            normalized = normalized.Replace("ة", "ه");

            // 4. توحيد كتابة (ازوارة / زوارة)
            normalized = normalized.Replace("ازواره", "زواره");

            // 5. توحيد كتابة حرف العطف (و الادوية -> والادوية)
            normalized = normalized.Replace("و الادويه", "والادويه")
                                   .Replace("و الاغذيه", "والاغذيه");

            // 6. توحيد الفواصل والشرطات
            normalized = Regex.Replace(normalized, @"\s*-\s*", " - ");

            // 7. إزالة المسافات المتكررة بين الكلمات وتحويلها إلى مسافة واحدة
            normalized = Regex.Replace(normalized, @"\s+", " ");

            return normalized.Trim();
        }
    }
}
