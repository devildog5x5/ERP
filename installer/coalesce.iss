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

#define DefaultDir "{localappdata}\Programs\Coalesce"

; Per-package AppIds so Client-only and Server-only can coexist.
; Combined replaces either dedicated install when used.
#define CombinedAppId "{{D0F6B2E5-7A81-4C24-AF5D-B3C2E4F60719}"
#define ClientAppId "{{E1A7B3C2-D4E5-4F60-8A91-B2C3D4E5F617}"
#define ServerAppId "{{F2B8C4D3-E5F6-4071-9BA2-C3D4E5F61728}"

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
  #define AppIdGuid "{{E1A7B3C2-D4E5-4F60-8A91-B2C3D4E5F617}"
  #define IsChooser "0"
  #define HasServer "0"
  #define HasClient "1"
#elif Package == "server"
  #define MyAppName "Coalesce Server"
  #define OutputName "CoalesceServerSetup"
  #define VersionDesc "Coalesce Server installer"
  #define InfoBefore "info-server.txt"
  #define UninstallIcon "{app}\Server\Coalesce.Server.exe"
  #define AppIdGuid "{{F2B8C4D3-E5F6-4071-9BA2-C3D4E5F61728}"
  #define IsChooser "0"
  #define HasServer "1"
  #define HasClient "0"
#else
  #define MyAppName "Coalesce"
  #define OutputName "CoalesceSetup"
  #define VersionDesc "Coalesce installer — choose Both, Server, or Client"
  #define InfoBefore "info-combined.txt"
  #define UninstallIcon "{app}\Client\Coalesce.Client.exe"
  #define AppIdGuid "{{D0F6B2E5-7A81-4C24-AF5D-B3C2E4F60719}"
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
DisableWelcomePage=yes
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
#if IsChooser == "0"
; Dedicated packages open with a short briefing. Combined skips this so the
; three role cards are the first thing the user sees.
InfoBeforeFile={#InfoBefore}
#endif
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
  RoleKeys: TNewStaticText;
  RoleSummaryPanel: TPanel;
  RoleSummary: TNewStaticText;
  RoleFoot: TNewStaticText;
  RoleBothPanel: TPanel;
  RoleServerPanel: TPanel;
  RoleClientPanel: TPanel;
  RoleBoth: TRadioButton;
  RoleClient: TRadioButton;
  RoleServer: TRadioButton;
  RoleHintBoth: TNewStaticText;
  RoleHintClient: TNewStaticText;
  RoleHintServer: TNewStaticText;
  RoleBadge: TNewStaticText;
  RoleBadgeServer: TNewStaticText;
  RoleBadgeClient: TNewStaticText;
  RolePickBoth: TNewStaticText;
  RolePickServer: TNewStaticText;
  RolePickClient: TNewStaticText;
  RoleNumBoth: TNewStaticText;
  RoleNumServer: TNewStaticText;
  RoleNumClient: TNewStaticText;
  RoleTitleBoth: TNewStaticText;
  RoleTitleServer: TNewStaticText;
  RoleTitleClient: TNewStaticText;
  RoleAccentBoth: TPanel;
  RoleAccentServer: TPanel;
  RoleAccentClient: TPanel;
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
  { Role-aware cleanup: Client-only must not wipe a dedicated Server install,
    and vice versa. Running a dedicated package does replace an older unified
    Combined install (same folder). Combined clears dedicated Client/Server
    entries so one product listing remains. }
  Result := UninstallByAppId('{#AppIdGuid}');
  if not Result then
    exit;

#if Package == "client"
  Result :=
    UninstallByAppId('{#CombinedAppId}') and
    UninstallByAppId('{#LegacyUnifiedAppId}') and
    UninstallByAppId('{#LegacyClientAppId}');
#elif Package == "server"
  Result :=
    UninstallByAppId('{#CombinedAppId}') and
    UninstallByAppId('{#LegacyUnifiedAppId}') and
    UninstallByAppId('{#LegacyServerAppId}');
#else
  Result :=
    UninstallByAppId('{#ClientAppId}') and
    UninstallByAppId('{#ServerAppId}') and
    UninstallByAppId('{#LegacyUnifiedAppId}') and
    UninstallByAppId('{#LegacyClientAppId}') and
    UninstallByAppId('{#LegacyServerAppId}');
#endif
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
procedure SelectSetupTypeByName(const TypeName: String);
var
  I: Integer;
begin
  for I := 0 to WizardForm.TypesCombo.Items.Count - 1 do
  begin
    WizardForm.TypesCombo.ItemIndex := I;
    if CompareText(WizardSetupType(False), TypeName) = 0 then
      exit;
  end;
  WizardForm.TypesCombo.ItemIndex := 0;
end;

function ChoiceSummary(): String;
begin
  { Includes / Skips so the receipt is unambiguous. }
  if RoleServer.Checked then
    Result := 'SERVER ONLY — Includes: Server (API/DB)  ·  Skips: Client'
  else if RoleClient.Checked then
    Result := 'CLIENT ONLY — Includes: Client (desktop)  ·  Skips: Server'
  else
    Result := 'BOTH — Includes: Server + Client  ·  Skips: nothing';
end;

function ChoiceOutcome(): String;
begin
  { One plain sentence: what this PC will be after Next. }
  if RoleServer.Checked then
    Result := 'After install this PC will HOST the data (API + database). No desktop UI here.'
  else if RoleClient.Checked then
    Result := 'After install this PC will be a DESK only. It talks to a Server that already runs elsewhere.'
  else
    Result := 'After install this PC will run the FULL app — Server and Client together. Best if unsure.';
end;

function RoleShortLabel(): String;
begin
  { Compact label used on later wizard pages so the choice stays visible. }
  if RoleServer.Checked then
    Result := 'SERVER ONLY'
  else if RoleClient.Checked then
    Result := 'CLIENT ONLY'
  else
    Result := 'BOTH (Server + Client)';
end;

procedure RestateRoleOnPage();
begin
  { Keep the chosen role in the page subtitle after leaving the cards —
    users should never wonder what Next is about to install. }
  if (RolePage = nil) or (WizardForm.CurPageID = RolePage.ID) then
    exit;
  WizardForm.PageDescriptionLabel.Caption :=
    'Installing: ' + RoleShortLabel() + '  —  ' + ChoiceOutcome();
end;

procedure UpdateRoleSummary();
begin
  if RoleSummary = nil then
    exit;
  { Receipt under the cards: role label + Includes/Skips + plain-English outcome.
    Tint the bar (and the selected card) yellow / rose / mint so the choice is
    obvious without reading every line. }
  RoleSummary.Caption :=
    '▶ YOUR CHOICE →  ' + ChoiceSummary() + #13#10 +
    ChoiceOutcome() + '  —  Next installs that (and only that).';
  if RoleServer.Checked then
  begin
    RoleSummary.Font.Color := clMaroon;
    if RoleSummaryPanel <> nil then
      RoleSummaryPanel.Color := $00E8E8FF; { light rose }
  end
  else if RoleClient.Checked then
  begin
    RoleSummary.Font.Color := clTeal;
    if RoleSummaryPanel <> nil then
      RoleSummaryPanel.Color := $00F0FFF0; { light mint }
  end
  else
  begin
    RoleSummary.Font.Color := clNavy;
    if RoleSummaryPanel <> nil then
      RoleSummaryPanel.Color := clInfoBk; { pale yellow — matches Both card }
  end;
end;

procedure UpdateNextButtonForRole();
begin
  { Spell the choice on Next itself so the action matches the card. }
  if (RolePage = nil) or (WizardForm.CurPageID <> RolePage.ID) then
    exit;
  if RoleServer.Checked then
    WizardForm.NextButton.Caption := 'Yes — Install Server →'
  else if RoleClient.Checked then
    WizardForm.NextButton.Caption := 'Yes — Install Client →'
  else
    WizardForm.NextButton.Caption := 'Yes — Install Both →';
end;

procedure PaintRolePanels();
begin
  { Selected card: sunk bevel, tint, left accent bar, ✓ SELECTED.
    Unselected: flat gray, muted accent, “Click to choose”. }
  if RoleBoth.Checked then
  begin
    RoleBothPanel.BevelOuter := bvLowered;
    RoleBothPanel.BevelWidth := 2;
    RoleBothPanel.Color := clInfoBk; { pale yellow }
    if RoleAccentBoth <> nil then
      RoleAccentBoth.Color := clNavy;
    if RoleTitleBoth <> nil then
      RoleTitleBoth.Font.Color := clNavy;
    if RolePickBoth <> nil then
    begin
      RolePickBoth.Caption := '✓ SELECTED';
      RolePickBoth.Font.Color := clNavy;
    end;
    if RoleNumBoth <> nil then
      RoleNumBoth.Font.Color := clNavy;
  end
  else
  begin
    RoleBothPanel.BevelOuter := bvRaised;
    RoleBothPanel.BevelWidth := 1;
    RoleBothPanel.Color := clBtnFace;
    if RoleAccentBoth <> nil then
      RoleAccentBoth.Color := clSilver;
    if RoleTitleBoth <> nil then
      RoleTitleBoth.Font.Color := clWindowText;
    if RolePickBoth <> nil then
    begin
      RolePickBoth.Caption := 'Click to choose';
      RolePickBoth.Font.Color := clGray;
    end;
    if RoleNumBoth <> nil then
      RoleNumBoth.Font.Color := clGray;
  end;

  if RoleServer.Checked then
  begin
    RoleServerPanel.BevelOuter := bvLowered;
    RoleServerPanel.BevelWidth := 2;
    RoleServerPanel.Color := $00E8E8FF; { light rose — matches summary bar }
    if RoleAccentServer <> nil then
      RoleAccentServer.Color := clMaroon;
    if RoleTitleServer <> nil then
      RoleTitleServer.Font.Color := clMaroon;
    if RolePickServer <> nil then
    begin
      RolePickServer.Caption := '✓ SELECTED';
      RolePickServer.Font.Color := clMaroon;
    end;
    if RoleNumServer <> nil then
      RoleNumServer.Font.Color := clMaroon;
  end
  else
  begin
    RoleServerPanel.BevelOuter := bvRaised;
    RoleServerPanel.BevelWidth := 1;
    RoleServerPanel.Color := clBtnFace;
    if RoleAccentServer <> nil then
      RoleAccentServer.Color := clSilver;
    if RoleTitleServer <> nil then
      RoleTitleServer.Font.Color := clWindowText;
    if RolePickServer <> nil then
    begin
      RolePickServer.Caption := 'Click to choose';
      RolePickServer.Font.Color := clGray;
    end;
    if RoleNumServer <> nil then
      RoleNumServer.Font.Color := clGray;
  end;

  if RoleClient.Checked then
  begin
    RoleClientPanel.BevelOuter := bvLowered;
    RoleClientPanel.BevelWidth := 2;
    RoleClientPanel.Color := $00F0FFF0; { light mint — matches summary bar }
    if RoleAccentClient <> nil then
      RoleAccentClient.Color := clTeal;
    if RoleTitleClient <> nil then
      RoleTitleClient.Font.Color := clTeal;
    if RolePickClient <> nil then
    begin
      RolePickClient.Caption := '✓ SELECTED';
      RolePickClient.Font.Color := clTeal;
    end;
    if RoleNumClient <> nil then
      RoleNumClient.Font.Color := clTeal;
  end
  else
  begin
    RoleClientPanel.BevelOuter := bvRaised;
    RoleClientPanel.BevelWidth := 1;
    RoleClientPanel.Color := clBtnFace;
    if RoleAccentClient <> nil then
      RoleAccentClient.Color := clSilver;
    if RoleTitleClient <> nil then
      RoleTitleClient.Font.Color := clWindowText;
    if RolePickClient <> nil then
    begin
      RolePickClient.Caption := 'Click to choose';
      RolePickClient.Font.Color := clGray;
    end;
    if RoleNumClient <> nil then
      RoleNumClient.Font.Color := clGray;
  end;

  UpdateRoleSummary();
  UpdateNextButtonForRole();
end;

procedure SelectBoth(Sender: TObject);
begin
  RoleBoth.Checked := True;
  PaintRolePanels();
end;

procedure SelectServer(Sender: TObject);
begin
  RoleServer.Checked := True;
  PaintRolePanels();
end;

procedure SelectClient(Sender: TObject);
begin
  RoleClient.Checked := True;
  PaintRolePanels();
end;

procedure CycleRole(Delta: Integer);
begin
  { Walk Both → Server → Client (and reverse) with the arrow keys. }
  if RoleBoth.Checked then
  begin
    if Delta > 0 then SelectServer(nil) else SelectClient(nil);
  end
  else if RoleServer.Checked then
  begin
    if Delta > 0 then SelectClient(nil) else SelectBoth(nil);
  end
  else
  begin
    if Delta > 0 then SelectBoth(nil) else SelectServer(nil);
  end;
end;

{ Keys 1 / 2 / 3 pick a card; arrows cycle; Enter / Space advance from the chooser. }
procedure WizardKeyDown(Sender: TObject; var Key: Word; Shift: TShiftState);
begin
  if (RolePage = nil) or (WizardForm.CurPageID <> RolePage.ID) then
    exit;

  { 49/50/51 = main row; 97/98/99 = numpad; 37/38/39/40 = arrows; 13 = Enter; 32 = Space }
  if (Key = 49) or (Key = 97) then
  begin
    SelectBoth(nil);
    Key := 0;
  end
  else if (Key = 50) or (Key = 98) then
  begin
    SelectServer(nil);
    Key := 0;
  end
  else if (Key = 51) or (Key = 99) then
  begin
    SelectClient(nil);
    Key := 0;
  end
  else if (Key = 38) or (Key = 37) then
  begin
    CycleRole(-1);
    Key := 0;
  end
  else if (Key = 40) or (Key = 39) then
  begin
    CycleRole(1);
    Key := 0;
  end
  else if (Key = 13) or (Key = 32) then
  begin
    WizardForm.NextButton.OnClick(WizardForm.NextButton);
    Key := 0;
  end;
end;

{ Double-click a card to select it and move on — same idea as most wizards. }
procedure AdvanceBoth(Sender: TObject);
begin
  SelectBoth(Sender);
  WizardForm.NextButton.OnClick(WizardForm.NextButton);
end;

procedure AdvanceServer(Sender: TObject);
begin
  SelectServer(Sender);
  WizardForm.NextButton.OnClick(WizardForm.NextButton);
end;

procedure AdvanceClient(Sender: TObject);
begin
  SelectClient(Sender);
  WizardForm.NextButton.OnClick(WizardForm.NextButton);
end;

procedure ApplyRoleSelection();
begin
  if RoleServer.Checked then
  begin
    WizardSelectComponents('server,!client');
    SelectSetupTypeByName('server');
  end
  else if RoleClient.Checked then
  begin
    WizardSelectComponents('client,!server');
    SelectSetupTypeByName('client');
  end
  else
  begin
    WizardSelectComponents('server,client');
    SelectSetupTypeByName('full');
  end;
  PaintRolePanels();
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
  PaintRolePanels();
end;

function MakeRolePanel(ParentPage: TWizardPage; LeftX, TopY, WidthPx, HeightPx: Integer): TPanel;
begin
  Result := TPanel.Create(ParentPage);
  Result.Parent := ParentPage.Surface;
  Result.Left := LeftX;
  Result.Top := TopY;
  Result.Width := WidthPx;
  Result.Height := HeightPx;
  Result.BevelOuter := bvRaised;
  Result.BevelWidth := 1;
  Result.Color := clBtnFace;
  Result.ParentBackground := False;
  Result.Cursor := crHand;
end;

function MakeAccentBar(ParentPanel: TPanel): TPanel;
begin
  { Left color strip — lights up when the card is selected. }
  Result := TPanel.Create(ParentPanel);
  Result.Parent := ParentPanel;
  Result.Left := 0;
  Result.Top := 0;
  Result.Width := ScaleX(8);
  Result.Height := ParentPanel.Height;
  Result.BevelOuter := bvNone;
  Result.Color := clSilver;
  Result.ParentBackground := False;
  Result.Cursor := crHand;
end;

procedure CreateRolePage();
var
  TopY: Integer;
  PanelH: Integer;
  HalfW: Integer;
  Gap: Integer;
  RoleSplit: TNewStaticText;
begin
  { First real page of Combined Setup (Welcome + InfoBefore are off).
    Layout: full-width Both on top, then Server | Client side by side so the
    two specialized roles read as an obvious fork under the safe default.
    Radios stay for Checked state / keyboard focus; titles are plain labels
    so the cards read as big buttons, not a form. }
  RolePage := CreateCustomPage(wpWelcome,
    'Pick ONE role for this PC',
    'Three cards. Click one. Setup installs only that role — nothing else.');

  RoleIntro := TNewStaticText.Create(RolePage);
  RoleIntro.Parent := RolePage.Surface;
  RoleIntro.Caption :=
    'Quick guide (read top → bottom):'#13#10 +
    '  • Only Coalesce PC on this network?     →  1  BOTH'#13#10 +
    '  • This PC holds the shared database?    →  2  SERVER'#13#10 +
    '  • Server already runs somewhere else?   →  3  CLIENT'#13#10 +
    'CHOOSE ONE card. Not sure? Leave Both selected — you can re-run Setup later.';
  RoleIntro.Font.Name := 'Segoe UI';
  RoleIntro.Font.Size := 9;
  RoleIntro.Font.Style := [fsBold];
  RoleIntro.AutoSize := False;
  RoleIntro.WordWrap := True;
  RoleIntro.Left := 0;
  RoleIntro.Top := 0;
  RoleIntro.Width := RolePage.SurfaceWidth;
  RoleIntro.Height := ScaleY(58);

  TopY := RoleIntro.Top + RoleIntro.Height + ScaleY(4);
  PanelH := ScaleY(74);

  { --- BOTH (full width — default / safest) --- }
  RoleBothPanel := MakeRolePanel(RolePage, 0, TopY, RolePage.SurfaceWidth, PanelH);
  RoleBothPanel.OnClick := @SelectBoth;
  RoleBothPanel.OnDblClick := @AdvanceBoth;

  RoleAccentBoth := MakeAccentBar(RoleBothPanel);
  RoleAccentBoth.OnClick := @SelectBoth;
  RoleAccentBoth.OnDblClick := @AdvanceBoth;

  RoleNumBoth := TNewStaticText.Create(RolePage);
  RoleNumBoth.Parent := RoleBothPanel;
  RoleNumBoth.Caption := '1';
  RoleNumBoth.Font.Name := 'Segoe UI';
  RoleNumBoth.Font.Size := 22;
  RoleNumBoth.Font.Style := [fsBold];
  RoleNumBoth.Font.Color := clNavy;
  RoleNumBoth.AutoSize := True;
  RoleNumBoth.Left := ScaleX(14);
  RoleNumBoth.Top := ScaleY(16);
  RoleNumBoth.Cursor := crHand;
  RoleNumBoth.OnClick := @SelectBoth;
  RoleNumBoth.OnDblClick := @AdvanceBoth;

  { Tiny radio — state holder only; title is the big label beside it. }
  RoleBoth := TRadioButton.Create(RolePage);
  RoleBoth.Parent := RoleBothPanel;
  RoleBoth.Caption := '';
  RoleBoth.Left := ScaleX(40);
  RoleBoth.Top := ScaleY(8);
  RoleBoth.Width := ScaleX(18);
  RoleBoth.Height := ScaleY(18);
  RoleBoth.Checked := True;
  RoleBoth.OnClick := @SelectBoth;
  RoleBoth.OnDblClick := @AdvanceBoth;

  RoleTitleBoth := TNewStaticText.Create(RolePage);
  RoleTitleBoth.Parent := RoleBothPanel;
  RoleTitleBoth.Caption := 'BOTH  —  Server + Client (one PC does it all)';
  RoleTitleBoth.Font.Name := 'Segoe UI';
  RoleTitleBoth.Font.Size := 13;
  RoleTitleBoth.Font.Style := [fsBold];
  RoleTitleBoth.Font.Color := clNavy;
  RoleTitleBoth.AutoSize := True;
  RoleTitleBoth.Left := ScaleX(58);
  RoleTitleBoth.Top := ScaleY(6);
  RoleTitleBoth.Cursor := crHand;
  RoleTitleBoth.OnClick := @SelectBoth;
  RoleTitleBoth.OnDblClick := @AdvanceBoth;

  RoleBadge := TNewStaticText.Create(RolePage);
  RoleBadge.Parent := RoleBothPanel;
  RoleBadge.Caption := '★ Recommended';
  RoleBadge.Font.Name := 'Segoe UI';
  RoleBadge.Font.Size := 9;
  RoleBadge.Font.Style := [fsBold];
  RoleBadge.Font.Color := clNavy;
  RoleBadge.AutoSize := True;
  RoleBadge.Left := RoleBothPanel.Width - ScaleX(108);
  RoleBadge.Top := ScaleY(6);
  RoleBadge.Cursor := crHand;
  RoleBadge.OnClick := @SelectBoth;
  RoleBadge.OnDblClick := @AdvanceBoth;

  RolePickBoth := TNewStaticText.Create(RolePage);
  RolePickBoth.Parent := RoleBothPanel;
  RolePickBoth.Caption := '✓ SELECTED';
  RolePickBoth.Font.Name := 'Segoe UI';
  RolePickBoth.Font.Size := 8;
  RolePickBoth.Font.Style := [fsBold];
  RolePickBoth.Font.Color := clNavy;
  RolePickBoth.AutoSize := True;
  RolePickBoth.Left := RoleBothPanel.Width - ScaleX(108);
  RolePickBoth.Top := ScaleY(26);
  RolePickBoth.Cursor := crHand;
  RolePickBoth.OnClick := @SelectBoth;
  RolePickBoth.OnDblClick := @AdvanceBoth;

  RoleHintBoth := TNewStaticText.Create(RolePage);
  RoleHintBoth.Parent := RoleBothPanel;
  RoleHintBoth.Caption :=
    'Includes: Server + Client.  Skips: nothing.'#13#10 +
    'Pick if: one machine does everything — safest default.';
  RoleHintBoth.Font.Name := 'Segoe UI';
  RoleHintBoth.Font.Size := 9;
  RoleHintBoth.Left := ScaleX(58);
  RoleHintBoth.Top := ScaleY(30);
  RoleHintBoth.Width := RoleBothPanel.Width - ScaleX(170);
  RoleHintBoth.AutoSize := False;
  RoleHintBoth.WordWrap := True;
  RoleHintBoth.Height := ScaleY(40);
  RoleHintBoth.Cursor := crHand;
  RoleHintBoth.OnClick := @SelectBoth;
  RoleHintBoth.OnDblClick := @AdvanceBoth;

  TopY := RoleBothPanel.Top + RoleBothPanel.Height + ScaleY(4);

  RoleSplit := TNewStaticText.Create(RolePage);
  RoleSplit.Parent := RolePage.Surface;
  RoleSplit.Caption := 'Need just one role?  Server (left)  |  Client (right)  — click the card';
  RoleSplit.Font.Name := 'Segoe UI';
  RoleSplit.Font.Size := 10;
  RoleSplit.Font.Style := [fsBold];
  RoleSplit.Font.Color := clWindowText;
  RoleSplit.AutoSize := True;
  RoleSplit.Left := 0;
  RoleSplit.Top := TopY;

  TopY := RoleSplit.Top + RoleSplit.Height + ScaleY(4);
  Gap := ScaleX(8);
  HalfW := (RolePage.SurfaceWidth - Gap) div 2;
  PanelH := ScaleY(100);

  { --- SERVER (left half) --- }
  RoleServerPanel := MakeRolePanel(RolePage, 0, TopY, HalfW, PanelH);
  RoleServerPanel.OnClick := @SelectServer;
  RoleServerPanel.OnDblClick := @AdvanceServer;

  RoleAccentServer := MakeAccentBar(RoleServerPanel);
  RoleAccentServer.OnClick := @SelectServer;
  RoleAccentServer.OnDblClick := @AdvanceServer;

  RoleNumServer := TNewStaticText.Create(RolePage);
  RoleNumServer.Parent := RoleServerPanel;
  RoleNumServer.Caption := '2';
  RoleNumServer.Font.Name := 'Segoe UI';
  RoleNumServer.Font.Size := 20;
  RoleNumServer.Font.Style := [fsBold];
  RoleNumServer.Font.Color := clGray;
  RoleNumServer.AutoSize := True;
  RoleNumServer.Left := ScaleX(12);
  RoleNumServer.Top := ScaleY(8);
  RoleNumServer.Cursor := crHand;
  RoleNumServer.OnClick := @SelectServer;
  RoleNumServer.OnDblClick := @AdvanceServer;

  RoleServer := TRadioButton.Create(RolePage);
  RoleServer.Parent := RoleServerPanel;
  RoleServer.Caption := '';
  RoleServer.Left := ScaleX(36);
  RoleServer.Top := ScaleY(6);
  RoleServer.Width := ScaleX(18);
  RoleServer.Height := ScaleY(18);
  RoleServer.OnClick := @SelectServer;
  RoleServer.OnDblClick := @AdvanceServer;

  RoleTitleServer := TNewStaticText.Create(RolePage);
  RoleTitleServer.Parent := RoleServerPanel;
  RoleTitleServer.Caption := 'SERVER ONLY  —  holds the data';
  RoleTitleServer.Font.Name := 'Segoe UI';
  RoleTitleServer.Font.Size := 11;
  RoleTitleServer.Font.Style := [fsBold];
  RoleTitleServer.AutoSize := True;
  RoleTitleServer.Left := ScaleX(54);
  RoleTitleServer.Top := ScaleY(6);
  RoleTitleServer.Cursor := crHand;
  RoleTitleServer.OnClick := @SelectServer;
  RoleTitleServer.OnDblClick := @AdvanceServer;

  RoleBadgeServer := TNewStaticText.Create(RolePage);
  RoleBadgeServer.Parent := RoleServerPanel;
  RoleBadgeServer.Caption := 'Host PC';
  RoleBadgeServer.Font.Name := 'Segoe UI';
  RoleBadgeServer.Font.Size := 8;
  RoleBadgeServer.Font.Style := [fsBold];
  RoleBadgeServer.Font.Color := clMaroon;
  RoleBadgeServer.AutoSize := True;
  RoleBadgeServer.Left := ScaleX(54);
  RoleBadgeServer.Top := ScaleY(26);
  RoleBadgeServer.Cursor := crHand;
  RoleBadgeServer.OnClick := @SelectServer;
  RoleBadgeServer.OnDblClick := @AdvanceServer;

  RolePickServer := TNewStaticText.Create(RolePage);
  RolePickServer.Parent := RoleServerPanel;
  RolePickServer.Caption := 'Click to choose';
  RolePickServer.Font.Name := 'Segoe UI';
  RolePickServer.Font.Size := 8;
  RolePickServer.Font.Style := [fsBold];
  RolePickServer.Font.Color := clGray;
  RolePickServer.AutoSize := True;
  RolePickServer.Left := RoleServerPanel.Width - ScaleX(92);
  RolePickServer.Top := ScaleY(26);
  RolePickServer.Cursor := crHand;
  RolePickServer.OnClick := @SelectServer;
  RolePickServer.OnDblClick := @AdvanceServer;

  RoleHintServer := TNewStaticText.Create(RolePage);
  RoleHintServer.Parent := RoleServerPanel;
  RoleHintServer.Caption :=
    'Includes: Server (API/DB).  Skips: Client.'#13#10 +
    'Pick if: this PC is the shared host; other desks get Client only.';
  RoleHintServer.Font.Name := 'Segoe UI';
  RoleHintServer.Font.Size := 8;
  RoleHintServer.Left := ScaleX(12);
  RoleHintServer.Top := ScaleY(46);
  RoleHintServer.Width := RoleServerPanel.Width - ScaleX(20);
  RoleHintServer.AutoSize := False;
  RoleHintServer.WordWrap := True;
  RoleHintServer.Height := ScaleY(48);
  RoleHintServer.Cursor := crHand;
  RoleHintServer.OnClick := @SelectServer;
  RoleHintServer.OnDblClick := @AdvanceServer;

  { --- CLIENT (right half) --- }
  RoleClientPanel := MakeRolePanel(RolePage, HalfW + Gap, TopY, HalfW, PanelH);
  RoleClientPanel.OnClick := @SelectClient;
  RoleClientPanel.OnDblClick := @AdvanceClient;

  RoleAccentClient := MakeAccentBar(RoleClientPanel);
  RoleAccentClient.OnClick := @SelectClient;
  RoleAccentClient.OnDblClick := @AdvanceClient;

  RoleNumClient := TNewStaticText.Create(RolePage);
  RoleNumClient.Parent := RoleClientPanel;
  RoleNumClient.Caption := '3';
  RoleNumClient.Font.Name := 'Segoe UI';
  RoleNumClient.Font.Size := 20;
  RoleNumClient.Font.Style := [fsBold];
  RoleNumClient.Font.Color := clGray;
  RoleNumClient.AutoSize := True;
  RoleNumClient.Left := ScaleX(12);
  RoleNumClient.Top := ScaleY(8);
  RoleNumClient.Cursor := crHand;
  RoleNumClient.OnClick := @SelectClient;
  RoleNumClient.OnDblClick := @AdvanceClient;

  RoleClient := TRadioButton.Create(RolePage);
  RoleClient.Parent := RoleClientPanel;
  RoleClient.Caption := '';
  RoleClient.Left := ScaleX(36);
  RoleClient.Top := ScaleY(6);
  RoleClient.Width := ScaleX(18);
  RoleClient.Height := ScaleY(18);
  RoleClient.OnClick := @SelectClient;
  RoleClient.OnDblClick := @AdvanceClient;

  RoleTitleClient := TNewStaticText.Create(RolePage);
  RoleTitleClient.Parent := RoleClientPanel;
  RoleTitleClient.Caption := 'CLIENT ONLY  —  the desk you work at';
  RoleTitleClient.Font.Name := 'Segoe UI';
  RoleTitleClient.Font.Size := 11;
  RoleTitleClient.Font.Style := [fsBold];
  RoleTitleClient.AutoSize := True;
  RoleTitleClient.Left := ScaleX(54);
  RoleTitleClient.Top := ScaleY(6);
  RoleTitleClient.Cursor := crHand;
  RoleTitleClient.OnClick := @SelectClient;
  RoleTitleClient.OnDblClick := @AdvanceClient;

  RoleBadgeClient := TNewStaticText.Create(RolePage);
  RoleBadgeClient.Parent := RoleClientPanel;
  RoleBadgeClient.Caption := 'Workstation';
  RoleBadgeClient.Font.Name := 'Segoe UI';
  RoleBadgeClient.Font.Size := 8;
  RoleBadgeClient.Font.Style := [fsBold];
  RoleBadgeClient.Font.Color := clTeal;
  RoleBadgeClient.AutoSize := True;
  RoleBadgeClient.Left := ScaleX(54);
  RoleBadgeClient.Top := ScaleY(26);
  RoleBadgeClient.Cursor := crHand;
  RoleBadgeClient.OnClick := @SelectClient;
  RoleBadgeClient.OnDblClick := @AdvanceClient;

  RolePickClient := TNewStaticText.Create(RolePage);
  RolePickClient.Parent := RoleClientPanel;
  RolePickClient.Caption := 'Click to choose';
  RolePickClient.Font.Name := 'Segoe UI';
  RolePickClient.Font.Size := 8;
  RolePickClient.Font.Style := [fsBold];
  RolePickClient.Font.Color := clGray;
  RolePickClient.AutoSize := True;
  RolePickClient.Left := RoleClientPanel.Width - ScaleX(92);
  RolePickClient.Top := ScaleY(26);
  RolePickClient.Cursor := crHand;
  RolePickClient.OnClick := @SelectClient;
  RolePickClient.OnDblClick := @AdvanceClient;

  RoleHintClient := TNewStaticText.Create(RolePage);
  RoleHintClient.Parent := RoleClientPanel;
  RoleHintClient.Caption :=
    'Includes: Client (desktop).  Skips: Server.'#13#10 +
    'Pick if: Server already runs elsewhere — this PC is just a desk.';
  RoleHintClient.Font.Name := 'Segoe UI';
  RoleHintClient.Font.Size := 8;
  RoleHintClient.Left := ScaleX(12);
  RoleHintClient.Top := ScaleY(46);
  RoleHintClient.Width := RoleClientPanel.Width - ScaleX(20);
  RoleHintClient.AutoSize := False;
  RoleHintClient.WordWrap := True;
  RoleHintClient.Height := ScaleY(48);
  RoleHintClient.Cursor := crHand;
  RoleHintClient.OnClick := @SelectClient;
  RoleHintClient.OnDblClick := @AdvanceClient;

  RoleKeys := TNewStaticText.Create(RolePage);
  RoleKeys.Parent := RolePage.Surface;
  RoleKeys.Caption :=
    'Keys:  [1] Both    [2] Server    [3] Client    ·    arrows move    ·    Enter / Space continues';
  RoleKeys.Font.Name := 'Segoe UI';
  RoleKeys.Font.Size := 9;
  RoleKeys.Font.Style := [fsBold];
  RoleKeys.Font.Color := clNavy;
  RoleKeys.AutoSize := True;
  RoleKeys.Left := 0;
  RoleKeys.Top := RoleServerPanel.Top + RoleServerPanel.Height + ScaleY(4);

  RoleSummaryPanel := TPanel.Create(RolePage);
  RoleSummaryPanel.Parent := RolePage.Surface;
  RoleSummaryPanel.Left := 0;
  RoleSummaryPanel.Top := RoleKeys.Top + RoleKeys.Height + ScaleY(4);
  RoleSummaryPanel.Width := RolePage.SurfaceWidth;
  RoleSummaryPanel.Height := ScaleY(48);
  RoleSummaryPanel.BevelOuter := bvLowered;
  RoleSummaryPanel.BevelWidth := 1;
  RoleSummaryPanel.Color := clInfoBk;
  RoleSummaryPanel.ParentBackground := False;

  RoleSummary := TNewStaticText.Create(RolePage);
  RoleSummary.Parent := RoleSummaryPanel;
  RoleSummary.Caption :=
    '▶ YOUR CHOICE →  BOTH — Includes: Server + Client  ·  Skips: nothing'#13#10 +
    'After install this PC will run the FULL app — Server and Client together. Best if unsure.  —  Next installs that (and only that).';
  RoleSummary.Font.Name := 'Segoe UI';
  RoleSummary.Font.Size := 9;
  RoleSummary.Font.Style := [fsBold];
  RoleSummary.Font.Color := clNavy;
  RoleSummary.AutoSize := False;
  RoleSummary.WordWrap := True;
  RoleSummary.Left := ScaleX(8);
  RoleSummary.Top := ScaleY(4);
  RoleSummary.Width := RoleSummaryPanel.Width - ScaleX(16);
  RoleSummary.Height := ScaleY(40);

  RoleFoot := TNewStaticText.Create(RolePage);
  RoleFoot.Parent := RolePage.Surface;
  RoleFoot.Caption :=
    'Click a card (or double-click to continue). Already sure of the role? Prefer the smaller dedicated packages:'#13#10 +
    'CoalesceServerSetup.exe  or  CoalesceClientSetup.exe — no chooser, one payload each.';
  RoleFoot.Font.Name := 'Segoe UI';
  RoleFoot.Font.Size := 8;
  RoleFoot.Font.Color := clGray;
  RoleFoot.AutoSize := False;
  RoleFoot.WordWrap := True;
  RoleFoot.Left := 0;
  RoleFoot.Top := RoleSummaryPanel.Top + RoleSummaryPanel.Height + ScaleY(4);
  RoleFoot.Width := RolePage.SurfaceWidth;
  RoleFoot.Height := ScaleY(32);

  WizardForm.OnKeyDown := @WizardKeyDown;
  WizardForm.KeyPreview := True;
  PaintRolePanels();
  UpdateNextButtonForRole();
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

procedure CurPageChanged(CurPageID: Integer);
begin
#if IsChooser == "1"
  if (RolePage <> nil) and (CurPageID = RolePage.ID) then
  begin
    UpdateNextButtonForRole();
    { Land keyboard focus on the selected card so Enter / arrows feel obvious. }
    if RoleServer.Checked then
      WizardForm.ActiveControl := RoleServer
    else if RoleClient.Checked then
      WizardForm.ActiveControl := RoleClient
    else
      WizardForm.ActiveControl := RoleBoth;
  end
  else if CurPageID = wpFinished then
  begin
    WizardForm.NextButton.Caption := SetupMessage(msgButtonFinish);
    { Match the finish copy to the card they picked — Client-only
      should never look like a Server install just finished. }
    if RoleClient.Checked then
      WizardForm.FinishedLabel.Caption :=
        'Done.'#13#10 +
        'Installed: ' + ChoiceSummary() + #13#10 +
        ChoiceOutcome() + #13#10#13#10 +
        'Start Coalesce Server first, then open the Client.'#13#10 +
        'If Server is on another machine, set the API URL in Client Settings.'
    else if RoleServer.Checked then
      WizardForm.FinishedLabel.Caption :=
        'Done.'#13#10 +
        'Installed: ' + ChoiceSummary() + #13#10 +
        ChoiceOutcome() + #13#10#13#10 +
        'Default login: admin / admin'#13#10 +
        'API: http://127.0.0.1:8000'#13#10#13#10 +
        'Next: start Server, then install Client on this PC or others.'
    else
      WizardForm.FinishedLabel.Caption :=
        'Done.'#13#10 +
        'Installed: ' + ChoiceSummary() + #13#10 +
        ChoiceOutcome() + #13#10#13#10 +
        'Default login: admin / admin'#13#10 +
        'API: http://127.0.0.1:8000'#13#10#13#10 +
        'Tip: start Server before Client when both live on this PC.';
  end
  else
  begin
    WizardForm.NextButton.Caption := SetupMessage(msgButtonNext);
    RestateRoleOnPage();
  end;
#elif Package == "server"
  if CurPageID = wpFinished then
    WizardForm.FinishedLabel.Caption :=
      'Installed: SERVER ONLY (this package never installs the Client).'#13#10#13#10 +
      'Default login: admin / admin'#13#10 +
      'API: http://127.0.0.1:8000'#13#10#13#10 +
      'Next: start Server, then install Coalesce Client on this PC or others.';
#elif Package == "client"
  if CurPageID = wpFinished then
    WizardForm.FinishedLabel.Caption :=
      'Installed: CLIENT ONLY (this package never installs the Server).'#13#10#13#10 +
      'Start Coalesce Server first, then open the Client.'#13#10 +
      'If Server is on another machine, set the API URL in Client Settings.';
#endif
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
  S := 'This PC will get:' + NewLine;
  S := S + Space + ChoiceSummary() + NewLine;
  S := S + Space + ChoiceOutcome() + NewLine + NewLine;
  S := S + MemoDirInfo + NewLine + NewLine;
  if MemoGroupInfo <> '' then
    S := S + MemoGroupInfo + NewLine + NewLine;
  if MemoTasksInfo <> '' then
    S := S + MemoTasksInfo + NewLine + NewLine;
  if RoleClient.Checked then
    S := S + 'Note: start Coalesce Server before opening the Client.' + NewLine
  else if RoleServer.Checked then
    S := S + 'Other desks: install CoalesceClientSetup.exe and connect here.' + NewLine
  else
    S := S + 'Tip: after setup, launch Server first, then Client.' + NewLine;
  Result := S;
end;
#endif

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    WriteCapacityConfig();
end;
