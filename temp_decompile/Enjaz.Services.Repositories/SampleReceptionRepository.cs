using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Enjaz.Models;
using Microsoft.Data.Sqlite;

namespace Enjaz.Services.Repositories;

public class SampleReceptionRepository
{
	private readonly DatabaseService _db;

	private readonly UserService _userService;

	public SampleReceptionRepository(DatabaseService db, UserService userService)
	{
		_db = db;
		_userService = userService;
	}

	public Task<List<string>> GetDistinctSendersAsync()
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<string> senders = new List<string>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT DISTINCT Sender FROM SampleReceptions WHERE Sender IS NOT NULL AND Sender != ''";
			using SqliteCommand command = new SqliteCommand(query, connection);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				senders.Add(reader.GetString(0));
			}
			return senders;
		}, "GetDistinctSendersAsync");
	}

	public Task<int> AddSampleReceptionAsync(SampleReception reception)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			using SqliteTransaction transaction = connection.BeginTransaction();
			try
			{
				string query = "INSERT INTO SampleReceptions \n                                 (AnalysisRequestNumber, NotificationNumber, DeclarationNumber, Supplier, Sender, Origin, \n                                  PolicyNumber, FinancialReceiptNumber, CertificateType, Date, Status, \n                                  CreatedBy, CreatedByName)\n                                 VALUES (@AnalysisRequestNumber, @NotificationNumber, @DeclarationNumber, @Supplier, @Sender, @Origin,\n                                         @PolicyNumber, @FinancialReceiptNumber, @CertificateType, @Date, @Status,\n                                         @CreatedBy, @CreatedByName);\n                                 SELECT last_insert_rowid();";
				using SqliteCommand command = new SqliteCommand(query, connection, transaction);
				command.Parameters.AddWithValue("@AnalysisRequestNumber", reception.AnalysisRequestNumber);
				command.Parameters.AddWithValue("@NotificationNumber", reception.NotificationNumber ?? "");
				command.Parameters.AddWithValue("@DeclarationNumber", reception.DeclarationNumber ?? "");
				command.Parameters.AddWithValue("@Supplier", reception.Supplier ?? "");
				command.Parameters.AddWithValue("@Sender", reception.Sender ?? "");
				command.Parameters.AddWithValue("@Origin", reception.Origin ?? "");
				command.Parameters.AddWithValue("@PolicyNumber", reception.PolicyNumber ?? "");
				command.Parameters.AddWithValue("@FinancialReceiptNumber", reception.FinancialReceiptNumber ?? "");
				command.Parameters.AddWithValue("@CertificateType", reception.CertificateType);
				command.Parameters.AddWithValue("@Date", reception.Date.ToString("yyyy-MM-dd HH:mm:ss"));
				command.Parameters.AddWithValue("@Status", reception.Status);
				command.Parameters.AddWithValue("@CreatedBy", _userService.CurrentUser?.Id ?? 1);
				command.Parameters.AddWithValue("@CreatedByName", _userService.CurrentUser?.FullName ?? "النظام");
				object idObj = await command.ExecuteScalarAsync();
				if (idObj == null)
				{
					throw new Exception("Failed to retrieve ID");
				}
				int newId = Convert.ToInt32(idObj);
				if (reception.Samples != null)
				{
					foreach (ReceptionSample sample in reception.Samples)
					{
						string insertSampleQuery = "INSERT INTO ReceptionSamples \n                                (ReceptionId, SampleNumber, Description)\n                                VALUES (@ReceptionId, @SampleNumber, @Description)";
						using SqliteCommand sampleCmd = new SqliteCommand(insertSampleQuery, connection, transaction);
						sampleCmd.Parameters.AddWithValue("@ReceptionId", newId);
						sampleCmd.Parameters.AddWithValue("@SampleNumber", sample.SampleNumber ?? "");
						sampleCmd.Parameters.AddWithValue("@Description", sample.Description ?? "");
						await sampleCmd.ExecuteNonQueryAsync();
					}
				}
				transaction.Commit();
				await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "إنشاء استلام", "إيصال استلام جديد رقم " + reception.AnalysisRequestNumber, newId);
				return newId;
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				LoggerService.LogError("Failed to add sample reception async (Transaction)", ex2);
				transaction.Rollback();
				return -1;
			}
		}, "AddSampleReceptionAsync");
	}

	public Task<bool> UpdateSampleReceptionAsync(SampleReception reception)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			using SqliteTransaction transaction = connection.BeginTransaction();
			try
			{
				string query = "UPDATE SampleReceptions SET \n                             AnalysisRequestNumber = @AnalysisRequestNumber,\n                             NotificationNumber = @NotificationNumber,\n                             DeclarationNumber = @DeclarationNumber,\n                             Supplier = @Supplier,\n                             Sender = @Sender,\n                             Origin = @Origin,\n                             PolicyNumber = @PolicyNumber,\n                             FinancialReceiptNumber = @FinancialReceiptNumber,\n                             CertificateType = @CertificateType,\n                             Date = @Date,\n                             Status = @Status,\n                             UpdatedBy = @UpdatedBy,\n                             UpdatedByName = @UpdatedByName,\n                             UpdatedAt = CURRENT_TIMESTAMP\n                             WHERE Id = @Id;";
				using SqliteCommand command = new SqliteCommand(query, connection, transaction);
				command.Parameters.AddWithValue("@Id", reception.Id);
				command.Parameters.AddWithValue("@AnalysisRequestNumber", reception.AnalysisRequestNumber);
				command.Parameters.AddWithValue("@NotificationNumber", reception.NotificationNumber ?? "");
				command.Parameters.AddWithValue("@DeclarationNumber", reception.DeclarationNumber ?? "");
				command.Parameters.AddWithValue("@Supplier", reception.Supplier ?? "");
				command.Parameters.AddWithValue("@Sender", reception.Sender ?? "");
				command.Parameters.AddWithValue("@Origin", reception.Origin ?? "");
				command.Parameters.AddWithValue("@PolicyNumber", reception.PolicyNumber ?? "");
				command.Parameters.AddWithValue("@FinancialReceiptNumber", reception.FinancialReceiptNumber ?? "");
				command.Parameters.AddWithValue("@CertificateType", reception.CertificateType);
				command.Parameters.AddWithValue("@Date", reception.Date.ToString("yyyy-MM-dd HH:mm:ss"));
				command.Parameters.AddWithValue("@Status", reception.Status);
				command.Parameters.AddWithValue("@UpdatedBy", _userService.CurrentUser?.Id ?? 1);
				command.Parameters.AddWithValue("@UpdatedByName", _userService.CurrentUser?.FullName ?? "النظام");
				await command.ExecuteNonQueryAsync();
				if (reception.Samples != null)
				{
					SqliteCommand deleteCmd = new SqliteCommand("DELETE FROM ReceptionSamples WHERE ReceptionId = @ReceptionId", connection, transaction);
					deleteCmd.Parameters.AddWithValue("@ReceptionId", reception.Id);
					await deleteCmd.ExecuteNonQueryAsync();
					foreach (ReceptionSample sample in reception.Samples)
					{
						string insertSampleQuery = "INSERT INTO ReceptionSamples \n                                (ReceptionId, SampleNumber, Description)\n                                VALUES (@ReceptionId, @SampleNumber, @Description)";
						using SqliteCommand sampleCmd = new SqliteCommand(insertSampleQuery, connection, transaction);
						sampleCmd.Parameters.AddWithValue("@ReceptionId", reception.Id);
						sampleCmd.Parameters.AddWithValue("@SampleNumber", sample.SampleNumber ?? "");
						sampleCmd.Parameters.AddWithValue("@Description", sample.Description ?? "");
						await sampleCmd.ExecuteNonQueryAsync();
					}
				}
				transaction.Commit();
				await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "تعديل استلام", "تعديل بيانات الاستلام رقم طلب التحليل " + reception.AnalysisRequestNumber, reception.Id);
				return true;
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				LoggerService.LogError("Failed to update sample reception async", ex2);
				transaction.Rollback();
				return false;
			}
		}, "UpdateSampleReceptionAsync");
	}

	public Task<bool> DeleteSampleReceptionAsync(int id)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "DELETE FROM SampleReceptions WHERE Id = @Id;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Id", id);
			if (await command.ExecuteNonQueryAsync() > 0)
			{
				await _db.LogActionAsync(_userService.CurrentUser?.Id, _userService.CurrentUser?.FullName ?? "غير معروف", "حذف استلام", $"حذف استلام برقم معرف {id}", id);
				return true;
			}
			return false;
		}, "DeleteSampleReceptionAsync");
	}

	public Task<List<SampleReception>> GetAllReceptionsAsync()
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<SampleReception> receptions = new List<SampleReception>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, \n                               Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, \n                               CertificateType, Date, Status, CreatedBy, CreatedByName\n                               FROM SampleReceptions \n                               ORDER BY Date DESC LIMIT 200;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				receptions.Add(new SampleReception
				{
					Id = reader.GetInt32(0),
					AnalysisRequestNumber = reader.GetString(1),
					NotificationNumber = (reader.IsDBNull(2) ? "" : reader.GetString(2)),
					DeclarationNumber = (reader.IsDBNull(3) ? "" : reader.GetString(3)),
					Supplier = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
					Sender = (reader.IsDBNull(5) ? "" : reader.GetString(5)),
					Origin = (reader.IsDBNull(6) ? "" : reader.GetString(6)),
					PolicyNumber = (reader.IsDBNull(7) ? "" : reader.GetString(7)),
					FinancialReceiptNumber = (reader.IsDBNull(8) ? "" : reader.GetString(8)),
					CertificateType = reader.GetString(9),
					Date = DateTime.ParseExact(reader.GetString(10), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
					Status = reader.GetString(11),
					CreatedBy = reader.GetInt32(12),
					CreatedByName = (reader.IsDBNull(13) ? "" : reader.GetString(13))
				});
			}
			foreach (SampleReception rec in receptions)
			{
				SampleReception sampleReception = rec;
				sampleReception.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
			}
			return receptions;
		}, "GetAllReceptionsAsync");
	}

	public Task<List<SampleReception>> GetPendingReceptionsAsync()
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<SampleReception> receptions = new List<SampleReception>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, \n                               Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, \n                               CertificateType, Date, Status, CreatedBy, CreatedByName\n                               FROM SampleReceptions \n                               WHERE Status = 'في انتظار إصدار شهادة'\n                               ORDER BY Date DESC;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				receptions.Add(new SampleReception
				{
					Id = reader.GetInt32(0),
					AnalysisRequestNumber = reader.GetString(1),
					NotificationNumber = (reader.IsDBNull(2) ? "" : reader.GetString(2)),
					DeclarationNumber = (reader.IsDBNull(3) ? "" : reader.GetString(3)),
					Supplier = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
					Sender = (reader.IsDBNull(5) ? "" : reader.GetString(5)),
					Origin = (reader.IsDBNull(6) ? "" : reader.GetString(6)),
					PolicyNumber = (reader.IsDBNull(7) ? "" : reader.GetString(7)),
					FinancialReceiptNumber = (reader.IsDBNull(8) ? "" : reader.GetString(8)),
					CertificateType = reader.GetString(9),
					Date = DateTime.ParseExact(reader.GetString(10), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
					Status = reader.GetString(11),
					CreatedBy = reader.GetInt32(12),
					CreatedByName = (reader.IsDBNull(13) ? "" : reader.GetString(13))
				});
			}
			foreach (SampleReception rec in receptions)
			{
				SampleReception sampleReception = rec;
				sampleReception.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
			}
			return receptions;
		}, "GetPendingReceptionsAsync");
	}

	public Task<List<SampleReception>> GetDelayedPendingReceptionsAsync(int daysDelayed)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<SampleReception> receptions = new List<SampleReception>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, \n                               Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, \n                               CertificateType, Date, Status, CreatedBy, CreatedByName\n                               FROM SampleReceptions \n                               WHERE Status = 'في انتظار إصدار شهادة' AND Date <= @ThresholdDate\n                               ORDER BY Date DESC;";
			string thresholdDateString = DateTime.Now.AddDays(-daysDelayed).ToString("yyyy-MM-dd HH:mm:ss");
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@ThresholdDate", thresholdDateString);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				receptions.Add(new SampleReception
				{
					Id = reader.GetInt32(0),
					AnalysisRequestNumber = reader.GetString(1),
					NotificationNumber = (reader.IsDBNull(2) ? "" : reader.GetString(2)),
					DeclarationNumber = (reader.IsDBNull(3) ? "" : reader.GetString(3)),
					Supplier = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
					Sender = (reader.IsDBNull(5) ? "" : reader.GetString(5)),
					Origin = (reader.IsDBNull(6) ? "" : reader.GetString(6)),
					PolicyNumber = (reader.IsDBNull(7) ? "" : reader.GetString(7)),
					FinancialReceiptNumber = (reader.IsDBNull(8) ? "" : reader.GetString(8)),
					CertificateType = reader.GetString(9),
					Date = DateTime.ParseExact(reader.GetString(10), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
					Status = reader.GetString(11),
					CreatedBy = reader.GetInt32(12),
					CreatedByName = (reader.IsDBNull(13) ? "" : reader.GetString(13))
				});
			}
			foreach (SampleReception rec in receptions)
			{
				SampleReception sampleReception = rec;
				sampleReception.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
			}
			return receptions;
		}, "GetDelayedPendingReceptionsAsync");
	}

	public Task<List<SampleReception>> SearchSampleReceptionsAsync(string searchTerm)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<SampleReception> receptions = new List<SampleReception>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, \n                              Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, \n                              CertificateType, Date, Status, CreatedBy, CreatedByName\n                              FROM SampleReceptions \n                              WHERE \n                              AnalysisRequestNumber LIKE @Search \n                              OR NotificationNumber LIKE @Search \n                              OR DeclarationNumber LIKE @Search \n                              OR Sender LIKE @Search \n                              OR Supplier LIKE @Search \n                              OR PolicyNumber LIKE @Search\n                              ORDER BY Date DESC LIMIT 200;";
			string searchPattern = "%" + searchTerm + "%";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Search", searchPattern);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				receptions.Add(new SampleReception
				{
					Id = reader.GetInt32(0),
					AnalysisRequestNumber = reader.GetString(1),
					NotificationNumber = (reader.IsDBNull(2) ? "" : reader.GetString(2)),
					DeclarationNumber = (reader.IsDBNull(3) ? "" : reader.GetString(3)),
					Supplier = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
					Sender = (reader.IsDBNull(5) ? "" : reader.GetString(5)),
					Origin = (reader.IsDBNull(6) ? "" : reader.GetString(6)),
					PolicyNumber = (reader.IsDBNull(7) ? "" : reader.GetString(7)),
					FinancialReceiptNumber = (reader.IsDBNull(8) ? "" : reader.GetString(8)),
					CertificateType = reader.GetString(9),
					Date = DateTime.ParseExact(reader.GetString(10), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
					Status = reader.GetString(11),
					CreatedBy = reader.GetInt32(12),
					CreatedByName = (reader.IsDBNull(13) ? "" : reader.GetString(13))
				});
			}
			foreach (SampleReception rec in receptions)
			{
				SampleReception sampleReception = rec;
				sampleReception.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
			}
			return receptions;
		}, "SearchSampleReceptionsAsync");
	}

	private async Task<ObservableCollection<ReceptionSample>> GetSamplesForReceptionAsync(SqliteConnection connection, int receptionId)
	{
		ObservableCollection<ReceptionSample> samples = new ObservableCollection<ReceptionSample>();
		string samplesQuery = "SELECT Id, SampleNumber, Description\n                                 FROM ReceptionSamples \n                                 WHERE ReceptionId = @ReceptionId";
		using SqliteCommand samplesCmd = new SqliteCommand(samplesQuery, connection);
		samplesCmd.Parameters.AddWithValue("@ReceptionId", receptionId);
		using SqliteDataReader samplesReader = await samplesCmd.ExecuteReaderAsync();
		int count = 1;
		while (await samplesReader.ReadAsync())
		{
			samples.Add(new ReceptionSample
			{
				Id = samplesReader.GetInt32(0),
				ReceptionId = receptionId,
				Root = count++.ToString(),
				SampleNumber = (samplesReader.IsDBNull(1) ? "" : samplesReader.GetString(1)),
				Description = (samplesReader.IsDBNull(2) ? "" : samplesReader.GetString(2))
			});
		}
		return samples;
	}

	public Task<List<SampleReception>> SearchReceptionsByFieldAsync(string field, string value)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<SampleReception> receptions = new List<SampleReception>();
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string searchPattern = "%" + value + "%";
			string query = ((field == "رقم العينة") ? "SELECT DISTINCT sr.Id, sr.AnalysisRequestNumber, sr.NotificationNumber, sr.DeclarationNumber, \n                              sr.Supplier, sr.Sender, sr.Origin, sr.PolicyNumber, sr.FinancialReceiptNumber, \n                              sr.CertificateType, sr.Date, sr.Status, sr.CreatedBy, sr.CreatedByName\n                              FROM SampleReceptions sr\n                              INNER JOIN ReceptionSamples rs ON rs.ReceptionId = sr.Id\n                              WHERE rs.SampleNumber LIKE @Search\n                              ORDER BY sr.Date DESC LIMIT 100;" : ((!(field == "رقم الإخطار") && !(field == "رقم الاخطار")) ? "SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, \n                              Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, \n                              CertificateType, Date, Status, CreatedBy, CreatedByName\n                              FROM SampleReceptions \n                              WHERE DeclarationNumber LIKE @Search\n                              ORDER BY Date DESC LIMIT 100;" : "SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, \n                              Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, \n                              CertificateType, Date, Status, CreatedBy, CreatedByName\n                              FROM SampleReceptions \n                              WHERE NotificationNumber LIKE @Search\n                              ORDER BY Date DESC LIMIT 100;"));
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Search", searchPattern);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				receptions.Add(new SampleReception
				{
					Id = reader.GetInt32(0),
					AnalysisRequestNumber = reader.GetString(1),
					NotificationNumber = (reader.IsDBNull(2) ? "" : reader.GetString(2)),
					DeclarationNumber = (reader.IsDBNull(3) ? "" : reader.GetString(3)),
					Supplier = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
					Sender = (reader.IsDBNull(5) ? "" : reader.GetString(5)),
					Origin = (reader.IsDBNull(6) ? "" : reader.GetString(6)),
					PolicyNumber = (reader.IsDBNull(7) ? "" : reader.GetString(7)),
					FinancialReceiptNumber = (reader.IsDBNull(8) ? "" : reader.GetString(8)),
					CertificateType = reader.GetString(9),
					Date = DateTime.ParseExact(reader.GetString(10), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
					Status = reader.GetString(11),
					CreatedBy = reader.GetInt32(12),
					CreatedByName = (reader.IsDBNull(13) ? "" : reader.GetString(13))
				});
			}
			foreach (SampleReception rec in receptions)
			{
				SampleReception sampleReception = rec;
				sampleReception.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
			}
			return receptions;
		}, "SearchReceptionsByFieldAsync");
	}

	public Task<bool> UpdateReceptionStatusAsync(int receptionId, string newStatus)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "UPDATE SampleReceptions SET Status = @Status, UpdatedBy = @UpdatedBy, UpdatedByName = @UpdatedByName, UpdatedAt = CURRENT_TIMESTAMP WHERE Id = @Id;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Id", receptionId);
			command.Parameters.AddWithValue("@Status", newStatus);
			command.Parameters.AddWithValue("@UpdatedBy", _userService.CurrentUser?.Id ?? 1);
			command.Parameters.AddWithValue("@UpdatedByName", _userService.CurrentUser?.FullName ?? "النظام");
			return await command.ExecuteNonQueryAsync() > 0;
		}, "UpdateReceptionStatusAsync");
	}

	public Task<SampleReception?> GetReceptionByIdAsync(int id)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT Id, AnalysisRequestNumber, NotificationNumber, DeclarationNumber, \n                               Supplier, Sender, Origin, PolicyNumber, FinancialReceiptNumber, \n                               CertificateType, Date, Status, CreatedBy, CreatedByName\n                               FROM SampleReceptions \n                               WHERE Id = @Id;";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Id", id);
			using SqliteDataReader reader = await command.ExecuteReaderAsync();
			if (await reader.ReadAsync())
			{
				SampleReception rec = new SampleReception
				{
					Id = reader.GetInt32(0),
					AnalysisRequestNumber = reader.GetString(1),
					NotificationNumber = (reader.IsDBNull(2) ? "" : reader.GetString(2)),
					DeclarationNumber = (reader.IsDBNull(3) ? "" : reader.GetString(3)),
					Supplier = (reader.IsDBNull(4) ? "" : reader.GetString(4)),
					Sender = (reader.IsDBNull(5) ? "" : reader.GetString(5)),
					Origin = (reader.IsDBNull(6) ? "" : reader.GetString(6)),
					PolicyNumber = (reader.IsDBNull(7) ? "" : reader.GetString(7)),
					FinancialReceiptNumber = (reader.IsDBNull(8) ? "" : reader.GetString(8)),
					CertificateType = reader.GetString(9),
					Date = DateTime.ParseExact(reader.GetString(10), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
					Status = reader.GetString(11),
					CreatedBy = reader.GetInt32(12),
					CreatedByName = (reader.IsDBNull(13) ? "" : reader.GetString(13))
				};
				SampleReception sampleReception = rec;
				sampleReception.Samples = await GetSamplesForReceptionAsync(connection, rec.Id);
				return rec;
			}
			return (SampleReception)null;
		}, "GetReceptionByIdAsync");
	}
}
