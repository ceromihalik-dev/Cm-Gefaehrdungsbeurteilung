# Abnahmeprotokoll 0.1.10 – sichere Updateinstallation

**Stand:** vorbereitet, nicht als erfolgreich getestet kennzeichnen.

## Testfälle
| ID | Test | Erwartung |
|---|---|---|
| UP-01 | 0.1.9 installiert, signiertes Release 0.1.10 | Neue Version wird erkannt |
| UP-02 | Releasetag 0.1.10, aber Setup-Dateiname 0.1.9 | Update wird abgewiesen |
| UP-03 | Prüfsummen-Datei heißt anders | Update wird abgewiesen |
| UP-04 | Setup/Prüfsumme stammen aus unterschiedlichen GitHub-Releases | Update wird abgewiesen |
| UP-05 | SHA256SUMS.txt enthält zwei passende Zeilen | Update wird abgewiesen |
| UP-06 | SHA256SUMS.txt zu groß (über 128 KiB) | Update wird abgewiesen |
| UP-07 | SHA-256 stimmt nicht überein | Update wird abgewiesen |
| UP-08 | Falsche oder ungültige Authenticode-Signatur | Update wird abgewiesen |
| UP-09 | Unsignierter CI-Entwicklungsbuild | Button zur automatischen Installation bleibt deaktiviert |
| UP-10 | Signiertes, vollständig geprüftes Release | Bestätigen, Backup erstellen, Installer starten |
| UP-11 | Nach Installation bestehende Testbeurteilung bearbeiten, speichern, drucken | Keine Datenverluste; Version 0.1.10 |
| UP-12 | Fehler beim Download | Keine Teilinstallation; erneuter Versuch möglich |

## Voraussetzungen
- 0.1.10 erfolgreich über GitHub Actions kompiliert und als manuelles Testsetup geprüft.
- Signiertes Ausgangsprogramm mit fest eingebetteter Vertrauenskennung, gültiges Code-Signing-Zertifikat und signierter Release-Installer. Der erste Übergang von bisherigen unsignierten Builds muss manuell erfolgen.
- Nur Testdaten verwenden. Verschlüsselungsschlüssel/Zertifikatsdateien niemals ins GitHub-Repository einchecken.
- Keine fachliche Freigabe der Gefährdungsbeurteilungen aus einem erfolgreichen Softwaretest ableiten.

## Bekannte Grenze
GitHub Actions erstellt weiterhin nur ein unsigniertes Testartifact. Ein Artifact ist kein GitHub Release und wird deshalb von der automatischen In-App-Updateprüfung nicht als freigegebenes Update behandelt.
