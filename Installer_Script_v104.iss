; ==========================================================
; Official Institutional Installer Script for "منظومة إنجاز"
; Optimized for .NET 8 WPF - Multi-Architecture (x86/x64)
; Version: 1.0.4 - Unified Edition
; ==========================================================

#define AppName "منظومة إنجاز لإدارة الشهادات"
#define AppVersion "1.0.4"
#define AppPublisher "م. ادريس فتح الله الهرى"
#define AppURL "edreeselhery@gmail.com"
#define AppExeName "Enjaz.exe"
#define SetupLocation "C:\منظومة SETUP"
#define SystemLogo SetupLocation + "\Assets\system_logo.png"
#define DesignerLogo SetupLocation + "\Assets\designer_logo.png"
#define SystemIcon SetupLocation + "\Assets\system_icon.ico"

[Setup]
; --- Application Identity ---
AppId={{C8E3F2A1-BD6E-4F3D-8A9C-D5F1A2B3C4D5}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppCopyright=Copyright (C) 2026
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes

; --- Branding & UI ---
WizardImageFile={#SystemLogo}
WizardSmallImageFile={#SystemLogo}
SetupIconFile={#SystemIcon}
UninstallDisplayIcon={app}\{#AppExeName}

; --- Institutional Standards ---
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64
DisableWelcomePage=no
OutputBaseFilename=Setup_Itaqan_System_v1.0.4_Universal
OutputDir={#SetupLocation}

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Dynamic Architecture Selection
Source: "{#SetupLocation}\Publish\x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: Is64BitInstallMode
Source: "{#SetupLocation}\Publish\x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not Is64BitInstallMode

; Support Files
Source: "{#SetupLocation}\README.md"; DestDir: "{app}"; Flags: isreadme
Source: "{#SetupLocation}\metadata.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#DesignerLogo}"; DestName: "designer_logo.png"; Flags: dontcopy

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  DesignerPage: TWizardPage;
  DesignerLogoImage: TBitmapImage;
  DesignerNameLabel, TitleLabel, NoteLabel, CopyrightLabel: TLabel;

procedure InitializeWizard();
var
  ContactLabel, EmailLabel, LocationLabel: TLabel;
begin
  // --- Create Custom Designer Information Page ---
  DesignerPage := CreateCustomPage(wpInfoBefore, 'معلومات المصمم والمطور', 'بيانات التواصل مع خبير تطوير النظم');
  
  // Designer Logo (Bundled in installer, extracted at runtime)
  try
    ExtractTemporaryFile('designer_logo.png');
    DesignerLogoImage := TBitmapImage.Create(DesignerPage);
    DesignerLogoImage.Parent := DesignerPage.Surface;
    DesignerLogoImage.Bitmap.LoadFromFile(ExpandConstant('{tmp}\designer_logo.png'));
    DesignerLogoImage.Left := 10;
    DesignerLogoImage.Top := 20;
    DesignerLogoImage.Width := 130;
    DesignerLogoImage.Height := 130;
    DesignerLogoImage.Stretch := True;
  except
    // If logo fails to load, the installer will still run without it
  end;

  DesignerNameLabel := TLabel.Create(DesignerPage);
  DesignerNameLabel.Parent := DesignerPage.Surface;
  DesignerNameLabel.Caption := 'المصمم والمطور: ادريس فتح الله الهرى';
  DesignerNameLabel.Font.Style := [fsBold];
  DesignerNameLabel.Font.Size := 12;
  DesignerNameLabel.AutoSize := True;
  DesignerNameLabel.Left := 140; // Position after logo
  DesignerNameLabel.Top := 20;

  ContactLabel := TLabel.Create(DesignerPage);
  ContactLabel.Parent := DesignerPage.Surface;
  ContactLabel.Caption := 'معلومات الاتصال: 0925126355 - 0917730110';
  ContactLabel.AutoSize := True;
  ContactLabel.Left := 140;
  ContactLabel.Top := DesignerNameLabel.Top + DesignerNameLabel.Height + 5;

  EmailLabel := TLabel.Create(DesignerPage);
  EmailLabel.Parent := DesignerPage.Surface;
  EmailLabel.Caption := 'البريد الإلكتروني: edreeselhery@gmail.com';
  EmailLabel.AutoSize := True;
  EmailLabel.Left := 140;
  EmailLabel.Top := ContactLabel.Top + ContactLabel.Height + 5;

  LocationLabel := TLabel.Create(DesignerPage);
  LocationLabel.Parent := DesignerPage.Surface;
  LocationLabel.Caption := 'بنغازي - ليبيا';
  LocationLabel.AutoSize := True;
  LocationLabel.Left := 140;
  LocationLabel.Top := EmailLabel.Top + EmailLabel.Height + 5;

  CopyrightLabel := TLabel.Create(DesignerPage);
  CopyrightLabel.Parent := DesignerPage.Surface;
  CopyrightLabel.Caption := 'جميع الحقوق التقنية محفوظة © 2026';
  CopyrightLabel.Font.Style := [fsBold];
  CopyrightLabel.AutoSize := True;
  CopyrightLabel.Left := 140;
  CopyrightLabel.Top := LocationLabel.Top + LocationLabel.Height + 15;
end;

function InitializeSetup(): Boolean;
begin
  // OS Version Check for Win 7+
  if GetWindowsVersion < $06010000 then
  begin
    MsgBox('هذه المنظومة تتطلب نظام تشغيل Windows 7 أو أحدث.', mbError, MB_OK);
    Result := False;
    Exit;
  end;
  Result := True;
end;
