# In-App-Update 0.1.6 – Freigabeverfahren

## Was bereits möglich ist
Die Anwendung fragt die GitHub-Releases-API des voreingestellten Repositories ab. Neue Releaseversionen werden angezeigt; die Aktualisierungsnotizen und ein Link zur Release-Seite können geöffnet werden.

## Sicherheit des automatischen Installers
Der Installationsbutton wird nur für Builds mit beim Herausgeber-Build fest eingebettetem Zertifikatsfingerabdruck aktiviert, wenn das GitHub Release das passende Setup und SHA256SUMS.txt enthält. Beim Download prüft die Anwendung SHA-256 und die gültige Authenticode-Signatur gegen den fest hinterlegten Herausgeber. Die aktuelle CI erstellt nur **unsignierte Test-Artefakte**; diese dürfen nicht automatisch installiert werden.

**Wichtige Grenze:** Vor dem ersten produktiven In-App-Update muss ein signierter Ausgangsbuild mit korrekt eingebettetem Herausgeber-Fingerabdruck installiert sein. Ein unsignierter Altbuild wird aus Sicherheitsgründen nicht automatisch zum signierten Update migrieren. Für diesen ersten Übergang ist eine manuelle Installation erforderlich.

## Freigabeablauf
1. Ein vertrauenswürdiges Code-Signing-Zertifikat bereitstellen und seinen Fingerabdruck außerhalb des Repositories verwalten.
2. Auf einem kontrollierten Herausgeber-Windows-System: Build-Setup.ps1 mit -TrustedSignerThumbprint ausführen.
3. Setup.exe mit Authenticode und vertrauenswürdigem Zeitstempel signieren.
4. Prepare-SignedRelease.ps1 mit dem erwarteten Herausgeberfingerabdruck ausführen; das Skript prüft die Signatur und erzeugt SHA256SUMS.txt.
5. Signierte Setup.exe und SHA256SUMS.txt unter dem GitHub Release-Tag v0.1.6 veröffentlichen; kein Prerelease.
6. Installations- und Datenübernahmetest mit Kopie bestehender Projektdaten durchführen. Automatische und manuelle Updates separat testen.
7. Erst nach erfolgreichem Sicherheits-/Migrationstest freigeben.

## Tests
- [ ] Unsigned Build: Prüfen nach Updates erlaubt, Installationsbutton deaktiviert.
- [ ] Signierter Testbuild erkennt GitHub Release v0.1.6.
- [ ] Falsche SHA-256: Download wird abgewiesen.
- [ ] Falscher/ungültiger Zertifikatsfingerabdruck: Ausführung wird abgewiesen.
- [ ] Richtige Signatur und Prüfsumme: Installer startet nach Bestätigung und Datensicherung.
- [ ] Projekte/Beurteilungen/Settings nach Update unverändert.
- [ ] Ausfall und Wiederherstellung dokumentiert.

Keine Zertifikatsschlüssel oder Geheimnisse im Repository speichern. Eine GitHub-Actions-Artifact-ZIP ist kein GitHub Release.
