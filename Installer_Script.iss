; ==========================================================
; Inno Setup Script for منظومة إنجاز الرقمية
; Developed by: Windows Installer Engineer
; Version: 1.00
; ==========================================================

#define MyAppName "منظومة إنجاز"
#define MyAppVersion "1.0.4"
#define MyAppPublisher "م. ادريس الهري"
#define MyAppExeName "Enjaz.exe"
#define OutputBase "C:\منظومة SETUP"

[Setup]
AppId={{D2A7E1C8-5B4F-4C8D-9E9A-3F8E1C2D4A5B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
SetupIconFile={#OutputBase}\app_icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
OutputDir={#OutputBase}
OutputBaseFilename=Setup_Enjaz_v1.0.4
PrivilegesRequired=admin

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#OutputBase}\Files\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKLM; Subkey: "Software\Enjaz"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey
