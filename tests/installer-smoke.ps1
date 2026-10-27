# This test installs/uninstalls the real app and touches the current user's Run key.
# It is deliberately restricted to a disposable GitHub-hosted Windows runner.
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'Run installer smoke tests only on a disposable GitHub-hosted runner.'
}
$taskRoot = Split-Path $PSScriptRoot -Parent
$installers = @(Get-ChildItem (Join-Path $taskRoot 'artifacts\release\SpotifyWidget-Setup-*-x64.exe'))
if ($installers.Count -ne 1) { throw 'Expected exactly one installer.' }
$installDir = Join-Path $env:RUNNER_TEMP 'SpotifyWidget Install Test'
$logDir = Join-Path $taskRoot 'artifacts\smoke-logs'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$exe = Join-Path $installDir 'SpotifyWidget.exe'
$uninstaller = Join-Path $installDir 'unins000.exe'
$shortcut = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Spotify Widget.lnk'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{6A49B803-21CD-4F4F-B95C-B2BF215976ED}_is1'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

function Get-StartupCommand {
    # A missing value is the expected result after uninstall, not a provider error.
    [Microsoft.Win32.Registry]::GetValue('HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run', 'SpotifyWidget', $null)
}

function Invoke-Checked([string]$File, [string[]]$Arguments) {
    $child = Start-Process -FilePath $File -ArgumentList $Arguments -PassThru -WindowStyle Hidden
    if (-not $child.WaitForExit(60000)) { $child.Kill(); throw "Timed out: $File" }
    if ($child.ExitCode -ne 0) { throw "$File exited with $($child.ExitCode)" }
}
function Install-Widget([string]$LogName) {
    Invoke-Checked $installers[0].FullName @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-', "/DIR=`"$installDir`"", "/LOG=`"$logDir\$LogName`"")
    foreach ($path in @($exe, $uninstaller, $shortcut, $uninstallKey)) {
        if (-not (Test-Path -LiteralPath $path)) { throw "Installation missing: $path" }
    }
    if ((Get-FileHash -LiteralPath $exe).Hash -ne (Get-FileHash -LiteralPath (Join-Path $taskRoot 'artifacts\publish\SpotifyWidget.exe')).Hash) {
        throw 'Installed executable differs from the published binary.'
    }
}
function Uninstall-Widget([string]$LogName) {
    Invoke-Checked $uninstaller @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=`"$logDir\$LogName`"")
    if ((Test-Path -LiteralPath $exe) -or (Test-Path -LiteralPath $shortcut) -or (Test-Path -LiteralPath $uninstallKey)) {
        throw 'Uninstall left the executable, shortcut, or registration behind.'
    }
}

Install-Widget 'install.log'
if (Get-StartupCommand) { throw 'Installation unexpectedly enabled startup.' }
# A real launch exercises extraction of the bundled runtime and loading WPF/WinRT.
# A hosted runner may have no interactive media service; diagnostics still must be valid JSON.
Invoke-Checked $exe @('--diagnose')
$diagnostics = Join-Path $env:LOCALAPPDATA 'SpotifyWidget\diagnostics.json'
Get-Content -LiteralPath $diagnostics -Raw | ConvertFrom-Json | Out-Null

# Reinstall/upgrade in place, retaining user settings.
$settingsFile = Join-Path $env:LOCALAPPDATA 'SpotifyWidget\settings.json'
'{"Placement":"Taskbar","Left":null,"Top":null}' | Set-Content -LiteralPath $settingsFile
$settingsHash = (Get-FileHash -LiteralPath $settingsFile).Hash
Install-Widget 'upgrade.log'
if ((Get-FileHash -LiteralPath $settingsFile).Hash -ne $settingsHash) { throw 'Upgrade changed user settings.' }

New-Item -Path $runKey -Force | Out-Null
New-ItemProperty -LiteralPath $runKey -Name SpotifyWidget -Value "`"$exe`"" -PropertyType String -Force | Out-Null
Uninstall-Widget 'uninstall.log'
if (Get-StartupCommand) { throw 'Uninstall left startup pointing to a removed executable.' }
if ((Get-FileHash -LiteralPath $settingsFile).Hash -ne $settingsHash) { throw 'Uninstall changed user settings.' }

# A portable installation's startup preference must not be removed.
Install-Widget 'reinstall.log'
$portableStartup = '"C:\Portable\SpotifyWidget.exe"'
New-ItemProperty -LiteralPath $runKey -Name SpotifyWidget -Value $portableStartup -PropertyType String -Force | Out-Null
Uninstall-Widget 'portable-preservation.log'
if ((Get-StartupCommand) -ne $portableStartup) { throw 'Uninstall removed another copy''s startup setting.' }
Remove-ItemProperty -LiteralPath $runKey -Name SpotifyWidget
Write-Host 'Installer checks passed: install, bundled runtime, upgrade, shortcut, uninstall, settings, startup cleanup, portable preservation.'
