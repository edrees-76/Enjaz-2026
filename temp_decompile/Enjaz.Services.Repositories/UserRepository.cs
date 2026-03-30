using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Enjaz.Helpers;
using Enjaz.Models;
using Microsoft.Data.Sqlite;

namespace Enjaz.Services.Repositories;

public class UserRepository
{
	private readonly DatabaseService _db;

	public UserRepository(DatabaseService db)
	{
		_db = db;
	}

	public Task<List<User>> GetAllUsersAsync()
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			List<User> users = new List<User>();
			using (SqliteConnection connection = new SqliteConnection(_db.ConnectionString))
			{
				await connection.OpenAsync();
				string query = "SELECT Id, Username, FullName, Role, IsActive, IsEditor, Permissions, CreatedAt FROM Users ORDER BY Role DESC, FullName";
				using SqliteCommand command = new SqliteCommand(query, connection);
				using SqliteDataReader reader = await command.ExecuteReaderAsync();
				while (await reader.ReadAsync())
				{
					users.Add(new User
					{
						Id = reader.GetInt32(0),
						Username = reader.GetString(1),
						FullName = reader.GetString(2),
						Role = (UserRole)reader.GetInt32(3),
						IsActive = (reader.GetInt32(4) == 1),
						IsEditor = (!reader.IsDBNull(5) && reader.GetInt32(5) == 1),
						Permissions = ((!reader.IsDBNull(6)) ? reader.GetString(6) : string.Empty),
						CreatedAt = (DateTime.TryParse(reader.GetString(7), out var dt) ? dt : DateTime.Now)
					});
				}
			}
			return users;
		}, "GetAllUsersAsync");
	}

	public Task<(User? user, LoginResult result)> ValidateUserWithStatusAsync(string username, string password)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using (SqliteConnection connection = new SqliteConnection(_db.ConnectionString))
			{
				await connection.OpenAsync();
				string query = "SELECT Id, Username, PasswordHash, FullName, Role, IsActive, IsEditor, Permissions, CreatedAt FROM Users WHERE Username = @Username";
				using SqliteCommand command = new SqliteCommand(query, connection);
				command.Parameters.AddWithValue("@Username", username);
				using SqliteDataReader reader = await command.ExecuteReaderAsync();
				if (await reader.ReadAsync())
				{
					string storedHash = reader.GetString(2);
					bool passwordVerified;
					if (!storedHash.StartsWith("v2:"))
					{
						LoggerService.LogInfo("Login attempt for user '" + username + "': Verifying password using legacy SHA256 hashing.");
						passwordVerified = PasswordHelper.VerifyPassword(password, storedHash);
						if (passwordVerified && username.Equals("admin", StringComparison.OrdinalIgnoreCase))
						{
							LoggerService.LogInfo("Admin user '" + username + "' logged in with legacy hash. Updating password to new algorithm.");
							string newHash = PasswordHelper.HashPassword(password);
							string updateQuery = "UPDATE Users SET PasswordHash = @NewHash WHERE Id = @Id";
							using (SqliteCommand updateCmd = new SqliteCommand(updateQuery, connection))
							{
								updateCmd.Parameters.AddWithValue("@NewHash", newHash);
								updateCmd.Parameters.AddWithValue("@Id", reader.GetInt32(0));
								await updateCmd.ExecuteNonQueryAsync();
							}
							LoggerService.LogInfo("Admin user '" + username + "' password hash updated successfully.");
						}
					}
					else
					{
						LoggerService.LogInfo("Login attempt for user '" + username + "': Verifying password using PBKDF2 (v2) hashing.");
						passwordVerified = PasswordHelper.VerifyPassword(password, storedHash);
					}
					if (passwordVerified)
					{
						bool isActive = reader.GetInt32(5) == 1;
						if (!isActive)
						{
							return ((User, LoginResult))(null, LoginResult.AccountFrozen);
						}
						DateTime dt;
						User user = new User
						{
							Id = reader.GetInt32(0),
							Username = reader.GetString(1),
							PasswordHash = storedHash,
							FullName = reader.GetString(3),
							Role = (UserRole)reader.GetInt32(4),
							IsActive = isActive,
							IsEditor = (!reader.IsDBNull(6) && reader.GetInt32(6) == 1),
							Permissions = ((!reader.IsDBNull(7)) ? reader.GetString(7) : string.Empty),
							CreatedAt = (DateTime.TryParse(reader.GetString(8), out dt) ? dt : DateTime.Now)
						};
						return (user, LoginResult.Success);
					}
					LoggerService.LogWarning("Login failed for user '" + username + "': Password verification failed.");
				}
				else
				{
					LoggerService.LogWarning("Login failed: Username '" + username + "' not found.");
				}
			}
			return ((User, LoginResult))(null, LoginResult.InvalidCredentials);
		}, "ValidateUserWithStatusAsync");
	}

	public Task<bool> IsUsernameUniqueAsync(string username, int? userIdToExclude = null)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
			if (userIdToExclude.HasValue)
			{
				query += " AND Id != @UserId";
			}
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Username", username);
			if (userIdToExclude.HasValue)
			{
				command.Parameters.AddWithValue("@UserId", userIdToExclude.Value);
			}
			long count = (long)((await command.ExecuteScalarAsync()) ?? ((object)0));
			return count == 0;
		}, "IsUsernameUniqueAsync");
	}

	public Task AddUserAsync(User user, string password)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "\r\n                        INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, IsEditor, Permissions, CreatedAt)\r\n                        VALUES (@Username, @PasswordHash, @FullName, @Role, @IsActive, @IsEditor, @Permissions, @CreatedAt)";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Username", user.Username);
			command.Parameters.AddWithValue("@PasswordHash", PasswordHelper.HashPassword(password));
			command.Parameters.AddWithValue("@FullName", user.FullName);
			command.Parameters.AddWithValue("@Role", (int)user.Role);
			command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
			command.Parameters.AddWithValue("@IsEditor", user.IsEditor ? 1 : 0);
			command.Parameters.AddWithValue("@Permissions", user.Permissions ?? string.Empty);
			command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			await command.ExecuteNonQueryAsync();
		}, "AddUserAsync");
	}

	public Task UpdateUserAsync(User user, string? newPassword = null)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string query = "\r\n                        UPDATE Users \r\n                        SET Username = @Username,\r\n                            FullName = @FullName,\r\n                            Role = @Role,\r\n                            IsActive = @IsActive,\r\n                            IsEditor = @IsEditor,\r\n                            Permissions = @Permissions";
			if (!string.IsNullOrEmpty(newPassword))
			{
				query += ", PasswordHash = @PasswordHash";
			}
			query += " WHERE Id = @Id";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Id", user.Id);
			command.Parameters.AddWithValue("@Username", user.Username);
			command.Parameters.AddWithValue("@FullName", user.FullName);
			command.Parameters.AddWithValue("@Role", (int)user.Role);
			command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
			command.Parameters.AddWithValue("@IsEditor", user.IsEditor ? 1 : 0);
			command.Parameters.AddWithValue("@Permissions", user.Permissions ?? string.Empty);
			if (!string.IsNullOrEmpty(newPassword))
			{
				command.Parameters.AddWithValue("@PasswordHash", PasswordHelper.HashPassword(newPassword));
			}
			await command.ExecuteNonQueryAsync();
		}, "UpdateUserAsync");
	}

	public Task ToggleUserFreezeAsync(int userId, bool freeze)
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			int isActive = ((!freeze) ? 1 : 0);
			string query = "UPDATE Users SET IsActive = @IsActive WHERE Id = @Id";
			using SqliteCommand command = new SqliteCommand(query, connection);
			command.Parameters.AddWithValue("@Id", userId);
			command.Parameters.AddWithValue("@IsActive", isActive);
			await command.ExecuteNonQueryAsync();
		}, "ToggleUserFreezeAsync");
	}

	public Task CreateDefaultAdminAsync()
	{
		return _db.ExecuteWithRetryAsync(async delegate
		{
			using SqliteConnection connection = new SqliteConnection(_db.ConnectionString);
			await connection.OpenAsync();
			string countQuery = "SELECT COUNT(*) FROM Users WHERE Username = 'admin'";
			using SqliteCommand command = new SqliteCommand(countQuery, connection);
			long count = (long)((await command.ExecuteScalarAsync()) ?? ((object)0));
			if (count == 0)
			{
				string insertQuery = "\r\n                                INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, CreatedAt)\r\n                                VALUES (@Username, @PasswordHash, @FullName, @Role, 1, @CreatedAt)";
				using SqliteCommand insertCmd = new SqliteCommand(insertQuery, connection);
				string tempPassword = Guid.NewGuid().ToString("N").Substring(0, 12);
				insertCmd.Parameters.AddWithValue("@Username", "admin");
				insertCmd.Parameters.AddWithValue("@PasswordHash", PasswordHelper.HashPassword(tempPassword));
				insertCmd.Parameters.AddWithValue("@FullName", "مدير النظام");
				insertCmd.Parameters.AddWithValue("@Role", 2);
				insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				await insertCmd.ExecuteNonQueryAsync();
				LoggerService.LogWarning("Default admin created with TEMPORARY password: " + tempPassword + ". CHANGE THIS IMMEDIATELY!");
			}
			else
			{
				LoggerService.LogInfo("Admin user already exists. Verifying state...");
				string checkAdminQuery = "SELECT IsActive, Role FROM Users WHERE Username = 'admin'";
				using SqliteCommand checkCmd = new SqliteCommand(checkAdminQuery, connection);
				using SqliteDataReader reader = await checkCmd.ExecuteReaderAsync();
				if (await reader.ReadAsync())
				{
					bool isActive = reader.GetInt32(0) == 1;
					UserRole role = (UserRole)reader.GetInt32(1);
					if (!isActive || role != UserRole.Admin)
					{
						LoggerService.LogWarning("Admin account was inactive or had wrong role. Fixing...");
						string fixQuery = "UPDATE Users SET IsActive = 1, Role = @Role WHERE Username = 'admin'";
						using SqliteCommand fixCmd = new SqliteCommand(fixQuery, connection);
						fixCmd.Parameters.AddWithValue("@Role", 2);
						await fixCmd.ExecuteNonQueryAsync();
					}
				}
			}
		}, "CreateDefaultAdminAsync");
	}
}
