# Windows-Testplan – 0.1.3 / Build 0001

**Status:** vor Ausführung, Testfälle noch nicht auf Windows durchgeführt.

## 1. Voraussetzungen
- Windows 10/11 x64 mit aktuellen Sicherheitsupdates
- .NET SDK 10 und Inno Setup 6/7
- Standardbenutzerkonto ohne Administratorrechte
- Testrechner ohne produktive Gefährdungsbeurteilungsdaten
- Zusätzlich separate VM / zweites Benutzerkonto für Migration 0.1.0/0.1.1 -> 0.1.2

## 2. Testprotokoll

| Nr. | Test | Erwartetes Ergebnis | Status |
|---|---|---|---|
| T01 | `python tests/Validate-Source.py` | Plausibilitätschecks PASS | OFFEN auf Windows |
| T02 | `./scripts/Build-Setup.ps1` | Fertiges Setup 0.1.2 erstellt | OFFEN |
| T03 | Setup als Standardbenutzer | Installation ohne Adminrechte möglich | OFFEN |
| T04 | Start per Startmenü/optional Desktop | Anwendung startet nur einmal | OFFEN |
| T05 | Neuer Vorgang, Stammdaten eingeben | Angaben können gespeichert werden | OFFEN |
| T06 | Objekt-Check Ja/Nein/Ungeklärt und Notiz | Antworten nach Neustart erhalten | OFFEN |
| T07 | Check ungeklärt, Prüfung anstoßen | Prüfung blockiert, Hinweis sichtbar | OFFEN |
| T08 | Kritischer Check = Ja, keine Fachklärung | Prüfung blockiert | OFFEN |
| T09 | Ja mit Nachweis, aber ohne verknüpfte Gefährdung | Prüfung blockiert | OFFEN |
| T10 | Vollständige Felder und Gefährdungsmaßnahmen | Fachprüfung kann dokumentiert werden (qualifizierter Tester) | OFFEN |
| T11 | Änderung nach fachlicher Prüfung | Status wechselt zurück in Entwurf | OFFEN |
| T12 | Druckdialog / Microsoft Print to PDF | Objekt-Check und Gefährdungen im Dokument | OFFEN |
| T13 | Manuelle Datensicherung erstellen | Datei vorhanden, SQLite integrity_check = ok | OFFEN |
| T14 | 0.1.0-/0.1.1-Datenbank unter 0.1.2 öffnen | Schema 3, alter Datensatz bleibt erhalten, geprüft wird „Erneute Prüfung erforderlich“ | OFFEN |
| T15 | Migration mit gesperrter Backupablage | Abbruch ohne Schemaänderung | OFFEN |
| T16 | Deinstallieren/Neuinstallieren | Vorhandene Daten bleiben erhalten | OFFEN |
| T17 | Öffnen mit DB-Version > 3 | Datenbank wird nicht überschrieben | OFFEN |
| T18 | Updates ohne Code-Signing/Thumbprint | Keine automatische Setup-Ausführung | OFFEN |
| T19 | Fehlerhaftes Setup / falsche Prüfsumme | Update wird verworfen | OFFEN |
| T20 | Gültig signiertes Update mit Testzertifikat | Nur erwarteter Herausgeber wird akzeptiert | OFFEN |

## 3. Rückwärtskompatibilität
- Immer mit einer **Kopie** echter historischer Version-0.1.0-Daten testen; niemals Produktivdaten als Ersttest verwenden.
- `PRAGMA user_version` vor und nach Migration vergleichen.
- Prüfen, ob ursprüngliche Beurteilungen, Gefährdungen, Firmeneinstellungen und Prüfzeitpunkte erhalten bleiben.
- Nach Rücksetzung auf 0.1.0 eine Datenbank aus 0.1.2 **nicht** öffnen (Rückwärtskompatibilität ist nicht vorgesehen); statt dessen Backup vor Schema 3 verwenden.

## 4. Freigabekriterien
Ein reales Release darf erst nach grünem Windows-Build, Installationstest, Datenmigrationstest, dokumentierter funktionaler Abnahme und erfolgreicher Signaturprüfung als benutzbare Installation veröffentlicht werden. Der Fachkatalog muss für die produktive Anwendung durch eine qualifizierte Person geprüft sein.


## 5. Zusätzliche Tests ab 0.1.2 (alle noch offen auf Windows)

| Nr. | Test | Erwartetes Ergebnis | Status |
|---|---|---|---|
| T21 | Wasser- und Brandtätigkeit gleichzeitig wählen | Beide Auswahlen werden gespeichert und nach Neustart wieder angezeigt | OFFEN |
| T22 | „Passende Gefährdungen übernehmen“ zweimal | Keine doppelte Katalogposition | OFFEN |
| T23 | Tätigkeitsauswahl nach fachlicher Prüfung ändern | Prüfung wird widerrufen; Entwurf und erneute Fachprüfung erforderlich | OFFEN |
| T24 | Änderung der Tätigkeiten mit zusätzlichem Bereich | Zugehörige Objektfragen erscheinen; neue Fragen sind „Ungeklärt“ | OFFEN |
| T25 | Migration Schema 2 → 3 | Backups geprüft, Auswahlfeld vorhanden, alter Inhalt erhalten | OFFEN |
| T26 | Firmenlogo PNG auswählen, Einstellungen speichern | Logo in Firmendaten kopiert, nach Programmneustart verfügbar | OFFEN |
| T27 | Druck/PDF mit Firmenlogo und Maßnahmenübersicht | Format und Seitenumbrüche geprüft, kein Text abgeschnitten | OFFEN |
| T28 | Update 0.1.1 → 0.1.2 mit unveränderten Daten | Installation ersetzt Programm, SQLite und alle Projekte bleiben vorhanden | OFFEN |
| T29 | Alte SQLite-DB 0.1.2 mit Programm 0.1.1 öffnen | Rückwärtsinkompatibilität erkannt, klare Fehlermeldung | OFFEN |


## 6. Zusätzliche Tests für 0.1.3

| Nr. | Test | Erwartung | Stand |
|---|---|---|---|
| T30 | Windows 10/11: `scripts/Build-Setup.ps1` | Echter WPF-Compile + Installer 0.1.3 | OFFEN |
| T31 | CI: `scripts/Test-Installer.ps1` | Stille Installation als Standardbenutzer | OFFEN |
| T32 | CI: Setup 0.1.3 ein zweites Mal starten | Aktualisierung ohne Verlust der Sentinel-Nutzdaten | OFFEN |
| T33 | CI: Deinstallation | Getrennte Nutzdaten bleiben vorhanden | OFFEN |
| T34 | Unsigned Entwicklungsbuild | Updateanzeige funktioniert, Installieren bleibt deaktiviert | OFFEN |
| T35 | Signierter Build mit erlaubter Zertifikatkennung | SHA-256 und Signatur PASS, danach Start der Installation | OFFEN |
| T36 | Gleiche SHA-256, fremdes Signaturzertifikat | Setup wird nicht gestartet | OFFEN |
| T37 | Download >500 MB | Abbruch ohne Start der Installation | OFFEN |
| T38 | Update-Datenbanksicherung | Backup mit `integrity_check=ok`, Fachfälle unverändert | OFFEN |
| T39 | Neues Programm mit Schema-3-Datenbank | Alle Projekte, Tätigkeiten, Fragen, Gefährdungen und Einstellungen vorhanden | OFFEN |
| T40 | Installation realer Altversion 0.1.2 → 0.1.3 | Vollständiger echter Upgrade-Test mit Bestandsdatensatz | OFFEN |

Die neuen Quellcode-Tests und die SQLite-Simulation wurden im Entwicklungscontainer ausgeführt. Die Tests T30–T40 benötigen Windows und wurden noch **nicht** als bestanden markiert. Der Installer-Test ist ausdrücklich nur auf **isoliertem** Windows oder in einem Wegwerf-CI-Runner auszuführen.
