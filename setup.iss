; LiteQMS — Inno Setup Installer
; Requires: publish.bat run first (produces .\dist\LiteQMS.exe)

#define MyAppName "LiteQMS"
#define MyAppVersion "1.0.3"
#define MyAppPublisher "LiteQMS"
#define MyAppURL "http://localhost:5000"
#define MyAppExeName "LiteQMS.exe"

[Setup]
AppId={{B8F4C3A2-1D5E-4F6A-9B7C-8D2E3F1A4B5C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
SetupIconFile=.\liteqms.ico
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=.\dist
OutputBaseFilename=LiteQMS-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Files]
Source: ".\dist\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.db"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"

[Run]
Filename: "powershell"; Parameters: "-Command ""Get-NetConnectionProfile | Where-Object NetworkCategory -EQ 'Public' | Set-NetConnectionProfile -NetworkCategory Private"""; Flags: runhidden
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""LiteQMS"" dir=in action=allow protocol=TCP localport=5000"; Flags: runhidden
Filename: "netsh"; Parameters: "http add urlacl url=http://+:5000/ user=BUILTIN\Users"; Flags: runhidden
Filename: "{app}\{#MyAppExeName}"; Description: "Launch LiteQMS"; Flags: postinstall nowait skipifsilent shellexec

[UninstallRun]
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""LiteQMS"""; Flags: runhidden
Filename: "netsh"; Parameters: "http delete urlacl url=http://+:5000/"; Flags: runhidden

[UninstallDelete]
; Remove logs from app folder if any
Type: files; Name: "{app}\*.log"
Type: dirifempty; Name: "{app}"

[Dirs]
; Ensure app directory exists
Name: "{app}"

[Code]
function InitializeUninstall: Boolean;
begin
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Leave %APPDATA%\LiteQMS\ untouched (DB preserved)
  end;
end;
