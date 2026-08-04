; Ledgerly ERP — Client installer
#define MyAppName "Ledgerly Client"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Ledgerly"
#define MyAppExeName "LedgerlyClient.exe"

[Setup]
AppId={{B8D4F2C3-5E69-4A02-8D3B-91F0A2C4E6B8}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Ledgerly\Client
DefaultGroupName=Ledgerly
DisableProgramGroupPage=yes
OutputDir=..\dist\installers
OutputBaseFilename=LedgerlyClientSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Start Ledgerly Client when I log in"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "..\dist\LedgerlyClient\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Ledgerly Client"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall Ledgerly Client"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Ledgerly Client"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\Ledgerly Client"; Filename: "{app}\{#MyAppExeName}"; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Ledgerly Client now"; Flags: nowait postinstall skipifsilent

[Code]
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpReady then
  begin
    MsgBox('Ledgerly Client needs the Ledgerly Server running at http://127.0.0.1:8000.'#13#10#13#10'Install and start the server first if you have not already.', mbInformation, MB_OK);
  end;
end;
