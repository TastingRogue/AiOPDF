#define AppName "AiOPDF"
#define VersionHandle FileOpen(SourcePath + "..\VERSION")
#define AppVersion Trim(FileRead(VersionHandle))
#expr FileClose(VersionHandle)
[Setup]
AppId={{8CC7E572-E669-41E9-9E89-BD1B62F3F422}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=TastingRogue
AppPublisherURL=https://github.com/TastingRogue/AiOPDF
AppSupportURL=https://github.com/TastingRogue/AiOPDF/issues
AppUpdatesURL=https://github.com/TastingRogue/AiOPDF/releases
LicenseFile=..\LICENSE
DefaultDirName={localappdata}\Programs\AiOPDF
DefaultGroupName=AiOPDF
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installers
OutputBaseFilename=AiOPDF-{#AppVersion}-windows-x64-setup
SetupIconFile=..\src\AiOPDF\Assets\app-icon.ico
UninstallDisplayIcon={app}\AiOPDF.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
Source: "..\artifacts\windows-x64-{#AppVersion}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\AiOPDF"; Filename: "{app}\AiOPDF.exe"
Name: "{autodesktop}\AiOPDF"; Filename: "{app}\AiOPDF.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\AiOPDF.exe"; Description: "{cm:LaunchProgram,AiOPDF}"; Flags: nowait postinstall skipifsilent






