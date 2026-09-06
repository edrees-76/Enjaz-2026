; =========================================================================
; Professional Windows Installer for "منظومة إنجاز 2026"
; Optimized for Modern UI and Institutional Identity
; Developer: م. ادريس فتح الله الهرى
; =========================================================================

#define AppName "منظومة إنجاز"
#define AppVersion "1.0.7"
#define AppPublisher "م. ادريس فتح الله الهرى"
#define AppURL "edreeselhery@gmail.com"
#define AppExeName "Enjaz.exe"
#define OutputName "Setup_Enjaz_2026"
#define ProjectDir "d:\منظومة انجاز 2026"
#define SourceDir ProjectDir + "\dist\publish"

[Setup]
; --- Application Identity ---
AppId={{D3E4F5A6-C7B8-4D9E-AF01-BD2C3D4E5F6A}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir={#ProjectDir}
OutputBaseFilename={#OutputName}
WizardSizePercent=140

; --- Branding & Icons ---
SetupIconFile={#ProjectDir}\Assets\enjaz_3d_icon_transparent.ico
WizardImageFile=D:\منظومة انجاز 2026\ملفات الثبيت\enjaz_3d.bmp
WizardSmallImageFile=D:\منظومة انجاز 2026\ملفات الثبيت\enjaz_3d.bmp
UninstallDisplayIcon={app}\{#AppExeName}

; --- Institutional Standards ---
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DisableWelcomePage=no
WizardStyle=modern
LanguageDetectionMethod=locale

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[LangOptions]
RightToLeft=yes

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "D:\منظومة انجاز 2026\ملفات الثبيت\designer_logo.bmp"; Flags: dontcopy

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
const
  bdLeftToRight = 0;
  bdRightToLeft = 1;
  WS_EX_RIGHT = $00001000;
  WS_EX_RTLREADING = $00002000;
  GWL_EXSTYLE = -20;

function GetWindowLong(hWnd: HWND; nIndex: Integer): Longint; external 'GetWindowLongW@user32.dll stdcall';
function SetWindowLong(hWnd: HWND; nIndex: Integer; dwNewLong: Longint): Longint; external 'SetWindowLongW@user32.dll stdcall';

var
  AboutSystemPage, AboutDesignerPage: TWizardPage;
  DesignerLogoImage: TBitmapImage;

procedure ApplyRTL(Control: TWinControl);
var
  ExStyle: Longint;
begin
  ExStyle := GetWindowLong(Control.Handle, GWL_EXSTYLE);
  SetWindowLong(Control.Handle, GWL_EXSTYLE, ExStyle or WS_EX_RIGHT or WS_EX_RTLREADING);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  DataDir: String;
begin
  // Purge existing data to ensure a clean install as requested by user
  if CurStep = ssInstall then
  begin
    DataDir := ExpandConstant('{localappdata}\CertificateSystem');
    if DirExists(DataDir) then
    begin
      DelTree(DataDir, True, True, True);
    end;
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  // Hide small system logo on Designer page to focus on designer branding
  if CurPageID = AboutDesignerPage.ID then
    WizardForm.WizardSmallBitmapImage.Visible := False
  else
    WizardForm.WizardSmallBitmapImage.Visible := True;
end;

procedure InitializeWizard();
var
  SystemInfoMemo, DesignerInfoMemo: TNewMemo;
begin
  // --- Page 1: About the System (نبذة عن المنظومة) ---
  AboutSystemPage := CreateCustomPage(wpWelcome, 'منظومة إنجاز لإدارة العينات والتحاليل', 'تعريف شامل بالنظام الرقمي المتكامل');
  
  SystemInfoMemo := TNewMemo.Create(AboutSystemPage);
  SystemInfoMemo.Parent := AboutSystemPage.Surface;
  SystemInfoMemo.Left := 0;
  SystemInfoMemo.Top := 0;
  SystemInfoMemo.Width := AboutSystemPage.SurfaceWidth;
  SystemInfoMemo.Height := AboutSystemPage.SurfaceHeight;
  SystemInfoMemo.ReadOnly := True;
  SystemInfoMemo.Color := clBtnFace;
  SystemInfoMemo.Font.Size := 11;
  SystemInfoMemo.Font.Name := 'Segoe UI';
  SystemInfoMemo.BorderStyle := bsNone;
  SystemInfoMemo.Alignment := taRightJustify;
  ApplyRTL(SystemInfoMemo);
  SystemInfoMemo.Lines.Add('مرحباً بك في برنامج منظومة إنجاز');
  SystemInfoMemo.Lines.Add('');
  SystemInfoMemo.Lines.Add('منظومة إنجاز هي نظام رقمي متكامل يهدف إلى تحسين إدارة العينات وضمان كفاءة ودقة إصدار الشهادات.');
  SystemInfoMemo.Lines.Add('تم تصميم النظام ليسرع وتيرة العمل اليومي في مختبرات القياس الإشعاعي مع ضمان أعلى معايير الجودة.');
  SystemInfoMemo.Lines.Add('');
  SystemInfoMemo.Lines.Add('أهداف النظام الرئيسية:');
  SystemInfoMemo.Lines.Add('- الأتمتة الكاملة لإدارة العينات.');
  SystemInfoMemo.Lines.Add('- تقليل الخطأ البشري في إدخال البيانات.');
  SystemInfoMemo.Lines.Add('- إصدار تقارير فنية وشهادات تحليل احترافية.');
  SystemInfoMemo.Lines.Add('- الأرشفة الإلكترونية والبحث السريع.');

  // Page 2: About the Designer (نبذة عن المصمم)
  AboutDesignerPage := CreateCustomPage(AboutSystemPage.ID, 'معلومات المصمم والمطور الفني', 'حقوق الملكية وتفاصيل التواصل');

  try
    ExtractTemporaryFile('designer_logo.bmp');
    DesignerLogoImage := TBitmapImage.Create(AboutDesignerPage);
    DesignerLogoImage.Parent := AboutDesignerPage.Surface;
    DesignerLogoImage.Bitmap.LoadFromFile(ExpandConstant('{tmp}\designer_logo.bmp'));
    // Huge designer logo (300x300) with breathing space
    DesignerLogoImage.Left := 15; 
    DesignerLogoImage.Top := (AboutDesignerPage.SurfaceHeight - 300) div 2;
    DesignerLogoImage.Width := 300;
    DesignerLogoImage.Height := 300;
    DesignerLogoImage.Stretch := True;
  except
  end;


  DesignerInfoMemo := TNewMemo.Create(AboutDesignerPage);
  DesignerInfoMemo.Parent := AboutDesignerPage.Surface;
  DesignerInfoMemo.Left := 330; // Shifted for massive logo
  DesignerInfoMemo.Top := 40;  // Center-aligned comfortably
  DesignerInfoMemo.Width := AboutDesignerPage.SurfaceWidth - 345;
  DesignerInfoMemo.Height := AboutDesignerPage.SurfaceHeight;
  DesignerInfoMemo.ReadOnly := True;
  DesignerInfoMemo.Color := clBtnFace;
  DesignerInfoMemo.Font.Size := 11;
  DesignerInfoMemo.Font.Name := 'Segoe UI';
  DesignerInfoMemo.BorderStyle := bsNone;
  DesignerInfoMemo.Alignment := taRightJustify;
  ApplyRTL(DesignerInfoMemo);
  DesignerInfoMemo.Lines.Add('تطوير المهندس: ادريس فتح الله الهرى');
  DesignerInfoMemo.Lines.Add('');
  DesignerInfoMemo.Lines.Add('تم تصميم هذا العمل لخدمة القطاع التقني والمختبري وفق أحدث التقنيات البرمجية لعام 2026.');
  DesignerInfoMemo.Lines.Add('');
  DesignerInfoMemo.Lines.Add('لطلب الدعم الفني أو التخصيص الإضافي:');
  DesignerInfoMemo.Lines.Add('الهاتف: 0925126355 - 0917730110');
  DesignerInfoMemo.Lines.Add('البريد: edreeselhery@gmail.com');
  DesignerInfoMemo.Lines.Add('');
  DesignerInfoMemo.Lines.Add('جميع الحقوق التقنية محفوظة © 2026');
end;
