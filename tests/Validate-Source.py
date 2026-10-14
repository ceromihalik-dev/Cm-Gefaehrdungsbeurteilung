"""Quellcode-Pruefungen fuer 0.1.3; kein Ersatz fuer einen Windows-WPF-Build."""
from pathlib import Path
from tempfile import TemporaryDirectory
import re
import sqlite3
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent.parent
src = root / 'src' / 'CM.Gefaehrdungsbeurteilung'
for path in (*root.rglob('*.xaml'), *root.rglob('*.csproj')):
    ET.parse(path)
print('PASS: XAML und Projekt-XML syntaktisch gueltig')

view = (src / 'MainWindow.xaml').read_text(encoding='utf-8')
code = (src / 'MainWindow.xaml.cs').read_text(encoding='utf-8')
handlers = set(re.findall(r'\b(?:Click|SelectionChanged|Checked|Unchecked|TextChanged|MouseDoubleClick)="([A-Za-z_][\w]*)"', view))
for handler in sorted(handlers):
    assert re.search(r'\b' + re.escape(handler) + r'\s*\(', code), f'WPF-Handler nicht gefunden: {handler}'
print(f'PASS: {len(handlers)} WPF-Ereignishandler im Code referenziert')

storage = (src / 'Services' / 'StorageService.cs').read_text(encoding='utf-8')
create = re.search(r'create.CommandText = """(.*?)""";', storage, re.DOTALL)
migration_2 = re.search(r'migrate.CommandText = """(.*?)""";', storage, re.DOTALL)
migration_3 = re.search(r'migrate3.CommandText = """(.*?)""";', storage, re.DOTALL)
assert create and migration_2 and migration_3, 'Schemaerstellung oder Migration fehlt'

v1 = '''
 CREATE TABLE assessments (id TEXT PRIMARY KEY, project_number TEXT NOT NULL,
   site_address TEXT NOT NULL, area TEXT NOT NULL, work_description TEXT NOT NULL,
   responsible TEXT NOT NULL, status TEXT NOT NULL, updated_utc TEXT NOT NULL, reviewed_utc TEXT NULL);
 CREATE TABLE assessment_hazards (id TEXT PRIMARY KEY, assessment_id TEXT NOT NULL REFERENCES assessments(id) ON DELETE CASCADE,
   source_id TEXT NOT NULL, title TEXT NOT NULL, description TEXT NOT NULL,
   measures TEXT NOT NULL, before_probability INTEGER NOT NULL, before_severity INTEGER NOT NULL,
   after_probability INTEGER NOT NULL, after_severity INTEGER NOT NULL,
   owner TEXT NOT NULL, due_date TEXT NULL, effectiveness_checked INTEGER NOT NULL);
 CREATE TABLE settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
 INSERT INTO assessments VALUES ('a1','P-001','Musterstrasse 1','Wasserschadensanierung',
   'Trocknung','Muster','Fachlich geprüft','2026-10-13T10:00:00Z','2026-10-13T11:00:00Z');
 PRAGMA user_version=1;
'''
with TemporaryDirectory() as tmp:
    tmp = Path(tmp)
    for initial_version in [0, 1, 2]:
        db = sqlite3.connect(tmp / f'database{initial_version}.sqlite')
        db.execute('PRAGMA foreign_keys=ON')
        if initial_version == 0:
            db.executescript(create.group(1))
        else:
            db.executescript(v1)
            if initial_version == 2:
                db.executescript(migration_2.group(1))
        if initial_version >= 1:
            backup = sqlite3.connect(tmp / f'backup{initial_version}.sqlite')
            db.backup(backup)
            assert backup.execute('PRAGMA integrity_check').fetchone()[0] == 'ok'
            if initial_version == 1:
                db.executescript(migration_2.group(1))
            db.executescript(migration_3.group(1))
            assert db.execute("SELECT status FROM assessments WHERE id='a1'").fetchone()[0] == 'Erneute Prüfung erforderlich'
            assert db.execute("SELECT reviewed_utc FROM assessments WHERE id='a1'").fetchone()[0] == '2026-10-13T11:00:00Z'
            assert db.execute("SELECT selected_activities FROM assessments WHERE id='a1'").fetchone()[0] == ''
            assert backup.execute('PRAGMA user_version').fetchone()[0] == initial_version
            backup.close()
        assert db.execute('PRAGMA user_version').fetchone()[0] == 3
        assert db.execute('PRAGMA integrity_check').fetchone()[0] == 'ok'
        assert 'selected_activities' in [c[1] for c in db.execute('PRAGMA table_info(assessments)')]
        db.execute("INSERT INTO assessments (id,project_number,site_address,area,work_description,selected_activities,responsible,status,updated_utc) VALUES ('n','N','Obj','Allgemein','','W-TR,B-BS','R','Entwurf','2026-10-13T12:00:00Z')")
        assert db.execute("SELECT selected_activities FROM assessments WHERE id='n'").fetchone()[0] == 'W-TR,B-BS'
        db.close()
print('PASS: Schema 0/1/2 -> 3, Altpruefung, IDs und SQLite-Integritaet')

activities = (src / 'Services' / 'ActivityService.cs').read_text(encoding='utf-8')
catalog = (src / 'Services' / 'CatalogService.cs').read_text(encoding='utf-8')
a_ids = re.findall(r'new\("([A-Z]-[A-Z]{2})",', activities)
h_ids = set(re.findall(r'new\("((?:ALL|WAS|BRA)-\d{3})",', catalog))
assert len(a_ids) >= 12 and len(a_ids) == len(set(a_ids))
for line in activities.splitlines():
    if not line.strip().startswith('new('):
        continue
    proposed = re.findall(r'"((?:ALL|WAS|BRA)-\d{3})"', line)
    assert all(h in h_ids for h in proposed), f'Ungueltige Gefaehrdungsreferenz: {line.strip()}'
print(f'PASS: {len(a_ids)} eindeutige Taetigkeiten mit existierenden Gefaehrdungsreferenzen')
# Sicherheits- und Paketvertrag fuer Windows-Update-Downloads.
project_xml = (src / 'CM.Gefaehrdungsbeurteilung.csproj').read_text(encoding='utf-8')
installer = (root / 'installer' / 'Setup.iss').read_text(encoding='utf-8')
update = (src / 'Services' / 'UpdateService.cs').read_text(encoding='utf-8')
window = (src / 'MainWindow.xaml.cs').read_text(encoding='utf-8')
setup_script = (root / 'scripts' / 'Build-Setup.ps1').read_text(encoding='utf-8')
for source in (project_xml, installer, setup_script):
    assert '0.1.3' in source, 'Inkonsequente Versionsangabe'
assert 'VersionInfoVersion=0.1.3.1' in installer
assert 'AppId={{2836B6AC-8645-4A47-B1EE-C5840ECF1E07}' in installer
assert 'TrustedUpdateSignerThumbprint' in project_xml
assert 'GetCustomAttributes<AssemblyMetadataAttribute>' in update
assert 'CanAutoInstall' in update and 'Get-AuthenticodeSignature' in update
assert 'repo.signerThumbprint' not in window and 'CertThumbprintBox' not in window
assert 'UpdateService.TrustedSignerThumbprint' in window
assert '500L * 1024 * 1024' in update
assert (root / 'scripts' / 'Test-Installer.ps1').exists()
assert (root / 'scripts' / 'Prepare-SignedRelease.ps1').exists()
print('PASS: Versionsvertrag, Installer-AppId, Sicherung der Update-Vertrauenskette und CI-Testskripte')
print('OFFEN: Echter Windows-WPF-Build, Installationsablauf und signiertes Online-Update.')
