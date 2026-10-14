# Veralteter Skriptname; aus Sicherheitsgruenden sind Signatur und Freigabefingerabdruck jetzt Pflicht.
param(
    [Parameter(Mandatory=$true)][string]$SignedInstallerPath,
    [Parameter(Mandatory=$true)][string]$ExpectedSignerThumbprint
)
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'Prepare-SignedRelease.ps1') -SignedInstallerPath $SignedInstallerPath -ExpectedSignerThumbprint $ExpectedSignerThumbprint
