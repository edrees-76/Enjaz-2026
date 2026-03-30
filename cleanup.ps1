$dllPath = Join-Path $PSScriptRoot "bin\Debug\net8.0-windows\Microsoft.Data.Sqlite.dll"
Add-Type -Path $dllPath
$dbFile = Join-Path $env:LOCALAPPDATA "Enjaz\certificates.db"
$c = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$dbFile")
$c.Open()
$cmd = $c.CreateCommand()
$cmd.CommandText = "DELETE FROM Samples WHERE CertificateId IN (SELECT Id FROM Certificates WHERE CertificateNumber LIKE 'LOAD-%')"
$s = $cmd.ExecuteNonQuery()
$cmd.CommandText = "DELETE FROM Certificates WHERE CertificateNumber LIKE 'LOAD-%'"
$d = $cmd.ExecuteNonQuery()
$cmd.CommandText = "SELECT COUNT(*) FROM Certificates"
$r = $cmd.ExecuteScalar()
$c.Close()
Write-Host "Deleted $d certificates and $s samples"
Write-Host "Remaining: $r"
Write-Host "DONE!"
