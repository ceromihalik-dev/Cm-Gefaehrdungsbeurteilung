# ACHTUNG: Fuehrt eine echte Installation mit anschliessender Deinstallation aus.
# Nur auf einem FRISCHEN TEST-Windows oder im CI-Runner ausfuehren!
param([switch]$AllowLocalInstall)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $env:GITHUB_ACTIONS -and -not $AllowLocalInstall) {
    throw 'Installationsprüfung nur im CI-Runner oder nach explizitem -AllowLocalInstall auf isoliertem Test-Windows erlaubt.'
}
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$setup = Join-Path $root 'release\CM-Gefaehrdungsbeurteilung-Setup-0.1.3.exe'
$appDir = Join-Path $env:LOCALAPPDATA 'Programs\CM Gefaehrdungsbeurteilung'
$dataDir = Join-Path $env:LOCALAPPDATA 'CM\Gefaehrdungsbeurteilung'
$exe = Join-Path $appDir 'CM.Gefaehrdungsbeurteilung.exe'
$uninstall = Join-Path $appDir 'unins000.exe'
if (-not (Test-Path $setup)) { throw "Installer fehlt: $setup" }
if (Test-Path $appDir) { throw 'Test abgebrochen: Installationsverzeichnis existiert bereits (kein Ueberschreiben von Benutzerdaten).' }
if (Test-Path $dataDir) { throw 'Test abgebrochen: Datenverzeichnis existiert bereits (kein Test mit Bestandsdaten).' }
New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
$sentinel = Join-Path $dataDir 'CM-SETUP-TEST-DATEN-BLEIBEN.txt'
Set-Content -Path $sentinel -Value 'Unveraendert' -Encoding UTF8
try {
    foreach ($attempt in 1..2) {
        $process = Start-Process -FilePath $setup -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-' -PassThru -Wait
        if ($process.ExitCode -ne 0) { throw "Installer fehlgeschlagen (Versuch $attempt): ExitCode $($process.ExitCode)" }
        if (-not (Test-Path $exe)) { throw "Anwendung fehlt nach Installationsversuch $attempt." }
        if ((Get-Item $exe).VersionInfo.FileVersion -notlike '0.1.3*') {
            throw 'Installierte Version stimmt nicht mit 0.1.3 ueberein.'
        }
        if ((Get-Content $sentinel -Raw).Trim() -ne 'Unveraendert') {
            throw "Benutzerdaten wurden bei Installationsversuch $attempt veraendert."
        }
        Write-Host "PASS: Installation / In-Place-Update #$attempt, Programmversion und Nutzdaten."
    }
    if (-not (Test-Path $uninstall)) { throw 'Deinstallationsprogramm fehlt.' }
    $process = Start-Process -FilePath $uninstall -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART' -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "Deinstallation fehlgeschlagen: $($process.ExitCode)" }
    if (-not (Test-Path $sentinel)) { throw 'Benutzerdaten wurden durch Deinstallation geloescht.' }
    Write-Host 'PASS: Deinstallation erhaelt getrennte Nutzdaten.'
}
finally {
    # Nur die vom Test selbst angelegte Sentineldatei entsorgen. Bei Fehlern verbleiben
    # Programmdaten zur manuellen Untersuchung; keine rekursiven Datenloeschungen!
    if (Test-Path $sentinel) { Remove-Item $sentinel -Force }
}
