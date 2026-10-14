#define MyAppName "CM Gefährdungsbeurteilung"
#define MyAppVersion "0.1.7"
#define MyAppExeName "CM.Gefaehrdungsbeurteilung.exe"

[Setup]
; AppId bleibt bei ALLEN Updates identisch.
AppId={{2836B6AC-8645-4A47-B1EE-C5840ECF1E07}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=CM
AppCopyright=Copyright (c) CM
DefaultDirName={localappdata}\Programs\CM Gefaehrdungsbeurteilung
DefaultGroupName=CM Gefaehrdungsbeurteilung
UsePreviousAppDir=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\release
OutputBaseFilename=CM-Gefaehrdungsbeurteilung-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion=0.1.7.1
MinVersion=10.0.17763
DisableWelcomePage=no
; Inno Setup legt ausschließlich Programmdateien unter {app} ab.
; Nutzdaten unter %LOCALAPPDATA%\CM\Gefaehrdungsbeurteilung bleiben bei De-/Neuinstallation unangetastet.

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\CM Gefaehrdungsbeurteilung"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\CM Gefaehrdungsbeurteilung"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Desktopverknüpfung erstellen"; GroupDescription: "Zusätzliche Symbole:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "CM Gefährdungsbeurteilung starten"; Flags: nowait postinstall skipifsilent
