; Coalesce installers (C# / .NET Framework 4.8)
; Build: ISCC /DPackage=combined|client|server installer\coalesce.iss
;
;   combined → CoalesceSetup.exe       chooser: Both / Server / Client
;   client   → CoalesceClientSetup.exe Client payload only
;   server   → CoalesceServerSetup.exe Server payload only
;
#ifndef Package
  #define Package "combined"
#endif

#include "version.iss"

#define AppIdGuid "{{D0F6B2E5-7A81-4C24-AF5D-B3C2E4F60719}"
#define DefaultDir "{localappdata}\Programs\Coalesce"

; Old Ledgerly AppIds — still uninstalled on upgrade
#define LegacyUnifiedAppId "{{C9E5A1D4-6F70-4B13-9E4C-A2B1D3E5F708}"
#define LegacyClientAppId "{{A7B3C2D1-4E5F-6789-A0B1-C2D3E4F50617}"
#define LegacyServerAppId "{{B8C4D3E2-5F60-789A-B1C2-D3E4F5061728}"

#if Package == "client"
  #define MyAppName "Coalesce Client"
  #define OutputName "CoalesceClientSetup"
  #define VersionDesc "Coalesce Client installer"
  #define InfoBefore "info-client.txt"
  #define UninstallIcon "{app}\Client\Coalesce.Client.exe"
  #define IsChooser "0"
  #define HasServer "0"
  #define HasClient "1"
#elif Package == "server"
  #define MyAppName "Coalesce Server"
  #define OutputName "CoalesceServerSetup"
  #define VersionDesc "Coalesce Server installer"
  #define InfoBefore "info-server.txt"
  #define UninstallIcon "{app}\Server\Coalesce.Server.exe"
  #define IsChooser "0"
  #define HasServer "1"
  #define HasClient "0"
#else
  #define MyAppName "Coalesce"
  #define OutputName "CoalesceSetup"
  #define VersionDesc "Coalesce installer (Client / Server / Both)"
  #define InfoBefore "info-combined.txt"
  #define UninstallIcon "{app}\Client\Coalesce.Client.exe"
  #define IsChooser "1"
  #define HasServer "1"
  #define HasClient "1"
#endif

[Setup]
AppId={#AppIdGuid}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
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
AlwaysShowComponentsList=no
InfoBeforeFile={#InfoBefore}
SetupMutex=Coalesce_ERP_Setup_Mutex

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
SetupAppRunningError=Coalesce Setup is already running.%n%nClose the other installer window, then try again.

#if IsChooser == "1"
; Silent: CoalesceSetup.exe /TYPE=full|server|client
[Types]
Name: "full"; Description: "Both (Client and Server)"
Name: "server"; Description: "Server only"
Name: "client"; Description: "Client only"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Components]
Name: "server"; Description: "Coalesce Server (API on http://127.0.0.1:8000)"; Types: full server custom; Flags: checkablealone
Name: "client"; Description: "Coalesce Client (WPF desktop UI)"; Types: full client custom; Flags: checkablealone
#endif

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
#if IsChooser == "1"
Name: "autostartserver"; Description: "Start Coalesce Server when I log in"; GroupDescription: "Startup options:"; Flags: unchecked; Components: server
Name: "autostartclient"; Description: "Start Coalesce Client when I log in"; GroupDescription: "Startup options:"; Flags: unchecked; Components: client
#elif Package == "server"
Name: "autostartserver"; Description: "Start Coalesce Server when I log in"; GroupDescription: "Startup options:"; Flags: unchecked
#elif Package == "client"
Name: "autostartclient"; Description: "Start Coalesce Client when I log in"; GroupDescription: "Startup options:"; Flags: unchecked
#endif

[Files]
#if HasServer == "1"
#if IsChooser == "1"
Source: "..\dist\CoalesceServer\*"; DestDir: "{app}\Server"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: server
#else
Source: "..\dist\CoalesceServer\*"; DestDir: "{app}\Server"; Flags: ignoreversion recursesubdirs createallsubdirs
#endif
#endif
#if HasClient == "1"
#if IsChooser == "1"
Source: "..\dist\CoalesceClient\*"; DestDir: "{app}\Client"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: client
#else
Source: "..\dist\CoalesceClient\*"; DestDir: "{app}\Client"; Flags: ignoreversion recursesubdirs createallsubdirs
#endif
#endif

[Icons]
#if HasServer == "1"
#if IsChooser == "1"
Name: "{group}\Coalesce Server"; Filename: "{app}\Server\Coalesce.Server.exe"; WorkingDir: "{app}\Server"; Components: server
Name: "{autodesktop}\Coalesce Server"; Filename: "{app}\Server\Coalesce.Server.exe"; WorkingDir: "{app}\Server"; Tasks: desktopicon; Components: server
Name: "{userstartup}\Coalesce Server"; Filename: "{app}\Server\Coalesce.Server.exe"; WorkingDir: "{app}\Server"; Tasks: autostartserver; Components: server
#else
Name: "{group}\Coalesce Server"; Filename: "{app}\Server\Coalesce.Server.exe"; WorkingDir: "{app}\Server"
Name: "{autodesktop}\Coalesce Server"; Filename: "{app}\Server\Coalesce.Server.exe"; WorkingDir: "{app}\Server"; Tasks: desktopicon
Name: "{userstartup}\Coalesce Server"; Filename: "{app}\Server\Coalesce.Server.exe"; WorkingDir: "{app}\Server"; Tasks: autostartserver
#endif
#endif
#if HasClient == "1"
#if IsChooser == "1"
Name: "{group}\Coalesce Client"; Filename: "{app}\Client\Coalesce.Client.exe"; WorkingDir: "{app}\Client"; Components: client
Name: "{autodesktop}\Coalesce Client"; Filename: "{app}\Client\Coalesce.Client.exe"; WorkingDir: "{app}\Client"; Tasks: desktopicon; Components: client
Name: "{userstartup}\Coalesce Client"; Filename: "{app}\Client\Coalesce.Client.exe"; WorkingDir: "{app}\Client"; Tasks: autostartclient; Components: client
#else
Name: "{group}\Coalesce Client"; Filename: "{app}\Client\Coalesce.Client.exe"; WorkingDir: "{app}\Client"
Name: "{autodesktop}\Coalesce Client"; Filename: "{app}\Client\Coalesce.Client.exe"; WorkingDir: "{app}\Client"; Tasks: desktopicon
Name: "{userstartup}\Coalesce Client"; Filename: "{app}\Client\Coalesce.Client.exe"; WorkingDir: "{app}\Client"; Tasks: autostartclient
#endif
#endif
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

[Run]
#if HasServer == "1"
#if IsChooser == "1"
Filename: "{app}\Server\Coalesce.Server.exe"; Description: "Launch Coalesce Server now"; Flags: nowait postinstall skipifsilent unchecked; Components: server; WorkingDir: "{app}\Server"
#else
Filename: "{app}\Server\Coalesce.Server.exe"; Description: "Launch Coalesce Server now"; Flags: nowait postinstall skipifsilent unchecked; WorkingDir: "{app}\Server"
#endif
#endif
#if HasClient == "1"
#if IsChooser == "1"
Filename: "{app}\Client\Coalesce.Client.exe"; Description: "Launch Coalesce Client now"; Flags: nowait postinstall skipifsilent unchecked; Components: client; WorkingDir: "{app}\Client"
#else
Filename: "{app}\Client\Coalesce.Client.exe"; Description: "Launch Coalesce Client now"; Flags: nowait postinstall skipifsilent unchecked; WorkingDir: "{app}\Client"
#endif
#endif

[Code]
var
  RolePage: TWizardPage;
  RoleIntro: TNewStaticText;
  RoleBoth: TRadioButton;
  RoleClient: TRadioButton;
  RoleServer: TRadioButton;
  RoleHintBoth: TNewStaticText;
  RoleHintClient: TNewStaticText;
  RoleHintServer: TNewStaticText;
  RoleBadge: TNewStaticText;
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

#if IsChooser == "1"
procedure SelectBoth(Sender: TObject);
begin
  RoleBoth.Checked := True;
end;

procedure SelectServer(Sender: TObject);
begin
  RoleServer.Checked := True;
end;

procedure SelectClient(Sender: TObject);
begin
  RoleClient.Checked := True;
end;

procedure ApplyRoleSelection();
begin
  if RoleServer.Checked then
  begin
    WizardSelectComponents('server,!client');
    WizardForm.TypesCombo.ItemIndex := 1;
  end
  else if RoleClient.Checked then
  begin
    WizardSelectComponents('client,!server');
    WizardForm.TypesCombo.ItemIndex := 2;
  end
  else
  begin
    WizardSelectComponents('server,client');
    WizardForm.TypesCombo.ItemIndex := 0;
  end;
end;

procedure SyncRadiosFromType();
var
  TypeName: String;
begin
  TypeName := WizardSetupType(False);
  if CompareText(TypeName, 'server') = 0 then
    RoleServer.Checked := True
  else if CompareText(TypeName, 'client') = 0 then
    RoleClient.Checked := True
  else
    RoleBoth.Checked := True;
end;

function ChoiceSummary(): String;
begin
  if RoleServer.Checked then
    Result := 'SERVER only — API and database on this PC'
  else if RoleClient.Checked then
    Result := 'CLIENT only — desktop UI (needs a running Server)'
  else
    Result := 'BOTH — Server and Client on this PC';
end;

procedure CreateRolePage();
var
  TopY: Integer;
begin
  RolePage := CreateCustomPage(wpInfoBefore,
    'What do you want to install?',
    'One choice for this computer — Both, Server, or Client.');

  RoleIntro := TNewStaticText.Create(RolePage);
  RoleIntro.Parent := RolePage.Surface;
  RoleIntro.Caption :=
    'Coalesce is two programs: a Server that holds your data, and a Client ' +
    'you work in every day. Pick what belongs on THIS PC.';
  RoleIntro.Font.Name := 'Segoe UI';
  RoleIntro.Font.Size := 10;
  RoleIntro.AutoSize := False;
  RoleIntro.WordWrap := True;
  RoleIntro.Left := 0;
  RoleIntro.Top := 0;
  RoleIntro.Width := RolePage.SurfaceWidth;
  RoleIntro.Height := ScaleY(40);

  TopY := RoleIntro.Top + RoleIntro.Height + ScaleY(12);

  RoleBoth := TRadioButton.Create(RolePage);
  RoleBoth.Parent := RolePage.Surface;
  RoleBoth.Caption := 'BOTH  —  Server and Client';
  RoleBoth.Font.Name := 'Segoe UI';
  RoleBoth.Font.Size := 13;
  RoleBoth.Font.Style := [fsBold];
  RoleBoth.Left := 0;
  RoleBoth.Top := TopY;
  RoleBoth.Width := RolePage.SurfaceWidth - ScaleX(110);
  RoleBoth.Height := ScaleY(24);
  RoleBoth.Checked := True;
  RoleBoth.OnClick := @SelectBoth;

  RoleBadge := TNewStaticText.Create(RolePage);
  RoleBadge.Parent := RolePage.Surface;
  RoleBadge.Caption := 'Recommended';
  RoleBadge.Font.Name := 'Segoe UI';
  RoleBadge.Font.Size := 9;
  RoleBadge.Font.Style := [fsBold];
  RoleBadge.Font.Color := clNavy;
  RoleBadge.AutoSize := True;
  RoleBadge.Left := RolePage.SurfaceWidth - ScaleX(100);
  RoleBadge.Top := RoleBoth.Top + ScaleY(4);
  RoleBadge.Cursor := crHand;
  RoleBadge.OnClick := @SelectBoth;

  RoleHintBoth := TNewStaticText.Create(RolePage);
  RoleHintBoth.Parent := RolePage.Surface;
  RoleHintBoth.Caption :=
    'Full install on this PC. Best for a single-computer shop. ' +
    'After setup: start Server first, then Client.';
  RoleHintBoth.Font.Name := 'Segoe UI';
  RoleHintBoth.Font.Size := 9;
  RoleHintBoth.Left := ScaleX(22);
  RoleHintBoth.Top := RoleBoth.Top + RoleBoth.Height + ScaleY(2);
  RoleHintBoth.Width := RolePage.SurfaceWidth - ScaleX(22);
  RoleHintBoth.AutoSize := False;
  RoleHintBoth.WordWrap := True;
  RoleHintBoth.Height := ScaleY(34);
  RoleHintBoth.Cursor := crHand;
  RoleHintBoth.OnClick := @SelectBoth;

  TopY := RoleHintBoth.Top + RoleHintBoth.Height + ScaleY(14);

  RoleServer := TRadioButton.Create(RolePage);
  RoleServer.Parent := RolePage.Surface;
  RoleServer.Caption := 'SERVER  —  data and API';
  RoleServer.Font.Name := 'Segoe UI';
  RoleServer.Font.Size := 13;
  RoleServer.Font.Style := [fsBold];
  RoleServer.Left := 0;
  RoleServer.Top := TopY;
  RoleServer.Width := RolePage.SurfaceWidth;
  RoleServer.Height := ScaleY(24);
  RoleServer.OnClick := @SelectServer;

  RoleHintServer := TNewStaticText.Create(RolePage);
  RoleHintServer.Parent := RolePage.Surface;
  RoleHintServer.Caption :=
    'Install once on the machine that stores inventory and orders. ' +
    'Listens on http://127.0.0.1:8000. Login: admin / admin.';
  RoleHintServer.Font.Name := 'Segoe UI';
  RoleHintServer.Font.Size := 9;
  RoleHintServer.Left := ScaleX(22);
  RoleHintServer.Top := RoleServer.Top + RoleServer.Height + ScaleY(2);
  RoleHintServer.Width := RolePage.SurfaceWidth - ScaleX(22);
  RoleHintServer.AutoSize := False;
  RoleHintServer.WordWrap := True;
  RoleHintServer.Height := ScaleY(34);
  RoleHintServer.Cursor := crHand;
  RoleHintServer.OnClick := @SelectServer;

  TopY := RoleHintServer.Top + RoleHintServer.Height + ScaleY(14);

  RoleClient := TRadioButton.Create(RolePage);
  RoleClient.Parent := RolePage.Surface;
  RoleClient.Caption := 'CLIENT  —  the screen you work in';
  RoleClient.Font.Name := 'Segoe UI';
  RoleClient.Font.Size := 13;
  RoleClient.Font.Style := [fsBold];
  RoleClient.Left := 0;
  RoleClient.Top := TopY;
  RoleClient.Width := RolePage.SurfaceWidth;
  RoleClient.Height := ScaleY(24);
  RoleClient.OnClick := @SelectClient;

  RoleHintClient := TNewStaticText.Create(RolePage);
  RoleHintClient.Parent := RolePage.Surface;
  RoleHintClient.Caption :=
    'Install on each workstation. Needs a running Server ' +
    '(on this PC or another machine on your network).';
  RoleHintClient.Font.Name := 'Segoe UI';
  RoleHintClient.Font.Size := 9;
  RoleHintClient.Left := ScaleX(22);
  RoleHintClient.Top := RoleClient.Top + RoleClient.Height + ScaleY(2);
  RoleHintClient.Width := RolePage.SurfaceWidth - ScaleX(22);
  RoleHintClient.AutoSize := False;
  RoleHintClient.WordWrap := True;
  RoleHintClient.Height := ScaleY(34);
  RoleHintClient.Cursor := crHand;
  RoleHintClient.OnClick := @SelectClient;
end;
#endif

procedure CreateDbSizePage(AfterPageId: Integer);
var
  TopY: Integer;
begin
  DbSizePage := CreateCustomPage(AfterPageId,
    'Database size',
    'How large do you expect this Coalesce database to grow?');

  DbSizeHeadline := TNewStaticText.Create(DbSizePage);
  DbSizeHeadline.Parent := DbSizePage.Surface;
  DbSizeHeadline.Caption := 'Planned database size';
  DbSizeHeadline.Font.Name := 'Segoe UI';
  DbSizeHeadline.Font.Size := 14;
  DbSizeHeadline.Font.Style := [fsBold];
  DbSizeHeadline.AutoSize := True;
  DbSizeHeadline.Left := 0;
  DbSizeHeadline.Top := 0;

  DbSizeSubhead := TNewStaticText.Create(DbSizePage);
  DbSizeSubhead.Parent := DbSizePage.Surface;
  DbSizeSubhead.Caption :=
    'Starts on a local SQLite file. This sets planned capacity for status warnings — not a hard engine limit.';
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
  DbSizeSmall.Caption := 'Small  —  about 500 MB (light single-PC use)';
  DbSizeSmall.Font.Name := 'Segoe UI';
  DbSizeSmall.Font.Size := 11;
  DbSizeSmall.Left := ScaleX(4);
  DbSizeSmall.Top := TopY;
  DbSizeSmall.Width := DbSizePage.SurfaceWidth - ScaleX(8);
  DbSizeSmall.Height := ScaleY(22);

  TopY := DbSizeSmall.Top + DbSizeSmall.Height + ScaleY(10);

  DbSizeMedium := TRadioButton.Create(DbSizePage);
  DbSizeMedium.Parent := DbSizePage.Surface;
  DbSizeMedium.Caption := 'Medium  —  about 2 GB (recommended)';
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
  DbSizeLarge.Caption := 'Large  —  about 10 GB (busy warehouse / long history)';
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
    'Tip: for multi-user SQL Server / MySQL / PostgreSQL, pick Medium here, then use Settings → Grow database… after setup.';
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

procedure InitializeWizard();
begin
#if IsChooser == "1"
  CreateRolePage();
  SyncRadiosFromType();
  ApplyRoleSelection();
  CreateDbSizePage(RolePage.ID);
#elif HasServer == "1"
  CreateDbSizePage(wpInfoBefore);
#endif
end;

function ServerComponentSelected(): Boolean;
begin
#if HasServer == "0"
  Result := False;
#elif IsChooser == "1"
  Result := WizardIsComponentSelected('server');
#else
  Result := True;
#endif
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
    Result := 2048;
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
  if DbSizePage = nil then
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
#if IsChooser == "1"
  if PageID = wpSelectComponents then
    Result := True
  else
#endif
  if (DbSizePage <> nil) and (PageID = DbSizePage.ID) then
    Result := not ServerComponentSelected();
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  SizeMb: Integer;
begin
  Result := True;
#if IsChooser == "1"
  if (RolePage <> nil) and (CurPageID = RolePage.ID) then
  begin
    if (not RoleBoth.Checked) and (not RoleClient.Checked) and (not RoleServer.Checked) then
    begin
      MsgBox('Choose BOTH, SERVER, or CLIENT before continuing.', mbError, MB_OK);
      Result := False;
      exit;
    end;
    ApplyRoleSelection();
  end
  else
#endif
  if (DbSizePage <> nil) and (CurPageID = DbSizePage.ID) then
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

#if IsChooser == "1"
function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo,
  MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  S: String;
begin
  S := 'Installation choice:' + NewLine;
  S := S + Space + ChoiceSummary() + NewLine + NewLine;
  S := S + MemoDirInfo + NewLine + NewLine;
  if MemoGroupInfo <> '' then
    S := S + MemoGroupInfo + NewLine + NewLine;
  if MemoTasksInfo <> '' then
    S := S + MemoTasksInfo + NewLine + NewLine;
  if RoleClient.Checked then
    S := S + 'Note: start Coalesce Server before opening the Client.' + NewLine
  else if RoleBoth.Checked then
    S := S + 'Tip: after setup, launch Server first, then Client.' + NewLine;
  Result := S;
end;
#endif

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    WriteCapacityConfig();
end;
