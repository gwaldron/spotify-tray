param([switch]$FrameworkDependent, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $taskRoot 'dist\SpotifyWidget' }
$dotnetPath = Join-Path $taskRoot '.tools\dotnet\dotnet.exe'
if (-not (Test-Path $dotnetPath)) { $dotnetPath = (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
& $dotnetPath run --project (Join-Path $taskRoot 'tests\SpotifyWidget.Checks.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Checks failed' }
$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
& $dotnetPath publish (Join-Path $taskRoot 'SpotifyWidget\SpotifyWidget.csproj') -c Release -r win-x64 --self-contained $selfContained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Copy-Item -LiteralPath (Join-Path $taskRoot 'README.md') -Destination (Join-Path $OutputDirectory 'README.md') -Force
Write-Host "Ready: $OutputDirectory\SpotifyWidget.exe"
