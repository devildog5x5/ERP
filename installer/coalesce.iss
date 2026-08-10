; Coalesce installers (C# / .NET Framework 4.8)
; Build with: ISCC /DPackage=combined|client|server installer\coalesce.iss
#ifndef Package
  #define Package "combined"
#endif

#define MyAppVersion "1.6.14"
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
  DbSizePage: TWizardPage;
  DbSizeHeadline: TNewStaticText;
  DbSizeSubhead: TNewStaticText;
  DbSizeSmall: TRadioButton;
  DbSizeMedium: TRadioButton;
  DbSizeLarge: TRadioButton;
  DbSizeCustom: TRadioButton;
  DbSizeCustomLabel: TNewStaticText;
  DbSizeCustomEdit: TNewEdit;
  DbSizeHint: TNewStaticText;

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

  { Database size page — shown when Server is included }
  DbSizePage := CreateCustomPage(RolePage.ID,
    'DATABASE SIZE',
    'Choose how large you expect this Coalesce database to grow.');

  DbSizeHeadline := TNewStaticText.Create(DbSizePage);
  DbSizeHeadline.Parent := DbSizePage.Surface;
  DbSizeHeadline.Caption := 'PLANNED DATABASE SIZE';
  DbSizeHeadline.Font.Name := 'Segoe UI';
  DbSizeHeadline.Font.Size := 16;
  DbSizeHeadline.Font.Style := [fsBold];
  DbSizeHeadline.Font.Color := clMaroon;
  DbSizeHeadline.AutoSize := True;
  DbSizeHeadline.Left := 0;
  DbSizeHeadline.Top := 0;

  DbSizeSubhead := TNewStaticText.Create(DbSizePage);
  DbSizeSubhead.Parent := DbSizePage.Surface;
  DbSizeSubhead.Caption :=
    'Coalesce starts on a local SQLite file. This sets your planned capacity for status warnings (not a hard engine limit).';
  DbSizeSubhead.Font.Name := 'Segoe UI';
  DbSizeSubhead.Font.Size := 9;
  DbSizeSubhead.AutoSize := False;
  DbSizeSubhead.WordWrap := True;
  DbSizeSubhead.Left := 0;
  DbSizeSubhead.Top := DbSizeHeadline.Top + DbSizeHeadline.Height + ScaleY(8);
  DbSizeSubhead.Width := DbSizePage.SurfaceWidth;
  DbSizeSubhead.Height := ScaleY(36);

  TopY := DbSizeSubhead.Top + DbSizeSubhead.Height + ScaleY(12);

  DbSizeSmall := TRadioButton.Create(DbSizePage);
  DbSizeSmall.Parent := DbSizePage.Surface;
  DbSizeSmall.Caption := 'Small  —  up to about 500 MB (light single-PC use)';
  DbSizeSmall.Font.Name := 'Segoe UI';
  DbSizeSmall.Font.Size := 11;
  DbSizeSmall.Left := ScaleX(4);
  DbSizeSmall.Top := TopY;
  DbSizeSmall.Width := DbSizePage.SurfaceWidth - ScaleX(8);
  DbSizeSmall.Height := ScaleY(22);

  TopY := DbSizeSmall.Top + DbSizeSmall.Height + ScaleY(10);

  DbSizeMedium := TRadioButton.Create(DbSizePage);
  DbSizeMedium.Parent := DbSizePage.Surface;
  DbSizeMedium.Caption := 'Medium  —  up to about 2 GB (recommended default)';
  DbSizeMedium.Font.Name := 'Segoe UI';
  DbSizeMedium.Font.Size := 11;
  DbSizeMedium.Font.Style := [fsBold];
  DbSizeMedium.Left := ScaleX(4);
  DbSizeMedium.Top := TopY;
  DbSizeMedium.Width := DbSizePage.SurfaceWidth - ScaleX(8);
  DbSizeMedium.Height := ScaleY(22);
  DbSizeMedium.Checked := True;

  TopY := DbSizeMedium.Top + DbSizeMedium.Height + ScaleY(10);

  DbSizeLarge := TRadioButton.Create(DbSizePage);
  DbSizeLarge.Parent := DbSizePage.Surface;
  DbSizeLarge.Caption := 'Large  —  up to about 10 GB (busy warehouse / many years of history)';
  DbSizeLarge.Font.Name := 'Segoe UI';
  DbSizeLarge.Font.Size := 11;
  DbSizeLarge.Left := ScaleX(4);
  DbSizeLarge.Top := TopY;
  DbSizeLarge.Width := DbSizePage.SurfaceWidth - ScaleX(8);
  DbSizeLarge.Height := ScaleY(22);

  TopY := DbSizeLarge.Top + DbSizeLarge.Height + ScaleY(10);

  DbSizeCustom := TRadioButton.Create(DbSizePage);
  DbSizeCustom.Parent := DbSizePage.Surface;
  DbSizeCustom.Caption := 'Custom size (megabytes)';
  DbSizeCustom.Font.Name := 'Segoe UI';
  DbSizeCustom.Font.Size := 11;
  DbSizeCustom.Left := ScaleX(4);
  DbSizeCustom.Top := TopY;
  DbSizeCustom.Width := DbSizePage.SurfaceWidth - ScaleX(8);
  DbSizeCustom.Height := ScaleY(22);

  DbSizeCustomLabel := TNewStaticText.Create(DbSizePage);
  DbSizeCustomLabel.Parent := DbSizePage.Surface;
  DbSizeCustomLabel.Caption := 'MB:';
  DbSizeCustomLabel.Font.Name := 'Segoe UI';
  DbSizeCustomLabel.Font.Size := 10;
  DbSizeCustomLabel.Left := ScaleX(28);
  DbSizeCustomLabel.Top := DbSizeCustom.Top + DbSizeCustom.Height + ScaleY(6);
  DbSizeCustomLabel.AutoSize := True;

  DbSizeCustomEdit := TNewEdit.Create(DbSizePage);
  DbSizeCustomEdit.Parent := DbSizePage.Surface;
  DbSizeCustomEdit.Left := DbSizeCustomLabel.Left + DbSizeCustomLabel.Width + ScaleX(8);
  DbSizeCustomEdit.Top := DbSizeCustomLabel.Top - ScaleY(2);
  DbSizeCustomEdit.Width := ScaleX(100);
  DbSizeCustomEdit.Text := '4096';

  DbSizeHint := TNewStaticText.Create(DbSizePage);
  DbSizeHint.Parent := DbSizePage.Surface;
  DbSizeHint.Caption :=
    'Tip: for multi-user or hosted SQL Server / MySQL / PostgreSQL, install with Medium, then use Settings → Grow database… after setup.';
  DbSizeHint.Font.Name := 'Segoe UI';
  DbSizeHint.Font.Size := 9;
  DbSizeHint.Font.Color := clGray;
  DbSizeHint.AutoSize := False;
  DbSizeHint.WordWrap := True;
  DbSizeHint.Left := 0;
  DbSizeHint.Top := DbSizeCustomEdit.Top + DbSizeCustomEdit.Height + ScaleY(16);
  DbSizeHint.Width := DbSizePage.SurfaceWidth;
  DbSizeHint.Height := ScaleY(40);
end;

function ServerComponentSelected(): Boolean;
begin
  Result := WizardIsComponentSelected('server');
end;

function SelectedDatabaseSizeMb(): Integer;
var
  CustomMb: Integer;
begin
  if DbSizeSmall.Checked then
    Result := 500
  else if DbSizeLarge.Checked then
    Result := 10240
  else if DbSizeCustom.Checked then
  begin
    CustomMb := StrToIntDef(Trim(DbSizeCustomEdit.Text), 0);
    Result := CustomMb;
  end
  else
    Result := 2048; { Medium }
end;

function SelectedCapacityProfile(): String;
begin
  if DbSizeSmall.Checked then
    Result := 'Small'
  else if DbSizeLarge.Checked then
    Result := 'Large'
  else if DbSizeCustom.Checked then
    Result := 'Custom'
  else
    Result := 'Medium';
end;

function JsonEscapePath(const Path: String): String;
var
  I: Integer;
begin
  Result := '';
  for I := 1 to Length(Path) do
  begin
    if Path[I] = '\' then
      Result := Result + '\\'
    else if Path[I] = '"' then
      Result := Result + '\"'
    else
      Result := Result + Path[I];
  end;
end;

function BuildServerJsonText(SizeMb: Integer; const Profile: String): String;
var
  DbPath: String;
begin
  DbPath := ExpandConstant('{localappdata}\Coalesce\Server\coalesce.db');
  Result :=
    '{' + #13#10 +
    '  "Provider": "Sqlite",' + #13#10 +
    '  "ConnectionString": "Data Source=' + JsonEscapePath(DbPath) + '",' + #13#10 +
    '  "ListenUrl": "http://127.0.0.1:8000/",' + #13#10 +
    '  "DatabaseSizeMb": ' + IntToStr(SizeMb) + ',' + #13#10 +
    '  "CapacityProfile": "' + Profile + '"' + #13#10 +
    '}';
end;

procedure WriteCapacityConfig();
var
  Dir, ServerJson, ScriptPath, Script: String;
  SizeMb: Integer;
  Profile: String;
  ResultCode: Integer;
begin
  if not ServerComponentSelected() then
    exit;

  Dir := ExpandConstant('{localappdata}\Coalesce\Server');
  if not DirExists(Dir) then
    ForceDirectories(Dir);

  SizeMb := SelectedDatabaseSizeMb();
  if SizeMb < 100 then
    SizeMb := 2048;
  Profile := SelectedCapacityProfile();
  ServerJson := Dir + '\server.json';
  ScriptPath := ExpandConstant('{tmp}\coalesce-set-capacity.ps1');

  { Create or patch server.json quietly in the background }
  Script :=
    '$ErrorActionPreference = ''Stop''' + #13#10 +
    '$dir = ''' + Dir + '''' + #13#10 +
    '$path = Join-Path $dir ''server.json''' + #13#10 +
    '$db = Join-Path $dir ''coalesce.db''' + #13#10 +
    '$size = ' + IntToStr(SizeMb) + #13#10 +
    '$profile = ''' + Profile + '''' + #13#10 +
    'if (Test-Path -LiteralPath $path) {' + #13#10 +
    '  $j = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json' + #13#10 +
    '} else {' + #13#10 +
    '  $j = [pscustomobject]@{' + #13#10 +
    '    Provider = ''Sqlite''' + #13#10 +
    '    ConnectionString = (''Data Source={0}'' -f $db)' + #13#10 +
    '    ListenUrl = ''http://127.0.0.1:8000/''' + #13#10 +
    '  }' + #13#10 +
    '}' + #13#10 +
    'if (-not $j.PSObject.Properties[''Provider'']) { $j | Add-Member Provider ''Sqlite'' }' + #13#10 +
    'if (-not $j.PSObject.Properties[''ConnectionString''] -or [string]::IsNullOrWhiteSpace([string]$j.ConnectionString)) {' + #13#10 +
    '  $j | Add-Member ConnectionString (''Data Source={0}'' -f $db) -Force' + #13#10 +
    '}' + #13#10 +
    'if (-not $j.PSObject.Properties[''ListenUrl''] -or [string]::IsNullOrWhiteSpace([string]$j.ListenUrl)) {' + #13#10 +
    '  $j | Add-Member ListenUrl ''http://127.0.0.1:8000/'' -Force' + #13#10 +
    '}' + #13#10 +
    '$j | Add-Member DatabaseSizeMb $size -Force' + #13#10 +
    '$j | Add-Member CapacityProfile $profile -Force' + #13#10 +
    '($j | ConvertTo-Json -Depth 5) + [Environment]::NewLine | Set-Content -LiteralPath $path -Encoding UTF8' + #13#10 +
    'Remove-Item -LiteralPath (Join-Path $dir ''capacity.json'') -ErrorAction SilentlyContinue' + #13#10;

  if not SaveStringToFile(ScriptPath, Script, False) then
  begin
    Log('Warning: could not write capacity PowerShell script');
    if not FileExists(ServerJson) then
      SaveStringToFile(ServerJson, BuildServerJsonText(SizeMb, Profile) + #13#10, False);
  end
  else if not Exec('powershell.exe',
      '-NoProfile -ExecutionPolicy Bypass -File "' + ScriptPath + '"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
  begin
    Log('Warning: PowerShell capacity update failed (code ' + IntToStr(ResultCode) + '); writing server.json fallback');
    if not FileExists(ServerJson) then
      SaveStringToFile(ServerJson, BuildServerJsonText(SizeMb, Profile) + #13#10, False);
  end
  else
    Log('Updated ' + ServerJson + ' with DatabaseSizeMb=' + IntToStr(SizeMb));
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if PageID = wpSelectComponents then
    Result := True
  else if (DbSizePage <> nil) and (PageID = DbSizePage.ID) then
    Result := not ServerComponentSelected();
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  SizeMb: Integer;
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
  end
  else if (DbSizePage <> nil) and (CurPageID = DbSizePage.ID) then
  begin
    SizeMb := SelectedDatabaseSizeMb();
    if SizeMb < 100 then
    begin
      MsgBox('Enter a custom database size of at least 100 MB.', mbError, MB_OK);
      Result := False;
      exit;
    end;
    if SizeMb > 1048576 then
    begin
      MsgBox('Custom database size cannot exceed 1,048,576 MB (1 TB).', mbError, MB_OK);
      Result := False;
      exit;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    WriteCapacityConfig();
end;
