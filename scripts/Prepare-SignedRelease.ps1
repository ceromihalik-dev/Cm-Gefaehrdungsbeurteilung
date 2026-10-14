# Prueft bereits signierte Setups; dieses Skript signiert NICHT.
param(
    [Parameter(Mandatory=$true)][string]$SignedInstallerPath,
    [Parameter(Mandatory=$true)][string]$ExpectedSignerThumbprint,
    [string]$ProjectPath = ''
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($ExpectedSignerThumbprint -notmatch '^[a-fA-F0-9]{40}$') {
    throw 'Ungueltiger Zertifikatsfingerabdruck.'
}
if (-not $ProjectPath) {
    $ProjectPath = Join-Path $PSScriptRoot '..\src\CM.Gefaehrdungsbeurteilung\CM.Gefaehrdungsbeurteilung.csproj'
}
$file = (Resolve-Path -LiteralPath $SignedInstallerPath).Path
$projectXml = [xml](Get-Content -LiteralPath $ProjectPath -Raw)
$version = [string]$projectXml.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Projektversion muss im Format Major.Minor.Patch vorliegen.'
}
$expectedName = "CM-Gefaehrdungsbeurteilung-Setup-$version.exe"
if ([IO.Path]::GetFileName($file) -cne $expectedName) {
    throw "Unerwarteter Installername. Erwartet: $expectedName"
}
$sig = Get-AuthenticodeSignature -LiteralPath $file
if ($sig.Status -ne 'Valid' -or -not $sig.SignerCertificate -or
    $sig.SignerCertificate.Thumbprint -ne $ExpectedSignerThumbprint) {
    throw "Ungueltige Authenticode-Signatur oder falscher Herausgeber ($($sig.Status))."
}
$hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
$dest = Join-Path (Split-Path -Parent $file) 'SHA256SUMS.txt'
[IO.File]::WriteAllText($dest, "$hash *$([IO.Path]::GetFileName($file))$([char]10)", [Text.Encoding]::ASCII)
Write-Host "Signierter Installer geprueft. SHA256SUMS: $dest"
Write-Host "Installer und SHA256SUMS.txt gemeinsam im GitHub Release v$version veroeffentlichen."
