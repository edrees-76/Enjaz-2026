using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Enjaz.Models;
using Microsoft.Data.Sqlite;

namespace Enjaz.Services.Repositories;

public class CertificateRepository
{
	private readonly DatabaseService _db;

	private readonly UserService _userService;

	public CertificateRepository(DatabaseService db, UserService userService)
	{
		_db = db;
		_userService = userService;
	}

	private static Certificate MapCertificateFromReader(SqliteDataReader reader, int columnCount = 0)
	{
		if (columnCount == 0)
		{
			columnCount = reader.FieldCount;
		}
		return new Certificate
		{
			Id = reader.GetInt32(0),
			CertificateNumber = reader.GetString(1),
			RecipientName = reader.GetString(2),
			CertificateType = reader.GetString(3),
			Description = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
			IssueDate = SafeParseDate(reader.GetString(5), "yyyy-MM-dd"),
			ExpiryDate = (reader.IsDBNull(6) ? ((DateTime?)null) : new DateTime?(SafeParseDate(reader.GetString(6), "yyyy-MM-dd"))),
			IssuingAuthority = (reader.IsDBNull(7) ? "" : reader.GetString(7)),
			CreatedBy = reader.GetInt32(8),
			CreatedByName = (reader.IsDBNull(9) ? "" : reader.GetString(9)),
			CreatedAt = SafeParseDate(reader.GetString(10), "yyyy-MM-dd HH:mm:ss"),
			AnalysisType = (reader.IsDBNull(11) ? "" : reader.GetString(11)),
			Sender = (reader.IsDBNull(12) ? "" : reader.GetString(12)),
			Supplier = (reader.IsDBNull(13) ? "" : reader.GetString(13)),
			Origin = (reader.IsDBNull(14) ? "" : reader.GetString(14)),
			DeclarationNumber = (reader.IsDBNull(15) ? "" : reader.GetString(15)),
			PolicyNumber = (reader.IsDBNull(16) ? "" : reader.GetString(16)),
			NotificationNumber = (reader.IsDBNull(17) ? "" : reader.GetString(17)),
			FinancialReceiptNumber = (reader.IsDBNull(18) ? "" : reader.GetString(18)),
			SpecialistName = (reader.IsDBNull(19) ? "" : reader.GetString(19)),
			SectionHeadName = (reader.IsDBNull(20) ? "" : reader.GetString(20)),
			ManagerName = (reader.IsDBNull(21) ? "" : reader.GetString(21)),
			Notes = (reader.IsDBNull(22) ? "" : reader.GetString(22)),
			UpdatedBy = (reader.IsDBNull(23) ? ((int?)null) : new int?(reader.GetInt32(23))),
			UpdatedByName = (reader.IsDBNull(24) ? "" : reader.GetString(24)),
			UpdatedAt = (reader.IsDBNull(25) ? ((DateTime?)null) : new DateTime?(SafeParseDate(reader.GetString(25), "yyyy-MM-dd HH:mm:ss"))),
			SampleCount = ((columnCount > 26 && !reader.IsDBNull(26)) ? reader.GetInt32(26) : 0),
			ReceptionId = ((columnCount > 26 && reader.GetName(columnCount - 1) == "ReceptionId" && !reader.IsDBNull(columnCount - 1)) ? new int?(reader.GetInt32(columnCount - 1)) : ((int?)null))
		};
	}

	private static Sample MapSampleFromReader(SqliteDataReader reader)
	{
		return new Sample
		{
			Id = reader.GetInt32(0),
			CertificateId = reader.GetInt32(1),
			Root = reader.GetInt32(2),
			SampleNumber = (reader.IsDBNull(3) ? "" : reader.GetString(3)),
			Description = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
			MeasurementDate = SafeParseDate(reader.GetString(5), "yyyy-MM-dd"),
			Result = (reader.IsDBNull(6) ? "" : reader.GetString(6)),
			IsotopeK40 = (reader.IsDBNull(7) ? "" : reader.GetString(7)),
			IsotopeRa226 = (reader.IsDBNull(8) ? "" : reader.GetString(8)),
			IsotopeTh232 = (reader.IsDBNull(9) ? "" : reader.GetString(9)),
			IsotopeRa = (reader.IsDBNull(10) ? "" : reader.GetString(10)),
			IsotopeCs137 = (reader.IsDBNull(11) ? "" : reader.GetString(11))
		};
	}

	private static DateTime SafeParseDate(string dateStr, string primaryFormat)
	{
		if (DateTime.TryParseExact(dateStr, primaryFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
		{
			return result;
		}
		string[] formats = new string[5] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd", "dd/MM/yyyy", "MM/dd/yyyy" };
		if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
		{
			return result;
		}
		if (DateTime.TryParse(dateStr, out result))
		{
			return result;
		}
		LoggerService.LogWarning($"Failed to parse date: '{dateStr}' with format '{primaryFormat}'. Using DateTime.MinValue.");
		return DateTime.MinValue;
	}

	private string GetCertificateSummary(Certificate c)
	{
		if (c == null)
		{
			return "ط؛ظٹط± ظ…ط¹ط±ظˆظپ";
		}
		List<string> list = new List<string>();
		list.Add("ط±ظ‚ظ… ط§ظ„ط\u00b4ظ‡ط§ط\u00afط©: " + c.CertificateNumber);
		list.Add("ط§ظ„ظ†ظˆط¹: " + c.CertificateType);
		list.Add("ط§ظ„ط¬ظ‡ط©: " + c.RecipientName);
		if (!string.IsNullOrEmpty(c.Sender))
		{
			list.Add("ط§ظ„ظ…ط±ط³ظ„: " + c.Sender);
		}
		if (!string.IsNullOrEmpty(c.Supplier))
		{
			list.Add("ط§ظ„ظ…ظˆط±ط\u00af: " + c.Supplier);
		}
		if (!string.IsNullOrEmpty(c.Origin))
		{
			list.Add("ط§ظ„ظ…ظ†ط\u00b4ط£: " + c.Origin);
		}
		if (!string.IsNullOrEmpty(c.NotificationNumber))
		{
			list.Add("ط§ظ„ط¥ط®ط·ط§ط±: " + c.NotificationNumber);
		}
		if (!string.IsNullOrEmpty(c.DeclarationNumber))
		{
			list.Add("ط§ظ„ط¥ظ‚ط±ط§ط±: " + c.DeclarationNumber);
		}
		if (!string.IsNullOrEmpty(c.FinancialReceiptNumber))
		{
			list.Add("ط§ظ„ط¥ظٹطµط§ظ„: " + c.FinancialReceiptNumber);
		}
		if (!string.IsNullOrEmpty(c.PolicyNumber))
		{
			list.Add("ط§ظ„ط\u00a8ظˆظ„ظٹطµط©: " + c.PolicyNumber);
		}
		if (!string.IsNullOrEmpty(c.AnalysisType))
		{
			list.Add("ط§ظ„طھط\u00adظ„ظٹظ„: " + c.AnalysisType);
		}
		if (!string.IsNullOrEmpty(c.SpecialistName))
		{
			list.Add("ط§ظ„ظ…ط®طھطµ: " + c.SpecialistName);
		}
		if (!string.IsNullOrEmpty(c.SectionHeadName))
		{
			list.Add("ط±ط¦ظٹط³ ط§ظ„ظ‚ط³ظ…: " + c.SectionHeadName);
		}
		if (!string.IsNullOrEmpty(c.ManagerName))
		{
			list.Add("ط§ظ„ظ…ط\u00afظٹط±: " + c.ManagerName);
		}
		return string.Join(" | ", list);
	}

	private string GetCertificateChanges(Certificate oldCert, Certificate newCert)
	{
		List<string> list = new List<string>();
		if (oldCert == null || newCert == null)
		{
			return "طھط¹ط\u00afظٹظ„ ط\u00a8ظٹط§ظ†ط§طھ";
		}
		if (oldCert.RecipientName != newCert.RecipientName)
		{
			list.Add(Fmt("ط§ظ„ط¬ظ‡ط©", oldCert.RecipientName, newCert.RecipientName));
		}
		if (oldCert.CertificateType != newCert.CertificateType)
		{
			list.Add(Fmt("ط§ظ„ظ†ظˆط¹", oldCert.CertificateType, newCert.CertificateType));
		}
		if (oldCert.Description != newCert.Description)
		{
			list.Add(Fmt("ط§ظ„ظˆطµظپ", oldCert.Description, newCert.Description));
		}
		if (oldCert.IssueDate.Date != newCert.IssueDate.Date)
		{
			list.Add(Fmt("طھط§ط±ظٹط® ط§ظ„ط¥طµط\u00afط§ط±", oldCert.IssueDate.ToString("yyyy/MM/dd"), newCert.IssueDate.ToString("yyyy/MM/dd")));
		}
		if (oldCert.ExpiryDate != newCert.ExpiryDate)
		{
			list.Add(Fmt("طھط§ط±ظٹط® ط§ظ„طµظ„ط§ط\u00adظٹط©", oldCert.ExpiryDate?.ToString("yyyy/MM/dd"), newCert.ExpiryDate?.ToString("yyyy/MM/dd")));
		}
		if (oldCert.IssuingAuthority != newCert.IssuingAuthority)
		{
			list.Add(Fmt("ط¬ظ‡ط© ط§ظ„ط¥طµط\u00afط§ط±", oldCert.IssuingAuthority, newCert.IssuingAuthority));
		}
		if (oldCert.Sender != newCert.Sender)
		{
			list.Add(Fmt("ط§ظ„ط¬ظ‡ط© ط§ظ„ظ…ط±ط³ظ„ط©", oldCert.Sender, newCert.Sender));
		}
		if (oldCert.Supplier != newCert.Supplier)
		{
			list.Add(Fmt("ط§ظ„ظ…ظˆط±ط\u00af", oldCert.Supplier, newCert.Supplier));
		}
		if (oldCert.Origin != newCert.Origin)
		{
			list.Add(Fmt("ط\u00a8ظ„ط\u00af ط§ظ„ظ…ظ†ط\u00b4ط£", oldCert.Origin, newCert.Origin));
		}
		if (oldCert.DeclarationNumber != newCert.DeclarationNumber)
		{
			list.Add(Fmt("ط§ظ„ط¥ظ‚ط±ط§ط± ط§ظ„ط¬ظ…ط±ظƒظٹ", oldCert.DeclarationNumber, newCert.DeclarationNumber));
		}
		if (oldCert.NotificationNumber != newCert.NotificationNumber)
		{
			list.Add(Fmt("ط±ظ‚ظ… ط§ظ„ط¥ط®ط·ط§ط±", oldCert.NotificationNumber, newCert.NotificationNumber));
		}
		if (oldCert.PolicyNumber != newCert.PolicyNumber)
		{
			list.Add(Fmt("ط±ظ‚ظ… ط§ظ„ط\u00a8ظˆظ„ظٹطµط©", oldCert.PolicyNumber, newCert.PolicyNumber));
		}
		if (oldCert.FinancialReceiptNumber != newCert.FinancialReceiptNumber)
		{
			list.Add(Fmt("ط§ظ„ط¥ظٹطµط§ظ„ ط§ظ„ظ…ط§ظ„ظٹ", oldCert.FinancialReceiptNumber, newCert.FinancialReceiptNumber));
		}
		if (oldCert.AnalysisType != newCert.AnalysisType)
		{
			list.Add(Fmt("ظ†ظˆط¹ ط§ظ„طھط\u00adظ„ظٹظ„", oldCert.AnalysisType, newCert.AnalysisType));
		}
		if (oldCert.SpecialistName != newCert.SpecialistName)
		{
			list.Add(Fmt("ط§ظ„ظ…ط®طھطµ", oldCert.SpecialistName, newCert.SpecialistName));
		}
		if (oldCert.SectionHeadName != newCert.SectionHeadName)
		{
			list.Add(Fmt("ط±ط¦ظٹط³ ط§ظ„ظ‚ط³ظ…", oldCert.SectionHeadName, newCert.SectionHeadName));
		}
		if (oldCert.ManagerName != newCert.ManagerName)
		{
			list.Add(Fmt("ط§ظ„ظ…ط\u00afظٹط±", oldCert.ManagerName, newCert.ManagerName));
		}
		if (oldCert.Notes != newCert.Notes)
		{
			list.Add(Fmt("ط§ظ„ظ…ظ„ط§ط\u00adط\u00b8ط§طھ", oldCert.Notes, newCert.Notes));
		}
		List<Sample> oldSamples = oldCert.Samples?.ToList() ?? new List<Sample>();
		List<Sample> newSamples = newCert.Samples?.ToList() ?? new List<Sample>();
		List<Sample> list2 = newSamples.Where((Sample ns) => ns.Id == 0 || !oldSamples.Any((Sample os) => os.Id == ns.Id)).ToList();
		foreach (Sample item in list2)
		{
			list.Add($"ظ‚ط§ظ… ط\u00a8ط¥ط¶ط§ظپط© ط¹ظٹظ†ط© ط±ظ‚ظ… ({item.SampleNumber}) ظˆظˆطµظپظ‡ط§ ({item.Description}) ظˆظ†طھظٹط¬طھظ‡ط§ ({item.Result})");
		}
		foreach (Sample sOld in oldSamples)
		{
			Sample sample = newSamples.FirstOrDefault((Sample ns) => ns.Id == sOld.Id);
			if (sample != null)
			{
				List<string> list3 = new List<string>();
				if (sOld.SampleNumber != sample.SampleNumber)
				{
					list3.Add(Fmt("ط±ظ‚ظ… ط§ظ„ط¹ظٹظ†ط©", sOld.SampleNumber, sample.SampleNumber));
				}
				if (sOld.Description != sample.Description)
				{
					list3.Add(Fmt("ط§ظ„ظˆطµظپ", sOld.Description, sample.Description));
				}
				if (sOld.Result != sample.Result)
				{
					list3.Add(Fmt("ط§ظ„ظ†طھظٹط¬ط©", sOld.Result, sample.Result));
				}
				if (sOld.IsotopeK40 != sample.IsotopeK40)
				{
					list3.Add(Fmt("K40", sOld.IsotopeK40, sample.IsotopeK40));
				}
				if (sOld.IsotopeRa226 != sample.IsotopeRa226)
				{
					list3.Add(Fmt("Ra226", sOld.IsotopeRa226, sample.IsotopeRa226));
				}
				if (sOld.IsotopeTh232 != sample.IsotopeTh232)
				{
					list3.Add(Fmt("Th232", sOld.IsotopeTh232, sample.IsotopeTh232));
				}
				if (sOld.IsotopeRa != sample.IsotopeRa)
				{
					list3.Add(Fmt("Ra", sOld.IsotopeRa, sample.IsotopeRa));
				}
				if (sOld.IsotopeCs137 != sample.IsotopeCs137)
				{
					list3.Add(Fmt("Cs137", sOld.IsotopeCs137, sample.IsotopeCs137));
				}
				if (list3.Count > 0)
				{
					list.Add("طھط¹ط\u00afظٹظ„ ط¹ظٹظ†ط© (ط±ظ‚ظ… " + sOld.SampleNumber + "): " + string.Join(" | ", list3));
				}
			}
		}
		List<Sample> list4 = oldSamples.Where((Sample os) => os.Id > 0 && !newSamples.Any((Sample ns) => ns.Id == os.Id)).ToList();
		foreach (Sample item2 in list4)
		{
			list.Add($"ظ‚ط§ظ… ط\u00a8ط\u00adط°ظپ ط¹ظٹظ†ط© ط±ظ‚ظ… ({item2.SampleNumber}) ظˆظˆطµظپظ‡ط§ ({item2.Description})");
		}
		return (list.Count > 0) ? string.Join(" | ", list) : "طھط¹ط\u00afظٹظ„ ط\u00a8ط\u00afظˆظ† طھط؛ظٹظٹط± ظپظٹ ط§ظ„ط\u00adظ‚ظˆظ„ ط§ظ„ط£ط³ط§ط³ظٹط©";
		static string Fmt(string field, string? oldV, string? newV)
		{
			return $"{field}: ظ…ظ† ({oldV ?? "ظپط§ط±ط؛"}) ط¥ظ„ظ‰ ({newV ?? "ظپط§ط±ط؛"})";
		}
	}

	public Task<List<Certificate>> GetCertificatesPaginatedAsync(int pageNumber, int pageSize, bool showDeleted = false)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<Certificate> certificates = new List<Certificate>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			int offset = (pageNumber - 1) * pageSize;
			string query = "SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, \r\n                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, \r\n                              c.CreatedBy, c.CreatedByName, datetime(c.CreatedAt, 'localtime'),\r\n                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,\r\n                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,\r\n                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,\r\n                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt,\r\n                              (SELECT COUNT(*) FROM Samples s WHERE s.CertificateId = c.Id) AS SampleCount\r\n                              FROM Certificates c \r\n                              ORDER BY c.CreatedAt DESC\r\n                              LIMIT @Limit OFFSET @Offset;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Limit", pageSize);
			command.Parameters.AddWithValue("@Offset", offset);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				try
				{
					certificates.Add(MapCertificateFromReader(reader));
				}
				catch (Exception ex)
				{
					LoggerService.LogError("Failed to map certificate record, skipping...", ex);
				}
			}
			return certificates;
		}, "GetCertificatesPaginatedAsync");
	}

	[Obsolete("Use GetCertificatesPaginatedAsync for better memory management")]
	public Task<List<Certificate>> GetAllCertificatesAsync()
	{
		LoggerService.LogWarning("GetAllCertificatesAsync called — consider using pagination instead");
		return GetCertificatesPaginatedAsync(1, 200);
	}

	public Task<List<Certificate>> GetCertificatesByDateRangeAsync(DateTime startDate, DateTime endDate)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<Certificate> certificates = new List<Certificate>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, \r\n                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, \r\n                              c.CreatedBy, c.CreatedByName, datetime(c.CreatedAt, 'localtime'),\r\n                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,\r\n                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,\r\n                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,\r\n                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt,\r\n                              (SELECT COUNT(*) FROM Samples s WHERE s.CertificateId = c.Id) AS SampleCount\r\n                              FROM Certificates c \r\n                              WHERE date(c.IssueDate) >= date(@StartDate) \r\n                              AND date(c.IssueDate) <= date(@EndDate)\r\n                              ORDER BY c.IssueDate ASC;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
			command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				certificates.Add(MapCertificateFromReader(reader));
			}
			return certificates;
		}, "GetCertificatesByDateRangeAsync");
	}

	public bool IsFinancialReceiptNumberUnique(string receiptNumber, int? excludeCertificateId = null)
	{
		if (string.IsNullOrWhiteSpace(receiptNumber))
		{
			return true;
		}
		using SqliteConnection sqliteConnection = new SqliteConnection(_db.ConnectionString);
		sqliteConnection.Open();
		string commandText = (excludeCertificateId.HasValue ? "SELECT COUNT(*) FROM Certificates WHERE FinancialReceiptNumber = @ReceiptNumber AND Id != @ExcludeId" : "SELECT COUNT(*) FROM Certificates WHERE FinancialReceiptNumber = @ReceiptNumber");
		using SqliteCommand sqliteCommand = new SqliteCommand(commandText, sqliteConnection);
		sqliteCommand.Parameters.AddWithValue("@ReceiptNumber", receiptNumber);
		if (excludeCertificateId.HasValue)
		{
			sqliteCommand.Parameters.AddWithValue("@ExcludeId", excludeCertificateId.Value);
		}
		int num = Convert.ToInt32(sqliteCommand.ExecuteScalar());
		return num == 0;
	}

	public Task<Certificate?> GetCertificateByReceptionIdAsync(int receptionId)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, \r\n                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, \r\n                              c.CreatedBy, c.CreatedByName, datetime(c.CreatedAt, 'localtime'),\r\n                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,\r\n                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,\r\n                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,\r\n                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt\r\n                              FROM Certificates c \r\n                              WHERE c.ReceptionId = @ReceptionId;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@ReceptionId", receptionId);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			if (await reader.ReadAsync())
			{
				Certificate cert = MapCertificateFromReader(reader);
				cert.Samples = new ObservableCollection<Sample>();
				reader.Close();
				string samplesQuery = "SELECT Id, CertificateId, Root, SampleNumber, Description, MeasurementDate, Result,\r\n                                      IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137\r\n                                      FROM Samples WHERE CertificateId = @Id ORDER BY Root ASC";
				using SqliteCommand samplesCmd = new SqliteCommand(samplesQuery, connection);
				samplesCmd.Parameters.AddWithValue("@Id", cert.Id);
				using SqliteDataReader samplesReader = await samplesCmd.ExecuteReaderAsync();
				while (await samplesReader.ReadAsync())
				{
					cert.Samples.Add(MapSampleFromReader(samplesReader));
				}
				return cert;
			}
			return (Certificate)null;
		}, "GetCertificateByReceptionIdAsync");
	}

	public Task<Certificate?> GetCertificateByIdAsync(int id)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, \r\n                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, \r\n                              c.CreatedBy, c.CreatedByName, datetime(c.CreatedAt, 'localtime'),\r\n                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,\r\n                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,\r\n                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,\r\n                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt, c.ReceptionId\r\n                              FROM Certificates c \r\n                              WHERE c.Id = @Id;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Id", id);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			if (await reader.ReadAsync())
			{
				Certificate cert = MapCertificateFromReader(reader);
				cert.Samples = new ObservableCollection<Sample>();
				reader.Close();
				string samplesQuery = "SELECT Id, CertificateId, Root, SampleNumber, Description, MeasurementDate, Result,\r\n                                      IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137\r\n                                      FROM Samples WHERE CertificateId = @Id ORDER BY Root ASC";
				using SqliteCommand samplesCmd = new SqliteCommand(samplesQuery, connection);
				samplesCmd.Parameters.AddWithValue("@Id", id);
				using SqliteDataReader samplesReader = await samplesCmd.ExecuteReaderAsync();
				while (await samplesReader.ReadAsync())
				{
					cert.Samples.Add(MapSampleFromReader(samplesReader));
				}
				return cert;
			}
			return (Certificate)null;
		}, "GetCertificateByIdAsync");
	}

	public Task<int> AddCertificateAsync(Certificate certificate)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			using SqliteTransaction transaction = connection.BeginTransaction();
			try
			{
				string query = "INSERT INTO Certificates \r\n                                 (CertificateNumber, RecipientName, CertificateType, Description, \r\n                                  IssueDate, ExpiryDate, IssuingAuthority, CreatedBy, CreatedByName,\r\n                                  AnalysisType, Sender, Supplier, Origin, DeclarationNumber,\r\n                                  PolicyNumber, NotificationNumber, FinancialReceiptNumber,\r\n                                  SpecialistName, SectionHeadName, ManagerName, Notes, ReceptionId)\r\n                                 VALUES (@CertificateNumber, @RecipientName, @CertificateType, @Description,\r\n                                         @IssueDate, @ExpiryDate, @IssuingAuthority, @CreatedBy, @CreatedByName,\r\n                                         @AnalysisType, @Sender, @Supplier, @Origin, @DeclarationNumber,\r\n                                         @PolicyNumber, @NotificationNumber, @FinancialReceiptNumber,\r\n                                         @SpecialistName, @SectionHeadName, @ManagerName, @Notes, @ReceptionId);\r\n                                 SELECT last_insert_rowid();";
				using SqliteCommand command = new SqliteCommand(query, connection, transaction);
				command.Parameters.AddWithValue("@CertificateNumber", "TEMP-" + Guid.NewGuid());
				command.Parameters.AddWithValue("@RecipientName", certificate.RecipientName);
				command.Parameters.AddWithValue("@CertificateType", certificate.CertificateType);
				command.Parameters.AddWithValue("@Description", certificate.Description ?? "");
				command.Parameters.AddWithValue("@IssueDate", certificate.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
				command.Parameters.AddWithValue("@ExpiryDate", certificate.ExpiryDate.HasValue ? ((IConvertible)certificate.ExpiryDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : ((IConvertible)DBNull.Value));
				command.Parameters.AddWithValue("@IssuingAuthority", certificate.IssuingAuthority ?? "");
				SqliteCommand checkUserCmd = new SqliteCommand("SELECT COUNT(*) FROM Users WHERE Id = @Uid", connection, transaction);
				checkUserCmd.Parameters.AddWithValue("@Uid", certificate.CreatedBy);
				long userCount = (long)((await checkUserCmd.ExecuteScalarAsync()) ?? ((object)0));
				int validUserId = certificate.CreatedBy;
				if (userCount == 0)
				{
					SqliteCommand getAdminCmd = new SqliteCommand("SELECT Id FROM Users WHERE Role = 2 LIMIT 1", connection, transaction);
					object adminId = await getAdminCmd.ExecuteScalarAsync();
					if (adminId != null)
					{
						validUserId = Convert.ToInt32(adminId);
					}
					else
					{
						SqliteCommand getUserCmd = new SqliteCommand("SELECT Id FROM Users LIMIT 1", connection, transaction);
						object anyId = await getUserCmd.ExecuteScalarAsync();
						validUserId = ((anyId == null) ? 1 : Convert.ToInt32(anyId));
					}
				}
				command.Parameters.AddWithValue("@CreatedBy", validUserId);
				command.Parameters.AddWithValue("@CreatedByName", certificate.CreatedByName ?? "");
				command.Parameters.AddWithValue("@AnalysisType", certificate.AnalysisType ?? "");
				command.Parameters.AddWithValue("@Sender", certificate.Sender ?? "");
				command.Parameters.AddWithValue("@Supplier", certificate.Supplier ?? "");
				command.Parameters.AddWithValue("@Origin", certificate.Origin ?? "");
				command.Parameters.AddWithValue("@DeclarationNumber", certificate.DeclarationNumber ?? "");
				command.Parameters.AddWithValue("@PolicyNumber", certificate.PolicyNumber ?? "");
				command.Parameters.AddWithValue("@NotificationNumber", certificate.NotificationNumber ?? "");
				command.Parameters.AddWithValue("@FinancialReceiptNumber", certificate.FinancialReceiptNumber ?? "");
				command.Parameters.AddWithValue("@SpecialistName", certificate.SpecialistName ?? "");
				command.Parameters.AddWithValue("@SectionHeadName", certificate.SectionHeadName ?? "");
				command.Parameters.AddWithValue("@ManagerName", certificate.ManagerName ?? "");
				command.Parameters.AddWithValue("@Notes", certificate.Notes ?? "");
				command.Parameters.AddWithValue("@ReceptionId", ((object)certificate.ReceptionId) ?? DBNull.Value);
				object idObj = await command.ExecuteScalarAsync();
				if (idObj == null)
				{
					throw new Exception("Failed to retrieve ID");
				}
				int newId = Convert.ToInt32(idObj);
				string typeCode = (certificate.CertificateType.Contains("ط\u00a8ظٹط¦ظٹط©") ? "E" : "C");
				string year = certificate.IssueDate.ToString("yy");
				string patternE = "RM-E-" + year + "-%";
				string patternC = "RM-C-" + year + "-%";
				string patternU = "RM-" + year + "-%";
				SqliteCommand maxCmd = new SqliteCommand("SELECT CertificateNumber FROM Certificates WHERE (CertificateNumber LIKE @PatE OR CertificateNumber LIKE @PatC OR CertificateNumber LIKE @PatU) AND Id != @CurrentId ORDER BY Id DESC LIMIT 1;", connection, transaction);
				maxCmd.Parameters.AddWithValue("@PatE", patternE);
				maxCmd.Parameters.AddWithValue("@PatC", patternC);
				maxCmd.Parameters.AddWithValue("@PatU", patternU);
				maxCmd.Parameters.AddWithValue("@CurrentId", newId);
				object lastNumObj = await maxCmd.ExecuteScalarAsync();
				int nextSequence = 1;
				if (lastNumObj != null && lastNumObj != DBNull.Value)
				{
					string lastNum = lastNumObj.ToString() ?? "";
					string[] parts = lastNum.Split('-');
					if (parts.Length >= 3 && int.TryParse(parts[^1], out var lastSeq))
					{
						nextSequence = lastSeq + 1;
					}
				}
				int sequenceInYear = nextSequence;
				string finalNumber = $"RM-{typeCode}-{year}-{sequenceInYear:D4}";
				SqliteCommand updateCmd = new SqliteCommand("UPDATE Certificates SET CertificateNumber = @Num WHERE Id = @Id;", connection, transaction);
				updateCmd.Parameters.AddWithValue("@Num", finalNumber);
				updateCmd.Parameters.AddWithValue("@Id", newId);
				await updateCmd.ExecuteNonQueryAsync();
				if (certificate.Samples != null && certificate.Samples.Any())
				{
					int rootNumber = 1;
					foreach (Sample sample in certificate.Samples)
					{
						string insertSampleQuery = "INSERT INTO Samples \r\n                                (CertificateId, Root, SampleNumber, Description, MeasurementDate, Result,\r\n                                 IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137)\r\n                                VALUES (@CertificateId, @Root, @SampleNumber, @Description, @MeasurementDate, @Result,\r\n                                        @IsotopeK40, @IsotopeRa226, @IsotopeTh232, @IsotopeRa, @IsotopeCs137)";
						using SqliteCommand sampleCmd = new SqliteCommand(insertSampleQuery, connection, transaction);
						sampleCmd.Parameters.AddWithValue("@CertificateId", newId);
						sampleCmd.Parameters.AddWithValue("@Root", rootNumber++);
						sampleCmd.Parameters.AddWithValue("@SampleNumber", sample.SampleNumber ?? "");
						sampleCmd.Parameters.AddWithValue("@Description", sample.Description ?? "");
						sampleCmd.Parameters.AddWithValue("@MeasurementDate", sample.MeasurementDate.ToString("yyyy-MM-dd"));
						sampleCmd.Parameters.AddWithValue("@Result", sample.Result ?? "");
						sampleCmd.Parameters.AddWithValue("@IsotopeK40", sample.IsotopeK40 ?? "");
						sampleCmd.Parameters.AddWithValue("@IsotopeRa226", sample.IsotopeRa226 ?? "");
						sampleCmd.Parameters.AddWithValue("@IsotopeTh232", sample.IsotopeTh232 ?? "");
						sampleCmd.Parameters.AddWithValue("@IsotopeRa", sample.IsotopeRa ?? "");
						sampleCmd.Parameters.AddWithValue("@IsotopeCs137", sample.IsotopeCs137 ?? "");
						await sampleCmd.ExecuteNonQueryAsync();
					}
				}
				transaction.Commit();
				certificate.CertificateNumber = finalNumber;
				await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "ط؛ظٹط± ظ…ط¹ط±ظˆظپ", "ط¥ظ†ط\u00b4ط§ط،", "ط\u00b4ظ‡ط§ط\u00afط© ط¬ط\u00afظٹط\u00afط© ط±ظ‚ظ… " + finalNumber + " - ط±ظ‚ظ… ط¥ط®ط·ط§ط± " + (certificate.NotificationNumber ?? "ط\u00a8ط\u00afظˆظ†"), newId);
				return newId;
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				LoggerService.LogError("Failed to add certificate async (Transaction)", ex2);
				transaction.Rollback();
				return -1;
			}
		}, "AddCertificateAsync");
	}

	public Task<bool> UpdateCertificateAsync(Certificate certificate)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			Certificate oldCert = await GetCertificateByIdAsync(certificate.Id);
			string changes = ((oldCert != null) ? GetCertificateChanges(oldCert, certificate) : "ط¥ط¶ط§ظپط© ط¬ط\u00afظٹط\u00afط©");
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			using SqliteTransaction transaction = connection.BeginTransaction();
			try
			{
				string query = "UPDATE Certificates SET \r\n                             RecipientName = @RecipientName, \r\n                             CertificateType = @CertificateType, \r\n                             Description = @Description,\r\n                             IssueDate = @IssueDate, \r\n                             ExpiryDate = @ExpiryDate, \r\n                             IssuingAuthority = @IssuingAuthority,\r\n                             AnalysisType = @AnalysisType,\r\n                             Sender = @Sender,\r\n                             Supplier = @Supplier,\r\n                             Origin = @Origin,\r\n                             DeclarationNumber = @DeclarationNumber,\r\n                             PolicyNumber = @PolicyNumber,\r\n                             NotificationNumber = @NotificationNumber,\r\n                             FinancialReceiptNumber = @FinancialReceiptNumber,\r\n                             SpecialistName = @SpecialistName,\r\n                             SectionHeadName = @SectionHeadName,\r\n                             ManagerName = @ManagerName,\r\n                             Notes = @Notes,\r\n                             UpdatedBy = @UpdatedBy,\r\n                             UpdatedByName = @UpdatedByName,\r\n                             UpdatedAt = @UpdatedAt,\r\n                             ReceptionId = @ReceptionId\r\n                             WHERE Id = @Id;";
				using SqliteCommand command = new SqliteCommand(query, connection, transaction);
				command.Parameters.AddWithValue("@Id", certificate.Id);
				command.Parameters.AddWithValue("@RecipientName", certificate.RecipientName);
				command.Parameters.AddWithValue("@CertificateType", certificate.CertificateType);
				command.Parameters.AddWithValue("@Description", certificate.Description ?? "");
				command.Parameters.AddWithValue("@IssueDate", certificate.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
				command.Parameters.AddWithValue("@ExpiryDate", certificate.ExpiryDate.HasValue ? ((IConvertible)certificate.ExpiryDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : ((IConvertible)DBNull.Value));
				command.Parameters.AddWithValue("@IssuingAuthority", certificate.IssuingAuthority ?? "");
				command.Parameters.AddWithValue("@AnalysisType", certificate.AnalysisType ?? "");
				command.Parameters.AddWithValue("@Sender", certificate.Sender ?? "");
				command.Parameters.AddWithValue("@Supplier", certificate.Supplier ?? "");
				command.Parameters.AddWithValue("@Origin", certificate.Origin ?? "");
				command.Parameters.AddWithValue("@DeclarationNumber", certificate.DeclarationNumber ?? "");
				command.Parameters.AddWithValue("@PolicyNumber", certificate.PolicyNumber ?? "");
				command.Parameters.AddWithValue("@NotificationNumber", certificate.NotificationNumber ?? "");
				command.Parameters.AddWithValue("@FinancialReceiptNumber", certificate.FinancialReceiptNumber ?? "");
				command.Parameters.AddWithValue("@SpecialistName", certificate.SpecialistName ?? "");
				command.Parameters.AddWithValue("@SectionHeadName", certificate.SectionHeadName ?? "");
				command.Parameters.AddWithValue("@ManagerName", certificate.ManagerName ?? "");
				command.Parameters.AddWithValue("@Notes", certificate.Notes ?? "");
				command.Parameters.AddWithValue("@UpdatedBy", ((object)certificate.UpdatedBy) ?? DBNull.Value);
				command.Parameters.AddWithValue("@UpdatedByName", ((object)certificate.UpdatedByName) ?? ((object)DBNull.Value));
				command.Parameters.AddWithValue("@UpdatedAt", certificate.UpdatedAt.HasValue ? ((IConvertible)certificate.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")) : ((IConvertible)DBNull.Value));
				command.Parameters.AddWithValue("@ReceptionId", ((object)certificate.ReceptionId) ?? DBNull.Value);
				await command.ExecuteNonQueryAsync();
				if (certificate.Samples != null)
				{
					SqliteCommand deleteCmd = new SqliteCommand("DELETE FROM Samples WHERE CertificateId = @CertificateId", connection, transaction);
					deleteCmd.Parameters.AddWithValue("@CertificateId", certificate.Id);
					await deleteCmd.ExecuteNonQueryAsync();
					int rootNumber = 1;
					foreach (Sample sample in certificate.Samples)
					{
						string insertSampleQuery = "INSERT INTO Samples \r\n                                (CertificateId, Root, SampleNumber, Description, MeasurementDate, Result,\r\n                                 IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137)\r\n                                VALUES (@CertificateId, @Root, @SampleNumber, @Description, @MeasurementDate, @Result,\r\n                                        @IsotopeK40, @IsotopeRa226, @IsotopeTh232, @IsotopeRa, @IsotopeCs137)";
						using SqliteCommand sampleCmd = new SqliteCommand(insertSampleQuery, connection, transaction);
						sampleCmd.Parameters.AddWithValue("@CertificateId", certificate.Id);
						sampleCmd.Parameters.AddWithValue("@Root", rootNumber++);
						sampleCmd.Parameters.AddWithValue("@SampleNumber", sample.SampleNumber ?? "");
						sampleCmd.Parameters.AddWithValue("@Description", sample.Description ?? "");
						sampleCmd.Parameters.AddWithValue("@MeasurementDate", sample.MeasurementDate.ToString("yyyy-MM-dd"));
						sampleCmd.Parameters.AddWithValue("@Result", sample.Result ?? "");
						sampleCmd.Parameters.AddWithValue("@IsotopeK40", sample.IsotopeK40 ?? "");
						sampleCmd.Parameters.AddWithValue("@IsotopeRa226", sample.IsotopeRa226 ?? "");
						sampleCmd.Parameters.AddWithValue("@IsotopeTh232", sample.IsotopeTh232 ?? "");
						sampleCmd.Parameters.AddWithValue("@IsotopeRa", sample.IsotopeRa ?? "");
						sampleCmd.Parameters.AddWithValue("@IsotopeCs137", sample.IsotopeCs137 ?? "");
						await sampleCmd.ExecuteNonQueryAsync();
					}
				}
				transaction.Commit();
				await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "ط؛ظٹط± ظ…ط¹ط±ظˆظپ", "طھط¹ط\u00afظٹظ„", changes + " (ط±ظ‚ظ… ط§ظ„ط\u00b4ظ‡ط§ط\u00afط©: " + certificate.CertificateNumber + ")", certificate.Id);
				return true;
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				LoggerService.LogError("Failed to update certificate async (Transaction)", ex2);
				transaction.Rollback();
				return false;
			}
		}, "UpdateCertificateAsync");
	}

	public Task<List<AuditLog>> GetCertificateHistoryAsync(int certificateId)
	{
		return _db.GetLogsByReferenceIdAsync(certificateId);
	}

	public string GenerateCertificateNumber()
	{
		return $"CERT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
	}

	public Task<bool> AddReferralLetterAsync(ReferralLetter letter)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "INSERT INTO ReferralLetters (SenderName, CertificateCount, SampleCount, OutputPath, StartDate, EndDate, IncludedColumns)\n                              VALUES (@SenderName, @CertificateCount, @SampleCount, @OutputPath, @StartDate, @EndDate, @IncludedColumns);";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@SenderName", letter.SenderName);
			command.Parameters.AddWithValue("@CertificateCount", letter.CertificateCount);
			command.Parameters.AddWithValue("@SampleCount", letter.SampleCount);
			command.Parameters.AddWithValue("@OutputPath", letter.OutputPath);
			command.Parameters.AddWithValue("@StartDate", letter.StartDate.ToString("yyyy-MM-dd"));
			command.Parameters.AddWithValue("@EndDate", letter.EndDate.ToString("yyyy-MM-dd"));
			command.Parameters.AddWithValue("@IncludedColumns", letter.IncludedColumns);
			return await command.ExecuteNonQueryAsync() > 0;
		}, "AddReferralLetterAsync");
	}

	public Task LogReferralLetterGenerationAsync(int? userId, string userName, string senderName, int certificateCount)
	{
		return _db.LogActionAsync(userId, userName, "إصدار رسالة إحالة", $"تم توليد رسالة إحالة موجهة إلى '{senderName}' تحتوي على {certificateCount} شهادة.");
	}

	public Task<List<ReferralLetter>> GetReferralLettersAsync()
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<ReferralLetter> letters = new List<ReferralLetter>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT Id, GeneratedAt, SenderName, CertificateCount, SampleCount, OutputPath, StartDate, EndDate, IncludedColumns FROM ReferralLetters ORDER BY GeneratedAt DESC;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				letters.Add(new ReferralLetter
				{
					Id = reader.GetInt32(0),
					GeneratedAt = DateTime.Parse(reader.GetString(1)),
					SenderName = reader.GetString(2),
					CertificateCount = reader.GetInt32(3),
					SampleCount = reader.GetInt32(4),
					OutputPath = reader.GetString(5),
					StartDate = DateTime.Parse(reader.GetString(6)),
					EndDate = DateTime.Parse(reader.GetString(7)),
					IncludedColumns = (reader.IsDBNull(8) ? "" : reader.GetString(8))
				});
			}
			return letters;
		}, "GetReferralLettersAsync");
	}

	public Task<List<Certificate>> GetExpiringCertificatesAsync(int daysAhead)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<Certificate> certificates = new List<Certificate>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string cutoffDate = DateTime.Now.AddDays(daysAhead).ToString("yyyy-MM-dd");
			string today = DateTime.Now.ToString("yyyy-MM-dd");
			string query = "SELECT Id, CertificateNumber, RecipientName, ExpiryDate \n                              FROM Certificates \n                              WHERE ExpiryDate IS NOT NULL \n                              AND date(ExpiryDate) >= date(@Today) \n                              AND date(ExpiryDate) <= date(@Cutoff)\n                              ORDER BY ExpiryDate ASC;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Today", today);
			command.Parameters.AddWithValue("@Cutoff", cutoffDate);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				certificates.Add(new Certificate
				{
					Id = reader.GetInt32(0),
					CertificateNumber = reader.GetString(1),
					RecipientName = reader.GetString(2),
					ExpiryDate = DateTime.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture)
				});
			}
			return certificates;
		}, "GetExpiringCertificatesAsync");
	}

	public Task<List<Sample>> GetUnusualSamplesAsync()
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<Sample> samples = new List<Sample>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string[] unusualKeywords = new string[7] { "%مرفوض%", "%غير صالح%", "%فشل%", "%unfit%", "%rejected%", "%failed%", "%غير مطابق%" };
			string query = "SELECT Id, CertificateId, SampleNumber, Result FROM Samples WHERE 1=0";
			string[] array = unusualKeywords;
			foreach (string k in array)
			{
				query = query + " OR Result LIKE '" + k + "'";
			}
			query += " ORDER BY Id DESC LIMIT 20;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				samples.Add(new Sample
				{
					Id = reader.GetInt32(0),
					CertificateId = reader.GetInt32(1),
					SampleNumber = (reader.IsDBNull(2) ? "" : reader.GetString(2)),
					Result = (reader.IsDBNull(3) ? "" : reader.GetString(3))
				});
			}
			return samples;
		}, "GetUnusualSamplesAsync");
	}

	public Task<(int total, int consumable, int environmental, int totalSamples)> GetCertificateCountsByTypeAsync()
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT \n                              COUNT(*) as Total,\n                              SUM(CASE WHEN CertificateType LIKE '%استهلاكية%' THEN 1 ELSE 0 END) as Consumable,\n                              SUM(CASE WHEN CertificateType LIKE '%بيئية%' THEN 1 ELSE 0 END) as Environmental,\n                              (SELECT COUNT(*) FROM Samples) as TotalSamples\n                              FROM Certificates;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			if (await reader.ReadAsync())
			{
				return ((!reader.IsDBNull(0)) ? reader.GetInt32(0) : 0, (!reader.IsDBNull(1)) ? reader.GetInt32(1) : 0, (!reader.IsDBNull(2)) ? reader.GetInt32(2) : 0, (!reader.IsDBNull(3)) ? reader.GetInt32(3) : 0);
			}
			return (0, 0, 0, 0);
		}, "GetCertificateCountsByTypeAsync");
	}

	public Task<bool> AddSampleAsync(Sample sample)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "INSERT INTO Samples (CertificateId, Root, SampleNumber, Description, MeasurementDate, Result, \n                                                   IsotopeK40, IsotopeRa226, IsotopeTh232, IsotopeRa, IsotopeCs137)\n                              VALUES (@CertificateId, @Root, @SampleNumber, @Description, @MeasurementDate, @Result,\n                                      @IsotopeK40, @IsotopeRa226, @IsotopeTh232, @IsotopeRa, @IsotopeCs137);";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@CertificateId", sample.CertificateId);
			command.Parameters.AddWithValue("@Root", sample.Root);
			command.Parameters.AddWithValue("@SampleNumber", sample.SampleNumber ?? "");
			command.Parameters.AddWithValue("@Description", sample.Description ?? "");
			command.Parameters.AddWithValue("@MeasurementDate", sample.MeasurementDate.ToString("yyyy-MM-dd"));
			command.Parameters.AddWithValue("@Result", sample.Result ?? "");
			command.Parameters.AddWithValue("@IsotopeK40", sample.IsotopeK40 ?? "");
			command.Parameters.AddWithValue("@IsotopeRa226", sample.IsotopeRa226 ?? "");
			command.Parameters.AddWithValue("@IsotopeTh232", sample.IsotopeTh232 ?? "");
			command.Parameters.AddWithValue("@IsotopeRa", sample.IsotopeRa ?? "");
			command.Parameters.AddWithValue("@IsotopeCs137", sample.IsotopeCs137 ?? "");
			return await command.ExecuteNonQueryAsync() > 0;
		}, "AddSampleAsync");
	}

	public Task<List<Sample>> GetSamplesByCertificateIdAsync(int certificateId)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<Sample> samples = new List<Sample>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT s.Id, s.CertificateId, s.Root, s.SampleNumber, s.Description, s.MeasurementDate, s.Result,\n                                     s.IsotopeK40, s.IsotopeRa226, s.IsotopeTh232, s.IsotopeRa, s.IsotopeCs137 \n                              FROM Samples s\n                              INNER JOIN Certificates c ON s.CertificateId = c.Id\n                              WHERE s.CertificateId = @CertificateId \n                              ORDER BY s.Root;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@CertificateId", certificateId);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				samples.Add(new Sample
				{
					Id = reader.GetInt32(0),
					CertificateId = reader.GetInt32(1),
					Root = reader.GetInt32(2),
					SampleNumber = (reader.IsDBNull(3) ? "" : reader.GetString(3)),
					Description = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
					MeasurementDate = DateTime.Parse(reader.GetString(5)),
					Result = (reader.IsDBNull(6) ? "" : reader.GetString(6)),
					IsotopeK40 = (reader.IsDBNull(7) ? "" : reader.GetString(7)),
					IsotopeRa226 = (reader.IsDBNull(8) ? "" : reader.GetString(8)),
					IsotopeTh232 = (reader.IsDBNull(9) ? "" : reader.GetString(9)),
					IsotopeRa = (reader.IsDBNull(10) ? "" : reader.GetString(10)),
					IsotopeCs137 = (reader.IsDBNull(11) ? "" : reader.GetString(11))
				});
			}
			return samples;
		}, "GetSamplesByCertificateIdAsync");
	}

	public Task<List<Sample>> GetSamplesByDateRangeAsync(DateTime startDate, DateTime endDate)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<Sample> samples = new List<Sample>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT s.Id, s.CertificateId, s.Root, s.SampleNumber, s.Description, s.MeasurementDate, s.Result,\n                                     s.IsotopeK40, s.IsotopeRa226, s.IsotopeTh232, s.IsotopeRa, s.IsotopeCs137 \n                              FROM Samples s\n                              INNER JOIN Certificates c ON s.CertificateId = c.Id\n                              WHERE date(c.IssueDate) >= date(@StartDate) \n                              AND date(c.IssueDate) <= date(@EndDate)\n                              ORDER BY c.IssueDate ASC, s.Root ASC;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
			command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				samples.Add(new Sample
				{
					Id = reader.GetInt32(0),
					CertificateId = reader.GetInt32(1),
					Root = reader.GetInt32(2),
					SampleNumber = (reader.IsDBNull(3) ? "" : reader.GetString(3)),
					Description = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
					MeasurementDate = DateTime.Parse(reader.GetString(5)),
					Result = (reader.IsDBNull(6) ? "" : reader.GetString(6)),
					IsotopeK40 = (reader.IsDBNull(7) ? "" : reader.GetString(7)),
					IsotopeRa226 = (reader.IsDBNull(8) ? "" : reader.GetString(8)),
					IsotopeTh232 = (reader.IsDBNull(9) ? "" : reader.GetString(9)),
					IsotopeRa = (reader.IsDBNull(10) ? "" : reader.GetString(10)),
					IsotopeCs137 = (reader.IsDBNull(11) ? "" : reader.GetString(11))
				});
			}
			return samples;
		}, "GetSamplesByDateRangeAsync");
	}

	public Task<bool> DeleteSamplesByCertificateIdAsync(int certificateId)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "DELETE FROM Samples WHERE CertificateId = @CertificateId;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@CertificateId", certificateId);
			return await command.ExecuteNonQueryAsync() >= 0;
		}, "DeleteSamplesByCertificateIdAsync");
	}

	public Task<int> GetSearchCertificatesCountAsync(string searchTerm, string searchCriteria, DateTime? startDate = null, DateTime? endDate = null, bool showDeleted = false)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT COUNT(*) FROM Certificates c LEFT JOIN Users u ON c.CreatedBy = u.Id WHERE 1=1";
			(string, Dictionary<string, object>) tuple = BuildSearchFilter(searchTerm, searchCriteria, startDate, endDate);
			string sqlFilter = tuple.Item1;
			Dictionary<string, object> parameters = tuple.Item2;
			query += sqlFilter;
			using SqliteCommand command = new SqliteCommand(query, connection);
			foreach (KeyValuePair<string, object> param in parameters)
			{
				command.Parameters.AddWithValue(param.Key, param.Value);
			}
			return Convert.ToInt32(await command.ExecuteScalarAsync());
		}, "GetSearchCertificatesCountAsync");
	}

	private (string sql, Dictionary<string, object> parameters) BuildSearchFilter(string searchTerm, string searchCriteria, DateTime? startDate, DateTime? endDate)
	{
		string text = "";
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		if (startDate.HasValue)
		{
			text += " AND date(c.IssueDate) >= date(@StartDate)";
			dictionary.Add("@StartDate", startDate.Value.ToString("yyyy-MM-dd"));
		}
		if (endDate.HasValue)
		{
			text += " AND date(c.IssueDate) <= date(@EndDate)";
			dictionary.Add("@EndDate", endDate.Value.ToString("yyyy-MM-dd"));
		}
		if (searchCriteria != "التاريخ" && !string.IsNullOrWhiteSpace(searchTerm))
		{
			string text2 = searchTerm.Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا")
				.Replace("ة", "ه")
				.Replace("ى", "ي");
			dictionary.Add("@SearchTerm", "%" + text2 + "%");
			text = searchCriteria switch
			{
				"رقم العينة" => text + " AND EXISTS (SELECT 1 FROM Samples s WHERE s.CertificateId = c.Id AND " + SqlNorm("s.SampleNumber") + " LIKE @SearchTerm)", 
				"رقم الشهادة" => text + " AND " + SqlNorm("c.CertificateNumber") + " LIKE @SearchTerm", 
				"رقم الاخطار" => text + " AND " + SqlNorm("c.NotificationNumber") + " LIKE @SearchTerm", 
				"رقم الاقرار الجمركى" => text + " AND " + SqlNorm("c.DeclarationNumber") + " LIKE @SearchTerm", 
				"الجهة المرسلة" => text + " AND " + SqlNorm("c.Sender") + " LIKE @SearchTerm", 
				"المورد" => text + " AND " + SqlNorm("c.Supplier") + " LIKE @SearchTerm", 
				"رقم الايصال المالى" => text + " AND " + SqlNorm("c.FinancialReceiptNumber") + " LIKE @SearchTerm", 
				"رقم البوليصة" => text + " AND " + SqlNorm("c.PolicyNumber") + " LIKE @SearchTerm", 
				"اسم المستخدم" => text + $" AND ({SqlNorm("u.Username")} LIKE @SearchTerm OR {SqlNorm("u.FullName")} LIKE @SearchTerm)", 
				_ => text + $" AND (\n                            {SqlNorm("c.RecipientName")} LIKE @SearchTerm OR \n                            {SqlNorm("c.CertificateNumber")} LIKE @SearchTerm OR \n                            {SqlNorm("c.Supplier")} LIKE @SearchTerm OR \n                            {SqlNorm("c.NotificationNumber")} LIKE @SearchTerm OR \n                            {SqlNorm("c.DeclarationNumber")} LIKE @SearchTerm OR \n                            {SqlNorm("c.Sender")} LIKE @SearchTerm OR \n                            {SqlNorm("c.FinancialReceiptNumber")} LIKE @SearchTerm OR \n                            {SqlNorm("c.PolicyNumber")} LIKE @SearchTerm OR\n                            EXISTS (SELECT 1 FROM Samples s WHERE s.CertificateId = c.Id AND {SqlNorm("s.SampleNumber")} LIKE @SearchTerm))", 
			};
		}
		return (sql: text, parameters: dictionary);
		static string SqlNorm(string col)
		{
			return "REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(" + col + ", 'أ', 'ا'), 'إ', 'ا'), 'آ', 'ا'), 'ة', 'ه'), 'ى', 'ي')";
		}
	}

	public Task<List<Certificate>> SearchCertificatesAsync(string searchTerm, string searchCriteria, int pageNumber, int pageSize, DateTime? startDate = null, DateTime? endDate = null, bool showDeleted = false)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<Certificate> certificates = new List<Certificate>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			int offset = (pageNumber - 1) * pageSize;
			string query = "SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, \n                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, \n                              c.CreatedBy, u.FullName, datetime(c.CreatedAt, 'localtime'),\n                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,\n                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,\n                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,\n                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt,\n                              (SELECT COUNT(*) FROM Samples s WHERE s.CertificateId = c.Id) AS SampleCount,\n                              c.ReceptionId\n                              FROM Certificates c \n                              LEFT JOIN Users u ON c.CreatedBy = u.Id\n                              WHERE 1=1";
			(string, Dictionary<string, object>) tuple = BuildSearchFilter(searchTerm, searchCriteria, startDate, endDate);
			string sqlFilter = tuple.Item1;
			Dictionary<string, object> parameters = tuple.Item2;
			query += sqlFilter;
			query += " ORDER BY c.CreatedAt DESC LIMIT @Limit OFFSET @Offset;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Limit", pageSize);
			command.Parameters.AddWithValue("@Offset", offset);
			foreach (KeyValuePair<string, object> param in parameters)
			{
				command.Parameters.AddWithValue(param.Key, param.Value);
			}
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				certificates.Add(new Certificate
				{
					Id = reader.GetInt32(0),
					CertificateNumber = reader.GetString(1),
					RecipientName = reader.GetString(2),
					CertificateType = reader.GetString(3),
					Description = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
					IssueDate = DateTime.ParseExact(reader.GetString(5), "yyyy-MM-dd", CultureInfo.InvariantCulture),
					ExpiryDate = (reader.IsDBNull(6) ? ((DateTime?)null) : new DateTime?(DateTime.ParseExact(reader.GetString(6), "yyyy-MM-dd", CultureInfo.InvariantCulture))),
					IssuingAuthority = (reader.IsDBNull(7) ? "" : reader.GetString(7)),
					CreatedBy = reader.GetInt32(8),
					CreatedByName = (reader.IsDBNull(9) ? "" : reader.GetString(9)),
					CreatedAt = DateTime.ParseExact(reader.GetString(10), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
					AnalysisType = (reader.IsDBNull(11) ? "" : reader.GetString(11)),
					Sender = (reader.IsDBNull(12) ? "" : reader.GetString(12)),
					Supplier = (reader.IsDBNull(13) ? "" : reader.GetString(13)),
					Origin = (reader.IsDBNull(14) ? "" : reader.GetString(14)),
					DeclarationNumber = (reader.IsDBNull(15) ? "" : reader.GetString(15)),
					PolicyNumber = (reader.IsDBNull(16) ? "" : reader.GetString(16)),
					NotificationNumber = (reader.IsDBNull(17) ? "" : reader.GetString(17)),
					FinancialReceiptNumber = (reader.IsDBNull(18) ? "" : reader.GetString(18)),
					SpecialistName = (reader.IsDBNull(19) ? "" : reader.GetString(19)),
					SectionHeadName = (reader.IsDBNull(20) ? "" : reader.GetString(20)),
					ManagerName = (reader.IsDBNull(21) ? "" : reader.GetString(21)),
					Notes = (reader.IsDBNull(22) ? "" : reader.GetString(22)),
					UpdatedBy = (reader.IsDBNull(23) ? ((int?)null) : new int?(reader.GetInt32(23))),
					UpdatedByName = (reader.IsDBNull(24) ? "" : reader.GetString(24)),
					UpdatedAt = (reader.IsDBNull(25) ? ((DateTime?)null) : new DateTime?(DateTime.ParseExact(reader.GetString(25), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))),
					SampleCount = reader.GetInt32(26),
					ReceptionId = (reader.IsDBNull(27) ? ((int?)null) : new int?(reader.GetInt32(27)))
				});
			}
			return certificates;
		}, "SearchCertificatesAsync");
	}

	public Task<List<Certificate>> GetCertificatesBySenderAndDateAsync(string sender, DateTime startDate, DateTime endDate)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<Certificate> certificates = new List<Certificate>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT c.Id, c.CertificateNumber, c.RecipientName, c.CertificateType, \n                              c.Description, c.IssueDate, c.ExpiryDate, c.IssuingAuthority, \n                              c.CreatedBy, COALESCE(u.FullName, ''), datetime(c.CreatedAt, 'localtime'),\n                              c.AnalysisType, c.Sender, c.Supplier, c.Origin, c.DeclarationNumber,\n                              c.PolicyNumber, c.NotificationNumber, c.FinancialReceiptNumber,\n                              c.SpecialistName, c.SectionHeadName, c.ManagerName, c.Notes,\n                              c.UpdatedBy, c.UpdatedByName, c.UpdatedAt,\n                              (SELECT COUNT(*) FROM Samples s WHERE s.CertificateId = c.Id) AS SampleCount\n                              FROM Certificates c \n                              LEFT JOIN Users u ON c.CreatedBy = u.Id\n                              WHERE c.Sender LIKE @Sender\n                              AND date(c.IssueDate) >= date(@StartDate)\n                              AND date(c.IssueDate) <= date(@EndDate)\n                              ORDER BY c.IssueDate ASC;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Sender", "%" + sender + "%");
			command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
			command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				certificates.Add(MapCertificateFromReader(reader));
			}
			return certificates;
		}, "GetCertificatesBySenderAndDateAsync");
	}

	public Task<List<string>> GetDistinctFieldValuesAsync(string columnName)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<string> values = new List<string>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			HashSet<string> allowedColumns = new HashSet<string> { "RecipientName", "Sender", "Supplier", "Origin", "AnalysisType", "SpecialistName", "SectionHeadName", "ManagerName", "Result" };
			if (!allowedColumns.Contains(columnName))
			{
				return values;
			}
			string query = $"SELECT DISTINCT {columnName} FROM Certificates WHERE {columnName} IS NOT NULL AND {columnName} != '' ORDER BY {columnName} ASC";
			if (columnName == "Result")
			{
				query = $"SELECT DISTINCT {columnName} FROM Samples WHERE {columnName} IS NOT NULL AND {columnName} != '' ORDER BY {columnName} ASC";
			}
			using SqliteCommand command = new SqliteCommand(query, connection);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				values.Add(reader.GetString(0));
			}
			return values;
		}, "GetDistinctFieldValuesAsync");
	}

	public Task<bool> IsFinancialReceiptDuplicateAsync(string receiptNumber, int? excludeId = null)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			if (string.IsNullOrWhiteSpace(receiptNumber))
			{
				return false;
			}
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT COUNT(*) FROM Certificates WHERE FinancialReceiptNumber = @ReceiptNumber";
			if (excludeId.HasValue)
			{
				query += " AND Id != @ExcludeId";
			}
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@ReceiptNumber", receiptNumber.Trim());
			if (excludeId.HasValue)
			{
				command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);
			}
			int count = Convert.ToInt32(await command.ExecuteScalarAsync());
			return count > 0;
		}, "IsFinancialReceiptDuplicateAsync");
	}

	public Task<int> GetTotalCertificatesCountAsync(bool showDeleted = false, int? year = null)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = (showDeleted ? "SELECT COUNT(*) FROM Certificates WHERE (@Year IS NULL OR strftime('%Y', IssueDate) = @Year)" : "SELECT COUNT(*) FROM Certificates WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND (@Year IS NULL OR strftime('%Y', IssueDate) = @Year)");
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Year", ((object)year?.ToString()) ?? ((object)DBNull.Value));
			return Convert.ToInt32(await command.ExecuteScalarAsync());
		}, "GetTotalCertificatesCountAsync");
	}

	public Task<int> GetCertificatesCountByDateAsync(DateTime date)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			SqliteCommand command = new SqliteCommand("SELECT COUNT(*) FROM Certificates WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND DATE(IssueDate) = DATE(@Date)", connection);
			command.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
			return Convert.ToInt32(await command.ExecuteScalarAsync());
		}, "GetCertificatesCountByDateAsync");
	}

	public Task<Dictionary<int, int>> GetMonthlyStatisticsAsync(int year)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			Dictionary<int, int> stats = new Dictionary<int, int>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			for (int i = 1; i <= 12; i++)
			{
				stats[i] = 0;
			}
			string query = "SELECT strftime('%m', IssueDate) as Month, COUNT(*) as Count \n                              FROM Certificates \n                              WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND strftime('%Y', IssueDate) = @Year \n                              GROUP BY Month";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Year", year.ToString());
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				int month = Convert.ToInt32(reader.GetString(0));
				int count = reader.GetInt32(1);
				if (stats.ContainsKey(month))
				{
					stats[month] = count;
				}
			}
			return stats;
		}, "GetMonthlyStatisticsAsync");
	}

	public Task<Dictionary<int, int>> GetMonthlyStatisticsByTypeAsync(int year, string type)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			Dictionary<int, int> stats = new Dictionary<int, int>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			for (int i = 1; i <= 12; i++)
			{
				stats[i] = 0;
			}
			string query = "SELECT strftime('%m', IssueDate) as Month, COUNT(*) as Count \n                              FROM Certificates \n                              WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND strftime('%Y', IssueDate) = @Year \n                              AND CertificateType LIKE @Type\n                              GROUP BY Month";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Year", year.ToString());
			command.Parameters.AddWithValue("@Type", "%" + type + "%");
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				int month = Convert.ToInt32(reader.GetString(0));
				int count = reader.GetInt32(1);
				if (stats.ContainsKey(month))
				{
					stats[month] = count;
				}
			}
			return stats;
		}, "GetMonthlyStatisticsByTypeAsync");
	}

	public Task<int> GetCertificatesCountByTypeAsync(string type, int? year = null)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT COUNT(*) FROM Certificates WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND CertificateType LIKE @Type AND (@Year IS NULL OR strftime('%Y', IssueDate) = @Year)";
			SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Type", "%" + type + "%");
			command.Parameters.AddWithValue("@Year", ((object)year?.ToString()) ?? ((object)DBNull.Value));
			return Convert.ToInt32(await command.ExecuteScalarAsync());
		}, "GetCertificatesCountByTypeAsync");
	}

	public Task<int> GetTotalSamplesCountAsync(int? year = null)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT COUNT(*) FROM Samples s INNER JOIN Certificates c ON s.CertificateId = c.Id WHERE (c.IsDeleted IS NULL OR c.IsDeleted = 0) AND (@Year IS NULL OR strftime('%Y', c.IssueDate) = @Year)";
			SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Year", ((object)year?.ToString()) ?? ((object)DBNull.Value));
			return Convert.ToInt32(await command.ExecuteScalarAsync());
		}, "GetTotalSamplesCountAsync");
	}

	public Task<int> GetSamplesCountByDateAsync(DateTime date)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			SqliteCommand command = new SqliteCommand("SELECT COUNT(*) FROM Samples s \n                    INNER JOIN Certificates c ON s.CertificateId = c.Id \n                    WHERE (c.IsDeleted IS NULL OR c.IsDeleted = 0) AND DATE(c.IssueDate) = DATE(@Date)", connection);
			command.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
			return Convert.ToInt32(await command.ExecuteScalarAsync());
		}, "GetSamplesCountByDateAsync");
	}

	public Task<int> GetSamplesCountByTypeAsync(string type, int? year = null)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT COUNT(*) FROM Samples s \n                    INNER JOIN Certificates c ON s.CertificateId = c.Id \n                    WHERE (c.IsDeleted IS NULL OR c.IsDeleted = 0) AND c.CertificateType LIKE @Type AND (@Year IS NULL OR strftime('%Y', c.IssueDate) = @Year)";
			SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Type", "%" + type + "%");
			command.Parameters.AddWithValue("@Year", ((object)year?.ToString()) ?? ((object)DBNull.Value));
			return Convert.ToInt32(await command.ExecuteScalarAsync());
		}, "GetSamplesCountByTypeAsync");
	}

	public Task<Dictionary<int, int>> GetMonthlySamplesStatisticsByTypeAsync(int year, string type)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			Dictionary<int, int> stats = new Dictionary<int, int>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			for (int i = 1; i <= 12; i++)
			{
				stats[i] = 0;
			}
			string query = "SELECT strftime('%m', c.IssueDate) as Month, COUNT(*) as Count \n                              FROM Samples s\n                              INNER JOIN Certificates c ON s.CertificateId = c.Id\n                              WHERE strftime('%Y', c.IssueDate) = @Year \n                              AND c.CertificateType LIKE @Type\n                              AND c.IsDeleted = 0\n                              GROUP BY Month";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Year", year.ToString());
			command.Parameters.AddWithValue("@Type", "%" + type + "%");
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				int month = Convert.ToInt32(reader.GetString(0));
				int count = reader.GetInt32(1);
				if (stats.ContainsKey(month))
				{
					stats[month] = count;
				}
			}
			return stats;
		}, "GetMonthlySamplesStatisticsByTypeAsync");
	}
}
