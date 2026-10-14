# Verhindert auseinanderlaufende Versionsnummern vor dem Windows-Build.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $root 'src\CM.Gefaehrdungsbeurteilung\CM.Gefaehrdungsbeurteilung.csproj'
$setupPath = Join-Path $root 'installer\Setup.iss'
$project = [xml](Get-Content -LiteralPath $projectPath -Raw)
$version = [string]$project.Project.PropertyGroup.Version
$assembly = [string]$project.Project.PropertyGroup.AssemblyVersion
$file = [string]$project.Project.PropertyGroup.FileVersion
$setup = Get-Content -LiteralPath $setupPath -Raw
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Ungueltige Projektversion: $version" }
$expectedAssembly = "$version.0"
if ($assembly -ne $expectedAssembly) { throw "AssemblyVersion $assembly ungleich $expectedAssembly" }
if ($file -notmatch ('^' + [regex]::Escape($version) + '\.\d+$')) {
    throw "FileVersion $file passt nicht zur Programmversion $version"
}
$match = [regex]::Match($setup, '(?m)^#define MyAppVersion "([^"]+)"')
if (-not $match.Success -or $match.Groups[1].Value -ne $version) {
    throw "Inno-Setup-Version stimmt nicht mit $version ueberein."
}
$match = [regex]::Match($setup, '(?m)^VersionInfoVersion=(\d+\.\d+\.\d+\.\d+)\s*$')
if (-not $match.Success -or $match.Groups[1].Value -ne $file) {
    throw "Inno-Setup-VersionInfoVersion stimmt nicht mit $file ueberein."
}
Write-Host "PASS: Versionsnummern konsistent: $version, Assembly $assembly, File $file"
