# Publish a previously signed and verified installer to GitHub Releases.
# Requires gh CLI authenticated with release-write permission. Never stores keys.
param(
    [Parameter(Mandatory=$true)][string]$SignedInstallerPath,
    [Parameter(Mandatory=$true)][string]$ExpectedSignerThumbprint,
    [switch]$Publish
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $root 'src\CM.Gefaehrdungsbeurteilung\CM.Gefaehrdungsbeurteilung.csproj'
$project = [xml](Get-Content -LiteralPath $projectPath -Raw)
$version = [string]$project.Project.PropertyGroup.Version
$tag = "v$version"
$file = (Resolve-Path -LiteralPath $SignedInstallerPath).Path
$repo = 'ceromihalik-dev/Cm-Gefaehrdungsbeurteilung'
& (Join-Path $PSScriptRoot 'Test-VersionConsistency.ps1')
if (-not $?) { throw 'Version validation failed.' }
& (Join-Path $PSScriptRoot 'Prepare-SignedRelease.ps1') -SignedInstallerPath $file -ExpectedSignerThumbprint $ExpectedSignerThumbprint
if (-not $?) { throw 'Signed installer verification failed.' }
$checksums = Join-Path (Split-Path -Parent $file) 'SHA256SUMS.txt'
if (-not (Test-Path -LiteralPath $checksums)) { throw 'SHA256SUMS.txt missing.' }
Write-Host "Signed release validated: $tag"
if (-not $Publish) {
    Write-Host 'DRY RUN: no GitHub release published. Add -Publish after Windows upgrade testing.'
    return
}
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw 'GitHub CLI (gh) missing.' }
& gh auth status
if ($LASTEXITCODE -ne 0) { throw 'GitHub CLI is not authenticated.' }
& gh release view $tag --repo $repo *> $null
if ($LASTEXITCODE -eq 0) { throw "Release $tag already exists. Refusing to replace its assets." }
& gh release create $tag $file $checksums --repo $repo --title "CM Gefaehrdungsbeurteilung $version" --notes-file (Join-Path $root 'CHANGELOG.md') --latest
if ($LASTEXITCODE -ne 0) { throw 'GitHub release publication failed.' }
Write-Host "Release $tag published: https://github.com/$repo/releases/tag/$tag"
