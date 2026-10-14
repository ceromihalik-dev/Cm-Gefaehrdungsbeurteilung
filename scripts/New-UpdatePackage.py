"""Erstellt ein manuell einlesbares .cmupdate-Paket aus einer Windows-Setup.exe.

Keine kryptografische Herausgeberauthentifizierung! SHA-256 dient der Integritaet.
"""
import hashlib
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET
import zipfile

root = pathlib.Path(__file__).resolve().parent.parent
project = root / "src" / "CM.Gefaehrdungsbeurteilung" / "CM.Gefaehrdungsbeurteilung.csproj"
version = ET.parse(project).getroot().findtext("./PropertyGroup/Version")
if not version or not re.fullmatch(r"\d+\.\d+\.\d+", version):
    raise SystemExit("Ungueltige Projektversion.")
name = f"CM-Gefaehrdungsbeurteilung-Setup-{version}.exe"
installer = root / "release" / name
if not installer.is_file():
    raise SystemExit(f"Setup fehlt: {installer}")
if not 0 < installer.stat().st_size <= 500 * 1024 * 1024:
    raise SystemExit("Installergroesse unzulaessig.")
with installer.open("rb") as fp:
    sha = hashlib.file_digest(fp, "sha256").hexdigest()
manifest = {
    "SchemaVersion": 1,
    "Version": version,
    "InstallerFile": name,
    "Sha256": sha,
    "Notes": "CM Gefaehrdungsbeurteilung " + version +
             ". Manuelles Test-Update. Quelle und Integritaet vor Nutzung pruefen."
}
destination = root / "release" / f"CM-Gefaehrdungsbeurteilung-Update-{version}.cmupdate"
with zipfile.ZipFile(destination, "w", compression=zipfile.ZIP_DEFLATED,
                     compresslevel=6) as archive:
    archive.writestr("manifest.json", json.dumps(manifest, ensure_ascii=False, indent=2))
    archive.write(installer, arcname=name, compress_type=zipfile.ZIP_STORED)
with destination.open("rb") as fp:
    package_hash = hashlib.file_digest(fp, "sha256").hexdigest()
(destination.parent / (destination.name + ".sha256")).write_text(
    package_hash + "  " + destination.name + "\n", encoding="ascii")
print(f"CM Update-Datei: {destination}")
print(f"SHA-256 Paket: {package_hash}")
