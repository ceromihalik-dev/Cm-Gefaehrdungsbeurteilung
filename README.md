# CM Gefährdungsbeurteilung

Windows-Desktopprogramm zur Erstellung und Dokumentation von Gefährdungsbeurteilungen für die Wasser- und Brandschadensanierung.

## Aktueller Entwicklungsstand

- **Quellcode:** 0.1.13 / Build 0001 (Windows-Test noch ausstehend)
- **Bestätigter Bedienstand:** 0.1.11 (Anwenderprüfung PASS)
- **Technik:** C# / WPF / .NET 10 / SQLite / Inno Setup
- **Updateverfahren ab 0.1.13:** lokale, vom Benutzer ausgewählte `.cmupdate`-Datei – kein Internet zum Installieren erforderlich
- **Datenbank:** Schema 3, Nutzerdaten vom Installationsordner getrennt
- **Fachliche Einträge:** Entwürfe, keine automatisch freigegebenen Gefährdungsbeurteilungen

## Windows-Setup und Update-Dateien

[GitHub Actions – Windows Setup](https://github.com/ceromihalik-dev/Cm-Gefaehrdungsbeurteilung/actions/workflows/windows-build.yml) erstellt als Testartefakt:

- `CM-Gefaehrdungsbeurteilung-Setup-<Version>.exe`
- `CM-Gefaehrdungsbeurteilung-Update-<Version>.cmupdate`
- SHA-256-Prüfsummendateien

**Einmalig:** Version 0.1.13 muss noch per Setup.exe installiert werden, da ältere Versionen keine lokale Update-Datei importieren können. Danach kann eine neuere `.cmupdate` über **Updates → Update-Datei auswählen und prüfen** eingelesen werden.

**Sicherheit:** Eine SHA-256-Prüfsumme belegt die Integrität des Paketinhalts, nicht die Identität des Herausgebers. Unsignierte Pakete nur aus einer eigenständig geprüften Quelle und mit ausdrücklicher Bestätigung installieren. Es gibt keine stille Ausführung von Update-Dateien.

[Anleitung für Datei-Updates](docs/DATEI_UPDATES.md) · [Changelog](CHANGELOG.md)

## Fachlicher Hinweis

Die Gefährdungsvorschläge sind nicht abschließend. Vor Einsatz und Unterzeichnung müssen die objektspezifischen Gefährdungen und Maßnahmen durch eine fachkundige verantwortliche Person beurteilt werden.
