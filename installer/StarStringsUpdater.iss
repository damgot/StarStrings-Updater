; Script Inno Setup pour StarStrings Updater.
; Compilation : voir installer\build-installer.ps1 (qui publie l'app .NET puis appelle ISCC.exe sur ce fichier).
; Prérequis sur la machine de build : Inno Setup 6 (https://jrsoftware.org/isinfo.php).

#define MyAppName "StarStrings Updater"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "StarStrings Updater"
#define MyAppExeName "StarStringsUpdater.exe"
#define MyPublishDir "..\src\StarStringsUpdater\bin\Release\net8.0\win-x64\publish"
#define MyIconFile "..\src\StarStringsUpdater\Assets\app.ico"

[Setup]
SetupIconFile={#MyIconFile}
AppId={{6C2C6C0B-9E7C-4B7C-9C0B-6B7A7A9E0B1A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\StarStringsUpdater
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Installation par utilisateur, sans besoin de droits administrateur (pas d'invite UAC).
PrivilegesRequired=lowest
OutputDir=Output
OutputBaseFilename=StarStringsUpdater-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; state.json is written by the app itself at runtime, so it isn't part of [Files] and Inno
; wouldn't remove it (or the now-non-empty {app} folder) on uninstall without this entry.
[UninstallDelete]
Type: files; Name: "{app}\state.json"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
