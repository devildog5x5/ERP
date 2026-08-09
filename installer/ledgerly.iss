; Ledgerly ERP installers (C# / .NET Framework 4.8)
; Build with: ISCC /DPackage=combined|client|server installer\ledgerly.iss
; All three packages include Client + Server payloads and open with a loud
; Client / Server / Both chooser. Package only changes the EXE name and
; the default selection on that page.
#ifndef Package
  #define Package "combined"
#endif

#define MyAppVersion "1.5.3"
#define MyAppPublisher "Ledgerly"
#define MyAppURL "https://github.com/devildog5x5/ERP"
#define MyAppName "Ledgerly ERP"
#define AppIdGuid "{{C9E5A1D4-6F70-4B13-9E4C-A2B1D3E5F708}"
#define DefaultDir "{localappdata}\Programs\Ledgerly"
#define UninstallIcon "{app}\Client\Ledgerly.Client.exe"

#if Package == "client"
  #define OutputName "LedgerlyClientSetup"
  #define VersionDesc "Ledgerly ERP installer (Client / Server / Both)"
  #define DefaultRole "client"
#elif Package == "server"
  #define OutputName "LedgerlyServerSetup"
  #define VersionDesc "Ledgerly ERP installer (Client / Server / Both)"
  #define DefaultRole "server"
#else
  #define OutputName "LedgerlySetup"
  #define VersionDesc "Ledgerly ERP installer (Client / Server / Both)"
  #define DefaultRole "both"
#endif

; Legacy AppIds from older Client-only / Server-only packages (for upgrade cleanup)
#define LegacyClientAppId "{{A7B3C2D1-4E5F-6789-A0B1-C2D3E4F50617}"
#define LegacyServerAppId "{{B8C4D3E2-5F60-789A-B1C2-D3E4F5061728}"

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
UsePreviousAppDir=yes
UsePreviousGroup=yes
CloseApplications=yes
RestartApplications=no
ShowComponentSizes=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Both (Client and Server)"
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

; Keep %LOCALAPPDATA%\Ledgerly\* (database, server.json, client settings) across
; uninstall/reinstall so upgrades do not wipe company data. Use Settings → Refresh
; database when a clean slate is wanted.

[Code]
var
  RolePage: TWizardPage;
  RoleHeadline: TNewStaticText;
  RoleSubhead: TNewStaticText;
  RoleBoth: TRadioButton;
  RoleClient: TRadioButton;
  RoleServer: TRadioButton;
  RoleHintBoth: TNewStaticText;
  RoleHintClient: TNewStaticText;
  RoleHintServer: TNewStaticText;

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

function UninstallRegKeyForAppId(const AppIdGuid: String): String;
begin
  Result := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + AppIdGuid + '_is1';
end;

function TryGetUninstallStringForAppId(const AppIdGuid: String; var UninstallString: String): Boolean;
var
  Key: String;
begin
  Key := UninstallRegKeyForAppId(AppIdGuid);
  UninstallString := '';
  Result :=
    RegQueryStringValue(HKCU, Key, 'UninstallString', UninstallString) or
    RegQueryStringValue(HKLM, Key, 'UninstallString', UninstallString);
end;

procedure StopLedgerlyApps();
var
  ResultCode: Integer;
begin
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

function UninstallByAppId(const AppIdGuid: String): Boolean;
var
  UninstallString: String;
  UninstallerPath: String;
  ResultCode: Integer;
begin
  Result := True;
  if not TryGetUninstallStringForAppId(AppIdGuid, UninstallString) then
    exit;

  UninstallerPath := RemoveQuotes(UninstallString);
  if (UninstallerPath = '') or (not FileExists(UninstallerPath)) then
    exit;

  StopLedgerlyApps();

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

function UninstallPreviousVersions(): Boolean;
begin
  // Current unified package + older Client-only / Server-only AppIds
  Result :=
    UninstallByAppId('{#AppIdGuid}') and
    UninstallByAppId('{#LegacyClientAppId}') and
    UninstallByAppId('{#LegacyServerAppId}');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  NeedsRestart := False;
  Result := '';
  StopLedgerlyApps();
  if not UninstallPreviousVersions() then
    Result := 'Could not uninstall a previous Ledgerly install. ' +
      'Close Ledgerly completely, uninstall it from Apps & features, then run this installer again.';
end;

procedure ApplyRoleSelection();
begin
  if RoleBoth.Checked then
    WizardSelectComponents('server,client')
  else if RoleClient.Checked then
    WizardSelectComponents('client,!server')
  else
    WizardSelectComponents('server,!client');
end;

procedure InitializeWizard();
var
  TopY: Integer;
begin
  RolePage := CreateCustomPage(wpWelcome,
    'WHAT DO YOU WANT TO INSTALL?',
    'Choose Client, Server, or BOTH before anything else.');

  RoleHeadline := TNewStaticText.Create(RolePage);
  RoleHeadline.Parent := RolePage.Surface;
  RoleHeadline.Caption := 'INSTALL CLIENT, SERVER, OR BOTH';
  RoleHeadline.Font.Name := 'Segoe UI';
  RoleHeadline.Font.Size := 16;
  RoleHeadline.Font.Style := [fsBold];
  RoleHeadline.Font.Color := clMaroon;
  RoleHeadline.AutoSize := True;
  RoleHeadline.Left := 0;
  RoleHeadline.Top := 0;

  RoleSubhead := TNewStaticText.Create(RolePage);
  RoleSubhead.Parent := RolePage.Surface;
  RoleSubhead.Caption := 'Pick one option below. This is the most important step.';
  RoleSubhead.Font.Name := 'Segoe UI';
  RoleSubhead.Font.Size := 11;
  RoleSubhead.Font.Style := [fsBold];
  RoleSubhead.Font.Color := clWindowText;
  RoleSubhead.AutoSize := True;
  RoleSubhead.Left := 0;
  RoleSubhead.Top := RoleHeadline.Top + RoleHeadline.Height + ScaleY(8);

  TopY := RoleSubhead.Top + RoleSubhead.Height + ScaleY(18);

  RoleBoth := TRadioButton.Create(RolePage);
  RoleBoth.Parent := RolePage.Surface;
  RoleBoth.Caption := 'BOTH  —  Client and Server (recommended)';
  RoleBoth.Font.Name := 'Segoe UI';
  RoleBoth.Font.Size := 12;
  RoleBoth.Font.Style := [fsBold];
  RoleBoth.Left := ScaleX(4);
  RoleBoth.Top := TopY;
  RoleBoth.Width := RolePage.SurfaceWidth - ScaleX(8);
  RoleBoth.Height := ScaleY(24);
  RoleBoth.Checked := '{#DefaultRole}' = 'both';

  RoleHintBoth := TNewStaticText.Create(RolePage);
  RoleHintBoth.Parent := RolePage.Surface;
  RoleHintBoth.Caption := 'Full ERP on this PC: API/database host + desktop UI.';
  RoleHintBoth.Font.Name := 'Segoe UI';
  RoleHintBoth.Font.Size := 9;
  RoleHintBoth.Left := ScaleX(28);
  RoleHintBoth.Top := RoleBoth.Top + RoleBoth.Height + ScaleY(2);
  RoleHintBoth.Width := RolePage.SurfaceWidth - ScaleX(32);
  RoleHintBoth.WordWrap := True;

  TopY := RoleHintBoth.Top + RoleHintBoth.Height + ScaleY(16);

  RoleClient := TRadioButton.Create(RolePage);
  RoleClient.Parent := RolePage.Surface;
  RoleClient.Caption := 'CLIENT ONLY  —  Desktop UI';
  RoleClient.Font.Name := 'Segoe UI';
  RoleClient.Font.Size := 12;
  RoleClient.Font.Style := [fsBold];
  RoleClient.Left := ScaleX(4);
  RoleClient.Top := TopY;
  RoleClient.Width := RolePage.SurfaceWidth - ScaleX(8);
  RoleClient.Height := ScaleY(24);
  RoleClient.Checked := '{#DefaultRole}' = 'client';

  RoleHintClient := TNewStaticText.Create(RolePage);
  RoleHintClient.Parent := RolePage.Surface;
  RoleHintClient.Caption := 'Installs the WPF client. A Ledgerly Server must already be running somewhere.';
  RoleHintClient.Font.Name := 'Segoe UI';
  RoleHintClient.Font.Size := 9;
  RoleHintClient.Left := ScaleX(28);
  RoleHintClient.Top := RoleClient.Top + RoleClient.Height + ScaleY(2);
  RoleHintClient.Width := RolePage.SurfaceWidth - ScaleX(32);
  RoleHintClient.WordWrap := True;

  TopY := RoleHintClient.Top + RoleHintClient.Height + ScaleY(16);

  RoleServer := TRadioButton.Create(RolePage);
  RoleServer.Parent := RolePage.Surface;
  RoleServer.Caption := 'SERVER ONLY  —  API and database';
  RoleServer.Font.Name := 'Segoe UI';
  RoleServer.Font.Size := 12;
  RoleServer.Font.Style := [fsBold];
  RoleServer.Left := ScaleX(4);
  RoleServer.Top := TopY;
  RoleServer.Width := RolePage.SurfaceWidth - ScaleX(8);
  RoleServer.Height := ScaleY(24);
  RoleServer.Checked := '{#DefaultRole}' = 'server';

  RoleHintServer := TNewStaticText.Create(RolePage);
  RoleHintServer.Parent := RolePage.Surface;
  RoleHintServer.Caption := 'Installs the API host (default http://127.0.0.1:8000). Clients connect to it.';
  RoleHintServer.Font.Name := 'Segoe UI';
  RoleHintServer.Font.Size := 9;
  RoleHintServer.Left := ScaleX(28);
  RoleHintServer.Top := RoleServer.Top + RoleServer.Height + ScaleY(2);
  RoleHintServer.Width := RolePage.SurfaceWidth - ScaleX(32);
  RoleHintServer.WordWrap := True;

  // Ensure a default is selected even if preprocessor string compare fails.
  if (not RoleBoth.Checked) and (not RoleClient.Checked) and (not RoleServer.Checked) then
    RoleBoth.Checked := True;

  ApplyRoleSelection();
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  // Loud custom page replaces the stock component list.
  Result := PageID = wpSelectComponents;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = RolePage.ID then
  begin
    if (not RoleBoth.Checked) and (not RoleClient.Checked) and (not RoleServer.Checked) then
    begin
      MsgBox('Choose CLIENT, SERVER, or BOTH before continuing.', mbError, MB_OK);
      Result := False;
      exit;
    end;
    ApplyRoleSelection();
  end;
end;
