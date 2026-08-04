; Ledgerly ERP — combined Client / Server installer (Windows 10+)
#define MyAppName "Ledgerly ERP"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Ledgerly"
#define MyAppURL "https://github.com/devildog5x5/ERP"

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
; Windows 10 and later only
MinVersion=10.0
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\Server\LedgerlyServer.exe
InfoBeforeFile=
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
Name: "server"; Description: "Ledgerly Server (API on port 8000)"; Types: full server custom; Flags: checkablealone
Name: "client"; Description: "Ledgerly Client (UI on port 3000)"; Types: full client custom; Flags: checkablealone

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
