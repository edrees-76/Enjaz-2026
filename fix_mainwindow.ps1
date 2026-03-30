# سكريبت إصلاح ملف MainWindow.xaml
# يقوم بنسخ النسخة الاحتياطية السليمة واستبدال الملف التالف

$backupPath = "D:\نسخة احتياطبة منظومة انجاز\نسخة_2026-03-25\Views\MainWindow.xaml"
$targetPath = "d:\منظومة انجاز\Views\MainWindow.xaml"

Write-Host "جاري نسخ الملف السليم من النسخة الاحتياطية..." -ForegroundColor Yellow

# نسخ الملف مع الحفاظ على الترميز الأصلي (byte-by-byte copy)
[System.IO.File]::Copy($backupPath, $targetPath, $true)

Write-Host "تم نسخ الملف بنجاح!" -ForegroundColor Green

# التحقق من الحجم
$backupSize = (Get-Item $backupPath).Length
$targetSize = (Get-Item $targetPath).Length
Write-Host "حجم النسخة الاحتياطية: $backupSize bytes"
Write-Host "حجم الملف الحالي: $targetSize bytes"

if ($backupSize -eq $targetSize) {
    Write-Host "الملفات متطابقة - تمت العملية بنجاح!" -ForegroundColor Green
} else {
    Write-Host "تحذير: الملفات غير متطابقة!" -ForegroundColor Red
}

Write-Host ""
Write-Host "اضغط أي مفتاح للإغلاق..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
