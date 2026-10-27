param([string]$ISCCPath)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$project = [xml](Get-Content -LiteralPath (Join-Path $taskRoot 'SpotifyWidget\SpotifyWidget.csproj') -Raw)
$version = [string]$project.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Project Version must have three numeric components.' }

if (-not $ISCCPath) {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
        (Join-Path $taskRoot '.tools\Inno Setup\ISCC.exe')
    )
    $ISCCPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $ISCCPath) { $ISCCPath = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source }
}
if (-not $ISCCPath -or -not (Test-Path -LiteralPath $ISCCPath)) {
    throw 'Install Inno Setup 6.3 or newer, or pass -ISCCPath. GitHub Windows runners include it.'
}

$publishDir = Join-Path $taskRoot 'artifacts\publish'
$artifactDir = Join-Path $taskRoot 'artifacts\release'
& (Join-Path $taskRoot 'build.ps1') -OutputDirectory $publishDir
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
& $ISCCPath "/DAppVersion=$version" "/DPublishDir=$publishDir" "/DArtifactDir=$artifactDir" (Join-Path $taskRoot 'installer\SpotifyWidget.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }

$installer = Join-Path $artifactDir "SpotifyWidget-Setup-$version-x64.exe"
$portable = Join-Path $artifactDir "SpotifyWidget-Portable-$version-x64.zip"
Compress-Archive -LiteralPath (Join-Path $publishDir 'SpotifyWidget.exe'), (Join-Path $publishDir 'README.md') -DestinationPath $portable -Force
$checksums = foreach ($file in @($installer, $portable)) {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant(), [IO.Path]::GetFileName($file)
}
# LF endings keep GNU sha256sum on the release runner from treating CR as part of a filename.
[IO.File]::WriteAllText((Join-Path $artifactDir 'SHA256SUMS.txt'), ($checksums -join "`n") + "`n", [Text.Encoding]::ASCII)
Write-Host "Release files: $artifactDir"
