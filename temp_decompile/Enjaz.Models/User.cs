using System;

namespace Enjaz.Models;

public class User
{
	public int Id { get; set; }

	public string Username { get; set; } = string.Empty;

	public string PasswordHash { get; set; } = string.Empty;

	public string FullName { get; set; } = string.Empty;

	public UserRole Role { get; set; } = UserRole.User;

	public bool IsAdmin
	{
		get
		{
			return Role == UserRole.Admin;
		}
		set
		{
			Role = ((!value) ? UserRole.User : UserRole.Admin);
		}
	}

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	public bool IsActive { get; set; } = true;

	public string RoleDisplayName
	{
		get
		{
			UserRole role = Role;
			if (1 == 0)
			{
			}
			string result = role switch
			{
				UserRole.Admin => "مدير النظام", 
				UserRole.User => "مستخدم", 
				UserRole.Viewer => "مشاهد", 
				_ => "غير محدد", 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}

	public string StatusDisplayName => IsActive ? "نشط" : "موقوف";

	public bool CanIssueCertificates => Role == UserRole.Admin || Role == UserRole.User;

	public bool IsEditor { get; set; } = true;

	public bool CanEditCertificates => Role == UserRole.Admin || (Role == UserRole.User && IsEditor);

	public string Permissions { get; set; } = string.Empty;

	public bool CanManageUsers => Role == UserRole.Admin;

	public bool HasCertificatesPermission => Role == UserRole.Admin || (Permissions?.Contains("Certificates") ?? false);

	public bool HasReportsPermission => Role == UserRole.Admin || (Permissions?.Contains("Reports") ?? false);

	public bool HasSettingsPermission => Role == UserRole.Admin || (Permissions?.Contains("Settings") ?? false);

	public bool HasAdminProceduresPermission => Role == UserRole.Admin || (Permissions?.Contains("AdminProcedures") ?? false);

	public bool HasSampleReceptionsPermission => Role == UserRole.Admin || (Permissions?.Contains("SampleReceptions") ?? false);

	public bool HasUsersPermission => Role == UserRole.Admin || (Permissions?.Contains("Users") ?? false);
}
