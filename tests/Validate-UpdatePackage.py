"""Prueft das CI-Updatepaket, ohne seine Herausgeberidentitaet zu behaupten."""
import hashlib
import json
import pathlib
import re
import xml.etree.ElementTree as ET
import zipfile

root = pathlib.Path(__file__).resolve().parent.parent
version = ET.parse(root / "src/CM.Gefaehrdungsbeurteilung/CM.Gefaehrdungsbeurteilung.csproj").getroot().findtext("./PropertyGroup/Version")
assert version and re.fullmatch(r"\d+\.\d+\.\d+", version)
filename = f"CM-Gefaehrdungsbeurteilung-Setup-{version}.exe"
setup = root / "release" / filename
package = root / "release" / f"CM-Gefaehrdungsbeurteilung-Update-{version}.cmupdate"
assert setup.is_file() and package.is_file(), "Setup oder .cmupdate fehlt"
with zipfile.ZipFile(package) as z:
    assert sorted(z.namelist()) == sorted(["manifest.json", filename]), "Unerwartete Paketstruktur"
    manifest = json.loads(z.read("manifest.json"))
    assert manifest["SchemaVersion"] == 1
    assert manifest["Version"] == version
    assert manifest["InstallerFile"] == filename
    with setup.open("rb") as fp:
        digest = hashlib.file_digest(fp, "sha256").hexdigest()
    assert manifest["Sha256"].lower() == digest
    with z.open(filename) as payload:
        assert hashlib.file_digest(payload, "sha256").hexdigest() == digest
    assert z.getinfo(filename).file_size == setup.stat().st_size
with package.open("rb") as fp:
    package_hash = hashlib.file_digest(fp, "sha256").hexdigest()
checksum_path = package.with_name(package.name + ".sha256")
assert checksum_path.read_text(encoding="ascii").strip() == package_hash + "  " + package.name
print(f"PASS: Offline-Updatepaket {version}, 2 Dateieintraege, SHA-256 verifiziert.")
