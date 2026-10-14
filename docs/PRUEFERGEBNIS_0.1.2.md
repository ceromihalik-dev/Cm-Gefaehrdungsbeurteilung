# Prüfprotokoll – CM Gefährdungsbeurteilung 0.1.2 (13.10.2026)

## Erfolgreich ausgeführte automatisierte Prüfungen in der Entwicklungsumgebung

- PASS: Alle `.xaml`- und `.csproj`-Dateien als XML syntaktisch geprüft.
- PASS: 30 aus XAML referenzierte Ereignishandler im Code vorhanden.
- PASS: Datenbank-Neuanlage Schema 3 mit SQLite-Testdaten.
- PASS: Migration von Schema 1 → 2 → 3 und Schema 2 → 3, einschließlich SQLite-Backup-Konsistenz, Daten- und Prüfzeitpunkterhalt, Prüfstatus sowie Tätigkeitsfeld.
- PASS: 13 eindeutige Tätigkeiten und gültige Verknüpfungen zu den vorhandenen Gefährdungskennungen.
- PASS: Interaktive HTML-Vorschau in Headless Chromium (zwei Tätigkeiten, sieben unterschiedliche Gefährdungen, Navigation und Updateanzeige); keine JavaScript-Laufzeitfehler.

## Nicht durchgeführt / keine Freigabe

- OFFEN: Kompilieren der echten WPF-App mit .NET 10 SDK unter Windows.
- OFFEN: Inno-Setup.exe erzeugen und als normaler Windows-Benutzer installieren.
- OFFEN: Windows-Druckdialog, Seitenumbruch, Logo und echte PDF-Ausgabe testen.
- OFFEN: Start, Speicherung, Datensicherung, Benutzeroberfläche und Update im Windows-Desktop testen.
- OFFEN: Signierter GitHub-Release, authentifizierter Updateprozess und Rollback-Test.
- OFFEN: Fachliche Prüfung der Textbausteine, Schutzmaßnahmen und Risikomethodik durch eine geeignete fachkundige Person.

**Fazit:** Quellcode-Testpaket, nicht produktiv einsatzbereit und keine freigegebene Windows-Installation.
