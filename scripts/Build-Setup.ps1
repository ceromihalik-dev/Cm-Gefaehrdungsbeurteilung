# Builds an unsigned Windows setup. Signing and release approval are separate steps.
param(
    [string]$ISCC = '',
    [string]$TrustedSignerThumbprint = ''
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $root 'src\CM.Gefaehrdungsbeurteilung\CM.Gefaehrdungsbeurteilung.csproj'
$publish = Join-Path $root 'publish'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET 10 SDK fehlt.' }
$sdks = (& dotnet --list-sdks) -join "`n"
if ($LASTEXITCODE -ne 0 -or $sdks -notmatch '(?m)^10\.') { throw '.NET 10 SDK fehlt.' }
if ($TrustedSignerThumbprint -and $TrustedSignerThumbprint -notmatch '^[a-fA-F0-9]{40}$') { throw 'Ungueltiger Herausgeber-Fingerabdruck.' }
if (-not $ISCC) {
    foreach ($base in @($env:ProgramFiles, ${env:ProgramFiles(x86)}) | Where-Object { $_ }) {
        foreach ($major in @('7', '6')) {
            $candidate = Join-Path $base "Inno Setup $major\ISCC.exe"
            if (Test-Path -LiteralPath $candidate) { $ISCC = $candidate; break }
        }
        if ($ISCC) { break }
    }
}
if (-not $ISCC -or -not (Test-Path -LiteralPath $ISCC)) { throw 'Inno Setup (ISCC.exe) fehlt.' }
& (Join-Path $PSScriptRoot 'Test-VersionConsistency.ps1')
if (-not $?) { throw 'Versionsabgleich fehlgeschlagen.' }
$xml = [xml](Get-Content -LiteralPath $project -Raw)
$version = [string]$xml.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Ungueltige Projektversion: $version" }
$exeName = "CM-Gefaehrdungsbeurteilung-Setup-$version.exe"
$releaseDir = Join-Path $root 'release'
New-Item -Path $releaseDir -ItemType Directory -Force | Out-Null
$expectedInstaller = Join-Path $releaseDir $exeName
if (Test-Path -LiteralPath $expectedInstaller) { Remove-Item -LiteralPath $expectedInstaller -Force }
$argsList = @('publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=false', '-o', $publish)
if ($TrustedSignerThumbprint) { $argsList += "-p:TrustedUpdateSignerThumbprint=$($TrustedSignerThumbprint.ToUpperInvariant())" }
Write-Host 'Baue Windows-Anwendung (.NET 10, win-x64) ...'
& dotnet @argsList
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish fehlgeschlagen.' }
if (-not (Test-Path (Join-Path $publish 'CM.Gefaehrdungsbeurteilung.exe'))) { throw 'Anwendungs-EXE fehlt.' }
Write-Host 'Erstelle Inno Setup ...'
& $ISCC (Join-Path $root 'installer\Setup.iss')
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $expectedInstaller)) { throw 'Inno Setup hat kein erwartetes Setup erzeugt.' }
Write-Host "ERFOLG: $expectedInstaller"
Write-Warning 'TEST-SETUP OHNE SIGNATUR. Keine automatische Updatefreigabe!'
