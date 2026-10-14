# CM Gefährdungsbeurteilung

Windows-Desktopsoftware zur Erstellung und Verwaltung von Gefährdungsbeurteilungen für Wasser- und Brandschadensanierung.

## Entwicklungsstand

- Lokaler Windows-Build: 0.1.3 / Build 0001 (vom Anwender erfolgreich als EXE erstellt)
- GitHub-Quellcodeübernahme: ausstehend
- Zielplattform: Windows 10/11, C# / WPF / .NET 10, SQLite
- Windows-Setup: Inno Setup 7
- Updatefähigkeit mit Datenerhalt ist verbindliche Anforderung.

## Nächste Schritte

1. Den vollständigen lokalen Projektordner `D:\cm-beurteilung` übernehmen, **ohne** `bin/`, `obj/`, lokale Datenbanken, Zugangsdaten oder Zertifikatsschlüssel.
2. GitHub Actions für Build, Tests und Setup-Artefakte einrichten.
3. Release- und Updateprozess mit signierten Installern, Prüfsummen und gesicherten Datenbankmigrationen testen.
4. Fachkataloge, Berichtsausgabe und Bedienoberfläche weiterentwickeln.

**Hinweis:** Vor fachlichem Einsatz müssen Vorlagen und Gefährdungsbeurteilungen sachkundig geprüft werden.
