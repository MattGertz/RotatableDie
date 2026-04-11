[Setup]
AppName=Matt's Yacht
AppVersion=1.0
AppPublisher=Matt
DefaultDirName={autopf}\MattsYacht
DefaultGroupName=Matt's Yacht
UninstallDisplayIcon={app}\YachtDiceMaui.exe
OutputDir=..\Setup
OutputBaseFilename=MattsYachtSetup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "bin\Release\net9.0-windows10.0.19041.0\win10-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Matt's Yacht"; Filename: "{app}\YachtDiceMaui.exe"
Name: "{commondesktop}\Matt's Yacht"; Filename: "{app}\YachtDiceMaui.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\YachtDiceMaui.exe"; Description: "Launch Matt's Yacht"; Flags: nowait postinstall skipifsilent
