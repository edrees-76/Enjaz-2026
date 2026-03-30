namespace Enjaz
{
    /// <summary>
    /// ثوابت المنظومة — القيم الافتراضية والمعرفات الثابتة
    /// Application-wide constants — default values and fixed identifiers
    /// </summary>
    public static class AppConstants
    {
        // Certificate Defaults
        public const string DefaultCertificateType = "شهادة خلو من الاشعاع";
        public const string DefaultIssuingAuthority = "ادارة الرقابة";
        public const string DefaultRecipientName = "عام";
        public const string DefaultEnvironmentalAnalysisType = "تحليل مبدئي (دون الوصول لحالة الاتزان)";
        public const string DefaultResult = "خالية من العناصر المشعة المصنعة";

        // Security
        public const int MaxLoginAttempts = 5;
        public const int LoginLockoutMinutes = 5;
        public const int SessionTimeoutMinutes = 10;

        // Pagination
        public const int DefaultPageSize = 50;
        public const int MaxExportPageSize = 1000;

        // Date Formats
        public const string DateFormat = "yyyy-MM-dd";
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const string ArabicDateFormat = "dddd، dd MMMM yyyy - hh:mm tt";
        public const string ArabicCulture = "ar-LY";

        // Database
        public const string DatabaseFileName = "certificates.db";
        public const string AppFolderName = "Enjaz";
        public const string BackupFolderName = "Enjaz_Backups";
        public const string SettingsFileName = "settings.json";
    }
}
