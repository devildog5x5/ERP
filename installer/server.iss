; Ledgerly ERP — Server installer
#define MyAppName "Ledgerly Server"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Ledgerly"
#define MyAppExeName "LedgerlyServer.exe"
#define MyAppURL "http://127.0.0.1:8000"

[Setup]
AppId={{A7C3E1B2-4D58-4F91-9C2A-81E0B3D5F7A9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Ledgerly\Server
DefaultGroupName=Ledgerly
DisableProgramGroupPage=yes
OutputDir=..\dist\installers
OutputBaseFilename=LedgerlyServerSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=
InfoBeforeFile=
LicenseFile=

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Start Ledgerly Server when I log in"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "..\dist\LedgerlyServer\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Ledgerly Server"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\API Documentation"; Filename: "http://127.0.0.1:8000/docs"
Name: "{group}\Uninstall Ledgerly Server"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Ledgerly Server"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\Ledgerly Server"; Filename: "{app}\{#MyAppExeName}"; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Ledgerly Server now"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\Ledgerly\Server"
