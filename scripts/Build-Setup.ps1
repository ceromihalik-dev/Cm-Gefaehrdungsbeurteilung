# Erstellt ein noch NICHT signiertes Setup. Ausfuehrung unter Windows 10/11 x64.
param(
    [string]$ISCC = '',
    [string]$TrustedSignerThumbprint = ''
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $root 'src\CM.Gefaehrdungsbeurteilung\CM.Gefaehrdungsbeurteilung.csproj'
$publish = Join-Path $root 'publish'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 10 SDK fehlt. https://dotnet.microsoft.com/download/dotnet/10.0'
}
$sdks = (& dotnet --list-sdks) -join "`n"
if ($LASTEXITCODE -ne 0 -or $sdks -notmatch '(?m)^10\.') {
    throw '.NET 10 SDK fehlt. Bitte .NET 10 SDK fuer Windows installieren.'
}
if ($TrustedSignerThumbprint -and $TrustedSignerThumbprint -notmatch '^[a-fA-F0-9]{40}$') {
    throw 'TrustedSignerThumbprint muss genau 40 Hex-Zeichen enthalten (SHA-1-Zertifikat-Fingerabdruck).'
}

if (-not $ISCC) {
    $programFiles = @($env:ProgramFiles, ${env:ProgramFiles(x86)}) | Where-Object { $_ }
    foreach ($base in $programFiles) {
        foreach ($major in @('6', '7')) {
            $candidate = Join-Path $base "Inno Setup $major\ISCC.exe"
            if (Test-Path $candidate) { $ISCC = $candidate; break }
        }
        if ($ISCC) { break }
    }
}
if (-not $ISCC -or -not (Test-Path $ISCC)) {
    throw 'Inno Setup (ISCC.exe) fehlt. Inno Setup 6/7 installieren oder -ISCC angeben.'
}

$xml = [xml](Get-Content -LiteralPath $project -Raw)
$version = $xml.Project.PropertyGroup.Version
if ($version -ne '0.1.3') { throw "Projektversion unerwartet: $version" }
$exeName = "CM-Gefaehrdungsbeurteilung-Setup-$version.exe"
$expectedInstaller = Join-Path $root "release\$exeName"
if (Test-Path $expectedInstaller) { Remove-Item $expectedInstaller -Force }

$publishArgs = @('publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
  '-p:PublishSingleFile=false', '-o', $publish)
if ($TrustedSignerThumbprint) {
    # Identitaet wird in Assembly-Metadaten kompiliert, niemals aus lokalen Programmeinstellungen gelesen.
    $publishArgs += "-p:TrustedUpdateSignerThumbprint=$($TrustedSignerThumbprint.ToUpperInvariant())"
}
Write-Host 'Baue Windows-Anwendung (.NET 10 / self-contained) ...'
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish fehlgeschlagen.' }
if (-not (Test-Path (Join-Path $publish 'CM.Gefaehrdungsbeurteilung.exe'))) {
    throw 'Nach dem Publish fehlt die Windows-Programmdatei.'
}
Write-Host 'Baue Setup ueber Inno Setup ...'
& $ISCC (Join-Path $root 'installer\Setup.iss')
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $expectedInstaller)) {
    throw 'Inno Setup hat kein erwartetes Setup erzeugt.'
}
Write-Host "ERFOLG: $expectedInstaller"
Write-Warning 'Dies ist ein NICHT SIGNIERTES TEST-SETUP. Keine automatische Updatefreigabe!'
Write-Host 'Naechster Schritt: tests/Test-Installer.ps1 auf isoliertem Windows-Testsystem und Signaturprozess in docs/RELEASE_PROZESS.md.'
