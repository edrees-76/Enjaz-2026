using NUnit.Framework;
using Enjaz.Services;
using Enjaz.Services.Repositories;
using Enjaz.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Enjaz.Tests
{
    
    public class CertificateRepositoryTests 
    {
        private string _testDbPath;
        private DatabaseService _dbService;
        private UserService _userService;
        private CertificateRepository _repository;

        [SetUp]
        public async Task Setup()
        {
            // Use a unique temporary file for each test
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_certs_{Guid.NewGuid()}.db");
            _dbService = new DatabaseService($"Data Source={_testDbPath}");
            _userService = new UserService();
            
            // CRITICAL: Insert a dummy user because of foreign key constraint in Certificates table
            using (var connection = new SqliteConnection(_dbService.ConnectionString))
            {
                await connection.OpenAsync();
                var cmd = new SqliteCommand("INSERT INTO Users (Id, Username, PasswordHash, FullName, Role) VALUES (1, 'admin', 'hash', 'Admin User', 2)", connection);
                await cmd.ExecuteNonQueryAsync();
            }

            // Mock a logged in user
            _userService.Login(new User { Id = 1, Username = "admin", FullName = "Admin User" });
            
            _repository = new CertificateRepository(_dbService, _userService);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up the temporary database
            if (File.Exists(_testDbPath))
            {
                try { File.Delete(_testDbPath); } catch { /* ignore */ }
            }
        }

        [Test]
        public async Task AddCertificateAsync_ShouldInsertAndReturnId()
        {
            // Arrange
            var cert = new Certificate
            {
                CertificateNumber = "REQ-2025-001",
                RecipientName = "Test Recipient",
                CertificateType = "بيئية",
                IssueDate = DateTime.Now,
                CreatedBy = 1,
                CreatedByName = "Admin User"
            };

            // Act
            int id = await _repository.AddCertificateAsync(cert);
            var result = await _repository.GetCertificateByIdAsync(id);

            // Assert
            Assert.IsTrue(id > 0);
            Assert.IsNotNull(result);
            Assert.That(result.CertificateNumber, Is.Not.Null.Or.Empty);
            Assert.AreEqual("Test Recipient", result.RecipientName);
        }

        [Test]
        public async Task SearchCertificatesAsync_ShouldFindMatchingCertificates()
        {
            // Arrange
            var cert1 = new Certificate { CertificateNumber = "C1", RecipientName = "Alpha", CertificateType = "T1", IssueDate = DateTime.Now, CreatedBy = 1 };
            var cert2 = new Certificate { CertificateNumber = "C2", RecipientName = "Beta", CertificateType = "T2", IssueDate = DateTime.Now, CreatedBy = 1 };
            await _repository.AddCertificateAsync(cert1);
            await _repository.AddCertificateAsync(cert2);

            // Act
            var results = await _repository.SearchCertificatesAsync("Alpha", "All", 1, 10);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Alpha", results[0].RecipientName);
        }

        [Test]
        public async Task AddSampleAsync_ShouldLinkSampleToCertificate()
        {
            // Arrange
            var cert = new Certificate { CertificateNumber = "C-SAMP", RecipientName = "R", CertificateType = "T", IssueDate = DateTime.Now, CreatedBy = 1 };
            int certId = await _repository.AddCertificateAsync(cert);

            var sample = new Sample
            {
                CertificateId = certId,
                SampleNumber = "S-001",
                Description = "Test Sample",
                Result = "Pass"
            };

            // Act
            await _repository.AddSampleAsync(sample);
            var samples = await _repository.GetSamplesByCertificateIdAsync(certId);

            // Assert
            Assert.IsTrue(samples.Count > 0);
            Assert.AreEqual("S-001", samples[0].SampleNumber);
        }
    }
}
