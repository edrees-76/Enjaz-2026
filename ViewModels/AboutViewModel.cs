using System;

namespace Enjaz.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public string SystemName => "منظومة إنجاز الرقمية 2026";
        public string Version => "إصدار 1.0.0 (النسخة النهائية)";
        public string DesignerName => "م. ادريس فتح الله الهرى";
        public string DesignerPhone1 => "0925126355";
        public string DesignerPhone2 => "0917730110";
        public string DesignerEmail => "edreeselhery@gmail.com";
        
        public string SystemDescription => "تعد منظومة إنجاز الرقمية قفزة نوعية في إدارة وتوثيق الشهادات والبيانات، حيث تجمع بين قوة الأداء وبساطة التصميم. تعتمد المنظومة على تقنيات مايكروسوفت الحديثة لضمان أمان البيانات وسرعة المعالجة، مع واجهة مستخدم ذكية تدعم تعدد الأنماط البصرية (الفاتح والداكن) وتوفر تجربة مستخدم (UX) فائقة السلاسة تناسب تطلعات المؤسسات العصرية.";

        public AboutViewModel()
        {
        }
    }
}
