; Coalesce installers (C# / .NET Framework 4.8)
; Build with: ISCC /DPackage=combined|client|server installer\coalesce.iss
#ifndef Package
  #define Package "combined"
#endif

#define MyAppVersion "1.6.10"
#define MyAppPublisher "Coalesce"
#define MyAppURL "https://github.com/devildog5x5/ERP"
#define MyAppName "Coalesce"
#define AppIdGuid "{{D0F6B2E5-7A81-4C24-AF5D-B3C2E4F60719}"
#define DefaultDir "{localappdata}\Programs\Coalesce"
#define UninstallIcon "{app}\Client\Coalesce.Client.exe"

#if Package == "client"
  #define OutputName "CoalesceClientSetup"
  #define VersionDesc "Coalesce installer (Client / Server / Both)"
  #define DefaultRole "client"
#elif Package == "server"
  #define OutputName "CoalesceServerSetup"
  #define VersionDesc "Coalesce installer (Client / Server / Both)"
  #define DefaultRole "server"
#else
  #define OutputName "CoalesceSetup"
  #define VersionDesc "Coalesce installer (Client / Server / Both)"
  #define DefaultRole "both"
#endif

; Prior Ledgerly AppIds (uninstall on upgrade)
#define LegacyUnifiedAppId "{{C9E5A1D4-6F70-4B13-9E4C-A2B1D3E5F708}"
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
DefaultGroupName=Coalesce
DisableProgramGroupPage=no
OutputDir=..\dist\installers
OutputBaseFilename={#OutputName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\assets\coalesce.ico
UninstallDisplayIcon={#UninstallIcon}
MinVersion=6.1sp1
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
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
Name: "server"; Description: "Coalesce Server (API on http://127.0.0.1:8000)"; Types: full server custom; Flags: checkablealone
Name: "client"; Description: "Coalesce Client (WPF desktop UI)"; Types: full client custom; Flags: checkablealone

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostartserver"; Description: "Start Coalesce Server when I log in"; GroupDescription: "Startup options:"; Components: server; Flags: unchecked
Name: "autostartclient"; Description: "Start Coalesce Client when I log in"; GroupDescription: "Startup options:"; Components: client; Flags: unchecked

[Files]
Source: "..\dist\CoalesceServer\*"; DestDir: "{app}\Server"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: server
Source: "..\dist\CoalesceClient\*"; DestDir: "{app}\Client"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: client

[Icons]
Name: "{group}\Coalesce Server"; Filename: "{app}\Server\Coalesce.Server.exe"; WorkingDir: "{app}\Server"; Components: server
Name: "{group}\Coalesce Client"; Filename: "{app}\Client\Coalesce.Client.exe"; WorkingDir: "{app}\Client"; Components: client
Name: "{group}\Uninstall Coalesce"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Coalesce Server"; Filename: "{app}\Server\Coalesce.Server.exe"; WorkingDir: "{app}\Server"; Tasks: desktopicon; Components: server
Name: "{autodesktop}\Coalesce Client"; Filename: "{app}\Client\Coalesce.Client.exe"; WorkingDir: "{app}\Client"; Tasks: desktopicon; Components: client
Name: "{userstartup}\Coalesce Server"; Filename: "{app}\Server\Coalesce.Server.exe"; WorkingDir: "{app}\Server"; Tasks: autostartserver; Components: server
Name: "{userstartup}\Coalesce Client"; Filename: "{app}\Client\Coalesce.Client.exe"; WorkingDir: "{app}\Client"; Tasks: autostartclient; Components: client

[Run]
Filename: "{app}\Server\Coalesce.Server.exe"; Description: "Launch Coalesce Server now"; Flags: nowait postinstall skipifsilent unchecked; Components: server; WorkingDir: "{app}\Server"
Filename: "{app}\Client\Coalesce.Client.exe"; Description: "Launch Coalesce Client now"; Flags: nowait postinstall skipifsilent unchecked; Components: client; WorkingDir: "{app}\Client"

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
    MsgBox('Coalesce requires Windows 7 SP1 or later.', mbError, MB_OK);
    Result := False;
    exit;
  end;

  if not IsDotNet48OrLater() then
  begin
    MsgBox('Coalesce requires .NET Framework 4.8.'#13#10#13#10 +
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

procedure StopApps();
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM Coalesce.Client.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM Coalesce.Server.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
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

  StopApps();

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
  Result :=
    UninstallByAppId('{#AppIdGuid}') and
    UninstallByAppId('{#LegacyUnifiedAppId}') and
    UninstallByAppId('{#LegacyClientAppId}') and
    UninstallByAppId('{#LegacyServerAppId}');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  NeedsRestart := False;
  Result := '';
  StopApps();
  if not UninstallPreviousVersions() then
    Result := 'Could not uninstall a previous Coalesce/Ledgerly install. ' +
      'Close the apps, uninstall from Apps & features, then run this installer again.';
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
  RoleSubhead.Caption := 'Where ERP and CRM come together - pick one option below.';
  RoleSubhead.Font.Name := 'Segoe UI';
  RoleSubhead.Font.Size := 11;
  RoleSubhead.Font.Style := [fsBold];
  RoleSubhead.AutoSize := True;
  RoleSubhead.Left := 0;
  RoleSubhead.Top := RoleHeadline.Top + RoleHeadline.Height + ScaleY(8);

  TopY := RoleSubhead.Top + RoleSubhead.Height + ScaleY(18);

  RoleBoth := TRadioButton.Create(RolePage);
  RoleBoth.Parent := RolePage.Surface;
  RoleBoth.Caption := 'BOTH  -  Client and Server (recommended)';
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
  RoleHintBoth.Caption := 'Full Coalesce on this PC: API/database host + desktop UI.';
  RoleHintBoth.Font.Name := 'Segoe UI';
  RoleHintBoth.Font.Size := 9;
  RoleHintBoth.Left := ScaleX(28);
  RoleHintBoth.Top := RoleBoth.Top + RoleBoth.Height + ScaleY(2);
  RoleHintBoth.Width := RolePage.SurfaceWidth - ScaleX(32);
  RoleHintBoth.WordWrap := True;

  TopY := RoleHintBoth.Top + RoleHintBoth.Height + ScaleY(16);

  RoleClient := TRadioButton.Create(RolePage);
  RoleClient.Parent := RolePage.Surface;
  RoleClient.Caption := 'CLIENT ONLY  -  Desktop UI';
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
  RoleHintClient.Caption := 'Installs the WPF client. A Coalesce Server must already be running somewhere.';
  RoleHintClient.Font.Name := 'Segoe UI';
  RoleHintClient.Font.Size := 9;
  RoleHintClient.Left := ScaleX(28);
  RoleHintClient.Top := RoleClient.Top + RoleClient.Height + ScaleY(2);
  RoleHintClient.Width := RolePage.SurfaceWidth - ScaleX(32);
  RoleHintClient.WordWrap := True;

  TopY := RoleHintClient.Top + RoleHintClient.Height + ScaleY(16);

  RoleServer := TRadioButton.Create(RolePage);
  RoleServer.Parent := RolePage.Surface;
  RoleServer.Caption := 'SERVER ONLY  -  API and database';
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

  if (not RoleBoth.Checked) and (not RoleClient.Checked) and (not RoleServer.Checked) then
    RoleBoth.Checked := True;

  ApplyRoleSelection();
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
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
