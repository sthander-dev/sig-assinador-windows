#define MyAppName "Assinador SIG"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sthander Info"
#define MyAppURL "https://sig.sthanderinfo.com.br"
#define MyAppExeName "AssinadorSIG.exe"

[Setup]
AppId={{DA7FD465-2721-4BBF-A483-3DC6516D3D62}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\Sthander Info\Assinador SIG
DefaultGroupName=Assinador SIG
OutputDir=.
OutputBaseFilename=Assinador-SIG-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
ChangesAssociations=yes

[Files]
Source: "publicacao\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Assinador SIG"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Assinador SIG"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"; Flags: checkedonce

[Registry]
Root: HKA; Subkey: "Software\Classes\.sigjob"; ValueType: string; ValueName: ""; ValueData: "SIG.SigningJob"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\SIG.SigningJob"; ValueType: string; ValueName: ""; ValueData: "Autorização do Assinador SIG"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SIG.SigningJob\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\SIG.SigningJob\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir o Assinador SIG"; Flags: nowait postinstall skipifsilent
