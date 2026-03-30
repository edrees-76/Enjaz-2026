using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Enjaz.Models;
using Enjaz.Helpers;
using Enjaz.Services;

namespace Enjaz.Services.Repositories
{
    public class UserRepository
    {
        private readonly DatabaseService _db;

        public UserRepository(DatabaseService db)
        {
            _db = db;
        }

        /// <summary>
        /// الحصول على جميع المستخدمين بشكل غير متزامن
        /// Get all users asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<List<User>> GetAllUsersAsync()
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                var users = new List<User>();
                using (var connection = new SqliteConnection(_db.ConnectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT Id, Username, FullName, Role, IsActive, IsEditor, Permissions, CreatedAt FROM Users ORDER BY Role DESC, FullName";
                    using (var command = new SqliteCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                FullName = reader.GetString(2),
                                Role = (UserRole)reader.GetInt32(3),
                                IsActive = reader.GetInt32(4) == 1,
                                IsEditor = !reader.IsDBNull(5) && reader.GetInt32(5) == 1,
                                Permissions = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty,
                                CreatedAt = DateTime.TryParse(reader.GetString(7), out var dt) ? dt : DateTime.Now
                            });
                        }
                    }
                }
                return users;
            }, "GetAllUsersAsync");
        }

        /// <summary>
        /// التحقق من صحة بيانات تسجيل الدخول بشكل غير متزامن
        /// Validate login credentials asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<(User? user, LoginResult result)> ValidateUserWithStatusAsync(string username, string password)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using (var connection = new SqliteConnection(_db.ConnectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT Id, Username, PasswordHash, FullName, Role, IsActive, IsEditor, Permissions, CreatedAt FROM Users WHERE Username = @Username";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string storedHash = reader.GetString(2);

                                // Improved password verification logging and potential fixup logic
                                bool passwordVerified = false;
                                if (!storedHash.StartsWith("v2:"))
                                {
                                    LoggerService.LogInfo($"Login attempt for user '{username}': Verifying password using legacy SHA256 hashing.");
                                    // Assuming PasswordHelper.VerifyPassword can handle legacy hashes or there's a separate method
                                    // For this example, we'll assume PasswordHelper.VerifyPassword handles both.
                                    // If a separate legacy verification is needed, it would be called here.
                                    passwordVerified = PasswordHelper.VerifyPassword(password, storedHash);

                                    // Admin account fixup logic: If legacy hash is used for 'admin' and it's correct,
                                    // re-hash with new algorithm and update the database.
                                    if (passwordVerified && username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                                    {
                                        LoggerService.LogInfo($"Admin user '{username}' logged in with legacy hash. Updating password to new algorithm.");
                                        string newHash = PasswordHelper.HashPassword(password);
                                        string updateQuery = "UPDATE Users SET PasswordHash = @NewHash WHERE Id = @Id";
                                        using (var updateCmd = new SqliteCommand(updateQuery, connection))
                                        {
                                            updateCmd.Parameters.AddWithValue("@NewHash", newHash);
                                            updateCmd.Parameters.AddWithValue("@Id", reader.GetInt32(0));
                                            await updateCmd.ExecuteNonQueryAsync();
                                        }
                                        LoggerService.LogInfo($"Admin user '{username}' password hash updated successfully.");
                                    }
                                }
                                else
                                {
                                    LoggerService.LogInfo($"Login attempt for user '{username}': Verifying password using PBKDF2 (v2) hashing.");
                                    passwordVerified = PasswordHelper.VerifyPassword(password, storedHash);
                                }

                                if (passwordVerified)
                                {
                                    bool isActive = reader.GetInt32(5) == 1;
                                    if (!isActive)
                                    {
                                        return ((User?)null, LoginResult.AccountFrozen);
                                    }

                                    var user = new User
                                    {
                                        Id = reader.GetInt32(0),
                                        Username = reader.GetString(1),
                                        PasswordHash = storedHash,
                                        FullName = reader.GetString(3),
                                        Role = (UserRole)reader.GetInt32(4),
                                        IsActive = isActive,
                                        IsEditor = !reader.IsDBNull(6) && reader.GetInt32(6) == 1,
                                        Permissions = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty,
                                        CreatedAt = DateTime.TryParse(reader.GetString(8), out var dt) ? dt : DateTime.Now
                                    };
                                    return (user, LoginResult.Success);
                                }
                                else
                                {
                                    LoggerService.LogWarning($"Login failed for user '{username}': Password verification failed.");
                                }
                            }
                            else
                            {
                                LoggerService.LogWarning($"Login failed: Username '{username}' not found.");
                            }
                        }
                    }
                }
                return ((User?)null, LoginResult.InvalidCredentials);
            }, "ValidateUserWithStatusAsync");
        }
        
        /// <summary>
        /// التحقق من تفرد اسم المستخدم بشكل غير متزامن
        /// </summary>
        public System.Threading.Tasks.Task<bool> IsUsernameUniqueAsync(string username, int? userIdToExclude = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using (var connection = new SqliteConnection(_db.ConnectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                    if (userIdToExclude.HasValue)
                    {
                        query += " AND Id != @UserId";
                    }

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        if (userIdToExclude.HasValue)
                        {
                            command.Parameters.AddWithValue("@UserId", userIdToExclude.Value);
                        }

                        long count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        return count == 0;
                    }
                }
            }, "IsUsernameUniqueAsync");
        }

        /// <summary>
        /// إضافة مستخدم جديد بشكل غير متزامن
        /// </summary>
        public System.Threading.Tasks.Task AddUserAsync(User user, string password)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using (var connection = new SqliteConnection(_db.ConnectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, IsEditor, Permissions, CreatedAt)
                        VALUES (@Username, @PasswordHash, @FullName, @Role, @IsActive, @IsEditor, @Permissions, @CreatedAt)";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", user.Username);
                        command.Parameters.AddWithValue("@PasswordHash", PasswordHelper.HashPassword(password));
                        command.Parameters.AddWithValue("@FullName", user.FullName);
                        command.Parameters.AddWithValue("@Role", (int)user.Role);
                        command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
                        command.Parameters.AddWithValue("@IsEditor", user.IsEditor ? 1 : 0);
                        command.Parameters.AddWithValue("@Permissions", user.Permissions ?? string.Empty);
                        command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }, "AddUserAsync");
        }

        /// <summary>
        /// تحديث بيانات المستخدم بشكل غير متزامن
        /// </summary>
        public System.Threading.Tasks.Task UpdateUserAsync(User user, string? newPassword = null)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using (var connection = new SqliteConnection(_db.ConnectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        UPDATE Users 
                        SET Username = @Username,
                            FullName = @FullName,
                            Role = @Role,
                            IsActive = @IsActive,
                            IsEditor = @IsEditor,
                            Permissions = @Permissions";

                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        query += ", PasswordHash = @PasswordHash";
                    }

                    query += " WHERE Id = @Id";

                    using (var command = new SqliteCommand(query, connection))
                    {
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
                    }
                }
            }, "UpdateUserAsync");
        }

        /// <summary>
        /// تجميد/إلغاء تجميد المستخدم بشكل غير متزامن
        /// </summary>
        public System.Threading.Tasks.Task ToggleUserFreezeAsync(int userId, bool freeze)
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using (var connection = new SqliteConnection(_db.ConnectionString))
                {
                    await connection.OpenAsync();
                    int isActive = freeze ? 0 : 1;

                    string query = "UPDATE Users SET IsActive = @IsActive WHERE Id = @Id";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", userId);
                        command.Parameters.AddWithValue("@IsActive", isActive);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }, "ToggleUserFreezeAsync");
        }
        
        public System.Threading.Tasks.Task CreateDefaultAdminAsync()
        {
            return _db.ExecuteWithRetryAsync(async () =>
            {
                using (var connection = new SqliteConnection(_db.ConnectionString))
                {
                    await connection.OpenAsync();
                    // Check if users exist
                    string countQuery = "SELECT COUNT(*) FROM Users WHERE Username = 'admin'";
                    using (var command = new SqliteCommand(countQuery, connection))
                    {
                        long count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        if (count == 0)
                        {
                             // Create Admin
                             string insertQuery = @"
                                INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive, CreatedAt)
                                VALUES (@Username, @PasswordHash, @FullName, @Role, 1, @CreatedAt)";
                             
                             using (var insertCmd = new SqliteCommand(insertQuery, connection))
                             {
                                 // QW6: Generate a random strong password instead of hardcoded '12345'
                                 string tempPassword = Guid.NewGuid().ToString("N")[..12];
                                 insertCmd.Parameters.AddWithValue("@Username", "admin");
                                 insertCmd.Parameters.AddWithValue("@PasswordHash", PasswordHelper.HashPassword(tempPassword));
                                 insertCmd.Parameters.AddWithValue("@FullName", "مدير النظام");
                                 insertCmd.Parameters.AddWithValue("@Role", (int)UserRole.Admin);
                                 insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                 await insertCmd.ExecuteNonQueryAsync();
                                 LoggerService.LogWarning($"Default admin created with TEMPORARY password: {tempPassword}. CHANGE THIS IMMEDIATELY!");
                             }
                        }
                        else
                        {
                             LoggerService.LogInfo("Admin user already exists. Verifying state...");
                             
                             // Check and fix IsActive and Role if necessary
                             string checkAdminQuery = "SELECT IsActive, Role FROM Users WHERE Username = 'admin'";
                             using (var checkCmd = new SqliteCommand(checkAdminQuery, connection))
                             using (var reader = await checkCmd.ExecuteReaderAsync())
                             {
                                 if (await reader.ReadAsync())
                                 {
                                     bool isActive = reader.GetInt32(0) == 1;
                                     UserRole role = (UserRole)reader.GetInt32(1);
                                     if (!isActive || role != UserRole.Admin)
                                     {
                                         LoggerService.LogWarning("Admin account was inactive or had wrong role. Fixing...");
                                         string fixQuery = "UPDATE Users SET IsActive = 1, Role = @Role WHERE Username = 'admin'";
                                         using (var fixCmd = new SqliteCommand(fixQuery, connection))
                                         {
                                             fixCmd.Parameters.AddWithValue("@Role", (int)UserRole.Admin);
                                             await fixCmd.ExecuteNonQueryAsync();
                                         }
                                     }
                                 }
                             }
                        }
                    }
                }
            }, "CreateDefaultAdminAsync");
        }
    }
}
