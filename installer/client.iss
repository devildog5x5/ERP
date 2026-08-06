; Ledgerly ERP — Client-only installer (Windows 10+)
#include "version.iss"

#define MyAppName "Ledgerly Client"
#define MyAppExeName "LedgerlyClient.exe"

[Setup]
AppId={{B8D4F2C3-5E69-4A02-8D3B-91F0A2C4E6B8}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\Ledgerly\Client
DefaultGroupName=Ledgerly
DisableProgramGroupPage=yes
OutputDir=..\dist\installers
OutputBaseFilename=LedgerlyClientSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
MinVersion=10.0
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
InfoBeforeFile=info-client.txt
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Ledgerly ERP client installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

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

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\Ledgerly\Client"

[Code]
function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  if Version.Major < 10 then
  begin
    MsgBox('Ledgerly Client requires Windows 10 or later.', mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpReady then
  begin
    MsgBox(
      'Before you open the Client:'#13#10#13#10 +
      '1. Install Ledgerly Server (if it is not already installed).'#13#10 +
      '2. Start Ledgerly Server so it is listening on port 8000.'#13#10#13#10 +
      'The Client talks to http://127.0.0.1:8000 by default.',
      mbInformation,
      MB_OK
    );
  end;
end;
