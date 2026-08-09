; Ledgerly ERP — combined Client / Server installer (C# / .NET Framework 4.8)
#define MyAppName "Ledgerly ERP"
#define MyAppVersion "1.3.0"
#define MyAppPublisher "Ledgerly"
#define MyAppURL "https://github.com/devildog5x5/ERP"

[Setup]
AppId={{C9E5A1D4-6F70-4B13-9E4C-A2B1D3E5F708}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
; Per-user default so the installer runs without admin on the build PC.
DefaultDirName={localappdata}\Programs\Ledgerly
DefaultGroupName=Ledgerly
DisableProgramGroupPage=no
OutputDir=..\dist\installers
OutputBaseFilename=LedgerlySetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; Windows 7 SP1 and later (matches net48 target)
MinVersion=6.1sp1
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Native C# apps are x64 (SQLite / PlatformTarget)
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\Client\Ledgerly.Client.exe
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Ledgerly ERP client and server installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Full installation (Server and Client)"
Name: "server"; Description: "Server only"
Name: "client"; Description: "Client only"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Components]
Name: "server"; Description: "Ledgerly Server (API on http://127.0.0.1:8000)"; Types: full server custom; Flags: checkablealone
Name: "client"; Description: "Ledgerly Client (WPF desktop UI)"; Types: full client custom; Flags: checkablealone

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostartserver"; Description: "Start Ledgerly Server when I log in"; GroupDescription: "Startup options:"; Components: server; Flags: unchecked
Name: "autostartclient"; Description: "Start Ledgerly Client when I log in"; GroupDescription: "Startup options:"; Components: client; Flags: unchecked

[Files]
Source: "..\dist\LedgerlyServer\*"; DestDir: "{app}\Server"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: server
Source: "..\dist\LedgerlyClient\*"; DestDir: "{app}\Client"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: client

[Icons]
Name: "{group}\Ledgerly Server"; Filename: "{app}\Server\Ledgerly.Server.exe"; WorkingDir: "{app}\Server"; Components: server
Name: "{group}\Ledgerly Client"; Filename: "{app}\Client\Ledgerly.Client.exe"; WorkingDir: "{app}\Client"; Components: client
Name: "{group}\Uninstall Ledgerly ERP"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Ledgerly Server"; Filename: "{app}\Server\Ledgerly.Server.exe"; WorkingDir: "{app}\Server"; Tasks: desktopicon; Components: server
Name: "{autodesktop}\Ledgerly Client"; Filename: "{app}\Client\Ledgerly.Client.exe"; WorkingDir: "{app}\Client"; Tasks: desktopicon; Components: client
Name: "{userstartup}\Ledgerly Server"; Filename: "{app}\Server\Ledgerly.Server.exe"; WorkingDir: "{app}\Server"; Tasks: autostartserver; Components: server
Name: "{userstartup}\Ledgerly Client"; Filename: "{app}\Client\Ledgerly.Client.exe"; WorkingDir: "{app}\Client"; Tasks: autostartclient; Components: client

[Run]
Filename: "{app}\Server\Ledgerly.Server.exe"; Description: "Launch Ledgerly Server now"; Flags: nowait postinstall skipifsilent unchecked; Components: server; WorkingDir: "{app}\Server"
Filename: "{app}\Client\Ledgerly.Client.exe"; Description: "Launch Ledgerly Client now"; Flags: nowait postinstall skipifsilent unchecked; Components: client; WorkingDir: "{app}\Client"

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\Ledgerly\Server"
Type: filesandordirs; Name: "{localappdata}\Ledgerly\Client"

[Code]
function IsDotNet48OrLater(): Boolean;
var
  Release: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then
    Result := Release >= 528040; { 4.8 }
end;

function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  if (Version.Major < 6) or ((Version.Major = 6) and (Version.Minor < 1)) then
  begin
    MsgBox('Ledgerly ERP requires Windows 7 SP1 or later.', mbError, MB_OK);
    Result := False;
    exit;
  end;

  if not IsDotNet48OrLater() then
  begin
    MsgBox('Ledgerly ERP requires .NET Framework 4.8.'#13#10#13#10 +
      'Install it from:'#13#10 +
      'https://dotnet.microsoft.com/download/dotnet-framework/net48',
      mbError, MB_OK);
    Result := False;
    exit;
  end;

  Result := True;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpSelectComponents then
  begin
    if (not WizardIsComponentSelected('server')) and (not WizardIsComponentSelected('client')) then
    begin
      MsgBox('Select at least one component: Server and/or Client.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;
