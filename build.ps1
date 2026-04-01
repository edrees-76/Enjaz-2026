# Build Script for Enjaz System 2026
# This script automates the publishing and preparation for the installer

$ProjectName = "Enjaz"
$BuildDir = "dist"
$PublishDir = "$BuildDir\publish"
$Runtime = "win-x64"

Write-Host "--- Starting Build Process for $ProjectName ---" -ForegroundColor Cyan

# 1. Clean previous builds
if (Test-Path $BuildDir) {
    Write-Host "Cleaning existing build directory..."
    Remove-Item -Path $BuildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

# 2. Run dotnet publish
Write-Host "Running dotnet publish for $Runtime..." -ForegroundColor Yellow
dotnet publish "$ProjectName.csproj" -c Release -r $Runtime --self-contained false -o $PublishDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "Dotnet publish failed!"
    exit 1
}

# 3. Purge Database
Write-Host "Purging published database from test data..." -ForegroundColor Yellow
python "reset_db.py"

# 4. Verify assets are included
Write-Host "Verifying assets..." -ForegroundColor Yellow
if (-not (Test-Path "$PublishDir\Assets")) {
    Write-Host "Copying Assets manually..." -ForegroundColor Gray
    Copy-Item -Path "Assets" -Destination "$PublishDir" -Recurse -Force
}

# 6. Run Inno Setup Compiler
Write-Host "--- Packaging Installer ---" -ForegroundColor Cyan
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "Installer_Script.iss"

# 7. Success message
Write-Host "--- Build Finished Successfully ---" -ForegroundColor Green
Write-Host "Production Setup: Setup_Enjaz_2026.exe is ready."
