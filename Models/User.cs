using System;

namespace Enjaz.Models
{
    /// <summary>
    /// أدوار المستخدمين في النظام
    /// User roles in the system
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// مشاهد - عرض وطباعة فقط
        /// Viewer - View and print only
        /// </summary>
        Viewer = 0,

        /// <summary>
        /// مستخدم - إصدار وتعديل الشهادات
        /// User - Issue and edit certificates
        /// </summary>
        User = 1,

        /// <summary>
        /// مدير النظام - تحكم كامل
        /// Admin - Full control
        /// </summary>
        Admin = 2
    }

    /// <summary>
    /// نتيجة تسجيل الدخول
    /// </summary>
    public enum LoginResult
    {
        Success,
        InvalidCredentials,
        AccountFrozen,
        Error
    }

    /// <summary>
    /// نموذج المستخدم - يمثل بيانات المستخدم في النظام
    /// User Model - Represents user data in the system
    /// </summary>
    public class User
    {
        /// <summary>
        /// المعرف الفريد للمستخدم
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// اسم المستخدم للدخول
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// كلمة المرور المشفرة
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// الاسم الكامل للمستخدم
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// دور المستخدم في النظام
        /// </summary>
        public UserRole Role { get; set; } = UserRole.User;

        /// <summary>
        /// هل المستخدم مدير؟ (للتوافقية مع الإصدار القديم)
        /// </summary>
        public bool IsAdmin 
        { 
            get => Role == UserRole.Admin;
            set => Role = value ? UserRole.Admin : UserRole.User;
        }

        /// <summary>
        /// تاريخ إنشاء الحساب
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// هل الحساب نشط؟ (false = مجمد)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// الحصول على اسم الدور بالعربية
        /// </summary>
        public string RoleDisplayName
        {
            get
            {
                return Role switch
                {
                    UserRole.Admin => "مدير النظام",
                    UserRole.User => "مستخدم",
                    UserRole.Viewer => "مشاهد",
                    _ => "غير محدد"
                };
            }
        }

        /// <summary>
        /// الحصول على حالة الحساب بالعربية
        /// </summary>
        public string StatusDisplayName => IsActive ? "نشط" : "موقوف";

        /// <summary>
        /// هل يمكن للمستخدم إصدار شهادات؟
        /// </summary>
        public bool CanIssueCertificates => Role == UserRole.Admin || Role == UserRole.User;

        /// <summary>
        /// هل يملك صلاحية التعديل على الشهادات؟ (حتى لو لم يكن مديراً)
        /// </summary>
        public bool IsEditor { get; set; } = true;

        /// <summary>
        /// هل يمكن للمستخدم تعديل الشهادات؟
        /// </summary>
        public bool CanEditCertificates => Role == UserRole.Admin || (Role == UserRole.User && IsEditor);

        /// <summary>
        /// صلاحيات المستخدم (مفصولة بفواصل)
        /// User permissions (comma separated)
        /// Example: "Certificates,Reports,Settings"
        /// </summary>
        public string Permissions { get; set; } = string.Empty;

        /// <summary>
        /// هل يمكن للمستخدم إدارة المستخدمين؟
        /// </summary>
        public bool CanManageUsers => Role == UserRole.Admin;

        /// <summary>
        /// هل يملك صلاحية الوصول لقسم الشهادات؟
        /// </summary>
        public bool HasCertificatesPermission => Role == UserRole.Admin || (Permissions?.Contains("Certificates") ?? false);

        /// <summary>
        /// هل يملك صلاحية الوصول لقسم التقارير؟
        /// </summary>
        public bool HasReportsPermission => Role == UserRole.Admin || (Permissions?.Contains("Reports") ?? false);

        /// <summary>
        /// هل يملك صلاحية الوصول لقسم الإعدادات؟
        /// </summary>
        public bool HasSettingsPermission => Role == UserRole.Admin || (Permissions?.Contains("Settings") ?? false);

        /// <summary>
        /// هل يملك صلاحية الوصول لقسم الإجراءات الإدارية؟
        /// </summary>
        public bool HasAdminProceduresPermission => Role == UserRole.Admin || (Permissions?.Contains("AdminProcedures") ?? false);

        /// <summary>
        /// هل يملك صلاحية الوصول لقسم استلام العينات؟
        /// </summary>
        public bool HasSampleReceptionsPermission => Role == UserRole.Admin || (Permissions?.Contains("SampleReceptions") ?? false);

        /// <summary>
        /// هل يملك صلاحية الوصول لقسم إدارة المستخدمين؟
        /// </summary>
        public bool HasUsersPermission => Role == UserRole.Admin || (Permissions?.Contains("Users") ?? false);
    }
}
