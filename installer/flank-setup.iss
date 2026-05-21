; Flank - Inno Setup installer script
; Requires Inno Setup 6: https://jrsoftware.org/isinfo.php
;
; To compile:
;   iscc installer\flank-setup.iss
; Or open in the Inno Setup IDE and press Ctrl+F9.
;
; Output: installer\Output\FlankSetup-<version>.exe

#define AppName    "Flank"
#define AppVersion "1.0.0"
#define AppPublisher "Joseph Driedger"
#define BuildDir   "..\Flank\Build"
#define ExeName    "Flank.exe"

[Setup]
AppId={{E3A1B2C4-7F8D-4E5A-9C0B-1D2E3F4A5B6C}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
; Require 64-bit Windows
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
; Compression
Compression=lzma2/ultra64
SolidCompression=yes
LZMANumBlockThreads=4
; Output
OutputDir=Output
OutputBaseFilename=FlankSetup-{#AppVersion}
; Minimum Windows version: Windows 10 (6.2 kernel, but practically Win10)
MinVersion=10.0
; Misc
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#ExeName}
UninstallDisplayName={#AppName}
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
; --- Core game files ---
Source: "{#BuildDir}\{#ExeName}";          DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\UnityPlayer.dll";     DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\UnityCrashHandler64.exe"; DestDir: "{app}"; Flags: ignoreversion

; --- D3D12 runtime ---
Source: "{#BuildDir}\D3D12\*";             DestDir: "{app}\D3D12"; Flags: ignoreversion recursesubdirs createallsubdirs

; --- Game data ---
Source: "{#BuildDir}\Flank_Data\*";        DestDir: "{app}\Flank_Data"; Flags: ignoreversion recursesubdirs createallsubdirs

; --- Mono runtime ---
Source: "{#BuildDir}\MonoBleedingEdge\*";  DestDir: "{app}\MonoBleedingEdge"; Flags: ignoreversion recursesubdirs createallsubdirs

; Note: Flank_BurstDebugInformation_DoNotShip is intentionally excluded.

[Icons]
; Start Menu
Name: "{group}\{#AppName}";         Filename: "{app}\{#ExeName}"; WorkingDir: "{app}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
; Desktop (optional task)
Name: "{autodesktop}\{#AppName}";   Filename: "{app}\{#ExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#ExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Remove any files the game writes at runtime in its install directory
Type: filesandordirs; Name: "{app}"
