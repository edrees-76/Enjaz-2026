using NUnit.Framework;
using Enjaz.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Enjaz.Tests
{
    
    public class ArchiveServiceTests 
    {
        private string _testDbPath;
        private string _archiveDbPath;
        private DatabaseService _dbService;
        private ArchiveService _archiveService;

        [SetUp]
        public void Setup()
        {
            // Create unique paths for each test
            string tempDir = Path.GetTempPath();
            string uniqueId = Guid.NewGuid().ToString();
            _testDbPath = Path.Combine(tempDir, $"test_main_{uniqueId}.db");
            _archiveDbPath = Path.Combine(tempDir, "archive.db"); // ArchiveService expects it in the same folder

            // Initialize DatabaseService with test path
            _dbService = new DatabaseService($"Data Source={_testDbPath}");
            _archiveService = new ArchiveService(_dbService);
            
            // Adjust archive path in test to match where ArchiveService will put it (same dir as main db)
            _archiveDbPath = _archiveService.GetArchivePath();

            // Clean up any pre-existing archive file (unlikely due to GUID, but good practice)
            if (File.Exists(_archiveDbPath)) File.Delete(_archiveDbPath);
        }

        [TearDown]
        public void TearDown()
        {
            // Close connections is handled by using blocks in methods, 
            // but we need to ensure files can be deleted.
            SqliteConnection.ClearAllPools();

            if (File.Exists(_testDbPath))
            {
                try { File.Delete(_testDbPath); } catch { /* ignore */ }
            }
            if (File.Exists(_archiveDbPath))
            {
                try { File.Delete(_archiveDbPath); } catch { /* ignore */ }
            }
        }

        private async Task InsertLogAsync(string action, DateTime timestamp)
        {
            using (var conn = new SqliteConnection(_dbService.ConnectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqliteCommand(
                    "INSERT INTO AuditLogs (Action, Timestamp) VALUES (@Action, @Timestamp)", conn);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@Timestamp", timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task<int> GetLogCountAsync(string dbPath)
        {
            if (!File.Exists(dbPath)) return 0;

            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                var cmd = new SqliteCommand("SELECT COUNT(*) FROM AuditLogs", conn);
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }

        [Test]
        public async Task ArchiveOldLogsAsync_ShouldMoveOldLogsToArchiveDb()
        {
            // Arrange
            // Old logs (should be archived)
            await InsertLogAsync("OldLog1", DateTime.Now.AddMonths(-7));
            await InsertLogAsync("OldLog2", DateTime.Now.AddMonths(-8));
            
            // New logs (should stay)
            await InsertLogAsync("NewLog1", DateTime.Now.AddMonths(-1));

            // Act
            int archivedCount = await _archiveService.ArchiveOldLogsAsync(6); // Keep 6 months

            // Assert
            Assert.That(archivedCount, Is.EqualTo(2), "Should return count of archived logs");

            int mainCount = await GetLogCountAsync(_testDbPath);
            int archiveCount = await GetLogCountAsync(_archiveDbPath);

            Assert.That(mainCount, Is.EqualTo(1), "Main DB should only have recent logs");
            Assert.That(archiveCount, Is.EqualTo(2), "Archive DB should have old logs");
        }

        [Test]
        public async Task ArchiveOldLogsAsync_ShouldKeepRecentLogsInMainDb()
        {
            // Arrange
            await InsertLogAsync("RecentLog1", DateTime.Now.AddMonths(-2));
            await InsertLogAsync("RecentLog2", DateTime.Now.AddMonths(-1));
            await InsertLogAsync("FutureLog", DateTime.Now.AddMonths(1));

            // Act
            int archivedCount = await _archiveService.ArchiveOldLogsAsync(6);

            // Assert
            Assert.AreEqual(0, archivedCount);
            Assert.AreEqual(3, await GetLogCountAsync(_testDbPath));
            Assert.AreEqual(0, await GetLogCountAsync(_archiveDbPath));
        }

        [Test]
        public async Task ArchiveOldLogsAsync_ShouldCreateArchiveDbIfMissing()
        {
            // Arrange
            Assert.That(File.Exists(_archiveDbPath), Is.False, "Archive DB should not exist initially");
            await InsertLogAsync("OldLog", DateTime.Now.AddMonths(-10));

            // Act
            await _archiveService.ArchiveOldLogsAsync(6);

            // Assert
            Assert.That(File.Exists(_archiveDbPath), Is.True, "Archive DB should be created");
            Assert.AreEqual(1, await GetLogCountAsync(_archiveDbPath));
        }
    }
}
