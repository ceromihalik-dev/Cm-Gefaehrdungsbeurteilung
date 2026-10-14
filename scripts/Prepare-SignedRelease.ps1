# Prueft eine bereits signierte Setup.exe und erzeugt SHA256SUMS.txt.
# Erst NACH dem Authenticode-Signieren ausfuehren.
param(
    [Parameter(Mandatory=$true)][string]$SignedInstallerPath,
    [Parameter(Mandatory=$true)][string]$ExpectedSignerThumbprint
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($ExpectedSignerThumbprint -notmatch '^[a-fA-F0-9]{40}$') {
    throw 'Ein Herausgeber-Fingerabdruck mit 40 Hex-Zeichen ist erforderlich.'
}
$file = (Resolve-Path -LiteralPath $SignedInstallerPath).Path
if ([IO.Path]::GetFileName($file) -ne 'CM-Gefaehrdungsbeurteilung-Setup-0.1.3.exe') {
    throw 'Unerwarteter Installername. Das GitHub-Release muss genau zur Programmversion passen.'
}
$sig = Get-AuthenticodeSignature -LiteralPath $file
if ($sig.Status -ne 'Valid' -or -not $sig.SignerCertificate -or
    $sig.SignerCertificate.Thumbprint -ne $ExpectedSignerThumbprint) {
    throw "Unguelige Authenticode-Signatur oder falscher Herausgeber ($($sig.Status)). Kein Release erstellt."
}
$hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
$dest = Join-Path (Split-Path -Parent $file) 'SHA256SUMS.txt'
[IO.File]::WriteAllText($dest, "$hash *$([IO.Path]::GetFileName($file))`n", [Text.Encoding]::ASCII)
Write-Host "Signierter Installer geprueft. SHA256SUMS: $dest"
Write-Host 'HINWEIS: Installer und SHA256SUMS.txt gemeinsam in einem GitHub Release veroeffentlichen.'
