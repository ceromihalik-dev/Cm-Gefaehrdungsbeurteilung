# Updates per Datei – ab Version 0.1.13

## Ziel
CM Gefährdungsbeurteilung aktualisiert sich über vom Anwender **ausgewählte Update-Dateien**. Für die Auswahl und Installation ist keine Internetverbindung nötig.

## Einmaliger Übergang
Frühere Versionen (bis einschließlich 0.1.12) haben den Dateimport noch nicht. Daher muss **0.1.13 einmalig als Setup.exe manuell installiert** werden. Danach können neuere Versionen als .cmupdate-Datei eingelesen werden.

## Update erstellen
GitHub Actions (Windows Setup) erzeugt:
- `CM-Gefaehrdungsbeurteilung-Setup-0.1.13.exe`
- `CM-Gefaehrdungsbeurteilung-Update-0.1.13.cmupdate`
- `CM-Gefaehrdungsbeurteilung-Update-0.1.13.cmupdate.sha256` (Paketprüfsumme)
- Eine separate SHA-256-Prüfsumme des Installers.

Die .cmupdate-Datei ist ein ZIP-Archiv mit zwei Dateien:
1. `manifest.json` (SchemaVersion, Version, InstallerFile, Sha256, Notes)
2. `CM-Gefaehrdungsbeurteilung-Setup-<Version>.exe`

Die Versionsnummer wird aus der csproj-Datei gelesen. `scripts/New-UpdatePackage.py` erzeugt das Paket, `tests/Validate-UpdatePackage.py` prüft es.

## Update einlesen
1. In der Windows-Anwendung **Updates** öffnen.
2. **Update-Datei auswählen und prüfen** anklicken.
3. Die .cmupdate-Datei aus einem vertrauenswürdigen Ordner oder von einem USB-Stick auswählen.
4. Angezeigte Version und SHA-256-Paketprüfsumme prüfen. Bei einem GitHub-Artefakt die Prüfsumme mit der separat bereitgestellten .sha256-Datei vergleichen.
5. Sicherheitswarnung lesen und die Installation ausdrücklich bestätigen.
6. Die Anwendung sichert die SQLite-Datenbank unter `%LOCALAPPDATA%\CM\Gefaehrdungsbeurteilung\Backups`, überprüft die extrahierte Setup.exe und startet den Installationsassistenten.
7. Nach Abschluss das Programm erneut starten und Version, Projekte, Maßnahmen und Druckausgabe kontrollieren.

## Grenzen und Sicherheitsmodell
- **SHA-256 ist eine Integritätsprüfung und beweist keine Herausgeberidentität.** Ein Angreifer, der Paket und enthaltenes Manifest verändern kann, könnte beide Prüfsummen anpassen.
- Deshalb ist dieser Ablauf eine **manuell vom Anwender autorisierte Installation**, kein vollautomatisches signiertes Update. Nur aus Quellen beziehen, deren Vertrauenswürdigkeit unabhängig überprüft wurde.
- Optional kann der Herausgeber später eine digitale Paketsignatur oder ein vertrauenswürdig signiertes Setup hinzufügen. Ein gekaufter Code-Signing-Dienst ist für die manuelle Funktion nicht erforderlich.
- Der Import verwirft Dateien mit falscher Endung, fehlerhaftem Format, nicht neuerer Version, falschem Installernamen oder abweichender Prüfsumme.
- Bei Installationsfehlern bleiben die getrennt gespeicherten Projekte bestehen; die Wiederherstellung eines älteren Schemas muss im Einzelfall geprüft werden. Vor Tests eine zusätzliche Sicherung erstellen.
- Katalogvorlagen sind weiterhin fachkundig und objektspezifisch zu prüfen.

## Abnahme
- [ ] GitHub-Windows-Build erzeugt Setup.exe und .cmupdate.
- [ ] Der Paketintegritätstest besteht.
- [ ] Einmalige manuelle Installation von 0.1.13 startet korrekt.
- [ ] Mit nachfolgender Testversion wird eine .cmupdate-Datei eingelesen.
- [ ] Alte/defekte/manipulierte Pakete werden abgewiesen.
- [ ] Bestehende Beurteilungen bleiben nach einem Update erhalten.
- [ ] Ohne Internet und ohne Zertifikat läuft die manuelle Installation.
