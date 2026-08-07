; Ledgerly ERP — chooser installer (Windows 10+)
; One Setup.exe: Server, Client, or Both.
#include "version.iss"

#define MyAppName "Ledgerly ERP"

[Setup]
AppId={{C9E5A1D4-6F70-4B13-9E4C-A2B1D3E5F708}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\Ledgerly
DefaultGroupName=Ledgerly
DisableProgramGroupPage=no
OutputDir=..\dist\installers
OutputBaseFilename=LedgerlySetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
MinVersion=10.0
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\Server\LedgerlyServer.exe
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Ledgerly ERP installer (Server / Client / Both)
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
ShowComponentSizes=no
AlwaysShowComponentsList=no
FlatComponentsList=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

; Silent installs: LedgerlySetup.exe /TYPE=full|server|client
[Types]
Name: "full"; Description: "Both Server and Client"
Name: "server"; Description: "Server only"
Name: "client"; Description: "Client only"
Name: "custom"; Description: "Custom"; Flags: iscustom

[Components]
Name: "server"; Description: "Ledgerly Server"; Types: full server custom; Flags: checkablealone
Name: "client"; Description: "Ledgerly Client"; Types: full client custom; Flags: checkablealone

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostartserver"; Description: "Start Ledgerly Server when I log in"; GroupDescription: "Startup options:"; Components: server; Flags: unchecked
Name: "autostartclient"; Description: "Start Ledgerly Client when I log in"; GroupDescription: "Startup options:"; Components: client; Flags: unchecked

[Files]
Source: "..\dist\LedgerlyServer\*"; DestDir: "{app}\Server"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: server
Source: "..\dist\LedgerlyClient\*"; DestDir: "{app}\Client"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: client

[Icons]
Name: "{group}\Ledgerly Server"; Filename: "{app}\Server\LedgerlyServer.exe"; Components: server
Name: "{group}\Ledgerly Client"; Filename: "{app}\Client\LedgerlyClient.exe"; Components: client
Name: "{group}\API Documentation"; Filename: "http://127.0.0.1:8000/docs"; Components: server
Name: "{group}\Uninstall Ledgerly ERP"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Ledgerly Server"; Filename: "{app}\Server\LedgerlyServer.exe"; Tasks: desktopicon; Components: server
Name: "{autodesktop}\Ledgerly Client"; Filename: "{app}\Client\LedgerlyClient.exe"; Tasks: desktopicon; Components: client
Name: "{userstartup}\Ledgerly Server"; Filename: "{app}\Server\LedgerlyServer.exe"; Tasks: autostartserver; Components: server
Name: "{userstartup}\Ledgerly Client"; Filename: "{app}\Client\LedgerlyClient.exe"; Tasks: autostartclient; Components: client

[Run]
Filename: "{app}\Server\LedgerlyServer.exe"; Description: "Launch Ledgerly Server now"; Flags: nowait postinstall skipifsilent unchecked; Components: server
Filename: "{app}\Client\LedgerlyClient.exe"; Description: "Launch Ledgerly Client now"; Flags: nowait postinstall skipifsilent unchecked; Components: client

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\Ledgerly\Server"
Type: filesandordirs; Name: "{localappdata}\Ledgerly\Client"

[Code]
var
  ChoicePage: TWizardPage;
  RadioBoth: TNewRadioButton;
  RadioServer: TNewRadioButton;
  RadioClient: TNewRadioButton;
  LabelBothHint: TNewStaticText;
  LabelServerHint: TNewStaticText;
  LabelClientHint: TNewStaticText;
  LabelIntro: TNewStaticText;

procedure ApplyChoiceToComponents;
begin
  // "!" deselects a component.
  if RadioServer.Checked then
  begin
    WizardSelectComponents('server,!client');
    WizardForm.TypesCombo.ItemIndex := WizardForm.TypesCombo.Items.IndexOf('Server only');
    if WizardForm.TypesCombo.ItemIndex < 0 then
      WizardForm.TypesCombo.ItemIndex := 1;
  end
  else if RadioClient.Checked then
  begin
    WizardSelectComponents('!server,client');
    WizardForm.TypesCombo.ItemIndex := WizardForm.TypesCombo.Items.IndexOf('Client only');
    if WizardForm.TypesCombo.ItemIndex < 0 then
      WizardForm.TypesCombo.ItemIndex := 2;
  end
  else
  begin
    WizardSelectComponents('server,client');
    WizardForm.TypesCombo.ItemIndex := WizardForm.TypesCombo.Items.IndexOf('Both Server and Client');
    if WizardForm.TypesCombo.ItemIndex < 0 then
      WizardForm.TypesCombo.ItemIndex := 0;
  end;
end;

procedure SyncRadiosFromType;
var
  TypeName: String;
begin
  TypeName := WizardSetupType(False);
  if CompareText(TypeName, 'server') = 0 then
    RadioServer.Checked := True
  else if CompareText(TypeName, 'client') = 0 then
    RadioClient.Checked := True
  else
    RadioBoth.Checked := True;
end;

function ChoiceSummary: String;
begin
  if RadioServer.Checked then
    Result := 'Server only — API and database on this PC'
  else if RadioClient.Checked then
    Result := 'Client only — work screen (needs a running Server)'
  else
    Result := 'Both — Server and Client on this PC';
end;

procedure CreateChoicePage;
var
  TopPos: Integer;
begin
  ChoicePage := CreateCustomPage(
    wpWelcome,
    'What do you want to install?',
    'Choose Server, Client, or Both for this computer.'
  );

  LabelIntro := TNewStaticText.Create(ChoicePage);
  LabelIntro.Parent := ChoicePage.Surface;
  LabelIntro.Left := 0;
  LabelIntro.Top := 0;
  LabelIntro.Width := ChoicePage.SurfaceWidth;
  LabelIntro.AutoSize := False;
  LabelIntro.WordWrap := True;
  LabelIntro.Caption :=
    'Ledgerly has two parts: a Server that stores your data, and a Client ' +
    'you work in. Pick what belongs on THIS computer.';
  LabelIntro.Height := ScaleY(40);

  TopPos := LabelIntro.Top + LabelIntro.Height + ScaleY(14);

  RadioBoth := TNewRadioButton.Create(ChoicePage);
  RadioBoth.Parent := ChoicePage.Surface;
  RadioBoth.Left := 0;
  RadioBoth.Top := TopPos;
  RadioBoth.Width := ChoicePage.SurfaceWidth;
  RadioBoth.Height := ScaleY(22);
  RadioBoth.Caption := 'BOTH  —  Server and Client';
  RadioBoth.Checked := True;
  RadioBoth.Font.Style := [fsBold];

  LabelBothHint := TNewStaticText.Create(ChoicePage);
  LabelBothHint.Parent := ChoicePage.Surface;
  LabelBothHint.Left := ScaleX(22);
  LabelBothHint.Top := RadioBoth.Top + RadioBoth.Height + ScaleY(2);
  LabelBothHint.Width := ChoicePage.SurfaceWidth - ScaleX(22);
  LabelBothHint.AutoSize := False;
  LabelBothHint.WordWrap := True;
  LabelBothHint.Caption :=
    'Everything on this PC. Best for a single-computer shop. ' +
    'Start the Server first, then the Client.';
  LabelBothHint.Height := ScaleY(36);

  TopPos := LabelBothHint.Top + LabelBothHint.Height + ScaleY(12);

  RadioServer := TNewRadioButton.Create(ChoicePage);
  RadioServer.Parent := ChoicePage.Surface;
  RadioServer.Left := 0;
  RadioServer.Top := TopPos;
  RadioServer.Width := ChoicePage.SurfaceWidth;
  RadioServer.Height := ScaleY(22);
  RadioServer.Caption := 'SERVER  —  data and API';
  RadioServer.Font.Style := [fsBold];

  LabelServerHint := TNewStaticText.Create(ChoicePage);
  LabelServerHint.Parent := ChoicePage.Surface;
  LabelServerHint.Left := ScaleX(22);
  LabelServerHint.Top := RadioServer.Top + RadioServer.Height + ScaleY(2);
  LabelServerHint.Width := ChoicePage.SurfaceWidth - ScaleX(22);
  LabelServerHint.AutoSize := False;
  LabelServerHint.WordWrap := True;
  LabelServerHint.Caption :=
    'Install once on the machine that stores inventory and orders. ' +
    'Listens on http://127.0.0.1:8000';
  LabelServerHint.Height := ScaleY(36);

  TopPos := LabelServerHint.Top + LabelServerHint.Height + ScaleY(12);

  RadioClient := TNewRadioButton.Create(ChoicePage);
  RadioClient.Parent := ChoicePage.Surface;
  RadioClient.Left := 0;
  RadioClient.Top := TopPos;
  RadioClient.Width := ChoicePage.SurfaceWidth;
  RadioClient.Height := ScaleY(22);
  RadioClient.Caption := 'CLIENT  —  the screen you work in';
  RadioClient.Font.Style := [fsBold];

  LabelClientHint := TNewStaticText.Create(ChoicePage);
  LabelClientHint.Parent := ChoicePage.Surface;
  LabelClientHint.Left := ScaleX(22);
  LabelClientHint.Top := RadioClient.Top + RadioClient.Height + ScaleY(2);
  LabelClientHint.Width := ChoicePage.SurfaceWidth - ScaleX(22);
  LabelClientHint.AutoSize := False;
  LabelClientHint.WordWrap := True;
  LabelClientHint.Caption :=
    'Install on each workstation. Needs a running Server ' +
    '(on this PC or another machine on your network).';
  LabelClientHint.Height := ScaleY(36);
end;

function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  if Version.Major < 10 then
  begin
    MsgBox('Ledgerly ERP requires Windows 10 or later.', mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;

procedure InitializeWizard;
begin
  CreateChoicePage;
  SyncRadiosFromType;
  ApplyChoiceToComponents;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  // Custom radios replace the stock component checklist.
  Result := (PageID = wpSelectComponents);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (ChoicePage <> nil) and (CurPageID = ChoicePage.ID) then
  begin
    if (not RadioBoth.Checked) and (not RadioServer.Checked) and (not RadioClient.Checked) then
    begin
      MsgBox('Select Server, Client, or Both to continue.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    ApplyChoiceToComponents;
  end;
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo,
  MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  S: String;
begin
  S := 'Installation choice:' + NewLine;
  S := S + Space + ChoiceSummary + NewLine + NewLine;
  S := S + MemoDirInfo + NewLine + NewLine;
  if MemoGroupInfo <> '' then
    S := S + MemoGroupInfo + NewLine + NewLine;
  if MemoTasksInfo <> '' then
    S := S + MemoTasksInfo + NewLine + NewLine;
  if RadioClient.Checked then
    S := S + 'Note: start Ledgerly Server before opening the Client.' + NewLine
  else if RadioBoth.Checked then
    S := S + 'Tip: after setup, launch Server first, then Client.' + NewLine;
  Result := S;
end;
