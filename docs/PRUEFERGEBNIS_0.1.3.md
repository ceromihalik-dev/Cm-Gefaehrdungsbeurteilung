# Prüfergebnis – 0.1.3 / Build 0001 (14.10.2026)

## Tatsächlich lokal ausgeführt (Linux-Entwicklungscontainer)

| Prüfung | Ergebnis | Aussage |
|---|---|---|
| `python tests/Validate-Source.py` | PASS | XAML- und .csproj-XML gültig; 30 Ereignishandler auf passende C#-Methoden referenziert |
| SQLite-Migrationssimulation | PASS | Neu Schema 3 und Schema 1/2 → 3; Integrität und Testdatenerhaltung geprüft |
| 13 Tätigkeitseinträge + Katalogrelationen | PASS | Eindeutige Tätigkeiten und existierende Katalogreferenzen |
| Sicherheits-/Versionsvertrag | PASS | Versionszahlen vereinheitlicht; User-Eingabefeld für Signaturvertrauen entfernt; CI- und Release-Skripte vorhanden |
| `.github/workflows/windows-build.yml` | PASS – Syntax | YAML geparst. Kein Actions-Workflow ausgeführt |
| `Vorschau.html` | PASS – JS-Syntax | Inline-JavaScript mit `node --check` syntaktisch geprüft |

## Nicht ausgeführt / ausdrücklich offen

- **Kein echtes `dotnet publish`**, weil im aktuellen Container kein .NET SDK vorhanden ist.
- **Keine Windows-Setup.exe**, weil eine Windows-/Inno-Setup-Umgebung fehlt.
- WPF-Compiler, Windows-Laufzeitstart, Druck/PDF-Ausgabe und Installer-Test nicht ausgeführt.
- Signieren mit produktivem Authenticode-Zertifikat, SHA-256 + Signatur Ende-zu-Ende und GitHub-Releasedownload nicht ausgeführt.
- Sicherheitsrisiken aus dem Bestand und vollständige funktionale / fachliche Katalogprüfung nicht abschließend auditiert.

**Freigabestatus:** Entwicklungsstand. Nicht als produktive Arbeitsschutzsoftware oder freigegebenes Windows-Release bezeichnen.
