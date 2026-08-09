; Ledgerly ERP installers (C# / .NET Framework 4.8)
; Build with: ISCC /DPackage=combined|client|server installer\ledgerly.iss
#ifndef Package
  #define Package "combined"
#endif

#define MyAppVersion "1.5.2"
#define MyAppPublisher "Ledgerly"
#define MyAppURL "https://github.com/devildog5x5/ERP"

#if Package == "client"
  #define MyAppName "Ledgerly Client"
  #define OutputName "LedgerlyClientSetup"
  #define AppIdGuid "{{A7B3C2D1-4E5F-6789-A0B1-C2D3E4F50617}"
  #define DefaultDir "{localappdata}\Programs\LedgerlyClient"
  #define UninstallIcon "{app}\Ledgerly.Client.exe"
  #define VersionDesc "Ledgerly ERP client installer"
#elif Package == "server"
  #define MyAppName "Ledgerly Server"
  #define OutputName "LedgerlyServerSetup"
  #define AppIdGuid "{{B8C4D3E2-5F60-789A-B1C2-D3E4F5061728}"
  #define DefaultDir "{localappdata}\Programs\LedgerlyServer"
  #define UninstallIcon "{app}\Ledgerly.Server.exe"
  #define VersionDesc "Ledgerly ERP server installer"
#else
  #define MyAppName "Ledgerly ERP"
  #define OutputName "LedgerlySetup"
  #define AppIdGuid "{{C9E5A1D4-6F70-4B13-9E4C-A2B1D3E5F708}"
  #define DefaultDir "{localappdata}\Programs\Ledgerly"
  #define UninstallIcon "{app}\Client\Ledgerly.Client.exe"
  #define VersionDesc "Ledgerly ERP client and server installer"
#endif

[Setup]
AppId={#AppIdGuid}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={#DefaultDir}
DefaultGroupName=Ledgerly
DisableProgramGroupPage=no
OutputDir=..\dist\installers
OutputBaseFilename={#OutputName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
MinVersion=6.1sp1
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={#UninstallIcon}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#VersionDesc}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
; Replace an existing install of this package (same AppId) instead of stacking copies.
UsePreviousAppDir=yes
UsePreviousGroup=yes
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

#if Package == "combined"
[Types]
Name: "full"; Description: "Full installation (Server and Client)"
Name: "server"; Description: "Server only"
Name: "client"; Description: "Client only"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Components]
Name: "server"; Description: "Ledgerly Server (API on http://127.0.0.1:8000)"; Types: full server custom; Flags: checkablealone
Name: "client"; Description: "Ledgerly Client (WPF desktop UI)"; Types: full client custom; Flags: checkablealone
#endif

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
#if Package == "combined"
Name: "autostartserver"; Description: "Start Ledgerly Server when I log in"; GroupDescription: "Startup options:"; Components: server; Flags: unchecked
Name: "autostartclient"; Description: "Start Ledgerly Client when I log in"; GroupDescription: "Startup options:"; Components: client; Flags: unchecked
#elif Package == "server"
Name: "autostartserver"; Description: "Start Ledgerly Server when I log in"; GroupDescription: "Startup options:"; Flags: unchecked
#else
Name: "autostartclient"; Description: "Start Ledgerly Client when I log in"; GroupDescription: "Startup options:"; Flags: unchecked
#endif

[Files]
#if Package == "combined"
Source: "..\dist\LedgerlyServer\*"; DestDir: "{app}\Server"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: server
Source: "..\dist\LedgerlyClient\*"; DestDir: "{app}\Client"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: client
#elif Package == "server"
Source: "..\dist\LedgerlyServer\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
#else
Source: "..\dist\LedgerlyClient\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
#endif

[Icons]
#if Package == "combined"
Name: "{group}\Ledgerly Server"; Filename: "{app}\Server\Ledgerly.Server.exe"; WorkingDir: "{app}\Server"; Components: server
Name: "{group}\Ledgerly Client"; Filename: "{app}\Client\Ledgerly.Client.exe"; WorkingDir: "{app}\Client"; Components: client
Name: "{group}\Uninstall Ledgerly ERP"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Ledgerly Server"; Filename: "{app}\Server\Ledgerly.Server.exe"; WorkingDir: "{app}\Server"; Tasks: desktopicon; Components: server
Name: "{autodesktop}\Ledgerly Client"; Filename: "{app}\Client\Ledgerly.Client.exe"; WorkingDir: "{app}\Client"; Tasks: desktopicon; Components: client
Name: "{userstartup}\Ledgerly Server"; Filename: "{app}\Server\Ledgerly.Server.exe"; WorkingDir: "{app}\Server"; Tasks: autostartserver; Components: server
Name: "{userstartup}\Ledgerly Client"; Filename: "{app}\Client\Ledgerly.Client.exe"; WorkingDir: "{app}\Client"; Tasks: autostartclient; Components: client
#elif Package == "server"
Name: "{group}\Ledgerly Server"; Filename: "{app}\Ledgerly.Server.exe"; WorkingDir: "{app}"
Name: "{group}\Uninstall Ledgerly Server"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Ledgerly Server"; Filename: "{app}\Ledgerly.Server.exe"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{userstartup}\Ledgerly Server"; Filename: "{app}\Ledgerly.Server.exe"; WorkingDir: "{app}"; Tasks: autostartserver
#else
Name: "{group}\Ledgerly Client"; Filename: "{app}\Ledgerly.Client.exe"; WorkingDir: "{app}"
Name: "{group}\Uninstall Ledgerly Client"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Ledgerly Client"; Filename: "{app}\Ledgerly.Client.exe"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{userstartup}\Ledgerly Client"; Filename: "{app}\Ledgerly.Client.exe"; WorkingDir: "{app}"; Tasks: autostartclient
#endif

[Run]
#if Package == "combined"
Filename: "{app}\Server\Ledgerly.Server.exe"; Description: "Launch Ledgerly Server now"; Flags: nowait postinstall skipifsilent unchecked; Components: server; WorkingDir: "{app}\Server"
Filename: "{app}\Client\Ledgerly.Client.exe"; Description: "Launch Ledgerly Client now"; Flags: nowait postinstall skipifsilent unchecked; Components: client; WorkingDir: "{app}\Client"
#elif Package == "server"
Filename: "{app}\Ledgerly.Server.exe"; Description: "Launch Ledgerly Server now"; Flags: nowait postinstall skipifsilent unchecked; WorkingDir: "{app}"
#else
Filename: "{app}\Ledgerly.Client.exe"; Description: "Launch Ledgerly Client now"; Flags: nowait postinstall skipifsilent unchecked; WorkingDir: "{app}"
#endif

; Keep %LOCALAPPDATA%\Ledgerly\* (database, server.json, client settings) across
; uninstall/reinstall so upgrades do not wipe company data. Use Settings → Refresh
; database when a clean slate is wanted.

[Code]
function IsDotNet48OrLater(): Boolean;
var
  Release: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then
    Result := Release >= 528040;
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

function GetUninstallRegKey(): String;
begin
  // Same AppId as [Setup]; uninstall key is AppId + _is1
  Result := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#AppIdGuid}_is1';
end;

function TryGetUninstallString(var UninstallString: String): Boolean;
var
  Key: String;
begin
  Key := GetUninstallRegKey();
  UninstallString := '';
  Result :=
    RegQueryStringValue(HKCU, Key, 'UninstallString', UninstallString) or
    RegQueryStringValue(HKLM, Key, 'UninstallString', UninstallString);
end;

procedure StopLedgerlyApps();
var
  ResultCode: Integer;
begin
  // Best-effort: unlock binaries before uninstall/replace
  Exec('taskkill.exe', '/F /IM Ledgerly.Client.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM Ledgerly.Server.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(500);
end;

procedure WaitForFileGone(const FileName: String; TimeoutMs: Integer);
var
  Elapsed: Integer;
begin
  Elapsed := 0;
  while (Elapsed < TimeoutMs) and FileExists(FileName) do
  begin
    Sleep(250);
    Elapsed := Elapsed + 250;
  end;
end;

function UninstallPreviousVersion(): Boolean;
var
  UninstallString: String;
  UninstallerPath: String;
  ResultCode: Integer;
begin
  Result := True;
  if not TryGetUninstallString(UninstallString) then
    exit;

  UninstallerPath := RemoveQuotes(UninstallString);
  if (UninstallerPath = '') or (not FileExists(UninstallerPath)) then
    exit;

  StopLedgerlyApps();

  // unins*.exe spawns a child and exits early; wait until the uninstaller file is gone.
  if not Exec(UninstallerPath, '/VERYSILENT /NORESTART /SUPPRESSMSGBOXES', '',
       SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := False;
    exit;
  end;

  WaitForFileGone(UninstallerPath, 60000);
  Sleep(500);
  Result := True;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  NeedsRestart := False;
  Result := '';
  StopLedgerlyApps();
  if not UninstallPreviousVersion() then
    Result := 'Could not uninstall the previous version of {#MyAppName}. ' +
      'Close Ledgerly completely, uninstall it from Apps & features, then run this installer again.';
end;

#if Package == "combined"
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
#endif
