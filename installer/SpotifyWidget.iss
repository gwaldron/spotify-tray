#ifndef AppVersion
  #error AppVersion must be supplied by build-installer.ps1
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif
#ifndef ArtifactDir
  #define ArtifactDir "..\artifacts\release"
#endif

[Setup]
AppId={{6A49B803-21CD-4F4F-B95C-B2BF215976ED}
AppName=Spotify Widget
AppVersion={#AppVersion}
AppPublisher=gwaldron
AppPublisherURL=https://github.com/gwaldron/spotify-tray
AppSupportURL=https://github.com/gwaldron/spotify-tray/issues
AppUpdatesURL=https://github.com/gwaldron/spotify-tray/releases
DefaultDirName={localappdata}\Programs\SpotifyWidget
DefaultGroupName=Spotify Widget
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
AppMutex=Local\SpotifyWidget.Desktop
CloseApplications=no
RestartApplications=no
UninstallDisplayIcon={app}\SpotifyWidget.exe
OutputDir={#ArtifactDir}
OutputBaseFilename=SpotifyWidget-Setup-{#AppVersion}-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\SpotifyWidget.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Spotify Widget"; Filename: "{app}\SpotifyWidget.exe"
Name: "{autodesktop}\Spotify Widget"; Filename: "{app}\SpotifyWidget.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SpotifyWidget.exe"; Description: "Launch Spotify Widget"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StartupCommand: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    { Remove startup only when it points at this installation, not a portable copy. }
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run',
      'SpotifyWidget', StartupCommand) then
    begin
      if CompareText(StartupCommand, '"' + ExpandConstant('{app}\SpotifyWidget.exe') + '"') = 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'SpotifyWidget');
    end;
  end;
end;
