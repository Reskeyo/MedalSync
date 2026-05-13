; MedalSync installer (Inno Setup)
; Build: iscc installer\MedalSync.iss

#define MyAppName "MedalSync"
#define MyAppVersion "1.2.1"
#define MyAppPublisher "Reskeyo"
#define MyAppExeName "MedalSync.exe"
#define MyAppSourceDir "..\publish"

[Setup]
AppId={{A5C59C0C-6E2F-4A1F-A0C9-3A7E8E9C6D7A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=MedalSync-Setup
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "autorun"; Description: "Start with Windows"; Flags: checkedonce
Name: "runapp"; Description: "Start MedalSync after setup"; Flags: checkedonce

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "MedalSync"; ValueData: """{app}\{#MyAppExeName}"""; Tasks: autorun; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Start MedalSync"; Tasks: runapp; Flags: nowait postinstall skipifsilent
