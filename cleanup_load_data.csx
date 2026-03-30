// Temporary cleanup script - delete LOAD test data from database
#r "bin\Debug\net8.0-windows\Microsoft.Data.Sqlite.dll"
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Enjaz", "certificates.db");
Console.WriteLine($"DB: {dbPath}");

if (!File.Exists(dbPath)) { Console.WriteLine("DB NOT FOUND!"); return; }

using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

var cmd = conn.CreateCommand();

// Count before
cmd.CommandText = "SELECT COUNT(*) FROM Certificates WHERE CertificateNumber LIKE 'LOAD-%'";
Console.WriteLine($"LOAD records found: {cmd.ExecuteScalar()}");

// Delete samples first (foreign key)
cmd.CommandText = "DELETE FROM Samples WHERE CertificateId IN (SELECT Id FROM Certificates WHERE CertificateNumber LIKE 'LOAD-%')";
var s = cmd.ExecuteNonQuery();
Console.WriteLine($"Deleted {s} fake samples");

// Delete certificates
cmd.CommandText = "DELETE FROM Certificates WHERE CertificateNumber LIKE 'LOAD-%'";
var c = cmd.ExecuteNonQuery();
Console.WriteLine($"Deleted {c} fake certificates");

// Verify
cmd.CommandText = "SELECT COUNT(*) FROM Certificates";
Console.WriteLine($"Remaining certificates: {cmd.ExecuteScalar()}");

// Clean up FTS index
cmd.CommandText = "DELETE FROM Certificates_FTS";
cmd.ExecuteNonQuery();
cmd.CommandText = "INSERT INTO Certificates_FTS(Certificates_FTS) VALUES('rebuild')";
cmd.ExecuteNonQuery();
Console.WriteLine("FTS index rebuilt");

// VACUUM to reclaim space
cmd.CommandText = "VACUUM";
cmd.ExecuteNonQuery();
Console.WriteLine("Database compacted. DONE!");
